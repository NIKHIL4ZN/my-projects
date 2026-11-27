using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class AnomalyInvestigationResponse
    {
        public Guid InvestigationId { get; set; }

        public Guid SensorId { get; set; }

        public required string SensorName { get; set; }

        public required string DeploymentLocation { get; set; }

        public AnomalySeverity Severity { get; set; }

        public required string Title { get; set; }

        public required string Explanation { get; set; }

        public DateTime DetectedAtUtc { get; set; }

        public double ConfidencePercent { get; set; }

        public List<string> Evidence { get; set; } = [];

        public List<InvestigationStepResponse> Steps { get; set; }
            = [];
    }
}
