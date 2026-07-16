using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Data;
using Shop.Api.Dtos;
using Shop.Api.Entities;
using Shop.Api.Jobs;

namespace Shop.Api.Tests;

public class OrderLifecycleTests(ShopApiFactory factory) : IClassFixture<ShopApiFactory>
{
    private const string Email = "demo@shop.test";
    private const string Password = "Passw0rd!";
    private const string SkuA = "BOOK-001";
    private const string SkuB = "BOOK-002";

    [Fact]
    public async Task Increasing_a_line_quantity_reserves_additional_stock()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);
        var before = product.StockQuantity;

        var order = await CreateOrderAsync(client, (product.Id, 2));
        Assert.Equal(before - 2, await StockAsync(client, SkuA));

        var updated = await UpdateOrderAsync(client, order.Id, items: [(product.Id, 5)]);

        Assert.Equal(5, Assert.Single(updated.Items).Quantity);
        Assert.Equal(before - 5, await StockAsync(client, SkuA));
    }

    [Fact]
    public async Task Decreasing_a_line_quantity_releases_stock()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);
        var before = product.StockQuantity;

        var order = await CreateOrderAsync(client, (product.Id, 5));
        var updated = await UpdateOrderAsync(client, order.Id, items: [(product.Id, 2)]);

        Assert.Equal(2, Assert.Single(updated.Items).Quantity);
        Assert.Equal(before - 2, await StockAsync(client, SkuA));
    }

    [Fact]
    public async Task Removing_a_line_restores_its_stock()
    {
        var client = await AuthenticatedClientAsync();
        var a = await ProductAsync(client, SkuA);
        var b = await ProductAsync(client, SkuB);
        var beforeB = b.StockQuantity;

        var order = await CreateOrderAsync(client, (a.Id, 2), (b.Id, 3));
        Assert.Equal(beforeB - 3, await StockAsync(client, SkuB));

        var updated = await UpdateOrderAsync(client, order.Id, items: [(a.Id, 2)]);

        Assert.Equal(a.Id, Assert.Single(updated.Items).ProductId);
        Assert.Equal(beforeB, await StockAsync(client, SkuB));
    }

    [Fact]
    public async Task Adding_a_line_reserves_stock_for_the_new_product()
    {
        var client = await AuthenticatedClientAsync();
        var a = await ProductAsync(client, SkuA);
        var b = await ProductAsync(client, SkuB);
        var beforeB = b.StockQuantity;

        var order = await CreateOrderAsync(client, (a.Id, 1));
        var updated = await UpdateOrderAsync(client, order.Id, items: [(a.Id, 1), (b.Id, 2)]);

        Assert.Equal(2, updated.Items.Count);
        Assert.Equal(beforeB - 2, await StockAsync(client, SkuB));
    }

    [Fact]
    public async Task A_coupon_can_be_applied_and_removed_during_update()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);

        var order = await CreateOrderAsync(client, (product.Id, 2));
        Assert.Null(order.CouponCode);
        Assert.Equal(0m, order.Discount);

        var discounted = await UpdateOrderAsync(client, order.Id, items: [(product.Id, 2)], couponCode: "SAVE10");
        Assert.Equal("SAVE10", discounted.CouponCode);
        Assert.True(discounted.Discount > 0m);

        var cleared = await UpdateOrderAsync(client, order.Id, items: [(product.Id, 2)], couponCode: null);
        Assert.Null(cleared.CouponCode);
        Assert.Equal(0m, cleared.Discount);
    }

    [Fact]
    public async Task A_draft_can_be_placed_and_is_then_read_only()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);

        var order = await CreateOrderAsync(client, (product.Id, 1));
        var placed = await UpdateOrderAsync(client, order.Id, items: [(product.Id, 1)], status: "Placed");
        Assert.Equal("Placed", placed.Status);

        var response = await PutOrderAsync(client, order.Id, items: [(product.Id, 2)]);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_an_order_releases_stock_and_hides_it()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);
        var before = product.StockQuantity;

        var order = await CreateOrderAsync(client, (product.Id, 4));
        Assert.Equal(before - 4, await StockAsync(client, SkuA));

        var delete = await client.DeleteAsync($"/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(before, await StockAsync(client, SkuA));

        var fetch = await client.GetAsync($"/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Duplicate_product_lines_are_consolidated()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);
        var before = product.StockQuantity;

        var order = await CreateOrderAsync(client, (product.Id, 2), (product.Id, 3));

        var line = Assert.Single(order.Items);
        Assert.Equal(5, line.Quantity);
        Assert.Equal(before - 5, await StockAsync(client, SkuA));
    }

    [Fact]
    public async Task An_invalid_quantity_is_rejected()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);

        var response = await client.PostAsJsonAsync("/orders", new
        {
            items = new[] { new { productId = product.Id, quantity = 0 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Editing_a_coupon_does_not_change_an_existing_orders_total()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);

        var order = await CreateOrderAsync(client, (product.Id, 2));
        var discounted = await UpdateOrderAsync(client, order.Id, items: [(product.Id, 2)], couponCode: "WELCOME25");
        Assert.True(discounted.Discount > 0m);

        try
        {
            await MutateCouponValueAsync("WELCOME25", 90m);

            var refetched = await client.GetFromJsonAsync<OrderDto>($"/orders/{order.Id}");
            Assert.Equal(discounted.Discount, refetched!.Discount);
            Assert.Equal(discounted.Total, refetched.Total);
        }
        finally
        {
            await MutateCouponValueAsync("WELCOME25", 25m);
        }
    }

    [Fact]
    public async Task Negative_stock_is_rejected_by_the_database()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.Products.Add(new Product
        {
            Sku = $"NEG-{Guid.NewGuid():N}",
            Name = "Constraint probe",
            Description = "Should violate CK_Products_StockQuantity.",
            Category = "Test",
            Price = 1.00m,
            StockQuantity = -1
        });

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task The_sweeper_expires_stale_drafts_and_releases_their_stock()
    {
        var client = await AuthenticatedClientAsync();
        var product = await ProductAsync(client, SkuA);
        var before = product.StockQuantity;

        var order = await CreateOrderAsync(client, (product.Id, 3));
        Assert.Equal(before - 3, await StockAsync(client, SkuA));

        await BackdateOrderAsync(order.Id, TimeSpan.FromHours(1));

        SweepResult result;
        using (var scope = factory.Services.CreateScope())
        {
            var sweeper = scope.ServiceProvider.GetRequiredService<IAbandonedOrderSweeper>();
            result = await sweeper.SweepAsync();
        }

        Assert.True(result.ExpiredOrders >= 1);
        Assert.Equal(before, await StockAsync(client, SkuA));

        var expired = await client.GetFromJsonAsync<OrderDto>($"/orders/{order.Id}");
        Assert.Equal("Expired", expired!.Status);
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

    private static async Task<ProductDto> ProductAsync(HttpClient client, string sku)
    {
        var products = await client.GetFromJsonAsync<PagedResult<ProductDto>>("/products?page=1&pageSize=100");
        return products!.Items.First(p => p.Sku == sku);
    }

    private static async Task<int> StockAsync(HttpClient client, string sku)
        => (await ProductAsync(client, sku)).StockQuantity;

    private static async Task<OrderDto> CreateOrderAsync(HttpClient client, params (Guid ProductId, int Quantity)[] items)
    {
        var response = await client.PostAsJsonAsync("/orders", new
        {
            items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity })
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private static async Task<OrderDto> UpdateOrderAsync(
        HttpClient client, Guid orderId, (Guid ProductId, int Quantity)[] items, string? couponCode = null, string? status = null)
    {
        var response = await PutOrderAsync(client, orderId, items, couponCode, status);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private static Task<HttpResponseMessage> PutOrderAsync(
        HttpClient client, Guid orderId, (Guid ProductId, int Quantity)[] items, string? couponCode = null, string? status = null)
        => client.PutAsJsonAsync($"/orders/{orderId}", new
        {
            items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }),
            couponCode,
            status
        });

    private async Task MutateCouponValueAsync(string code, decimal value)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await db.Coupons
            .Where(c => c.Code == code)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Value, value));
    }

    private async Task BackdateOrderAsync(Guid orderId, TimeSpan age)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var cutoff = DateTime.UtcNow - age;
        await db.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.UpdatedAt, cutoff));
    }
}
