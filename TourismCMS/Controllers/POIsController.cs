using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TourismCMS.Data;
using TourismCMS.Models;

namespace TourismCMS.Controllers
{
    [Authorize(Roles = "admin,poi_owner")]
    public class POIsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public POIsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: POIs
        public async Task<IActionResult> Index()
        {
            var query = _context.POIs.AsQueryable();

            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                query = query.Where(p => p.OwnerId == userId && p.Status != "Đã xóa" && p.Status != "Ðã xóa");
            }
            else if (User.IsInRole("admin"))
            {
                // Admin page index only shows approved by default
                query = query.Where(p => p.Status != "Chờ duyệt" && p.Status != "Đã xóa");
                ViewData["Layout"] = "~/Views/Shared/_AdminLayout.cshtml";
            }

            ViewData["Title"] = "Danh sách quán ăn";
            return View("Index", await query.ToListAsync());
        }

        // GET: POIs/Pending
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Pending()
        {
            var query = _context.POIs.Where(p => p.Status == "Chờ duyệt");
            ViewData["Title"] = "Danh sách chờ duyệt";
            ViewData["Layout"] = "~/Views/Shared/_AdminLayout.cshtml";
            return View("Index", await query.ToListAsync());
        }

        // GET: POIs/Approved
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Approved()
        {
            var query = _context.POIs.Where(p => p.Status != "Chờ duyệt" && p.Status != "Đã xóa");
            ViewData["Title"] = "Danh sách đã duyệt";
            ViewData["Layout"] = "~/Views/Shared/_AdminLayout.cshtml";
            return View("Index", await query.ToListAsync());
        }

        // GET: POIs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pOI = await _context.POIs
                .FirstOrDefaultAsync(m => m.Poiid == id);
            if (pOI == null)
            {
                return NotFound();
            }

            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            return View(pOI);
        }

        // GET: POIs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: POIs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Poiid,Name,Latitude,Longitude,Address,Description,Status,ImagePath,CreatedAt")] POI pOI, IFormFile? ImageFile)
        {
            // Loại bỏ kiểm tra các navigation property & collection khỏi ModelState
            ModelState.Remove("Categories");
            ModelState.Remove("Menus");
            ModelState.Remove("VisitLogs");
            ModelState.Remove("Poiid");
            ModelState.Remove("Status");
            ModelState.Remove("OwnerId");
            ModelState.Remove("ImageFile"); // Ignore validation for IFormFile

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "pois");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }
                    pOI.ImagePath = "/images/pois/" + uniqueFileName;
                }

                if (User.IsInRole("poi_owner"))
                {
                    pOI.OwnerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    pOI.Status = "Chờ duyệt"; // Chủ quán đăng ký sẽ ở trạng thái chờ duyệt
                }
                else if (User.IsInRole("admin"))
                {
                    pOI.OwnerId = 0;
                    pOI.Status = "Đã duyệt"; // Admin đăng ký thì duyệt auto
                }

                pOI.CreatedAt = DateTime.Now;
                pOI.Id = 0; // Fix: Khởi tạo giá trị tạm thời để tránh lỗi không cho phép null

                _context.Add(pOI);
                await _context.SaveChangesAsync();

                // Cập nhật Id bằng với Poiid để đồng bộ khóa chính
                pOI.Id = pOI.Poiid;
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(pOI);
        }

        // GET: POIs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Poiid == id);
            if (pOI == null)
            {
                return NotFound();
            }

            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            return View(pOI);
        }

        // POST: POIs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Poiid,Id,Name,Latitude,Longitude,Address,Description,Status,ImagePath,CreatedAt")] POI pOI, IFormFile? ImageFile)
        {
            if (id != pOI.Poiid)
            {
                return NotFound();
            }

            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            ModelState.Remove("Categories");
            ModelState.Remove("Menus");
            ModelState.Remove("VisitLogs");
            ModelState.Remove("OwnerId");
            ModelState.Remove("ImageFile");

            if (pOI.Id == null || pOI.Id == 0)
            {
                pOI.Id = pOI.Poiid;
            }

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "pois");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }
                    pOI.ImagePath = "/images/pois/" + uniqueFileName;
                }

                try
                {
                    _context.Update(pOI);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!POIExists(pOI.Poiid))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pOI);
        }

        // GET: POIs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pOI = await _context.POIs
                .FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);
            if (pOI == null)
            {
                return NotFound();
            }

            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            return View(pOI);
        }

        // POST: POIs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pOI = await _context.POIs
                .FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);

            if (pOI != null)
            {
                if (User.IsInRole("poi_owner"))
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }

                // Chuyển sang trạng thái "Đã xóa" thay vì xóa cứng khỏi CSDL
                pOI.Status = "Đã xóa";
                _context.Update(pOI);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Approve(int id)
        {
            // Kiểm tra cả Id và Poiid do một số record cũ có sự khác biệt giữa khóa
            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);
            if (pOI != null)
            {
                // Khi đã duyệt xong thì set trạng thái về Open để hiển thị thành "Mở" hoặc "Open"
                pOI.Status = "Open";
                _context.Update(pOI);
                await _context.SaveChangesAsync();
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && referer.Contains("/POIs/Pending", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectRequestUrl();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var pOI = await _context.POIs
                .FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);

            if (pOI != null)
            {
                pOI.Status = "Đã hủy";
                _context.Update(pOI);
                await _context.SaveChangesAsync();
            }
            return RedirectRequestUrl();
        }

        private IActionResult RedirectRequestUrl()
        {
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool POIExists(int id)
        {
            return _context.POIs.Any(e => e.Poiid == id);
        }
    }

    [ApiController]
    [Route("api/pois")]
    public class PoisApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PoisApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<POI>>> GetPOIs()
        {
            // Bỏ qua các record đang chờ duyệt hoặc đã bị xóa
            return await _context.POIs.Where(p => p.Status != "Chờ duyệt" && p.Status != "Đã xóa").ToListAsync();
        }
    }
}
