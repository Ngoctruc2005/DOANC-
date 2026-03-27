using Microsoft.Maui.Storage;

namespace TourismApp.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();

        // Load ngôn ngữ đã lưu
        var lang = Preferences.Get("language", "vi");
        languagePicker.SelectedItem = lang;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var lang = languagePicker.SelectedItem?.ToString() ?? "vi";

        Preferences.Set("language", lang);

        DisplayAlert("Thông báo", "Đã lưu cài đặt", "OK");
    }
}