using TourismApp.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace TourismApp.Services;

public static class FavoriteService
{
    private const string FavoritesStorageKey = "favorite_pois";
    public static List<Poi> Favorites = LoadFavorites();

    private static List<Poi> LoadFavorites()
    {
        var json = Preferences.Get(FavoritesStorageKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new List<Poi>();

        try
        {
            // Try to deserialize to current model first
            var list = JsonSerializer.Deserialize<List<Poi>>(json);
            if (list != null && list.Count > 0)
            {
                return list;
            }

            // Fallback: older versions used `POI` class (different shape, may not include Thumbnail/ImagePath)
            try
            {
                var old = JsonSerializer.Deserialize<List<POI>>(json);
                if (old != null && old.Count > 0)
                {
                    var migrated = old.Select(o => new Poi
                    {
                        Poiid = 0,
                        Name = o.Name,
                        Description = o.Description,
                        Latitude = o.Latitude,
                        Longitude = o.Longitude,
                        Radius = o.Radius
                    }).ToList();

                    // Persist migrated favorites in new format
                    Favorites = migrated;
                    SaveFavorites();
                    return migrated;
                }
            }
            catch (Exception exOld)
            {
                System.Diagnostics.Debug.WriteLine($"[FavoriteService] fallback deserialize old POI failed: {exOld.Message}");
            }

            return new List<Poi>();
        }
        catch
        {
            return new List<Poi>();
        }
    }

    private static void SaveFavorites()
    {
        var json = JsonSerializer.Serialize(Favorites);
        Preferences.Set(FavoritesStorageKey, json);
    }

    public static bool Contains(Poi r)
    {
        if (r == null) return false;

        if (r.Poiid > 0)
            return Favorites.Any(x => x.Poiid == r.Poiid);

        return Favorites.Any(x => x.Name == r.Name);
    }

    public static void Add(Poi r)
    {
        if (!Contains(r))
        {
            Favorites.Add(r);
            SaveFavorites();
        }
    }

    public static void Remove(Poi r)
    {
        Poi? itemToRemove = null;
        if (r.Poiid > 0)
            itemToRemove = Favorites.FirstOrDefault(x => x.Poiid == r.Poiid);

        itemToRemove ??= Favorites.FirstOrDefault(x => x.Name == r.Name);
        if (itemToRemove != null)
        {
            Favorites.Remove(itemToRemove);
            SaveFavorites();
        }
    }

    public static List<Poi> GetAll()
    {
        return Favorites;
    }
}