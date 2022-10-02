namespace Enlyn;

public interface IOpcode { }

public record struct CONST(int index) : IOpcode;
public record struct COPY : IOpcode;
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
public record struct JUMPF(int ip) : IOpcode;

public record struct CALL(string name, int args) : IOpcode;
public record struct RETURN : IOpcode;

public record struct PRINT : IOpcode; // TODO: Replace with standard library

public class Construct
{

    public int? Parent { get; init; }
    public int Fields { get; init; }

    public Dictionary<string, IChunk> Chunks { get; init; } = null!;

}

public class Instance
{

    public int Type { get; init; }
    public Instance?[] Fields { get; }

    public Instance(int length) => Fields = new Instance?[length];

}

#pragma warning disable CS0659
public abstract class ValueInstance<T> : Instance
{

    public T Value { get; init; } = default!;
    protected ValueInstance() : base(0) { }

    public override bool Equals(object? obj) => obj is ValueInstance<T> value && Value!.Equals(value.Value); 

}

public class NumberInstance : ValueInstance<int> { public NumberInstance() => Type = Executable.Number; }
public class BooleanInstance : ValueInstance<bool> { public BooleanInstance() => Type = Executable.Boolean; }
public class StringInstance : Instance
{

    public string Value { get; }

    public StringInstance(string value) : base(1)
    {
        Type = Executable.String;
        Value = value;

        Fields[0] = new NumberInstance { Value = value.Length };
    }

}

public class Executable
{

    internal static int Any { get; } = 0;
    internal static int Number { get; } = 1;
    internal static int String { get; } = 2;
    internal static int Boolean { get; } = 3;

    public static readonly Construct[] standard =
    {
        new()
        {
            Chunks = new()
            {
                ["new"] = new NativeChunk { Function = _ => null },
                ["binary =="] = new NativeChunk
                {
                    Function = args => new BooleanInstance { Value = args[0]!.Equals(args[1]) }
                },
                ["binary !="] = new NativeChunk
                {
                    Function = args => new BooleanInstance { Value = !args[0]!.Equals(args[1]) }
                }
            },
        },
        new() { Parent = Any },
        new()
        {
            Parent = Any,
            Chunks = new()
            {
                ["binary +"] = new NativeChunk
                {
                    Function = args =>
                    {
                        var a = (StringInstance) args[0]!;
                        var b = (StringInstance) args[1]!;

                        return new StringInstance(a.Value + b.Value);
                    }
                }
            }
        },
        new() { Parent = Any },
        new()
        {
            Parent = Any,
            Chunks = new()
            {
                ["out"] = new NativeChunk
                {
                    Function = args =>
                    {
                        // TODO: IO class
                        return null;
                    }
                },
                ["in"] = new NativeChunk
                {
                    Function = _ =>
                    {
                        return null;
                    }
                }
            }
        }
    };


    public Construct[] Constructs { get; init; } = null!;
    public Chunk Main { get; init; } = null!;

    public Instance[] Constants { get; init; } = null!;

}

public interface IChunk { }
public class NativeChunk : IChunk { public Func<Instance?[], Instance?> Function { get; init; } = null!; }

public class Chunk : IChunk
{

    public IOpcode[] Instructions { get; init; } = null!;
    public int Locals { get; init; }

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

    private void Handle(COPY _) => frame.Stack.Push(frame.Stack.Peek());

    private void Handle(ZERO _) => frame.Stack.Push(new NumberInstance { Value = 0 });
    private void Handle(ONE _) => frame.Stack.Push(new NumberInstance { Value = 0 });
    
    private void Handle(TRUE _) => frame.Stack.Push(new BooleanInstance { Value = true });
    private void Handle(FALSE _) => frame.Stack.Push(new BooleanInstance { Value = false });

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


    private void HandleArithmetic(Func<int, int, int> handler)
    {
        var b = (NumberInstance) frame.Stack.Pop()!;
        var a = (NumberInstance) frame.Stack.Pop()!;

        frame.Stack.Push(new NumberInstance { Value = handler(a.Value, b.Value) });
    }
    
    private void HandleComparison(Func<int, int, bool> handler)
    {
        var b = (NumberInstance) frame.Stack.Pop()!;
        var a = (NumberInstance) frame.Stack.Pop()!;

        frame.Stack.Push(new BooleanInstance { Value = handler(a.Value, b.Value) });
    }

    private void HandleLogical(Func<bool, bool, bool> handler)
    {
        var b = (BooleanInstance) frame.Stack.Pop()!;
        var a = (BooleanInstance) frame.Stack.Pop()!;

        frame.Stack.Push(new BooleanInstance { Value = handler(a.Value, b.Value) });
    }

    private void Handle(ADD _) => HandleArithmetic((a, b) => a + b);
    private void Handle(SUB _) => HandleArithmetic((a, b) => a - b);
    private void Handle(MUL _) => HandleArithmetic((a, b) => a * b);
    private void Handle(DIV _) => HandleArithmetic((a, b) => a / b);
    private void Handle(MOD _) => HandleArithmetic((a, b) => a % b);
    private void Handle(NEG _)
    {
        var a = (NumberInstance) frame.Stack.Pop()!;
        frame.Stack.Push(new NumberInstance { Value = -a.Value });
    }

    private void Handle(LT _) => HandleComparison((a, b) => a < b);
    private void Handle(GT _) => HandleComparison((a, b) => a > b);
    private void Handle(LE _) => HandleComparison((a, b) => a <= b);
    private void Handle(GE _) => HandleComparison((a, b) => a >= b);

    private void Handle(AND _) => HandleLogical((a, b) => a && b);
    private void Handle(OR  _) => HandleLogical((a, b) => a || b);
    private void Handle(NOT _)
    {
        var a = (BooleanInstance) frame.Stack.Pop()!;
        frame.Stack.Push(new BooleanInstance { Value = !a.Value });
    }


    private bool TestType(int expected, int target)
    {
        if (expected == target) return true;
        Construct construct = executable.Constructs[target];

        if (construct.Parent is int parent) TestType(expected, parent);
        return false;
    }

    private void Handle(CAST opcode)
    {
        Instance? value = frame.Stack.Peek();
        if (value is null || TestType(opcode.type, value.Type)) return;

        throw new Exception("Invalid cast");
    }

    private void Handle(INST opcode)
    {
        Instance? value = frame.Stack.Pop();
        bool result = value is null ? false : TestType(opcode.type, value.Type);

        frame.Stack.Push(new BooleanInstance { Value = result });
    }


    private void Handle(NEW opcode)
    {
        Construct construct = executable.Constructs[opcode.type];
        Instance instance = new(construct.Fields) { Type = opcode.type };

        frame.Stack.Push(instance);
    }

    private void Handle(GETF opcode)
    {
        Instance instance = frame.Stack.Pop()!;
        frame.Stack.Push(instance.Fields[opcode.index]);
    }

    private void Handle(SETF opcode)
    {
        Instance? value = frame.Stack.Pop();
        Instance instance = frame.Stack.Pop()!;

        instance.Fields[opcode.index] = value;
    }


    private void Handle(JUMP opcode) => frame.ip = opcode.ip;
    private void Handle(JUMPF opcode)
    {
        var condition = (BooleanInstance) frame.Stack.Pop()!;
        if (!condition.Value) frame.ip = opcode.ip;
    }

    private void Handle(CALL opcode)
    {
        Instance?[] arguments = new Instance?[opcode.args];
        for (int i = opcode.args - 1; i >= 0; i--) arguments[i] = frame.Stack.Pop();

        Instance instance = arguments[0]!;
        IChunk chunk = GetChunk(instance.Type, opcode.name);

        if (chunk is NativeChunk native)
        {
            Instance? value = native.Function(arguments);
            frame.Stack.Push(value);

            return;
        }

        // Transfer arguments to variable array
        frame = new Frame((Chunk) chunk, frame);
        for (int i = 0; i < arguments.Length; i++) frame.Variables[i] = arguments[i];
    }

    private IChunk GetChunk(int type, string name)
    {
        Construct construct = executable.Constructs[type];
        int? parent = construct.Parent;

        if (construct.Chunks.ContainsKey(name) || parent is null) return construct.Chunks[name];
        return GetChunk((int) parent, name);
    }

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
            NumberInstance n => n.Value,
            BooleanInstance b => b.Value,
            StringInstance s => s.Value,

            null => "null",
            _ => "instance"
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
