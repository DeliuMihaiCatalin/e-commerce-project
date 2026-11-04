using GunSlop.Models;

namespace GunSlop.Data;

/// <summary>Fictional catalog (own text, placeholder images) - see risk #3 in the plan.</summary>
public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Products.Any()) return;

        db.Products.AddRange(
            // Firearms
            P("Falcon FP-9 Compact", "Falcon Arms", Category.Firearm, "9mm", 54900, 12,
              "Compact striker-fired pistol for sport and range use. 15+1 capacity, 4.0\" barrel, 680 g unloaded, interchangeable backstraps."),
            P("Falcon FP-9 Competition", "Falcon Arms", Category.Firearm, "9mm", 79900, 5,
              "Long-slide competition pistol with adjustable sights, 5\" barrel and a light trigger tuned for sport shooting."),
            P("Ridgeback R22 Rifle", "Ridgeback", Category.Firearm, ".22 LR", 38900, 20,
              "Semi-automatic rimfire rifle, ideal for beginners and training. 10-round magazine, beech stock, 4.1 kg."),
            P("Ridgeback Hunter 308", "Ridgeback", Category.Firearm, ".308 Win", 119900, 6,
              "Bolt-action hunting rifle with a 22\" threaded barrel, adjustable trigger and 4-round detachable magazine."),
            P("Stagline Pump 12", "Stagline", Category.Firearm, "12 gauge", 42900, 9,
              "Pump-action shotgun for clay shooting and hunting. 3\" chamber, 28\" barrel, three choke tubes included."),
            P("Stagline Over-Under 20", "Stagline", Category.Firearm, "20 gauge", 149900, 0,
              "Classic over-under sporting shotgun with walnut stock and engraved receiver. Currently out of stock."),
            P("Ironwood Revolver .357", "Ironwood", Category.Firearm, ".357 Mag", 69900, 7,
              "Six-shot double-action revolver in stainless steel with a 4\" barrel, rubber grips and fixed sights."),

            // Ammunition
            P("Falcon Range 9mm FMJ (50 rds)", "Falcon Arms", Category.Ammunition, "9mm", 2490, 400,
              "115 gr full metal jacket target ammunition. Clean-burning and consistent for range practice."),
            P("Falcon Defense 9mm JHP (20 rds)", "Falcon Arms", Category.Ammunition, "9mm", 2190, 150,
              "124 gr jacketed hollow point, reliable expansion. Sold in boxes of 20."),
            P("Ridgeback .22 LR Match (100 rds)", "Ridgeback", Category.Ammunition, ".22 LR", 1190, 900,
              "40 gr lead round nose rimfire ammunition for target and plinking."),
            P("Ridgeback .308 Soft Point (20 rds)", "Ridgeback", Category.Ammunition, ".308 Win", 3490, 220,
              "150 gr soft point hunting ammunition with controlled expansion."),
            P("Stagline Clay Load 12ga (25 rds)", "Stagline", Category.Ammunition, "12 gauge", 1090, 600,
              "28 g #7.5 clay target load, low recoil."),
            P("Ironwood .357 Mag JSP (50 rds)", "Ironwood", Category.Ammunition, ".357 Mag", 4290, 80,
              "158 gr jacketed soft point revolver ammunition."),

            // Accessories
            P("Sentinel Hearing Protection Muffs", "Sentinel", Category.Accessory, null, 2990, 60,
              "Passive earmuffs, NRR 26 dB, foldable design with padded headband."),
            P("Sentinel Shooting Glasses", "Sentinel", Category.Accessory, null, 1990, 80,
              "Impact-resistant anti-fog lenses, clear and amber lens set included.", licence: false),
            P("Sentinel Electronic Ear Muffs", "Sentinel", Category.Accessory, null, 8990, 25,
              "Electronic hearing protection amplifies low sounds and blocks loud noise instantly."),
            P("Vault Pistol Case (Hard)", "Vault", Category.Accessory, "9mm", 4990, 30,
              "Lockable hard case with foam interior for one pistol and two magazines."),
            P("Vault Rifle Safe 5-Gun", "Vault", Category.Accessory, null, 38900, 4,
              "Steel gun safe with electronic lock, fits five long guns. 62 kg."),
            P("CleanPro Cleaning Kit 9mm", "CleanPro", Category.Accessory, "9mm", 2490, 70,
              "Complete cleaning kit: rod, brushes, patches, solvent and carry pouch."),
            P("CleanPro Cleaning Kit .22", "CleanPro", Category.Accessory, ".22 LR", 1790, 55,
              "Compact rimfire cleaning kit with bore brush, jag and patches."),
            P("Optix Red Dot Sight 3 MOA", "Optix", Category.Accessory, null, 17900, 18,
              "Waterproof reflex sight with 3 MOA dot, 12 brightness settings and 50,000 hour battery life."),
            P("Optix Rifle Scope 3-9x40", "Optix", Category.Accessory, null, 12900, 0,
              "Variable zoom rifle scope with duplex reticle and parallax adjustment. Currently out of stock.")
        );
        db.SaveChanges();
    }

    private static Product P(string name, string brand, Category cat, string? caliber,
        int cents, int stock, string desc, bool? licence = null) => new()
    {
        Name = name,
        Brand = brand,
        Category = cat,
        Caliber = caliber,
        PriceCents = cents,
        StockQuantity = stock,
        Description = desc,
        RequiresLicence = licence ?? (cat != Category.Accessory),
        ImageUrl = $"https://placehold.co/600x400?text={Uri.EscapeDataString(name)}"
    };
}
