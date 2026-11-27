using SmartX.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Interfaces
{
    public interface ISensorAttachmentService
    {
        Task<SensorAttachmentResponse> UploadAsync(
            Guid sensorId,
            Stream content,
            string originalFileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SensorAttachmentResponse>>
            GetForSensorAsync(
                Guid sensorId,
                CancellationToken cancellationToken = default);

        Task<SensorAttachmentDownload?> DownloadAsync(
            Guid sensorId,
            Guid attachmentId,
            CancellationToken cancellationToken = default);
    }
}
