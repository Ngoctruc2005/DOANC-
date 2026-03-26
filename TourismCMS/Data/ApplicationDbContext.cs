using Microsoft.EntityFrameworkCore;
using TourismCMS.Models;

namespace TourismCMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<POI> POIs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PoiOwnerRegistration> PoiOwnerRegistrations { get; set; }
    }
}