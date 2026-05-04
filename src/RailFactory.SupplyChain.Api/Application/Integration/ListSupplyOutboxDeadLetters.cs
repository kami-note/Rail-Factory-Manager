using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Application.Integration;

public sealed class ListSupplyOutboxDeadLetters(ISupplyOutboxDiagnostics diagnostics)
{
    public Task<IReadOnlyList<SupplyOutboxDeadLetterInfo>> ExecuteAsync(
        string tenantCode,
        int take,
        CancellationToken cancellationToken)
    {
        var boundedTake = Math.Clamp(take, 1, 200);
        return diagnostics.ListDeadLettersAsync(tenantCode, boundedTake, cancellationToken);
    }
}
