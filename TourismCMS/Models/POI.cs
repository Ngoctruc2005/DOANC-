using System.ComponentModel.DataAnnotations;

namespace TourismCMS.Models
{
    public class POI
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int Radius { get; set; }

        public string ImagePath { get; set; }

        public string AudioPath { get; set; }
    }
}