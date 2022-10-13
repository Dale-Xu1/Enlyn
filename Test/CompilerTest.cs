namespace Test;
using Antlr4.Runtime;
using Enlyn;

using Environment = System.Environment;

[TestClass]
public class CompilerTest
{

    private static Executable Compile(string input)
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
        return compiler.Compile(tree);
    }


    [TestMethod]
    public void TestIOCall()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : IO",
            "{",
            "    private new() : base()",
            "    {",
            "        let x = \"Hi\"",
            "        new A().out(x)",
            "    }",
            "}",
            "class A",
            "{",
            "    public new() = return",
            "    public out(a : string) = new IO().out(a)",
            "}");

        Executable executable = Compile(input);
        VirtualMachine interpreter = new(executable);

        using StringWriter writer = new();
        Console.SetOut(writer);

        interpreter.Run();
        Assert.AreEqual(string.Join(Environment.NewLine,
            "Hi", ""), writer.ToString());
    }

    [TestMethod]
    public void TestScopeStack()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : IO",
            "{",
            "    public new()",
            "    {",
            "        let a = 1",
            "        let b = 2",
            "        let c = 3",
            "        let d = 4",
            "        let e = 5",
            "        let f = 6",
            "    }",
            "}");

        Executable executable = Compile(input);
        Chunk chunk = (Chunk) executable.Constructs[executable.Main].Chunks[Enlyn.Environment.constructor];

        Assert.AreEqual(7, chunk.Locals);
    }

    [TestMethod]
    public void TestBaseCall()
    {
        string input = string.Join(Environment.NewLine,
            "class Main",
            "{",
            "    private new()",
            "    {",
            "        new A(1)",
            "    }",
            "}",
            "class A : B",
            "{",
            "    public new(x : number) : base(x, x) = new IO().out(x)",
            "}",
            "class B",
            "{",
            "    public new(x : number, y : number)",
            "    {",
            "        new IO().out(x)",
            "        new IO().out(y)",
            "    }",
            "}");

        Executable executable = Compile(input);
        VirtualMachine interpreter = new(executable);

        using StringWriter writer = new();
        Console.SetOut(writer);

        interpreter.Run();
        Assert.AreEqual(string.Join(Environment.NewLine,
            "1", "1", "1", ""), writer.ToString());
    }

    [TestMethod]
    public void TestFieldIndex()
    {
        string input = string.Join(Environment.NewLine,
            "class Main { public new() = return }",
            "class A : B",
            "{",
            "    public new() = return",
            "    public a : number",
            "    public b : number",
            "    public c : number",
            "}",
            "class B",
            "{",
            "    public new() = return",
            "    public a : number",
            "    public b : number",
            "    public c : number",
            "}");

        Executable executable = Compile(input);
        Assert.AreEqual(6, executable.Constructs[6].Fields);
    }

    [TestMethod]
    public void TestFieldInitializer()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : A",
            "{",
            "    public new()",
            "    {",
            "        this.a = \"12345\"",
            "        new IO().out(this.a.length)",
            "    }",
            "}",
            "class A",
            "{",
            "    public a : string = \"123\"",
            "}");

        Executable executable = Compile(input);
        VirtualMachine interpreter = new(executable);

        using StringWriter writer = new();
        Console.SetOut(writer);

        interpreter.Run();
        Assert.AreEqual(string.Join(Environment.NewLine,
            "5", ""), writer.ToString());
    }

    [TestMethod]
    public void TestCastAndInstance()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : IO",
            "{",
            "    public new()",
            "    {",
            "        let a : number? = null",
            "        this.out(a is number)",
            "        this.out(a is number?)",
            "        let b : any = 5",
            "        this.out(b is boolean)",
            "        this.out(null is string???)",
            "        this.out(a is null)",
            "        this.out(b is any)",
            "    }",
            "}");

        Executable executable = Compile(input);
        VirtualMachine interpreter = new(executable);

        using StringWriter writer = new();
        Console.SetOut(writer);

        interpreter.Run();
        Assert.AreEqual(string.Join(Environment.NewLine,
            "false", "true", "false", "true", "true", "true", ""), writer.ToString());
    }

}
