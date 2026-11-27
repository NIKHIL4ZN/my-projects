using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Exceptions
{
    public class InvalidAttachmentException : Exception
    {
        public InvalidAttachmentException(string message)
            : base(message)
        {
        }
    }
}
