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

        bom.AddItem(input.MaterialCode, input.Quantity, input.UnitOfMeasure);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

public sealed record AddBomItemInput(Guid BomId, string MaterialCode, decimal Quantity, string UnitOfMeasure);
