using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TourismApp.Views;

public partial class QRPage : ContentPage
{
    readonly CameraBarcodeReaderView _camera = new();
    bool _isHandlingScan;

    public QRPage()
    {
        InitializeComponent();

        _camera.BarcodesDetected += OnDetected;
        cameraHost.Content = _camera;
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
            resultLabel.Text = "Ðang tìm quán an...";

            if (int.TryParse(result, out int poiId))
            {
                var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
                var apiService = new TourismApp.Services.PoiApiService(dbContext);
                var pois = await apiService.GetAllPOIsAsync();

                // Ki?m tra xem danh sách có tr? v? l?i API không
                var apiErrorPoi = pois.FirstOrDefault(p => p.Poiid == -1);
                if (apiErrorPoi != null)
                {
                    resultLabel.Text = "L?i API";
                    await DisplayAlert("L?i k?t n?i", $"Không th? l?y d? li?u t? Backend:\n{apiErrorPoi.Description}", "OK");

                    _camera.IsDetecting = true;
                    _isHandlingScan = false;
                    return;
                }

                var restaurant = pois.FirstOrDefault(p => p.Poiid == poiId);
                if (restaurant != null)
                {
                    resultLabel.Text = result;
                    await Navigation.PushAsync(new RestaurantDetailPage(restaurant));
                }
                else
                {
                    resultLabel.Text = "Không tìm th?y quán an";
                    await DisplayAlert("Thông báo", $"Không tìm th?y thông tin quán an v?i mã: {result}", "OK");
                }
            }
            else
            {
                resultLabel.Text = "Mã QR không h?p l?";
                await DisplayAlert("Thông báo", $"Ð?nh d?ng QR không h?p l?: {result}.\nYêu c?u quét mã s? quán an.", "OK");
            }

            _camera.IsDetecting = true;
            _isHandlingScan = false;
        });
    }
}
