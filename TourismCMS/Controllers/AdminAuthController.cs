using Microsoft.AspNetCore.Mvc;
using TourismCMS.Data;
using TourismCMS.Models;

namespace TourismCMS.Controllers
{
    [Route("admin/auth")]
    public class AdminAuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminAuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ ĐĂNG KÝ OWNER
        [HttpPost("register-owner")]
        public IActionResult RegisterOwner([FromForm] string username, [FromForm] string password)
        {
            // kiểm tra trùng username
            var exists = _context.Users.Any(u => u.Username == username);
            if (exists)
                return BadRequest("Username đã tồn tại");

            var user = new User
            {
                Username = username,
                Password = password, // (sau này nên hash)
                Role = "poi_owner",
                IsVerified = false
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Đăng ký thành công, chờ admin duyệt",
                userId = user.Id
            });
        }
    }
}