namespace Enlyn;

public class Map<K, V> : Dictionary<K, V> where K : notnull
{

    public Map<K, V>? Parent { get; internal set; }

    public Map(Map<K, V> parent) => Parent = parent;
    public Map() { }


    public new V this[K key]
    {
        get
        {
            if (ContainsKey(key)) return base[key];
            throw new EnlynError($"{key} not found");
        }
        set
        {
            if (ContainsKey(key)) throw new EnlynError($"Redefinition of {key}");
            base[key] = value;
        }
    }

    public bool Exists(K key) => ContainsKey(key) || (Parent?.Exists(key) ?? false);

}

public interface IType { }

public class Option : IType { public IType Type { get; init; } = null!; }
public class Type : IType
{

    public TypeNode Name { get; init; }

    private Type? parent;
    public Type? Parent
    {
        get => parent;
        internal set
        {
            parent = value;
            if (parent is null) return;

            Fields.Parent = parent.Fields;
            Methods.Parent = parent.Methods;
        }
    }

    public Map<IIdentifierNode, Field> Fields { get; init; } = new();
    public Map<IIdentifierNode, Method> Methods { get; init; } = new();


    public Type(string name) => Name = new TypeNode { Value = name };
    public Type() { }

}

public class Field
{

    public Access Access { get; init; }
    public IType Type { get; init; } = null!;

}

public class Method
{

    public Access Access { get; init; }

    public IType[] Parameters { get; init; } = null!;
    public IType Return { get; init; } = null!;

}

public class Environment
{

    private Map<IdentifierNode, IType> scope = new();
    public Map<TypeNode, Type> Classes { get; } =new()
    {
        [Standard.unit.Name   ] = Standard.unit,
        [Standard.any.Name    ] = Standard.any,
        [Standard.number.Name ] = Standard.number,
        [Standard.strType.Name] = Standard.strType,
        [Standard.boolean.Name] = Standard.boolean
    };


    public IType this[IdentifierNode name]
    {
        get => scope[name];
        set => scope[name] = value;
    }

    public void Enter() => scope = new Map<IdentifierNode, IType>(scope);
    public void Exit() => scope = scope.Parent!;

    public void Test(IType expected, IType? target)
    {
        if (expected is Option option) switch (target) // Null case falls through
        {
            case Option t: Test(option.Type, t.Type); break;
            case Type type: Test(option.Type, type); break;
        }
        else if (expected is Type e) switch (target)
        {
            case Option or null: throw new EnlynError($"Type {e.Name} is not an option");
            case Type t:
            {
                if (Check((Type) expected, t)) break;
                throw new EnlynError($"Type {t.Name} is not compatible with {e.Name}");
            }
        }

        bool Check(Type expected, Type target)
        {
            if (expected == target) return true;
            if (target.Parent is null) return false;

            // Check if expected type is a parent of the target
            return Check(expected, target.Parent);
        }
    }

}

internal static class Standard
{

    public static readonly Type unit = new("unit");
    public static readonly Type any = new("any")
    {
        Methods = new()
        {

        }
    };

    public static readonly Type number = new("number") { Parent = any };
    public static readonly Type strType = new("string") { Parent = any };
    public static readonly Type boolean = new("boolean") { Parent = any };


    public static readonly IdentifierNode current = new() { Value = "this" };
    public static readonly IdentifierNode constructor = new() { Value = "new" };
    public static readonly IdentifierNode method = new() { Value = "return" };

}

public abstract class EnvironmentVisitor<T> : ASTVisitor<T>
{

    public Environment Environment { get; }

    protected EnvironmentVisitor(Environment environment) => Environment = environment;


    protected Type This
    {
        get => (Type) Environment[Standard.current];
        set => Environment[Standard.current] = value;
    }

    protected Type Return
    {
        get => (Type) Environment[Standard.method];
        set => Environment[Standard.method] = value;
    }

}

public class TypeChecker : EnvironmentVisitor<object?>
{

    private readonly ErrorLogger error;

    public TypeChecker(ErrorLogger error) : base(new Environment()) => this.error = error;


    private record struct ClassData(ClassNode node, Type type);
    public override object? Visit(ProgramNode program)
    {
        // Initialize type objects
        List<ClassData> nodes = new();
        foreach (ClassNode node in program.Classes)
        {
            Type type = new() { Name = node.Identifier };
            error.Catch(node.Location, () =>
            {
                Environment.Classes[node.Identifier] = type;
                nodes.Add(new ClassData(node, type));
            });
        }

        // Initialize parent graph and check for cycles
        foreach ((ClassNode node, Type type) in nodes) error.Catch(node.Location, () =>
        {
            Type parent = node.Parent is TypeNode name ? Environment.Classes[name] : Standard.any;
            type.Parent = parent;
        });
        CheckCycles(program.Classes, from data in nodes select data.type);

        // Initialize and check members
        foreach ((ClassNode node, Type type) in nodes) foreach (IMemberNode member in node.Members)
            InitializeMember(type, (dynamic) member);
        foreach ((ClassNode node, Type type) in nodes)
        {
            Environment.Enter();
            This = type;

            foreach (IMemberNode member in node.Members) Visit(member);
            Environment.Exit();
        }

        return null;
    }

    private void CheckCycles(ClassNode[] nodes, IEnumerable<Type> types)
    {
        // Perform depth-first search starting at each node
        List<Type> visited = new();
        foreach (Type type in types) Check(type, new List<Type>());

        void Check(Type type, List<Type> children)
        {
            if (visited.Contains(type)) return;
            visited.Add(type);
            children.Add(type);

            if (type.Parent is not Type parent) return;

            // Cycle is detected if parent has already been traversed
            if (!children.Contains(parent)) Check(parent, children);
            else foreach (ClassNode node in nodes) if (node.Identifier == parent.Name)
            {
                error.Report($"Cyclic inheritance found at {parent.Name}", node.Location);
                return;
            }
        }
    }

    private void InitializeMember(Type type, FieldNode node) => error.Catch(node.Location, () =>
        type.Fields.Add(node.Identifier, new Field
        {
            Access = node.Access,
            Type = new TypeVisitor(Environment).Visit(node.Type)
        }));

    private void InitializeMember(Type type, MethodNode node) => error.Catch(node.Location, () =>
    {
        // Get return and parameter types
        IType returnType = node.Return switch  
        {
            ITypeNode typeNode => new TypeVisitor(Environment).Visit(typeNode),
            null => Standard.unit // Defaults to unit if type is unspecified
        };

        // Check operator cases
        int length = node.Parameters.Length;
        IIdentifierNode identifier = node.Identifier switch
        {
            BinaryIdentifierNode when length != 1 =>
                throw new EnlynError("Binary operation must be defined with 1 parameter"),
            UnaryIdentifierNode when length != 0 =>
                throw new EnlynError("Unary operation cannot be defined with parameter"),

            IIdentifierNode node => node
        };

        type.Methods.Add(identifier, new Method
        {
            Access = node.Access,
            Parameters = InitializeParameters(node.Parameters),
            Return = returnType
        });
    });

    private void InitializeMember(Type type, ConstructorNode node) => error.Catch(node.Location, () =>
        type.Methods.Add(Standard.constructor, new Method
        {
            Access = node.Access,
            Parameters = InitializeParameters(node.Parameters),
            Return = Standard.unit
        }));

    private IType[] InitializeParameters(ParameterNode[] parameters)
    {
        TypeVisitor visitor = new TypeVisitor(Environment);
        IEnumerable<IType> types =
            from parameter in parameters
            select visitor.Visit(parameter.Type);

        return types.ToArray();
    }

    public override object? Visit(FieldNode node) => error.Catch(node.Location, () =>
    {
        Field field = This.Fields[node.Identifier];
        if (node.Expression is IExpressionNode expression)
        {
            IType? type = new ExpressionVisitor(Environment).Visit(expression);
            Environment.Test(field.Type, type);
        }
    });

    public override object? Visit(MethodNode node) => error.Catch(node.Location, () =>
    {
        Method method = This.Methods[node.Identifier];
        CheckOverride(method, node);

        // TODO: Check method body
        Environment.Enter();
        Environment.Exit();
    });

    private void CheckOverride(Method method, MethodNode node)
    {
        // Test if parent has a method by the same name
        Type parent = This.Parent!;
        if (!parent.Methods.Exists(node.Identifier))
        {
            if (node.Override) throw new EnlynError($"No method {node.Identifier} found to override");
            return;
        }

        Method previous = parent.Methods[node.Identifier];
        // TODO: Override rules
    }

    public override object? Visit(ConstructorNode node) => error.Catch(node.Location, () =>
    {
        Environment.Enter();
        Environment.Exit();
    });

}

internal class ControlVisitor : ASTVisitor<bool>
{

    // TODO: Control flow analysis

}

internal class TypeVisitor : EnvironmentVisitor<IType>
{

    public TypeVisitor(Environment environment) : base(environment) { }


    public override IType Visit(TypeNode node) => Environment.Classes[node];
    public override IType Visit(OptionNode node) => new Option { Type = Visit(node.Type) };

}

internal class ExpressionVisitor : EnvironmentVisitor<IType?>
{

    public ExpressionVisitor(Environment environment) : base(environment) { }


    public override IType Visit(IdentifierNode node) => Environment[node];

    public override IType Visit(NumberNode node) => Standard.number;
    public override IType Visit(StringNode node) => Standard.strType;
    public override IType Visit(BooleanNode _) => Standard.boolean;

    public override IType Visit(ThisNode _) => This;
    public override IType Visit(BaseNode _) => This.Parent!;

}
