using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Converts all Reserved balances for a completed Production Order into consumed stock.
/// Called when <c>production.order_completed</c> is received from the Production service.
/// Idempotent: replays with the same EventId are skipped.
/// </summary>
public sealed class ConsumeReservedStock(IInventoryRepository repository)
{
    public async Task<bool> ExecuteAsync(ConsumeReservedStockInput input, CancellationToken cancellationToken)
    {
        if (await repository.IntegrationMessageProcessedAsync(input.EventId, cancellationToken))
            return false;

        var reservedBalances = await repository.GetReservedBalancesByOrderIdAsync(
            input.ProductionOrderId, cancellationToken);

        foreach (var balance in reservedBalances)
        {
            var consumedQty = balance.Quantity;
            balance.Consume(consumedQty);

            var detailsJson = JsonSerializer.Serialize(new
            {
                input.ProductionOrderId,
                input.OrderNumber,
                materialCode = balance.MaterialCode,
                consumedQty,
                input.CorrelationId,
                input.EventType
            });

            await repository.AddLedgerEntryAsync(
                InventoryLedgerEntry.Create(balance.Id, "production_consumed", -consumedQty, detailsJson),
                cancellationToken);
        }

        await repository.AddIntegrationMessageAsync(
            InventoryIntegrationMessage.Create(input.EventId, input.EventType),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record ConsumeReservedStockInput(
    Guid EventId,
    string EventType,
    string CorrelationId,
    Guid ProductionOrderId,
    string OrderNumber);
