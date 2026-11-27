using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Entities
{
    public class SensorAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SensorId { get; set; }

        public required string OriginalFileName { get; set; }

        public required string StoredFileName { get; set; }

        public required string ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        public required string RelativePath { get; set; }

        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

        public Sensor Sensor { get; set; } = null!;

        public bool IsEncrypted { get; set; } = true;
    }
}
