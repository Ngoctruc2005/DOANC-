using Microsoft.AspNetCore.Mvc;
using TourismCMS.Data;
using TourismCMS.Models;

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
        int userId = 1; // demo

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
        p.OwnerId = 1;
        p.Status = "pending";

        _context.POIs.Add(p);
        _context.SaveChanges();

        return RedirectToAction("MyRestaurants");
    }
}