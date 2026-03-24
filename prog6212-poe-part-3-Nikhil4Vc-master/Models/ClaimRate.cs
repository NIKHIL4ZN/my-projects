using System.ComponentModel.DataAnnotations;

namespace MonthlyClaims_System.Models
{
    public class ClaimRate
    {
        // Model used to handle claim rate details with top level security
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(0, 2000)]
        public decimal HourlyRate { get; set; }
    }
}

