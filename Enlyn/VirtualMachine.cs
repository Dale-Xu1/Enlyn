namespace Enlyn;

public interface IOpcode { }

public record struct CONST(int index) : IOpcode;
public record struct NULL : IOpcode;

public record struct LOAD(int index) : IOpcode;
public record struct STORE(int index) : IOpcode;
public record struct POP : IOpcode;

public record struct NEW(int length) : IOpcode;
public record struct GETF(int index) : IOpcode;
public record struct SETF(int index) : IOpcode;

public record struct JUMP(int ip) : IOpcode;
public record struct CALL(int index) : IOpcode;
public record struct RETURN : IOpcode;

public class Instance
{

    public Instance?[] Fields { get; }

    public Instance(int length) => Fields = new Instance?[length];

}

public class Executable
{

    public Chunk[] Chunks { get; init; } = null!;
    public Chunk Main { get; init; } = null!;

    public Instance[] Constants { get; init; } = null!;

}

public class Chunk
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
        Instance constant = executable.Constants[opcode.index];
        frame.Stack.Push(constant);
    }

    private void Handle(NULL _) => frame.Stack.Push(null);


    private void Handle(RETURN opcode)
    {
        Instance? value = frame.Stack.Pop();
        frame = frame.Parent;

        // Place popped value onto new frame if it not the last one
        if (frame is not null) frame.Stack.Push(value);
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
