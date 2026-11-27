using Microsoft.AspNetCore.Mvc;
using SmartX.Core.Commands.DTOs;
using SmartX.Core.Commands.Exceptions;
using SmartX.Core.Commands.Interfaces;
using SmartX.Core.Interfaces;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    private readonly ICommandStreamService
        _commandStreamService;

    private readonly ISensorService
        _sensorService;

    public CommandsController(
        ICommandStreamService commandStreamService,
        ISensorService sensorService)
    {
        _commandStreamService =
            commandStreamService;

        _sensorService = sensorService;
    }

    [HttpGet]
    public ActionResult<
        IReadOnlyList<CommandResponse>>
        GetCommands()
    {
        return Ok(
            _commandStreamService.GetCommands());
    }

    [HttpGet("timeline")]
    public ActionResult<
        IReadOnlyList<CommandResponse>>
        GetTimeline()
    {
        return Ok(
            _commandStreamService.GetTimeline());
    }

    [HttpGet("summary")]
    public ActionResult<CommandQueueSummary>
        GetSummary()
    {
        return Ok(
            _commandStreamService.GetSummary());
    }

    [HttpGet("{commandId:guid}")]
    public ActionResult<CommandResponse>
        GetCommand(Guid commandId)
    {
        try
        {
            return Ok(
                _commandStreamService.GetCommand(
                    commandId));
        }
        catch (
            CommandNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPost]
    public async Task<
        ActionResult<CommandResponse>>
        QueueCommand(
            QueueCommandRequest request,
            CancellationToken cancellationToken)
    {
        var sensor =
            await _sensorService.GetByIdAsync(
                request.SensorId,
                cancellationToken);

        if (sensor is null)
        {
            return NotFound(new
            {
                message =
                    $"Sensor '{request.SensorId}' " +
                    "could not be found."
            });
        }

        try
        {
            var response =
                _commandStreamService.QueueCommand(
                    request,
                    sensor.Name);

            return CreatedAtAction(
                nameof(GetCommand),
                new
                {
                    commandId = response.Id
                },
                response);
        }
        catch (
            DuplicateCommandException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPost("process-next")]
    public ActionResult<CommandResponse>
        ProcessNextCommand()
    {
        var response =
            _commandStreamService
                .ProcessNextCommand();

        if (response is null)
        {
            return Conflict(new
            {
                message =
                    "There are no queued commands " +
                    "to process."
            });
        }

        return Ok(response);
    }

    [HttpPost("undo")]
    public ActionResult<CommandResponse>
        UndoLastCompletedCommand()
    {
        var response =
            _commandStreamService
                .UndoLastCompletedCommand();

        if (response is null)
        {
            return Conflict(new
            {
                message =
                    "There is no completed command " +
                    "available to undo."
            });
        }

        return Ok(response);
    }
}