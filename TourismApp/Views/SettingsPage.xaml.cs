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
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var selectedItem = languagePicker.SelectedItem?.ToString() ?? "Tiếng Việt (vi)";
        string lang = "vi";
        if (selectedItem.Contains("en")) lang = "en";
        else if (selectedItem.Contains("zh")) lang = "zh";
        else if (selectedItem.Contains("ja")) lang = "ja";
        else if (selectedItem.Contains("ko")) lang = "ko";

        // Vừa cập nhật Preference vừa cập nhật Global Service
        LocalizationService.Instance.CurrentLanguage = lang;

        DisplayAlert("Thông báo", $"Đã lưu cài đặt ngôn ngữ / Saved language: {lang}", "OK");
    }
}