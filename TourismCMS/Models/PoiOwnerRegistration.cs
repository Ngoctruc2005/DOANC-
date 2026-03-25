namespace TourismCMS.Models
{
    public class PoiOwnerRegistration
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } // pending / approved / rejected
    }
}