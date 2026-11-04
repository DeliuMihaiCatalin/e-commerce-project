namespace GunSlop.Models;

/// <summary>What the API returns (never expose the entity directly).</summary>
public record ProductDto(
    int Id,
    string Name,
    string Brand,
    string Category,
    string? Caliber,
    decimal Price,
    string Description,
    string? ImageUrl,
    int StockQuantity,
    bool InStock,
    bool RequiresLicence);

/// <summary>Query-string parameters for GET /api/products.</summary>
public class ProductQuery
{
    /// <summary>Basic search text (name, brand, caliber, description).</summary>
    public string? Q { get; set; }
    public Category? Category { get; set; }
    public string? Brand { get; set; }
    public string? Caliber { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }

    /// <summary>name | price_asc | price_desc | newest</summary>
    public string Sort { get; set; } = "name";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record FilterOptions(
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Brands,
    IReadOnlyList<string> Calibers);
