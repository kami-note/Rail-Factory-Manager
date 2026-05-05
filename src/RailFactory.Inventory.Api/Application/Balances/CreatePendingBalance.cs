using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

public sealed class CreatePendingBalance(IInventoryRepository repository)
{
    public async Task<bool> ExecuteAsync(CreatePendingBalanceInput input, CancellationToken cancellationToken)
    {
        if (await repository.IntegrationMessageProcessedAsync(input.EventId, cancellationToken))
        {
            return false;
        }

        await repository.EnsureDefaultLocationAsync(cancellationToken);
        var location = await repository.FindDefaultLocationAsync(cancellationToken)
            ?? throw new InvalidOperationException("Default stock location was not found.");

        var sourceReference = $"{input.ReceiptId}:{input.ReceiptItemId}";
        var balance = InventoryBalance.CreatePending(
            input.MaterialCode,
            input.UnitOfMeasure,
            input.Quantity,
            location.Id,
            sourceReference);

        await repository.AddBalanceAsync(balance, cancellationToken);

        var detailsJson = JsonSerializer.Serialize(new
        {
            input.ReceiptId,
            input.ReceiptItemId,
            input.ReceiptNumber,
            input.CorrelationId,
            input.EventType
        });

        await repository.AddLedgerEntryAsync(
            InventoryLedgerEntry.Create(balance.Id, "pending_balance_created", input.Quantity, detailsJson),
            cancellationToken);

        await repository.AddIntegrationMessageAsync(
            InventoryIntegrationMessage.Create(input.EventId, input.EventType),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record CreatePendingBalanceInput(
    Guid EventId,
    string EventType,
    string CorrelationId,
    Guid ReceiptId,
    Guid ReceiptItemId,
    string ReceiptNumber,
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure);
