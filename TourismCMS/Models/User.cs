namespace TourismCMS.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // admin / poi_owner / user
        public bool IsVerified { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }
}