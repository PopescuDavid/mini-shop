using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Entities;

namespace Shop.Api.Jobs;

public class AbandonedOrderSweeper(
    IServiceScopeFactory scopeFactory,
    IOptions<SweeperOptions> options,
    ILogger<AbandonedOrderSweeper> logger) : BackgroundService
{
    private readonly SweeperOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Abandoned-order sweeper started (interval {Interval}s, expiry threshold {Threshold}min).",
            _options.IntervalSeconds, _options.DraftExpiryMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Abandoned-order sweep failed.");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var cutoff = DateTime.UtcNow.AddMinutes(-_options.DraftExpiryMinutes);
        var abandoned = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Draft && o.UpdatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (abandoned.Count == 0)
        {
            logger.LogInformation("Sweep complete: no abandoned drafts.");
            return;
        }

        var productIds = abandoned.SelectMany(o => o.Items.Select(i => i.ProductId)).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var releasedUnits = 0;
        foreach (var order in abandoned)
        {
            foreach (var item in order.Items)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                {
                    product.StockQuantity += item.Quantity;
                    releasedUnits += item.Quantity;
                }
            }

            order.Status = OrderStatus.Expired;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Sweep complete: expired {OrderCount} draft(s), released {Units} reserved unit(s).",
            abandoned.Count, releasedUnits);
    }
}
