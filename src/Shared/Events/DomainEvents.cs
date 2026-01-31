namespace Shared.Events;

public class OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class PaymentProcessedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public class OrderShippedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}

public class UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class InventoryUpdatedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public int NewQuantity { get; set; }
}
