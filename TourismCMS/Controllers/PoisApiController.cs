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
            var pois = await _db.POIs
                .Select(p => new
                {
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
    }
}