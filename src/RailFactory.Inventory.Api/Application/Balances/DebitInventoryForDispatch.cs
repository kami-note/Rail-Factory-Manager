using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Debits Available stock when a logistics shipment dispatch ships items out of the warehouse.
/// Consumes available balances FIFO. If available stock is insufficient, debits all available
/// stock and logs the shortfall — the dispatch has already left the warehouse.
/// Idempotent: replays with the same EventId are skipped.
/// </summary>
public sealed class DebitInventoryForDispatch(
    IInventoryRepository repository,
    ILogger<DebitInventoryForDispatch> logger)
{
    public async Task<bool> ExecuteAsync(DebitInventoryForDispatchInput input, CancellationToken cancellationToken)
    {
        if (await repository.IntegrationMessageProcessedAsync(input.EventId, cancellationToken))
            return false;

        var availableBalances = await repository.GetAvailableBalancesByMaterialCodeAsync(input.MaterialCode, cancellationToken);
        var totalAvailable = availableBalances.Sum(b => b.Quantity);

        if (totalAvailable < input.Quantity)
            logger.LogWarning(
                "Insufficient stock for dispatch debit: material {MaterialCode}, required {Required}, available {Available}. " +
                "Debiting all available stock.",
                input.MaterialCode, input.Quantity, totalAvailable);

        var detailsJson = JsonSerializer.Serialize(new
        {
            input.DispatchId,
            input.TrackingCode,
            input.OrderNumber,
            input.MaterialCode,
            input.Quantity,
            input.CorrelationId,
            input.EventType
        });

        decimal remaining = input.Quantity;
        foreach (var balance in availableBalances)
        {
            if (remaining <= 0) break;

            var debitQty = Math.Min(balance.Quantity, remaining);
            if (debitQty <= 0) continue;

            balance.Debit(debitQty);
            remaining -= debitQty;

            await repository.AddLedgerEntryAsync(
                InventoryLedgerEntry.Create(balance.Id, "stock_dispatched", -debitQty, detailsJson),
                cancellationToken);
        }

        await repository.AddIntegrationMessageAsync(
            InventoryIntegrationMessage.Create(input.EventId, input.EventType),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record DebitInventoryForDispatchInput(
    Guid EventId,
    string EventType,
    string CorrelationId,
    Guid DispatchId,
    string TrackingCode,
    string OrderNumber,
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure);
