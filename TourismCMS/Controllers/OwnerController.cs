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