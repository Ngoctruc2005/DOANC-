using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;

using TourismApp.Models;

namespace TourismApp.Views;

public partial class MapPage : ContentPage
{
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

    // 📍 Hiển thị khu Vĩnh Khánh Q4
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

    // 🍜 Hiển thị quán ăn
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

            // ✅ SỬA Ở ĐÂY: KHÔNG chuyển trang nữa
            pin.MarkerClicked += (s, e) =>
            {
                ShowDetail(r);
            };

            map.Pins.Add(pin);
        }
    }

    // 📌 Hiển thị panel chi tiết
    void ShowDetail(Restaurant r)
    {
        nameLabel.Text = r.Name;
        descLabel.Text = r.Description;
        bestSellerLabel.Text = "Best: " + r.BestSeller;

        detailPanel.IsVisible = true;
    }

    // ❌ Đóng panel
    void OnCloseClicked(object sender, EventArgs e)
    {
        detailPanel.IsVisible = false;
    }
}