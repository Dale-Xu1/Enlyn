namespace Enlyn;

public interface IOpcode { }

public record struct CONST(int index) : IOpcode;
public record struct ZERO : IOpcode;
public record struct ONE : IOpcode;
public record struct TRUE : IOpcode;
public record struct FALSE : IOpcode;
public record struct NULL : IOpcode;

public record struct LOAD(int index) : IOpcode;
public record struct STORE(int index) : IOpcode;
public record struct POP : IOpcode;

public record struct ADD : IOpcode;
public record struct SUB : IOpcode;
public record struct MUL : IOpcode;
public record struct DIV : IOpcode;
public record struct MOD : IOpcode;
public record struct NEG : IOpcode;

public record struct LT : IOpcode;
public record struct GT : IOpcode;
public record struct LE : IOpcode;
public record struct GE : IOpcode;

public record struct AND : IOpcode;
public record struct OR  : IOpcode;
public record struct NOT : IOpcode;

public record struct CAST(int type) : IOpcode;
public record struct INST(int type) : IOpcode;

public record struct NEW(int type) : IOpcode;
public record struct GETF(int index) : IOpcode;
public record struct SETF(int index) : IOpcode;

public record struct JUMP(int ip) : IOpcode;
public record struct CALL(string name) : IOpcode;
public record struct RETURN : IOpcode;

public record struct PRINT : IOpcode; // TODO: Replace with standard library

public class Construct
{

    public int Parent { get; init; }
    public int Fields { get; init; }

    public Dictionary<string, IChunk> Chunks { get; init; } = null!;

}

public class Instance
{

    public int Type { get; init; }
    public Instance?[] Fields { get; }

    public Instance(int length) => Fields = new Instance?[length];

}

public class ValueInstance<T> : Instance
{

    public T Value { get; init; } = default!;

    public ValueInstance() : base(0) { }

}

public class Executable
{

    public Construct[] Constructs { get; init; } = null!;
    public Chunk Main { get; init; } = null!;

    public Instance[] Constants { get; init; } = null!;

}

public interface IChunk { }
public class Chunk : IChunk
{

    public IOpcode[] Instructions { get; init; } = null!;

    public int Locals { get; init; }
    public int Arguments { get; init; }

}

public class VirtualMachine
{

    private readonly Executable executable;
    private Frame frame;


    public VirtualMachine(Executable executable)
    {
        this.executable = executable;
        frame = new Frame(executable.Main);
    }

    public void Run()
    {
        // Keep reading and dispatching opcodes until final frame is exited
        while (frame is not null) Handle((dynamic) frame.Next);
    }


    private void Handle(CONST opcode)
    {
        Instance value = executable.Constants[opcode.index];
        frame.Stack.Push(value);
    }

    private void Handle(ZERO _) => frame.Stack.Push(new ValueInstance<int> { Value = 0 });
    private void Handle(ONE _) => frame.Stack.Push(new ValueInstance<int> { Value = 0 });
    
    private void Handle(TRUE _) => frame.Stack.Push(new ValueInstance<bool> { Value = true });
    private void Handle(FALSE _) => frame.Stack.Push(new ValueInstance<bool> { Value = false });

    private void Handle(NULL _) => frame.Stack.Push(null);


    private void Handle(LOAD opcode)
    {
        Instance? value = frame.Variables[opcode.index];
        frame.Stack.Push(value);
    }

    private void Handle(STORE opcode)
    {
        Instance? value = frame.Stack.Pop();
        frame.Variables[opcode.index] = value;
    }

    private void Handle(POP _) => frame.Stack.Pop();


    private void HandleBinary<T, U>(Func<T, T, U> handler)
    {
        var b = frame.Stack.Pop() as ValueInstance<T>;
        var a = frame.Stack.Pop() as ValueInstance<T>;

        if (a is null || b is null) throw new Exception("Invalid binary operation");
        frame.Stack.Push(new ValueInstance<U> { Value = handler(a.Value, b.Value) });
    }

    private void HandleUnary<T, U>(Func<T, U> handler)
    {
        var a = frame.Stack.Pop() as ValueInstance<T>;

        if (a is null) throw new Exception("Invalid unary operation");
        frame.Stack.Push(new ValueInstance<U> { Value = handler(a.Value) });
    }

    private void Handle(ADD _) => HandleBinary((int a, int b) => a + b);
    private void Handle(SUB _) => HandleBinary((int a, int b) => a - b);
    private void Handle(MUL _) => HandleBinary((int a, int b) => a * b);
    private void Handle(DIV _) => HandleBinary((int a, int b) => a / b);
    private void Handle(MOD _) => HandleBinary((int a, int b) => a % b);
    private void Handle(NEG _) => HandleUnary((int a) => -a);

    private void Handle(LT _) => HandleBinary((int a, int b) => a < b);
    private void Handle(GT _) => HandleBinary((int a, int b) => a > b);
    private void Handle(LE _) => HandleBinary((int a, int b) => a <= b);
    private void Handle(GE _) => HandleBinary((int a, int b) => a >= b);

    private void Handle(AND _) => HandleBinary((bool a, bool b) => a && b);
    private void Handle(OR  _) => HandleBinary((bool a, bool b) => a || b);
    private void Handle(NOT _) => HandleUnary((bool a) => !a);


    private void Handle(CAST opcode) { }
    private void Handle(INST opcode) { }


    private void Handle(NEW opcode) { }
    private void Handle(GETF opcode) { }
    private void Handle(SETF opcode) { }


    private void Handle(JUMP opcode) => frame.ip = opcode.ip;
    private void Handle(CALL opcode) { }

    private void Handle(RETURN _)
    {
        Instance? value = frame.Stack.Pop();
        frame = frame.Parent;

        // Place popped value onto new frame if it not the last one
        if (frame is not null) frame.Stack.Push(value);
    }


    private void Handle(PRINT _)
    {
        Instance? value = frame.Stack.Pop();
        Console.WriteLine(value switch
        {
            ValueInstance<int> n => n.Value,
            ValueInstance<bool> n => n.Value,
            null => "null",
            _ => value
        });
    }

}

internal class Frame
{

    public Frame Parent { get; }

    public Stack<Instance?> Stack { get; } = new();
    public Instance?[] Variables { get; }

    private readonly Chunk chunk;
    public int ip = 0;


    public Frame(Chunk chunk, Frame parent = null!)
    {
        Parent = parent;
        Variables = new Instance[chunk.Locals];

        this.chunk = chunk;
    }

    public IOpcode Next => chunk.Instructions[ip++];

}
