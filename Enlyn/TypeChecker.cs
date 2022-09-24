namespace Enlyn;

public interface IType { }

public class Option : IType { public IType Type { get; init; } = null!; }
public class Type : IType
{

    public TypeNode Name { get; init; }
    public Type? Parent { get; set; }

    public Dictionary<IdentifierNode, Field> Fields { get; init; } = new();
    public Dictionary<IdentifierNode, Method> Methods { get; init; } = new();

}

public class Field
{

}

public class Method
{

}

internal static class StandardLibrary
{

    public static readonly Type unit = new();
    public static readonly Type any = new()
    {
        Methods = new()
        {

        }
    };


    public static readonly Dictionary<TypeNode, Type> classes = new()
    {
        [new() { Value = "unit" }] = unit,
        [new() { Value = "any"  }] = any
    };

}

internal class Environment
{
    
    private readonly Dictionary<TypeNode, Type> classes = new(StandardLibrary.classes);
    private readonly Stack<Dictionary<IdentifierNode, IType>> scope = new();


    public IResult<Type> AddClass(TypeNode name, Type type)
    {
        if (classes.TryAdd(name, type)) return new Result.Ok<Type>(type);
        else return new Result.Error<Type>($"Redefinition of class {name.Value}");
    }

    public IResult<Type> LookupType(TypeNode name)
    {
        if (classes.ContainsKey(name)) return new Result.Ok<Type>(classes[name]);
        return new Result.Error<Type>($"Class {name.Value} not found");
    }

}

public class TypeChecker : ASTVisitor<object?>
{

    private readonly Environment environment = new();
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
            if (environment.AddClass(node.Identifier, type).Handle(error, node.Location) is not null)
                nodes.Add(new ClassData(node, type));
        }

        foreach ((ClassNode node, Type type) in nodes)
        {
            if (node.Parent is not TypeNode name) continue;
            if (environment.LookupType(name).Handle(error, node.Location) is Type parent)
                type.Parent = parent;
        }
        CheckCycles(program.Classes, from data in nodes select data.type);

        foreach ((ClassNode node, Type type) in nodes) InitializeMembers(type, node.Members);
        foreach ((ClassNode node, Type type) in nodes)
        {
            // TODO: Enter scope
            foreach (IMemberNode member in node.Members) { }
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
                error.Report($"Cyclic inheritance found at class {parent.Name.Value}", node.Location);
                return;
            }
        }
    }

    private void InitializeMembers(Type type, IMemberNode[] members)
    {
        foreach (IMemberNode member in members)
        {
            
        }
    }

}
