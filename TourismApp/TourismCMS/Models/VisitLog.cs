using System;

namespace TourismCMS.Models
{
    public partial class VisitLog
    {
        public int VisitId { get; set; }
        public int? Poiid { get; set; }
        public string? DeviceId { get; set; }
        public DateTime? VisitTime { get; set; }

        public virtual POI? POI { get; set; }
    }
}
