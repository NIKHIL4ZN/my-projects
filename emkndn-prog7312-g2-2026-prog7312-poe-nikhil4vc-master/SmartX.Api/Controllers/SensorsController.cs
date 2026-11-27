using Microsoft.AspNetCore.Mvc;
using SmartX.Core.DTOs;
using SmartX.Core.Exceptions;
using SmartX.Infrastructure.Services;
using SmartX.Core.Interfaces;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/sensors")]
    public class SensorsController : ControllerBase
    {
        private readonly ISensorService _sensorService;

        public SensorsController(ISensorService sensorService)
        {
            _sensorService = sensorService;
        }

        [HttpGet]
        public async Task<ActionResult<
            IReadOnlyList<SensorResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var sensors = await _sensorService.GetAllAsync(
                cancellationToken);

            return Ok(sensors);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SensorResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var sensor = await _sensorService.GetByIdAsync(
                id,
                cancellationToken);

            if (sensor is null)
            {
                return NotFound(new
                {
                    message = "Sensor not found."
                });
            }

            return Ok(sensor);
        }

        [HttpPost]
        public async Task<ActionResult<SensorResponse>> Register(
            RegisterSensorRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var sensor =
                    await _sensorService.RegisterAsync(
                        request,
                        cancellationToken);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = sensor.Id },
                    sensor);
            }
            catch (DuplicateSensorException exception)
            {
                return Conflict(new
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
