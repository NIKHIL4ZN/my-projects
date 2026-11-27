using SmartX.Core.Commands.DTOs;
using SmartX.Core.Commands.Enums;
using SmartX.Core.Commands.Exceptions;
using SmartX.Core.Commands.Interfaces;
using SmartX.Core.Commands.Models;

namespace SmartX.Infrastructure.Commands;

public class CommandStreamService
    : ICommandStreamService
{
    private readonly object _syncRoot = new();

    // Standard FIFO queue for normal commands.
    private readonly Queue<DeviceCommand>
        _normalCommandQueue = new();

    // .NET removes the lowest numeric priority first.
    // Negative values make Critical execute before High.
    private readonly PriorityQueue<
        DeviceCommand,
        int> _priorityCommandQueue = new();

    // LIFO history used by the undo operation.
    private readonly Stack<DeviceCommand>
        _completedCommandHistory = new();

    // O(1) lookup by command ID.
    private readonly Dictionary<
        Guid,
        DeviceCommand> _commandsById = new();

    // Commands arranged chronologically.
    private readonly SortedDictionary<
        DateTime,
        List<Guid>> _timeline = new();

    // Prevent duplicate active commands.
    private readonly HashSet<string>
        _activeCommandKeys =
            new(StringComparer.OrdinalIgnoreCase);

    // Track every unique sensor receiving commands.
    private readonly HashSet<Guid>
        _commandedSensorIds = new();

    public CommandResponse QueueCommand(
        QueueCommandRequest request,
        string sensorName)
    {
        var command = new DeviceCommand
        {
            SensorId = request.SensorId,
            SensorName = sensorName.Trim(),
            Type = request.Type,
            Priority = request.Priority,
            Payload = request.Payload?.Trim()
        };

        lock (_syncRoot)
        {
            if (!_activeCommandKeys.Add(
                    command.DuplicateKey))
            {
                throw new DuplicateCommandException();
            }

            _commandsById.Add(
                command.Id,
                command);

            _commandedSensorIds.Add(
                command.SensorId);

            AddToTimeline(command);

            if (command.Priority ==
                CommandPriority.Normal)
            {
                _normalCommandQueue.Enqueue(command);
            }
            else
            {
                _priorityCommandQueue.Enqueue(
                    command,
                    -(int)command.Priority);
            }

            return ToResponse(command);
        }
    }

    public CommandResponse? ProcessNextCommand()
    {
        lock (_syncRoot)
        {
            var command = GetNextQueuedCommand();

            if (command is null)
            {
                return null;
            }

            command.Status =
                CommandStatus.Processing;

            command.Status =
                CommandStatus.Completed;

            command.ProcessedAtUtc =
                DateTime.UtcNow;

            command.ResultMessage =
                "Command processed successfully.";

            _activeCommandKeys.Remove(
                command.DuplicateKey);

            _completedCommandHistory.Push(command);

            return ToResponse(command);
        }
    }

    public CommandResponse?
        UndoLastCompletedCommand()
    {
        lock (_syncRoot)
        {
            if (!_completedCommandHistory.TryPop(
                    out var command))
            {
                return null;
            }

            command.Status = CommandStatus.Undone;

            command.ResultMessage =
                "The last completed command was undone.";

            return ToResponse(command);
        }
    }

    public IReadOnlyList<CommandResponse>
        GetCommands()
    {
        lock (_syncRoot)
        {
            return _commandsById.Values
                .OrderByDescending(
                    command =>
                        command.CreatedAtUtc)
                .Select(ToResponse)
                .ToList();
        }
    }

    public IReadOnlyList<CommandResponse>
        GetTimeline()
    {
        lock (_syncRoot)
        {
            var results =
                new List<CommandResponse>(
                    _commandsById.Count);

            foreach (var timelineEntry
                     in _timeline)
            {
                foreach (var commandId
                         in timelineEntry.Value)
                {
                    results.Add(
                        ToResponse(
                            _commandsById[commandId]));
                }
            }

            return results;
        }
    }

    public CommandResponse GetCommand(
        Guid commandId)
    {
        lock (_syncRoot)
        {
            if (!_commandsById.TryGetValue(
                    commandId,
                    out var command))
            {
                throw new CommandNotFoundException(
                    commandId);
            }

            return ToResponse(command);
        }
    }

    public CommandQueueSummary GetSummary()
    {
        lock (_syncRoot)
        {
            var completedCount =
                _commandsById.Values.Count(
                    command =>
                        command.Status ==
                        CommandStatus.Completed);

            return new CommandQueueSummary
            {
                CriticalCommands =
                    _priorityCommandQueue.Count,

                NormalCommands =
                    _normalCommandQueue.Count,

                CompletedCommands =
                    completedCount,

                UniqueDevices =
                    _commandedSensorIds.Count,

                TotalTrackedCommands =
                    _commandsById.Count
            };
        }
    }

    private DeviceCommand?
        GetNextQueuedCommand()
    {
        if (_priorityCommandQueue.TryDequeue(
                out var priorityCommand,
                out _))
        {
            return priorityCommand;
        }

        if (_normalCommandQueue.TryDequeue(
                out var normalCommand))
        {
            return normalCommand;
        }

        return null;
    }

    private void AddToTimeline(
        DeviceCommand command)
    {
        var timestamp = command.CreatedAtUtc;

        if (!_timeline.TryGetValue(
                timestamp,
                out var commandIds))
        {
            commandIds = new List<Guid>();

            _timeline.Add(
                timestamp,
                commandIds);
        }

        commandIds.Add(command.Id);
    }

    private static CommandResponse ToResponse(
        DeviceCommand command)
    {
        return new CommandResponse
        {
            Id = command.Id,
            SensorId = command.SensorId,
            SensorName = command.SensorName,
            Type = command.Type,
            Priority = command.Priority,
            Status = command.Status,
            Payload = command.Payload,
            CreatedAtUtc = command.CreatedAtUtc,
            ProcessedAtUtc =
                command.ProcessedAtUtc,
            ResultMessage =
                command.ResultMessage
        };
    }
}

// Code attributes

/*
Title: Dictionary in C#
Author(s): GeeksforGeeks
Date: 2026
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/c-sharp/dictionary-in-c-sharp/
*/

/*
Title: Implement Stack using Queues
Author(s): GeeksforGeeks
Date: 2026
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/dsa/implement-stack-using-queue/
*/

/*
Title: HashSet in C#
Author(s): GeeksforGeeks
Date: 2026
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/c-sharp/hashset-in-c-sharp/
*/

/*
Title: Queues: From FIFO to Distributed Systems
Author(s): ashutoshkmrsingh
Date: n.d.
Version: Not applicable
Type: Medium article
Availability: https://ashutoshkmrsingh.medium.com/queues-from-fifo-to-distributed-systems-9c148ef8ecec
*/
