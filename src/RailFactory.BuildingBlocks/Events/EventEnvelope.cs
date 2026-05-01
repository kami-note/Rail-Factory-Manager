namespace RailFactory.BuildingBlocks.Events;

public sealed record EventEnvelope<TPayload>(
    Guid EventId,
    string EventType,
    int EventVersion,
    DateTimeOffset OccurredAt,
    string TenantCode,
    string CorrelationId,
    string Producer,
    TPayload Payload)
{
    public static EventEnvelope<TPayload> Create(
        string eventType,
        int eventVersion,
        string tenantCode,
        string correlationId,
        string producer,
        TPayload payload,
        DateTimeOffset? occurredAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);

        return new EventEnvelope<TPayload>(
            Guid.NewGuid(),
            eventType,
            eventVersion,
            occurredAt ?? DateTimeOffset.UtcNow,
            tenantCode,
            correlationId,
            producer,
            payload);
    }
}
