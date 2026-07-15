namespace Shop.Api.Dtos;

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class ProductQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
