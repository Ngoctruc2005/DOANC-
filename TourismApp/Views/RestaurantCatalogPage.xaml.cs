using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp.Views
{
    public class RestaurantCatalogPage : ContentPage
    {
        private const string DefaultRestaurantImageUrl = "https://th.bing.com/th/id/OIG2.cM2sC3m65gCok8JmZJq1?pid=ImgGn";
        private readonly CollectionView _restaurantList;

        public RestaurantCatalogPage()
        {
            BindingContext = LocalizationService.Instance;
            BackgroundColor = Color.FromArgb("#F5F5F5");
            LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;

            _restaurantList = new CollectionView
            {
                SelectionMode = SelectionMode.None,
                ItemTemplate = new DataTemplate(() =>
                {
                    var name = new Label { FontAttributes = FontAttributes.Bold, FontSize = 20, TextColor = Color.FromArgb("#222") };
                    name.SetBinding(Label.TextProperty, nameof(RestaurantCatalogItem.DisplayName));

                    var description = new Label { FontSize = 13, TextColor = Color.FromArgb("#666"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation };
                    description.SetBinding(Label.TextProperty, nameof(RestaurantCatalogItem.DisplayDescription));

                    var distance = new Label { FontSize = 13, TextColor = Color.FromArgb("#0B8F3A"), FontAttributes = FontAttributes.Bold };
                    distance.SetBinding(Label.TextProperty, nameof(RestaurantCatalogItem.DistanceText));

                    var right = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(8, 0, 0, 0), Children = { name, description, distance } };

                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                        ColumnSpacing = 12
                    };

                    grid.Add(right);

                    var border = new Border
                    {
                        Margin = new Thickness(0, 6),
                        Padding = 12,
                        StrokeShape = new RoundRectangle { CornerRadius = 14 },
                        Stroke = Colors.Transparent,
                        BackgroundColor = Colors.White,
                        Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(0, 2), Opacity = 0.12f, Radius = 4 },
                        Content = grid
                    };

                    var tap = new TapGestureRecognizer();
                    tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
                    tap.Tapped += OnRestaurantTapped;
                    border.GestureRecognizers.Add(tap);

                    return border;
                })
            };

            var rootGrid = new Grid
            {
                Padding = new Thickness(12, 10),
                RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Star }
            },
                RowSpacing = 10
            };

            rootGrid.Add(_restaurantList, 0, 0);
            Content = rootGrid;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCatalogAsync();
        }

        private async void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LocalizationService.CurrentLanguage) && e.PropertyName != "Item")
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(LoadCatalogAsync);
        }

        private async Task LoadCatalogAsync()
        {
            var dbContext = Handler?.MauiContext?.Services.GetService<TourismCMS.Data.FoodDbContext>();
            var apiService = new PoiApiService(dbContext);
            var pois = await apiService.GetAllPOIsAsync();
            var loc = LocalizationService.Instance;
            var lang = loc.CurrentLanguage;

            var userLocation = await TryGetCurrentLocationAsync();
            var geofenceService = new GeofenceService();

            var items = new List<RestaurantCatalogItem>();
            foreach (var p in pois.Where(p => p.Poiid > 0))
            {
                double? distance = null;
                if (userLocation != null && p.Latitude.HasValue && p.Longitude.HasValue)
                {
                    distance = geofenceService.GetDistance(
                        userLocation.Latitude,
                        userLocation.Longitude,
                        p.Latitude.Value,
                        p.Longitude.Value);
                }

                var displayName = p.Name ?? string.Empty;
                var displayDescription = p.Description ?? string.Empty;

                if (lang != "vi")
                {
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = await TTSHelper.TranslateTextAsync(displayName, lang);
                    }

                    if (!string.IsNullOrWhiteSpace(displayDescription))
                    {
                        displayDescription = await TTSHelper.TranslateTextAsync(displayDescription, lang);
                    }
                }

                var imageSource = await ResolveImageSourceAsync(p);

                items.Add(new RestaurantCatalogItem
                {
                    Poi = p,
                    DisplayName = displayName,
                    DisplayDescription = displayDescription,
                    DistanceMeters = distance,
                    DistanceText = distance.HasValue
                        ? $"{loc["DistancePrefix"]} {Math.Round(distance.Value)} m"
                        : loc["DistanceUnknown"],
                    ImageSource = imageSource
                });
            }

            items = items.OrderBy(i => i.DistanceMeters ?? double.MaxValue).ToList();

            _restaurantList.ItemsSource = items;
        }

        private async Task<Location?> TryGetCurrentLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    return null;
                }

                return await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
            }
            catch
            {
                return null;
            }
        }

        private static ImageSource ResolveImageSource(Poi poi)
        {
            var imageUrl = !string.IsNullOrWhiteSpace(poi.ImagePath) ? poi.ImagePath : poi.Thumbnail;
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return ImageSource.FromUri(new Uri(DefaultRestaurantImageUrl));
            }

            var resolved = ResolveImageUrl(imageUrl);
            if (Uri.TryCreate(resolved, UriKind.Absolute, out var uri))
            {
                return ImageSource.FromUri(uri);
            }

            return ImageSource.FromUri(new Uri(DefaultRestaurantImageUrl));
        }

        private static async Task<ImageSource> ResolveImageSourceAsync(Poi poi)
        {
            try
            {
                var imageUrl = !string.IsNullOrWhiteSpace(poi.ImagePath) ? poi.ImagePath : poi.Thumbnail;
                if (string.IsNullOrWhiteSpace(imageUrl))
                    return ImageSource.FromUri(new Uri(DefaultRestaurantImageUrl));

                var resolved = ResolveImageUrl(imageUrl);
                System.Diagnostics.Debug.WriteLine($"[ResolveImageSourceAsync] resolved={resolved}");

                if (Uri.TryCreate(resolved, UriKind.Absolute, out var uri))
                {
                    try
                    {
                        var handler = new HttpClientHandler();
                        handler.ServerCertificateCustomValidationCallback = (m, cert, chain, errors) => true;
                        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                        client.DefaultRequestHeaders.Add("X-DevTunnels-Skip-Anti-Phishing-Page", "true");
                        client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

                        // Add common browser User-Agent to help some dev-tunnel/ngrok endpoints return image data
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        var resp = await client.GetAsync(uri);
                        if (resp.IsSuccessStatusCode)
                        {
                            var bytes = await resp.Content.ReadAsByteArrayAsync();
                            return ImageSource.FromStream(() => new MemoryStream(bytes));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ResolveImageSourceAsync] download failed: {ex.Message}");
                    }

                    // Last chance: return ImageSource.FromUri so MAUI can try its own loader
                    return ImageSource.FromUri(uri);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ResolveImageSourceAsync] error: {ex.Message}");
            }

            return ImageSource.FromUri(new Uri(DefaultRestaurantImageUrl));
        }

        public static string ResolveImageUrl(string rawImageUrl)
        {
            System.Diagnostics.Debug.WriteLine($"[ResolveImageUrl] rawImageUrl={rawImageUrl}");
            var imageUrl = rawImageUrl.Trim().Replace("~/", string.Empty);
            if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return imageUrl;
            }

            var apiBaseUrl = Preferences.Get("api_base_url", string.Empty);
            System.Diagnostics.Debug.WriteLine($"[ResolveImageUrl] apiBaseUrl={apiBaseUrl}");
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                apiBaseUrl = "https://nqrwpkxp-5219.asse.devtunnels.ms/api/";
            }

            var baseUrl = apiBaseUrl.Trim();
            if (baseUrl.EndsWith("/api/", StringComparison.OrdinalIgnoreCase))
                baseUrl = baseUrl[..^5];
            else if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                baseUrl = baseUrl[..^4];

            var resolvedUrl = $"{baseUrl.TrimEnd('/')}/{imageUrl.TrimStart('/')}";
            System.Diagnostics.Debug.WriteLine($"[ResolveImageUrl] resolvedUrl={resolvedUrl}");
            return resolvedUrl;
        }

        private async void OnRestaurantTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is RestaurantCatalogItem item)
            {
                await Navigation.PushAsync(new RestaurantDetailPage(item.Poi));
            }
        }

        // ... helper ResolveImageSourceAsync already exists above - ensure method signature present

        private sealed class RestaurantCatalogItem
        {
            public Poi Poi { get; set; } = default!;
            public string DisplayName { get; set; } = string.Empty;
            public string DisplayDescription { get; set; } = string.Empty;
            public double? DistanceMeters { get; set; }
            public string DistanceText { get; set; } = string.Empty;
            public ImageSource ImageSource { get; set; } = default!;
        }
    }
}