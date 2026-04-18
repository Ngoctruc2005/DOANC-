using Microsoft.Extensions.DependencyInjection;
using TourismApp.Services;

namespace TourismApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        // Register device on app start so installs / cold-starts (APK launches) are counted
        _ = DeviceRegistrationService.RegisterDeviceAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}