namespace TourismCMS.Models
{
    public class Registration
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int POIId { get; set; }

        public string Status { get; set; }
    }
}