using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Reserves available stock for a Production Order, transitioning balances from Available to Reserved.
/// </summary>
/// <remarks>
/// Invariant: Total available quantity for the material must cover the required quantity.
/// Balances are consumed FIFO (oldest first) until the required quantity is met.
/// This is idempotent: if the event was already processed, it returns false.
/// </remarks>
public sealed class ReserveInventoryBalance(IInventoryRepository repository)
{
    public async Task<bool> ExecuteAsync(ReserveInventoryBalanceInput input, CancellationToken cancellationToken)
    {
        if (await repository.IntegrationMessageProcessedAsync(input.EventId, cancellationToken))
            return false;

        var availableBalances = await repository.GetAvailableBalancesByMaterialCodeAsync(input.MaterialCode, cancellationToken);
        var totalAvailable = availableBalances.Sum(b => b.Quantity);

        if (totalAvailable < input.RequiredQuantity)
            throw new InvalidOperationException(
                $"Insufficient stock for material '{input.MaterialCode}': required {input.RequiredQuantity}, available {totalAvailable}.");

        decimal remaining = input.RequiredQuantity;
        foreach (var balance in availableBalances)
        {
            if (remaining <= 0) break;

            balance.Reserve(input.ProductionOrderId, Math.Min(balance.Quantity, remaining));
            remaining -= balance.Quantity;

            var detailsJson = JsonSerializer.Serialize(new
            {
                input.ProductionOrderId,
                input.OrderNumber,
                input.MaterialCode,
                input.RequiredQuantity,
                input.CorrelationId,
                input.EventType
            });

            await repository.AddLedgerEntryAsync(
                InventoryLedgerEntry.Create(balance.Id, "stock_reserved", -balance.Quantity, detailsJson),
                cancellationToken);
        }

        await repository.AddIntegrationMessageAsync(
            InventoryIntegrationMessage.Create(input.EventId, input.EventType),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record ReserveInventoryBalanceInput(
    Guid EventId,
    string EventType,
    string CorrelationId,
    Guid ProductionOrderId,
    string OrderNumber,
    string MaterialCode,
    decimal RequiredQuantity,
    string UnitOfMeasure);
