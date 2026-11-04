using Microsoft.EntityFrameworkCore;

namespace GunSlop.Models
{
    public enum UserRole { Customer, Admin }

    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string FullName { get; set; } = "";
        public UserRole Role { get; set; } = UserRole.Customer;
        public string? LicenceNumber { get; set; }
        public string? LicenceCountry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<CartItem> CartItems { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
    }

    /// <summary>Category tree: e.g. "Firearms" (parent) -> "Pistols" (child).</summary>
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? ParentId { get; set; }
        public Category? Parent { get; set; }

        public List<Category> Children { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }

    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string Name { get; set; } = "";
        public string? Brand { get; set; }
        public string? Description { get; set; }
        public string? Caliber { get; set; }

        /// <summary>Price in cents.</summary>
        public int PriceCents { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public bool RequiresLicence { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Review> Reviews { get; set; } = new();
    }

    public class CartItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public enum OrderStatus { Pending, Paid, Shipped, Cancelled }

    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public int TotalCents { get; set; }
        public string ShippingName { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public string ShippingCity { get; set; } = "";
        public string ShippingCountry { get; set; } = "";
        public bool LicenceConfirmed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderItem> Items { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }

        /// <summary>Price at the time of purchase (so later price changes don't alter old orders).</summary>
        public int UnitPriceCents { get; set; }
    }

    public enum PaymentStatus { Pending, Succeeded, Failed }

    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public string? StripePaymentIntentId { get; set; }
        public int AmountCents { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int Rating { get; set; }          // 1..5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

namespace GunSlop.Data
{
    using Models;


    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // ---------- Users ----------
            b.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Email).IsRequired();
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.FullName).IsRequired();
                e.Property(x => x.Role).HasConversion<string>();           // stored as 'Customer' / 'Admin'
                e.Property(x => x.CreatedAt).HasDefaultValueSql("datetime('now')");
                e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "Role IN ('Customer', 'Admin')"));
            });

            // ---------- Categories (tree) ----------
            b.Entity<Category>(e =>
            {
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Name).IsRequired();
                e.HasOne(x => x.Parent)
                 .WithMany(x => x.Children)
                 .HasForeignKey(x => x.ParentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Products ----------
            b.Entity<Product>(e =>
            {
                e.Property(x => x.Name).IsRequired();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("datetime('now')");
                e.HasOne(x => x.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(x => x.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.CategoryId).HasDatabaseName("IX_Products_Category");
                e.HasIndex(x => x.Name).HasDatabaseName("IX_Products_Name");
                e.HasIndex(x => x.Caliber).HasDatabaseName("IX_Products_Caliber");
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Products_Price", "PriceCents >= 0");
                    t.HasCheckConstraint("CK_Products_Stock", "StockQuantity >= 0");
                });
            });

            // ---------- CartItems ----------
            b.Entity<CartItem>(e =>
            {
                e.Property(x => x.AddedAt).HasDefaultValueSql("datetime('now')");
                e.HasOne(x => x.User).WithMany(u => u.CartItems)
                 .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Product).WithMany()
                 .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
                e.ToTable(t => t.HasCheckConstraint("CK_CartItems_Quantity", "Quantity > 0"));
            });

            // ---------- Orders ----------
            b.Entity<Order>(e =>
            {
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.ShippingName).IsRequired();
                e.Property(x => x.ShippingAddress).IsRequired();
                e.Property(x => x.ShippingCity).IsRequired();
                e.Property(x => x.ShippingCountry).IsRequired();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("datetime('now')");
                e.HasOne(x => x.User).WithMany(u => u.Orders)
                 .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.UserId).HasDatabaseName("IX_Orders_User");
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Orders_Status", "Status IN ('Pending', 'Paid', 'Shipped', 'Cancelled')");
                    t.HasCheckConstraint("CK_Orders_Total", "TotalCents >= 0");
                });
            });

            // ---------- OrderItems ----------
            b.Entity<OrderItem>(e =>
            {
                e.HasOne(x => x.Order).WithMany(o => o.Items)
                 .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Product).WithMany()
                 .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_OrderItems_Quantity", "Quantity > 0");
                    t.HasCheckConstraint("CK_OrderItems_Price", "UnitPriceCents >= 0");
                });
            });

            // ---------- Payments ----------
            b.Entity<Payment>(e =>
            {
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("datetime('now')");
                e.HasIndex(x => x.StripePaymentIntentId).IsUnique();
                e.HasOne(x => x.Order).WithMany(o => o.Payments)
                 .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t => t.HasCheckConstraint("CK_Payments_Status", "Status IN ('Pending', 'Succeeded', 'Failed')"));
            });

            // ---------- Reviews ----------
            b.Entity<Review>(e =>
            {
                e.Property(x => x.CreatedAt).HasDefaultValueSql("datetime('now')");
                e.HasOne(x => x.Product).WithMany(p => p.Reviews)
                 .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany(u => u.Reviews)
                 .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();
                e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "Rating BETWEEN 1 AND 5"));
            });
        }
    }
}
