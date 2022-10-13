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

    public ClassNode Node { get; init; } = null!;


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
            Fields = fieldCount,
            Chunks = chunks
        };
    }

    private int CountFields()
    {
        if (Standard) return Fields.Count;

        int fields = parent?.CountFields() ?? 0;
        foreach (IMemberNode member in Node.Members) if (member is FieldNode) fields++;

        return fields;
    }

    private int fieldCount;
    public void InitializeFields()
    {
        fieldCount = CountFields();
        int offset = fieldCount;
        foreach (IMemberNode member in Node.Members) if (member is FieldNode) offset--;

        foreach (IMemberNode member in Node.Members) if (member is FieldNode field)
            Fields[field.Identifier] = offset++;
    }

}

public class CChunk
{

    public List<IOpcode> Instructions { get; } = new();
    public Map<IdentifierNode, int> Scope { get; private set; } = new();

    public int Arguments { get; set; }

    private int locals = 0;

    private int offset = 0;
    private readonly Stack<int> offsetStack = new();


    public void Emit(IOpcode opcode) => Instructions.Add(opcode);
    public int EmitPlaceholder()
    {
        Instructions.Add(null!);
        return Instructions.Count - 1;
    }

    public void Emit(int i, IOpcode opcode) => Instructions[i] = opcode;

    public void AddThis() { offset++; locals++; }
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

    private CConstruct curConst = null!;
    private CChunk curr = null!;


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
        List<Construct> list = new();
        foreach (CConstruct c in constructs.Values) if (!c.Standard) list.Add(c.ToConstruct());

        return new()
        {
            Constructs = Executable.standard.Concat(list).ToArray(),
            Main = constructs[new TypeNode { Value = "Main" }].Index,
            Constants = constants.ToArray()
        };
    }

    public override object? Visit(ProgramNode program)
    {
        foreach (ClassNode node in program.Classes)
            constructs[node.Identifier] = new CConstruct { Index = typeIndex++, Node = node };

        List<CConstruct> localConstructs = new();
        foreach (ClassNode node in program.Classes)
        {
            CConstruct current = constructs[node.Identifier];
            CConstruct parent = node.Parent is TypeNode name ? constructs[name] : CStandard.Any;
            current.Parent = parent;

            localConstructs.Add(current);
        }

        foreach (CConstruct construct in localConstructs) construct.InitializeFields();
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

        curConst = constructs[node.Identifier];

        Visit(fields.ToArray());
        if (constructor is not null) Visit(constructor);
        foreach (MethodNode method in methods) Visit(method);

        return null;
    }

    public static IdentifierNode fieldInit = new() { Value = "null" };
    private void Visit(FieldNode[] fields)
    {
        CChunk chunk = new() { Arguments = 1 };
        curConst.Chunks[fieldInit] = chunk;
        curr = chunk;

        chunk.AddThis();

        // Base call
        curr.Emit(new LOAD(0));
        curr.Emit(new INVOKE(curConst.Parent!.Index, fieldInit));
        curr.Emit(new POP());

        foreach (FieldNode field in fields)
        {
            if (field.Expression is null) continue;

            chunk.Emit(new LOAD(0));
            Visit(field.Expression);
            chunk.Emit(new SETF(curConst.Fields[field.Identifier]));
            chunk.Emit(new POP());
        }

        chunk.Emit(new NULL());
        chunk.Emit(new RETURN());
    }

    public override object? Visit(MethodNode node)
    {
        CChunk chunk = new() { Arguments = node.Parameters.Length + 1 };
        curConst.Chunks[node.Identifier] = chunk;
        curr = chunk;
        
        chunk.AddThis();
        foreach (ParameterNode param in node.Parameters) chunk.AddVariable(param.Identifier);

        Visit(node.Body);
        chunk.Emit(new NULL());
        chunk.Emit(new RETURN());

        return null;
    }

    public override object? Visit(ConstructorNode node)
    {
        CChunk chunk = new() { Arguments = node.Parameters.Length + 1 };
        curConst.Chunks[Environment.constructor] = chunk;
        curr = chunk;

        chunk.AddThis();
        foreach (ParameterNode param in node.Parameters) chunk.AddVariable(param.Identifier);

        // Base call
        if (node.Arguments is not null)
        {
            curr.Emit(new LOAD(0));
            foreach (IExpressionNode expression in node.Arguments) Visit(expression);
            curr.Emit(new INVOKE(curConst.Parent!.Index, Environment.constructor));
            curr.Emit(new POP());
        }

        Visit(node.Body);

        chunk.Emit(new NULL());
        chunk.Emit(new RETURN());

        return null;
    }


    public override object? Visit(LetNode node)
    {
        int i = curr.AddVariable(node.Identifier);
        Visit(node.Expression);
        curr.Emit(new STORE(i));
        return null;
    }

    public override object? Visit(IfNode node) // TODO: Control flow
    {
        Visit(node.Condition);
        int thenJump = curr.EmitPlaceholder();
        Visit(node.Then);

        if (node.Else is not null)
        {
            int elseJump = curr.EmitPlaceholder();
            curr.Emit(thenJump, new JUMPF(curr.Instructions.Count));
            Visit(node.Else);

            curr.Emit(elseJump, new JUMP(curr.Instructions.Count));
        }
        else curr.Emit(thenJump, new JUMPF(curr.Instructions.Count));
        return null;
    }

    public override object? Visit(WhileNode node)
    {
        int start = curr.Instructions.Count;
        Visit(node.Condition);
        int jump = curr.EmitPlaceholder();
        Visit(node.Body);

        curr.Emit(new JUMP(start));
        curr.Emit(jump, new JUMPF(curr.Instructions.Count));
        return null;
    }

    public override object? Visit(ReturnNode node)
    {
        if (node.Expression is null) curr.Emit(new NULL());
        else Visit(node.Expression);
        curr.Emit(new RETURN());
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
        curr.Emit(new POP());

        return null;
    }


    public override object? Visit(AccessNode node)
    {
        Visit(node.Target);

        int i = constructs[Environment.Types[node].Name].Fields[node.Identifier];
        curr.Emit(new GETF(i));
        return null;
    }

    public override object? Visit(CallNode node)
    {
        AccessNode access = (AccessNode) node.Target;
        Visit(access.Target);
        foreach (IExpressionNode expression in node.Arguments) Visit(expression);

        int i = constructs[Environment.Types[node].Name].Index;
        curr.Emit(new VIRTUAL(i, access.Identifier));
        return null;
    }

    public override object? Visit(NewNode node)
    {
        int i = constructs[node.Type].Index;
        curr.Emit(new NEW(i));
        curr.Emit(new COPY());

        foreach (IExpressionNode expression in node.Arguments) Visit(expression);
        curr.Emit(new INVOKE(i, Environment.constructor));
        curr.Emit(new POP());
        return null;
    }

    public override object? Visit(AssertNode node) => Visit(node.Expression);
    public override object? Visit(AssignNode node)
    {
        if (node.Target is IdentifierNode id)
        {
            Visit(node.Expression);
            curr.Emit(new STORE(curr.Scope[id]));
        }
        else if (node.Target is AccessNode access)
        {
            Visit(access.Target);
            Visit(node.Expression);

            int i = constructs[Environment.Types[access].Name].Fields[access.Identifier];
            curr.Emit(new SETF(i));
        }
        return null;
    }

    public override object? Visit(InstanceNode node)
    {
        Visit(node.Expression);
        if (node.Type is null)
        {
            curr.Emit(new INST(0, false));
            curr.Emit(new NOT());
        }
        else
        {
            (int i, bool option) = GetOpcodeArgs(node.Type);
            curr.Emit(new INST(i, option));
        }
        return null;
    }

    public override object? Visit(CastNode node)
    {
        Visit(node.Expression);
        (int i, bool option) = GetOpcodeArgs(node.Type);
        curr.Emit(new CAST(i, option));
        return null;
    }

    private (int i, bool option) GetOpcodeArgs(ITypeNode node) => node switch
    {
        OptionNode option => (GetOpcodeArgs(option.Type).i, true),
        TypeNode type => (constructs[type].Index, false),
        _ => throw new Exception("unexpected")
    };

    public override object? Visit(BinaryNode node)
    {
        Visit(node.Left);
        Visit(node.Right);
        Type type = Environment.Types[node];

        if (type == Standard.Number)
        {
            IOpcode? opcode = null;
            switch (node.Operation)
            {
                case Operation.Add: opcode = new ADD(); break;
                case Operation.Sub: opcode = new SUB(); break;
                case Operation.Mul: opcode = new MUL(); break;
                case Operation.Div: opcode = new DIV(); break;
                case Operation.Mod: opcode = new MOD(); break;

                case Operation.Lt:  opcode = new LT();  break;
                case Operation.Gt:  opcode = new GT();  break;
                case Operation.Le:  opcode = new LE();  break;
                case Operation.Ge:  opcode = new GE();  break;
            }

            if (opcode is not null)
            {
                curr.Emit(opcode);
                return null;
            }
        }
        else if (type == Standard.Boolean)
        {
            IOpcode? opcode = null;
            switch (node.Operation)
            {
                case Operation.And: opcode = new AND(); break;
                case Operation.Or:  opcode = new OR();  break;
            }

            if (opcode is not null)
            {
                curr.Emit(opcode);
                return null;
            }
        }

        int i = constructs[type.Name].Index;
        curr.Emit(new VIRTUAL(i, new BinaryIdentifierNode() { Operation = node.Operation }));

        return null;
    }

    public override object? Visit(UnaryNode node)
    {
        Visit(node.Expression);
        Type type = Environment.Types[node];

        if (type == Standard.Number && node.Operation == Operation.Neg)
        {
            curr.Emit(new NEG());
            return null;
        }
        else if (type == Standard.Boolean && node.Operation == Operation.Not)
        {
            curr.Emit(new NOT());
            return null;
        }

        int i = constructs[type.Name].Index;
        curr.Emit(new VIRTUAL(i, new BinaryIdentifierNode() { Operation = node.Operation }));

        return null;
    }

    public override object? Visit(IdentifierNode node)
    {
        int i = curr.Scope[node];
        curr.Emit(new LOAD(i));
        return null;
    }

    public override object? Visit(NumberNode node)
    {
        if (node.Value == 0) curr.Emit(new ZERO());
        else if (node.Value == 1) curr.Emit(new ONE());
        else
        {
            int i = AddConstant(new NumberInstance { Value = node.Value });
            curr.Emit(new CONST(i));
        }

        return null;
    }

    public override object? Visit(StringNode node)
    {
        int i = AddConstant(new StringInstance(node.Value));
        curr.Emit(new CONST(i));
        return null;
    }

    public override object? Visit(BooleanNode node)
    {
        curr.Emit(node.Value ? new TRUE() : new FALSE());
        return null;
    }

    public override object? Visit(BaseNode _) => This();
    public override object? Visit(ThisNode _) => This();

    private object? This()
    {
        curr.Emit(new LOAD(0));
        return null;
    }

    public override object? Visit(NullNode _)
    {
        curr.Emit(new NULL());
        return null;
    }

}
