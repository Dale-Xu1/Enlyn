namespace Enlyn;

public class Compiler : ASTVisitor<object?>
{

    private Environment Environment { get; }
    private readonly ErrorLogger error;


    public Compiler(Environment environment, ErrorLogger error)
    {
        Environment = environment;
        this.error = error;
    }


    public override object? Visit(ProgramNode program)
    {
        foreach (ClassNode node in program.Classes)
        {
            foreach (IMemberNode member in node.Members) Visit(member);
        }
        return null;
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
