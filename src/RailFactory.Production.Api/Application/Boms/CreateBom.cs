using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Boms;

/// <summary>
/// Creates a new BOM draft for a product, auto-incrementing the version number.
/// </summary>
public sealed class CreateBom(IBomRepository repository)
{
    public async Task<BillOfMaterials> ExecuteAsync(CreateBomInput input, CancellationToken cancellationToken)
    {
        var nextVersion = await repository.GetLatestVersionNumberAsync(input.ProductCode, cancellationToken) + 1;
        var batchSize = input.BatchSize ?? 1.0m;
        var bom = BillOfMaterials.Create(input.ProductCode, nextVersion, batchSize);

        await repository.AddAsync(bom, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return bom;
    }
}

public sealed record CreateBomInput(string ProductCode, decimal? BatchSize = null);
