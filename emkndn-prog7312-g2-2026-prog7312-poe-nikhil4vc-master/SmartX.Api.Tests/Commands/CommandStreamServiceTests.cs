using SmartX.Core.Commands.DTOs;
using SmartX.Core.Commands.Enums;
using SmartX.Core.Commands.Exceptions;
using SmartX.Infrastructure.Commands;

namespace SmartX.Api.Tests.Commands;

public class CommandStreamServiceTests
{
    [Fact]
    public void ProcessNextCommand_PrioritisesCriticalCommand()
    {
        var service = new CommandStreamService();

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal),
            "Normal Sensor");

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.Restart,
                CommandPriority.Critical),
            "Critical Sensor");

        var processed =
            service.ProcessNextCommand();

        Assert.NotNull(processed);
        Assert.Equal(
            CommandPriority.Critical,
            processed.Priority);
        Assert.Equal(
            DeviceCommandType.Restart,
            processed.Type);
        Assert.Equal(
            CommandStatus.Completed,
            processed.Status);
        Assert.NotNull(processed.ProcessedAtUtc);
    }

    [Fact]
    public void ProcessNextCommand_ProcessesNormalCommandsInFifoOrder()
    {
        var service = new CommandStreamService();

        var first = service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal,
                "First command"),
            "First Sensor");

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.Calibrate,
                CommandPriority.Normal,
                "Second command"),
            "Second Sensor");

        var processed =
            service.ProcessNextCommand();

        Assert.NotNull(processed);
        Assert.Equal(first.Id, processed.Id);
        Assert.Equal(
            "First command",
            processed.Payload);
    }

    [Fact]
    public void ProcessNextCommand_ProcessesHighBeforeNormal()
    {
        var service = new CommandStreamService();

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal),
            "Normal Sensor");

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.Calibrate,
                CommandPriority.High),
            "High Priority Sensor");

        var processed =
            service.ProcessNextCommand();

        Assert.NotNull(processed);
        Assert.Equal(
            CommandPriority.High,
            processed.Priority);
        Assert.Equal(
            DeviceCommandType.Calibrate,
            processed.Type);
    }

    [Fact]
    public void ProcessNextCommand_ReturnsNullWhenQueueIsEmpty()
    {
        var service = new CommandStreamService();

        var processed =
            service.ProcessNextCommand();

        Assert.Null(processed);
    }

    [Fact]
    public void UndoLastCompletedCommand_UsesLifoOrder()
    {
        var service = new CommandStreamService();

        var first = service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal,
                "First completed command"),
            "First Sensor");

        var second = service.QueueCommand(
            CreateRequest(
                DeviceCommandType.Calibrate,
                CommandPriority.Normal,
                "Second completed command"),
            "Second Sensor");

        service.ProcessNextCommand();
        service.ProcessNextCommand();

        var undone =
            service.UndoLastCompletedCommand();

        Assert.NotNull(undone);
        Assert.Equal(second.Id, undone.Id);
        Assert.Equal(
            CommandStatus.Undone,
            undone.Status);

        var earlierCommand =
            service.GetCommand(first.Id);

        Assert.Equal(
            CommandStatus.Completed,
            earlierCommand.Status);
    }

    [Fact]
    public void UndoLastCompletedCommand_ReturnsNullWhenHistoryIsEmpty()
    {
        var service = new CommandStreamService();

        var result =
            service.UndoLastCompletedCommand();

        Assert.Null(result);
    }

    [Fact]
    public void QueueCommand_RejectsDuplicateActiveCommand()
    {
        var service = new CommandStreamService();
        var sensorId = Guid.NewGuid();

        var request = CreateRequest(
            DeviceCommandType.Restart,
            CommandPriority.Critical,
            "Restart duplicate device",
            sensorId);

        service.QueueCommand(
            request,
            "Duplicate Sensor");

        Assert.Throws<DuplicateCommandException>(
            () => service.QueueCommand(
                request,
                "Duplicate Sensor"));
    }

    [Fact]
    public void ProcessedCommand_CanBeQueuedAgain()
    {
        var service = new CommandStreamService();
        var sensorId = Guid.NewGuid();

        var request = CreateRequest(
            DeviceCommandType.Restart,
            CommandPriority.Critical,
            "Restart device",
            sensorId);

        service.QueueCommand(
            request,
            "Restart Sensor");

        service.ProcessNextCommand();

        var secondCommand =
            service.QueueCommand(
                request,
                "Restart Sensor");

        Assert.Equal(
            CommandStatus.Queued,
            secondCommand.Status);
    }

    [Fact]
    public void GetCommand_ReturnsCommandFromDictionary()
    {
        var service = new CommandStreamService();

        var queued = service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal),
            "Lookup Sensor");

        var result =
            service.GetCommand(queued.Id);

        Assert.Equal(queued.Id, result.Id);
        Assert.Equal(
            "Lookup Sensor",
            result.SensorName);
    }

    [Fact]
    public void GetSummary_CountsQueuesAndUniqueDevices()
    {
        var service = new CommandStreamService();
        var firstSensorId = Guid.NewGuid();
        var secondSensorId = Guid.NewGuid();

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.Restart,
                CommandPriority.Critical,
                "Restart first sensor",
                firstSensorId),
            "First Sensor");

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal,
                "Inspect first sensor",
                firstSensorId),
            "First Sensor");

        service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal,
                "Inspect second sensor",
                secondSensorId),
            "Second Sensor");

        var summary = service.GetSummary();

        Assert.Equal(
            1,
            summary.CriticalCommands);
        Assert.Equal(
            2,
            summary.NormalCommands);
        Assert.Equal(
            0,
            summary.CompletedCommands);
        Assert.Equal(
            2,
            summary.UniqueDevices);
        Assert.Equal(
            3,
            summary.TotalTrackedCommands);
    }

    [Fact]
    public void GetTimeline_ReturnsCommandsChronologically()
    {
        var service = new CommandStreamService();

        var first = service.QueueCommand(
            CreateRequest(
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal,
                "Timeline first"),
            "First Sensor");

        Thread.Sleep(5);

        var second = service.QueueCommand(
            CreateRequest(
                DeviceCommandType.Calibrate,
                CommandPriority.High,
                "Timeline second"),
            "Second Sensor");

        var timeline =
            service.GetTimeline();

        Assert.Equal(2, timeline.Count);
        Assert.Equal(first.Id, timeline[0].Id);
        Assert.Equal(second.Id, timeline[1].Id);

        Assert.True(
            timeline[0].CreatedAtUtc <=
            timeline[1].CreatedAtUtc);
    }

    private static QueueCommandRequest CreateRequest(
        DeviceCommandType type,
        CommandPriority priority,
        string payload = "Test command",
        Guid? sensorId = null)
    {
        return new QueueCommandRequest
        {
            SensorId =
                sensorId ?? Guid.NewGuid(),
            Type = type,
            Priority = priority,
            Payload = payload
        };
    }
}