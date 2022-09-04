namespace Test;
using Antlr4.Runtime;
using Enlyn;

[TestClass]
public class ParserTest
{

    private static IList<IToken> InitTokens(string input)
    {
        AntlrInputStream inputStream = new(input);
        EnlynLexerFilter lexer = new(inputStream);

        CommonTokenStream tokens = new(lexer);
        tokens.Fill();

        return tokens.GetTokens();
    }

    private static EnlynParser Init(string input)
    {
        AntlrInputStream stream = new(input);
        EnlynLexerFilter lexer = new(stream);

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
            "1 +",
            "{",
            "  2",
            "");

        int newlines = 0;
        foreach (IToken token in InitTokens(input))
        {
            string name = EnlynLexer.DefaultVocabulary.GetSymbolicName(token.Type);
            Console.WriteLine($"{name,-15} '{token.Text}'");

            if (token.Type == EnlynLexer.NEWLINE) newlines++;
        }

        Assert.AreEqual(0, newlines);
    }
    
    [TestMethod]
    public void TestExpr()
    {
        string input = string.Join(Environment.NewLine,
            "1 + \"hi\"");
            
        EnlynParser.ExprContext context = Init(input).expr();
        EnlynVisitor visitor = new();

        visitor.Visit(context);
    }

}
