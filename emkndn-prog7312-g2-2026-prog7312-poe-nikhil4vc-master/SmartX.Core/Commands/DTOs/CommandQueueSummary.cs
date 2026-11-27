namespace SmartX.Core.Commands.DTOs;

public class CommandQueueSummary
{
    public int CriticalCommands { get; init; }

    public int NormalCommands { get; init; }

    public int CompletedCommands { get; init; }

    public int UniqueDevices { get; init; }

    public int TotalTrackedCommands { get; init; }
}