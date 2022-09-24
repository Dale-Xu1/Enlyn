#define TEST
namespace Test;
using Antlr4.Runtime;
using Enlyn;

using Environment = System.Environment;

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
    public void TestClassRedefinition()
    {
        string input = string.Join(Environment.NewLine,
            "class A { }",
            "class A { }");

        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Redefinition of class A", error.Errors[0].Message);
    }

    [TestMethod]
    public void TestCyclicInheritance()
    {
        string input = string.Join(Environment.NewLine,
            "class A : D { }",
            "class B : A { }",
            "class C : B { }",
            "class D : C { }");

        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Cyclic inheritance found at class A", error.Errors[0].Message);
    }
    
}
