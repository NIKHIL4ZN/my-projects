namespace MonthlyClaims_System.Models
{
    public class EditUserViewModel
    {
        // Model used to handle user details when edited by HR user
        public string UserId { get; set; }
        public string Email { get; set; }
        public string SelectedRole { get; set; }
    }
}
