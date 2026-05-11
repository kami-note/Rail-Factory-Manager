using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Exceptionally overrides a supplier product code from the invoice.
/// </summary>
public sealed class OverrideSupplierProductCode(
    ISupplyChainRepository repository,
    ISupplyChainTransactionRunner transactionRunner)
{
    public async Task<AssociateReceiptItemResponse> ExecuteAsync(
        Guid receiptId,
        Guid itemId,
        DateTimeOffset expectedVersion,
        string correctedCode,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        AssociateReceiptItemResponse? result = null;

        await transactionRunner.ExecuteAsync(async ct =>
        {
            var receipt = await repository.GetReceiptByIdAsync(receiptId, ct)
                ?? throw new AssociationValidationException("receipt.not_found", "Receipt was not found.");

            if (receipt.Status != MaterialReceiptStatus.PendingAssociation)
            {
                throw new AssociationValidationException("association.invalid_receipt_status", $"Receipt in status '{receipt.Status}' cannot be modified.");
            }

            var item = receipt.Items.FirstOrDefault(x => x.Id == itemId)
                ?? throw new AssociationValidationException("association.item_not_found", "Receipt item was not found.");

            if (item.AssociationUpdatedAt != expectedVersion)
            {
                throw new AssociationConflictException(item.Id, item.AssociationUpdatedAt);
            }

            item.OverrideSupplierProductCode(correctedCode, reason, actor);
            await repository.SaveChangesAsync(ct);

            result = new AssociateReceiptItemResponse(
                item.Id,
                item.AssociationUpdatedAt,
                item.AssociationStatus.ToString(),
                item.InternalMaterialCode?.Value,
                item.AssociationConversionFactor,
                receipt.Items.All(x => 
                    x.AssociationStatus is MaterialReceiptItemAssociationStatus.Mapped 
                    or MaterialReceiptItemAssociationStatus.CreatedAndMapped));
        }, cancellationToken);

        return result!;
    }
}
