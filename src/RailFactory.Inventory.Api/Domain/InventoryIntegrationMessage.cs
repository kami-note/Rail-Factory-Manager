namespace RailFactory.Inventory.Api.Domain;

public sealed class InventoryIntegrationMessage
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private InventoryIntegrationMessage()
    {
        EventType = string.Empty;
    }

    private InventoryIntegrationMessage(Guid eventId, string eventType)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public static InventoryIntegrationMessage Create(Guid eventId, string eventType)
        => new(eventId, eventType);
}
