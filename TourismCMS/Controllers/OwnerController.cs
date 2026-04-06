using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult MyRestaurants()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var list = _context.POIs
            .Where(p => p.OwnerId == userId)
            .ToList();

        return View(list);
    }

    // ➕ form
    public IActionResult Create()
    {
        return View();
    }

    // ➕ thêm quán
    [HttpPost]
    public IActionResult Create(POI p)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        p.OwnerId = userId;
        p.Status = "Chờ duyệt";

        _context.POIs.Add(p);
        _context.SaveChanges();

        return RedirectToAction("MyRestaurants");
    }
}