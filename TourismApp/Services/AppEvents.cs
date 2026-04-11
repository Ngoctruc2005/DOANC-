using System;

namespace TourismApp.Services;

public static class AppEvents
{
    public static event Action<double>? ScanRadiusChanged;

    public static void RaiseScanRadiusChanged(double value)
    {
        try { ScanRadiusChanged?.Invoke(value); } catch { }
    }
}
