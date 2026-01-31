using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using StackExchange.Redis;
using System.Text.Json;
using Shared.Messaging;
using Shared.Events;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ProductDbContext context,
        IConnectionMultiplexer redis,
        IMessageBus messageBus,
        ILogger<ProductsController> logger)
    {
        _context = context;
        _redis = redis;
        _messageBus = messageBus;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] ProductSearchQuery query)
    {
        var productsQuery = _context.Products.Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            productsQuery = productsQuery.Where(p => 
                p.Name.Contains(query.SearchTerm) || 
                (p.Description != null && p.Description.Contains(query.SearchTerm)));
        }

        if (!string.IsNullOrEmpty(query.Category))
        {
            productsQuery = productsQuery.Where(p => p.Category == query.Category);
        }

        if (query.MinPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price <= query.MaxPrice.Value);
        }

        var totalCount = await productsQuery.CountAsync();
        var products = await productsQuery
            .OrderBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return Ok(new
        {
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            Products = products
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        // Try to get from cache first
        var db = _redis.GetDatabase();
        var cacheKey = $"product:{id}";
        var cachedProduct = await db.StringGetAsync(cacheKey);

        if (!cachedProduct.IsNullOrEmpty)
        {
            var product = JsonSerializer.Deserialize<Product>(cachedProduct!);
            _logger.LogInformation($"Product retrieved from cache: {id}");
            return Ok(product);
        }

        // If not in cache, get from database
        var productFromDb = await _context.Products.FindAsync(id);

        if (productFromDb == null || !productFromDb.IsActive)
        {
            return NotFound();
        }

        // Cache the product
        await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(productFromDb), TimeSpan.FromMinutes(30));
        _logger.LogInformation($"Product cached: {id}");

        return Ok(productFromDb);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Product created: {product.Id}");

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(request.Name))
            product.Name = request.Name;
        if (request.Description != null)
            product.Description = request.Description;
        if (request.Price.HasValue)
            product.Price = request.Price.Value;
        if (request.StockQuantity.HasValue)
            product.StockQuantity = request.StockQuantity.Value;
        if (request.Category != null)
            product.Category = request.Category;
        if (request.ImageUrl != null)
            product.ImageUrl = request.ImageUrl;

        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"product:{id}");

        // Publish inventory update event
        var inventoryEvent = new InventoryUpdatedEvent
        {
            ProductId = product.Id,
            NewQuantity = product.StockQuantity
        };
        _messageBus.Publish(inventoryEvent, "ecommerce.events", "inventory.updated");

        _logger.LogInformation($"Product updated: {id}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"product:{id}");

        _logger.LogInformation($"Product deleted: {id}");

        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var categories = await _context.Products
            .Where(p => p.IsActive && p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .ToListAsync();

        return Ok(categories);
    }
}
