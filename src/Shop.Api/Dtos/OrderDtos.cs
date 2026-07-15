using System.ComponentModel.DataAnnotations;

namespace Shop.Api.Dtos;

public record OrderItemRequest(
    Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity);

public record CreateOrderRequest(
    [Required, MinLength(1)] List<OrderItemRequest> Items,
    string? CouponCode);

public record UpdateOrderRequest(
    [Required, MinLength(1)] List<OrderItemRequest> Items,
    string? CouponCode,
    string? Status);

public record OrderItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDto(
    Guid Id,
    string Status,
    IReadOnlyList<OrderItemDto> Items,
    string? CouponCode,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    DateTime CreatedAt,
    DateTime UpdatedAt);
