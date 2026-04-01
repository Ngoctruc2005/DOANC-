using System;
using System.Collections.Generic;

namespace TourismCMS.Models
{
    public partial class POI
    {
        public int Id { get; set; }
        public int Poiid { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public string? Description { get; set; }
        public string? Thumbnail { get; set; }
        public string? Status { get; set; }
        public double? Radius { get; set; }
        public string? ImagePath { get; set; }
        public string? AudioPath { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int OwnerId { get; set; }

        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
        public virtual ICollection<VisitLog> VisitLogs { get; set; } = new List<VisitLog>();
    }

    public partial class Menu
    {
        public int MenuId { get; set; }
        public int? Poiid { get; set; }
        public string? FoodName { get; set; }
        public double? Price { get; set; }
        public string? Image { get; set; }

        public virtual POI? Poi { get; set; }
    }
}