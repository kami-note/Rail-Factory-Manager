using RailFactory.SupplyChain.Api.Application.Integration;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Releases a receipt from association work back to the conference-ready flow.
/// </summary>
public sealed class ReleaseReceiptToConference(
    ISupplyChainRepository repository,
    ISupplyChainTransactionRunner transactionRunner,
    ISupplyOutbox outbox)
{
    public async Task<ReleaseReceiptToConferenceResponse> ExecuteAsync(
        Guid receiptId,
        DateTimeOffset expectedReceiptVersion,
        string actor,
        CancellationToken cancellationToken)
    {
        ReleaseReceiptToConferenceResponse? result = null;

        await transactionRunner.ExecuteAsync(async ct =>
        {
            var receipt = await repository.GetReceiptByIdAsync(receiptId, ct)
                ?? throw new AssociationValidationException("receipt.not_found", "Receipt was not found.");

            if (receipt.UpdatedAt != expectedReceiptVersion)
            {
                throw new AssociationConflictException(receipt.Id, receipt.UpdatedAt);
            }

            if (receipt.Status != MaterialReceiptStatus.PendingAssociation)
            {
                throw new AssociationValidationException("association.invalid_receipt_status", $"Receipt in status '{receipt.Status}' cannot be released to conference.");
            }

            var blockers = BuildBlockers(receipt.Items);
            if (blockers.Count > 0)
            {
                throw new AssociationReleaseBlockedException(blockers);
            }

            var supplier = await repository.GetSupplierByIdAsync(receipt.SupplierId, ct);

            receipt.ReleaseAssociation();

            await repository.AddAuditEntryAsync(
                SupplyAuditEntry.Create(
                    "association_released",
                    actor,
                    $"{{\"receiptId\": \"{receipt.Id}\", \"receiptNumber\": \"{receipt.ReceiptNumber}\"}}"),
                ct);

            // ELITE FIX: Synchronize with Inventory after SKU resolution.
            // Items are now fully mapped/created, so we can create pending balances.
            foreach (var item in receipt.Items)
            {
                var integrationEvent = new ReceiptItemRegisteredIntegrationEvent(
                    receipt.Id,
                    item.Id,
                    receipt.ReceiptNumber,
                    supplier?.Name ?? "Unknown Supplier",
                    item.MaterialCode,
                    item.ExpectedQuantity,
                    item.UnitOfMeasure,
                    item.UnitPrice,
                    item.OriginalDescription,
                    receipt.AccessKey,
                    "supply-chain",
                    item.Ncm,
                    item.Ean);

                await outbox.EnqueueAsync("supply.receipt_item_registered", integrationEvent, Guid.NewGuid().ToString(), ct);
            }

            await repository.SaveChangesAsync(ct);

            result = new ReleaseReceiptToConferenceResponse(
                receipt.Id,
                receipt.Status.ToString(),
                CanStartConference: true);
        }, cancellationToken);

        return result!;
    }

    private static IReadOnlyList<AssociationReleaseBlocker> BuildBlockers(IEnumerable<MaterialReceiptItem> items) =>
        items
            .Where(x => x.AssociationStatus is not MaterialReceiptItemAssociationStatus.Mapped and not MaterialReceiptItemAssociationStatus.CreatedAndMapped)
            .Select(x => new AssociationReleaseBlocker(x.Id, x.AssociationStatus.ToString(), "Item requires an internal material association decision."))
            .ToList();
}

public sealed record ReleaseReceiptToConferenceResponse(
    Guid ReceiptId,
    string Status,
    bool CanStartConference);

public sealed record AssociationReleaseBlocker(
    Guid ItemId,
    string AssociationStatus,
    string Message);

public sealed class AssociationReleaseBlockedException(IReadOnlyList<AssociationReleaseBlocker> blockers)
    : InvalidOperationException("Resolve blocking association items before releasing the receipt.")
{
    public IReadOnlyList<AssociationReleaseBlocker> Blockers { get; } = blockers;
}
