namespace Enlyn;

public class CStandard
{

    public static CConstruct Unit { get; } = new() { Index = 0, Standard = true };
    public static CConstruct Any { get; } = new() { Index = 1, Standard = true };

    public static CConstruct Number { get; } = new() { Index = 2, Standard = true, Parent = Any };
    public static CConstruct String { get; } = new() { Index = 3, Standard = true, Parent = Any };
    public static CConstruct Boolean { get; } = new() { Index = 4, Standard = true, Parent = Any };

    public static CConstruct IO { get; } = new() { Index = 5, Standard = true, Parent = Any };

    public static Map<TypeNode, CConstruct> Constructs => new()
    {
        [new() { Value = "unit" }] = Unit,
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

}

public class CChunk
{

    public List<IOpcode> Instructions { get; } = new();
    public List<Instance> Constants { get; } = new();

    public Dictionary<IdentifierNode, int> Scope { get; } = new();

    public int Arguments { get; init; }


    public void Emit(IOpcode opcode) => Instructions.Add(opcode);
    public int AddConstant(Instance instance)
    {
        Constants.Add(instance);
        return Constants.Count - 1;
    }

}

public class Compiler : ASTVisitor<object?>
{

    private Environment Environment { get; }
    private readonly ErrorLogger error;

    private int typeIndex = CStandard.Constructs.Count;
    private readonly Map<TypeNode, CConstruct> constructs = CStandard.Constructs;

    private CConstruct currentConstruct = null!;
    private CChunk currentChunk = null!;


    public Compiler(Environment environment, ErrorLogger error)
    {
        Environment = environment;
        this.error = error;
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
        CChunk chunk;
        if (node is null)
        {
            chunk = new() { Arguments = 0 };
            // TODO: Generate base call
        }
        else
        {
            chunk = new() { Arguments = node.Parameters.Length };
        }

        currentConstruct.Chunks[Environment.constructor] = chunk;
        currentChunk = chunk;

        // TODO: Emit field initializers
    }

    public override object? Visit(MethodNode node)
    {
        CChunk chunk = new() { Arguments = node.Parameters.Length };
        currentConstruct.Chunks[node.Identifier] = chunk;
        currentChunk = chunk;

        Visit(node.Body);
        return null;
    }


    public override object? Visit(LetNode node)
    {
        return null;
    }

    public override object? Visit(ExpressionStatementNode node) => Visit(node.Expression);


    public override object? Visit(IdentifierNode node)
    {
        int i = currentChunk.Scope[node];
        currentChunk.Emit(new LOAD(i));
        return null;
    }

    public override object? Visit(NumberNode node)
    {
        int i = currentChunk.AddConstant(new NumberInstance { Value = node.Value });
        currentChunk.Emit(new CONST(i));
        return null;
    }

    public override object? Visit(StringNode node)
    {
        int i = currentChunk.AddConstant(new StringInstance(node.Value));
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
