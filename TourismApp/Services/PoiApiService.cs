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
        // Cập nhật lại IP Wifi của bạn để chạy trên thiết bị thật
        private string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.10.31.246:5219/api/" // Dùng mạng LAN với điện thoại (cổng HTTP của TourismCMS)
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
                var pois = await _httpClient.GetFromJsonAsync<List<Poi>>($"{BaseUrl}pois");
                return pois ?? new List<Poi>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI GỌI API] {ex.Message}");

                // Trả về danh sách rỗng thay vì dữ liệu mock
                return new List<Poi>();
            }
        }
    }
}