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
                Fields = 0,
                Chunks = new() { ["main"] = chunk }
            }
        }).ToArray(), Main = chunk,
        Constants = new Instance[0]
    });


    [TestMethod]
    public void Test()
    {
        Chunk main = new()
        {
            Instructions = new IOpcode[]
            {
                new NULL(),
                new RETURN()
            },
            Locals = 0, Arguments = 0
        };

        VirtualMachine interpreter = InitInterpreter(main, new Instance[0]);
        interpreter.Run();
    }

}
