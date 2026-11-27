namespace SmartX.Core.Commands.Exceptions;

public class CommandNotFoundException : Exception
{
    public CommandNotFoundException(Guid commandId)
        : base(
            $"Command '{commandId}' could not be found.")
    {
    }
}