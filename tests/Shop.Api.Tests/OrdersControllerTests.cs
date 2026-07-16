using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Data;
using Shop.Api.Dtos;
using Shop.Api.Entities;

namespace Shop.Api.Tests;

public class OrdersControllerTests(ShopApiFactory factory) : IClassFixture<ShopApiFactory>
{
    private const string Email = "demo@shop.test";
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Login_returns_a_token()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = Email, password = Password });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_with_an_unknown_email_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@shop.test", password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Order_endpoints_require_authentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Creating_an_order_computes_totals_and_persists_it()
    {
        var client = await AuthenticatedClientAsync();
        var productId = await FirstProductIdAsync(client);

        var create = await client.PostAsJsonAsync("/orders", new
        {
            items = new[] { new { productId, quantity = 2 } },
            couponCode = "SAVE10"
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<OrderDto>();

        var fetched = await client.GetFromJsonAsync<OrderDto>($"/orders/{created!.Id}");

        Assert.Equal("Draft", fetched!.Status);
        Assert.Single(fetched.Items);
        Assert.Equal("SAVE10", fetched.CouponCode);
        Assert.True(fetched.Discount > 0m);
        Assert.Equal(fetched.Subtotal - fetched.Discount, fetched.Total);
    }

    [Fact]
    public async Task Creating_an_order_beyond_available_stock_is_rejected()
    {
        var client = await AuthenticatedClientAsync();
        var productId = await FirstProductIdAsync(client);

        var response = await client.PostAsJsonAsync("/orders", new
        {
            items = new[] { new { productId, quantity = 1_000_000 } }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task An_order_owned_by_another_user_is_not_found()
    {
        var strangerOrderId = await SeedStrangerOrderAsync();
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/orders/{strangerOrderId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login", new { email = Email, password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body!.Token);
        return client;
    }

    private static async Task<Guid> FirstProductIdAsync(HttpClient client)
    {
        var products = await client.GetFromJsonAsync<PagedResult<ProductDto>>("/products?page=1&pageSize=1");
        return products!.Items[0].Id;
    }

    private async Task<Guid> SeedStrangerOrderAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var product = db.Products.First();
        var stranger = new User { Email = $"stranger-{Guid.NewGuid():N}@shop.test", PasswordHash = "x" };
        var order = new Order
        {
            User = stranger,
            Status = OrderStatus.Draft,
            Items = { new OrderItem { ProductId = product.Id, Quantity = 1, UnitPrice = product.Price } }
        };
        db.Users.Add(stranger);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order.Id;
    }
}
