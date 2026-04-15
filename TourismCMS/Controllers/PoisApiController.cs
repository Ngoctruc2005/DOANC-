using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourismCMS.Data;
using TourismCMS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TourismCMS.Controllers
{
    [ApiController]
    [Route("api/pois")]
    public class PoisApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PoisApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPois()
        {
            // Exclude POIs that have been marked deleted on the CMS so app stays in sync when web deletes a POI
            var pois = await _db.POIs
                .Where(p => p.Status == null || (!p.Status.Contains("xóa") && !p.Status.Contains("hủy")))
                .Select(p => new
                {
                    id = p.Id,
                    poiid = p.Poiid,
                    name = p.Name,
                    description = p.Description,
                    latitude = p.Latitude,
                    longitude = p.Longitude,
                    address = p.Address,
                    status = p.Status,
                    imagePath = p.ImagePath,
                    createdAt = p.CreatedAt,
                    thumbnail = p.ImagePath,
                    radius = 0,
                    audioPath = ""
                })
                .ToListAsync();

            return Ok(pois);
        }

        // GET: api/pois/{id}
        // Returns JSON detail for single POI (for native app consumption)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPoi(int id)
        {
            var p = await _db.POIs.FirstOrDefaultAsync(x => x.Poiid == id || x.Id == id);
            if (p == null) return NotFound();

            var dto = new
            {
                id = p.Id,
                poiid = p.Poiid,
                name = p.Name,
                description = p.Description,
                latitude = p.Latitude,
                longitude = p.Longitude,
                address = p.Address,
                status = p.Status,
                imagePath = p.ImagePath,
                createdAt = p.CreatedAt,
                visitCount = await _db.VisitLogs.CountAsync(v => v.Poiid == p.Poiid)
            };

            return Ok(dto);
        }

        [HttpGet("menus")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenus()
        {
            var menus = await _db.Menus
                .Select(m => new
                {
                    menuId = m.MenuId,
                    poiid = m.Poiid,
                    foodName = m.FoodName,
                    price = m.Price,
                    image = m.Image
                })
                .ToListAsync();

            return Ok(menus);
        }

        // POST: api/pois/{id}/visit
        // App should call this endpoint after scanning QR to register a visit (returns JSON).
        [HttpPost("{id}/visit")]
        [AllowAnonymous]
        public async Task<IActionResult> PostVisit(int id)
        {
            var poi = await _db.POIs.FirstOrDefaultAsync(p => p.Poiid == id || p.Id == id);
            if (poi == null)
            {
                return NotFound(new { success = false, message = "POI not found" });
            }

            try
            {
                var device = Request.Headers["User-Agent"].ToString();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var visit = new VisitLog
                {
                    Poiid = poi.Poiid,
                    DeviceId = string.IsNullOrWhiteSpace(device) ? ip : (device + (string.IsNullOrWhiteSpace(ip) ? string.Empty : " | " + ip)),
                    VisitTime = DateTime.Now
                };

                _db.VisitLogs.Add(visit);
                await _db.SaveChangesAsync();

                var count = await _db.VisitLogs.CountAsync(v => v.Poiid == poi.Poiid);
                return Ok(new { success = true, visitId = visit.VisitId, visits = count });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Error saving visit" });
            }
        }
    }
}