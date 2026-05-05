namespace RailFactory.SupplyChain.Api.Application.Ports;

public interface ISupplyOutboxDiagnostics
{
    Task<IReadOnlyList<SupplyOutboxDeadLetterInfo>> ListDeadLettersAsync(
        int take,
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
