using TourismApp.Views;

namespace TourismApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    // 👉 Hiện / ẩn chọn ngôn ngữ
    private void OnPlayAudioClicked(object sender, EventArgs e)
    {
        languagePicker.IsVisible = !languagePicker.IsVisible;
    }

    // 👉 Phát thuyết minh
    async void OnSpeakClicked(object sender, EventArgs e)
    {
        var lang = languagePicker.SelectedItem?.ToString() ?? "vi";

        string text = lang == "en"
            ? "Welcome to the tourism app. Discover delicious food and famous places."
            : "Chào mừng đến với ứng dụng du lịch ẩm thực. Khám phá các món ăn và địa điểm nổi tiếng.";

        await TextToSpeech.Default.SpeakAsync(text);
    }

    async void OnOpenMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }

    async void OnOpenFavoriteClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FavoritePage());
    }
}