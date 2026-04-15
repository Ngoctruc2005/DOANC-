using System.Xml;
using TourismApp.Models;
using TourismApp.Services;

using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;

namespace TourismApp.Views;

public partial class RestaurantDetailPage : ContentPage
{
    private Poi _restaurant;
    private bool _autoPlayDescription;

    public RestaurantDetailPage(Poi restaurant, bool autoPlayDescription = false)
    {
        InitializeComponent();
        BindingContext = LocalizationService.Instance;
        _restaurant = restaurant;
        _autoPlayDescription = autoPlayDescription;

        nameLabel.Text = _restaurant.Name;
        descriptionLabel.Text = _restaurant.Description;

        // Xử lý hình ảnh (ImagePath hoặc Thumbnail)
        string? imageUrl = !string.IsNullOrWhiteSpace(_restaurant.ImagePath) ? _restaurant.ImagePath : _restaurant.Thumbnail;

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            imageUrl = imageUrl.Trim();
            string fullImageUrl = imageUrl;

            if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu backend ném về tên file (VD: "uploads/image.jpg")
                // Ta nối với Base URL của server để lấy ảnh trực tiếp từ internet
                string baseUrl = "https://nqrwpkxp-5219.asse.devtunnels.ms/";

                // Thử kết nối lấy ảnh từ URL baseUrl nếu có
                var instance = new PoiApiService(null);
                var urls = instance.GetType().GetMethod("GetApiBaseUrls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(instance, null) as IEnumerable<string>;
                if (urls != null)
                {
                    baseUrl = urls.FirstOrDefault(u => !string.IsNullOrEmpty(u) && !u.Contains("localhost") && !u.Contains("127.0.0.1") && u.Contains("devtunnels.ms")) ?? baseUrl;

                    baseUrl = baseUrl.Replace("api/", ""); // xoá api path ra khỏi image path
                }

                // Chuẩn hóa chuỗi URL tránh bị lỗi dính 2 dấu gạch chéo
                fullImageUrl = baseUrl.TrimEnd('/') + "/" + imageUrl.TrimStart('/');
            }

            LoadImageWithBypassAsync(fullImageUrl);
        }
        else
        {
            // Nếu API không trả về ảnh quán, sử dụng luôn ảnh mặc định từ Internet
            restaurantImage.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
        }

        UpdateFavoriteButtonText();
    }

    private async void LoadImageWithBypassAsync(string fullImageUrl)
    {
        try
        {
            // Tự động tải ảnh bằng HttpClient cho mọi URL để vượt qua SSL lỗi / Dev Tunnels
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            // Header bypass cho Dev Tunnels, Ngrok
            client.DefaultRequestHeaders.Add("X-DevTunnels-Skip-Anti-Phishing-Page", "true");
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await client.GetAsync(fullImageUrl);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    restaurantImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[HTTP Lỗi tải ảnh] Mã {response.StatusCode} từ {fullImageUrl}");
                // Fallback nếu link hỏng
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    restaurantImage.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lỗi tải ảnh Exception] {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                restaurantImage.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
            });
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateFavoriteButtonText();
        var lang = Microsoft.Maui.Storage.Preferences.Get("language", "vi");

        string finalName = _restaurant.Name;
        string finalDescription = _restaurant.Description;

        if (lang != "vi")
        {
            nameLabel.Text = LocalizationService.Instance["Loading"];
            descriptionLabel.Text = LocalizationService.Instance["Loading"];

            string translatedName = await TTSHelper.TranslateTextAsync(_restaurant.Name, lang);
            string translatedDesc = await TTSHelper.TranslateTextAsync(_restaurant.Description, lang);
            finalName = translatedName;
            finalDescription = translatedDesc;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                nameLabel.Text = translatedName;
                descriptionLabel.Text = translatedDesc;
            });
        }
        else
        {
            nameLabel.Text = _restaurant.Name;
            descriptionLabel.Text = _restaurant.Description;
        }

        if (_autoPlayDescription)
        {
            _autoPlayDescription = false; // Chỉ phát 1 lần khi mới quét QR xong
            var name = string.IsNullOrWhiteSpace(finalName) ? "Quán ăn" : finalName;
            var desc = string.IsNullOrWhiteSpace(finalDescription) ? string.Empty : $". {finalDescription}";
            var textToSpeak = $"{name}{desc}";

            if (!string.IsNullOrWhiteSpace(textToSpeak))
            {
                _ = TTSHelper.SpeakDescriptionAsync(textToSpeak);
            }
        }
    }


    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        var name = string.IsNullOrWhiteSpace(_restaurant.Name) ? "Quán ăn" : _restaurant.Name;
        var desc = string.IsNullOrWhiteSpace(_restaurant.Description) ? string.Empty : $". {_restaurant.Description}";
        var textToSpeak = $"{name}{desc}";
        await TTSHelper.SpeakDescriptionAsync(textToSpeak);
    }

    private async void OnDirectionsClicked(object sender, EventArgs e)
    {
        if (_restaurant == null || !_restaurant.Latitude.HasValue || !_restaurant.Longitude.HasValue)
        {
            await DisplayAlert("Thông báo", "Quán ăn này chưa có tọa độ trên bản đồ.", "OK");
            return;
        }

        var location = new Location(_restaurant.Latitude.Value, _restaurant.Longitude.Value);
        var options = new MapLaunchOptions { Name = _restaurant.Name };

        try
        {
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể mở bản đồ: {ex.Message}", "OK");
        }
    }

    private bool IsFavorite()
    {
        return _restaurant != null && FavoriteService.Contains(_restaurant);
    }

    private void UpdateFavoriteButtonText()
    {
        btnFavorite.Text = IsFavorite()
            ? LocalizationService.Instance["Unfavorite"]
            : LocalizationService.Instance["Favorites"];
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (_restaurant == null)
            return;

        var localization = LocalizationService.Instance;
        if (IsFavorite())
        {
            FavoriteService.Remove(_restaurant);
            UpdateFavoriteButtonText();
            await DisplayAlert(localization["Notice"], localization["RemovedFromFavorites"], localization["OK"]);
            return;
        }

        FavoriteService.Add(_restaurant);
        UpdateFavoriteButtonText();
        await DisplayAlert(localization["Notice"], localization["AddedToFavorites"], localization["OK"]);
    }

    // Thêm nút Back để hỗ trợ người dùng có thể quay lại tab yêu thích
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}