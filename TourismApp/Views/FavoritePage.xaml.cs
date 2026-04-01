using TourismApp.Services;

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
        list.ItemsSource = null;
        list.ItemsSource = FavoriteService.GetAll();
    }
}