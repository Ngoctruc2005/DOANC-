using System.Collections.Generic;

namespace TourismApp.Models;

public class Restaurant
{
    public string? Name { get; set; }           // T�n qu�n
    public string? Description { get; set; }    // M� t?
    public double Latitude { get; set; }       // Vi d?
    public double Longitude { get; set; }      // Kinh d?
    public string? BestSeller { get; set; }     // M�n n?i b?t
    public List<string>? Menu { get; set; }     // Menu qu�n

    // ?? Thuy?t minh da ng�n ng?: key = "vi" ho?c "en"
    public Dictionary<string, string> AudioDescription { get; set; } = new();
}