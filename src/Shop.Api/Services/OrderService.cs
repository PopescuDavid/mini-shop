using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Dtos;
using Shop.Api.Entities;
using Shop.Api.ExceptionHandling;

namespace Shop.Api.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(Guid userId, CreateOrderRequest request);
    Task<OrderDto> GetAsync(Guid userId, Guid orderId);
    Task<OrderDto> UpdateAsync(Guid userId, Guid orderId, UpdateOrderRequest request);
    Task DeleteAsync(Guid userId, Guid orderId);
}

public class OrderService(ShopDbContext db, IOrderPricingService pricing) : IOrderService
{
    public async Task<OrderDto> CreateAsync(Guid userId, CreateOrderRequest request)
    {
        var quantities = Consolidate(request.Items);
        var products = await LoadProductsAsync(quantities.Keys);

        var order = new Order { UserId = userId, Status = OrderStatus.Draft };

        foreach (var (productId, quantity) in quantities)
        {
            var product = products[productId];
            EnsureStock(product, quantity);
            product.StockQuantity -= quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }

        order.Coupon = await ResolveCouponAsync(request.CouponCode);
        order.CouponId = order.Coupon?.Id;
        SnapshotDiscount(order);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return ToDto(order);
    }

    public async Task<OrderDto> GetAsync(Guid userId, Guid orderId)
    {
        var order = await QueryOwned(userId).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        return ToDto(order);
    }

    public async Task<OrderDto> UpdateAsync(Guid userId, Guid orderId, UpdateOrderRequest request)
    {
        var order = await QueryOwned(userId).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Draft)
            throw new ConflictException("Only draft orders can be modified.");

        var desired = Consolidate(request.Items);
        var affectedIds = desired.Keys.Union(order.Items.Select(i => i.ProductId));
        var products = await LoadProductsAsync(affectedIds);

        foreach (var item in order.Items.ToList())
        {
            if (desired.ContainsKey(item.ProductId))
                continue;

            products[item.ProductId].StockQuantity += item.Quantity;
            order.Items.Remove(item);
        }

        foreach (var (productId, quantity) in desired)
        {
            var product = products[productId];
            var existing = order.Items.FirstOrDefault(i => i.ProductId == productId);
            var delta = quantity - (existing?.Quantity ?? 0);

            if (delta > 0)
                EnsureStock(product, delta);
            product.StockQuantity -= delta;

            if (existing is null)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
            }
            else
            {
                existing.Quantity = quantity;
            }
        }

        order.Coupon = await ResolveCouponAsync(request.CouponCode);
        order.CouponId = order.Coupon?.Id;
        SnapshotDiscount(order);

        ApplyStatusTransition(order, request.Status);

        await db.SaveChangesAsync();

        return ToDto(order);
    }

    public async Task DeleteAsync(Guid userId, Guid orderId)
    {
        var order = await QueryOwned(userId).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status is OrderStatus.Draft or OrderStatus.Placed)
        {
            var products = await LoadProductsAsync(order.Items.Select(i => i.ProductId));
            foreach (var item in order.Items)
                products[item.ProductId].StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;
        order.IsDeleted = true;

        await db.SaveChangesAsync();
    }

    private IQueryable<Order> QueryOwned(Guid userId) => db.Orders
        .Include(o => o.Coupon)
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .Where(o => o.UserId == userId);

    private static Dictionary<Guid, int> Consolidate(IEnumerable<OrderItemRequest> items)
    {
        var quantities = items
            .GroupBy(i => i.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(i => i.Quantity));

        if (quantities.Count == 0)
            throw new ValidationException("An order must contain at least one item.");

        return quantities;
    }

    private async Task<Dictionary<Guid, Product>> LoadProductsAsync(IEnumerable<Guid> productIds)
    {
        var ids = productIds.Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var missing = ids.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new ValidationException($"Unknown product(s): {string.Join(", ", missing)}.");

        return products;
    }

    private static void EnsureStock(Product product, int quantity)
    {
        if (product.StockQuantity < quantity)
            throw new ConflictException($"Insufficient stock for '{product.Name}'. Available {product.StockQuantity}, requested {quantity}.");
    }

    private async Task<Coupon?> ResolveCouponAsync(string? couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return null;

        var code = couponCode.Trim().ToUpperInvariant();
        return await db.Coupons.FirstOrDefaultAsync(c => c.Code == code && c.IsActive)
            ?? throw new ValidationException($"Coupon '{couponCode}' is invalid or inactive.");
    }

    private static void ApplyStatusTransition(Order order, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return;

        if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var target))
            throw new ValidationException($"Unknown status '{status}'.");

        if (target == order.Status)
            return;

        if (target != OrderStatus.Placed)
            throw new ValidationException("A draft order can only be transitioned to 'Placed'.");

        order.Status = OrderStatus.Placed;
    }

    private void SnapshotDiscount(Order order) =>
        order.DiscountAmount = pricing.Calculate(Lines(order), order.Coupon).Discount;

    private OrderDto ToDto(Order order)
    {
        // Read against the snapshotted discount, not the live coupon, so a placed
        // order's total never shifts if the coupon is edited afterwards.
        var subtotal = pricing.Subtotal(Lines(order));
        var discount = Math.Clamp(order.DiscountAmount, 0m, subtotal);

        var items = order.Items
            .OrderBy(i => i.Product.Name)
            .Select(i => new OrderItemDto(
                i.ProductId,
                i.Product.Sku,
                i.Product.Name,
                i.Quantity,
                i.UnitPrice,
                pricing.Subtotal([new PricingLine(i.UnitPrice, i.Quantity)])))
            .ToList();

        return new OrderDto(
            order.Id,
            order.Status.ToString(),
            items,
            order.Coupon?.Code,
            subtotal,
            discount,
            subtotal - discount,
            order.CreatedAt,
            order.UpdatedAt);
    }

    private static IEnumerable<PricingLine> Lines(Order order) =>
        order.Items.Select(i => new PricingLine(i.UnitPrice, i.Quantity));
}
