using GunSlop.Data;
using GunSlop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GunSlop.Controllers;

/// <summary>
/// Cart of the current user (one row per product in CartItems).
/// TODO (week 4): replace CurrentUserId() with the id of the logged-in user (from the auth token)
/// and add [Authorize]. Until accounts exist, everyone shares the seeded demo user.
/// </summary>
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;

    private Task<int> CurrentUserId() =>
        _db.Users.Where(u => u.Email == SeedData.DemoEmail).Select(u => u.Id).FirstAsync();

    private async Task<List<CartItem>> LoadItems(int userId) =>
        await _db.CartItems.Include(i => i.Product)
            .Where(i => i.UserId == userId).OrderBy(i => i.Id).ToListAsync();

    // GET /api/cart
    [HttpGet]
    public async Task<ActionResult<CartDto>> Get()
        => Ok(ToDto(await LoadItems(await CurrentUserId())));

    // POST /api/cart/items   { "productId": 1, "quantity": 1 }
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddItemRequest req)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == req.ProductId && p.IsActive);
        if (product is null) return NotFound(new { message = "Product not found." });
        if (product.StockQuantity <= 0) return BadRequest(new { message = $"{product.Name} is out of stock." });

        var userId = await CurrentUserId();
        var items = await LoadItems(userId);

        var item = items.FirstOrDefault(i => i.ProductId == product.Id);
        var newQty = (item?.Quantity ?? 0) + req.Quantity;
        if (newQty > product.StockQuantity)
            return BadRequest(new { message = $"Only {product.StockQuantity} of {product.Name} in stock." });

        if (item is null)
        {
            item = new CartItem { UserId = userId, ProductId = product.Id, Product = product, Quantity = req.Quantity };
            _db.CartItems.Add(item);
            items.Add(item);
        }
        else
        {
            item.Quantity = newQty;
        }

        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { message = "Cart changed, please try again." }); }

        return Ok(ToDto(items));
    }

    // PUT /api/cart/items/5   { "quantity": 3 }   (0 removes)
    [HttpPut("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int productId, [FromBody] UpdateItemRequest req)
    {
        var items = await LoadItems(await CurrentUserId());
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return NotFound(new { message = "Item not in cart." });

        if (req.Quantity == 0)
        {
            _db.CartItems.Remove(item);
            items.Remove(item);
        }
        else
        {
            if (req.Quantity > item.Product.StockQuantity)
                return BadRequest(new { message = $"Only {item.Product.StockQuantity} of {item.Product.Name} in stock." });
            item.Quantity = req.Quantity;
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(items));
    }

    // DELETE /api/cart/items/5
    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int productId)
    {
        var items = await LoadItems(await CurrentUserId());
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return NotFound(new { message = "Item not in cart." });

        _db.CartItems.Remove(item);
        items.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(ToDto(items));
    }

    // DELETE /api/cart   (empty the cart)
    [HttpDelete]
    public async Task<ActionResult<CartDto>> Clear()
    {
        var items = await LoadItems(await CurrentUserId());
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
        return Ok(ToDto(new List<CartItem>()));
    }

    private static CartDto ToDto(List<CartItem> items)
    {
        var lines = items.Select(i => new CartItemDto(
            i.ProductId, i.Product.Name, i.Product.Brand ?? "", i.Product.ImageUrl,
            i.Product.PriceCents / 100m, i.Quantity,
            i.Product.PriceCents * i.Quantity / 100m, i.Product.StockQuantity)).ToList();

        var subtotalCents = items.Sum(i => (long)i.Product.PriceCents * i.Quantity);
        return new CartDto(lines, items.Sum(i => i.Quantity),
            subtotalCents / 100m, items.Any(i => i.Product.RequiresLicence));
    }
}
