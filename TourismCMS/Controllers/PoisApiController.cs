using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourismCMS.Data;
using TourismCMS.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace TourismCMS.Controllers
{
    [ApiController]
    [Route("api/pois")]
    public class PoisApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly TourismCMS.Services.DeviceTracker _tracker;

        public PoisApiController(ApplicationDbContext db, TourismCMS.Services.DeviceTracker tracker)
        {
            _db = db;
            _tracker = tracker;
        }

        private static bool IsEmulatorAgent(string agent)
        {
            if (string.IsNullOrWhiteSpace(agent)) return false;
            var lower = agent.ToLowerInvariant();
            // common emulator identifiers
            var emulatorIndicators = new[] { "emulator", "genymotion", "android-sdk", "sdk_gphone", "google_sdk", "android x86", "simulator" };
            return emulatorIndicators.Any(ind => lower.Contains(ind));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPois()
        {
            // Exclude POIs that have been marked deleted on the CMS so app stays in sync when web deletes a POI
            var pois = await _db.POIs
                .Where(p => p.Status == null || (!p.Status.Contains("xóa") && !p.Status.Contains("hủy")))
                .Select(p => new
                {
                    id = p.Id,
                    poiid = p.Poiid,
                    name = p.Name,
                    description = p.Description,
                    latitude = p.Latitude,
                    longitude = p.Longitude,
                    address = p.Address,
                    status = p.Status,
                    imagePath = p.ImagePath,
                    createdAt = p.CreatedAt,
                    thumbnail = p.ImagePath,
                    radius = 0,
                    audioPath = ""
                })
                .ToListAsync();

            return Ok(pois);
        }

        // GET: api/pois/{id}
        // Returns JSON detail for single POI (for native app consumption)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPoi(int id)
        {
            var p = await _db.POIs.FirstOrDefaultAsync(x => x.Poiid == id || x.Id == id);
            if (p == null) return NotFound();

            var dto = new
            {
                id = p.Id,
                poiid = p.Poiid,
                name = p.Name,
                description = p.Description,
                latitude = p.Latitude,
                longitude = p.Longitude,
                address = p.Address,
                status = p.Status,
                imagePath = p.ImagePath,
                createdAt = p.CreatedAt,
                visitCount = await _db.VisitLogs.CountAsync(v => v.Poiid == p.Poiid)
            };

            return Ok(dto);
        }

        [HttpGet("menus")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenus()
        {
            var menus = await _db.Menus
                .Select(m => new
                {
                    menuId = m.MenuId,
                    poiid = m.Poiid,
                    foodName = m.FoodName,
                    price = m.Price,
                    image = m.Image
                })
                .ToListAsync();

            return Ok(menus);
        }

        // POST: api/pois/{id}/visit
        // App should call this endpoint after scanning QR to register a visit (returns JSON).
        [HttpPost("{id}/visit")]
        [AllowAnonymous]
        public async Task<IActionResult> PostVisit(int id)
        {
            var poi = await _db.POIs.FirstOrDefaultAsync(p => p.Poiid == id || p.Id == id);
            if (poi == null)
            {
                return NotFound(new { success = false, message = "POI not found" });
            }

            try
            {
                var device = Request.Headers["User-Agent"].ToString();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var visit = new VisitLog
                {
                    Poiid = poi.Poiid,
                    DeviceId = string.IsNullOrWhiteSpace(device) ? ip : (device + (string.IsNullOrWhiteSpace(ip) ? string.Empty : " | " + ip)),
                    VisitTime = DateTime.Now
                };

                _db.VisitLogs.Add(visit);
                await _db.SaveChangesAsync();

                // Upsert device summary in Devices table
                try
                {
                    var deviceId = visit.DeviceId ?? string.Empty;
                    var agent = deviceId.Split(new[] { " | " }, System.StringSplitOptions.None).FirstOrDefault() ?? deviceId;
                    var existing = await _db.Devices.FindAsync(deviceId);
                    if (existing == null)
                    {
                        existing = new TourismCMS.Models.Device
                        {
                            DeviceId = deviceId,
                            AgentSample = agent,
                            FirstSeen = visit.VisitTime,
                            LastSeen = visit.VisitTime,
                            TotalVisits = 1,
                            IsActive = false
                        };
                        _db.Devices.Add(existing);
                    }
                    else
                    {
                        existing.LastSeen = visit.VisitTime;
                        existing.TotalVisits = existing.TotalVisits + 1;
                    }

                    await _db.SaveChangesAsync();
                }
                catch { }

                // NOTE: Do NOT mark devices as active or update Devices.IsActive for QR-scan visits.
                // QR visits (PostVisit) should not affect the in-memory active device tracker or the
                // "active devices" table shown in the admin UI. This keeps the active device list
                // representative of app-level sessions only.

                var count = await _db.VisitLogs.CountAsync(v => v.Poiid == poi.Poiid);
                return Ok(new { success = true, visitId = visit.VisitId, visits = count });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Error saving visit" });
            }
        }

        // POST: api/pois/device/leave
        // App can call this on app exit to mark device as inactive
        [HttpPost("device/leave")]
        [AllowAnonymous]
        public async Task<IActionResult> PostDeviceLeave()
        {
            // Read raw body so we accept either a JSON string (old clients) or an object { Manufacturer, Model, AppVersion }
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(body)) return BadRequest();

            string normalized = null;

            try
            {
                // If body starts with '{' parse as object
                if (body.TrimStart().StartsWith("{"))
                {
                    var doc = JsonSerializer.Deserialize<TourismCMS.Models.DeviceRegisterRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (doc != null)
                    {
                        // prefer uuid if provided
                        if (!string.IsNullOrWhiteSpace(doc.Uuid))
                        {
                            normalized = doc.Uuid.Trim();
                        }
                        else
                        {
                            normalized = ((doc.Manufacturer ?? string.Empty) + " " + (doc.Model ?? string.Empty)).Trim();
                        }
                    }
                }
                else
                {
                    // body may be a JSON string literal like "Manufacturer Model | App/1.0"
                    try
                    {
                        var s = JsonSerializer.Deserialize<string>(body);
                        if (!string.IsNullOrEmpty(s)) normalized = s.Split(new[] { " | " }, StringSplitOptions.None).FirstOrDefault()?.Trim();
                    }
                    catch
                    {
                        // fallback: use raw body
                        normalized = body.Trim('"', '\r', '\n', ' ');
                    }
                }
            }
            catch
            {
                normalized = body.Trim('"', '\r', '\n', ' ');
            }

            if (string.IsNullOrEmpty(normalized)) return BadRequest();

                // Remove tracker entries that match the normalized agent prefix
            try
            {
                var activeKeys = _tracker.GetActiveDeviceIds();
                foreach (var k in activeKeys)
                {
                    // if we have a uuid, match keys that start with "uuid |" to avoid removing other devices with similar agent names
                    if (!string.IsNullOrEmpty(k))
                    {
                        if (!string.IsNullOrEmpty(normalized) && System.Guid.TryParse(normalized, out _))
                        {
                            if (k.Equals(normalized, System.StringComparison.OrdinalIgnoreCase) || k.StartsWith(normalized + " |", System.StringComparison.OrdinalIgnoreCase))
                                _tracker.Remove(k);
                        }
                        else if (k.StartsWith(normalized, System.StringComparison.OrdinalIgnoreCase))
                        {
                            _tracker.Remove(k);
                        }
                    }
                }
            }
            catch { }

            try
            {
                // Do not delete VisitLogs. Instead, mark matching Devices.IsActive = false
                try
                {
                    var devicesQuery = _db.Devices.AsQueryable();
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        if (System.Guid.TryParse(normalized, out _))
                        {
                            devicesQuery = devicesQuery.Where(d => d.DeviceId.Equals(normalized) || d.DeviceId.StartsWith(normalized + " |"));
                        }
                        else
                        {
                            devicesQuery = devicesQuery.Where(d => d.DeviceId.StartsWith(normalized));
                        }
                    }

                    var matchedDevices = await devicesQuery.ToListAsync();
                    foreach (var md in matchedDevices)
                    {
                        md.IsActive = false;
                    }
                    if (matchedDevices.Any()) await _db.SaveChangesAsync();
                }
                catch { }
            }
            catch { }

            // notify SignalR clients
            try
            {
                var hub = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<TourismCMS.Services.DeviceHub>)) as Microsoft.AspNetCore.SignalR.IHubContext<TourismCMS.Services.DeviceHub>;
                if (hub != null)
                {
                    await hub.Clients.All.SendCoreAsync("DeviceListChanged", new object[] { });
                }
            }
            catch { }

            return Ok(new { success = true });
        }

        // POST: api/pois/device/enter
        // App can call this on app start to register a device (no POI) and mark it active
        [HttpPost("device/enter")]
        [AllowAnonymous]
        public async Task<IActionResult> PostDeviceEnter()
        {
            // Read raw body so we accept either a JSON object { Uuid, Manufacturer, Model, AppVersion } or a string
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            string uuid = null;
            string manufacturer = null;
            string model = null;
            string appv = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith("{"))
                {
                    var doc = JsonSerializer.Deserialize<TourismCMS.Models.DeviceRegisterRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (doc != null)
                    {
                        uuid = doc.Uuid;
                        manufacturer = doc.Manufacturer;
                        model = doc.Model;
                        appv = doc.AppVersion;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(body))
                {
                    // fallback: treat as raw string
                    var s = JsonSerializer.Deserialize<string>(body);
                    if (!string.IsNullOrEmpty(s))
                    {
                        // attempt to split if it's the old pipe format
                        var parts = s.Split(new[] { " | " }, System.StringSplitOptions.None);
                        if (parts.Length > 0) uuid = parts[0];
                        if (parts.Length > 1) manufacturer = parts[1];
                        if (parts.Length > 2) model = parts[2];
                    }
                }
            }
            catch
            {
                // ignore parse errors and fallback to headers
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Build device id including uuid as prefix if available
            string agentPart = string.Join(" ", new[] { manufacturer, model }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            var segments = new List<string>();
            if (!string.IsNullOrWhiteSpace(uuid)) segments.Add(uuid);
            if (!string.IsNullOrWhiteSpace(agentPart)) segments.Add(agentPart);
            if (!string.IsNullOrWhiteSpace(appv)) segments.Add(appv);
            if (!string.IsNullOrWhiteSpace(ip)) segments.Add(ip);

            var fullDeviceId = segments.Count > 0 ? string.Join(" | ", segments) : (Request.Headers["User-Agent"].ToString() ?? ip ?? string.Empty);

            try
            {
                var visit = new VisitLog
                {
                    Poiid = null,
                    DeviceId = fullDeviceId,
                    VisitTime = DateTime.UtcNow
                };

                _db.VisitLogs.Add(visit);
                await _db.SaveChangesAsync();

                // Upsert device summary
                try
                {
                    var deviceId = visit.DeviceId ?? string.Empty;
                    var agent = deviceId.Split(new[] { " | " }, System.StringSplitOptions.None).FirstOrDefault() ?? deviceId;
                    var existing = await _db.Devices.FindAsync(deviceId);
                    if (existing == null)
                    {
                        existing = new TourismCMS.Models.Device
                        {
                            DeviceId = deviceId,
                            AgentSample = agent,
                            FirstSeen = visit.VisitTime,
                            LastSeen = visit.VisitTime,
                            TotalVisits = 1,
                            IsActive = true
                        };
                        _db.Devices.Add(existing);
                    }
                    else
                    {
                        existing.LastSeen = visit.VisitTime;
                        existing.TotalVisits = existing.TotalVisits + 1;
                        existing.IsActive = true;
                    }

                    await _db.SaveChangesAsync();
                }
                catch { }

                // mark device active in tracker (use fullDeviceId stored)
                _tracker.MarkActive(visit.DeviceId);

                // notify SignalR clients that device list changed
                try
                {
                    var hub = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<TourismCMS.Services.DeviceHub>)) as Microsoft.AspNetCore.SignalR.IHubContext<TourismCMS.Services.DeviceHub>;
                    if (hub != null)
                    {
                        await hub.Clients.All.SendCoreAsync("DeviceListChanged", new object[] { });
                    }
                }
                catch { }

                return Ok(new { success = true, visitId = visit.VisitId });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Error saving device visit" });
            }
        }
    }
}