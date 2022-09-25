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
            "class A : any { }",
            "class A { }");

        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Redefinition of A", error.Errors[0].Message);
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
        Assert.AreEqual("Cyclic inheritance found at A", error.Errors[0].Message);
    }

    [TestMethod]
    public void TestField()
    {
        string input = string.Join(Environment.NewLine,
            "class A",
            "{",
            "    public x : any = 5",
            "}");
            
        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));
        Enlyn.Environment environment = checker.Environment;

        Type type = environment.Classes[new TypeNode { Value = "A" }];
        Field field = type.Fields[new IdentifierNode { Value = "x" }];

        Type any = environment.Classes[new TypeNode { Value = "any" }];
        Assert.AreEqual(Access.Public, field.Access);
        Assert.AreEqual(any, field.Type);
    }

    [TestMethod]
    public void TestMethod()
    {
        string input = string.Join(Environment.NewLine,
            "class B { }",
            "class A",
            "{",
            "    private main(a : A, b : B) { }",
            "}");

        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));
        Enlyn.Environment environment = checker.Environment;

        Type a = environment.Classes[new TypeNode { Value = "A" }];
        Type b = environment.Classes[new TypeNode { Value = "B" }];
        Type unit = environment.Classes[new TypeNode { Value = "unit" }];

        Method main = a.Methods[new IdentifierNode { Value = "main" }];

        Assert.AreEqual(Access.Private, main.Access);
        Assert.AreEqual(unit, main.Return);

        Assert.AreEqual(2, main.Parameters.Length);
        Assert.AreEqual(a, main.Parameters[0]);
        Assert.AreEqual(b, main.Parameters[1]);
    }

    [TestMethod]
    public void TestOperator()
    {
        string input = string.Join(Environment.NewLine,
            "class A",
            "{",
            "    protected binary +(a : A, b : any) -> A { }",
            "}");

        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));
        Enlyn.Environment environment = checker.Environment;

        Assert.AreEqual(2, error.Errors.Count);
        Assert.AreEqual("Binary operation must be defined with 1 parameter", error.Errors[0].Message);
    }

    [TestMethod]
    public void TestTypeTest()
    {
        string input = string.Join(Environment.NewLine,
            "class A",
            "{",
            "    public x : any = 5",
            "    private y : number? = 5",
            "    private z : number? = true",
            "}");

        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Type boolean is not compatible with number", error.Errors[0].Message);
    }

}
