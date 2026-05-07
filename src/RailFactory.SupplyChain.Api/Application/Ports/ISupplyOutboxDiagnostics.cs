namespace RailFactory.SupplyChain.Api.Application.Ports;

public interface ISupplyOutboxDiagnostics
{
    Task<IReadOnlyList<SupplyOutboxDeadLetterInfo>> ListDeadLettersAsync(
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resets the state of dead-lettered messages so they can be retried by the outbox processor.
    /// </summary>
    /// <param name="messageIds">The optional list of specific message IDs to replay. If null or empty, all dead letters are replayed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages reset for replay.</returns>
    Task<int> ReplayDeadLettersAsync(
        IEnumerable<Guid>? messageIds,
        CancellationToken cancellationToken);
}

public sealed record SupplyOutboxDeadLetterInfo(
    Guid Id,
    string EventType,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    int AttemptCount,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? DeadLetteredAt,
    string? LastError);
