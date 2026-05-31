using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Releases all Reserved balances back to Available when a Production Order is cancelled.
/// Called when <c>production.order_cancelled</c> is received from the Production service.
/// Idempotent: replays with the same EventId are skipped.
/// </summary>
public sealed class ReleaseOrderReservation(IInventoryRepository repository)
{
    public async Task<bool> ExecuteAsync(ReleaseOrderReservationInput input, CancellationToken cancellationToken)
    {
        if (await repository.IntegrationMessageProcessedAsync(input.EventId, cancellationToken))
            return false;

        var reservedBalances = await repository.GetReservedBalancesByOrderIdAsync(
            input.ProductionOrderId, cancellationToken);

        foreach (var balance in reservedBalances)
        {
            var releasedQty = balance.Quantity;
            balance.ReleaseReservation();

            var detailsJson = JsonSerializer.Serialize(new
            {
                input.ProductionOrderId,
                input.OrderNumber,
                materialCode = balance.MaterialCode,
                releasedQty,
                input.CorrelationId,
                input.EventType
            });

            await repository.AddLedgerEntryAsync(
                InventoryLedgerEntry.Create(balance.Id, "reservation_released", releasedQty, detailsJson),
                cancellationToken);
        }

        await repository.AddIntegrationMessageAsync(
            InventoryIntegrationMessage.Create(input.EventId, input.EventType),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record ReleaseOrderReservationInput(
    Guid EventId,
    string EventType,
    string CorrelationId,
    Guid ProductionOrderId,
    string OrderNumber);
