using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;
using System.IO;
using System.Net.Http;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp.Views;

public partial class MapPage : ContentPage
{
    private const string DefaultRestaurantImageUrl = "https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn";
    private const int TrackingIntervalMs = 2000;
    private IEnumerable<Poi>? restaurants;
    private Poi? selectedRestaurant;

    private AudioService audioService = new AudioService();
    private GeofenceService geofenceService = new GeofenceService();
    private string? _lastAutoNarratedPoiKey;
    private bool _isAutoNarrating;

    public MapPage()
    {
        InitializeComponent();
        BindingContext = LocalizationService.Instance;
        LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
    }

    void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LocalizationService.CurrentLanguage) && e.PropertyName != "Item") return;
        if (selectedRestaurant == null) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await UpdateSelectedRestaurantTextAsync(selectedRestaurant);
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (status == PermissionStatus.Granted)
        {
            map.IsShowingUser = true;
        }

        MainThread.BeginInvokeOnMainThread(() => 
        {
            try { ShowVinhKhanh(); } catch { } 
        });

        // Khởi tạo ApiService lấy dữ liệu thật
        var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
        var apiService = new PoiApiService(dbContext);
        var apiRestaurants = await apiService.GetAllPOIsAsync();

        if (apiRestaurants != null && apiRestaurants.Any())
        {
            restaurants = apiRestaurants;

            // Đợi UI map render hoàn tất trước khi thao tác ghim Pin
            await Task.Delay(1500);

            MainThread.BeginInvokeOnMainThread(() => 
            {
                map.Pins.Clear();
                LoadRestaurants();
            });
        }
        else
        {
            var loc = LocalizationService.Instance;
            MainThread.BeginInvokeOnMainThread(() => _ = DisplayAlert(loc["Notice"], loc["ConnectionError"], loc["OK"]));
        }

        _ = StartTracking();
    }

    // 📍 Hiển thị khu Vĩnh Khánh
    void ShowVinhKhanh()
    {
        var location = new Location(10.7607, 106.7029); // Phố ẩm thực Vĩnh Khánh
        map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(1000)));
    }

    // 🍜 Load quán ăn lên map
    void LoadRestaurants()
    {
        if (restaurants == null || !restaurants.Any())
        {
            DisplayAlert("Thông báo", "Không có dữ liệu quán ăn nào.", "OK");
            return;
        }

        foreach (var r in restaurants)
        {
            if (r.Poiid == -1) // Bỏ qua pin lỗi API
            {
                var loc = LocalizationService.Instance;
                MainThread.BeginInvokeOnMainThread(() => _ = DisplayAlert(loc["ApiError"], $"{loc["BackendErrorMsg"]}{r.Description}", loc["OK"]));

                // Hiển thị luôn pin báo lỗi lên bản đồ cho dễ nhận diện
                var errPin = new Pin
                {
                    Label = loc["ApiError"],
                    Address = r.Description ?? loc["BackendErrorMsg"],
                    Type = PinType.Generic,
                    Location = new Location(10.7607, 106.7029)
                };
                map.Pins.Add(errPin);
                continue;
            }

            if (r.Latitude == null || r.Longitude == null || r.Latitude == 0 || r.Longitude == 0) continue;

            var pin = new Pin
            {
                Label = string.IsNullOrWhiteSpace(r.Name) ? "Chưa có tên" : r.Name,
                Address = string.IsNullOrWhiteSpace(r.Description) ? "Chưa có mô tả" : r.Description,
                Type = PinType.Place,
                Location = new Location(r.Latitude.Value, r.Longitude.Value)
            };

            pin.MarkerClicked += (s, e) =>
            {
                e.HideInfoWindow = true;
                ShowDetail(r);
            };

            map.Pins.Add(pin);
        }
    }

    // 📌 Hiển thị chi tiết quán
    async void ShowDetail(Poi r)
    {
        // Khi người dùng nhấn vào pin trên bản đồ, mở trang chi tiết giống như khi quét QR
        try
        {
            await Navigation.PushAsync(new RestaurantDetailPage(r));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowDetail] Navigation failed: {ex.Message}");
            // Fallback: hiển thị panel nhỏ nếu không thể điều hướng
            selectedRestaurant = r;
            await UpdateSelectedRestaurantTextAsync(r);
            await SetRestaurantImageAsync(r);
            detailPanel.IsVisible = true;
        }
    }

    async Task UpdateSelectedRestaurantTextAsync(Poi r)
    {
        var lang = LocalizationService.Instance.CurrentLanguage;

        var name = r.Name ?? string.Empty;
        var description = r.Description ?? string.Empty;

        if (lang != "vi")
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = await TTSHelper.TranslateTextAsync(name, lang);
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                description = await TTSHelper.TranslateTextAsync(description, lang);
            }
        }

        var selectedKey = selectedRestaurant?.ApiId?.ToString() ?? selectedRestaurant?.Id ?? selectedRestaurant?.Poiid.ToString();
        var targetKey = r.ApiId?.ToString() ?? r.Id ?? r.Poiid.ToString();
        if (!string.Equals(selectedKey, targetKey, StringComparison.Ordinal))
        {
            return;
        }

        nameLabel.Text = name;
        descLabel.Text = description;
    }

    private async Task SetRestaurantImageAsync(Poi r)
    {
        restaurantImage.IsVisible = true;
        restaurantImage.Source = ImageSource.FromUri(new Uri(DefaultRestaurantImageUrl));

        var imageUrl = !string.IsNullOrWhiteSpace(r.ImagePath) ? r.ImagePath : r.Thumbnail;
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var fullImageUrl = ResolveImageUrl(imageUrl);
        // Try to download image like in catalog to ensure it shows on devices with dev tunnels / SSL issues
        var resolvedSource = await ResolveImageSourceAsync(r);
        if (resolvedSource != null)
        {
            restaurantImage.Source = resolvedSource;
            restaurantImage.IsVisible = true;
            return;
        }

        await LoadImageWithBypassAsync(fullImageUrl);
    }

    private static async Task<ImageSource?> ResolveImageSourceAsync(Poi poi)
    {
        try
        {
            var imageUrl = !string.IsNullOrWhiteSpace(poi.ImagePath) ? poi.ImagePath : poi.Thumbnail;
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            var resolved = ResolveImageUrl(imageUrl);
            System.Diagnostics.Debug.WriteLine($"[MapPage.ResolveImageSourceAsync] resolved={resolved}");

            if (Uri.TryCreate(resolved, UriKind.Absolute, out var uri))
            {
                try
                {
                    var handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (m, cert, chain, errors) => true;
                    using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                    client.DefaultRequestHeaders.Add("X-DevTunnels-Skip-Anti-Phishing-Page", "true");
                    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

                    var resp = await client.GetAsync(uri);
                    if (resp.IsSuccessStatusCode)
                    {
                        var bytes = await resp.Content.ReadAsByteArrayAsync();
                        return ImageSource.FromStream(() => new MemoryStream(bytes));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MapPage.ResolveImageSourceAsync] download failed: {ex.Message}");
                }

                return ImageSource.FromUri(uri);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage.ResolveImageSourceAsync] error: {ex.Message}");
        }

        return null;
    }

    private async Task LoadImageWithBypassAsync(string fullImageUrl)
    {
        try
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            client.DefaultRequestHeaders.Add("X-DevTunnels-Skip-Anti-Phishing-Page", "true");
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TourismApp/1.0");

            var response = await client.GetAsync(fullImageUrl);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                restaurantImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                restaurantImage.IsVisible = true;
            });
        }
        catch
        {
            // giữ ảnh mặc định
        }
    }

    static string ResolveImageUrl(string rawImageUrl)
    {
        System.Diagnostics.Debug.WriteLine($"[MapPage.ResolveImageUrl] rawImageUrl={rawImageUrl}");
        var imageUrl = rawImageUrl.Trim().Replace("~/", string.Empty);
        if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return imageUrl;
        }

        var apiBaseUrl = Preferences.Get("api_base_url", string.Empty);
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            apiBaseUrl = "https://nqrwpkxp-5219.asse.devtunnels.ms/api/";
        }

        var baseUrl = apiBaseUrl.Trim();
        if (baseUrl.EndsWith("/api/", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^4];
        else if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^3];

        return $"{baseUrl.TrimEnd('/')}/{imageUrl.TrimStart('/')}";
    }

    // ❤️ Thêm vào yêu thích
    void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (selectedRestaurant != null)
        {
            FavoriteService.Add(selectedRestaurant);
            var loc = LocalizationService.Instance;
            MainThread.BeginInvokeOnMainThread(() => _ = DisplayAlert(loc["Notice"], loc["AddedToFavorites"], loc["OK"]));
        }
    }

    // ❌ Đóng panel
    void OnCloseClicked(object sender, EventArgs e)
    {
        detailPanel.IsVisible = false;
    }

    // Thay đổi ngôn ngữ từ Picker trong panel chi tiết
    void OnLanguageChanged(object sender, EventArgs e)
    {
        try
        {
            if (sender is Picker p && p.SelectedIndex >= 0)
            {
                var lang = p.Items[p.SelectedIndex];
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    LocalizationService.Instance.CurrentLanguage = lang;
                }
            }
        }
        catch { }
    }

    // 👉 NÚT "NGHE"
    async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        if (selectedRestaurant == null) return;

        string lang = LocalizationService.Instance.CurrentLanguage;
        var name = string.IsNullOrWhiteSpace(selectedRestaurant.Name) ? "Quán ăn" : selectedRestaurant.Name;
        var desc = string.IsNullOrWhiteSpace(selectedRestaurant.Description) ? string.Empty : $". {selectedRestaurant.Description}";
        string textToSpeak = $"{name}{desc}";

        if (lang != "vi" && !string.IsNullOrWhiteSpace(textToSpeak))
        {
            textToSpeak = await TTSHelper.TranslateTextAsync(textToSpeak, lang);
        }

        _ = audioService.Speak(textToSpeak);
    }

    // 👉 NÚT CHỈ ĐƯỜNG
    async void OnDirectionsClicked(object sender, EventArgs e)
    {
        if (selectedRestaurant?.Latitude == null || selectedRestaurant?.Longitude == null) return;

        var location = new Location(selectedRestaurant.Latitude.Value, selectedRestaurant.Longitude.Value);
        var options = new MapLaunchOptions { Name = selectedRestaurant.Name };

        try
        {
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc["Notice"], $"{loc["ConnectionError"]}: {ex.Message}", loc["OK"]);
        }
    }

    async Task StartTracking()
    {
        while (true)
        {
            try
            {
                var location = await GetCurrentLocationFastAsync();

                if (location != null)
                    await CheckNearbyAsync(location);
            }
            catch { }

            await Task.Delay(200);
        }
    }

    async Task<Location?> GetCurrentLocationFastAsync()
    {
        try
        {
            return await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromMilliseconds(TrackingIntervalMs)));
        }
        catch
        {
            return await Geolocation.GetLastKnownLocationAsync();
        }
    }

    async Task CheckNearbyAsync(Location user)
    {
        if (restaurants == null || !restaurants.Any())
        {
            return;
        }

        const double autoNarrationRadiusMeters = 100;

        var nearest = restaurants
            .Where(r => r.Latitude.HasValue && r.Longitude.HasValue)
            .Select(r => new
            {
                Poi = r,
                Distance = geofenceService.GetDistance(
                    user.Latitude,
                    user.Longitude,
                    r.Latitude!.Value,
                    r.Longitude!.Value)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearest?.Poi == null || nearest.Distance > autoNarrationRadiusMeters)
        {
            _lastAutoNarratedPoiKey = null;
            return;
        }

        var poi = nearest.Poi;
        var poiKey = poi.ApiId?.ToString() ?? poi.Id ?? poi.Poiid.ToString();
        if (string.Equals(_lastAutoNarratedPoiKey, poiKey, StringComparison.Ordinal))
        {
            return;
        }

        if (_isAutoNarrating)
        {
            return;
        }

        _isAutoNarrating = true;
        _lastAutoNarratedPoiKey = poiKey;

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (poi.Latitude.HasValue && poi.Longitude.HasValue)
                {
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(
                        new Location(poi.Latitude.Value, poi.Longitude.Value),
                        Distance.FromMeters(400)));
                }

                ShowDetail(poi);
            });

            var poiName = string.IsNullOrWhiteSpace(poi.Name) ? "này" : poi.Name;
            var intro = $"Bạn đang đến gần quán {poiName}.";
            var description = string.IsNullOrWhiteSpace(poi.Description) ? string.Empty : $" {poi.Description}";
            string textToSpeak = $"{intro}{description}".Trim();

            var lang = LocalizationService.Instance.CurrentLanguage;
            if (lang != "vi" && !string.IsNullOrWhiteSpace(textToSpeak))
            {
                textToSpeak = await TTSHelper.TranslateTextAsync(textToSpeak, lang);
            }

            if (!string.IsNullOrWhiteSpace(textToSpeak))
            {
                await audioService.Speak(textToSpeak);
            }
        }
        finally
        {
            _isAutoNarrating = false;
        }
    }

    void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string query = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            searchResultsBorder.IsVisible = false;
            searchResults.ItemsSource = null;
        }
        else
        {
            var filtered = restaurants?.Where(r => r.Name != null && r.Name.ToLower().Contains(query)).ToList();
            if (filtered != null && filtered.Any())
            {
                searchResults.ItemsSource = filtered;
                searchResultsBorder.IsVisible = true;
            }
            else
            {
                searchResultsBorder.IsVisible = false;
            }
        }
    }

    void OnSearchButtonPressed(object sender, EventArgs e)
    {
        searchBar.Unfocus();
        searchResultsBorder.IsVisible = false;

        string query = searchBar.Text?.ToLower() ?? "";
        var firstMatch = restaurants?.FirstOrDefault(r => r.Name != null && r.Name.ToLower().Contains(query));

        if (firstMatch != null && firstMatch.Latitude != null && firstMatch.Longitude != null)
        {
            map.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(firstMatch.Latitude.Value, firstMatch.Longitude.Value), 
                Distance.FromMeters(500)));
            ShowDetail(firstMatch);
        }
    }

    void OnSearchResultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Poi selectedPoi)
        {
            searchBar.Text = selectedPoi.Name;
            searchBar.Unfocus();
            searchResultsBorder.IsVisible = false;
            searchResults.SelectedItem = null; // Clear selection

            if (selectedPoi.Latitude != null && selectedPoi.Longitude != null)
            {
                map.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(selectedPoi.Latitude.Value, selectedPoi.Longitude.Value), 
                    Distance.FromMeters(500)));
                ShowDetail(selectedPoi);
            }
        }
    }

    async void OnRefreshGpsClicked(object sender, EventArgs e)
    {
        var location = await GetCurrentLocationFastAsync();
        if (location != null)
        {
            await CheckNearbyAsync(location);
        }
    }
}