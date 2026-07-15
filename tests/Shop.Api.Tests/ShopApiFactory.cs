using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Shop.Api.Tests;

public class ShopApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Jwt:Issuer", "shop-api-tests");
        builder.UseSetting("Jwt:Audience", "shop-client-tests");
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-at-least-32-bytes-long");
        builder.UseSetting("Sweeper:IntervalSeconds", "3600");
    }

    Task IAsyncLifetime.InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
