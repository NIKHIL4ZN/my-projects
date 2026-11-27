using Microsoft.EntityFrameworkCore;
using SmartX.Core.DataStructures;
using SmartX.Core.DTOs;
using SmartX.Core.Entities;
using SmartX.Core.Enums;
using SmartX.Core.Exceptions;
using SmartX.Core.Interfaces;
using SmartX.Infrastructure.Data;
using SmartX.Infrastructure.Topology;

namespace SmartX.Infrastructure.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly SmartXDbContext _dbContext;
        private readonly TelemetryHistoryIndex _historyIndex;

        public TelemetryService(
            SmartXDbContext dbContext,
            TelemetryHistoryIndex historyIndex)
        {
            _dbContext = dbContext;
            _historyIndex = historyIndex;
        }

        public async Task<TelemetryResponse> IngestFloatAsync(
            TelemetryPacket<double> packet,
            CancellationToken cancellationToken = default)
        {
            if (!packet.IsValid(double.IsFinite))
            {
                throw new ArgumentException(
                    "The floating-point telemetry packet is invalid.");
            }

            var sensor = await GetSensorAsync(
                packet.SensorId,
                SensorDataType.Float,
                cancellationToken);

            var reading = CreateBaseReading(
                packet,
                SensorDataType.Float);

            reading.FloatValue = packet.Value;

            return await SaveReadingAsync(
                reading,
                sensor,
                cancellationToken);
        }

        public async Task<TelemetryResponse> IngestIntegerAsync(
            TelemetryPacket<int> packet,
            CancellationToken cancellationToken = default)
        {
            if (!packet.IsValid(value => value >= 0))
            {
                throw new ArgumentException(
                    "Integer telemetry must be zero or greater.");
            }

            var sensor = await GetSensorAsync(
                packet.SensorId,
                SensorDataType.Integer,
                cancellationToken);

            var reading = CreateBaseReading(
                packet,
                SensorDataType.Integer);

            reading.IntegerValue = packet.Value;

            return await SaveReadingAsync(
                reading,
                sensor,
                cancellationToken);
        }

        public async Task<TelemetryResponse> IngestBooleanAsync(
            TelemetryPacket<bool> packet,
            CancellationToken cancellationToken = default)
        {
            if (!packet.IsValid(_ => true))
            {
                throw new ArgumentException(
                    "The boolean telemetry packet is invalid.");
            }

            var sensor = await GetSensorAsync(
                packet.SensorId,
                SensorDataType.Boolean,
                cancellationToken);

            var reading = CreateBaseReading(
                packet,
                SensorDataType.Boolean);

            reading.BooleanValue = packet.Value;

            return await SaveReadingAsync(
                reading,
                sensor,
                cancellationToken);
        }

        public Task<IReadOnlyList<TelemetryResponse>>
    IngestFloatBatchAsync(
        IReadOnlyList<TelemetryPacket<double>> packets,
        CancellationToken cancellationToken = default)
        {
            return IngestBatchAsync(
                packets,
                SensorDataType.Float,
                double.IsFinite,
                (reading, value) =>
                    reading.FloatValue = value,
                cancellationToken);
        }

        public Task<IReadOnlyList<TelemetryResponse>>
            IngestIntegerBatchAsync(
                IReadOnlyList<TelemetryPacket<int>> packets,
                CancellationToken cancellationToken = default)
        {
            return IngestBatchAsync(
                packets,
                SensorDataType.Integer,
                value => value >= 0,
                (reading, value) =>
                    reading.IntegerValue = value,
                cancellationToken);
        }

        public Task<IReadOnlyList<TelemetryResponse>>
            IngestBooleanBatchAsync(
                IReadOnlyList<TelemetryPacket<bool>> packets,
                CancellationToken cancellationToken = default)
        {
            return IngestBatchAsync(
                packets,
                SensorDataType.Boolean,
                _ => true,
                (reading, value) =>
                    reading.BooleanValue = value,
                cancellationToken);
        }

        private async Task<IReadOnlyList<TelemetryResponse>>
    IngestBatchAsync<T>(
        IReadOnlyList<TelemetryPacket<T>> packets,
        SensorDataType dataType,
        Func<T, bool> validationRule,
        Action<TelemetryReading, T> assignValue,
        CancellationToken cancellationToken)
    where T : struct
        {
            ArgumentNullException.ThrowIfNull(packets);

            if (packets.Count == 0)
            {
                throw new ArgumentException(
                    "The telemetry batch contains no packets.");
            }

            if (packets.Count > 5000)
            {
                throw new ArgumentException(
                    "A telemetry batch cannot exceed 5000 packets.");
            }

            var sensorId = packets[0].SensorId;

            if (packets.Any(
                packet => packet.SensorId != sensorId))
            {
                throw new ArgumentException(
                    "All packets in a batch must belong " +
                    "to the same sensor.");
            }

            if (packets.Any(
                packet => !packet.IsValid(validationRule)))
            {
                throw new ArgumentException(
                    "The telemetry batch contains invalid packets.");
            }

            var uniqueSequenceNumbers = new HashSet<int>();

            foreach (var packet in packets)
            {
                if (!uniqueSequenceNumbers.Add(
                    packet.SequenceNumber))
                {
                    throw new DuplicateTelemetryPacketException(
                        sensorId,
                        packet.SequenceNumber);
                }
            }

            var sensor = await GetSensorAsync(
                sensorId,
                dataType,
                cancellationToken);

            var sequenceNumbers =
                uniqueSequenceNumbers.ToList();

            var existingSequenceNumber =
                await _dbContext.TelemetryReadings
                    .AsNoTracking()
                    .Where(reading =>
                        reading.SensorId == sensorId
                        && sequenceNumbers.Contains(
                            reading.SequenceNumber))
                    .Select(reading => (int?)reading.SequenceNumber)
                    .FirstOrDefaultAsync(cancellationToken);

            if (existingSequenceNumber.HasValue)
            {
                throw new DuplicateTelemetryPacketException(
                    sensorId,
                    existingSequenceNumber.Value);
            }

            var readings =
                new List<TelemetryReading>(packets.Count);

            foreach (var packet in packets)
            {
                var reading = CreateBaseReading(
                    packet,
                    dataType);

                assignValue(reading, packet.Value);

                readings.Add(reading);
            }

            sensor.LastSeenAtUtc =
                readings.Max(reading => reading.RecordedAtUtc);

            sensor.Status = SensorStatus.Online;

            _dbContext.TelemetryReadings.AddRange(readings);

            await _dbContext.SaveChangesAsync(
    cancellationToken);

            var responses = readings
                .Select(reading => ToResponse(reading, sensor.Name))
                .ToList();

            await _historyIndex.AppendAsync(
                sensor.Id,
                responses);

            return responses;
        }

        private async Task<Sensor> GetSensorAsync(
            Guid sensorId,
            SensorDataType receivedDataType,
            CancellationToken cancellationToken)
        {
            var sensor = await _dbContext.Sensors
                .SingleOrDefaultAsync(
                    item => item.Id == sensorId,
                    cancellationToken);

            if (sensor is null)
            {
                throw new SensorNotFoundException(sensorId);
            }

            if (sensor.DataType != receivedDataType)
            {
                throw new SensorDataTypeMismatchException(
                    sensor.DataType,
                    receivedDataType);
            }

            return sensor;
        }

        private async Task<TelemetryResponse> SaveReadingAsync(
            TelemetryReading reading,
            Sensor sensor,
            CancellationToken cancellationToken)
        {
            var duplicateExists =
                await _dbContext.TelemetryReadings.AnyAsync(
                    item =>
                        item.SensorId == reading.SensorId
                        && item.SequenceNumber
                            == reading.SequenceNumber,
                    cancellationToken);

            if (duplicateExists)
            {
                throw new DuplicateTelemetryPacketException(
                    reading.SensorId,
                    reading.SequenceNumber);
            }

            sensor.LastSeenAtUtc = reading.RecordedAtUtc;
            sensor.Status = SensorStatus.Online;

            _dbContext.TelemetryReadings.Add(reading);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = ToResponse(reading, sensor.Name);

            await _historyIndex.AppendAsync(
                sensor.Id,
                new[] { response });

            return response;
        }

        private static TelemetryReading CreateBaseReading<T>(
            TelemetryPacket<T> packet,
            SensorDataType dataType)
            where T : struct
        {
            return new TelemetryReading
            {
                SensorId = packet.SensorId,
                DataType = dataType,
                Unit = packet.Unit.Trim(),
                SequenceNumber = packet.SequenceNumber,
                RecordedAtUtc = packet.CapturedAtUtc
            };
        }

        public async Task<IReadOnlyList<TelemetryResponse>>
    GetSensorHistoryAsync(
        Guid sensorId,
        int limit = 100,
        CancellationToken cancellationToken = default)
        {
            var sensorExists = await _dbContext.Sensors
                .AsNoTracking()
                .AnyAsync(
                    sensor => sensor.Id == sensorId,
                    cancellationToken);

            if (!sensorExists)
            {
                throw new SensorNotFoundException(sensorId);
            }

            var safeLimit = Math.Clamp(limit, 1, 500);

            var readings = await _dbContext.TelemetryReadings
    .AsNoTracking()
    .Where(reading => reading.SensorId == sensorId)
    .OrderByDescending(reading => reading.RecordedAtUtc)
    .Take(safeLimit)
    .Select(reading => new TelemetryResponse
    {
        Id = reading.Id,
        SensorId = reading.SensorId,
        SensorName = reading.Sensor.Name,
        DataType = reading.DataType,
        FloatValue = reading.FloatValue,
        IntegerValue = reading.IntegerValue,
        BooleanValue = reading.BooleanValue,
        Unit = reading.Unit,
        SequenceNumber = reading.SequenceNumber,
        RecordedAtUtc = reading.RecordedAtUtc
    })
    .ToListAsync(cancellationToken);

            foreach (var reading in readings)
            {
                reading.RecordedAtUtc = DateTime.SpecifyKind(
                    reading.RecordedAtUtc,
                    DateTimeKind.Utc);
            }

            return readings;
        }
        public async Task<IReadOnlyList<TelemetryResponse>>
        GetSensorHistoryRangeAsync(
            Guid sensorId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default)
        {
            if (fromUtc.Kind != DateTimeKind.Utc ||
                toUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Both timestamps must be UTC.");
            }

            if (fromUtc > toUtc)
            {
                throw new ArgumentException(
                    "The start timestamp must not exceed the end timestamp.");
            }

            var sensorName = await _dbContext.Sensors
                .AsNoTracking()
                .Where(sensor => sensor.Id == sensorId)
                .Select(sensor => sensor.Name)
                .SingleOrDefaultAsync(cancellationToken);

            if (sensorName is null)
            {
                throw new SensorNotFoundException(sensorId);
            }

            return await _historyIndex.QueryAsync(
                sensorId,
                fromUtc,
                toUtc,
                async token =>
                {
                    var readings = await _dbContext.TelemetryReadings
                        .AsNoTracking()
                        .Where(reading => reading.SensorId == sensorId)
                        .ToListAsync(token);

                    return readings
                        .Select(reading => ToResponse(reading, sensorName))
                        .ToList();
                },
                cancellationToken);
        }

        private static TelemetryResponse ToResponse(
            TelemetryReading reading,
            string sensorName)
        {
            return new TelemetryResponse
            {
                Id = reading.Id,
                SensorId = reading.SensorId,
                SensorName = sensorName,
                DataType = reading.DataType,
                FloatValue = reading.FloatValue,
                IntegerValue = reading.IntegerValue,
                BooleanValue = reading.BooleanValue,
                Unit = reading.Unit,
                SequenceNumber = reading.SequenceNumber,

                // SQL Server datetime2 does not preserve DateTime.Kind.
                // These values are stored under our UTC convention.
                RecordedAtUtc = DateTime.SpecifyKind(
                    reading.RecordedAtUtc,
                    DateTimeKind.Utc)
            };
        }
    }
}
