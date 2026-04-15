using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using TourismApp.Services;

namespace TourismApp.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = LocalizationService.Instance;

        // Load ngôn ngữ đã lưu
        var lang = Preferences.Get("language", "vi");
        switch (lang)
        {
            case "en": languagePicker.SelectedItem = "English (en)"; break;
            case "zh": languagePicker.SelectedItem = "中文 (zh)"; break;
            case "ja": languagePicker.SelectedItem = "日本語 (ja)"; break;
            case "ko": languagePicker.SelectedItem = "한국어 (ko)"; break;
            default: languagePicker.SelectedItem = "Tiếng Việt (vi)"; break;
        }

        var apiBaseEntry = this.FindByName<Entry>("apiBaseUrlEntry");
        if (apiBaseEntry != null)
        {
            var defaultApiBase = DeviceInfo.DeviceType == DeviceType.Physical
                ? string.Empty
                : "http://10.0.2.2:5219/api/";

            apiBaseEntry.Text = Preferences.Get("api_base_url", defaultApiBase);
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selectedItem = languagePicker.SelectedItem?.ToString() ?? "Tiếng Việt (vi)";
        string lang = "vi";
        if (selectedItem.Contains("en")) lang = "en";
        else if (selectedItem.Contains("zh")) lang = "zh";
        else if (selectedItem.Contains("ja")) lang = "ja";
        else if (selectedItem.Contains("ko")) lang = "ko";

        LocalizationService.Instance.CurrentLanguage = lang;

        var apiBaseUrl = this.FindByName<Entry>("apiBaseUrlEntry")?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            if (DeviceInfo.DeviceType == DeviceType.Physical &&
                apiBaseUrl.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Thông báo", "Máy thật không dùng được 10.0.2.2. Hãy nhập IP Wi-Fi của máy tính, ví dụ: http://192.168.x.x:5219/api/", "OK");
                return;
            }

            if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";
            if (!apiBaseUrl.EndsWith("api/", StringComparison.OrdinalIgnoreCase)) apiBaseUrl += "api/";
            Preferences.Set("api_base_url", apiBaseUrl);
        }
        else
        {
            Preferences.Remove("api_base_url");
        }

        var localization = LocalizationService.Instance;
        await DisplayAlert(
            localization["Notice"],
            $"{localization["NoticeSaved"]}{lang}",
            localization["OK"]);
    }
}