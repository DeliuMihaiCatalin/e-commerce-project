using GunSlop.Data;
using GunSlop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GunSlop.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private const int MaxPageSize = 50;
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db) => _db = db;

    // GET /api/products?q=falcon&categoryId=2&brand=Falcon%20Arms&caliber=9mm
    //                  &minPrice=100&maxPrice=900&inStockOnly=true&sort=price_asc&page=1&pageSize=12
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> Get([FromQuery] ProductQuery query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        IQueryable<Product> products = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        // ---- basic search: every word must match name, brand, caliber or description ----
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var words = query.Q.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(8);

            foreach (var word in words)
            {
                var w = word; // capture
                products = products.Where(p =>
                    EF.Functions.Like(p.Name, $"%{w}%") ||
                    (p.Brand != null && EF.Functions.Like(p.Brand, $"%{w}%")) ||
                    (p.Caliber != null && EF.Functions.Like(p.Caliber, $"%{w}%")) ||
                    (p.Description != null && EF.Functions.Like(p.Description, $"%{w}%")));
            }
        }

        // ---- filters ----
        if (query.CategoryId.HasValue)
        {
            var cid = query.CategoryId.Value;   // a parent category also matches its children
            products = products.Where(p => p.CategoryId == cid || p.Category.ParentId == cid);
        }

        if (!string.IsNullOrWhiteSpace(query.Brand))
        {
            var brand = query.Brand.Trim();
            products = products.Where(p => p.Brand == brand);
        }

        if (!string.IsNullOrWhiteSpace(query.Caliber))
        {
            var caliber = query.Caliber.Trim();
            products = products.Where(p => p.Caliber == caliber);
        }

        if (query.MinPrice.HasValue)
        {
            var min = (int)Math.Round(query.MinPrice.Value * 100);
            products = products.Where(p => p.PriceCents >= min);
        }

        if (query.MaxPrice.HasValue)
        {
            var max = (int)Math.Round(query.MaxPrice.Value * 100);
            products = products.Where(p => p.PriceCents <= max);
        }

        if (query.InStockOnly)
            products = products.Where(p => p.StockQuantity > 0);

        // ---- sorting ----
        products = query.Sort?.ToLowerInvariant() switch
        {
            "price_asc" => products.OrderBy(p => p.PriceCents).ThenBy(p => p.Name),
            "price_desc" => products.OrderByDescending(p => p.PriceCents).ThenBy(p => p.Name),
            "newest" => products.OrderByDescending(p => p.Id),
            _ => products.OrderBy(p => p.Name)
        };

        // ---- paging ----
        var total = await products.CountAsync();
        var rows = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<ProductDto>(rows.Select(ToDto).ToList(), page, pageSize, total));
    }

    // GET /api/products/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        return product is null ? NotFound() : Ok(ToDto(product));
    }

    // GET /api/products/filters
    [HttpGet("filters")]
    public async Task<ActionResult<FilterOptions>> GetFilters()
    {
        var categories = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.ParentId))
            .ToListAsync();

        var brands = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Brand != null)
            .Select(p => p.Brand ?? "").Distinct().OrderBy(b => b).ToListAsync();

        var calibers = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Caliber != null)
            .Select(p => p.Caliber ?? "").Distinct().OrderBy(c => c).ToListAsync();

        return Ok(new FilterOptions(categories, brands, calibers));
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Brand ?? "", p.Category.Name, p.Caliber,
        p.PriceCents / 100m, p.Description ?? "", p.ImageUrl,
        p.StockQuantity, p.StockQuantity > 0, p.RequiresLicence);
}