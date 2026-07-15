using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Entities;

namespace Shop.Api;

public static class MigrationExtensions
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var db = services.GetRequiredService<ShopDbContext>();

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Max}); retrying in 2s.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        await DbSeeder.SeedAsync(db, passwordHasher, configuration);

        logger.LogInformation("Database migrated and seeded.");
    }
}
