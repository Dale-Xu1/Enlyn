namespace Test;
using Antlr4.Runtime;
using Enlyn;

using Environment = System.Environment;

[TestClass]
public class CompilerTest
{

    private static void Compile(string input)
    {
        ErrorLogger error = new();
        TypeChecker checker = new(error);

        AntlrInputStream stream = new(input);
        EnlynLexerFilter lexer = new(stream);

        CommonTokenStream tokens = new(lexer);
        EnlynParser parser = new(tokens);

        ParseTreeVisitor visitor = new("Test");
        ProgramNode tree = visitor.VisitProgram(parser.program());

        checker.Visit(tree);
        Assert.AreEqual(0, error.Errors.Count);

        Compiler compiler = new(checker.Environment, error);
        compiler.Visit(tree);
    }


    [TestMethod]
    public void Test()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : IO",
            "{",
            "    public main()",
            "    {",
            "        this.out(\"Hello world\")",
            "    }",
            "}");
        Compile(input);
    }

}
