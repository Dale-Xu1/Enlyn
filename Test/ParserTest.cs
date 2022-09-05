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
        
        EnlynParser.ExprContext context = InitParser(input).expr();
        ParseTreeVisitor visitor = new();

        BinaryNode tree = (BinaryNode) visitor.VisitExpr(context);
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
        string input = "f(1, 2, 3, 4, 5)";
            
        EnlynParser.ExprContext context = InitParser(input).expr();
        ParseTreeVisitor visitor = new();

        CallNode tree = (CallNode) visitor.VisitExpr(context);
        IdentifierNode target = (IdentifierNode) tree.Target;

        Assert.AreEqual("f", target.Value);
        Assert.AreEqual(5, tree.Arguments.Length);
    }

}
