using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Dtos;
using Shop.Api.Entities;

namespace Shop.Api.Controllers;

[ApiController]
[Route("products")]
public class ProductsController(ShopDbContext db) : ControllerBase
{
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> List([FromQuery] ProductQueryParams query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var descending = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        var source = Sort(db.Products.AsNoTracking(), query.SortBy, descending);
        var totalCount = await source.CountAsync();

        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.Price, p.StockQuantity, p.Category, p.CreatedAt, p.UpdatedAt))
            .ToListAsync();

        return Ok(new PagedResult<ProductDto>(items, page, pageSize, totalCount));
    }

    private static IQueryable<Product> Sort(IQueryable<Product> query, string? sortBy, bool descending)
    {
        var ordered = sortBy?.ToLowerInvariant() switch
        {
            "price" => descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "stock" or "stockquantity" => descending ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
            "category" => descending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
            "createdat" => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        return ordered.ThenBy(p => p.Id);
    }
}
