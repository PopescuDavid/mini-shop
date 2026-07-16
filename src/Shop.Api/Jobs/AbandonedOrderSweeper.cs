using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Entities;

namespace Shop.Api.Jobs;

public record SweepResult(int ExpiredOrders, int ReleasedUnits);

public interface IAbandonedOrderSweeper
{
    Task<SweepResult> SweepAsync(CancellationToken cancellationToken = default);
}

public class AbandonedOrderSweeper(
    ShopDbContext db,
    IOptions<SweeperOptions> options,
    ILogger<AbandonedOrderSweeper> logger) : IAbandonedOrderSweeper
{
    private readonly SweeperOptions _options = options.Value;

    public async Task<SweepResult> SweepAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-_options.DraftExpiryMinutes);
        var abandoned = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Draft && o.UpdatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (abandoned.Count == 0)
        {
            logger.LogInformation("Sweep complete: no abandoned drafts.");
            return new SweepResult(0, 0);
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

        return new SweepResult(abandoned.Count, releasedUnits);
    }
}
