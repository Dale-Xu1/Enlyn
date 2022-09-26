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

    private static ErrorLogger CheckProgram(string input)
    {
        ErrorLogger error = new();
        TypeChecker checker = new(error);

        checker.Visit(ParseProgram(input));
        return error;
    }


    [TestMethod]
    public void TestClassRedefinition()
    {
        string input = string.Join(Environment.NewLine,
            "class A : any { }",
            "class A { }");
        ErrorLogger error = CheckProgram(input);

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
        ErrorLogger error = CheckProgram(input);

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
            "    private y : number? = null",
            "    private w : number? = 5",
            "    protected y : number? = true",
            "    private z : string = null",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(3, error.Errors.Count);
        Assert.AreEqual("Redefinition of y", error.Errors[0].Message);
        Assert.AreEqual("Type boolean is not compatible with number", error.Errors[1].Message);
        Assert.AreEqual("Type string is not an option", error.Errors[2].Message);
    }

    [TestMethod]
    public void TestParameter()
    {
        string input = string.Join(Environment.NewLine,
            "class A",
            "{",
            "    public f(a : number, a : string) { }",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Redefinition of a", error.Errors[0].Message);
    }

    [TestMethod]
    public void TestOverride()
    {
        string input = string.Join(Environment.NewLine,
            "class B",
            "{",
            "    public f(a : number) -> any = return 1",
            "    public g(x : any) -> any = return 1",
            "}",
            "class A : B",
            "{",
            "    public override f(a : number) -> boolean = return true",
            "    public override g(x : string) -> boolean = return false",
            "    public override h(i : unit) -> unit = return",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(2, error.Errors.Count);
        Assert.AreEqual("Type any is not compatible with string", error.Errors[0].Message);
        Assert.AreEqual("No method h found to override", error.Errors[1].Message);
    }

    [TestMethod]
    public void TestControlSuccess()
    {
        string input = string.Join(Environment.NewLine,
            "class A",
            "{",
            "    public f() -> number",
            "    {",
            "        if true then return 1",
            "        else return 2",
            "    }",
            "}");

        ErrorLogger error = CheckProgram(input);
        Assert.AreEqual(0, error.Errors.Count);
    }

    [TestMethod]
    public void TestControlFail()
    {
        string input = string.Join(Environment.NewLine,
            "class A",
            "{",
            "    public f() -> number",
            "    {",
            "        this.f()",
            "        while true do return 1",
            "    }",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Method does not always return a value", error.Errors[0].Message);
    }

    [TestMethod]
    public void TestAccess()
    {
        string input = string.Join(Environment.NewLine,
            "class B",
            "{",
            "    private a : string",
            "    protected b : number",
            "}",
            "class A : B",
            "{",
            "    public x : string = this.a",
            "    public y : number = this.b",
            "    protected new() : base(5) { }",
            "    private z : unit",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(2, error.Errors.Count);
        Assert.AreEqual("Member a is private", error.Errors[0].Message);
        Assert.AreEqual("Invalid number of arguments", error.Errors[1].Message);
    }

    [TestMethod]
    public void TestCall()
    {
        string input = string.Join(Environment.NewLine,
            "class A : unit",
            "{",
            "    private z : unit",
            "    public f(a : number) = this.g(a)",
            "    public g(x : any) = this.f(x)",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Type any is not compatible with number", error.Errors[0].Message);
    }

    [TestMethod]
    public void TestAssertAndCast()
    {
        string input = string.Join(Environment.NewLine,
            "class A : unit",
            "{",
            "    private a : unit = null!",
            "    private b : number?",
            "    private c : number = this.b!",
            "    private d : number = this.b as number",
            "    private e : any = null as any",
            "    private f : boolean = this.e is string",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(2, error.Errors.Count);
        Assert.AreEqual("Invalid assertion target", error.Errors[0].Message);
        Assert.AreEqual("Type any is not compatible with null", error.Errors[1].Message);
    }

    [TestMethod]
    public void TestOperators()
    {
        string input = string.Join(Environment.NewLine,
            "class A : unit",
            "{",
            "    private a : number = 1 + 2",
            "    private b : boolean = true & \"hi\"",
            "    private c : boolean = \"\" == 5",
            "    private x : any? = null",
            "    private d : boolean = this.x == null",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(2, error.Errors.Count);
        Assert.AreEqual("Type string is not compatible with boolean", error.Errors[0].Message);
        Assert.AreEqual("Member == not found", error.Errors[1].Message);
    }

    [TestMethod]
    public void TestReturn()
    {
        string input = string.Join(Environment.NewLine,
            "class A : unit",
            "{",
            "    public f() = return",
            "    public g() -> number",
            "    {",
            "        return",
            "        return true",
            "    }",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(2, error.Errors.Count);
        Assert.AreEqual("Method cannot return unit", error.Errors[0].Message);
        Assert.AreEqual("Type boolean is not compatible with number", error.Errors[1].Message);
    }

    [TestMethod]
    public void TestLetAndAssign()
    {
        string input = string.Join(Environment.NewLine,
            "class Main",
            "{",
            "    public f()",
            "    {",
            "        let x = null",
            "        let y : boolean = true",
            "        let z : boolean? = y",
            "        x = 2",
            "        y = 5",
            "    }",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(1, error.Errors.Count);
        Assert.AreEqual("Type number is not compatible with boolean", error.Errors[0].Message);
    }

    [TestMethod]
    public void TestIfAndWhile()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : any",
            "{",
            "    public new() : base()",
            "    {",
            "        if true & false | true",
            "        {",
            "            let a = true",
            "            while a do return",
            "        }",
            "        else if false { }",
            "    }",
            "}");

        ErrorLogger error = CheckProgram(input);
        Assert.AreEqual(0, error.Errors.Count);
    }

    [TestMethod]
    public void TestConditionFail()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : any",
            "{",
            "    public new() : base()",
            "    {",
            "        if 5 then return",
            "        else if false { }",
            "        while null do 5",
            "    }",
            "}");
        ErrorLogger error = CheckProgram(input);

        Assert.AreEqual(2, error.Errors.Count);
        Assert.AreEqual("Type number is not compatible with boolean", error.Errors[0].Message);
        Assert.AreEqual("Type boolean is not an option", error.Errors[1].Message);
    }

    [TestMethod]
    public void TestStandardLibrary()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : any",
            "{",
            "    public new() : base()",
            "    {",
            "        let io = new IO()",
            "        io.out(io.in() + \"Hello\")",
            "    }",
            "}");

        ErrorLogger error = CheckProgram(input);
        Assert.AreEqual(0, error.Errors.Count);
    }

}
