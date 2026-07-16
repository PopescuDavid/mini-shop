using Shop.Api.Entities;

namespace Shop.Api.Services;

public record PricingLine(decimal UnitPrice, int Quantity);

public record OrderTotals(decimal Subtotal, decimal Discount, decimal Total);

public interface IOrderPricingService
{
    decimal Subtotal(IEnumerable<PricingLine> lines);
    OrderTotals Calculate(IEnumerable<PricingLine> lines, Coupon? coupon);
}

public class OrderPricingService : IOrderPricingService
{
    public decimal Subtotal(IEnumerable<PricingLine> lines) => Round(lines.Sum(line => line.UnitPrice * line.Quantity));

    public OrderTotals Calculate(IEnumerable<PricingLine> lines, Coupon? coupon)
    {
        var subtotal = Subtotal(lines);
        var discount = coupon is null ? 0m : Round(DiscountFor(subtotal, coupon));
        discount = Math.Clamp(discount, 0m, subtotal);

        return new OrderTotals(subtotal, discount, subtotal - discount);
    }

    private static decimal DiscountFor(decimal subtotal, Coupon coupon) => coupon.Type switch
    {
        CouponType.Percentage => subtotal * coupon.Value / 100m,
        CouponType.FixedAmount => coupon.Value,
        _ => 0m
    };

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
