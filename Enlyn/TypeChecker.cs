namespace Enlyn;

public class Map<K, V> : Dictionary<K, V> where K : notnull
{

    public Map(Dictionary<K, V> map) : base(map) { }
    public Map() { }


    public new IResult<V> this[K key]
    {
        get
        {
            if (ContainsKey(key)) return Result.Ok<V>(base[key]);
            return Result.Error<V>($"{key} not found");
        }
    }

    public new IResult<unit> Add(K key, V value)
    {
        if (TryAdd(key, value)) return Result.Unit;
        else return Result.Error($"Redefinition of {key}");
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
        Methods = new(new Dictionary<IIdentifierNode, Method>
        {

        })
    };

    public static readonly IdentifierNode current = new() { Value = "this" };
    public static readonly IdentifierNode constructor = new() { Value = "new" };
    public static readonly IdentifierNode method = new() { Value = "return" };


    public static readonly Map<TypeNode, Type> classes = new(new Dictionary<TypeNode, Type>
    {
        [unit.Name] = unit,
        [any.Name ] = any
    });

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
            if (Environment.Classes.Add(node.Identifier, type).Unwrap(error, node.Location) is not null)
                nodes.Add(new ClassData(node, type));
        }

        foreach ((ClassNode node, Type type) in nodes)
        {
            if (node.Parent is not TypeNode name) continue;

            Type? parent = Environment.Classes[name].Unwrap(error, node.Location);
            if (parent is not null) type.Parent = parent;
        }

        CheckCycles(program.Classes, from data in nodes select data.type);
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

    private void InitializeMember(Type type, FieldNode node)
    {
        IType? field = new TypeVisitor(Environment).Visit(node.Type).Unwrap(error, node.Location);
        if (field is null) return;

        type.Fields.Add(node.Identifier, new Field
        {
            Access = node.Access,
            Type = field
        });
    }

    private void InitializeMember(Type type, MethodNode node)
    {
        // Get types
        IType? returnType = node.Return switch  
        {
            ITypeNode t => new TypeVisitor(Environment).Visit(t).Unwrap(error, node.Location),
            null => StandardLibrary.unit // Defaults to unit if type is unspecified
        };
        IType[]? parameters = InitializeParameters(node.Parameters);

        if (returnType is null || parameters is null) return;
        if (CheckOperator(node) is null) return;

        type.Methods.Add(node.Identifier, new Method
        {
            Access = node.Access,
            Parameters = parameters,
            Return = returnType
        });
    }

    private object? CheckOperator(MethodNode node)
    {
        int l = node.Parameters.Length;
        IResult<unit> result = node.Identifier switch
        {
            BinaryIdentifierNode => l == 1 ? Result.Unit :
                Result.Error("Binary operation must be defined with 1 parameter"),
            UnaryIdentifierNode => l == 0 ? Result.Unit :
                Result.Error("Unary operation cannot be defined with parameter"),
            _ => Result.Unit
        };

        return result.Unwrap(error, node.Location);
    }

    private void InitializeMember(Type type, ConstructorNode node)
    {
        IType[]? parameters = InitializeParameters(node.Parameters);
        if (parameters is null) return;

        type.Methods.Add(StandardLibrary.constructor, new Method
        {
            Access = node.Access,
            Parameters = parameters,
            Return = StandardLibrary.unit
        });
    }

    private IType[]? InitializeParameters(ParameterNode[] parameters)
    {
        TypeVisitor visitor = new TypeVisitor(Environment);
        IEnumerable<IType?> types =
            from parameter in parameters
            select visitor.Visit(parameter.Type).Unwrap(error, parameter.Location);

        // Fail the entire list if any parameter fails
        foreach (IType? parameter in types) if (parameter is null) return null;
        return types.ToArray()!;
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

internal class TypeVisitor : ASTVisitor<IResult<IType>>
{

    private readonly Environment environment;

    public TypeVisitor(Environment environment) => this.environment = environment;


    public override IResult<IType> Visit(TypeNode node) => environment.Classes[node].Cast<IType>();
    public override IResult<IType> Visit(OptionNode node)
    {
        return Visit(node.Type) switch
        {
            Ok<IType>(IType type) => Result.Ok<IType>(new Option { Type = type }),
            Error<IType> error => error,

            _ => throw new Exception()
        };
    }

}
