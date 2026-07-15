namespace Shop.Api.Entities;

public class Order : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public Guid? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
