using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Application.Integration;

public sealed class ListSupplyOutboxDeadLetters(ISupplyOutboxDiagnostics diagnostics)
{
    public Task<IReadOnlyList<SupplyOutboxDeadLetterInfo>> ExecuteAsync(
        int? take,
        CancellationToken cancellationToken)
    {
        var boundedTake = Math.Clamp(take ?? 50, 1, 100);
        return diagnostics.ListDeadLettersAsync(boundedTake, cancellationToken);
    }
}
