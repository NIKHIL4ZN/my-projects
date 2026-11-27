using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class DeploymentValidationResult
    {
        public bool IsValid => Errors.Count == 0;

        public int NodesVisited { get; set; }

        public int MaximumDepthReached { get; set; }

        public List<string> Errors { get; set; } = [];
    }
}
