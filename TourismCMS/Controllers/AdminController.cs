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
        public IActionResult Registrations()
        {
            var list = _context.PoiOwnerRegistrations
                .Where(r => r.Status == "pending")
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new OwnerRegistrationViewModel
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

        public IActionResult Owners()
        {
            var owners = _context.Users
                .Where(u => u.Role == "poi_owner" && u.IsVerified
                    && !string.IsNullOrWhiteSpace(u.FullName)
                    && !string.IsNullOrWhiteSpace(u.PhoneNumber))
                .Select(u => new OwnerListItemViewModel
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
                    (r, u) => new OwnerRegistrationViewModel
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
                .Select(u => new OwnerListItemViewModel
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

            return RedirectToAction("Registrations");
        }

        // ❌ Từ chối owner
        public async Task<IActionResult> Reject(int id)
        {
            var reg = await _context.PoiOwnerRegistrations.FindAsync(id);
            if (reg == null) return NotFound();

            reg.Status = "rejected";

            await _context.SaveChangesAsync();

            return RedirectToAction("Registrations");
        }
    }
}