using Microsoft.AspNetCore.Mvc;
using SmartX.Core.DataStructures;
using SmartX.Core.DTOs;
using SmartX.Core.Exceptions;
using SmartX.Core.Interfaces;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/telemetry")]
    public class TelemetryController : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;

        public TelemetryController(
            ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        [HttpPost("float")]
        public Task<ActionResult<TelemetryResponse>> IngestFloat(
            TelemetryIngestionRequest<double> request,
            CancellationToken cancellationToken)
        {
            return IngestAsync(
                request,
                _telemetryService.IngestFloatAsync,
                cancellationToken);
        }

        [HttpPost("integer")]
        public Task<ActionResult<TelemetryResponse>> IngestInteger(
            TelemetryIngestionRequest<int> request,
            CancellationToken cancellationToken)
        {
            return IngestAsync(
                request,
                _telemetryService.IngestIntegerAsync,
                cancellationToken);
        }

        [HttpPost("boolean")]
        public Task<ActionResult<TelemetryResponse>> IngestBoolean(
            TelemetryIngestionRequest<bool> request,
            CancellationToken cancellationToken)
        {
            return IngestAsync(
                request,
                _telemetryService.IngestBooleanAsync,
                cancellationToken);
        }

        [HttpGet("sensor/{sensorId:guid}")]
        public async Task<ActionResult<
    IReadOnlyList<TelemetryResponse>>> GetSensorHistory(
    Guid sensorId,
    [FromQuery] int limit = 100,
    CancellationToken cancellationToken = default)
        {
            try
            {
                var history =
                    await _telemetryService.GetSensorHistoryAsync(
                        sensorId,
                        limit,
                        cancellationToken);

                return Ok(history);
            }
            catch (SensorNotFoundException exception)
            {
                return NotFound(new
                {
                    message = exception.Message
                });
            }
        }

        [HttpGet("sensor/{sensorId:guid}/range")]
        public async Task<ActionResult<IReadOnlyList<TelemetryResponse>>>
    GetSensorHistoryRange(
        Guid sensorId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default)
        {
            if (!fromUtc.HasValue || !toUtc.HasValue)
            {
                return BadRequest(new
                {
                    message = "Both fromUtc and toUtc are required."
                });
            }

            try
            {
                var history =
                    await _telemetryService.GetSensorHistoryRangeAsync(
                        sensorId,
                        fromUtc.Value.UtcDateTime,
                        toUtc.Value.UtcDateTime,
                        cancellationToken);

                return Ok(history);
            }
            catch (SensorNotFoundException exception)
            {
                return NotFound(new
                {
                    message = exception.Message
                });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
        }

        [HttpPost("float/batch")]
        public Task<ActionResult<
    IReadOnlyList<TelemetryResponse>>> IngestFloatBatch(
    TelemetryBatchRequest<double> request,
    CancellationToken cancellationToken)
        {
            return IngestBatchAsync(
                request,
                _telemetryService.IngestFloatBatchAsync,
                cancellationToken);
        }

        [HttpPost("integer/batch")]
        public Task<ActionResult<
            IReadOnlyList<TelemetryResponse>>> IngestIntegerBatch(
            TelemetryBatchRequest<int> request,
            CancellationToken cancellationToken)
        {
            return IngestBatchAsync(
                request,
                _telemetryService.IngestIntegerBatchAsync,
                cancellationToken);
        }

        [HttpPost("boolean/batch")]
        public Task<ActionResult<
            IReadOnlyList<TelemetryResponse>>> IngestBooleanBatch(
            TelemetryBatchRequest<bool> request,
            CancellationToken cancellationToken)
        {
            return IngestBatchAsync(
                request,
                _telemetryService.IngestBooleanBatchAsync,
                cancellationToken);
        }

        private async Task<ActionResult<
    IReadOnlyList<TelemetryResponse>>> IngestBatchAsync<T>(
    TelemetryBatchRequest<T> request,
    Func<
        IReadOnlyList<TelemetryPacket<T>>,
        CancellationToken,
        Task<IReadOnlyList<TelemetryResponse>>>
        ingestAction,
    CancellationToken cancellationToken)
    where T : struct
        {
            try
            {
                var packets =
                    TelemetryBatchProcessor.ConvertToPackets(
                        request.SensorId,
                        request.Batches,
                        request.Unit,
                        request.StartingSequenceNumber,
                        request.StartedAtUtc ?? DateTime.UtcNow,
                        TimeSpan.FromSeconds(
                            request.IntervalSeconds));

                var response = await ingestAction(
                    packets,
                    cancellationToken);

                return StatusCode(
                    StatusCodes.Status201Created,
                    response);
            }
            catch (SensorNotFoundException exception)
            {
                return NotFound(new
                {
                    message = exception.Message
                });
            }
            catch (DuplicateTelemetryPacketException exception)
            {
                return Conflict(new
                {
                    message = exception.Message
                });
            }
            catch (SensorDataTypeMismatchException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
        }

        private async Task<ActionResult<TelemetryResponse>>
            IngestAsync<T>(
                TelemetryIngestionRequest<T> request,
                Func<
                    TelemetryPacket<T>,
                    CancellationToken,
                    Task<TelemetryResponse>> ingestAction,
                CancellationToken cancellationToken)
            where T : struct
        {
            var packet = new TelemetryPacket<T>
            {
                SensorId = request.SensorId,
                Value = request.Value,
                Unit = request.Unit,
                SequenceNumber = request.SequenceNumber,
                CapturedAtUtc =
                    request.CapturedAtUtc ?? DateTime.UtcNow
            };

            try
            {
                var response = await ingestAction(
                    packet,
                    cancellationToken);

                return StatusCode(
                    StatusCodes.Status201Created,
                    response);
            }
            catch (SensorNotFoundException exception)
            {
                return NotFound(new
                {
                    message = exception.Message
                });
            }
            catch (DuplicateTelemetryPacketException exception)
            {
                return Conflict(new
                {
                    message = exception.Message
                });
            }
            catch (SensorDataTypeMismatchException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
        }
    }
}

