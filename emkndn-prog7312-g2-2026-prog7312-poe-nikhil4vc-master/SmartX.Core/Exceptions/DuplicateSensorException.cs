using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Exceptions
{
    public class DuplicateSensorException : Exception
    {
        public DuplicateSensorException(string macAddress)
            : base(
                $"A sensor with MAC address '{macAddress}' is already registered.")
        {
        }
    }
}
