using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index()
        {
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
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            var username = User.Identity?.Name;

            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return Unauthorized();

            if (user.Password != currentPassword)
                return BadRequest("Sai mật khẩu hiện tại");

            user.Password = newPassword;
            _context.SaveChanges();

            return Ok(new { success = true });
        }

        // 📋 Danh sách owner đăng ký (pending)
        public IActionResult Registrations()
        {
            var list = _context.PoiOwnerRegistrations
                .Where(r => r.Status == "pending")
                .ToList();

            return View(list);
        }

        // ✅ Duyệt owner
        public IActionResult Approve(int id)
        {
            var reg = _context.PoiOwnerRegistrations.Find(id);
            if (reg == null) return NotFound();

            reg.Status = "approved";

            var user = _context.Users.Find(reg.UserId);
            if (user != null)
            {
                user.IsVerified = true;
            }

            _context.SaveChanges();

            return RedirectToAction("Registrations");
        }

        // ❌ Từ chối owner
        public IActionResult Reject(int id)
        {
            var reg = _context.PoiOwnerRegistrations.Find(id);
            if (reg == null) return NotFound();

            reg.Status = "rejected";

            _context.SaveChanges();

            return RedirectToAction("Registrations");
        }
    }
}