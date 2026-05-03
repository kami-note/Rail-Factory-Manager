using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class ListReceipts(ISupplyChainRepository repository)
{
    public Task<List<MaterialReceipt>> ExecuteAsync(string tenantCode, CancellationToken cancellationToken)
        => repository.ListReceiptsAsync(tenantCode, cancellationToken);
}
