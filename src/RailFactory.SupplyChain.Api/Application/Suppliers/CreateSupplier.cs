using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Suppliers;

public sealed class CreateSupplier(ISupplyChainRepository repository)
{
    public async Task<Supplier> ExecuteAsync(string fiscalId, string name, CancellationToken cancellationToken)
    {
        var existing = await repository.GetSupplierByFiscalIdAsync(fiscalId.Trim(), cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var supplier = Supplier.Create(fiscalId, name);
        await repository.AddSupplierAsync(supplier, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return supplier;
    }
}
