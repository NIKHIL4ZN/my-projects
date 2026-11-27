using System.ComponentModel.DataAnnotations;

namespace SmartX.Core.Commands.DTOs;

public class RecommendationQueryRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Query { get; set; } =
        string.Empty;
}