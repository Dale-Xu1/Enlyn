namespace Test;
using Antlr4.Runtime;
using Enlyn;

using Environment = System.Environment;

[TestClass]
public class ParserTest
{

    private static EnlynLexerFilter InitLexer(string input)
    {
        AntlrInputStream stream = new(input);
        EnlynLexerFilter lexer = new(stream);

        return lexer;
    }

    private static IList<IToken> InitTokens(string input)
    {
        EnlynLexerFilter lexer = InitLexer(input);
        CommonTokenStream tokens = new(lexer);

        tokens.Fill();
        return tokens.GetTokens();
    }
    
    private static EnlynParser InitParser(string input)
    {
        EnlynLexerFilter lexer = InitLexer(input);
        CommonTokenStream tokens = new(lexer);

        EnlynParser parser = new(tokens);
        return parser;
    }


    [TestMethod]
    public void TestNewline()
    {
        string input = string.Join(Environment.NewLine,
            "1",
            "2",
            "  3",
            "4");

        int newlines = 0;
        foreach (IToken token in InitTokens(input))
        {
            if (token.Type == EnlynLexer.NEWLINE) newlines++;
        }

        Assert.AreEqual(2, newlines);
    }

    [TestMethod]
    public void TestIgnoreNewline()
    {
        string input = string.Join(Environment.NewLine,
            "1 <",
            "{",
            "  true",
            "");

        foreach (IToken token in InitTokens(input))
        {
            string name = EnlynLexer.DefaultVocabulary.GetSymbolicName(token.Type);
            Console.WriteLine($"{name,-15} '{token.Text}'");

            Assert.AreNotEqual(EnlynLexer.NEWLINE, token.Type);
        }
    }
    
    [TestMethod]
    public void TestExpr()
    {
        string input = "false * (\"Hello\\n\" + .2e-3)";
        
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        BinaryNode tree = (BinaryNode) visitor.VisitExpr(parser.expr());
        BinaryNode right = (BinaryNode) tree.Right;

        Assert.AreEqual(Operation.Mul, tree.Operation);
        Assert.AreEqual(Operation.Add, right.Operation);

        Assert.AreEqual(false, ((BooleanNode) tree.Left).Value);
        Assert.AreEqual("Hello\n", ((StringNode) right.Left).Value);
        Assert.AreEqual(0.2e-3, ((NumberNode) right.Right).Value);
    }

    [TestMethod]
    public void TestCall()
    {
        string input = "null(1, 2, 3, 4, 5)";
        
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        CallNode tree = (CallNode) visitor.VisitExpr(parser.expr());
        
        Assert.IsInstanceOfType(tree.Target, typeof(NullNode));
        Assert.AreEqual(5, tree.Arguments.Length);
    }
    
    [TestMethod]
    public void TestEmptyCall()
    {
        string input = "f()";
        
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        CallNode tree = (CallNode) visitor.VisitExpr(parser.expr());
        Assert.AreEqual(0, tree.Arguments.Length);
    }

    [TestMethod]
    public void TestProgram()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : object",
            "{",
            "    public x : int = 1 + 2",
            "}");
            
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        ProgramNode program = visitor.VisitProgram(parser.program());
        ClassNode tree = program.Classes[0];

        Assert.AreEqual(1, program.Classes.Length);
        Assert.AreEqual("Main", tree.Identifier.Value);
        Assert.AreEqual("object", tree.Parent?.Value);

        Assert.AreEqual(1, tree.Members.Length);
        FieldNode field = (FieldNode) tree.Members[0];

        Assert.AreEqual("x", field.Identifier.Value);
        Assert.AreEqual("int", ((TypeNode) field.Type).Value);
    }

    [TestMethod]
    public void TestMethod()
    {
        string input = "private main(a : float) -> int { 5 }";

        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        MethodNode method = visitor.VisitMethod((EnlynParser.MethodContext) parser.member());

        Assert.AreEqual(false, method.Override);
        Assert.AreEqual(1, method.Parameters.Length);
        ParameterNode parameter = method.Parameters[0];

        Assert.AreEqual("a", parameter.Identifier.Value);
        Assert.AreEqual("float", ((TypeNode) parameter.Type).Value);

        Assert.AreEqual("int", ((TypeNode) method.Return!).Value);
    }

    [TestMethod]
    public void TestConstructor()
    {
        string input = "private new() : base(true) = 1";

        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        ConstructorNode constructor = visitor.VisitConstructor((EnlynParser.ConstructorContext) parser.member());
        
        Assert.AreEqual(0, constructor.Parameters.Length);
        Assert.AreEqual(1, constructor.Arguments.Length);

        Assert.AreEqual(true, ((BooleanNode) constructor.Arguments[0]).Value);
    }

    [TestMethod]
    public void TestOperation()
    {
        string input = "public override binary +() -> int { true is boolean }";

        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        MethodNode method = visitor.VisitMethod((EnlynParser.MethodContext) parser.member());

        Assert.AreEqual(true, method.Override);
        Assert.AreEqual(0, method.Parameters.Length);
        
        Assert.AreEqual(Operation.Add, ((BinaryIdentifierNode) method.Identifier).Operation);
    }

    [TestMethod]
    public void TestIfPrecedence()
    {
        string input = string.Join(Environment.NewLine,
            "if x then if y then z",
            "else w");
            
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        IfNode branch = visitor.VisitIf((EnlynParser.IfContext) parser.stmt());
        IfNode then = (IfNode) branch.Then;

        Assert.AreEqual("x", ((IdentifierNode) branch.Condition).Value);
        Assert.AreEqual("y", ((IdentifierNode) then.Condition).Value);
        Assert.AreEqual("z", ((IdentifierNode) ((ExpressionStatementNode) then.Then).Expression).Value);
        Assert.AreEqual("w", ((IdentifierNode) ((ExpressionStatementNode) then.Else!).Expression).Value);

        Assert.AreEqual(null, branch.Else);
    }

    [TestMethod]
    public void TestIfBlock()
    {
        string input = string.Join(Environment.NewLine,
            "if x { if y then z }",
            "else w");
            
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new("Test");

        IfNode branch = visitor.VisitIf((EnlynParser.IfContext) parser.stmt());
        IfNode then = (IfNode) ((BlockNode) branch.Then).Statements[0];
        
        Assert.AreEqual("x", ((IdentifierNode) branch.Condition).Value);
        Assert.AreEqual("y", ((IdentifierNode) then.Condition).Value);
        Assert.AreEqual("z", ((IdentifierNode) ((ExpressionStatementNode) then.Then).Expression).Value);
        Assert.AreEqual("w", ((IdentifierNode) ((ExpressionStatementNode) branch.Else!).Expression).Value);
        
        Assert.AreEqual(null, then.Else);
    }

}
