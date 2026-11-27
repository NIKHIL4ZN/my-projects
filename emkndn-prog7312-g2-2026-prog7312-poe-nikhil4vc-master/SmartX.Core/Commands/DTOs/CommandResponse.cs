using SmartX.Core.Commands.Enums;

namespace SmartX.Core.Commands.DTOs;

public class CommandResponse
{
    public Guid Id { get; init; }

    public Guid SensorId { get; init; }

    public string SensorName { get; init; } =
        string.Empty;

    public DeviceCommandType Type { get; init; }

    public CommandPriority Priority { get; init; }

    public CommandStatus Status { get; init; }

    public string? Payload { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ProcessedAtUtc { get; init; }

    public string? ResultMessage { get; init; }
}