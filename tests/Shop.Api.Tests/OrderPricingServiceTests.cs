using Shop.Api.Entities;
using Shop.Api.Services;

namespace Shop.Api.Tests;

public class OrderPricingServiceTests
{
    private readonly OrderPricingService _pricing = new();

    private static PricingLine Line(decimal unitPrice, int quantity) => new(unitPrice, quantity);

    private static Coupon Percentage(decimal value) => new() { Code = "PCT", Type = CouponType.Percentage, Value = value, IsActive = true };

    private static Coupon Fixed(decimal value) => new() { Code = "FIX", Type = CouponType.FixedAmount, Value = value, IsActive = true };

    [Fact]
    public void Subtotal_sums_line_items_with_no_coupon()
    {
        var totals = _pricing.Calculate([Line(10.00m, 2), Line(5.50m, 3)], null);

        Assert.Equal(36.50m, totals.Subtotal);
        Assert.Equal(0m, totals.Discount);
        Assert.Equal(36.50m, totals.Total);
    }

    [Fact]
    public void Percentage_coupon_rounds_half_away_from_zero()
    {
        var totals = _pricing.Calculate([Line(9.99m, 3)], Percentage(10m));

        Assert.Equal(29.97m, totals.Subtotal);
        Assert.Equal(3.00m, totals.Discount);
        Assert.Equal(26.97m, totals.Total);
    }

    [Fact]
    public void Fixed_coupon_subtracts_amount()
    {
        var totals = _pricing.Calculate([Line(20.00m, 2)], Fixed(15m));

        Assert.Equal(40.00m, totals.Subtotal);
        Assert.Equal(15.00m, totals.Discount);
        Assert.Equal(25.00m, totals.Total);
    }

    [Fact]
    public void Fixed_coupon_larger_than_subtotal_clamps_total_to_zero()
    {
        var totals = _pricing.Calculate([Line(4.00m, 1)], Fixed(15m));

        Assert.Equal(4.00m, totals.Subtotal);
        Assert.Equal(4.00m, totals.Discount);
        Assert.Equal(0m, totals.Total);
    }

    [Fact]
    public void Percentage_coupon_never_produces_a_negative_total()
    {
        var totals = _pricing.Calculate([Line(12.34m, 1)], Percentage(100m));

        Assert.Equal(12.34m, totals.Discount);
        Assert.Equal(0m, totals.Total);
    }
}
