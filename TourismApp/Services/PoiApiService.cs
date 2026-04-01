using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TourismApp.Models;
using TourismCMS.Data;

namespace TourismApp.Services
{
    public class PoiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly FoodDbContext? _dbContext;

        // Use appropriate URL for emulator / physical device
        // Android emulator: 10.0.2.2 points to 127.0.0.1 on the host machine
        // Nếu chạy trên thiết bị thật, hãy dùng địa chỉ IP LAN của máy tính (VD: 10.10.31.246)
        private string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5219/api/" // Dùng 10.0.2.2 để Emulator gọi được localhost của máy tính
            : "https://localhost:7141/api/"; 

        public PoiApiService(FoodDbContext? dbContext = null)
        {
            // Bypass SSL certificate path errors trên Android Emulator
            var handler = new HttpClientHandler();
#if ANDROID
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            // Nếu bạn dùng HTTP thì có thể cho phép cleartext traffic
#endif
            _httpClient = new HttpClient(handler);
            _dbContext = dbContext;
            // _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public async Task<List<Poi>> GetAllPOIsAsync()
        {
            try
            {
                // Thử lấy dữ liệu từ database cục bộ nếu có thể kết nối (Chỉ áp dụng trên Windows vì Android không thể chạy LocalDB)
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
                System.Diagnostics.Debug.WriteLine($"[LỖI DB LẤY QUÁN ĂN] {dbEx.Message}");
            }

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
                };
                var pois = await _httpClient.GetFromJsonAsync<List<Poi>>($"{BaseUrl}pois", options);
                return pois ?? new List<Poi>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI GỌI API] {ex.Message}");

                // Thay vì danh sách rỗng, chèn 1 pin báo lỗi để biết lý do
                return new List<Poi>
                {
                    new Poi
                    {
                        Poiid = -1,
                        Name = "Lỗi API",
                        Description = ex.Message + " " + ex.InnerException?.Message,
                        Latitude = 10.7607,
                        Longitude = 106.7029
                    }
                };
            }
        }

        public async Task<List<Menu>> GetMenusForPoiAsync(string poiId)
        {
            try
            {
                // Thử lấy dữ liệu từ database cục bộ nếu có thể kết nối (Chỉ áp dụng trên Windows vì Android không thể chạy LocalDB)
                if (_dbContext != null && DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    bool canConnect = await _dbContext.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        var dbMenus = await _dbContext.Menus.Where(m => m.PoiId == poiId).ToListAsync();
                        if (dbMenus != null && dbMenus.Any())
                        {
                            return dbMenus;
                        }
                    }
                }
            }
            catch (Exception dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI DB LẤY MENU] {dbEx.Message}");
            }

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
                };
                var menus = await _httpClient.GetFromJsonAsync<List<Menu>>($"{BaseUrl}menus", options);
                return menus?.Where(m => m.PoiId == poiId).ToList() ?? new List<Menu>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI GỌI API MENU] {ex.Message}");
                return new List<Menu>();
            }
        }
    }
}