namespace TourismCMS.Models
{
    public class OwnerRegistrationViewModel
    {
        public int RegistrationId { get; set; }
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public string Status { get; set; } = "pending";
    }
}
