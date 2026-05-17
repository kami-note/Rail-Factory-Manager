using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Ports;

public interface IInventoryRepository
{
    Task<StockLocation?> FindDefaultLocationAsync(CancellationToken cancellationToken);
    Task EnsureDefaultLocationAsync(CancellationToken cancellationToken);
    Task<bool> IntegrationMessageProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task AddIntegrationMessageAsync(InventoryIntegrationMessage message, CancellationToken cancellationToken);
    Task AddBalanceAsync(InventoryBalance balance, CancellationToken cancellationToken);
    Task AddLedgerEntryAsync(InventoryLedgerEntry entry, CancellationToken cancellationToken);
    Task<InventoryBalance?> GetBalanceByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<InventoryBalance?> GetBalanceBySourceReferenceAsync(string sourceReference, CancellationToken cancellationToken);
    Task<InventoryBalance?> GetLatestBalanceByMaterialCodeAsync(string materialCode, CancellationToken cancellationToken);
    Task<List<InventoryBalance>> GetBalancesByMaterialCodeAsync(string materialCode, CancellationToken cancellationToken);
    Task<List<InventoryLedgerEntry>> GetLedgerEntriesByBalanceIdAsync(Guid balanceId, CancellationToken cancellationToken);
    Task<List<InventoryBalance>> ListBalancesAsync(InventoryBalanceStatus? status, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all Available balances for a given material code, ordered by creation date (FIFO).
    /// </summary>
    Task<List<InventoryBalance>> GetAvailableBalancesByMaterialCodeAsync(string materialCode, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all Reserved balances for a given Production Order.
    /// </summary>
    Task<List<InventoryBalance>> GetReservedBalancesByOrderIdAsync(Guid productionOrderId, CancellationToken cancellationToken);

    Task<InventoryStockSummary> GetStockSummaryAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record InventoryStockSummary(
    int TotalMaterials,
    int MaterialsWithStock,
    int AvailableCount,
    int ReservedCount);
