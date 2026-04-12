using TourismApp.Services;
using TourismApp.Models;

namespace TourismApp.Views;

public partial class FavoritePage : ContentPage
{
    public FavoritePage()
    {
        InitializeComponent();
        BindingContext = LocalizationService.Instance;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 🔥 load lại dữ liệu mỗi lần mở tab
        RefreshList();
    }

    private void RefreshList()
    {
        list.ItemsSource = null;
        list.ItemsSource = FavoriteService.GetAll();
    }

    // 🗑️ Lệnh xóa quán khỏi danh sách yêu thích
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Poi restaurant)
        {
            bool confirm = await DisplayAlert("Xác nhận xóa", $"Bạn có chắc muốn xóa '{restaurant.Name}' khỏi danh sách yêu thích?", "Có", "Không");
            if (confirm)
            {
                FavoriteService.Remove(restaurant);
                RefreshList();
            }
        }
    }

    // ➡️ Lệnh bấm vào khung tên quán (Frame Tapped) để chuyển sang trang chi tiết
    private async void OnRestaurantTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is Poi selectedRestaurant)
        {
            // Chuyển hướng sang trang chi tiết quán
            await Navigation.PushAsync(new RestaurantDetailPage(selectedRestaurant));
        }
    }

    // Thumb loader for the CollectionView items: resolves ImagePath/Thumbnail, downloads bytes (bypass SSL/dev tunnels)
    private async void OnThumbBindingContextChanged(object sender, EventArgs e)
    {
        if (sender is Image img && img.BindingContext is Poi poi)
        {
            try
            {
                var imageUrl = !string.IsNullOrWhiteSpace(poi.ImagePath) ? poi.ImagePath : poi.Thumbnail;
                System.Diagnostics.Debug.WriteLine($"[FavoritePage] thumb imageUrl raw='{imageUrl}' for poi='{poi.Name}'");
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    img.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
                    return;
                }

                var resolved = RestaurantCatalogPage.ResolveImageUrl(imageUrl);
                System.Diagnostics.Debug.WriteLine($"[FavoritePage] resolved image url='{resolved}'");
                if (!Uri.TryCreate(resolved, UriKind.Absolute, out var uri))
                {
                    img.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
                    return;
                }

                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (m, cert, chain, errors) => true;
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Add("X-DevTunnels-Skip-Anti-Phishing-Page", "true");
                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var resp = await client.GetAsync(uri);
                System.Diagnostics.Debug.WriteLine($"[FavoritePage] HTTP GET {uri} => {(int)resp.StatusCode} {resp.ReasonPhrase}");
                if (resp.IsSuccessStatusCode)
                {
                    var bytes = await resp.Content.ReadAsByteArrayAsync();
                    img.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                else
                {
                    img.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
                }
            }
            catch
            {
                img.Source = ImageSource.FromUri(new Uri("https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn"));
            }
        }
    }
}