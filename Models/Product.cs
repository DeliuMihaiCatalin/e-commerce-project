using System.ComponentModel.DataAnnotations;

namespace GunSlop.Models;

public enum Category
{
    Firearm = 1,
    Ammunition = 2,
    Accessory = 3
}

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [Required, MaxLength(80)]
    public string Brand { get; set; } = "";

    public Category Category { get; set; }

    /// <summary>Caliber, e.g. "9mm". Null for accessories that have none.</summary>
    [MaxLength(40)]
    public string? Caliber { get; set; }

    /// <summary>Price in cents (avoids decimal problems in SQLite).</summary>
    public int PriceCents { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = "";

    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>True for firearms/ammo: a licence notice is shown at checkout later.</summary>
    public bool RequiresLicence { get; set; }
}
