using Microsoft.EntityFrameworkCore;
using Shop.Api.Entities;

namespace Shop.Api.Data;

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Sku).HasMaxLength(64);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Category).HasMaxLength(100);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Products_StockQuantity", "\"StockQuantity\" >= 0"));
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.Property(c => c.Code).HasMaxLength(64);
            entity.Property(c => c.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(c => c.Value).HasPrecision(18, 2);
            entity.HasIndex(c => c.Code).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Coupons_Value", "\"Value\" >= 0"));
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            entity.HasIndex(o => new { o.Status, o.UpdatedAt });
            entity.HasQueryFilter(o => !o.IsDeleted);
            entity.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(o => o.Coupon).WithMany().HasForeignKey(o => o.CouponId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
