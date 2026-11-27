using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Exceptions
{
    public class DuplicateTelemetryPacketException : Exception
    {
        public DuplicateTelemetryPacketException(
            Guid sensorId,
            int sequenceNumber)
            : base(
                $"Telemetry packet {sequenceNumber} has already " +
                $"been processed for sensor '{sensorId}'.")
        {
        }
    }
}
