namespace Test;
using Antlr4.Runtime;
using Enlyn;

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
        ParseTreeVisitor visitor = new();

        BinaryNode tree = (BinaryNode) visitor.VisitExpr(parser.expr());
        BinaryNode right = (BinaryNode) tree.Right;
        
        Assert.AreEqual(Operation.Mul, tree.Operation);
        Assert.AreEqual(Operation.Add, right.Operation);

        BooleanNode a = (BooleanNode) tree.Left;
        StringNode b = (StringNode) right.Left;
        NumberNode c = (NumberNode) right.Right;

        Assert.AreEqual(false, a.Value);
        Assert.AreEqual("Hello\n", b.Value);
        Assert.AreEqual(0.2e-3, c.Value);
    }

    [TestMethod]
    public void TestCall()
    {
        string input = "null(1, 2, 3, 4, 5)";
        
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new();

        CallNode tree = (CallNode) visitor.VisitExpr(parser.expr());
        
        Assert.IsInstanceOfType(tree.Target, typeof(NullNode));
        Assert.AreEqual(5, tree.Arguments.Length);
    }
    
    [TestMethod]
    public void TestEmptyCall()
    {
        string input = "f()";
        
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new();

        CallNode tree = (CallNode) visitor.VisitExpr(parser.expr());
        Assert.AreEqual(0, tree.Arguments.Length);
    }

    [TestMethod]
    public void TestProgram()
    {
        string input = string.Join(Environment.NewLine,
            "class Main : object",
            "{",
            // "    public x : int",
            "}");
            
        EnlynParser parser = InitParser(input);
        ParseTreeVisitor visitor = new();

        ProgramNode program = visitor.VisitProgram(parser.program());
        ClassNode tree = program.Classes[0];
        
        Assert.AreEqual(1, program.Classes.Length);
        Assert.AreEqual("Main", tree.Identifier.Value);
        Assert.AreEqual("object", tree.Parent?.Value);
    }

}
