using System.Collections.Generic;

namespace TourismCMS.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        // Basic info
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }

        // Approval flag used by admin
        public bool IsApproved { get; set; }

        // Optional / existing fields
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string BestSeller { get; set; }
        public List<string> Menu { get; set; } = new();

        // Multilingual audio descriptions
        public Dictionary<string, string> AudioDescription { get; set; } = new();
    }
}