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


    public override object? Visit(ProgramNode node)
    {
        return null;
    }

}
