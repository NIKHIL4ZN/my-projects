using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class SensorAttachmentResponse
    {
        public Guid Id { get; set; }

        public Guid SensorId { get; set; }

        public required string OriginalFileName { get; set; }

        public required string ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAtUtc { get; set; }

        public bool IsEncrypted { get; set; }
    }
}
