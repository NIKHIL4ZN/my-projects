using SmartX.Core.DataStructures;
using SmartX.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Interfaces
{
    public interface ITelemetryService
    {
        Task<TelemetryResponse> IngestFloatAsync(
            TelemetryPacket<double> packet,
            CancellationToken cancellationToken = default);

        Task<TelemetryResponse> IngestIntegerAsync(
            TelemetryPacket<int> packet,
            CancellationToken cancellationToken = default);

        Task<TelemetryResponse> IngestBooleanAsync(
            TelemetryPacket<bool> packet,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TelemetryResponse>>
    GetSensorHistoryAsync(
        Guid sensorId,
        int limit = 100,
        CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TelemetryResponse>>
    GetSensorHistoryRangeAsync(
        Guid sensorId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TelemetryResponse>>
    IngestFloatBatchAsync(
        IReadOnlyList<TelemetryPacket<double>> packets,
        CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TelemetryResponse>>
            IngestIntegerBatchAsync(
                IReadOnlyList<TelemetryPacket<int>> packets,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TelemetryResponse>>
            IngestBooleanBatchAsync(
                IReadOnlyList<TelemetryPacket<bool>> packets,
                CancellationToken cancellationToken = default);
    }
}
