namespace Enlyn;
using Antlr4.Runtime;

public class ErrorLogger
{

    public class Error
    {
        public string Message { get; init; } = null!;
        public Location Location { get; init; }
    }

    public List<Error> Errors { get; } = new();


    public void Report(string message, Location location) => Errors.Add(new Error
    {
        Message = message,
        Location = location,
    });

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

public interface IResult<T>
{

    public T Ignore()
    {
        Result.Ok<T> result = (Result.Ok<T>) this;
        return result.Value;
    }

    public T? Handle(ErrorLogger error, Location location)
    {
        switch (this)
        {
            case Result.Ok<T>(T value): return value;
            case Result.Error<T>(string message):
                error.Report(message, location);
                break;
        }

        return default;
    }

}

public static class Result
{

    public struct Ok<T> : IResult<T>
    {

        public T Value { get; }
        public Ok(T value) => Value = value;

        public void Deconstruct(out T value) => value = Value;

    }

    public struct Error<T> : IResult<T>
    {

        public string Message { get; }
        public Error(string message) => Message = message;

        public void Deconstruct(out string message) => message = Message;

    }

}
