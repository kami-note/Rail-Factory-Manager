using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.Boms;

/// <summary>
/// Adds a material item to an existing BOM draft.
/// </summary>
public sealed class AddBomItem(IBomRepository repository)
{
    public async Task ExecuteAsync(AddBomItemInput input, CancellationToken cancellationToken)
    {
        var bom = await repository.GetByIdAsync(input.BomId, cancellationToken)
            ?? throw new KeyNotFoundException($"BOM '{input.BomId}' not found.");

        var newItem = bom.AddItem(input.MaterialCode, input.Quantity, input.UnitOfMeasure);

        // Use raw-SQL path to bypass EF Core 10 + Npgsql 10 change-tracking quirk where
        // a new BomItem with a non-zero Guid PK (ValueGeneratedOnAdd convention) is treated
        // as Unchanged → SaveChanges generates UPDATE on a non-existent row → concurrency error.
        await repository.AddItemDirectAsync(bom.Id, newItem, bom.UpdatedAt, cancellationToken);
    }
}

public sealed record AddBomItemInput(Guid BomId, string MaterialCode, decimal Quantity, string UnitOfMeasure);
