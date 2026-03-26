using System.Collections.Generic;

namespace TourismApp.Models;

public class Restaurant
{
    public string Name { get; set; }           // Tên quán
    public string Description { get; set; }    // Mô tả
    public double Latitude { get; set; }       // Vĩ độ
    public double Longitude { get; set; }      // Kinh độ
    public string BestSeller { get; set; }     // Món nổi bật
    public List<string> Menu { get; set; }     // Menu quán

    // 🔊 Thuyết minh đa ngôn ngữ: key = "vi" hoặc "en"
    public Dictionary<string, string> AudioDescription { get; set; } = new();
}