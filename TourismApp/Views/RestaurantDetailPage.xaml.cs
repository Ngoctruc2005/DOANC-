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

    public RestaurantDetailPage(Poi restaurant)
    {
        InitializeComponent();
        BindingContext = LocalizationService.Instance;
        _restaurant = restaurant;

        nameLabel.Text = _restaurant.Name;
        descriptionLabel.Text = _restaurant.Description;
    }

    private async void LoadMenu()
    {
        try
        {
            var dbContext = this.Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
            var apiService = new PoiApiService(dbContext);
            var menus = await apiService.GetMenusForPoiAsync(_restaurant.Id);

            if (menus != null && menus.Any())
            {
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    menuList.ItemsSource = menus;
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    menuList.ItemsSource = new List<Menu>();
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lỗi tải Menu] {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadMenu(); // Tải menu khi trang hiển thị để đảm bảo Handler không null
        var lang = Microsoft.Maui.Storage.Preferences.Get("language", "vi");
        if (lang != "vi")
        {
            descriptionLabel.Text = LocalizationService.Instance["Loading"];
            string translatedDesc = await TTSHelper.TranslateTextAsync(_restaurant.Description, lang);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                descriptionLabel.Text = translatedDesc;
            });
        }
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        await TTSHelper.SpeakDescriptionAsync(_restaurant.Description);
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
}
