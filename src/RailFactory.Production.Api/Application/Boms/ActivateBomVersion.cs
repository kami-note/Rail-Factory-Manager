using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.Boms;

/// <summary>
/// Activates a BOM version, atomically reverting any previously active version for the same product to Draft.
/// </summary>
public sealed class ActivateBomVersion(IBomRepository repository)
{
    public async Task ExecuteAsync(Guid bomId, CancellationToken cancellationToken)
    {
        var bom = await repository.GetByIdAsync(bomId, cancellationToken)
            ?? throw new KeyNotFoundException($"BOM '{bomId}' not found.");

        var currentlyActive = await repository.GetActiveByProductCodeAsync(bom.ProductCode.Value, cancellationToken);
        if (currentlyActive is not null && currentlyActive.Id != bom.Id)
            currentlyActive.RevertToDraft();

        bom.Activate();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
