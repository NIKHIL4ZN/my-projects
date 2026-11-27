using SmartX.Core.DTOs;

namespace SmartX.Core.Interfaces;

public interface ISensorService
{
    Task<IReadOnlyList<SensorResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<SensorResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SensorResponse> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken cancellationToken = default);
}