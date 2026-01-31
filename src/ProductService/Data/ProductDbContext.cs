using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Seed data
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Laptop Dell XPS 15",
                Description = "High-performance laptop with Intel i7, 16GB RAM, 512GB SSD",
                Price = 1499.99m,
                StockQuantity = 50,
                Category = "Electronics",
                ImageUrl = "https://example.com/laptop.jpg",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Smartphone Samsung Galaxy S23",
                Description = "Latest Samsung flagship with amazing camera",
                Price = 999.99m,
                StockQuantity = 100,
                Category = "Electronics",
                ImageUrl = "https://example.com/phone.jpg",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Wireless Headphones Sony WH-1000XM5",
                Description = "Premium noise-cancelling headphones",
                Price = 399.99m,
                StockQuantity = 75,
                Category = "Electronics",
                ImageUrl = "https://example.com/headphones.jpg",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }
        );
    }
}
