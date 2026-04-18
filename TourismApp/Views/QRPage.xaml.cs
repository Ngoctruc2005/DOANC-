using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using System.Text.RegularExpressions;

namespace TourismApp.Views;

public partial class QRPage : ContentPage
{
    readonly CameraBarcodeReaderView _camera = new();
    readonly SemaphoreSlim _scanLock = new(1, 1);
    private bool _isAnimating = false;
    private bool _isPageActive;

    public QRPage()
    {
        InitializeComponent();
        BindingContext = TourismApp.Services.LocalizationService.Instance;

        _camera.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
        _camera.CameraLocation = CameraLocation.Rear;
        _camera.BarcodesDetected += OnDetected;
        cameraHost.Content = _camera;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageActive = true;
        var hasPermission = await CheckCameraPermissionAsync();
        if (!hasPermission)
        {
            resultLabel.Text = "Cần cấp quyền Camera để quét QR.";
            return;
        }

        _isAnimating = true;
        _camera.IsDetecting = true;
        AnimateScanLine();
    }

    private async Task<bool> CheckCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                var loc = TourismApp.Services.LocalizationService.Instance;
                var openSettings = await DisplayAlert(
                    loc["Notice"] ?? "Lỗi",
                    "Quyền truy cập Camera bị từ chối. Không thể quét QR.",
                    "Mở cài đặt",
                    loc["Cancel"] ?? "Hủy");

                if (openSettings)
                {
                    AppInfo.ShowSettingsUI();
                }

                return false;
            }
        }

        return true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isPageActive = false;
        _isAnimating = false;
        _camera.IsDetecting = false;
        // Notify server that device left (best-effort)
        try
        {
            var apiService = new TourismApp.Services.PoiApiService(null);
            _ = apiService.PostDeviceLeaveAsync();
        }
        catch { }
    }

    private async void AnimateScanLine()
    {
        while (_isAnimating && scanLine != null)
        {
            // Di chuy?n tia laser t? tr�n xu?ng d�?i
            await scanLine.TranslateTo(0, 252, 1200, Easing.Linear);
            // K�o ng�?c l?i t? d�?i l�n tr�n
            await scanLine.TranslateTo(0, 0, 1200, Easing.Linear);
        }
    }

    void OnDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault()?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        _ = HandleDetectedAsync(result);
    }

    private async Task HandleDetectedAsync(string result)
    {
        if (!_isPageActive)
        {
            return;
        }

        if (!await _scanLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            _camera.IsDetecting = false;
            var loc = TourismApp.Services.LocalizationService.Instance;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                resultLabel.Text = loc["SearchingRestaurant"];
            });

            if (TryExtractPoiId(result, out int poiId))
            {
                var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
                var apiService = new TourismApp.Services.PoiApiService(dbContext);
                var pois = await apiService.GetAllPOIsAsync();

                // Ki?m tra xem danh s�ch c� tr? v? l?i API kh�ng
                var apiErrorPoi = pois.FirstOrDefault(p => p.Poiid == -1);
                if (apiErrorPoi != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        resultLabel.Text = loc["ApiError"];
                        await DisplayAlert(loc["ConnectionError"], $"{loc["BackendErrorMsg"]}{apiErrorPoi.Description}", loc["OK"]);
                    });
                    return;
                }

                var restaurant = pois.FirstOrDefault(p => p.Poiid == poiId || p.ApiId == poiId || p.Id == poiId.ToString());
                if (restaurant != null)
                {
                    // Register visit on backend (silent) then open restaurant page
                    _ = apiService.PostVisitAsync(restaurant.Poiid);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        resultLabel.Text = $"{loc["OpeningRestaurant"]}{restaurant.Name}...";
                        await Navigation.PushAsync(new RestaurantDetailPage(restaurant, autoPlayDescription: true));
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        resultLabel.Text = loc["RestaurantNotFound"];
                        await DisplayAlert(loc["Notice"], $"{loc["RestaurantNotFoundWithId"]}{result}", loc["OK"]);
                    });
                }
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    resultLabel.Text = loc["InvalidQRCode"];
                    await DisplayAlert(loc["Notice"], $"{loc["InvalidQRFormat"]}{result}.\n{loc["RequireScanRestaurant"]}", loc["OK"]);
                });
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QR SCAN ERROR] {ex}");
            var loc = TourismApp.Services.LocalizationService.Instance;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                resultLabel.Text = loc["Notice"];
                await DisplayAlert(loc["Notice"], $"Lỗi khi quét QR: {ex.Message}", loc["OK"]);
            });
        }
        finally
        {
            if (_isPageActive)
            {
                _camera.IsDetecting = true;
            }

            _scanLock.Release();
        }
    }

    private static bool TryExtractPoiId(string rawValue, out int poiId)
    {
        poiId = 0;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var value = rawValue.Trim();

        if (int.TryParse(value, out poiId))
        {
            return poiId > 0;
        }

        var prefixedMatch = Regex.Match(value, @"(?i)^(poi|poiid|id)\s*[:=]\s*(\d+)$");
        if (prefixedMatch.Success && int.TryParse(prefixedMatch.Groups[2].Value, out poiId))
        {
            return poiId > 0;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var query = uri.Query.TrimStart('?');
            if (!string.IsNullOrWhiteSpace(query))
            {
                foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2)
                    {
                        continue;
                    }

                    var key = Uri.UnescapeDataString(kv[0]);
                    var val = Uri.UnescapeDataString(kv[1]);
                    if ((key.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("poi", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("poiid", StringComparison.OrdinalIgnoreCase)) &&
                        int.TryParse(val, out poiId))
                    {
                        return poiId > 0;
                    }
                }
            }
        }

        var fallback = Regex.Match(value, @"(?i)(?:poi|poiid|id)[^\d]*(\d+)");
        if (fallback.Success && int.TryParse(fallback.Groups[1].Value, out poiId))
        {
            return poiId > 0;
        }

        var allNumbers = Regex.Matches(value, @"\d+").Select(m => m.Value).ToList();
        if (allNumbers.Count == 1 && int.TryParse(allNumbers[0], out poiId))
        {
            return poiId > 0;
        }

        return false;
    }
}