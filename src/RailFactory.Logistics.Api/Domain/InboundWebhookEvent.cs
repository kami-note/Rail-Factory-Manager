namespace RailFactory.Logistics.Api.Domain;

public enum InboundWebhookStatus { Pending, Processed, Failed, DeadLettered }

public sealed class InboundWebhookEvent
{
    private InboundWebhookEvent() { Provider = string.Empty; EventType = string.Empty; ExternalId = string.Empty; Payload = string.Empty; }

    private InboundWebhookEvent(
        Guid id, string tenantId, string provider, string eventType,
        string externalId, string payload, DateTimeOffset receivedAt)
    {
        Id = id;
        TenantId = tenantId;
        Provider = provider;
        EventType = eventType;
        ExternalId = externalId;
        Payload = payload;
        Status = InboundWebhookStatus.Pending;
        RetryCount = 0;
        ReceivedAt = receivedAt;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Provider { get; private set; }
    public string EventType { get; private set; }
    public string ExternalId { get; private set; }
    public string Payload { get; private set; }
    public InboundWebhookStatus Status { get; private set; }
    public short RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    public static InboundWebhookEvent Receive(
        string tenantId, string provider, string eventType, string externalId, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        return new InboundWebhookEvent(
            Guid.NewGuid(), tenantId, provider, eventType, externalId, payload, DateTimeOffset.UtcNow);
    }

    public void MarkProcessed()
    {
        Status = InboundWebhookStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error, int maxRetries = 5)
    {
        RetryCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
        Status = RetryCount >= maxRetries
            ? InboundWebhookStatus.DeadLettered
            : InboundWebhookStatus.Failed;
    }

    public void ResetToPending()
    {
        Status = InboundWebhookStatus.Pending;
    }
}
