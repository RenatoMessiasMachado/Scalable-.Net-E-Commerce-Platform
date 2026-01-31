namespace CartService.Models;

public class ShoppingCart
{
    public Guid UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class CartItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class AddToCartRequest
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}
