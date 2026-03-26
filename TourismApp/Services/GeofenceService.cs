using TourismApp.Models;

public class GeofenceService
{
    public double GetDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371000; // bán kính trái đất (m)
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) *
                Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    public bool IsInside(Location user, POI poi)
    {
        double distance = GetDistance(user.Latitude, user.Longitude, poi.Latitude, poi.Longitude);
        return distance <= poi.Radius;
    }
}