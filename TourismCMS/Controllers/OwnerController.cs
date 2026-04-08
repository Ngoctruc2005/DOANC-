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
    public async Task<IActionResult> Create(POI p)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        p.OwnerId = userId;
        p.Status = "Chờ duyệt";
        p.CreatedAt = DateTime.Now;

        _context.POIs.Add(p);
        await _context.SaveChangesAsync();

        return RedirectToAction("MyRestaurants");
    }
}