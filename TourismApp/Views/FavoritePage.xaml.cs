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
}