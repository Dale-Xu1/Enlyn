namespace Test;
using Enlyn;

[TestClass]
public class VirtualMachineTest
{

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

        VirtualMachine interpreter = new(new Executable()
        {
            Chunks = new[] { main }, Main = main,
            Constants = new Instance[0]
        });
        interpreter.Run();
    }

}
