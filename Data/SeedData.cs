using GunSlop.Models;

namespace GunSlop.Data;

/// <summary>Fictional sample data (own text, no real products).</summary>
public static class SeedData
{
    /// <summary>
    /// The cart uses this user until real login exists (week 4).
    /// See CartController.CurrentUserId().
    /// </summary>
    public const string DemoEmail = "customer@gunslop.test";

    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Products.Any()) return;

        // ---------- Users ----------
        // TODO: replace "REPLACE_WITH_HASH" with real password hashes once accounts are built.
        var admin = new User
        {
            Email = "admin@gunslop.test",
            PasswordHash = "REPLACE_WITH_HASH",
            FullName = "Store Admin",
            Role = UserRole.Admin
        };
        var customer = new User
        {
            Email = DemoEmail,
            PasswordHash = "REPLACE_WITH_HASH",
            FullName = "Test Customer",
            Role = UserRole.Customer,
            LicenceNumber = "LIC-0001",
            LicenceCountry = "RO"
        };
        db.Users.AddRange(admin, customer);

        // ---------- Categories ----------
        var firearms    = new Category { Name = "Firearms" };
        var ammunition  = new Category { Name = "Ammunition" };
        var accessories = new Category { Name = "Accessories" };

        var pistols  = new Category { Name = "Pistols",  Parent = firearms };
        var rifles   = new Category { Name = "Rifles",   Parent = firearms };
        var shotguns = new Category { Name = "Shotguns", Parent = firearms };
        var optics   = new Category { Name = "Optics",   Parent = accessories };
        var cleaning = new Category { Name = "Cleaning Kits", Parent = accessories };

        db.Categories.AddRange(firearms, ammunition, accessories, pistols, rifles, shotguns, optics, cleaning);

        // ---------- Products ----------
        Product P(Category cat, string name, string brand, string desc, string? caliber,
                  int priceCents, int stock, bool requiresLicence) => new()
        {
            Category = cat,
            Name = name,
            Brand = brand,
            Description = desc,
            Caliber = caliber,
            PriceCents = priceCents,
            StockQuantity = stock,
            RequiresLicence = requiresLicence,
            // placeholder picture (replace with licensed images later)
            ImageUrl = $"https://placehold.co/600x400?text={Uri.EscapeDataString(name)}"
        };

        var redDot = P(optics, "Red Dot Sight RD-1", "ClearView",
            "Fictional compact red dot optic.", null, 15900, 25, false);

        db.Products.AddRange(
            P(pistols,    "Falcon P9 Pistol",         "Falcon Arms", "Fictional compact 9mm pistol for sport shooting.", "9mm",      54900,  10, true),
            P(rifles,     "Ridge Hunter Rifle",       "Ridgeline",   "Fictional bolt-action hunting rifle.",              ".308",     89900,   6, true),
            P(shotguns,   "Stormfield Pump Shotgun",  "Stormfield",  "Fictional pump-action shotgun for clay shooting.",  "12 gauge", 42900,   8, true),
            P(ammunition, "Range Pack 9mm (50 rds)",  "Falcon Arms", "Fictional practice ammunition box.",                "9mm",       2900, 200, true),
            P(ammunition, "Hunter Pack .308 (20 rds)","Ridgeline",   "Fictional hunting ammunition box.",                 ".308",      3400, 150, true),
            redDot,
            P(cleaning,   "Universal Cleaning Kit",   "ClearView",   "Fictional multi-caliber cleaning kit.",             null,        2400,  60, false)
        );

        // ---------- Reviews ----------
        db.Reviews.Add(new Review
        {
            Product = redDot,
            User = customer,
            Rating = 5,
            Comment = "Sample review: clear dot, easy to mount."
        });

        db.SaveChanges();
    }
}
