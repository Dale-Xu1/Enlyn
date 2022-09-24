namespace Enlyn;
using Antlr4.Runtime;

internal class EnlynError : Exception { public EnlynError(string message) : base(message) { } }
public class ErrorLogger
{

    public struct Error
    {
        public string Message { get; init; }
        public Location Location { get; init; }
    }

    public List<Error> Errors { get; } = new();


    public void Report(string message, Location location) => Errors.Add(new Error
    {
        Message = message,
        Location = location,
    });

    public void Catch(Location location, Action handler)
    {
        try { handler(); }
        catch (EnlynError error) { Report(error.Message, location); }
    }


    public void LogErrors() { foreach (Error error in Errors) LogError(error); }
    private void LogError(Error error)
    {
        Location location = error.Location;

        using StreamReader reader = new(location.File);
        for (int i = 1; i < location.Line; i++) reader.ReadLine();

        string name = Path.GetFileName(location.File);
        Console.Error.WriteLine($"{name}:{location.Line}:{location.Col} - {error.Message}");
        Console.Error.WriteLine($"{location.Line} | {reader.ReadLine()}\n");
    }

}

internal class ErrorListener : BaseErrorListener
{

    private readonly ErrorLogger error;
    private readonly string file;


    public ErrorListener(ErrorLogger error, string file)
    {
        this.error = error;
        this.file = file;
    }


    public override void SyntaxError(
        TextWriter output, IRecognizer recognizer,
        IToken token, int line, int col, string msg,
        RecognitionException e)
    {
        string message = $"Syntax Error: Unexpected '{token.Text}'";
        Location location = new() { File = file, Line = line, Col = col };

        error.Report(message, location);
    }

}
