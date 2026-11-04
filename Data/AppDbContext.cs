using GunSlop.Models;
using Microsoft.EntityFrameworkCore;

namespace GunSlop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.Property(p => p.Category).HasConversion<int>();
            e.HasIndex(p => p.Name);
            e.HasIndex(p => p.Category);
            e.HasIndex(p => p.Brand);
        });
    }
}
