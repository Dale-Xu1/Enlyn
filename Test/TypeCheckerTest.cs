#define TEST
namespace Test;
using Antlr4.Runtime;
using Enlyn;

[TestClass]
public class TypeCheckerTest
{

    private static ProgramNode ParseProgram(string input)
    {
        AntlrInputStream stream = new(input);
        EnlynLexerFilter lexer = new(stream);

        CommonTokenStream tokens = new(lexer);
        EnlynParser parser = new(tokens);

        ParseTreeVisitor visitor = new("Test");
        ProgramNode tree = visitor.VisitProgram(parser.program());
        
        return tree;
    }


    [TestMethod]
    public void Test()
    {
        string input = string.Join(Environment.NewLine,
            "class A { }",
            "class A { }");

        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));
        Assert.AreEqual(1, error.Errors.Count);
    }
    
}
