using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Exceptions
{
    public class SensorNotFoundException : Exception
    {
        public SensorNotFoundException(Guid sensorId)
            : base($"Sensor '{sensorId}' was not found.")
        {
        }
    }
}