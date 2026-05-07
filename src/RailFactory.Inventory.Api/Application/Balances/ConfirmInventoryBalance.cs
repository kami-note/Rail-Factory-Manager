using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Updates a pending balance with conference results (quantity, lot, expiry) and activates it.
/// </summary>
public sealed class ConfirmInventoryBalance(IInventoryRepository repository)
{
    public async Task<bool> ExecuteAsync(ConfirmInventoryBalanceInput input, CancellationToken cancellationToken)
    {
        if (await repository.IntegrationMessageProcessedAsync(input.EventId, cancellationToken))
        {
            return false; // Idempotency
        }

        var sourceReference = $"{input.ReceiptId}:{input.ReceiptItemId}";
        var balance = await repository.GetBalanceBySourceReferenceAsync(sourceReference, cancellationToken)
            ?? throw new InvalidOperationException($"Balance with source reference '{sourceReference}' not found.");

        // ELITE FIX: Individual item approval status from integration event
        balance.Confirm(input.CountedQuantity, input.LotNumber, input.ExpirationDate, input.IsApproved);

        await repository.AddIntegrationMessageAsync(
            InventoryIntegrationMessage.Create(input.EventId, input.EventType),
            cancellationToken);

        var detailsJson = JsonSerializer.Serialize(new
        {
            input.ReceiptId,
            input.ReceiptItemId,
            input.ReceiptStatus,
            input.IsApproved,
            input.CountedQuantity,
            input.LotNumber,
            input.ExpirationDate,
            input.CorrelationId,
            input.EventType
        });

        await repository.AddLedgerEntryAsync(
            InventoryLedgerEntry.Create(balance.Id, "balance_confirmed", input.CountedQuantity - balance.Quantity, detailsJson),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public sealed record ConfirmInventoryBalanceInput(
    Guid EventId,
    string EventType,
    string CorrelationId,
    Guid ReceiptId,
    Guid ReceiptItemId,
    string ReceiptStatus,
    bool IsApproved,
    decimal CountedQuantity,
    string? LotNumber,
    DateTimeOffset? ExpirationDate);
