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
        "RestaurantCatalog" => "Danh mục",
        "Favorites" => "❤Yêu thích",
        "Unfavorite" => "💔 Bỏ thích",
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
        "DistancePrefix" => "📍 Cách",
        "DistanceUnknown" => "📍 Không xác định",
        "RestaurantDetailTitle" => "Chi tiết quán",
        "ScanQRInstruction" => "Đưa camera vào mã QR để quét",
        "Delete" => "🗑️ Xóa",
        "SearchingRestaurant" => "Đang tìm quán ăn...",
        "ApiError" => "Lỗi API",
        "ConnectionError" => "Lỗi kết nối",
        "BackendErrorMsg" => "Không thể lấy dữ liệu từ Backend:\n",
        "OpeningRestaurant" => "Đang mở ",
        "RestaurantNotFound" => "Không tìm thấy quán ăn...",
        "Notice" => "Thông báo",
        "RestaurantNotFoundWithId" => "Không tìm thấy thông tin quán ăn với mã: ",
        "InvalidQRCode" => "Mã QR không hợp lệ",
        "InvalidQRFormat" => "Định dạng QR không hợp lệ: ",
        "RequireScanRestaurant" => "Yêu cầu quét mã số quán ăn.",
        "AddedToFavorites" => "Đã thêm vào yêu thích ❤️",
        "RemovedFromFavorites" => "Đã bỏ khỏi yêu thích",
        "NoticeSaved" => "Đã lưu cài đặt. Ngôn ngữ: ",
        "ConfirmDelete" => "Xác nhận xóa",
        "Yes" => "Có",
        "No" => "Không",
        "OK" => "OK",
        _ => key
    };

    private string GetEnglishText(string key) => key switch
    {
        "Explore" => "Explore",
        "RestaurantCatalog" => "Catalog",
        "Favorites" => "❤ Favorites",
        "Unfavorite" => "💔 Unfavorite",
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
        "DistancePrefix" => "📍",
        "DistanceUnknown" => "📍 Unknown",
        "RestaurantDetailTitle" => "Restaurant Details",
        "ScanQRInstruction" => "Point camera at QR code to scan",
        "Delete" => "🗑️ Delete",
        "SearchingRestaurant" => "Searching for restaurant...",
        "ApiError" => "API Error",
        "ConnectionError" => "Connection Error",
        "BackendErrorMsg" => "Could not retrieve data from Backend:\n",
        "OpeningRestaurant" => "Opening ",
        "RestaurantNotFound" => "Restaurant not found...",
        "Notice" => "Notice",
        "RestaurantNotFoundWithId" => "Could not find restaurant with ID: ",
        "InvalidQRCode" => "Invalid QR Code",
        "InvalidQRFormat" => "Invalid QR format: ",
        "RequireScanRestaurant" => "Please scan a valid restaurant QR.",
        "AddedToFavorites" => "Added to favorites ❤️",
        "RemovedFromFavorites" => "Removed from favorites",
        "NoticeSaved" => "Settings saved. Language: ",
        "ConfirmDelete" => "Confirm delete",
        "Yes" => "Yes",
        "No" => "No",
        "OK" => "OK",
        _ => key
    };

    private string GetChineseText(string key) => key switch
    {
        "Explore" => "探索",
        "RestaurantCatalog" => "目录",
        "Favorites" => "❤ 最爱",
        "Unfavorite" => "💔 取消收藏",
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
        "DistancePrefix" => "📍 距离",
        "DistanceUnknown" => "📍 未知",
        "RestaurantDetailTitle" => "餐厅详情",
        "ScanQRInstruction" => "将相机对准二维码进行扫描",
        "Delete" => "🗑️ 删除",
        "SearchingRestaurant" => "正在寻找餐厅...",
        "ApiError" => "API错误",
        "ConnectionError" => "连接错误",
        "BackendErrorMsg" => "无法从后端获取数据:\n",
        "OpeningRestaurant" => "正在打开 ",
        "RestaurantNotFound" => "找不到餐厅...",
        "Notice" => "通知",
        "RestaurantNotFoundWithId" => "找不到此代码的餐厅信息: ",
        "InvalidQRCode" => "二维码无效",
        "InvalidQRFormat" => "二维码格式无效: ",
        "RequireScanRestaurant" => "请扫描餐厅二维码。",
        "AddedToFavorites" => "已添加到收藏 ❤️",
        "RemovedFromFavorites" => "已从收藏移除",
        "NoticeSaved" => "设置已保存。语言: ",
        "ConfirmDelete" => "确认删除",
        "Yes" => "确定",
        "No" => "取消",
        "OK" => "确定",
        _ => key
    };

    private string GetJapaneseText(string key) => key switch
    {
        "Explore" => "探検する",
        "RestaurantCatalog" => "カタログ",
        "Favorites" => "❤お気に入り",
        "Unfavorite" => "💔 お気に入り解除",
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
        "DistancePrefix" => "📍 距離",
        "DistanceUnknown" => "📍 不明",
        "RestaurantDetailTitle" => "レストランの詳細",
        "ScanQRInstruction" => "カメラをQRコードに向けてスキャン",
        "Delete" => "🗑️ 削除",
        "SearchingRestaurant" => "レストランを探しています...",
        "ApiError" => "API エラー",
        "ConnectionError" => "接続エラー",
        "BackendErrorMsg" => "バックエンドからデータを取得できませんでした:\n",
        "OpeningRestaurant" => "開いています ",
        "RestaurantNotFound" => "レストランが見つかりません...",
        "Notice" => "通知",
        "RestaurantNotFoundWithId" => "次のコードのレストラン情報が見つかりません: ",
        "InvalidQRCode" => "無効なQRコード",
        "InvalidQRFormat" => "無効なQRフォーマット: ",
        "RequireScanRestaurant" => "レストランのQRをスキャンしてください。",
        "AddedToFavorites" => "お気に入りに追加しました ❤️",
        "RemovedFromFavorites" => "お気に入りから外しました",
        "NoticeSaved" => "設定を保存しました。言語: ",
        "ConfirmDelete" => "削除を確認",
        "Yes" => "はい",
        "No" => "いいえ",
        "OK" => "OK",
        _ => key
    };

    private string GetKoreanText(string key) => key switch
    {
        "Explore" => "탐험하다",
        "RestaurantCatalog" => "카탈로그",
        "Favorites" => "❤즐겨찾기",
        "Unfavorite" => "💔 즐겨찾기 해제",
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
        "DistancePrefix" => "📍 거리",
        "DistanceUnknown" => "📍 알 수 없음",
        "RestaurantDetailTitle" => "레스토랑 세부 정보",
        "ScanQRInstruction" => "카메라를 QR 코드에 맞춰 스캔하세요",
        "Delete" => "🗑️ 삭제",
        "SearchingRestaurant" => "레스토랑 찾는 중...",
        "ApiError" => "API 오류",
        "ConnectionError" => "연결 오류",
        "BackendErrorMsg" => "백엔드에서 데이터를 가져올 수 없습니다:\n",
        "OpeningRestaurant" => "여는 중 ",
        "RestaurantNotFound" => "레스토랑을 찾을 수 없습니다...",
        "Notice" => "알림",
        "RestaurantNotFoundWithId" => "해당 코드의 레스토랑 정보를 찾을 수 없습니다: ",
        "InvalidQRCode" => "잘못된 QR 코드",
        "InvalidQRFormat" => "잘못된 QR 형식: ",
        "RequireScanRestaurant" => "레스토랑 코드를 스캔하세요.",
        "AddedToFavorites" => "즐겨찾기에 추가되었습니다 ❤️",
        "RemovedFromFavorites" => "즐겨찾기에서 제거되었습니다",
        "NoticeSaved" => "설정이 저장되었습니다. 언어: ",
        "ConfirmDelete" => "삭제 확인",
        "Yes" => "예",
        "No" => "아니요",
        "OK" => "확인",
        _ => key
    };

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}