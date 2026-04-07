namespace TourismCMS.Models
{
    public class OwnerListItemViewModel
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public int RestaurantCount { get; set; }
    }
}
