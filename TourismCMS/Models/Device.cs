using System;

namespace TourismCMS.Models
{
    public class Device
    {
        // Primary key: full stored device id (agent | ip | ...)
        public string DeviceId { get; set; }

        // short sample for display (agent or uuid)
        public string? AgentSample { get; set; }

        public DateTime? FirstSeen { get; set; }
        public DateTime? LastSeen { get; set; }

        // aggregate count of visits recorded
        public int TotalVisits { get; set; }

        // whether the device is currently active (in-memory tracker)
        public bool IsActive { get; set; }
    }
} 