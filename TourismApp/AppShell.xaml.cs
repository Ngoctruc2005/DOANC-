using TourismApp.Services;

namespace TourismApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            BindingContext = LocalizationService.Instance;
        }
    }
}
