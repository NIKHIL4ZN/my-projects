using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Exceptions
{
    public class SensorDataTypeMismatchException : Exception
    {
        public SensorDataTypeMismatchException(
            SensorDataType expected,
            SensorDataType received)
            : base(
                $"This sensor expects {expected} telemetry, " +
                $"but received {received} telemetry.")
        {
        }
    }
}

