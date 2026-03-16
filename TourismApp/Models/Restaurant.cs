namespace TourismApp.Models
{
    public class Restaurant
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public List<string> Menu { get; set; }

        public string BestSeller { get; set; }
    }
}