namespace Test;
using Enlyn;

[TestClass]
public class VirtualMachineTest
{

    private static VirtualMachine InitInterpreter(Chunk chunk, Instance[] constants) => new(new Executable()
    {
        Constructs = Executable.standard.Concat(new Construct[]
        {
            new()
            {
                Parent = 0,
                Chunks = new() { [new IdentifierNode { Value = "main" }] = chunk }
            }
        }).ToArray(), Main = 5,
        Constants = constants
    });


    [TestMethod]
    public void Test()
    {
        Chunk main = new()
        {
            Instructions = new IOpcode[]
            {
                new ONE(),
                new CAST(1),
                new CONST(0),
                new ADD(),
                new PRINT(),
                new NULL(),
                new RETURN()
            },
            Locals = 0
        };
        Instance[] constants =
        {
            new NumberInstance { Value = 2 }
        };

        var x = new NumberInstance { Value = 2 };
        var y = new NumberInstance { Value = 2 };

        Assert.AreEqual(true, x.Equals(y));

        VirtualMachine interpreter = InitInterpreter(main, constants);
        interpreter.Run();
    }

    [TestMethod]
    public void TestCall()
    {
        Chunk main = new()
        {
            Instructions = new IOpcode[]
            {
                new CONST(0),
                new CONST(1),
                new CALL(2, new BinaryIdentifierNode { Operation = Operation.Add }),
                new COPY(),
                new GETF(0),
                new PRINT(),
                new PRINT(),
                new NULL(),
                new RETURN()
            },
            Locals = 0
        };
        Instance[] constants =
        {
            new StringInstance("Hello "),
            new StringInstance("world"),
        };

        VirtualMachine interpreter = InitInterpreter(main, constants);
        interpreter.Run();
    }

}
