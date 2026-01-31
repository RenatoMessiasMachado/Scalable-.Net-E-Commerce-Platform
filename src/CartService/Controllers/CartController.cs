using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using CartService.Models;

namespace CartService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CartController> _logger;

    public CartController(IConnectionMultiplexer redis, ILogger<CartController> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private string GetCartKey(Guid userId) => $"cart:{userId}";

    [HttpGet("{userId}")]
    public async Task<ActionResult<ShoppingCart>> GetCart(Guid userId)
    {
        var db = _redis.GetDatabase();
        var cartData = await db.StringGetAsync(GetCartKey(userId));

        if (cartData.IsNullOrEmpty)
        {
            return Ok(new ShoppingCart { UserId = userId });
        }

        var cart = JsonSerializer.Deserialize<ShoppingCart>(cartData!);
        return Ok(cart);
    }

    [HttpPost("{userId}/items")]
    public async Task<ActionResult<ShoppingCart>> AddToCart(Guid userId, [FromBody] AddToCartRequest request)
    {
        var db = _redis.GetDatabase();
        var cartKey = GetCartKey(userId);
        var cartData = await db.StringGetAsync(cartKey);

        ShoppingCart cart;
        if (cartData.IsNullOrEmpty)
        {
            cart = new ShoppingCart { UserId = userId };
        }
        else
        {
            cart = JsonSerializer.Deserialize<ShoppingCart>(cartData!) ?? new ShoppingCart { UserId = userId };
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = request.ProductId,
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                Price = request.Price
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;

        await db.StringSetAsync(cartKey, JsonSerializer.Serialize(cart), TimeSpan.FromDays(7));

        _logger.LogInformation($"Item added to cart for user {userId}");

        return Ok(cart);
    }

    [HttpPut("{userId}/items/{productId}")]
    public async Task<ActionResult<ShoppingCart>> UpdateCartItem(
        Guid userId,
        Guid productId,
        [FromBody] UpdateCartItemRequest request)
    {
        var db = _redis.GetDatabase();
        var cartKey = GetCartKey(userId);
        var cartData = await db.StringGetAsync(cartKey);

        if (cartData.IsNullOrEmpty)
        {
            return NotFound("Cart not found");
        }

        var cart = JsonSerializer.Deserialize<ShoppingCart>(cartData!);
        if (cart == null)
        {
            return NotFound("Cart not found");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            return NotFound("Item not found in cart");
        }

        if (request.Quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;

        await db.StringSetAsync(cartKey, JsonSerializer.Serialize(cart), TimeSpan.FromDays(7));

        _logger.LogInformation($"Cart item updated for user {userId}");

        return Ok(cart);
    }

    [HttpDelete("{userId}/items/{productId}")]
    public async Task<ActionResult<ShoppingCart>> RemoveFromCart(Guid userId, Guid productId)
    {
        var db = _redis.GetDatabase();
        var cartKey = GetCartKey(userId);
        var cartData = await db.StringGetAsync(cartKey);

        if (cartData.IsNullOrEmpty)
        {
            return NotFound("Cart not found");
        }

        var cart = JsonSerializer.Deserialize<ShoppingCart>(cartData!);
        if (cart == null)
        {
            return NotFound("Cart not found");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;

            await db.StringSetAsync(cartKey, JsonSerializer.Serialize(cart), TimeSpan.FromDays(7));

            _logger.LogInformation($"Item removed from cart for user {userId}");
        }

        return Ok(cart);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> ClearCart(Guid userId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(GetCartKey(userId));

        _logger.LogInformation($"Cart cleared for user {userId}");

        return NoContent();
    }
}
