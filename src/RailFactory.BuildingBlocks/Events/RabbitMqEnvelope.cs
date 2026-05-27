using System.Text.Json;

namespace RailFactory.BuildingBlocks.Events;

/// <summary>
/// Standardized envelope for all cross-service integration events transported over RabbitMQ.
/// </summary>
/// <param name="EventId">
/// Idempotency key — unique per logical event. Must be deterministic for events derived
/// from a single outbox message (e.g. per-BOM-item reservations share the outbox ID but
/// differ in <c>EventId</c> based on the item ID).
/// </param>
/// <param name="EventType">
/// Domain event type key (e.g. <c>"supply.receipt_item_registered"</c>).
/// Used by the consumer to route to the correct use-case.
/// </param>
/// <param name="CorrelationId">Propagated from the originating outbox message for end-to-end tracing. May be a GUID string or any opaque identifier.</param>
/// <param name="TenantCode">Tenant identifier — the consumer resolves the DB connection string from this value.</param>
/// <param name="Payload">Event-specific payload as a <see cref="JsonElement"/> to avoid double-serialization.</param>
/// <param name="OccurredAt">Timestamp when the domain event originally occurred.</param>
public sealed record RabbitMqEnvelope(
    Guid EventId,
    string EventType,
    string CorrelationId,
    string TenantCode,
    JsonElement Payload,
    DateTimeOffset OccurredAt);
