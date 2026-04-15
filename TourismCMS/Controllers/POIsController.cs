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
using System.Net;
using System.Collections.Concurrent;
using TourismCMS.Data;
using TourismCMS.Models;

namespace TourismCMS.Controllers
{
    [Authorize(Roles = "admin,poi_owner")]
    public class POIsController : Controller
    {
        private static readonly ConcurrentDictionary<string, (DateTimeOffset ExpiresAt, string Json)> _geocodeCache = new(StringComparer.OrdinalIgnoreCase);
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
            var query = _context.POIs.Where(p => p.Status != "Chờ duyệt" && p.Status != "Ch? duy?t" && p.Status != "Đã xóa");
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

            if (TryGetCachedGeocode(normalizedAddress, out var cachedJson))
            {
                return Content(cachedJson, "application/json");
            }

            if (TryParseCoordinatesFromInput(normalizedAddress, out var parsedLat, out var parsedLon))
            {
                var parsedJson = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        lat = parsedLat,
                        lon = parsedLon,
                        display_name = normalizedAddress,
                        source = "manual"
                    }
                });

                SetCachedGeocode(normalizedAddress, parsedJson);
                return Content(parsedJson, "application/json");
            }

            var googleApiKey = _configuration["Geocoding:GoogleApiKey"];

            if (!string.IsNullOrWhiteSpace(googleApiKey))
            {
                var googleResults = await SearchWithGoogleGeocodingAsync(normalizedAddress, googleApiKey);
                if (googleResults.Count > 0)
                {
                    var googleJson = JsonSerializer.Serialize(googleResults);
                    SetCachedGeocode(normalizedAddress, googleJson);
                    return Content(googleJson, "application/json");
                }
            }

            var photonResults = await SearchWithPhotonGeocodingAsync(normalizedAddress);
            if (photonResults.Count > 0)
            {
                var photonJson = JsonSerializer.Serialize(photonResults);
                SetCachedGeocode(normalizedAddress, photonJson);
                return Content(photonJson, "application/json");
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
                    var nominatimRateLimited = false;

                    foreach (var (query, useCountryCode) in searchQueries.Distinct())
                    {
                        var searchUrl = $"https://nominatim.openstreetmap.org/search?format=jsonv2&q={Uri.EscapeDataString(query)}&accept-language=vi&addressdetails=1&limit=10";
                        if (useCountryCode)
                        {
                            searchUrl += "&countrycodes=vn";
                        }

                        HttpResponseMessage response;
                        try
                        {
                            response = await httpClient.GetAsync(searchUrl);
                        }
                        catch
                        {
                            continue;
                        }

                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            nominatimRateLimited = true;
                            continue;
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            continue;
                        }

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

                                var countryCode = string.Empty;
                                if (item.TryGetProperty("address", out var addressElement) &&
                                    addressElement.ValueKind == JsonValueKind.Object &&
                                    addressElement.TryGetProperty("country_code", out var countryCodeElement))
                                {
                                    countryCode = countryCodeElement.GetString() ?? string.Empty;
                                }

                                if (string.Equals(countryCode, "vn", StringComparison.OrdinalIgnoreCase))
                                {
                                    score += 20;
                                }
                                else if (!string.IsNullOrWhiteSpace(countryCode))
                                {
                                    score -= 50;
                                }

                                if (item.TryGetProperty("place_rank", out var placeRankElement) && placeRankElement.TryGetInt32(out var placeRank))
                                {
                                    if (placeRank >= 28)
                                    {
                                        score += 12;
                                    }
                                    else if (placeRank <= 20)
                                    {
                                        score -= 8;
                                    }
                                }

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

                        var orderedJson = $"[{string.Join(",", ordered)}]";
                        SetCachedGeocode(normalizedAddress, orderedJson);
                        return Content(orderedJson, "application/json");
                    }

                    var mapsCoResults = await SearchWithMapsCoGeocodingAsync(normalizedAddress);
                    if (mapsCoResults.Count > 0)
                    {
                        var mapsCoJson = JsonSerializer.Serialize(mapsCoResults);
                        SetCachedGeocode(normalizedAddress, mapsCoJson);
                        return Content(mapsCoJson, "application/json");
                    }

                    if (nominatimRateLimited)
                    {
                        return Content("[]", "application/json");
                    }

                    return Content("[]", "application/json");
                }
                catch (Exception ex)
                {
                    return Content("[]", "application/json");
                }
            }
        }

        private static bool TryGetCachedGeocode(string address, out string json)
        {
            if (_geocodeCache.TryGetValue(address, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                json = cached.Json;
                return true;
            }

            _geocodeCache.TryRemove(address, out _);
            json = string.Empty;
            return false;
        }

        private static void SetCachedGeocode(string address, string json)
        {
            _geocodeCache[address] = (DateTimeOffset.UtcNow.AddMinutes(30), json);
        }

        private static bool TryParseCoordinatesFromInput(string input, out double lat, out double lon)
        {
            lat = 0;
            lon = 0;

            var cleaned = input.Replace(";", ",", StringComparison.Ordinal).Trim();
            var parts = cleaned.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.GetCultureInfo("vi-VN"), out lat))
            {
                return false;
            }

            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lon) &&
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.GetCultureInfo("vi-VN"), out lon))
            {
                return false;
            }

            return lat is >= -90 and <= 90 && lon is >= -180 and <= 180;
        }

        private static async Task<List<object>> SearchWithPhotonGeocodingAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return new List<object>();
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "TourismCMS/1.0 (github.com/Ngoctruc2005/DOANC-)");
            var list = new List<object>();
            var queryCandidates = new[]
            {
                address,
                NormalizeAddressForCompare(address),
                NormalizeAddressForCompare(address).Replace(' ', '+')
            }
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Distinct(StringComparer.Ordinal)
            .ToList();

            foreach (var query in queryCandidates)
            {
                var endpoint = $"https://photon.komoot.io/api/?q={Uri.EscapeDataString(query)}";
                HttpResponseMessage response;
                try
                {
                    response = await httpClient.GetAsync(endpoint);
                }
                catch
                {
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(jsonString);

                if (!json.RootElement.TryGetProperty("features", out var featuresElement) ||
                    featuresElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var feature in featuresElement.EnumerateArray())
                {
                    if (!feature.TryGetProperty("geometry", out var geometryElement) ||
                        !geometryElement.TryGetProperty("coordinates", out var coordinatesElement) ||
                        coordinatesElement.ValueKind != JsonValueKind.Array ||
                        coordinatesElement.GetArrayLength() < 2)
                    {
                        continue;
                    }

                    var lon = coordinatesElement[0].GetDouble();
                    var lat = coordinatesElement[1].GetDouble();

                    string displayName;
                    string countryCode = string.Empty;
                    if (feature.TryGetProperty("properties", out var propertiesElement))
                    {
                        var name = propertiesElement.TryGetProperty("name", out var nameElement)
                            ? nameElement.GetString()
                            : null;
                        var street = propertiesElement.TryGetProperty("street", out var streetElement)
                            ? streetElement.GetString()
                            : null;
                        var city = propertiesElement.TryGetProperty("city", out var cityElement)
                            ? cityElement.GetString()
                            : null;
                        var country = propertiesElement.TryGetProperty("country", out var countryElement)
                            ? countryElement.GetString()
                            : null;

                        countryCode = propertiesElement.TryGetProperty("countrycode", out var ccElement)
                            ? ccElement.GetString() ?? string.Empty
                            : string.Empty;

                        displayName = string.Join(", ", new[] { name, street, city, country }.Where(s => !string.IsNullOrWhiteSpace(s)));
                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = address;
                        }
                    }
                    else
                    {
                        displayName = address;
                    }

                    if (!string.IsNullOrWhiteSpace(countryCode) && !string.Equals(countryCode, "VN", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    list.Add(new
                    {
                        lat,
                        lon,
                        display_name = displayName,
                        source = "photon"
                    });
                }

                if (list.Count > 0)
                {
                    break;
                }
            }

            return list;
        }

        private static async Task<List<object>> SearchWithMapsCoGeocodingAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return new List<object>();
            }

            var endpoint = $"https://geocode.maps.co/search?q={Uri.EscapeDataString(address)}&country=VN";
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "TourismCMS/1.0 (github.com/Ngoctruc2005/DOANC-)");

            var response = await httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                return new List<object>();
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(jsonString);
            if (json.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<object>();
            }

            var results = new List<object>();
            foreach (var item in json.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("lat", out var latElement) ||
                    !item.TryGetProperty("lon", out var lonElement))
                {
                    continue;
                }

                var latString = latElement.GetString();
                var lonString = lonElement.GetString();
                if (!double.TryParse(latString, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(lonString, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                {
                    continue;
                }

                var displayName = item.TryGetProperty("display_name", out var displayNameElement)
                    ? displayNameElement.GetString() ?? address
                    : address;

                results.Add(new
                {
                    lat,
                    lon,
                    display_name = displayName,
                    source = "mapsco"
                });
            }

            return results;
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

            if (normalizedDisplayName.StartsWith(normalizedQuery, StringComparison.Ordinal))
            {
                score += 30;
            }

            var tokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 1)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            bool allTokensMatched = true;

            foreach (var token in tokens)
            {
                var isNumericToken = token.All(char.IsDigit);
                if (normalizedDisplayName.Contains(token, StringComparison.Ordinal))
                {
                    score += isNumericToken ? 25 : 8;
                }
                else
                {
                    allTokensMatched = false;
                    score -= isNumericToken ? 20 : 3;
                }
            }

            if (allTokensMatched && tokens.Count > 0)
            {
                score += 20;
            }

            var numericTokens = tokens.Where(static t => t.All(char.IsDigit)).ToList();
            if (numericTokens.Count > 0)
            {
                var numericMatched = numericTokens.Count(t => normalizedDisplayName.Contains(t, StringComparison.Ordinal));
                if (numericMatched == 0)
                {
                    score -= 40;
                }
                else if (numericMatched < numericTokens.Count)
                {
                    score -= 15;
                }
                else
                {
                    score += 10;
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
            var endpoint = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&region=vn&components=country:VN&language=vi&key={Uri.EscapeDataString(apiKey)}";

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
                if (User.IsInRole("poi_owner"))
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    if (pOI.OwnerId != userId)
                    {
                        return Forbid();
                    }

                    var menus = await _context.Menus.Where(m => m.Poiid == pOI.Poiid).ToListAsync();
                    if (menus.Any())
                    {
                        _context.Menus.RemoveRange(menus);
                    }

                    var visits = await _context.VisitLogs.Where(v => v.Poiid == pOI.Poiid).ToListAsync();
                    if (visits.Any())
                    {
                        _context.VisitLogs.RemoveRange(visits);
                    }

                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM POI_Categories WHERE POIID = {0}", pOI.Poiid);

                    _context.POIs.Remove(pOI);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("MyRestaurants", "Owner");
                }

                pOI.Status = "Đã bị admin xóa";
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
