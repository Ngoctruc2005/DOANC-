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

    public class DeviceItemViewModel
    {
        public string DeviceId { get; set; }
        public int TotalVisits { get; set; }
        public DateTime? FirstSeen { get; set; }
        public DateTime? LastSeen { get; set; }
        public int DistinctPoiCount { get; set; }
        // Parsed sample fields for display
        public string? AgentSample { get; set; }
        public string? IpSample { get; set; }
        // Current activity status
        public bool IsActive { get; set; }
        public string? StatusLabel { get; set; }
    }

    public class DeviceVisitViewModel
    {
        public int VisitId { get; set; }
        public int? Poiid { get; set; }
        public string? PoiName { get; set; }
        public DateTime? VisitTime { get; set; }
        public string? RawDeviceId { get; set; }
        public string? DeviceAgent { get; set; }
        public string? Ip { get; set; }
    }

    public class DevicesPageViewModel
    {
        public List<DeviceItemViewModel> ActiveDevices { get; set; } = new List<DeviceItemViewModel>();
        public List<DeviceItemViewModel> AllDevices { get; set; } = new List<DeviceItemViewModel>();
        public int TotalUniqueDevices { get; set; }
        public int ActiveDeviceCount => ActiveDevices?.Count ?? 0;
    }
}
