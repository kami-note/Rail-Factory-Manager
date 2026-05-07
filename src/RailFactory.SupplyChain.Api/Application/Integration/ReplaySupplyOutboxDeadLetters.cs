using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Application.Integration;

/// <summary>
/// Replays dead-lettered outbox messages by resetting their state.
/// </summary>
/// <param name="diagnostics">The outbox diagnostics port.</param>
public sealed class ReplaySupplyOutboxDeadLetters(ISupplyOutboxDiagnostics diagnostics)
{
    /// <summary>
    /// Executes the replay of dead-lettered messages.
    /// </summary>
    /// <param name="messageIds">The optional list of specific message IDs to replay.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages reset for replay.</returns>
    public async Task<int> ExecuteAsync(
        IEnumerable<Guid>? messageIds,
        CancellationToken cancellationToken)
    {
        return await diagnostics.ReplayDeadLettersAsync(messageIds, cancellationToken);
    }
}
