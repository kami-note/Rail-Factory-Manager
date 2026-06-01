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
    Task<List<InventoryBalance>> ListBalancesAsync(InventoryBalanceStatus? status, InventorySourceType? sourceType, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all Available balances for a given material code, ordered by creation date (FIFO).
    /// </summary>
    Task<List<InventoryBalance>> GetAvailableBalancesByMaterialCodeAsync(string materialCode, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all Reserved balances for a given Production Order.
    /// </summary>
    Task<List<InventoryBalance>> GetReservedBalancesByOrderIdAsync(Guid productionOrderId, CancellationToken cancellationToken);

    Task<InventoryStockSummary> GetStockSummaryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns all ledger entries for a completed production order, joined with balance lot/unit data (RF-15).
    /// </summary>
    Task<List<ProductionTraceabilityLine>> GetProductionTraceabilityAsync(Guid orderId, CancellationToken ct);

    /// <summary>
    /// Aggregates production_consumed ledger entries by material code for the cost dashboard (RF-38).
    /// </summary>
    Task<List<MaterialConsumptionSummary>> GetProductionCostSummaryAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record InventoryStockSummary(
    int TotalMaterials,
    int MaterialsWithStock,
    int AvailableCount,
    int ReservedCount,
    int BlockedCount);

public sealed record ProductionTraceabilityLine(
    Guid BalanceId,
    string MaterialCode,
    string? LotNumber,
    string UnitOfMeasure,
    decimal ConsumedQty,
    DateTimeOffset ConsumedAt);

public sealed record MaterialConsumptionSummary(
    string MaterialCode,
    string UnitOfMeasure,
    decimal TotalConsumedQty,
    int EventCount,
    DateTimeOffset? LastConsumedAt);
