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

    async void OnDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isHandlingScan)
        {
            return;
        }

        var result = e.Results.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        _isHandlingScan = true;
        _camera.IsDetecting = false;
        resultLabel.Text = result;

        await DisplayAlertAsync("QR", $"Bạn vừa quét: {result}", "OK");

        _camera.IsDetecting = true;
        _isHandlingScan = false;
    }
}