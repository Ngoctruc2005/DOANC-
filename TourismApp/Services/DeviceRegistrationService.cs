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
                apiBase = "http://192.168.1.176:5219/api/";
            }

            if (!apiBase.EndsWith("/")) apiBase += "/";
            if (!apiBase.EndsWith("api/", StringComparison.OrdinalIgnoreCase)) apiBase += "api/";

            var url = apiBase + "pois/device/enter";

            var idParts = new[] { DeviceInfo.Platform.ToString(), DeviceInfo.Manufacturer, DeviceInfo.Model, DeviceInfo.VersionString };
            var deviceId = string.Join(" | ", idParts);

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TourismApp/1.0");

            await client.PostAsJsonAsync(url, deviceId);
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
                apiBase = "http://192.168.1.176:5219/api/";
            }

            if (!apiBase.EndsWith("/")) apiBase += "/";
            if (!apiBase.EndsWith("api/", StringComparison.OrdinalIgnoreCase)) apiBase += "api/";

            var url = apiBase + "pois/device/leave";

            var idParts = new[] { DeviceInfo.Platform.ToString(), DeviceInfo.Manufacturer, DeviceInfo.Model, DeviceInfo.VersionString };
            var deviceId = string.Join(" | ", idParts);

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TourismApp/1.0");

            await client.PostAsJsonAsync(url, deviceId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceRegister] UnregisterDeviceAsync failed: {ex.Message}");
        }
    }
}
