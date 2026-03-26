using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;

using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp.Views;

public partial class MapPage : ContentPage
{
    Restaurant selectedRestaurant;

    LocationService locationService = new();
    GeofenceService geofenceService = new();
    AudioService audioService = new();

    HashSet<string> triggered = new();

    List<Restaurant> restaurants = new()
    {
        new Restaurant
        {
            Name = "Ốc Oanh",
            Description = "Ốc nổi tiếng Vĩnh Khánh",
            Latitude = 10.7578,
            Longitude = 106.7039,
            BestSeller = "Ốc len xào dừa",
            Menu = new List<string>{"Ốc len xào dừa","Ốc hương rang muối","Sò điệp nướng"},
            AudioDescription = new Dictionary<string,string>
            {
                {"vi","Ốc Oanh, Ốc nổi tiếng Vĩnh Khánh. Món nổi bật: Ốc len xào dừa"},
                {"en","Oc Oanh, famous in Vinh Khanh. Best seller: Coconut stir-fried snails"}
            }
        },
        new Restaurant
        {
            Name = "Bún đậu A Chảnh",
            Description = "Bún đậu mắm tôm",
            Latitude = 10.7569,
            Longitude = 106.7045,
            BestSeller = "Bún đậu đầy đủ",
            Menu = new List<string>{"Bún đậu","Chả cốm","Nem rán"},
            AudioDescription = new Dictionary<string,string>
            {
                {"vi","Bún đậu A Chảnh, Bún đậu mắm tôm. Món nổi bật: Bún đậu đầy đủ"},
                {"en","Bun Dau A Chanh, fermented shrimp paste bun. Best seller: Full Bun Dau set"}
            }
        },
        new Restaurant
        {
            Name = "Phá lấu bò",
            Description = "Phá lấu đậm đà",
            Latitude = 10.7572,
            Longitude = 106.7042,
            BestSeller = "Phá lấu bánh mì",
            Menu = new List<string>{"Phá lấu","Mì phá lấu"},
            AudioDescription = new Dictionary<string,string>
            {
                {"vi","Phá lấu bò, Phá lấu đậm đà. Món nổi bật: Phá lấu bánh mì"},
                {"en","Beef Pha Lau, flavorful stew. Best seller: Pha Lau with bread"}
            }
        }
    };

    public MapPage()
    {
        InitializeComponent();

        ShowVinhKhanh();
        LoadRestaurants();

        StartTracking();
    }

    void ShowVinhKhanh()
    {
        var location = new Location(10.7575, 106.7040);
        map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(500)));
    }

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

            pin.MarkerClicked += (s, e) => ShowDetail(r);
            map.Pins.Add(pin);
        }
    }

    void ShowDetail(Restaurant r)
    {
        selectedRestaurant = r;

        nameLabel.Text = r.Name;
        descLabel.Text = r.Description;
        bestSellerLabel.Text = "Best: " + r.BestSeller;
        menuList.ItemsSource = r.Menu;

        detailPanel.IsVisible = true;
    }

    void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (selectedRestaurant != null)
        {
            FavoriteService.Add(selectedRestaurant);
            DisplayAlert("Thông báo", "Đã thêm vào yêu thích ❤️", "OK");
        }
    }

    void OnCloseClicked(object sender, EventArgs e)
    {
        detailPanel.IsVisible = false;
    }

    async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        if (selectedRestaurant != null)
        {
            var lang = languagePicker.SelectedItem?.ToString() ?? "vi";
            if (selectedRestaurant.AudioDescription.TryGetValue(lang, out var text))
            {
                await audioService.Speak(text);
            }
            else
            {
                await audioService.Speak(selectedRestaurant.Description);
            }
        }
    }

    async void StartTracking()
    {
        while (true)
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
                if (location != null) CheckNearby(location);
            }
            catch { }
            await Task.Delay(5000);
        }
    }

    void CheckNearby(Location user)
    {
        foreach (var r in restaurants)
        {
            double distance = geofenceService.GetDistance(user.Latitude, user.Longitude, r.Latitude, r.Longitude);
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