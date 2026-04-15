using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourismCMS.Models
{
    public partial class POI
    {
        // Make these properties nullable by adding '?'
        public int? Id { get; set; } 
        public int Poiid { get; set; }
        // OwnerId can be null for existing rows; make nullable to match DB mapping
        public int? OwnerId { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        // Ward/District/City removed as requested
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? ImagePath { get; set; }
        [NotMapped]
        public bool IsApproved { get; set; }
        public DateTime? CreatedAt { get; set; }

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

    public partial class POI
    {
        // Radius is removed from the database; keep property for in-memory use
        // but do not map it to the database to avoid SQL errors.
        [NotMapped]
        public double? Radius { get; set; } = 50; // mét
    }
}