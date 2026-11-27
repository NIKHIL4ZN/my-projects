using SmartX.Core.Commands.Enums;

namespace SmartX.Core.Commands.Models;

public class DeviceCommand
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SensorId { get; init; }

    public string SensorName { get; init; } =
        string.Empty;

    public DeviceCommandType Type { get; init; }

    public CommandPriority Priority { get; init; }

    public CommandStatus Status { get; set; } =
        CommandStatus.Queued;

    public string? Payload { get; init; }

    public DateTime CreatedAtUtc { get; init; } =
        DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }

    public string? ResultMessage { get; set; }

    public string DuplicateKey =>
        $"{SensorId}:{Type}:{Payload?.Trim().ToUpperInvariant()}";
}