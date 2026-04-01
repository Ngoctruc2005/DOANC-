using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;

using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp.Views;

public partial class MapPage : ContentPage
{
    // 👉 lưu quán đang chọn
    Restaurant selectedRestaurant;
    Restaurant selectedRestaurant;
    Restaurant selectedRestaurant;
    Restaurant selectedRestaurant;
    Restaurant selectedRestaurant;
    Restaurant selectedRestaurant;

    List<Restaurant> restaurants = new()
    {
        new Restaurant
        {
            Name = "Ốc Oanh",
            Description = "Ốc nổi tiếng Vĩnh Khánh",
            Latitude = 10.7578,
            Longitude = 106.7039,
            BestSeller = "Ốc len xào dừa",
            Menu = new List<string>
            {
                "Ốc len xào dừa",
                "Ốc hương rang muối",
                "Sò điệp nướng"
            }
        },

        new Restaurant
        {
            Name = "Bún đậu A Chảnh",
            Description = "Bún đậu mắm tôm",
            Latitude = 10.7569,
            Longitude = 106.7045,
            BestSeller = "Bún đậu đầy đủ",
            Menu = new List<string>
            {
                "Bún đậu",
                "Chả cốm",
                "Nem rán"
            }
        },

        new Restaurant
        {
            Name = "Phá lấu bò",
            Description = "Phá lấu đậm đà",
            Latitude = 10.7572,
            Longitude = 106.7042,
            BestSeller = "Phá lấu bánh mì",
            Menu = new List<string>
            {
                "Phá lấu",
                "Mì phá lấu"
            }
        }
    };

    public MapPage()
    {
        InitializeComponent();

        ShowVinhKhanh();
        LoadRestaurants();
    }

    // 📍 Hiển thị khu Vĩnh Khánh
    void ShowVinhKhanh()
    {
        var location = new Location(10.7575, 106.7040);

        map.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                location,
                Distance.FromMeters(500)
            )
        );
    }

    // 🍜 Load quán ăn lên map
    void LoadRestaurants()
    {
        foreach (var r in restaurants)
        {
            var pin = new Pin
            {
                Label = r.Name,
                Address = r.Description,
                Type = PinType.Place,
                Location = new Location(r.Latitude, r.Longitude)
            };

            pin.MarkerClicked += (s, e) =>
            {
                ShowDetail(r);
            };

            map.Pins.Add(pin);
        }
    }

    // 📌 Hiển thị chi tiết quán
    void ShowDetail(Restaurant r)
    {
        selectedRestaurant = r; // 🔥 lưu lại quán đang chọn

        nameLabel.Text = r.Name;
        descLabel.Text = r.Description;
        bestSellerLabel.Text = "Best: " + r.BestSeller;

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

        if (selectedRestaurant.AudioDescription.TryGetValue(lang, out var text))
        {
            await audioService.Speak(text);
        }
        else
        {
            await audioService.Speak(selectedRestaurant.Description);
        }

        languagePicker.IsVisible = false;
    }

    async void StartTracking()
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
        foreach (var r in restaurants)
        {
            double distance = geofenceService.GetDistance(
                user.Latitude, user.Longitude,
                r.Latitude, r.Longitude);

            if (distance <= 50 && !triggered.Contains(r.Name))
            {
                triggered.Add(r.Name);

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var lang = languagePicker.SelectedItem?.ToString() ?? "vi";

                    if (r.AudioDescription.TryGetValue(lang, out var text))
                        await audioService.Speak($"Bạn đang đến {r.Name}. {text}");
                    else
                        await audioService.Speak($"Bạn đang đến {r.Name}");
                });
            }
        }
    }
}