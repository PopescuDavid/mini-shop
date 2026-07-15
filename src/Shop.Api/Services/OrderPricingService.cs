using Shop.Api.Entities;

namespace Shop.Api.Services;

public record PricingLine(decimal UnitPrice, int Quantity);

public record OrderTotals(decimal Subtotal, decimal Discount, decimal Total);

public interface IOrderPricingService
{
    OrderTotals Calculate(IEnumerable<PricingLine> lines, Coupon? coupon);
}

public class OrderPricingService : IOrderPricingService
{
    public OrderTotals Calculate(IEnumerable<PricingLine> lines, Coupon? coupon)
    {
        var subtotal = Round(lines.Sum(line => line.UnitPrice * line.Quantity));
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
