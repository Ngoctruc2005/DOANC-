using Microsoft.EntityFrameworkCore;
using TourismCMS.Models;

namespace TourismCMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<POI> POIs { get; set; }
    }
}