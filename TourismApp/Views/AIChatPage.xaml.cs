using System.Collections.ObjectModel;
using TourismApp.Models;
using TourismApp.Services;
using Microsoft.Maui.Graphics;

namespace TourismApp.Views;

public class ChatMessage
{
    public string Text { get; set; } = string.Empty;
    public bool IsUser { get; set; }

    public LayoutOptions Alignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;
    
    // Màu nền tin nhắn
    public Color BgColor => IsUser ? Color.FromArgb("#9370DB") : Color.FromArgb("#E0E0E0");
    
    // Màu chữ
    public Color TextColor => IsUser ? Colors.White : Colors.Black;
}

public partial class AIChatPage : ContentPage
{
    public ObservableCollection<ChatMessage> Messages { get; set; } = new();
    private Poi? _restaurant;
    private PoiApiService? _apiService;

    public AIChatPage(Poi? restaurant = null)
    {
        InitializeComponent();
        _restaurant = restaurant;

        // Gán danh sách tin nhắn vào giao diện
        chatList.ItemsSource = Messages;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (_apiService == null)
        {
            var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
            _apiService = new PoiApiService(dbContext);

            // Gửi tin nhắn chào đầu tiên
            string welcomeText = _restaurant != null 
                ? $"✨ Xin chào! Tôi là Trợ lý AI ẩm thực. Bạn có thắc mắc gì về quán '{_restaurant.Name}' này không?" 
                : "✨ Xin chào! Tôi có thể giúp gì cho bạn hôm nay?";
                
            AddAiMessage(welcomeText);
        }
    }

    private void AddUserMessage(string text)
    {
        Messages.Add(new ChatMessage { Text = text, IsUser = true });
        ScrollToBottom();
    }

    private void AddAiMessage(string text)
    {
        // Kiểm tra nếu đang có dòng Loading thì xóa đi
        var last = Messages.LastOrDefault();
        if (last != null && !last.IsUser && last.Text == "Đang suy nghĩ...")
        {
            Messages.Remove(last);
        }

        Messages.Add(new ChatMessage { Text = text, IsUser = false });
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (Messages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                chatList.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: true);
            });
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        string text = messageEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        messageEntry.Text = string.Empty;
        messageEntry.IsEnabled = false;

        // 1. Hiện tin nhắn của mình
        AddUserMessage(text);
        
        // 2. Hiện trạng thái đang nhập
        Messages.Add(new ChatMessage { Text = "Đang suy nghĩ...", IsUser = false });
        ScrollToBottom();

        // 3. Gọi AI
        string prompt = text;
        if (_restaurant != null)
        {
            prompt = $"Ngữ cảnh: Khách hàng đang hỏi về quán ăn '{_restaurant.Name}' (Mô tả: {_restaurant.Description}). Câu hỏi của khách: {text}";
        }

        string response = string.Empty;

        try
        {
            if (_apiService != null)
            {
                // Gọi API backend (TourismCMS)
                response = await _apiService.ChatWithAI(prompt);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            // Trả lời khẩn cấp ngay trên thiết bị mà không cần chờ nếu API Lỗi (không có internet, backend đang sập)
            response = TạmDịchHoặcTrảLờiCơBảnTrựcTiếpDướiMáy(text);
        }

        // 4. Bỏ chữ Đang suy nghĩ và hiện câu trả lời thật
        AddAiMessage(response);
        messageEntry.IsEnabled = true;
        messageEntry.Focus();
    }

    private string TạmDịchHoặcTrảLờiCơBảnTrựcTiếpDướiMáy(string q)
    {
        string lowQ = q.ToLower();
        if (lowQ.Contains("chào")) return "Chào bạn, tính năng chatbot đang tải dữ liệu offline do mạng yếu.";
        if (lowQ.Contains("ngon")) return "Các quán ăn trên bản đồ đều mang lại những trải nghiệm tốt. Bạn hãy xem thử!";
        if (_restaurant != null) return $"Dạ về '{_restaurant.Name}', bạn có thể xem mô tả và chỉ đường ở menu nhé!";
        return "Xin lỗi, hiện tại tôi không thể kết nối tới máy chủ AI. Vui lòng thử lại sau!";
    }
}