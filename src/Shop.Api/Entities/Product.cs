namespace Shop.Api.Entities;

public class Product : IAuditable
{
    public Guid Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public required string Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
