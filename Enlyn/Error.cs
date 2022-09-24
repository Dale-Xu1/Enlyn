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

    public T Unwrap() => this switch
    {
        Ok<T>(T value) => value,
        _ => throw new Exception("Cannot unwrap error result")
    };

    public T? Unwrap(ErrorLogger error, Location location)
    {
        T? Report(string message) { error.Report(message, location); return default; }
        return this switch
        {
            Ok<T>(T value) => value,
            Error<T>(string message) => Report(message),

            _ => throw new Exception()
        };
    }

    // TODO: Bind

    public IResult<U> Cast<U>() => this switch
    {
        Ok<T>(T value) => Result.Ok<U>((dynamic) value),
        Error<T>(string message) => Result.Error<U>(message),
        
        _ => throw new Exception()
    };

}

public struct Ok<T> : IResult<T>
{

    public T Value { get; init; }
    public void Deconstruct(out T value) => value = Value;

}

public struct Error<T> : IResult<T>
{

    public string Message { get; init; }
    public void Deconstruct(out string message) => message = Message;

}

public class unit { }
public static class Result
{

    internal static Ok<T> Ok<T>(T value) => new Ok<T> { Value = value };
    internal static Ok<unit> Unit { get; } = Ok<unit>(new());

    internal static Error<T> Error<T>(string message) => new Error<T> { Message = message };
    internal static Error<unit> Error(string message) => Error<unit>(message);

}
