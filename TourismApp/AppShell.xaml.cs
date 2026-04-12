using TourismApp.Services;
using TourismApp.Views;
using System.ComponentModel;

namespace TourismApp
{
    public partial class AppShell : Shell
    {
        private ShellContent? _catalogShellContent;

        public AppShell()
        {
            InitializeComponent();
            BindingContext = LocalizationService.Instance;

            var loc = LocalizationService.Instance;
            var tabBar = Items.OfType<TabBar>().FirstOrDefault();
            if (tabBar != null)
            {
                _catalogShellContent = new ShellContent
                {
                    Title = loc["RestaurantCatalog"],
                    Icon = "home.png",
                    ContentTemplate = new DataTemplate(() => new RestaurantCatalogPage())
                };

                tabBar.Items.Insert(2, _catalogShellContent);
            }

            LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LocalizationService.CurrentLanguage) && e.PropertyName != "Item")
            {
                return;
            }

            if (_catalogShellContent != null)
            {
                _catalogShellContent.Title = LocalizationService.Instance["RestaurantCatalog"];
            }
        }
    }
}
