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
        string input = string.Join(Environment.NewLine,
            "false + (\"Hello\\n\")");
            
        EnlynParser.ProgramContext context = InitParser(input).program();
        ParseTreeVisitor visitor = new();

        INode tree = visitor.Visit(context);
    }

}
