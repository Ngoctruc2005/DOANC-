using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace TourismApp.Services;

public static class DeviceRegistrationService
{
    public static async Task RegisterDeviceAsync()
    {
        try
        {
            var apiBase = Preferences.Get("api_base_url", string.Empty);
            if (string.IsNullOrWhiteSpace(apiBase))
            {
                // If running on Android emulator, use 10.0.2.2 to reach host machine's localhost
                if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType != DeviceType.Physical)
                {
                    apiBase = "http://10.0.2.2:5219/api/";
                }
                else
                {
                    apiBase = "http://192.168.1.176:5219/api/";
                }
            }

            if (!apiBase.EndsWith("/")) apiBase += "/";
            if (!apiBase.EndsWith("api/", StringComparison.OrdinalIgnoreCase)) apiBase += "api/";

            var url = apiBase + "pois/device/enter";

            // ensure persistent uuid per app install
            var uuid = Preferences.Get("device_uuid", string.Empty);
            if (string.IsNullOrWhiteSpace(uuid))
            {
                uuid = Guid.NewGuid().ToString();
                Preferences.Set("device_uuid", uuid);
            }

            var payload = new { Uuid = uuid, Manufacturer = DeviceInfo.Manufacturer ?? string.Empty, Model = DeviceInfo.Model ?? string.Empty, AppVersion = DeviceInfo.VersionString ?? "" };

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TourismApp/1.0");

            await client.PostAsJsonAsync(url, payload);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceRegister] RegisterDeviceAsync failed: {ex.Message}");
        }
    }

    public static async Task UnregisterDeviceAsync()
    {
        try
        {
            var apiBase = Preferences.Get("api_base_url", string.Empty);
            if (string.IsNullOrWhiteSpace(apiBase))
            {
                if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType != DeviceType.Physical)
                {
                    apiBase = "http://10.0.2.2:5219/api/";
                }
                else
                {
                    apiBase = "http://192.168.1.176:5219/api/";
                }
            }

            if (!apiBase.EndsWith("/")) apiBase += "/";
            if (!apiBase.EndsWith("api/", StringComparison.OrdinalIgnoreCase)) apiBase += "api/";

            var url = apiBase + "pois/device/leave";

            var uuid = Preferences.Get("device_uuid", string.Empty);
            if (string.IsNullOrWhiteSpace(uuid))
            {
                uuid = Guid.NewGuid().ToString();
                Preferences.Set("device_uuid", uuid);
            }

            var payload = new { Uuid = uuid, Manufacturer = DeviceInfo.Manufacturer ?? string.Empty, Model = DeviceInfo.Model ?? string.Empty, AppVersion = DeviceInfo.VersionString ?? "" };

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TourismApp/1.0");

            await client.PostAsJsonAsync(url, payload);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceRegister] UnregisterDeviceAsync failed: {ex.Message}");
        }
    }
}
