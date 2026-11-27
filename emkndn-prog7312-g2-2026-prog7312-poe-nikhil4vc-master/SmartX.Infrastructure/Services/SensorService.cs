using Microsoft.EntityFrameworkCore;
using SmartX.Core.DTOs;
using SmartX.Core.Entities;
using SmartX.Core.Enums;
using SmartX.Core.Exceptions;
using SmartX.Core.Interfaces;
using SmartX.Infrastructure.Data;

namespace SmartX.Infrastructure.Services
{
    public class SensorService : ISensorService
    {
        private readonly SmartXDbContext _dbContext;

        public SensorService(SmartXDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<SensorResponse>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Sensors
                .AsNoTracking()
                .OrderBy(sensor => sensor.Name)
                .Select(sensor => ToResponse(sensor))
                .ToListAsync(cancellationToken);
        }

        public async Task<SensorResponse?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Sensors
                .AsNoTracking()
                .Where(sensor => sensor.Id == id)
                .Select(sensor => ToResponse(sensor))
                .SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<SensorResponse> RegisterAsync(
            RegisterSensorRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateEnums(request);

            var normalisedMacAddress =
                NormaliseMacAddress(request.MacAddress);

            var alreadyExists = await _dbContext.Sensors
                .AnyAsync(
                    sensor =>
                        sensor.MacAddress == normalisedMacAddress,
                    cancellationToken);

            if (alreadyExists)
            {
                throw new DuplicateSensorException(
                    normalisedMacAddress);
            }

            var sensor = new Sensor
            {
                Name = request.Name.Trim(),
                MacAddress = normalisedMacAddress,
                DeploymentLocation =
                    request.DeploymentLocation.Trim(),
                Category = request.Category,
                DataType = request.DataType,
                Status = SensorStatus.Online
            };

            _dbContext.Sensors.Add(sensor);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToResponse(sensor);
        }

        private static string NormaliseMacAddress(
            string macAddress)
        {
            return macAddress
                .Trim()
                .Replace('-', ':')
                .ToUpperInvariant();
        }

        private static void ValidateEnums(
            RegisterSensorRequest request)
        {
            if (!Enum.IsDefined(request.Category))
            {
                throw new ArgumentException(
                    "The selected sensor category is invalid.");
            }

            if (!Enum.IsDefined(request.DataType))
            {
                throw new ArgumentException(
                    "The selected sensor data type is invalid.");
            }
        }

        private static SensorResponse ToResponse(Sensor sensor)
        {
            return new SensorResponse
            {
                Id = sensor.Id,
                Name = sensor.Name,
                MacAddress = sensor.MacAddress,
                DeploymentLocation =
                    sensor.DeploymentLocation,
                Category = sensor.Category,
                DataType = sensor.DataType,
                Status = sensor.Status,
                CreatedAtUtc = sensor.CreatedAtUtc,
                LastSeenAtUtc = sensor.LastSeenAtUtc
            };
        }
    }
}
