using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shop.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=shop;Username=shop;Password=shop";

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ShopDbContext(options);
    }
}
