using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DataStructures
{
    public sealed class TelemetryPacket<T>
     where T : struct
    {
        public Guid SensorId { get; init; }

        public T Value { get; init; }

        public string Unit { get; init; } = string.Empty;

        public DateTime CapturedAtUtc { get; init; } =
            DateTime.UtcNow;

        public int SequenceNumber { get; init; }

        public bool IsValid(Func<T, bool> validationRule)
        {
            ArgumentNullException.ThrowIfNull(validationRule);

            return SensorId != Guid.Empty
                && SequenceNumber >= 0
                && validationRule(Value);
        }
    }
}

