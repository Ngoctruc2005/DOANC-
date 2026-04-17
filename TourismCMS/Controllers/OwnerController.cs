using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourismCMS.Data;
using TourismCMS.Models;

[Authorize(Roles = "poi_owner")]
public class OwnerController : Controller
{
    private readonly ApplicationDbContext _context;

    public OwnerController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> VisitHistory()
    {
        // Owners should see QR visit statistics only — redirect to VisitCounts
        return RedirectToAction(nameof(VisitCounts));
    }

    // Report: QR visit counts for this owner's POIs
    public async Task<IActionResult> VisitCounts()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var history = await _context.VisitLogs
            .AsNoTracking()
            .Include(v => v.POI)
            .Where(v => v.POI != null && v.POI.OwnerId == userId)
            .GroupBy(v => new
            {
                v.POI!.Poiid,
                v.POI.Name,
                v.POI.Address
            })
            .Select(g => new VisitHistoryItemViewModel
            {
                Poiid = g.Key.Poiid,
                PoiName = g.Key.Name,
                Address = g.Key.Address,
                TotalVisits = g.Count(),
                UniqueDevices = g.Select(x => x.DeviceId).Where(d => !string.IsNullOrEmpty(d)).Distinct().Count(),
                LastVisitTime = g.Max(x => x.VisitTime)
            })
            .OrderByDescending(x => x.TotalVisits)
            .ThenByDescending(x => x.LastVisitTime)
            .ToListAsync();

        return View(history);
    }

    // Report: unique devices for this owner's POIs
    public async Task<IActionResult> DeviceCounts()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var history = await _context.VisitLogs
            .AsNoTracking()
            .Include(v => v.POI)
            .Where(v => v.POI != null && v.POI.OwnerId == userId)
            .GroupBy(v => new
            {
                v.POI!.Poiid,
                v.POI.Name,
                v.POI.Address
            })
            .Select(g => new VisitHistoryItemViewModel
            {
                Poiid = g.Key.Poiid,
                PoiName = g.Key.Name,
                Address = g.Key.Address,
                TotalVisits = g.Count(),
                UniqueDevices = g.Select(x => x.DeviceId).Where(d => !string.IsNullOrEmpty(d)).Distinct().Count(),
                LastVisitTime = g.Max(x => x.VisitTime)
            })
            .OrderByDescending(x => x.UniqueDevices)
            .ThenByDescending(x => x.LastVisitTime)
            .ToListAsync();

        // Total unique devices across the whole application (not limited to this owner)
        var totalUniqueDevices = await _context.VisitLogs
            .AsNoTracking()
            .Select(v => v.DeviceId)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .CountAsync();

        ViewBag.TotalUniqueDevices = totalUniqueDevices;

        return View(history);
    }

    // List devices that visited this owner's POIs
    public async Task<IActionResult> Devices()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var devices = await _context.VisitLogs
            .AsNoTracking()
            .Include(v => v.POI)
            .Where(v => !string.IsNullOrEmpty(v.DeviceId) && v.POI != null && v.POI.OwnerId == userId)
            .GroupBy(v => v.DeviceId)
            .Select(g => new TourismCMS.Models.DeviceItemViewModel
            {
                DeviceId = g.Key!,
                TotalVisits = g.Count(),
                FirstSeen = g.Min(x => x.VisitTime),
                LastSeen = g.Max(x => x.VisitTime),
                DistinctPoiCount = g.Select(x => x.Poiid).Where(id => id != null).Distinct().Count()
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

        return View(devices);
    }

    public async Task<IActionResult> DeviceDetails(string id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(id)) return BadRequest();
        var decoded = System.Net.WebUtility.UrlDecode(id);

        var visits = await _context.VisitLogs
            .AsNoTracking()
            .Include(v => v.POI)
            .Where(v => v.DeviceId == decoded && v.POI != null && v.POI.OwnerId == userId)
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

    // Visit details for a specific POI (owner can view only their POIs)
    public async Task<IActionResult> VisitDetails(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var visits = await _context.VisitLogs
            .AsNoTracking()
            .Include(v => v.POI)
            .Where(v => v.Poiid == id && v.POI != null && v.POI.OwnerId == userId)
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


    // 📊 quán của tôi
    public async Task<IActionResult> MyRestaurants()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var list = await _context.POIs
            .Where(p => p.OwnerId == userId)
            .ToListAsync();

        return View(list);
    }

    // ➕ form
    public IActionResult Create()
    {
        return View();
    }

    // ➕ thêm quán
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(POI p, List<IFormFile> Images)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            p.OwnerId = userId;
            p.Status = "Chờ duyệt";
            p.CreatedAt = DateTime.Now;

            if (Images != null && Images.Count > 0)
            {
                var imagePaths = new List<string>();
                foreach (var image in Images)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/pois", fileName);

                    // Ensure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    imagePaths.Add("/images/pois/" + fileName);
                }
                p.ImagePath = string.Join(",", imagePaths);
            }

            _context.POIs.Add(p);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRestaurants));
        }
        return View(p);
    }
}