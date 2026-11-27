using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Entities
{
    public class TelemetryReading
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SensorId { get; set; }

        public SensorDataType DataType { get; set; }

        public double? FloatValue { get; set; }

        public int? IntegerValue { get; set; }

        public bool? BooleanValue { get; set; }

        public required string Unit { get; set; }

        public int SequenceNumber { get; set; }

        public DateTime RecordedAtUtc { get; set; } =
            DateTime.UtcNow;

        public Sensor Sensor { get; set; } = null!;
    }
}
