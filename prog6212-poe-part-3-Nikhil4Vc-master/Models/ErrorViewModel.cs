namespace MonthlyClaims_System.Models
{
    public class ErrorViewModel
    {
        // Model handles error views
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
