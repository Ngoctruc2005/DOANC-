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

        // Xử lý hình ảnh (ImagePath hoặc Thumbnail)
        string? imageUrl = !string.IsNullOrWhiteSpace(_restaurant.ImagePath) ? _restaurant.ImagePath : _restaurant.Thumbnail;

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                restaurantImage.Source = ImageSource.FromUri(new Uri(imageUrl));
            }
            else
            {
                // Nếu backend ném về tên file (VD: "uploads/image.jpg")
                // Ta nối với Base URL của server để lấy ảnh trực tiếp từ internet
                string baseUrl = "https://nqrwpkxp-5219.asse.devtunnels.ms/"; 

                // Chuẩn hóa chuỗi URL tránh bị lỗi dính 2 dấu gạch chéo
                string fullImageUrl = baseUrl.TrimEnd('/') + "/" + imageUrl.TrimStart('/');

                restaurantImage.Source = ImageSource.FromUri(new Uri(fullImageUrl));
            }
        }
        else
        {
            // Nếu API không trả về ảnh quán, sử dụng luôn ảnh mặc định từ Internet
            restaurantImage.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
        }
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

    private void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (_restaurant != null)
        {
            FavoriteService.Add(_restaurant);
            DisplayAlert("Thông báo", "Đã thêm vào yêu thích ❤️", "OK");
        }
    }

    // Thêm nút Back để hỗ trợ người dùng có thể quay lại tab yêu thích
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
