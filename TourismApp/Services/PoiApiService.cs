using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TourismApp.Models;
using TourismCMS.Data;
using Microsoft.Maui.Storage;

namespace TourismApp.Services
{
    public class PoiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly FoodDbContext? _dbContext;

        // Can override from Settings/Preferences: Preferences.Set("api_base_url", "http://10.0.2.2:5219/api/")
        private string? CustomBaseUrl => Preferences.Get("api_base_url", string.Empty);

        IEnumerable<string> GetApiBaseUrls()
        {
            if (!string.IsNullOrWhiteSpace(CustomBaseUrl))
            {
                yield return EnsureApiSuffix(CustomBaseUrl!);
            }

            // DÙNG URL CỦA DEV TUNNELS BẠN ĐANG MỞ
            yield return "https://nqrwpkxp-5219.asse.devtunnels.ms/api/"; 

            // IP Wi-Fi của máy tính bạn hiện tại để điện thoại thật kết nối được!
            if (DeviceInfo.DeviceType == DeviceType.Physical)
            {
                // Thay bằng IP của máy tính (vd: 10.10.20.153)
                yield return "https://10.10.20.153:7141/api/";
                yield return "http://10.10.20.153:5219/api/";
            }
            else if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Android emulator -> host localhost via 10.0.2.2
                // Ưu tiên đúng port 7141 (HTTPS) hoặc 5219 (HTTP) mà Kestrel/IIS Express đang chạy
                yield return "https://10.0.2.2:7141/api/";
                yield return "http://10.0.2.2:5219/api/";
            }
            else
            {
                // Cho Windows App (WinUI)
                yield return "https://localhost:7141/api/";
                yield return "http://localhost:5219/api/";
            }
        }

        static string EnsureApiSuffix(string baseUrl)
        {
            var url = baseUrl.Trim();
            if (!url.EndsWith("/")) url += "/";
            // Ép đuôi api/ (Đ? t?t)
            // if (!url.EndsWith("api/", StringComparison.OrdinalIgnoreCase)) url += "api/";
            return url;
        }

        public PoiApiService(FoodDbContext? dbContext = null)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15) // Tăng lên 15s để tránh Timeout trên máy ảo hoặc mạng chậm
            };
            _dbContext = dbContext;
        }

        public async Task<List<Poi>> GetAllPOIsAsync()
        {
            try
            {
                if (_dbContext != null && DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    bool canConnect = await _dbContext.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        var dbPois = await _dbContext.POIs.ToListAsync();
                        if (dbPois != null && dbPois.Any())
                        {
                            return dbPois;
                        }
                    }
                }
            }
            catch (Exception dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"[L?I DB L?Y QUÁN ĂN] {dbEx.Message}");
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            Exception? lastException = null;
            foreach (var baseUrl in GetApiBaseUrls().Distinct())
            {
                try
                {
                    var endpoint = $"{baseUrl}pois";
                    var pois = await _httpClient.GetFromJsonAsync<List<Poi>>(endpoint, options);
                    if (pois != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[API OK] {endpoint}");
                        return pois;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    System.Diagnostics.Debug.WriteLine($"[API FAIL] {baseUrl}pois -> {ex.Message}");
                }
            }

            return new List<Poi>
            {
                new Poi
                {
                    Poiid = -1,
                    Name = "L?i API",
                    Description = lastException?.Message ?? "Connection failure",
                    Latitude = 10.7607,
                    Longitude = 106.7029
                }
            };
        }

        public async Task<List<Menu>> GetMenusForPoiAsync(string poiId)
        {
            try
            {
                if (_dbContext != null && DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    bool canConnect = await _dbContext.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        var dbMenus = await _dbContext.Menus.Where(m => m.IntPoiId.ToString() == poiId).ToListAsync();
                        if (dbMenus != null && dbMenus.Any())
                        {
                            return dbMenus;
                        }
                    }
                }
            }
            catch (Exception dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"[L?I DB L?Y MENU] {dbEx.Message}");
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            foreach (var baseUrl in GetApiBaseUrls().Distinct())
            {
                try
                {
                    var endpoint = $"{baseUrl}menus";
                    var menus = await _httpClient.GetFromJsonAsync<List<Menu>>(endpoint, options);
                    if (menus != null)
                    {
                        return menus.Where(m => m.PoiId != null && m.PoiId.Equals(poiId)).ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[API MENU FAIL] {baseUrl}menus -> {ex.Message}");
                }
            }

            return new List<Menu>();
        }
    }
}
