using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using TourismCMS.Data;
using TourismCMS.Models;

namespace TourismCMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Các quán cũ trong DB có thể có Status="Open" thay vì NULL. Nên ta chỉ lọc bỏ "Chờ duyệt" và "Đã xóa"
            var pois = await _context.POIs.Where(p => p.Status != "Chờ duyệt" && p.Status != "Đã xóa").ToListAsync();
            return View(pois);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
