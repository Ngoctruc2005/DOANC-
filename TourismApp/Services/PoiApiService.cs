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

        static bool IsTimeoutError(Exception ex)
        {
            if (ex is TaskCanceledException || ex is OperationCanceledException)
                return true;

            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("HttpClient.Timeout", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return true;

            if (ex.InnerException != null)
                return IsTimeoutError(ex.InnerException);

            return false;
        }

        static string BuildFriendlyApiError(Exception? ex)
        {
            if (ex == null)
                return "Không thể kết nối API.";

            if (IsTimeoutError(ex))
                return "Kết nối API bị quá thời gian chờ. Hãy kiểm tra backend và mạng.";

            if (ex is HttpRequestException)
                return "Không thể kết nối tới máy chủ API. Hãy kiểm tra URL và backend.";

            return "Không thể tải dữ liệu từ API.";
        }

        // Can override from Settings/Preferences: Preferences.Set("api_base_url", "http://10.0.2.2:5219/api/")
        private string? CustomBaseUrl => Preferences.Get("api_base_url", string.Empty);
        private string? DebugBaseUrl => Environment.GetEnvironmentVariable("TOURISM_API_BASE_URL");

        private static string? _successfulBaseUrl = null;

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
                // Cập nhật lại IP hiện tại của máy tính
                yield return "http://10.10.20.153:5219/api/";
                yield return "https://10.10.20.153:7141/api/";
                yield return "http://10.10.20.153:5219/";
                yield return "https://10.10.20.153:7141/";
            }
            else if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Android emulator -> host localhost via 10.0.2.2
                yield return "http://10.0.2.2:5219/api/";
                yield return "http://10.0.2.2:5219/"; // Thêm d? ph?ng
                yield return "http://10.0.2.2:5000/api/";
                yield return "https://10.0.2.2:7141/api/";
                yield return "https://10.0.2.2:5001/api/";
            }
            else
            {
                yield return "https://localhost:7141/api/";
                yield return "http://localhost:5219/api/";
                yield return "https://localhost:5001/api/";
                yield return "http://localhost:5000/api/";
            }
        }

        static string EnsureApiSuffix(string baseUrl)
        {
            var url = baseUrl.Trim();
            if (!url.EndsWith("/")) url += "/";
            if (!url.EndsWith("api/", StringComparison.OrdinalIgnoreCase)) url += "api/";
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
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var pois = await _httpClient.GetFromJsonAsync<List<Poi>>(endpoint, options, cts.Token);
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
                    Name = "Lỗi API",
                    Description = BuildFriendlyApiError(lastException),
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
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var menus = await _httpClient.GetFromJsonAsync<List<Menu>>(endpoint, options, cts.Token);
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
