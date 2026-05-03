namespace RailFactory.Inventory.Api.Domain;

public sealed class InventoryIntegrationMessage
{
    public Guid EventId { get; private set; }
    public string TenantCode { get; private set; }
    public string EventType { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private InventoryIntegrationMessage()
    {
        TenantCode = string.Empty;
        EventType = string.Empty;
    }

    private InventoryIntegrationMessage(Guid eventId, string tenantCode, string eventType)
    {
        EventId = eventId;
        TenantCode = tenantCode;
        EventType = eventType;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public static InventoryIntegrationMessage Create(Guid eventId, string tenantCode, string eventType)
        => new(eventId, tenantCode, eventType);
}
