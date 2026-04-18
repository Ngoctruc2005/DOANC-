using System.Collections.Concurrent;
using TourismCMS.Models;

namespace TourismCMS.Services
{
    public class DeviceTracker
    {
        private readonly ConcurrentDictionary<string, DateTime> _active = new ConcurrentDictionary<string, DateTime>();

        public void MarkActive(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return;
            _active[deviceId] = DateTime.UtcNow;
        }

        public bool Remove(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return false;
            return _active.TryRemove(deviceId, out _);
        }

        public List<string> GetActiveDeviceIds()
        {
            var now = DateTime.UtcNow;
            var threshold = TimeSpan.FromMinutes(30);
            // Return only keys with recent activity
            return _active.Where(kvp => (now - kvp.Value) <= threshold).Select(kvp => kvp.Key).ToList();
        }

        public bool IsActive(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return false;
            return _active.ContainsKey(deviceId);
        }

        public int Count => _active.Count;
    }
}