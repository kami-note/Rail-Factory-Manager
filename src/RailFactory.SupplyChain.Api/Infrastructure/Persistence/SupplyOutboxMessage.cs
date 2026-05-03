namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyOutboxMessage
{
    public Guid Id { get; private set; }
    public string TenantCode { get; private set; }
    public string EventType { get; private set; }
    public string CorrelationId { get; private set; }
    public string PayloadJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }

    private SupplyOutboxMessage()
    {
        TenantCode = string.Empty;
        EventType = string.Empty;
        CorrelationId = string.Empty;
        PayloadJson = string.Empty;
    }

    public SupplyOutboxMessage(Guid id, string tenantCode, string eventType, string correlationId, string payloadJson)
    {
        Id = id;
        TenantCode = tenantCode;
        EventType = eventType;
        CorrelationId = correlationId;
        PayloadJson = payloadJson;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDispatched() => DispatchedAt = DateTimeOffset.UtcNow;
}
