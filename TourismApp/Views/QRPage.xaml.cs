using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TourismApp.Views;

public partial class QRPage : ContentPage
{
    readonly CameraBarcodeReaderView _camera = new();
    bool _isHandlingScan;
    private bool _isAnimating = false;

    public QRPage()
    {
        InitializeComponent();
        BindingContext = TourismApp.Services.LocalizationService.Instance;

        _camera.BarcodesDetected += OnDetected;
        cameraHost.Content = _camera;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isAnimating = true;
        _camera.IsDetecting = true;
        AnimateScanLine();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isAnimating = false;
        _camera.IsDetecting = false;
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
        if (_isHandlingScan)
        {
            return;
        }

        var result = e.Results.FirstOrDefault()?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        _isHandlingScan = true;

        MainThread.BeginInvokeOnMainThread(async () => 
        {
            _camera.IsDetecting = false;
            var loc = TourismApp.Services.LocalizationService.Instance;
            resultLabel.Text = loc["SearchingRestaurant"];

            if (int.TryParse(result, out int poiId))
            {
                var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
                var apiService = new TourismApp.Services.PoiApiService(dbContext);
                var pois = await apiService.GetAllPOIsAsync();

                // Ki?m tra xem danh s�ch c� tr? v? l?i API kh�ng
                var apiErrorPoi = pois.FirstOrDefault(p => p.Poiid == -1);
                if (apiErrorPoi != null)
                {
                    resultLabel.Text = loc["ApiError"];
                    await DisplayAlert(loc["ConnectionError"], $"{loc["BackendErrorMsg"]}{apiErrorPoi.Description}", loc["OK"]);

                    _camera.IsDetecting = true;
                    _isHandlingScan = false;
                    return;
                }

                var restaurant = pois.FirstOrDefault(p => p.Poiid == poiId);
                if (restaurant != null)
                {
                    resultLabel.Text = $"{loc["OpeningRestaurant"]}{restaurant.Name}...";
                    await Navigation.PushAsync(new RestaurantDetailPage(restaurant, autoPlayDescription: true));
                }
                else
                {
                    resultLabel.Text = loc["RestaurantNotFound"];
                    await DisplayAlert(loc["Notice"], $"{loc["RestaurantNotFoundWithId"]}{result}", loc["OK"]);
                }
            }
            else
            {
                resultLabel.Text = loc["InvalidQRCode"];
                await DisplayAlert(loc["Notice"], $"{loc["InvalidQRFormat"]}{result}.\n{loc["RequireScanRestaurant"]}", loc["OK"]);
            }

            _camera.IsDetecting = true;
            _isHandlingScan = false;
        });
    }
}
