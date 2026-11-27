using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class SensorResponse
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public required string MacAddress { get; set; }

        public required string DeploymentLocation { get; set; }

        public SensorCategory Category { get; set; }

        public SensorDataType DataType { get; set; }

        public SensorStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? LastSeenAtUtc { get; set; }
    }
}
