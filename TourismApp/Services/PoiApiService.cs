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

        private static string? _successfulBaseUrl = null;

        IEnumerable<string> GetApiBaseUrls()
        {
            if (!string.IsNullOrWhiteSpace(_successfulBaseUrl))
            {
                yield return _successfulBaseUrl;
            }

            if (!string.IsNullOrWhiteSpace(CustomBaseUrl))
            {
                yield return EnsureApiSuffix(CustomBaseUrl!);
            }

            // Ưu tiên localhost cục bộ trước để chạy cực nhanh nếu đang chạy Backend ở cùng máy tính
            if (DeviceInfo.DeviceType == DeviceType.Physical)
            {
                // Thiết bị thật cắm vào máy tính, cần lấy IP mạng LAN của máy
                yield return "http://192.168.1.191:5219/api/";
                yield return "https://192.168.1.191:7141/api/";
            }
            else if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Máy ảo Android truy cập localhost của máy tính thông qua IP 10.0.2.2
                yield return "http://10.0.2.2:5219/api/";
                yield return "https://10.0.2.2:7141/api/";
            }
            else
            {
                // Máy ảo Windows / Thiết bị khác
                yield return "http://localhost:5219/api/";
                yield return "https://localhost:7141/api/";
            }

            // Sau đó mới đến URL của Dev Tunnels
            yield return "https://nqrwpkxp-5219.asse.devtunnels.ms/api/"; 
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
                // 🔥 Giảm thời gian Timeout xuống 2s để API test nhanh các URL
                Timeout = TimeSpan.FromSeconds(2) 
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
                        _successfulBaseUrl = baseUrl;
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
                        _successfulBaseUrl = baseUrl;
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

        // Tích hợp AI
        public async Task<string> ChatWithAI(string prompt)
        {
            try
            {
                var requestBody = new { prompt = prompt };

                // Lấy danh sách URL base, tìm đến route AI
                foreach(var baseUrl in GetApiBaseUrls().Distinct())
                {
                    try
                    {
                        var endpoint = $"{baseUrl}ai/chat"; 
                        var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody);

                        if (response.IsSuccessStatusCode)
                        {
                            _successfulBaseUrl = baseUrl;
                            var result = await response.Content.ReadAsStringAsync();
                            // Loại bỏ dấu ngoặc kép thừa ở kết quả trả về từ API/JSON
                            return result?.Trim('"', ' ');
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[API AI FAIL] {baseUrl}ai/chat -> {ex.Message}");
                        // Thử endpoint khác
                    }
                }
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"[AI Tổng Lỗi] -> {ex.Message}");
            }

            return string.Empty; // Trả về lỗi rỗng nếu gọi API thất bại toàn bộ
        }
    }
}
