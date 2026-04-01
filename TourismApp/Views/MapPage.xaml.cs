using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp.Views;

public partial class MapPage : ContentPage
{
    private IEnumerable<Poi>? restaurants;
    private Poi? selectedRestaurant;

    private AudioService audioService = new AudioService();
    private GeofenceService geofenceService = new GeofenceService();
    private HashSet<string> triggered = new HashSet<string>();

    public MapPage()
    {
        InitializeComponent();
        BindingContext = LocalizationService.Instance;
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
            MainThread.BeginInvokeOnMainThread(() => 
            {
                DisplayAlert("Thông báo", "Không tải được danh sách quán ăn từ Database.", "OK");
            });
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
                DisplayAlert("Lỗi tải quán ăn", $"Có lỗi xảy ra: {r.Description}", "OK");

                // Hiển thị luôn pin báo lỗi lên bản đồ cho dễ nhận diện
                var errPin = new Pin
                {
                    Label = "Lỗi Kết Nối API",
                    Address = r.Description ?? "Không rõ lỗi",
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
        selectedRestaurant = r; // 🔥 lưu lại quán đang chọn

        nameLabel.Text = r.Name;
        descLabel.Text = r.Description;
        bestSellerLabel.Text = "Best: " + (r.BestSeller ?? "Đang cập nhật");

        // Tải Menu thực tế từ CSDL
        var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
        var apiService = new PoiApiService(dbContext);
        var menus = await apiService.GetMenusForPoiAsync(r.Id);

        if (menus != null && menus.Any())
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                menuList.ItemsSource = menus.Select(m => $"{m.ItemName} - {m.Price:N0} đ").ToList();
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                menuList.ItemsSource = new List<string> { "Đang cập nhật..." };
            });
        }

        detailPanel.IsVisible = true;
    }

    // ❤️ Thêm vào yêu thích
    void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (selectedRestaurant != null)
        {
            FavoriteService.Add(selectedRestaurant);
            DisplayAlert("Thông báo", "Đã thêm vào yêu thích ❤️", "OK");
        }
    }

    // ❌ Đóng panel
    void OnCloseClicked(object sender, EventArgs e)
    {
        detailPanel.IsVisible = false;
        languagePicker.IsVisible = false;
    }

    // 👉 NÚT "NGHE"
    void OnPlayAudioClicked(object sender, EventArgs e)
    {
        languagePicker.IsVisible = true;
    }

    // 👉 CHỌN NGÔN NGỮ → PHÁT AUDIO
    async void OnLanguageChanged(object sender, EventArgs e)
    {
        if (selectedRestaurant == null) return;

        var lang = languagePicker.SelectedItem?.ToString() ?? "vi";

        string textToSpeak = selectedRestaurant.Description;
        if (lang != "vi")
        {
            textToSpeak = await TTSHelper.TranslateTextAsync(selectedRestaurant.Description, lang);
        }

        await audioService.Speak(textToSpeak);
        languagePicker.IsVisible = false;
    }

    async Task StartTracking()
    {
        while (true)
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.High));

                if (location != null)
                    CheckNearby(location);
            }
            catch { }

            await Task.Delay(5000);
        }
    }

    void CheckNearby(Location user)
    {
        if (restaurants == null) return;

        foreach (var r in restaurants)
        {
            if (r.Latitude == null || r.Longitude == null) continue;

            double distance = geofenceService.GetDistance(
                user.Latitude, user.Longitude,
                r.Latitude.Value, r.Longitude.Value);

            if (distance <= 50 && r.Name != null && !triggered.Contains(r.Name))
            {
                triggered.Add(r.Name);

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await audioService.Speak($"Bạn đang đến {r.Name}");
                });
            }
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
}