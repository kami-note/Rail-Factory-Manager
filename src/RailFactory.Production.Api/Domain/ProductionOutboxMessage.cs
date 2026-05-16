namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Stores integration events for reliable at-least-once delivery to downstream services.
/// </summary>
/// <remarks>
/// Consumed by P5 (Inventory reservation) when <c>ProductionOrderReleased</c> events are dispatched.
/// </remarks>
public sealed class ProductionOutboxMessage
{
    /// <summary>
    /// Unique identifier for idempotent processing by consumers.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Canonical event type name (e.g., production_order_released).
    /// </summary>
    public string EventType { get; private set; }

    /// <summary>
    /// JSON-serialized event payload.
    /// </summary>
    public string Payload { get; private set; }

    /// <summary>
    /// Timestamp when the event was recorded.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Timestamp when the event was dispatched. Null if not yet dispatched.
    /// </summary>
    public DateTimeOffset? DispatchedAt { get; private set; }

    private ProductionOutboxMessage()
    {
        EventType = string.Empty;
        Payload = string.Empty;
    }

    private ProductionOutboxMessage(Guid id, string eventType, string payload)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Number of dispatch attempts made.
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Last error message from a failed dispatch attempt.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Factory method for creating a pending outbox entry.
    /// </summary>
    public static ProductionOutboxMessage Create(string eventType, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new ProductionOutboxMessage(Guid.NewGuid(), eventType, payload);
    }

    /// <summary>
    /// Records a successful dispatch to the downstream service.
    /// </summary>
    public void MarkDispatched()
    {
        AttemptCount++;
        DispatchedAt = DateTimeOffset.UtcNow;
        LastError = null;
    }

    /// <summary>
    /// Records a transient failure without permanently blocking the message.
    /// </summary>
    public void MarkTransientFailure(string error)
    {
        AttemptCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }

    /// <summary>
    /// Permanently blocks the message from future dispatch after max retries.
    /// </summary>
    public void MarkDeadLetter(string error)
    {
        AttemptCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
        DispatchedAt = null;
    }
}
