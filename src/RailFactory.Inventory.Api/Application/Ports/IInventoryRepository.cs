using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Ports;

public interface IInventoryRepository
{
    Task<StockLocation?> FindDefaultLocationAsync(string tenantCode, CancellationToken cancellationToken);
    Task EnsureDefaultLocationAsync(string tenantCode, CancellationToken cancellationToken);
    Task<bool> IntegrationMessageProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task AddIntegrationMessageAsync(InventoryIntegrationMessage message, CancellationToken cancellationToken);
    Task AddBalanceAsync(InventoryBalance balance, CancellationToken cancellationToken);
    Task AddLedgerEntryAsync(InventoryLedgerEntry entry, CancellationToken cancellationToken);
    Task<List<InventoryBalance>> ListPendingBalancesAsync(string tenantCode, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
