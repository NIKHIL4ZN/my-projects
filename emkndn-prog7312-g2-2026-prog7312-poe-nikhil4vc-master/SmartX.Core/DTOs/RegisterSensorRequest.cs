using SmartX.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartX.Core.DTOs
{
    public class RegisterSensorRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
            ErrorMessage =
                "MAC address must use the format AA:BB:CC:DD:EE:FF.")]
        public string MacAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string DeploymentLocation { get; set; } =
            string.Empty;

        [EnumDataType(typeof(SensorCategory))]
        public SensorCategory Category { get; set; }

        [EnumDataType(typeof(SensorDataType))]
        public SensorDataType DataType { get; set; }
    }
}
