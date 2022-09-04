namespace Enlyn;
using Antlr4.Runtime;

public class EnlynLexerFilter : EnlynLexer
{

    private readonly Stack<int> stack = new();
    private int Indent
    {
        get
        {
            IToken token = Lookahead();
            if (token.Type == NEWLINE)
            {
                Next();
                return Indent;
            }

            return token.Column;
        }
    }

    public EnlynLexerFilter(ICharStream stream) : base(stream) => stack.Push(Indent);


    private IToken? temp = null;
    private IToken Next()
    {
        IToken? token = temp;
        if (token is null) return base.NextToken();

        temp = null;
        return token;
    }

    private IToken Lookahead() => temp = Next();


    public override IToken NextToken()
    {
        IToken token = Next();
        switch (token.Type)
        {
            case NEWLINE: return Newline(token);

            case LBRACE: stack.Push(Indent); break;
            case RBRACE: stack.Pop(); break;
        }

        return token;
    }

    private IToken Newline(IToken newline)
    {
        int indent = Indent;
        switch (Lookahead().Type)
        {
            case LBRACE or RBRACE:
            case THEN or ELSE or DO:
            case Eof: return NextToken();
        }
        
        if (indent == stack.Peek()) return newline;
        return NextToken();
    }

}
