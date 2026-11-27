using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class TelemetryIngestionRequest<T>
      where T : struct
    {
        public Guid SensorId { get; set; }

        public T Value { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int SequenceNumber { get; set; }

        public DateTime? CapturedAtUtc { get; set; }
    }
}
