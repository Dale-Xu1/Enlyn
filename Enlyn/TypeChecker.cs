namespace Enlyn;

public class Map<K, V> : Dictionary<K, V> where K : notnull
{

    public Map(Map<K, V> map) : base(map) { }
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

}

public interface IType { }

public class Option : IType { public IType Type { get; init; } = null!; }
public class Type : IType
{

    public TypeNode Name { get; init; }
    public Type? Parent { get; set; }

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

    public Map<TypeNode, Type> Classes { get; } = new(StandardLibrary.classes);
    private readonly Stack<Dictionary<IdentifierNode, IType>> scope = new();


    public void Enter() => scope.Push(new Dictionary<IdentifierNode, IType>());
    public void Exit() => scope.Pop();

}

internal static class StandardLibrary
{

    public static readonly Type unit = new("unit");
    public static readonly Type any = new("any")
    {
        Methods = new()
        {

        }
    };

    public static readonly IdentifierNode current = new() { Value = "this" };
    public static readonly IdentifierNode constructor = new() { Value = "new" };
    public static readonly IdentifierNode method = new() { Value = "return" };


    public static readonly Map<TypeNode, Type> classes = new()
    {
        [unit.Name] = unit,
        [any.Name ] = any
    };

}

public class TypeChecker : ASTVisitor<object?>
{

    public Environment Environment { get; } = new();
    private readonly ErrorLogger error;


    public TypeChecker(ErrorLogger error) => this.error = error;


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
        foreach ((ClassNode node, Type type) in nodes)
        {
            if (node.Parent is not TypeNode name) continue;
            error.Catch(node.Location, () =>
            {
                Type parent = Environment.Classes[name];
                type.Parent = parent;
            });
        }
        CheckCycles(program.Classes, from data in nodes select data.type);

        // Initialize and check members
        foreach ((ClassNode node, Type type) in nodes) foreach (IMemberNode member in node.Members)
            InitializeMember(type, (dynamic) member);
        foreach ((ClassNode node, Type type) in nodes)
        {
            Environment.Enter();
            // TODO: Add "this" to scope

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
                error.Report($"Cyclic inheritance found at {parent.Name.Value}", node.Location);
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
            null => StandardLibrary.unit // Defaults to unit if type is unspecified
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
        type.Methods.Add(StandardLibrary.constructor, new Method
        {
            Access = node.Access,
            Parameters = InitializeParameters(node.Parameters),
            Return = StandardLibrary.unit
        }));

    private IType[] InitializeParameters(ParameterNode[] parameters)
    {
        TypeVisitor visitor = new TypeVisitor(Environment);
        IEnumerable<IType> types =
            from parameter in parameters
            select visitor.Visit(parameter.Type);

        return types.ToArray();
    }

    public override object? Visit(FieldNode node)
    {
        return null;
    }

    public override object? Visit(MethodNode node)
    {
        return null;
    }

    public override object? Visit(ConstructorNode node)
    {
        return null;
    }

}

internal class TypeVisitor : ASTVisitor<IType>
{

    private readonly Environment environment;

    public TypeVisitor(Environment environment) => this.environment = environment;


    public override IType Visit(TypeNode node) => environment.Classes[node];
    public override IType Visit(OptionNode node) => new Option { Type = Visit(node.Type) };

}
