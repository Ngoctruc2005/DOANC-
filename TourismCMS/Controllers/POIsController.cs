using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public POIsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: POIs
        public async Task<IActionResult> Index()
        {
            var query = _context.POIs.AsQueryable();

            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                query = query.Where(p => p.OwnerId == userId);
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
                if (pOI.OwnerId != userId)
                {
                    return Forbid();
                }
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
        public async Task<IActionResult> Create([Bind("Id,Poiid,Name,Latitude,Longitude,Address,Description,Thumbnail,Status,Radius,ImagePath,AudioPath,CreatedAt,OwnerId")] POI pOI)
        {
            // Loại bỏ kiểm tra các navigation property & collection khỏi ModelState
            ModelState.Remove("Categories");
            ModelState.Remove("Menus");
            ModelState.Remove("VisitLogs");
            ModelState.Remove("Poiid");
            ModelState.Remove("Status");
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                if (User.IsInRole("poi_owner"))
                {
                    pOI.OwnerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    pOI.Status = "Chờ duyệt"; // Chủ quán đăng ký sẽ ở trạng thái chờ duyệt
                }
                else if (User.IsInRole("admin"))
                {
                    pOI.Status = "Đã duyệt"; // Admin đăng ký thì duyệt auto
                }

                pOI.CreatedAt = DateTime.Now;

                _context.Add(pOI);
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
                if (pOI.OwnerId != userId)
                {
                    return Forbid();
                }
            }

            return View(pOI);
        }

        // POST: POIs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Poiid,Id,Name,Latitude,Longitude,Address,Description,Thumbnail,Status,Radius,ImagePath,AudioPath,CreatedAt,OwnerId")] POI pOI)
        {
            if (id != pOI.Poiid)
            {
                return NotFound();
            }

            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                // Đảm bảo không cho phép sửa OwnerId
                pOI.OwnerId = userId;
            }

            ModelState.Remove("Categories");
            ModelState.Remove("Menus");
            ModelState.Remove("VisitLogs");

            if (ModelState.IsValid)
            {
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
                if (pOI.OwnerId != userId)
                {
                    return Forbid();
                }
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
                    if (pOI.OwnerId != userId)
                    {
                        return Forbid();
                    }
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
            return RedirectRequestUrl();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var pOI = await _context.POIs
                .Include(p => p.Menus)
                .Include(p => p.VisitLogs)
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);

            if (pOI != null)
            {
                if (pOI.Menus.Any()) _context.Menus.RemoveRange(pOI.Menus);
                if (pOI.VisitLogs.Any()) _context.VisitLogs.RemoveRange(pOI.VisitLogs);
                if (pOI.Categories.Any()) pOI.Categories.Clear();

                _context.POIs.Remove(pOI);
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
