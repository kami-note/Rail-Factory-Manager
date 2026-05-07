using System.Text.Json;
using RailFactory.SupplyChain.Api.Application.Integration;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Orchestrates the closing of a material receipt conference and synchronization with Inventory.
/// </summary>
public sealed class CloseMaterialReceiptConference(
    ISupplyChainRepository repository,
    ISupplyChainTransactionRunner transactionRunner,
    ISupplyOutbox outbox)
{
    /// <summary>
    /// Executes the close conference command.
    /// </summary>
    public async Task<MaterialReceipt> ExecuteAsync(
        Guid receiptId,
        string userIdentifier,
        IReadOnlyCollection<CloseConferenceItemInput> countedResults,
        string correlationId,
        CancellationToken cancellationToken)
    {
        MaterialReceipt? receipt = null;

        await transactionRunner.ExecuteAsync(async ct =>
        {
            receipt = await repository.GetReceiptByIdAsync(receiptId, ct)
                ?? throw new InvalidOperationException($"Receipt with ID '{receiptId}' not found.");

            var domainResults = countedResults.Select(x => new CountedItemResult(
                x.ItemId,
                x.CountedQuantity,
                x.ConfirmedLotNumber,
                x.ConfirmedExpirationDate)).ToList();

            receipt.CloseConference(domainResults);

            await repository.AddAuditEntryAsync(
                SupplyAuditEntry.Create(
                    "conference_closed",
                    userIdentifier,
                    JsonSerializer.Serialize(new { receiptId = receipt.Id, status = receipt.Status.ToString() })),
                ct);

            foreach (var item in receipt.Items)
            {
                var integrationEvent = new ReceiptItemConferredIntegrationEvent(
                    receipt.Id,
                    item.Id,
                    receipt.Status.ToString(),
                    !item.HasDivergence, // ELITE FIX: Individual item approval status
                    item.CountedQuantity ?? 0,
                    item.ConfirmedLotNumber,
                    item.ConfirmedExpirationDate,
                    "supply-chain");

                await outbox.EnqueueAsync("supply.receipt_item_conferred", integrationEvent, correlationId, ct);
            }

            await repository.SaveChangesAsync(ct);
        }, cancellationToken);

        return receipt!;
    }
}

public sealed record CloseConferenceItemInput(
    Guid ItemId,
    decimal CountedQuantity,
    string? ConfirmedLotNumber,
    DateTimeOffset? ConfirmedExpirationDate);
