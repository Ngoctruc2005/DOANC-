using System.Xml;
using TourismApp.Models;

namespace TourismApp.Views;

public partial class RestaurantDetailPage : ContentPage
{
    public RestaurantDetailPage(Restaurant restaurant)
    {
        InitializeComponent();

        nameLabel.Text = restaurant.Name;

        descriptionLabel.Text = restaurant.Description;

        bestSellerLabel.Text = restaurant.BestSeller;

        menuList.ItemsSource = restaurant.Menu;
    }
}