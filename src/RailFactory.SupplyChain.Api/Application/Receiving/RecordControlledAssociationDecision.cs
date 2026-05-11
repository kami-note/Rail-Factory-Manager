using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Records controlled non-mapping decisions that keep association work explicit and auditable.
/// </summary>
public sealed class RecordControlledAssociationDecision(
    ISupplyChainRepository repository,
    ISupplyChainTransactionRunner transactionRunner)
{
    public async Task<ControlledAssociationDecisionResponse> ExecuteAsync(
        Guid receiptId,
        Guid itemId,
        DateTimeOffset expectedVersion,
        ControlledAssociationDecision decision,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new AssociationValidationException("association.reason_required", "A reason is required for this association decision.");
        }

        ControlledAssociationDecisionResponse? result = null;

        await transactionRunner.ExecuteAsync(async ct =>
        {
            var receipt = await repository.GetReceiptByIdAsync(receiptId, ct)
                ?? throw new AssociationValidationException("receipt.not_found", "Receipt was not found.");

            if (receipt.Status != MaterialReceiptStatus.PendingAssociation)
            {
                throw new AssociationValidationException("association.invalid_receipt_status", $"Receipt in status '{receipt.Status}' cannot receive association decisions.");
            }

            var item = receipt.Items.FirstOrDefault(x => x.Id == itemId)
                ?? throw new AssociationValidationException("association.item_not_found", "Receipt item was not found.");

            if (item.AssociationUpdatedAt != expectedVersion)
            {
                throw new AssociationConflictException(item.Id, item.AssociationUpdatedAt);
            }

            switch (decision)
            {
                case ControlledAssociationDecision.ReviewLater:
                    item.MarkReviewLater(reason, actor);
                    break;
                case ControlledAssociationDecision.Ignored:
                    item.MarkIgnored(reason, actor);
                    break;
                default:
                    throw new AssociationValidationException("association.invalid_decision", "Association decision is not valid.");
            }

            await repository.SaveChangesAsync(ct);

            result = new ControlledAssociationDecisionResponse(
                item.Id,
                item.AssociationUpdatedAt,
                item.AssociationStatus.ToString(),
                item.AssociationReason,
                CanReleaseReceiptToConference: false);
        }, cancellationToken);

        return result!;
    }
}

public enum ControlledAssociationDecision
{
    ReviewLater,
    Ignored
}

public sealed record ControlledAssociationDecisionResponse(
    Guid ItemId,
    DateTimeOffset Version,
    string AssociationStatus,
    string? ReviewReason,
    bool CanReleaseReceiptToConference);
