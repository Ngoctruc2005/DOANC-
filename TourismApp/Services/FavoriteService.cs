using TourismApp.Models;

namespace TourismApp.Services;

public static class FavoriteService
{
    public static List<Restaurant> Favorites = new();

    public static void Add(Restaurant r)
    {
        if (!Favorites.Any(x => x.Name == r.Name))
            Favorites.Add(r);
    }

    public static List<Restaurant> GetAll()
    {
        return Favorites;
    }
}