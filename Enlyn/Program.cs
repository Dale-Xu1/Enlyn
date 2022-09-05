using Antlr4.Runtime;
using Enlyn;

string file = args[0];
using StreamReader reader = new(file);

AntlrInputStream stream = new(reader);
EnlynLexerFilter lexer = new(stream);

CommonTokenStream tokens = new(lexer);
EnlynParser parser = new(tokens);

parser.RemoveErrorListeners();
parser.AddErrorListener(new ErrorListener(file));

EnlynParser.ExprContext context = parser.expr();
ParseTreeVisitor visitor = new();

IExpressionNode tree = visitor.VisitExpr(context);

class ErrorListener : BaseErrorListener
{

    private readonly string file;

    public ErrorListener(string file) => this.file = file;


    public override void SyntaxError(
        TextWriter output, IRecognizer recognizer,
        IToken token, int line, int col, string msg,
        RecognitionException e)
    {
        using StreamReader reader = new(file);
        for (int i = 1; i < line; i++) reader.ReadLine();

        string name = Path.GetFileName(file);
        Console.Error.WriteLine($"{name}:{line}:{col} - Syntax Error: Unexpected '{token.Text}'");
        Console.Error.WriteLine($"{line} | {reader.ReadLine()}\n");
    }

}
