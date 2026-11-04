using System.ComponentModel.DataAnnotations;

namespace GunSlop.Models;

public class AddItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 99)] public int Quantity { get; set; } = 1;
}

public class UpdateItemRequest
{
    /// <summary>0 removes the item.</summary>
    [Range(0, 99)] public int Quantity { get; set; }
}

public record CartItemDto(
    int ProductId, string Name, string Brand, string? ImageUrl,
    decimal UnitPrice, int Quantity, decimal LineTotal, int StockQuantity);

public record CartDto(
    IReadOnlyList<CartItemDto> Items, int ItemCount, decimal Subtotal, bool RequiresLicence);
