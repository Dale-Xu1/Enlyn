namespace Test;
using Enlyn;

[TestClass]
public class VirtualMachineTest
{

    private static VirtualMachine InitInterpreter(Chunk chunk, Instance[] constants) => new(new Executable()
    {
        Constructs = new Construct[]
        {
            new()
            {
                Parent = 0,
                Fields = 0,
                Chunks = new() { ["main"] = chunk }
            }
        }, Main = chunk,
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
                new CONST(0),
                new ADD(),
                new PRINT(),
                new NULL(),
                new RETURN()
            },
            Locals = 0, Arguments = 0
        };
        Instance[] constants = new Instance[]
        {
            new ValueInstance<int> { Value = 2 }
        };

        VirtualMachine interpreter = InitInterpreter(main, constants);
        interpreter.Run();
    }

}
