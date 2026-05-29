namespace RailFactory.Logistics.Api.Domain;

public sealed class LogisticsOutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? DeadLetteredAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private LogisticsOutboxMessage()
    {
        EventType = string.Empty;
        Payload = string.Empty;
    }

    private LogisticsOutboxMessage(Guid id, string eventType, string payload)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public static LogisticsOutboxMessage Create(string eventType, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        return new LogisticsOutboxMessage(Guid.NewGuid(), eventType, payload);
    }

    public void MarkDispatched()
    {
        AttemptCount++;
        DispatchedAt = DateTimeOffset.UtcNow;
        LastError = null;
    }

    public void MarkTransientFailure(string error)
    {
        AttemptCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }

    public void MarkDeadLetter(string error)
    {
        AttemptCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
        DeadLetteredAt = DateTimeOffset.UtcNow;
    }
}
