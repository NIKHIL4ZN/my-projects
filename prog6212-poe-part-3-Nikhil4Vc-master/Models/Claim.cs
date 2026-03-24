using System.ComponentModel.DataAnnotations;

namespace MonthlyClaims_System.Models
{
    public class Claim
    {
        // Model used to handle claim details with top level security
        [Key]
        public int ClaimId { get; set; }

        [DataType(DataType.Date)]
        public DateTime ClaimDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Hours worked are required.")]
        [Range(1, 300, ErrorMessage = "Hours must be between 1 and 300.")]
        public int HoursWorked { get; set; }

        [Required]
        public decimal HourlyRate { get; set; }
        [Required]
        public decimal TotalAmount { get; set; }

        [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
        public string? Notes { get; set; }

        public string? DocumentPath { get; set; }

        public string Status { get; set; } = "Pending";

        public string? LecturerEmail { get; set; }

        public string? LecturerName { get; set; } 

        public string? OriginalFileName { get; set; }
        public string? BlobFileName { get; set; }



    }

}
