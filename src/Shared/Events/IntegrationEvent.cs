namespace Shared.Events;

public abstract class IntegrationEvent
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
