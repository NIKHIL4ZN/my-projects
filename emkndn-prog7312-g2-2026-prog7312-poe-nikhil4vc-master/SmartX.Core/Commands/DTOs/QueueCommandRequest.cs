using System.ComponentModel.DataAnnotations;
using SmartX.Core.Commands.Enums;

namespace SmartX.Core.Commands.DTOs;

public class QueueCommandRequest
{
    public Guid SensorId { get; set; }

    [EnumDataType(typeof(DeviceCommandType))]
    public DeviceCommandType Type { get; set; }

    [EnumDataType(typeof(CommandPriority))]
    public CommandPriority Priority { get; set; } =
        CommandPriority.Normal;

    [StringLength(500)]
    public string? Payload { get; set; }
}
