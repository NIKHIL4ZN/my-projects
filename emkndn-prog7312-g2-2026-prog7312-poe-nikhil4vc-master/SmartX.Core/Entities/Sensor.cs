using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Entities
{
    public class Sensor
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Name { get; set; }

        public required string MacAddress { get; set; }

        public required string DeploymentLocation { get; set; }

        public SensorCategory Category { get; set; }

        public SensorDataType DataType { get; set; }

        public SensorStatus Status { get; set; } = SensorStatus.Online;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastSeenAtUtc { get; set; }

        public ICollection<TelemetryReading> TelemetryReadings { get; set; }
            = new List<TelemetryReading>();

        public ICollection<SensorAttachment> Attachments { get; set; }
            = new List<SensorAttachment>();
    }
}
