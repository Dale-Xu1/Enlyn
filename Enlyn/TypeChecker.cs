namespace Enlyn;

public interface IType { }

public class Option : IType { public IType Type { get; init; } = null!; }
public class Type : IType
{

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


public static class StandardLibrary
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

public class TypeChecker : ASTVisitor<object?>
{

    private readonly Dictionary<TypeNode, Type> classes = new(StandardLibrary.classes);
    private readonly Stack<Dictionary<IdentifierNode, IType>> scope = new();

    private readonly ErrorLogger error;


    public TypeChecker(ErrorLogger error) => this.error = error;


    public override object? Visit(ProgramNode node)
    {
        foreach (ClassNode n in node.Classes)
        {
            Type type = new();
            TypeNode identifier = n.Identifier;

            if (!classes.TryAdd(n.Identifier, type))
                error.Report($"Redefinition of class {identifier.Value}", n.Location);
        }

        CheckCycles();
        foreach (ClassNode n in node.Classes) InitializeMembers(n);
        foreach (ClassNode n in node.Classes) Visit(n);

        return null;
    }

    private void CheckCycles()
    {

    }

    private void InitializeMembers(ClassNode node)
    {

    }

    public override object? Visit(ClassNode node)
    {
        return null;
    }

}
