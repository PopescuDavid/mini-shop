using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Entities;

namespace Shop.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ShopDbContext db, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
    {
        await SeedUserAsync(db, passwordHasher, configuration);
        await SeedProductsAsync(db);
        await SeedCouponsAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(ShopDbContext db, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
    {
        var email = configuration["Seed:UserEmail"] ?? "demo@shop.test";
        var password = configuration["Seed:UserPassword"] ?? "Passw0rd!";

        if (await db.Users.AnyAsync(u => u.Email == email))
            return;

        var user = new User { Email = email, PasswordHash = string.Empty };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
    }

    private static async Task SeedProductsAsync(ShopDbContext db)
    {
        var catalogue = Catalogue();
        var skus = catalogue.Select(p => p.Sku).ToList();
        var existingSkus = await db.Products
            .Where(p => skus.Contains(p.Sku))
            .Select(p => p.Sku)
            .ToListAsync();

        db.Products.AddRange(catalogue.Where(p => !existingSkus.Contains(p.Sku)));
    }

    private static async Task SeedCouponsAsync(ShopDbContext db)
    {
        var coupons = new[]
        {
            new Coupon { Code = "SAVE10", Type = CouponType.Percentage, Value = 10m, IsActive = true },
            new Coupon { Code = "WELCOME25", Type = CouponType.Percentage, Value = 25m, IsActive = true },
            new Coupon { Code = "FLAT15", Type = CouponType.FixedAmount, Value = 15m, IsActive = true },
            new Coupon { Code = "EXPIRED5", Type = CouponType.FixedAmount, Value = 5m, IsActive = false }
        };

        var codes = coupons.Select(c => c.Code).ToList();
        var existingCodes = await db.Coupons
            .Where(c => codes.Contains(c.Code))
            .Select(c => c.Code)
            .ToListAsync();

        db.Coupons.AddRange(coupons.Where(c => !existingCodes.Contains(c.Code)));
    }

    private static List<Product> Catalogue() =>
    [
        new() { Sku = "ELEC-001", Name = "Wireless Mouse", Description = "Ergonomic 2.4GHz wireless mouse with silent clicks.", Price = 24.99m, StockQuantity = 150, Category = "Electronics" },
        new() { Sku = "ELEC-002", Name = "Mechanical Keyboard", Description = "Compact 75% hot-swappable mechanical keyboard.", Price = 79.90m, StockQuantity = 80, Category = "Electronics" },
        new() { Sku = "ELEC-003", Name = "USB-C Hub 7-in-1", Description = "HDMI, USB-A, SD card reader and 100W passthrough charging.", Price = 39.50m, StockQuantity = 60, Category = "Electronics" },
        new() { Sku = "DISP-001", Name = "27\" 4K Monitor", Description = "IPS 60Hz 4K display with USB-C and height-adjustable stand.", Price = 329.00m, StockQuantity = 25, Category = "Electronics" },
        new() { Sku = "AUD-001", Name = "Noise-Cancelling Headphones", Description = "Over-ear wireless headphones with 30h battery life.", Price = 199.00m, StockQuantity = 40, Category = "Audio" },
        new() { Sku = "AUD-002", Name = "Bluetooth Speaker", Description = "Portable IPX7 waterproof speaker with deep bass.", Price = 59.99m, StockQuantity = 120, Category = "Audio" },
        new() { Sku = "ACC-001", Name = "Aluminium Laptop Stand", Description = "Adjustable stand compatible with 11-16\" laptops.", Price = 34.95m, StockQuantity = 90, Category = "Accessories" },
        new() { Sku = "ACC-002", Name = "1080p Webcam", Description = "Full-HD webcam with dual noise-reducing microphones.", Price = 45.00m, StockQuantity = 70, Category = "Accessories" },
        new() { Sku = "HOME-001", Name = "Smart LED Bulb", Description = "Dimmable colour smart bulb with app and voice control.", Price = 14.99m, StockQuantity = 200, Category = "Home" },
        new() { Sku = "HOME-002", Name = "USB Desk Lamp", Description = "Three-tone LED desk lamp with touch dimming.", Price = 27.50m, StockQuantity = 110, Category = "Home" },
        new() { Sku = "BOOK-001", Name = "Clean Architecture", Description = "Robert C. Martin's guide to software structure and design.", Price = 32.00m, StockQuantity = 300, Category = "Books" },
        new() { Sku = "BOOK-002", Name = "The Pragmatic Programmer", Description = "20th anniversary edition, Hunt & Thomas.", Price = 41.25m, StockQuantity = 250, Category = "Books" }
    ];
}
