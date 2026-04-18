using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TourismCMS.Models;

namespace TourismCMS.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminUser> AdminUsers { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<POI> POIs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<VisitLog> VisitLogs { get; set; }

    public virtual DbSet<TourismCMS.Models.Device> Devices { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<PoiOwnerRegistration> PoiOwnerRegistrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__AdminUse__1788CCAC28FC9F8C");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.AdminUsers)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__AdminUser__RoleI__4CA06362");
        });

        modelBuilder.Entity<TourismCMS.Models.Device>(entity =>
        {
            entity.HasKey(e => e.DeviceId).HasName("PK_Devices_DeviceId");
            entity.ToTable("Devices");
            entity.Property(e => e.DeviceId).HasMaxLength(200).IsUnicode(false).HasColumnName("DeviceID");
            entity.Property(e => e.AgentSample).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.FirstSeen).HasColumnType("datetime");
            entity.Property(e => e.LastSeen).HasColumnType("datetime");
            entity.Property(e => e.TotalVisits);
            entity.Property(e => e.IsActive);
        });

        // Ensure EF Core does not try to map the Radius property (column was removed from DB)
        modelBuilder.Entity<POI>().Ignore(p => p.Radius);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__5E5499A81C6A02AB");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.Action).HasMaxLength(255);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2B39B330C8");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.MenuId).HasName("PK__Menu__C99ED250BB90BCE7");

            entity.ToTable("Menu");

            entity.Property(e => e.MenuId).HasColumnName("MenuID");
            entity.Property(e => e.FoodName).HasMaxLength(150);
            entity.Property(e => e.Image)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Poiid).HasColumnName("POIID");

            entity.HasOne(d => d.Poi).WithMany(p => p.Menus)
                .HasForeignKey(d => d.Poiid)
                .HasConstraintName("FK__Menu__POIID__5812160E");
        });

        modelBuilder.Entity<POI>(entity =>
        {
            entity.HasKey(e => e.Poiid).HasName("PK__POIs__5229E33F54FB0367");

            entity.ToTable("POIs");

            entity.Property(e => e.Poiid).HasColumnName("POIID");
             // OwnerId may be NULL in existing DB rows; map as optional
                entity.Property(e => e.OwnerId).IsRequired(false);
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(50);

            // Radius column was removed from the database; property is ignored by EF (see above)

            entity.HasMany(d => d.Categories).WithMany(p => p.POIs)
                .UsingEntity<Dictionary<string, object>>(
                    "PoiCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                         .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__POI_Categ__Categ__5535A963"),
                    l => l.HasOne<POI>().WithMany()
                        .HasForeignKey("Poiid")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__POI_Categ__POIID__5441852A"),
                    j =>
                    {
                        j.HasKey("Poiid", "CategoryId").HasName("PK__POI_Cate__F3B9709DF94CC70B");
                        j.ToTable("POI_Categories");
                        j.IndexerProperty<int>("Poiid").HasColumnName("POIID");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("CategoryID");
                    });
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AB48E9454");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<VisitLog>(entity =>
        {
            entity.HasKey(e => e.VisitId).HasName("PK__VisitLog__4D3AA1BE915D54F2");

            entity.ToTable("VisitLog");

            entity.Property(e => e.VisitId).HasColumnName("VisitID");
            entity.Property(e => e.DeviceId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DeviceID");
            entity.Property(e => e.Poiid).HasColumnName("POIID");
            entity.Property(e => e.VisitTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.POI).WithMany(p => p.VisitLogs)
                .HasForeignKey(d => d.Poiid)
                .HasConstraintName("FK__VisitLog__POIID__5BE2A6F2");
        });

        modelBuilder.Entity<PoiOwnerRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId);
            entity.Property(e => e.Status)
                .HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}