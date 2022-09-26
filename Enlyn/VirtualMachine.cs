namespace Enlyn;

public interface IOpcode { }

public record struct CONST(int index) : IOpcode;
public record struct COPY : IOpcode;
public record struct NULL : IOpcode;

public record struct LOAD(int index) : IOpcode;
public record struct STORE(int index) : IOpcode;
public record struct POP : IOpcode;

public record struct NEW(int index) : IOpcode;
public record struct INV(int index) : IOpcode;
public record struct GETF(int index) : IOpcode;
public record struct SETF(int index) : IOpcode;

public record struct JUMP(int index) : IOpcode;
public record struct RETURN : IOpcode;

public partial class VirtualMachine
{



}

internal class Frame
{

}
