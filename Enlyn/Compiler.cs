namespace Enlyn;

public class CStandard
{

    public static CConstruct Any { get; } = new() { Index = Executable.Any, Standard = true };

    public static CConstruct Number { get; } = new() { Index = Executable.Number, Standard = true, Parent = Any };
    public static CConstruct String { get; } = new() { Index = Executable.String, Standard = true, Parent = Any };
    public static CConstruct Boolean { get; } = new() { Index = Executable.Boolean, Standard = true, Parent = Any };

    public static CConstruct IO { get; } = new() { Index = Executable.IO, Standard = true, Parent = Any };

    public static Map<TypeNode, CConstruct> Constructs => new()
    {
        [new() { Value = "any" }] = Any,
        [new() { Value = "number" }] = Number,
        [new() { Value = "string" }] = String,
        [new() { Value = "boolean" }] = Boolean,
        [new() { Value = "IO" }] = IO
    };


    static CStandard()
    {
        String.Fields[new() { Value = "length" }] = 0;
    }

}

public class CConstruct
{

    public int Index { get; init; }
    public bool Standard { get; init; } = false;

    public Map<IdentifierNode, int> Fields { get; } = new();
    public Map<IIdentifierNode, CChunk> Chunks { get; } = new();


    private CConstruct? parent;
    public CConstruct? Parent
    {
        get => parent;
        internal set
        {
            parent = value;
            Fields.Parent = parent!.Fields;
        }
    }

    public Construct ToConstruct()
    {
        if (Standard) throw new Exception("not allowed");

        Dictionary<IIdentifierNode, IChunk> chunks = new();
        foreach (KeyValuePair<IIdentifierNode, CChunk> pair in Chunks) chunks[pair.Key] = pair.Value.ToChunk();
    
        return new()
        {
            Parent = parent!.Index,
            Fields = Fields.Count,
            Chunks = chunks
        };
    }

}

public class CChunk
{

    public List<IOpcode> Instructions { get; } = new();
    public Map<IdentifierNode, int> Scope { get; private set; } = new();

    public int Arguments { get; set; }

    private int locals;

    private int offset = 0;
    private readonly Stack<int> offsetStack = new();


    public void Emit(IOpcode opcode) => Instructions.Add(opcode);

    public void AddThis() { offset++; locals = 1; }
    public int AddVariable(IdentifierNode node)
    {
        Scope[node] = offset++;
        if (offset > locals) locals = offset;

        return offset - 1;
    }

    public void Enter()
    {
        Scope = new(Scope);
        offsetStack.Push(offset);
    }

    public void Exit()
    {
        Scope = Scope.Parent!;
        offset = offsetStack.Pop();
    }

    public Chunk ToChunk() => new()
    {
        Instructions = Instructions.ToArray(),
        Arguments = Arguments,
        Locals = locals
    };

}

public class Compiler : ASTVisitor<object?>
{

    private Environment Environment { get; }
    private readonly ErrorLogger error;

    private int typeIndex = CStandard.Constructs.Count;
    private readonly Map<TypeNode, CConstruct> constructs = CStandard.Constructs;
    
    private readonly List<Instance> constants = new();

    private CConstruct currentConstruct = null!;
    private CChunk currentChunk = null!;


    public Compiler(Environment environment, ErrorLogger error)
    {
        Environment = environment;
        this.error = error;
    }


    private int AddConstant(Instance instance)
    {
        constants.Add(instance);
        return constants.Count - 1;
    }

    public Executable Compile(ProgramNode program)
    {
        Visit(program);
        List<Construct> cList = new();
        foreach (CConstruct c in constructs.Values) if (!c.Standard) cList.Add(c.ToConstruct());

        return new()
        {
            Constructs = Executable.standard.Concat(cList).ToArray(),
            Main = constructs[new TypeNode { Value = "Main" }].Index,
            Constants = constants.ToArray()
        };
    }

    public override object? Visit(ProgramNode program)
    {
        foreach (ClassNode node in program.Classes)
            constructs[node.Identifier] = new CConstruct { Index = typeIndex++ };
        foreach (ClassNode node in program.Classes)
        {
            CConstruct parent = node.Parent is TypeNode name ? constructs[name] : CStandard.Any;
            constructs[node.Identifier].Parent = parent;
        }

        foreach (ClassNode node in program.Classes) Visit(node);
        return null;
    }

    public override object? Visit(ClassNode node)
    {
        ConstructorNode? constructor = null;
        List<FieldNode> fields = new();
        List<MethodNode> methods = new();

        foreach (IMemberNode member in node.Members) switch (member)
        {
            case ConstructorNode c: constructor = c; break;
            case FieldNode field: fields.Add(field); break;
            case MethodNode method: methods.Add(method); break;
        }

        currentConstruct = constructs[node.Identifier];

        Visit(constructor, fields.ToArray());
        foreach (MethodNode method in methods) Visit(method);

        return null;
    }

    private void Visit(ConstructorNode? node, FieldNode[] fields)
    {
        CChunk chunk = node is null ? new() { Arguments = 1 } : new() { Arguments = node.Parameters.Length + 1 };
        currentConstruct.Chunks[Environment.constructor] = chunk;
        currentChunk = chunk;

        chunk.AddThis();

        if (node is null)
        {
            // TODO: Generate base call
        }
        else foreach (ParameterNode param in node.Parameters) chunk.AddVariable(param.Identifier);
        
        // TODO: Emit field initializers
        if (node is not null) Visit(node.Body);

        chunk.Emit(new NULL());
        chunk.Emit(new RETURN());
    }

    public override object? Visit(MethodNode node)
    {
        CChunk chunk = new() { Arguments = node.Parameters.Length + 1 };
        currentConstruct.Chunks[node.Identifier] = chunk;
        currentChunk = chunk;
        
        chunk.AddThis();
        foreach (ParameterNode param in node.Parameters) chunk.AddVariable(param.Identifier);

        Visit(node.Body);
        chunk.Emit(new NULL());
        chunk.Emit(new RETURN());

        return null;
    }


    public override object? Visit(LetNode node)
    {
        int i = currentChunk.AddVariable(node.Identifier);
        Visit(node.Expression);
        currentChunk.Emit(new STORE(i));
        return null;
    }

    public override object? Visit(ReturnNode node)
    {
        if (node.Expression is null) currentChunk.Emit(new NULL());
        else Visit(node.Expression);
        currentChunk.Emit(new RETURN());
        return null;
    }

    public override object? Visit(BlockNode node)
    {
        foreach (IStatementNode statement in node.Statements) Visit(statement);
        return null;
    }

    public override object? Visit(ExpressionStatementNode node)
    {
        Visit(node.Expression);
        currentChunk.Emit(new POP());

        return null;
    }


    public override object? Visit(CallNode node)
    {
        AccessNode access = (AccessNode) node.Target;
        Visit(access.Target);
        foreach (IExpressionNode expression in node.Arguments) Visit(expression);

        currentChunk.Emit(new CALL(5, access.Identifier)); // TODO: Store call type information in type checker
        return null;
    }

    public override object? Visit(NewNode node)
    {
        int i = constructs[node.Type].Index;
        currentChunk.Emit(new NEW(i));
        foreach (IExpressionNode expression in node.Arguments) Visit(expression);

        currentChunk.Emit(new CALL(i, Environment.constructor));
        return null;
    }

    public override object? Visit(IdentifierNode node)
    {
        int i = currentChunk.Scope[node];
        currentChunk.Emit(new LOAD(i));
        return null;
    }

    public override object? Visit(NumberNode node)
    {
        if (node.Value == 0) currentChunk.Emit(new ZERO());
        else if (node.Value == 1) currentChunk.Emit(new ONE());
        else
        {
            int i = AddConstant(new NumberInstance { Value = node.Value });
            currentChunk.Emit(new CONST(i));
        }

        return null;
    }

    public override object? Visit(StringNode node)
    {
        int i = AddConstant(new StringInstance(node.Value));
        currentChunk.Emit(new CONST(i));
        return null;
    }

    public override object? Visit(BooleanNode node)
    {
        currentChunk.Emit(node.Value ? new TRUE() : new FALSE());
        return null;
    }

    public override object? Visit(BaseNode _) => This();
    public override object? Visit(ThisNode _) => This();

    private object? This()
    {
        currentChunk.Emit(new LOAD(0));
        return null;
    }

    public override object? Visit(NullNode _)
    {
        currentChunk.Emit(new NULL());
        return null;
    }

}
