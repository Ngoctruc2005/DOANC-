using System;
using System.Collections.Generic;

namespace TourismApp.Models
{
    // A. Content Module
    public class Poi
    {
        // Chỉnh sửa kiểu dữ liệu để khớp với JSON từ Backend API (`Poiid` kiểu int -> chuỗi)
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int Poiid { get; set; }
        public string? Name { get; set; } 
        public string? Description { get; set; } 

        // Tạm đổi Latitude/Longitude nullable do có thể null trong vài trường hợp JSON error (nếu cần)
        public double? Latitude { get; set; } 
        public double? Longitude { get; set; } 

        public string? Thumbnail { get; set; } // Khớp thuộc tính JSON Thumbnail

        public string? Address { get; set; } // Khớp JSON Address
        public string? Status { get; set; } // Khớp JSON Status

        public double? Radius { get; set; } // Khớp JSON Radius (null)
        public string? ImagePath { get; set; } // Khớp JSON ImagePath
        public string? AudioPath { get; set; } // Khớp JSON AudioPath

        public DateTime CreatedAt { get; set; }

        public string? BestSeller { get; set; } // Chỉ để tương thích front end cũ

        // Dùng property Id dạng chuỗi của cũ mapping qua Poiid mới nếu cần
        [System.Text.Json.Serialization.JsonIgnore]
        public string? Id => Poiid.ToString();
    }

    public class Menu
    {
        public string? Id { get; set; }
        public string? PoiId { get; set; } 
        public string? ItemName { get; set; }
        public decimal Price { get; set; }
        public string? ItemImageUrl { get; set; }
    }

    // B. Localization Module
    public class PoiLocalization
    {
        public string? Id { get; set; }
        public string? PoiId { get; set; } 
        public string? LanguageCode { get; set; } 
        public string? TranslatedName { get; set; } 
        public string? TranslatedDescription { get; set; } 
        public string? AudioUrl { get; set; } 
    }
}
