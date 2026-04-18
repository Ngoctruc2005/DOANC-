using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.Storage;
using Android.Content;
using System;
using System.Threading;
using System.Threading.Tasks;
using TourismApp.Services;

namespace TourismApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Register device immediately on resume
        protected override void OnResume()
        {
            base.OnResume();
            _ = DeviceRegistrationService.RegisterDeviceAsync();
        }

        // Unregister immediately on pause so CMS active list reflects only devices currently using the app
        protected override void OnPause()
        {
            base.OnPause();
            _ = DeviceRegistrationService.UnregisterDeviceAsync();
        }

        protected override void OnDestroy()
        {
            try
            {
                // Try to unregister when activity is destroyed
                _ = DeviceRegistrationService.UnregisterDeviceAsync();
            }
            catch { }

            base.OnDestroy();
        }
    }
}
