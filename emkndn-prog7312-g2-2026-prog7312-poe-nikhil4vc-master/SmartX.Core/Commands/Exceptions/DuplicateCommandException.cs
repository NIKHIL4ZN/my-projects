namespace SmartX.Core.Commands.Exceptions;

public class DuplicateCommandException : Exception
{
    public DuplicateCommandException()
        : base(
            "An identical command is already queued " +
            "for this sensor.")
    {
    }
}