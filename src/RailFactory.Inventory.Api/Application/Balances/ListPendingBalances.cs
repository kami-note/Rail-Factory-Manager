using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

public sealed class ListPendingBalances(IInventoryRepository repository)
{
    public Task<List<InventoryBalance>> ExecuteAsync(string tenantCode, CancellationToken cancellationToken)
        => repository.ListPendingBalancesAsync(tenantCode, cancellationToken);
}
