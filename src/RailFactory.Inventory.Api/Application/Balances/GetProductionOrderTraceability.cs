using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Returns the consumed-material lines for a completed production order (RF-15).
/// Each line identifies the balance (lot), material, quantity consumed, and timestamp.
/// </summary>
public sealed class GetProductionOrderTraceability(IInventoryRepository repository)
{
    public Task<List<ProductionTraceabilityLine>> ExecuteAsync(Guid orderId, CancellationToken ct)
        => repository.GetProductionTraceabilityAsync(orderId, ct);
}
