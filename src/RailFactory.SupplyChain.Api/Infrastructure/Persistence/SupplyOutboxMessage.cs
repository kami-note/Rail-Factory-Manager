namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public enum SupplyOutboxMessageStatus
{
    Pending,
    Dispatched,
    DeadLetter
}

public sealed class SupplyOutboxMessage
{
    private const int MaxErrorLength = 2000;

    public Guid Id { get; private set; }
    public string TenantCode { get; private set; }
    public string EventType { get; private set; }
    public string CorrelationId { get; private set; }
    public string PayloadJson { get; private set; }
    public SupplyOutboxMessageStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? DeadLetteredAt { get; private set; }

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
        Status = SupplyOutboxMessageStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkTransientFailure(string error)
    {
        AttemptCount++;
        LastAttemptAt = DateTimeOffset.UtcNow;
        LastError = TrimError(error);
    }

    public void MarkDispatched()
    {
        AttemptCount++;
        LastAttemptAt = DateTimeOffset.UtcNow;
        LastError = null;
        Status = SupplyOutboxMessageStatus.Dispatched;
        DispatchedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDeadLetter(string error)
    {
        AttemptCount++;
        LastAttemptAt = DateTimeOffset.UtcNow;
        LastError = TrimError(error);
        Status = SupplyOutboxMessageStatus.DeadLetter;
        DeadLetteredAt = DateTimeOffset.UtcNow;
    }

    private static string TrimError(string error) =>
        error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
}
