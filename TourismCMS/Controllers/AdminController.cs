using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourismCMS.Models;
using TourismCMS.Data;   // ✅ đặt đúng chỗ
using System.Linq;

namespace TourismCMS.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        // ✅ Constructor đúng
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🎛️ Dashboard
        public async Task<IActionResult> Index()
        {
            var pendingStatuses = new[] { "Chờ duyệt", "Ch? duy?t", "Cho duyet" };
            var cancelledStatuses = new[] { "Đã hủy", "?ã h?y", "Ðã h?y", "Ðã hủy", "Da h?y" };
            var deletedStatuses = new[] { "Đã xóa", "?ã xóa", "Ðã xóa", "Đã bị admin xóa", "Da b? admin xóa" };
            var approvedStatuses = new[] { "Open", "Approved", "Đã duyệt", "?ã duy?t", "Ðã duy?t", "Da duyet" };

            ViewBag.PendingRestaurants = await _context.POIs
                .CountAsync(p => pendingStatuses.Contains(p.Status ?? string.Empty));

            ViewBag.ApprovedRestaurants = await _context.POIs
                .CountAsync(p => approvedStatuses.Contains(p.Status ?? string.Empty));

            ViewBag.TotalRestaurants = await _context.POIs
                .CountAsync(p => !pendingStatuses.Contains(p.Status ?? string.Empty)
                                 && !cancelledStatuses.Contains(p.Status ?? string.Empty)
                                 && !deletedStatuses.Contains(p.Status ?? string.Empty));

            ViewBag.TotalOwners = await _context.Users
                .CountAsync(u => u.Role == "poi_owner" && u.IsVerified);

            return View();
        }

        // 🔐 Lấy thông tin admin hiện tại
        [HttpGet("/admin/auth/me")]
        public IActionResult AuthMe()
        {
            return Json(new
            {
                user = User?.Identity?.Name ?? "anonymous"
            });
        }

        // 🔑 Đổi mật khẩu
        [HttpPost("/admin/auth/change-password")]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var username = User.Identity?.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized();

            if (user.Password != currentPassword)
                return BadRequest("Sai mật khẩu hiện tại");

            user.Password = newPassword;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // 📋 Danh sách owner đăng ký (pending)
        public IActionResult OwnerRequests()
        {
            var list = _context.PoiOwnerRegistrations
                .Where(r => r.Status == "pending")
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new TourismCMS.Models.OwnerRegistrationViewModel
                    {
                        RegistrationId = r.Id,
                        UserId = u.Id,
                        FullName = u.FullName,
                        PhoneNumber = u.PhoneNumber,
                        Username = u.Username,
                        Status = r.Status
                    })
                .ToList();

            return View(list);
        }

        public IActionResult Registrations()
        {
            return RedirectToAction(nameof(OwnerRequests));
        }


        public IActionResult Owners()
        {
            var owners = _context.Users
                .Where(u => u.Role == "poi_owner" && u.IsVerified
                    && !string.IsNullOrWhiteSpace(u.FullName)
                    && !string.IsNullOrWhiteSpace(u.PhoneNumber))
                .Select(u => new TourismCMS.Models.OwnerListItemViewModel
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Username = u.Username,
                    RestaurantCount = _context.POIs.Count(p => p.OwnerId == u.Id)
                })
                .ToList();

            return View(owners);
        }

        public IActionResult Cancelled()
        {
            var cancelledOwners = _context.PoiOwnerRegistrations
                .Where(r => r.Status == "rejected")
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new TourismCMS.Models.OwnerRegistrationViewModel
                    {
                        RegistrationId = r.Id,
                        UserId = u.Id,
                        FullName = u.FullName,
                        PhoneNumber = u.PhoneNumber,
                        Username = u.Username,
                        Status = r.Status
                    })
                .ToList();

            var cancelledRestaurants = _context.POIs
                .Where(p => p.Status == "Đã hủy")
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            ViewBag.CancelledOwners = cancelledOwners;
            return View(cancelledRestaurants);
        }

        public IActionResult Deleted()
        {
            var deletedOwners = _context.Users
                .Where(u => u.Role == "deleted_owner")
                .Select(u => new TourismCMS.Models.OwnerListItemViewModel
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Username = u.Username,
                    RestaurantCount = _context.POIs.Count(p => p.OwnerId == u.Id)
                })
                .ToList();

            var deletedRestaurants = _context.POIs
                .Where(p => p.Status == "Đã xóa" || p.Status == "Đã bị admin xóa")
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            ViewBag.DeletedOwners = deletedOwners;
            return View(deletedRestaurants);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeDeleted()
        {
            var deletedStatuses = new[] { "Đã xóa", "?ã xóa", "Ðã xóa", "Đã bị admin xóa", "Da b? admin xóa" };

            // Remove related data for deleted POIs
            var pois = await _context.POIs.Where(p => deletedStatuses.Contains(p.Status ?? string.Empty)).ToListAsync();
            if (pois.Any())
            {
                var poiIds = pois.Select(p => p.Poiid).ToList();

                // Remove menus
                var menus = await _context.Menus.Where(m => m.Poiid != null && poiIds.Contains(m.Poiid.Value)).ToListAsync();
                if (menus.Any()) _context.Menus.RemoveRange(menus);

                // Remove visit logs
                var visits = await _context.VisitLogs.Where(v => v.Poiid != null && poiIds.Contains(v.Poiid.Value)).ToListAsync();
                if (visits.Any()) _context.VisitLogs.RemoveRange(visits);

                // Remove many-to-many join entries from POI_Categories table (if exist)
                if (poiIds.Any())
                {
                    var idsCsv = string.Join(",", poiIds);
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync($"DELETE FROM POI_Categories WHERE POIID IN ({idsCsv})");
                    }
                    catch
                    {
                        // ignore failures here to avoid stopping purge; join table may be empty or named differently
                    }
                }

                // Finally remove POIs
                _context.POIs.RemoveRange(pois);
            }

            // Remove deleted owner accounts and their registrations
            var deletedOwners = await _context.Users.Where(u => u.Role == "deleted_owner").ToListAsync();
            if (deletedOwners.Any())
            {
                var ownerIds = deletedOwners.Select(u => u.Id).ToList();
                var regs = await _context.PoiOwnerRegistrations.Where(r => ownerIds.Contains(r.UserId)).ToListAsync();
                if (regs.Any()) _context.PoiOwnerRegistrations.RemoveRange(regs);

                _context.Users.RemoveRange(deletedOwners);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Deleted));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOldOwners()
        {
            var oldOwnerIds = await _context.Users
                .Where(u => u.Role == "poi_owner" && u.IsVerified
                    && (string.IsNullOrWhiteSpace(u.FullName) || string.IsNullOrWhiteSpace(u.PhoneNumber)))
                .Select(u => u.Id)
                .ToListAsync();

            if (oldOwnerIds.Any())
            {
                var regs = await _context.PoiOwnerRegistrations.Where(r => oldOwnerIds.Contains(r.UserId)).ToListAsync();
                if (regs.Any())
                {
                    _context.PoiOwnerRegistrations.RemoveRange(regs);
                }

                var users = await _context.Users.Where(u => oldOwnerIds.Contains(u.Id)).ToListAsync();
                _context.Users.RemoveRange(users);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Owners));
        }

        public IActionResult OwnerRestaurants(int id)
        {
            var owner = _context.Users.FirstOrDefault(u => u.Id == id && u.Role == "poi_owner");
            if (owner == null)
            {
                return NotFound();
            }

            ViewBag.OwnerName = string.IsNullOrWhiteSpace(owner.FullName) ? owner.Username : owner.FullName;
            ViewBag.OwnerPhone = owner.PhoneNumber;

            var restaurants = _context.POIs
                .Where(p => p.OwnerId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(restaurants);
        }

        // VisitHistory page now shows a selection UI linking to specific reports
        public IActionResult VisitHistory()
        {
            return View();
        }

        // Report: QR visit counts per POI
        public async Task<IActionResult> VisitCounts()
        {
            var history = await _context.VisitLogs
                .AsNoTracking()
                .Include(v => v.POI)
                .Where(v => v.POI != null)
                .GroupBy(v => new
                {
                    v.POI!.Poiid,
                    v.POI.Name,
                    v.POI.Address,
                    v.POI.OwnerId
                })
                .Select(g => new VisitHistoryItemViewModel
                {
                    Poiid = g.Key.Poiid,
                    PoiName = g.Key.Name,
                    Address = g.Key.Address,
                    OwnerName = _context.Users
                        .Where(u => u.Id == g.Key.OwnerId)
                        .Select(u => string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName)
                        .FirstOrDefault(),
                    TotalVisits = g.Count(),
                    UniqueDevices = g.Select(x => x.DeviceId).Where(d => !string.IsNullOrEmpty(d)).Distinct().Count(),
                    LastVisitTime = g.Max(x => x.VisitTime)
                })
                .OrderByDescending(x => x.TotalVisits)
                .ThenByDescending(x => x.LastVisitTime)
                .ToListAsync();

            return View(history);
        }

        // Report: unique devices per POI
        public async Task<IActionResult> DeviceCounts()
        {
            // Build device list (each distinct DeviceId is treated as one device)
            // Only consider visit logs that are app-level (Poiid == null).
            // This excludes QR-scan visits which set Poiid to the scanned POI.
            var devices = await _context.VisitLogs
                .AsNoTracking()
                .Where(v => !string.IsNullOrEmpty(v.DeviceId) && v.Poiid == null)
                .GroupBy(v => v.DeviceId)
                .Select(g => new TourismCMS.Models.DeviceItemViewModel
                {
                    DeviceId = g.Key!,
                    TotalVisits = g.Count(),
                    FirstSeen = g.Min(x => x.VisitTime),
                    LastSeen = g.Max(x => x.VisitTime),
                    DistinctPoiCount = g.Select(x => x.Poiid).Where(id => id != null).Distinct().Count(),
                    // try to extract a readable agent sample from the stored DeviceId
                    AgentSample = (g.Key ?? string.Empty).Split(" | ").FirstOrDefault(),
                    IpSample = (g.Key ?? string.Empty).Split(" | ").Skip(1).FirstOrDefault(),
                    IsActive = false,
                    StatusLabel = ""
                })
                .OrderByDescending(d => d.LastSeen)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var d in devices)
            {
                var parts = (d.DeviceId ?? string.Empty).Split(" | ");
                if (parts.Length >= 2)
                {
                    d.AgentSample = parts[0];
                    d.IpSample = parts[1];
                }
                else
                {
                    d.AgentSample = d.DeviceId;
                    d.IpSample = null;
                }

                if (d.LastSeen.HasValue && (now - d.LastSeen.Value).TotalMinutes <= 30)
                {
                    d.IsActive = true;
                    d.StatusLabel = "Đang hoạt động";
                }
                else
                {
                    d.IsActive = false;
                    d.StatusLabel = d.LastSeen.HasValue ? $"Hoạt động lần cuối: {d.LastSeen.Value:dd/MM/yyyy HH:mm}" : "Không rõ";
                }
            }

            // total unique devices
            var totalUniqueDevices = devices.Select(d => d.DeviceId).Distinct().Count();

            // active devices (last seen within 30 minutes)
            // Use DeviceTracker to get currently active device ids if available
            var activeIds = new List<string>();
            try
            {
                var tracker = HttpContext.RequestServices.GetService(typeof(TourismCMS.Services.DeviceTracker)) as TourismCMS.Services.DeviceTracker;
                if (tracker != null)
                {
                    activeIds = tracker.GetActiveDeviceIds();
                }
            }
            catch
            {
                activeIds = new List<string>();
            }

            var activeDevices = devices.Where(d => activeIds.Contains(d.DeviceId)).ToList();

            var vm = new TourismCMS.Models.DevicesPageViewModel
            {
                ActiveDevices = activeDevices,
                AllDevices = devices,
                TotalUniqueDevices = totalUniqueDevices
            };

            return View("Devices", vm);
        }

        // List all devices that have visited the app
        public async Task<IActionResult> Devices()
        {
            // Read persisted Devices table for stable list of all devices
            var devices = await _context.Devices
                .AsNoTracking()
                .Select(d => new TourismCMS.Models.DeviceItemViewModel
                {
                    DeviceId = d.DeviceId,
                    AgentSample = d.AgentSample,
                    FirstSeen = d.FirstSeen,
                    LastSeen = d.LastSeen,
                    TotalVisits = d.TotalVisits,
                    IsActive = d.IsActive,
                    StatusLabel = d.IsActive ? "Đang hoạt động" : string.Empty
                })
                .OrderByDescending(d => d.LastSeen)
                .ToListAsync();

            // Build active devices list from in-memory tracker only (avoid showing stale DB entries)
            var activeIds = new List<string>();
            try
            {
                var tracker = HttpContext.RequestServices.GetService(typeof(TourismCMS.Services.DeviceTracker)) as TourismCMS.Services.DeviceTracker;
                if (tracker != null)
                {
                    activeIds = tracker.GetActiveDeviceIds();
                }
            }
            catch { }

            var activeDevices = new List<TourismCMS.Models.DeviceItemViewModel>();
            if (activeIds.Any())
            {
                foreach (var key in activeIds)
                {
                    // find the latest visit log that matches this active key (starts with key)
                    // only consider app-level logs (Poiid == null) when resolving active device details
                    var best = await _context.VisitLogs.AsNoTracking()
                        .Where(v => v.DeviceId != null && v.DeviceId.StartsWith(key) && v.Poiid == null)
                        .OrderByDescending(v => v.VisitTime)
                        .FirstOrDefaultAsync();

                    if (best != null)
                    {
                        var item = new TourismCMS.Models.DeviceItemViewModel
                        {
                            DeviceId = best.DeviceId,
                            FirstSeen = await _context.VisitLogs.Where(v => v.DeviceId == best.DeviceId).MinAsync(v => (DateTime?)v.VisitTime),
                            LastSeen = best.VisitTime,
                            TotalVisits = await _context.VisitLogs.CountAsync(v => v.DeviceId == best.DeviceId),
                            DistinctPoiCount = await _context.VisitLogs.Where(v => v.DeviceId == best.DeviceId && v.Poiid != null).Select(v => v.Poiid).Distinct().CountAsync(),
                            AgentSample = (best.DeviceId ?? string.Empty).Split(" | ").FirstOrDefault(),
                            IpSample = (best.DeviceId ?? string.Empty).Split(" | ").Skip(1).FirstOrDefault(),
                            IsActive = true,
                            StatusLabel = "Đang hoạt động"
                        };

                        activeDevices.Add(item);
                    }
                }
            }

            var vm = new TourismCMS.Models.DevicesPageViewModel
            {
                ActiveDevices = activeDevices,
                AllDevices = devices,
                TotalUniqueDevices = await _context.Devices.AsNoTracking().CountAsync()
            };

            return View(vm);
        }

        // Return partial view for active devices only (used by SignalR client to refresh area)
        [HttpGet("/Admin/DevicesActivePartial")]
        public async Task<IActionResult> DevicesActivePartial()
        {
            // Use in-memory tracker to determine active device keys and build partial from latest VisitLogs
            var activeDevices = new List<TourismCMS.Models.DeviceItemViewModel>();

            try
            {
                var tracker = HttpContext.RequestServices.GetService(typeof(TourismCMS.Services.DeviceTracker)) as TourismCMS.Services.DeviceTracker;
                var activeKeys = tracker?.GetActiveDeviceIds() ?? new List<string>();

                foreach (var key in activeKeys)
                {
                    var best = await _context.VisitLogs.AsNoTracking()
                        .Where(v => v.DeviceId != null && v.DeviceId.StartsWith(key))
                        .OrderByDescending(v => v.VisitTime)
                        .FirstOrDefaultAsync();

                    if (best != null)
                    {
                        var item = new TourismCMS.Models.DeviceItemViewModel
                        {
                            DeviceId = best.DeviceId,
                            FirstSeen = await _context.VisitLogs.Where(v => v.DeviceId == best.DeviceId).MinAsync(v => (DateTime?)v.VisitTime),
                            LastSeen = best.VisitTime,
                            TotalVisits = await _context.VisitLogs.CountAsync(v => v.DeviceId == best.DeviceId),
                            DistinctPoiCount = await _context.VisitLogs.Where(v => v.DeviceId == best.DeviceId && v.Poiid != null).Select(v => v.Poiid).Distinct().CountAsync(),
                            AgentSample = (best.DeviceId ?? string.Empty).Split(" | ").FirstOrDefault(),
                            IpSample = (best.DeviceId ?? string.Empty).Split(" | ").Skip(1).FirstOrDefault(),
                            IsActive = true,
                            StatusLabel = "Đang hoạt động"
                        };

                        item.FirstSeen = item.FirstSeen?.ToLocalTime();
                        item.LastSeen = item.LastSeen?.ToLocalTime();

                        activeDevices.Add(item);
                    }
                }
            }
            catch
            {
                // fallback: return empty list
            }

            return PartialView("_ActiveDevicesPartial", activeDevices);
        }

        // Return partial view for all devices (separate from active devices)
        [HttpGet("/Admin/AllDevicesPartial")]
        public async Task<IActionResult> AllDevicesPartial()
        {
            var devices = await _context.Devices
                .AsNoTracking()
                .Select(d => new TourismCMS.Models.DeviceItemViewModel
                {
                    DeviceId = d.DeviceId,
                    AgentSample = d.AgentSample,
                    LastSeen = d.LastSeen,
                    IsActive = d.IsActive
                })
                .OrderByDescending(d => d.LastSeen)
                .ToListAsync();

            // convert times to local in the partial
            devices.ForEach(d => d.LastSeen = d.LastSeen?.ToLocalTime());

            return PartialView("_AllDevicesPartial", devices);
        }

        // Debug: return current in-memory tracker keys (for troubleshooting why devices appear active)
        [HttpGet("/Admin/DeviceTrackerKeys")]
        public IActionResult DeviceTrackerKeys()
        {
            try
            {
                var tracker = HttpContext.RequestServices.GetService(typeof(TourismCMS.Services.DeviceTracker)) as TourismCMS.Services.DeviceTracker;
                var keys = tracker?.GetActiveDeviceIds() ?? new List<string>();
                return Json(new { keys = keys, count = keys.Count });
            }
            catch
            {
                return Json(new { keys = new List<string>(), count = 0 });
            }
        }

        // Admin action: clear active devices (remove from tracker and optionally delete VisitLogs that match)
        [HttpPost("/Admin/ClearActiveDevices")]
        public async Task<IActionResult> ClearActiveDevices()
        {
            try
            {
                var tracker = HttpContext.RequestServices.GetService(typeof(TourismCMS.Services.DeviceTracker)) as TourismCMS.Services.DeviceTracker;
                var keys = tracker?.GetActiveDeviceIds() ?? new List<string>();
                foreach (var k in keys)
                {
                    try { tracker?.Remove(k); } catch { }
                }

                // Do NOT delete VisitLogs here — keep historical device records in the database.
                // Only clear in-memory tracker keys so devices remain archived in "All devices" view.
                await Task.CompletedTask;

                return Ok(new { success = true });
            }
            catch
            {
                return StatusCode(500, new { success = false });
            }
        }

        // Device details by encoded device id (url encoded)
        public async Task<IActionResult> DeviceDetails(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var decoded = System.Net.WebUtility.UrlDecode(id);

            var visits = await _context.VisitLogs
                .AsNoTracking()
                .Where(v => v.DeviceId == decoded)
                .Include(v => v.POI)
                .OrderByDescending(v => v.VisitTime)
                .ToListAsync();

            if (!visits.Any()) return NotFound();

            var model = visits.Select(v => {
                var dv = new TourismCMS.Models.DeviceVisitViewModel
                {
                    VisitId = v.VisitId,
                    Poiid = v.Poiid,
                    PoiName = v.POI?.Name,
                    VisitTime = v.VisitTime,
                    RawDeviceId = v.DeviceId,
                    DeviceAgent = null,
                    Ip = null
                };

                if (!string.IsNullOrEmpty(v.DeviceId))
                {
                    var parts = v.DeviceId.Split(" | ");
                    if (parts.Length >= 2)
                    {
                        dv.DeviceAgent = parts[0];
                        dv.Ip = parts[1];
                    }
                    else
                    {
                        dv.DeviceAgent = v.DeviceId;
                    }
                }

                return dv;
            }).ToList();

            ViewBag.DeviceId = decoded;
            return View(model);
        }

        // Visit details for a specific POI (list individual visits with device and time)
        public async Task<IActionResult> VisitDetails(int id)
        {
            var visits = await _context.VisitLogs
                .AsNoTracking()
                .Where(v => v.Poiid == id)
                .Include(v => v.POI)
                .OrderByDescending(v => v.VisitTime)
                .ToListAsync();

            if (!visits.Any()) return View(new List<TourismCMS.Models.DeviceVisitViewModel>());

            var model = visits.Select(v => {
                var dv = new TourismCMS.Models.DeviceVisitViewModel
                {
                    VisitId = v.VisitId,
                    Poiid = v.Poiid,
                    PoiName = v.POI?.Name,
                    VisitTime = v.VisitTime,
                    RawDeviceId = v.DeviceId,
                    DeviceAgent = null,
                    Ip = null
                };

                if (!string.IsNullOrEmpty(v.DeviceId))
                {
                    var parts = v.DeviceId.Split(" | ");
                    if (parts.Length >= 2)
                    {
                        dv.DeviceAgent = parts[0];
                        dv.Ip = parts[1];
                    }
                    else
                    {
                        dv.DeviceAgent = v.DeviceId;
                    }
                }

                return dv;
            }).ToList();

            ViewBag.PoiId = id;
            ViewBag.PoiName = visits.FirstOrDefault()?.POI?.Name ?? "-";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOwner(int id)
        {
            var owner = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "poi_owner");
            if (owner == null)
            {
                return NotFound();
            }

            owner.Role = "deleted_owner";
            owner.IsVerified = false;

            var ownerRestaurants = await _context.POIs.Where(p => p.OwnerId == id).ToListAsync();
            foreach (var poi in ownerRestaurants)
            {
                poi.Status = "Đã bị admin xóa";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Owners));
        }

        // ✅ Duyệt owner
        public async Task<IActionResult> Approve(int id)
        {
            var reg = await _context.PoiOwnerRegistrations.FindAsync(id);
            if (reg == null) return NotFound();

            reg.Status = "approved";

            var user = await _context.Users.FindAsync(reg.UserId);
            if (user != null)
            {
                user.IsVerified = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Owners));
        }

        // ❌ Từ chối owner
        public async Task<IActionResult> Reject(int id)
        {
            var reg = await _context.PoiOwnerRegistrations.FindAsync(id);
            if (reg == null) return NotFound();

            reg.Status = "rejected";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Cancelled));
        }
    }
}