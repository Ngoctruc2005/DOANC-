using TourismApp.Models;

namespace TourismApp.Services;

public static class FavoriteService
{
    public static List<Poi> Favorites = new();

    public static void Add(Poi r)
    {
        if (!Favorites.Any(x => x.Name == r.Name))
            Favorites.Add(r);
    }

    public static List<Poi> GetAll()
    {
        return Favorites;
    }
}