using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DTOs
{
    public sealed class SensorAttachmentDownload
    {
        public required Stream Content { get; init; }

        public required string FileName { get; init; }

        public required string ContentType { get; init; }
    }
}
