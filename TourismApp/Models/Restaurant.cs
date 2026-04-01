using System.Collections.Generic;

namespace TourismApp.Models;

public class Restaurant
{
    public string? Name { get; set; }           // Tên quán
    public string? Description { get; set; }    // Mô t?
    public double Latitude { get; set; }       // Vi d?
    public double Longitude { get; set; }      // Kinh d?
    public string? BestSeller { get; set; }     // Món n?i b?t
    public List<string>? Menu { get; set; }     // Menu quán

    // ?? Thuy?t minh da ngôn ng?: key = "vi" ho?c "en"
    public Dictionary<string, string> AudioDescription { get; set; } = new();
}
