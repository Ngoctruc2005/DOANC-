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
        Description = "Quán ốc nổi tiếng trên đường Vĩnh Khánh",
        Latitude = 10.7578,
        Longitude = 106.7039,
        BestSeller = "Ốc len xào dừa",
        Menu = new List<string>{"Ốc len xào dừa","Ốc hương","Sò điệp"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Ốc Oanh là quán ốc nổi tiếng và lâu đời tại Vĩnh Khánh."},
            {"en","Oc Oanh is a famous long-standing seafood restaurant on Vinh Khanh street."}
        }
    },

    new Restaurant
    {
        Name = "Ốc Thảo",
        Description = "Quán ốc bình dân",
        Latitude = 10.7580,
        Longitude = 106.7042,
        BestSeller = "Ốc hương xào bơ",
        Menu = new List<string>{"Ốc hương","Nghêu hấp","Sò lông"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Ốc Thảo phục vụ các món ốc giá bình dân."},
            {"en","Oc Thao offers affordable seafood dishes."}
        }
    },

    new Restaurant
    {
        Name = "Ốc Vũ",
        Description = "Quán ốc đông khách buổi tối",
        Latitude = 10.7576,
        Longitude = 106.7045,
        BestSeller = "Ốc móng tay xào me",
        Menu = new List<string>{"Ốc móng tay","Ốc len","Càng ghẹ"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Ốc Vũ là điểm đến quen thuộc vào buổi tối."},
            {"en","Oc Vu is a popular evening seafood spot."}
        }
    },

    new Restaurant
    {
        Name = "Ốc Cúc",
        Description = "Quán ốc nhỏ nhưng nổi tiếng",
        Latitude = 10.7574,
        Longitude = 106.7041,
        BestSeller = "Ốc xào bơ",
        Menu = new List<string>{"Ốc bươu","Ốc mỡ","Nghêu"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Ốc Cúc là quán nhỏ nhưng rất được yêu thích."},
            {"en","Oc Cuc is a small but beloved seafood place."}
        }
    },

    new Restaurant
    {
        Name = "Ốc Hoa Kiều",
        Description = "Quán ốc phong cách Trung",
        Latitude = 10.7583,
        Longitude = 106.7037,
        BestSeller = "Ốc xào tỏi",
        Menu = new List<string>{"Ốc xào","Cua","Tôm"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Ốc Hoa Kiều có phong cách chế biến đặc trưng."},
            {"en","Oc Hoa Kieu features a unique cooking style."}
        }
    },

    new Restaurant
    {
        Name = "Quán Lãng",
        Description = "Quán ăn gia đình",
        Latitude = 10.7586,
        Longitude = 106.7035,
        BestSeller = "Cơm tấm",
        Menu = new List<string>{"Cơm tấm","Cá kho","Canh chua"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Quán Lãng là quán ăn gia đình gần Vĩnh Khánh."},
            {"en","Lang is a family restaurant near Vinh Khanh."}
        }
    },

    new Restaurant
    {
        Name = "Chilli BBQ & Hotpot",
        Description = "BBQ & lẩu",
        Latitude = 10.7582,
        Longitude = 106.7048,
        BestSeller = "Lẩu thái",
        Menu = new List<string>{"Lẩu","BBQ","Hải sản"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Chilli BBQ nổi tiếng với lẩu và nướng."},
            {"en","Chilli BBQ is known for hotpot and grilled food."}
        }
    },

    new Restaurant
    {
        Name = "Bún đậu A Chảnh",
        Description = "Bún đậu mắm tôm",
        Latitude = 10.7569,
        Longitude = 106.7045,
        BestSeller = "Bún đậu đầy đủ",
        Menu = new List<string>{"Bún đậu","Nem rán","Chả cốm"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Bún đậu A Chảnh rất nổi tiếng tại khu này."},
            {"en","Bun Dau A Chanh is a popular Vietnamese dish spot."}
        }
    },

    new Restaurant
    {
        Name = "Phá Lấu Bò",
        Description = "Món phá lấu truyền thống",
        Latitude = 10.7572,
        Longitude = 106.7042,
        BestSeller = "Phá lấu bánh mì",
        Menu = new List<string>{"Phá lấu","Mì phá lấu"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Phá lấu bò là món đặc trưng ở Vĩnh Khánh."},
            {"en","Beef Pha Lau is a signature dish here."}
        }
    },

    new Restaurant
    {
        Name = "Vinh Khanh Food Street",
        Description = "Khu ẩm thực Vĩnh Khánh",
        Latitude = 10.7575,
        Longitude = 106.7040,
        BestSeller = "Nhiều món ốc",
        Menu = new List<string>{"Ốc","Hải sản","BBQ"},
        AudioDescription = new Dictionary<string,string>
        {
            {"vi","Bạn đang ở khu ẩm thực nổi tiếng Vĩnh Khánh."},
            {"en","You are at the famous Vinh Khanh food street."}
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

        // ẩn picker khi mở lại quán mới
        languagePicker.IsVisible = false;
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