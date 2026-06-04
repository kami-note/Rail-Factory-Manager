using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.Boms;

/// <summary>
/// Activates a BOM version, atomically reverting any previously active version for the same product to Draft.
/// </summary>
public sealed class ActivateBomVersion(IBomRepository repository, IProductionOrderRepository orderRepository)
{
    public async Task ExecuteAsync(Guid bomId, CancellationToken cancellationToken)
    {
        var bom = await repository.GetByIdAsync(bomId, cancellationToken)
            ?? throw new KeyNotFoundException($"BOM '{bomId}' not found.");

        var currentlyActive = await repository.GetActiveByProductCodeAsync(bom.ProductCode.Value, cancellationToken);
        if (currentlyActive is not null && currentlyActive.Id != bom.Id)
        {
            var hasActiveOrders = await orderRepository.HasActiveOrdersForBomAsync(currentlyActive.Id, cancellationToken);
            if (hasActiveOrders)
                throw new InvalidOperationException(
                    $"Cannot deactivate BOM '{currentlyActive.Id}' (v{currentlyActive.Version}): it is still referenced by Released or InExecution production orders.");

            currentlyActive.RevertToDraft();
        }

        bom.Activate();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
