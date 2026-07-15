namespace Shop.Api.Entities;

public class Coupon
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
}
