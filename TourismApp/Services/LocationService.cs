using Microsoft.Maui.Devices.Sensors;

public class LocationService
{
    public async Task<Location> GetLocationAsync()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.High);
        return await Geolocation.GetLocationAsync(request);
    }
}