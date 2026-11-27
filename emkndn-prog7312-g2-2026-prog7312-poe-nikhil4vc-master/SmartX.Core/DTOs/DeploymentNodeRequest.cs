using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class DeploymentNodeRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [EnumDataType(typeof(DeploymentNodeType))]
        public DeploymentNodeType NodeType { get; set; }

        public string? SensorMacAddress { get; set; }

        public List<DeploymentNodeRequest> Children { get; set; }
            = [];
    }
}
