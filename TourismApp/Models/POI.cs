namespace TourismApp.Models
{
    public class POI
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; } = 50; // mét
    }
}