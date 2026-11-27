using SmartX.Core.Commands.DTOs;
using SmartX.Core.Commands.Enums;

namespace SmartX.Core.Commands.Interfaces;

public interface ICommandStreamService
{
    CommandResponse QueueCommand(
        QueueCommandRequest request,
        string sensorName);

    CommandResponse? ProcessNextCommand();

    CommandResponse? UndoLastCompletedCommand();

    IReadOnlyList<CommandResponse> GetCommands();

    IReadOnlyList<CommandResponse> GetTimeline();

    CommandResponse GetCommand(Guid commandId);

    CommandQueueSummary GetSummary();
}