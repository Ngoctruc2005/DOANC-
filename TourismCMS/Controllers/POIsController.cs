using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TourismCMS.Data;
using TourismCMS.Models;

namespace TourismCMS.Controllers
{
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
            return View(await _context.POIs.ToListAsync());
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
            if (ModelState.IsValid)
            {
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
                .FirstOrDefaultAsync(m => m.Poiid == id);
            if (pOI == null)
            {
                return NotFound();
            }

            return View(pOI);
        }

        // POST: POIs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Poiid == id);
            if (pOI != null)
            {
                _context.POIs.Remove(pOI);
            }

            await _context.SaveChangesAsync();
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
            return await _context.POIs.ToListAsync();
        }
    }
}
