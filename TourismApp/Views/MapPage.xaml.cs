using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;

using TourismApp.Models;
using TourismApp.Views;

namespace TourismApp.Views;

public partial class MapPage : ContentPage
{
    List<Restaurant> restaurants = new()
    {
        new Restaurant
        {
            Name = "Phở Hòa Pasteur",
            Description = "Quán phở nổi tiếng lâu đời",
            Latitude = 10.7814,
            Longitude = 106.6919,

            BestSeller = "Phở tái nạm",

            Menu = new List<string>
            {
                "Phở tái",
                "Phở tái nạm",
                "Phở bò viên",
                "Phở gân"
            }
        },

        new Restaurant
        {
            Name = "Pizza 4P's",
            Description = "Nhà hàng pizza nổi tiếng",
            Latitude = 10.7767,
            Longitude = 106.7030,

            BestSeller = "Pizza Burrata",

            Menu = new List<string>
            {
                "Pizza Burrata",
                "Pizza Salmon",
                "Spaghetti",
                "Lasagna"
            }
        }
    };

    public MapPage()
    {
        InitializeComponent();

        LoadLocation();

        LoadRestaurants();
    }

    async void LoadLocation()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
        {
            ShowHCM();
            return;
        }

        try
        {
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(10)
                )
            );

            if (location != null)
            {
                var userLocation = new Location(
                    location.Latitude,
                    location.Longitude
                );

                map.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        userLocation,
                        Distance.FromKilometers(1)
                    )
                );
            }
            else
            {
                ShowHCM();
            }
        }
        catch
        {
            ShowHCM();
        }
    }

    void ShowHCM()
    {
        var hcm = new Location(10.7769, 106.7009);

        map.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                hcm,
                Distance.FromKilometers(3)
            )
        );
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

            pin.MarkerClicked += async (s, e) =>
            {
                await Navigation.PushAsync(
                    new RestaurantDetailPage(r)
                );
            };

            map.Pins.Add(pin);
        }
    }
}