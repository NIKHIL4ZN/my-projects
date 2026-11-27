using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class InvestigationStepResponse
    {
        public int Order { get; set; }

        public required string Title { get; set; }

        public required string Guidance { get; set; }

        public required string ActionType { get; set; }
    }
}
