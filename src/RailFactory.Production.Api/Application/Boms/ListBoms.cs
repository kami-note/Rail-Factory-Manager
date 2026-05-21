using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Boms;

/// <summary>
/// Lists all BOM versions for a given product code.
/// </summary>
public sealed class ListBoms(IBomRepository repository)
{
    public Task<List<BillOfMaterials>> ExecuteAsync(string? productCode, CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(productCode)
            ? repository.ListAllAsync(cancellationToken)
            : repository.ListByProductCodeAsync(productCode, cancellationToken);
}
