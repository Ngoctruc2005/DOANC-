using Microsoft.EntityFrameworkCore;
using TourismApp.Models;

namespace TourismCMS.Data
{
    public class FoodDbContext : DbContext
    {
        public FoodDbContext(DbContextOptions<FoodDbContext> options) : base(options) { }

        public DbSet<Poi> POIs { get; set; }
        public DbSet<Menu> Menus { get; set; }
        // Thêm DbSet cho Roles, AdminUsers, Categories, v.v..
    }
}