namespace TourismCMS.Models
{
    public class OwnerRegistrationViewModel
    {
        public int RegistrationId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Username { get; set; }
        public string Status { get; set; }
    }

    public class OwnerListItemViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Username { get; set; }
        public int RestaurantCount { get; set; }
    }

    public class VisitHistoryItemViewModel
    {
        public int Poiid { get; set; }
        public string? PoiName { get; set; }
        public string? Address { get; set; }
        public string? OwnerName { get; set; }
        public int TotalVisits { get; set; }
        public int UniqueDevices { get; set; }
        public DateTime? LastVisitTime { get; set; }
    }
}
