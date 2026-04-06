using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;

namespace TourismApp.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static readonly LocalizationService _instance = new();
    public static LocalizationService Instance => _instance;

    private string _currentLanguage = "vi";
    
    private LocalizationService()
    {
        _currentLanguage = Preferences.Get("language", "vi");
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                Preferences.Set("language", value);
                OnPropertyChanged(nameof(CurrentLanguage));
                OnPropertyChanged("Item"); // This trick forces bindings with indexers to update.
            }
        }
    }

    // Indexer for bindings
    public string this[string key] => GetText(key);

    public string GetText(string key)
    {
        // Simple dictionary for hardcoded UI strings
        return _currentLanguage switch
        {
            "en" => GetEnglishText(key),
            "zh" => GetChineseText(key),
            "ja" => GetJapaneseText(key),
            "ko" => GetKoreanText(key),
            _ => GetVietnameseText(key)
        };
    }

    private string GetVietnameseText(string key) => key switch
    {
        "Explore" => "Khám phá",
        "Favorites" => "❤Yêu thích",
        "TabFavorites" => "Yêu thích",
        "ScanQR" => "Quét QR",
        "Settings" => "Cài đặt",
        "SettingsTitle" => "⚙️ Cài đặt",
        "SelectLanguage" => "Chọn ngôn ngữ:",
        "SaveSettings" => "💾 Lưu cài đặt",
        "ListenAudio" => "🔊 Thuyết minh",
        "Directions" => "📍 Chỉ đường",
        "Close" => "❌ Đóng",
        "BestSeller" => "Món nổi bật",
        "Loading" => "Đang dịch...",
        "Updating" => "Đang cập nhật",
        "FavoriteListTitle" => "❤️ Danh sách yêu thích",
        "Menu" => "🍜 Menu",
        "ViewMap" => "🗺️ Xem bản đồ",
        "SearchPlaceholder" => "Tìm kiếm quán ăn...",
        "RestaurantDetailTitle" => "Chi tiết quán",
        "ScanQRInstruction" => "Đưa camera vào mã QR để quét",
        "Delete" => "🗑️ Xóa",
        _ => key
    };

    private string GetEnglishText(string key) => key switch
    {
        "Explore" => "Explore",
        "Favorites" => "❤ Favorites",
        "TabFavorites" => "Favorites",
        "ScanQR" => "Scan QR",
        "Settings" => "Settings",
        "SettingsTitle" => "⚙️ Settings",
        "SelectLanguage" => "Select Language:",
        "SaveSettings" => "💾 Save Settings",
        "ListenAudio" => "🔊 Audio Guide",
        "Directions" => "📍 Directions",
        "Close" => "❌ Close",
        "BestSeller" => "Best Seller",
        "Loading" => "Translating...",
        "Updating" => "Updating",
        "FavoriteListTitle" => "❤️ Favorites List",
        "Menu" => "🍜 Menu",
        "ViewMap" => "🗺️ View Map",
        "SearchPlaceholder" => "Search restaurants...",
        "RestaurantDetailTitle" => "Restaurant Details",
        "ScanQRInstruction" => "Point camera at QR code to scan",
        "Delete" => "🗑️ Delete",
        _ => key
    };

    private string GetChineseText(string key) => key switch
    {
        "Explore" => "探索",
        "Favorites" => "❤ 最爱",
        "TabFavorites" => "最爱",
        "ScanQR" => "扫描二维码",
        "Settings" => "设置",
        "SettingsTitle" => "⚙️ 设置",
        "SelectLanguage" => "选择语言:",
        "SaveSettings" => "💾 保存设置",
        "ListenAudio" => "🔊 语音导览",
        "Directions" => "📍 导航",
        "Close" => "❌ 关闭",
        "BestSeller" => "畅销",
        "Loading" => "翻译中...",
        "Updating" => "更新中",
        "FavoriteListTitle" => "❤️ 收藏列表",
        "Menu" => "🍜 菜单",
        "ViewMap" => "🗺️ 查看地图",
        "SearchPlaceholder" => "搜索餐厅...",
        "RestaurantDetailTitle" => "餐厅详情",
        "ScanQRInstruction" => "将相机对准二维码进行扫描",
        "Delete" => "🗑️ 删除",
        _ => key
    };

    private string GetJapaneseText(string key) => key switch
    {
        "Explore" => "探検する",
        "Favorites" => "❤お気に入り",
        "TabFavorites" => "お気に入り",
        "ScanQR" => "QRをスキャン",
        "Settings" => "設定",
        "SettingsTitle" => "⚙️ 設定",
        "SelectLanguage" => "言語を選択:",
        "SaveSettings" => "💾 設定を保存",
        "ListenAudio" => "🔊 音声ガイド",
        "Directions" => "📍 行き方",
        "Close" => "❌ 閉じる",
        "BestSeller" => "ベストセラー",
        "Loading" => "翻訳中...",
        "Updating" => "更新中",
        "FavoriteListTitle" => "❤️ お気に入りリスト",
        "Menu" => "🍜 メニュー",
        "ViewMap" => "🗺️ 地図を見る",
        "SearchPlaceholder" => "レストランを検索...",
        "RestaurantDetailTitle" => "レストランの詳細",
        "ScanQRInstruction" => "カメラをQRコードに向けてスキャン",
        "Delete" => "🗑️ 削除",
        _ => key
    };

    private string GetKoreanText(string key) => key switch
    {
        "Explore" => "탐험하다",
        "Favorites" => "❤즐겨찾기",
        "TabFavorites" => "즐겨찾기",
        "ScanQR" => "QR 스캔",
        "Settings" => "설정",
        "SettingsTitle" => "⚙️ 설정",
        "SelectLanguage" => "언어 선택:",
        "SaveSettings" => "💾 설정 저장",
        "ListenAudio" => "🔊 오디오 가이드",
        "Directions" => "📍 길찾기",
        "Close" => "❌ 닫기",
        "BestSeller" => "베스트셀러",
        "Loading" => "번역 중...",
        "Updating" => "업데이트 중",
        "FavoriteListTitle" => "❤️ 즐겨찾기 목록",
        "Menu" => "🍜 메뉴",
        "ViewMap" => "🗺️ 지도 보기",
        "SearchPlaceholder" => "레스토랑 검색...",
        "RestaurantDetailTitle" => "레스토랑 세부 정보",
        "ScanQRInstruction" => "카메라를 QR 코드에 맞춰 스캔하세요",
        "Delete" => "🗑️ 삭제",
        _ => key
    };

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}