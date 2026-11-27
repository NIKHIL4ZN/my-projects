using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class TelemetryBatchRequest<T>
    where T : struct
    {
        public Guid SensorId { get; set; }

        [Required]
        public T[][] Batches { get; set; } = [];

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int StartingSequenceNumber { get; set; }

        public DateTime? StartedAtUtc { get; set; }

        [Range(1, 3600)]
        public int IntervalSeconds { get; set; } = 5;
    }
}
