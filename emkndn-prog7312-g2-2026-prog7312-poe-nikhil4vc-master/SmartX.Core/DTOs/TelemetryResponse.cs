using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class TelemetryResponse
    {
        public Guid Id { get; set; }

        public Guid SensorId { get; set; }

        public required string SensorName { get; set; }

        public SensorDataType DataType { get; set; }

        public double? FloatValue { get; set; }

        public int? IntegerValue { get; set; }

        public bool? BooleanValue { get; set; }

        public required string Unit { get; set; }

        public int SequenceNumber { get; set; }

        public DateTime RecordedAtUtc { get; set; }
    }
}
