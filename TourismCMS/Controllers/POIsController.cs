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
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Globalization;
using System.Text;
using TourismCMS.Data;
using TourismCMS.Models;

namespace TourismCMS.Controllers
{
    [Authorize(Roles = "admin,poi_owner")]
    public class POIsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public POIsController(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
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
                query = query.Where(p => p.Status != "Chờ duyệt" && p.Status != "Ch? duy?t" && p.Status != "Đã xóa" && p.Status != "Ðã xóa");
                ViewData["Layout"] = "~/Views/Shared/_AdminLayout.cshtml";
            }

            ViewData["Title"] = "Danh sách quán ăn";
            return View("Index", await query.ToListAsync());
        }

        // GET: POIs/Pending
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Pending()
        {
            var query = _context.POIs.Where(p => p.Status == "Chờ duyệt" || p.Status == "Ch? duy?t");
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
        public async Task<IActionResult> Create([Bind("Id,Poiid,Name,Latitude,Longitude,Address,Description,Status,ImagePath")] POI pOI, IFormFile? ImageFile)
        {
            // Lo?i b? ki?m tra các navigation property & collection kh?i ModelState
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

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
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
                    pOI.Status = "Ch? duy?t"; // Ch? quán đăng k? s? ? tr?ng thái ch? duy?t
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

            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);
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
        public async Task<IActionResult> Edit(int id, [Bind("Poiid,Id,Name,Latitude,Longitude,Address,Description,Status,ImagePath")] POI pOI, IFormFile? ImageFile)
        {
            if (id != pOI.Poiid)
            {
                return NotFound();
            }

            var existingPOI = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);
            if (existingPOI == null)
            {
                return NotFound();
            }

            // Security check: Ensure owner can only edit their own POIs
            if (User.IsInRole("poi_owner"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (existingPOI.OwnerId != userId)
                {
                    return Forbid();
                }
            }

            // Map editable fields from the submitted model (pOI) to the tracked entity.
            // Do not overwrite OwnerId so we don't introduce NULLs for that column.
            existingPOI.Name = pOI.Name;
            existingPOI.Latitude = pOI.Latitude;
            existingPOI.Longitude = pOI.Longitude;
            existingPOI.Address = pOI.Address;
            existingPOI.Description = pOI.Description;
            existingPOI.ImagePath = pOI.ImagePath;

            // Only allow admins to change Status via this action. Owners cannot change it.
            if (User.IsInRole("admin"))
            {
                existingPOI.Status = pOI.Status;
            }


            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "pois");
                Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }
                existingPOI.ImagePath = "/images/pois/" + uniqueFileName;
            }

            ModelState.Remove("Categories");
            ModelState.Remove("Menus");
            ModelState.Remove("VisitLogs");
            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(existingPOI);
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> SearchAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("Address cannot be empty.");
            }

            var normalizedAddress = string.Join(" ", address.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var normalizedQuery = NormalizeAddressForCompare(normalizedAddress);
            var googleApiKey = _configuration["Geocoding:GoogleApiKey"];

            if (!string.IsNullOrWhiteSpace(googleApiKey))
            {
                var googleResults = await SearchWithGoogleGeocodingAsync(normalizedAddress, googleApiKey);
                if (googleResults.Count > 0)
                {
                    return Content(JsonSerializer.Serialize(googleResults), "application/json");
                }
            }

            var searchQueries = new (string Query, bool UseCountryCode)[]
            {
                (normalizedAddress, true),
                ($"{normalizedAddress}, Việt Nam", true),
                (normalizedAddress, false),
                ($"{normalizedAddress}, Vietnam", false)
            };

            using (var httpClient = new HttpClient())
            {
                // Nominatim requires a valid, unique User-Agent.
                httpClient.DefaultRequestHeaders.Add("User-Agent", "TourismCMS/1.0 (github.com/Ngoctruc2005/DOANC-)");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                try
                {
                    var candidates = new List<(string RawJson, double Score, long PlaceId)>();
                    var seenPlaceIds = new HashSet<long>();

                    foreach (var (query, useCountryCode) in searchQueries.Distinct())
                    {
                        var searchUrl = $"https://nominatim.openstreetmap.org/search?format=jsonv2&q={Uri.EscapeDataString(query)}&accept-language=vi&addressdetails=1&limit=10";
                        if (useCountryCode)
                        {
                            searchUrl += "&countrycodes=vn";
                        }

                        var response = await httpClient.GetAsync(searchUrl);
                        response.EnsureSuccessStatusCode();

                        var jsonString = await response.Content.ReadAsStringAsync();
                        using var json = JsonDocument.Parse(jsonString);
                        if (json.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in json.RootElement.EnumerateArray())
                            {
                                if (!item.TryGetProperty("display_name", out var displayNameElement))
                                {
                                    continue;
                                }

                                var displayName = displayNameElement.GetString() ?? string.Empty;
                                var itemPlaceId = item.TryGetProperty("place_id", out var placeIdElement) && placeIdElement.TryGetInt64(out var pid)
                                    ? pid
                                    : 0L;

                                if (itemPlaceId != 0 && !seenPlaceIds.Add(itemPlaceId))
                                {
                                    continue;
                                }

                                var importance = item.TryGetProperty("importance", out var importanceElement) && importanceElement.TryGetDouble(out var imp)
                                    ? imp
                                    : 0d;

                                double score = CalculateAddressMatchScore(normalizedQuery, displayName) + (importance * 5);

                                if (item.TryGetProperty("type", out var typeElement))
                                {
                                    var type = (typeElement.GetString() ?? string.Empty).ToLowerInvariant();
                                    if (type is "house" or "residential" or "building" or "amenity")
                                    {
                                        score += 8;
                                    }
                                }

                                candidates.Add((item.GetRawText(), score, itemPlaceId));
                            }
                        }
                    }

                    if (candidates.Count > 0)
                    {
                        var ordered = candidates
                            .OrderByDescending(c => c.Score)
                            .ThenByDescending(c => c.PlaceId)
                            .Select(c => c.RawJson);

                        return Content($"[{string.Join(",", ordered)}]", "application/json");
                    }

                    return Content("[]", "application/json");
                }
                catch (HttpRequestException e)
                {
                    return StatusCode(502, $"Error fetching data from Nominatim: {e.Message}");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }

        private static string NormalizeAddressForCompare(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var c in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static double CalculateAddressMatchScore(string normalizedQuery, string displayName)
        {
            if (string.IsNullOrEmpty(normalizedQuery))
            {
                return 0;
            }

            var normalizedDisplayName = NormalizeAddressForCompare(displayName);
            if (string.IsNullOrEmpty(normalizedDisplayName))
            {
                return 0;
            }

            double score = 0;
            if (normalizedDisplayName.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                score += 50;
            }

            var tokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var token in tokens)
            {
                if (normalizedDisplayName.Contains(token, StringComparison.Ordinal))
                {
                    score += 8;
                }
                else
                {
                    score -= 3;
                }
            }

            return score;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ReverseGeocode(double lat, double lon)
        {
            var googleApiKey = _configuration["Geocoding:GoogleApiKey"];
            if (!string.IsNullOrWhiteSpace(googleApiKey))
            {
                var googleAddress = await ReverseWithGoogleGeocodingAsync(lat, lon, googleApiKey);
                if (!string.IsNullOrWhiteSpace(googleAddress))
                {
                    return Content(JsonSerializer.Serialize(new { display_name = googleAddress }), "application/json");
                }
            }

            var reverseUrl = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}&accept-language=vi";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "TourismCMS/1.0 (github.com/Ngoctruc2005/DOANC-)");

                try
                {
                    var response = await httpClient.GetAsync(reverseUrl);
                    response.EnsureSuccessStatusCode();
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return Content(jsonString, "application/json");
                }
                catch (HttpRequestException e)
                {
                    return StatusCode(502, $"Error fetching data from Nominatim: {e.Message}");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }

        private static async Task<List<object>> SearchWithGoogleGeocodingAsync(string address, string apiKey)
        {
            var endpoint = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&region=vn&language=vi&key={Uri.EscapeDataString(apiKey)}";

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                return new List<object>();
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(jsonString);

            if (!json.RootElement.TryGetProperty("status", out var statusElement) ||
                !string.Equals(statusElement.GetString(), "OK", StringComparison.OrdinalIgnoreCase) ||
                !json.RootElement.TryGetProperty("results", out var resultsElement) ||
                resultsElement.ValueKind != JsonValueKind.Array)
            {
                return new List<object>();
            }

            var list = new List<object>();
            foreach (var item in resultsElement.EnumerateArray())
            {
                if (!item.TryGetProperty("geometry", out var geometryElement) ||
                    !geometryElement.TryGetProperty("location", out var locationElement) ||
                    !locationElement.TryGetProperty("lat", out var latElement) ||
                    !locationElement.TryGetProperty("lng", out var lngElement))
                {
                    continue;
                }

                var displayName = item.TryGetProperty("formatted_address", out var formattedAddressElement)
                    ? formattedAddressElement.GetString()
                    : string.Empty;

                list.Add(new
                {
                    lat = latElement.GetDouble(),
                    lon = lngElement.GetDouble(),
                    display_name = displayName,
                    source = "google"
                });
            }

            return list;
        }

        private static async Task<string?> ReverseWithGoogleGeocodingAsync(double lat, double lon, string apiKey)
        {
            var endpoint = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}&language=vi&key={Uri.EscapeDataString(apiKey)}";

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(jsonString);

            if (!json.RootElement.TryGetProperty("status", out var statusElement) ||
                !string.Equals(statusElement.GetString(), "OK", StringComparison.OrdinalIgnoreCase) ||
                !json.RootElement.TryGetProperty("results", out var resultsElement) ||
                resultsElement.ValueKind != JsonValueKind.Array ||
                resultsElement.GetArrayLength() == 0)
            {
                return null;
            }

            var first = resultsElement[0];
            return first.TryGetProperty("formatted_address", out var formattedAddressElement)
                ? formattedAddressElement.GetString()
                : null;
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
                bool isOwner = false;
                if (User.IsInRole("poi_owner"))
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    if (pOI.OwnerId == userId)
                    {
                        isOwner = true;
                    }
                }

                // Chuyển sang trạng thái "Đã xóa" thay vì xóa cứng khỏi CSDL
                pOI.Status = "Đã xóa";
                _context.Update(pOI);
                await _context.SaveChangesAsync();

                if (isOwner)
                {
                    return RedirectToAction("MyRestaurants", "Owner");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Approve(int id)
        {
            // Ki?m tra c? Id và Poiid do m?t s? record c? có s? khác bi?t gi?a khóa
            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id || m.Poiid == id);
            if (pOI != null)
            {
                // Khi đ? duy?t xong th? set tr?ng thái v? Open đ? hi?n th? thành "M?" ho?c "Open"
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

}
