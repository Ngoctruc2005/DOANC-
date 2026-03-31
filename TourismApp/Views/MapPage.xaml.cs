using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;

using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp.Views;

public partial class MapPage : ContentPage
{
    private IEnumerable<Poi>? restaurants;

    public MapPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Di chuyển bản đồ đến Phố ẩm thực Vĩnh Khánh, Quận 4, TP.HCM
            var vinhKhanhLocation = new Location(10.7607, 106.7029);
            var mapSpan = MapSpan.FromCenterAndRadius(vinhKhanhLocation, Distance.FromKilometers(1));

            // Cần đảm bảo UI đã load xong trước khi move map
            MainThread.BeginInvokeOnMainThread(() => map.MoveToRegion(mapSpan));

            var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
            var apiService = new PoiApiService(dbContext);
            var apiRestaurants = await apiService.GetAllPOIsAsync();

            if (apiRestaurants != null && apiRestaurants.Any())
            {
                restaurants = apiRestaurants; // Gán dữ liệu sql
                // Cập nhật UI trên Main Thread
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    map.Pins.Clear();
                    LoadRestaurants(); // Hàm vẽ pin lên Map
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lỗi MapPage] {ex.Message}");
        }
    }

    private void LoadRestaurants()
    {
        if (restaurants == null) return;

        foreach (var r in restaurants)
        {
            var pin = new Pin
            {
                Label = r.Name,
                Address = r.Description,
                Type = PinType.Place,
                Location = new Location(r.Latitude, r.Longitude)
            };

            pin.MarkerClicked += (s, args) =>
            {
                args.HideInfoWindow = true;

                nameLabel.Text = r.Name;
                descLabel.Text = r.Description;
                bestSellerLabel.Text = "Món nổi bật: " + (string.IsNullOrEmpty(r.BestSeller) ? "Đang cập nhật" : r.BestSeller);

                detailPanel.IsVisible = true;
            };

            map.Pins.Add(pin);
        }
    }

    private void OnFavoriteClicked(object sender, EventArgs e)
    {
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        detailPanel.IsVisible = false;
    }

    private void OnPlayAudio(object sender, EventArgs e)
    {
    }
}