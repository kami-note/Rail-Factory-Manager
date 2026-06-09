using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Boms;

/// <summary>
/// Clones an existing BOM to create a new version in Draft status.
/// </summary>
public sealed class CloneBomVersion(IBomRepository repository)
{
    public async Task<BillOfMaterials> ExecuteAsync(Guid bomId, CancellationToken cancellationToken)
    {
        var original = await repository.GetByIdAsync(bomId, cancellationToken)
            ?? throw new KeyNotFoundException($"BOM '{bomId}' not found.");

        var latestVersion = await repository.GetLatestVersionNumberAsync(original.ProductCode.Value, cancellationToken);
        var newVersion = latestVersion + 1;

        // Create the new BOM version.
        // We create it empty (without items initially) so we can persist it first
        // and bypass EF Core change tracking issues with sub-entities (ValueGeneratedOnAdd quirk).
        var clone = BillOfMaterials.Create(original.ProductCode.Value, newVersion, original.BatchSize);

        await repository.AddAsync(clone, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Copy and persist each item using the raw SQL direct insert to prevent EF Core 10 / Npgsql 10 conflicts.
        foreach (var item in original.Items)
        {
            var clonedItem = BomItem.Create(clone.Id, item.MaterialCode, item.Quantity, item.UnitOfMeasure);
            await repository.AddItemDirectAsync(clone.Id, clonedItem, clone.UpdatedAt, cancellationToken);
        }

        // Reload from repository to return a fully populated aggregate.
        var finalBom = await repository.GetByIdAsync(clone.Id, cancellationToken);
        return finalBom!;
    }
}
