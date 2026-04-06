using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourismCMS.Data;
using TourismCMS.Models;

using Microsoft.AspNetCore.Authorization;

namespace TourismCMS.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu nhập lại không khớp.";
                return View();
            }

            var exists = _context.Users.Any(u => u.Username == username) || _context.AdminUsers.Any(a => a.Username == username);
            if (exists)
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại.";
                return View();
            }

            var user = new User
            {
                Username = username,
                Password = password, // (Thực tế nên mã hoá mật khẩu)
                Role = "poi_owner",
                IsVerified = false // Không gán true nữa, yêu cầu admin duyệt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Lưu yêu cầu đăng ký vào bảng PoiOwnerRegistrations để admin thấy trong phần Danh sách chờ duyệt
            var registration = new PoiOwnerRegistration
            {
                UserId = user.Id,
                Status = "pending"
            };
            _context.PoiOwnerRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Đăng ký thành công! Vui lòng chờ Admin duyệt tài khoản của bạn.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var claims = new List<Claim>();

            // Chỉ kiểm tra trong bảng Users (dành cho chủ quán và user bình thường)
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            if (user.Role == "poi_owner" && !user.IsVerified)
            {
                ViewBag.Error = "Tài khoản của bạn chưa được admin duyệt.";
                return View();
            }

            claims.Add(new Claim(ClaimTypes.Name, user.Username));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, user.Role)); // "poi_owner" hoặc "user"

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = false }; 

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            if (claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "poi_owner"))
            {
                return RedirectToAction("Index", "Owner");
            }

            return RedirectToAction("Index", "POIs");
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdminLogin(string username, string password)
        {
            var claims = new List<Claim>();

            // Chỉ kiểm tra trong bảng AdminUsers 
            var adminUser = _context.AdminUsers.FirstOrDefault(a => a.Username == username && a.Password == password);
            if (adminUser != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, adminUser.Username ?? ""));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, adminUser.UserId.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
            }
            else
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu quản trị không đúng.";
                return View();
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = false }; 

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Admin");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
