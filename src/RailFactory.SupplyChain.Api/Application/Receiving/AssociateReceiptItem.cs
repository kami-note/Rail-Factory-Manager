using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Resolves one receipt item by mapping its supplier SKU to an existing internal material.
/// </summary>
public sealed class AssociateReceiptItem(
    ISupplyChainRepository repository,
    IInventoryMaterialService inventoryMaterialService,
    ISupplyChainTransactionRunner transactionRunner,
    ISupplyOutbox outbox)
{
    public async Task<AssociateReceiptItemResponse> ExecuteAsync(
        Guid receiptId,
        Guid itemId,
        DateTimeOffset expectedVersion,
        string internalMaterialCode,
        decimal conversionFactor,
        string actor,
        CancellationToken cancellationToken)
    {
        if (conversionFactor <= 0)
        {
            throw new AssociationValidationException("association.invalid_conversion_factor", "Conversion factor must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(internalMaterialCode))
        {
            throw new AssociationValidationException("association.material_required", "Internal material code is required.");
        }

        var materials = await inventoryMaterialService.GetMaterialsByCodesAsync([internalMaterialCode], cancellationToken);
        if (!materials.TryGetValue(internalMaterialCode, out var materialMetadata))
        {
            throw new AssociationValidationException("association.material_not_found", "Internal material was not found.");
        }

        AssociateReceiptItemResponse? result = null;

        await transactionRunner.ExecuteAsync(async ct =>
        {
            var receipt = await repository.GetReceiptByIdAsync(receiptId, ct)
                ?? throw new AssociationValidationException("receipt.not_found", "Receipt was not found.");

            if (receipt.Status != MaterialReceiptStatus.PendingAssociation)
            {
                throw new AssociationValidationException("association.invalid_receipt_status", $"Receipt in status '{receipt.Status}' cannot be associated.");
            }

            var supplier = await repository.GetSupplierByIdAsync(receipt.SupplierId, ct)
                ?? throw new AssociationValidationException("supplier.not_found", "Supplier was not found.");

            var item = receipt.Items.FirstOrDefault(x => x.Id == itemId)
                ?? throw new AssociationValidationException("association.item_not_found", "Receipt item was not found.");

            if (item.AssociationUpdatedAt != expectedVersion)
            {
                throw new AssociationConflictException(item.Id, item.AssociationUpdatedAt);
            }

            // Create or update the persistent mapping for future automated receipts
            var existingMapping = await repository.GetSupplierMaterialMappingAsync(supplier.FiscalId, item.SupplierProductCode, ct);
            if (existingMapping is null)
            {
                await repository.AddSupplierMaterialMappingAsync(
                    SupplierMaterialMapping.Create(
                        FiscalId.From(supplier.FiscalId),
                        item.SupplierProductCode,
                        MaterialCode.From(internalMaterialCode),
                        materialMetadata.UnitOfMeasure,
                        item.SupplierUnitOfMeasure,
                        conversionFactor,
                        EmailAddress.From(actor)),
                    ct);
            }
            else
            {
                existingMapping.CorrectMapping(
                    MaterialCode.From(internalMaterialCode), 
                    materialMetadata.UnitOfMeasure, 
                    conversionFactor, 
                    EmailAddress.From(actor));
            }

            var integrationEvent = new SupplierMaterialMappingCreatedEvent(
                FiscalId.From(supplier.FiscalId),
                item.SupplierProductCode,
                MaterialCode.From(internalMaterialCode));
            
            await outbox.EnqueueAsync("supply.supplier_material_mapping_created", integrationEvent, Guid.NewGuid().ToString(), ct);

            // Update the current item state
            item.MapToExistingMaterial(internalMaterialCode, materialMetadata.UnitOfMeasure, conversionFactor, actor);
            await repository.SaveChangesAsync(ct);

            result = new AssociateReceiptItemResponse(
                item.Id,
                item.AssociationUpdatedAt,
                item.AssociationStatus.ToString(),
                item.InternalMaterialCode?.Value,
                item.AssociationConversionFactor,
                receipt.Items.All(IsResolved));
        }, cancellationToken);

        return result!;
    }

    private static bool IsResolved(MaterialReceiptItem item) =>
        item.AssociationStatus is MaterialReceiptItemAssociationStatus.Mapped or MaterialReceiptItemAssociationStatus.CreatedAndMapped;
}

public sealed record AssociateReceiptItemResponse(
    Guid ItemId,
    DateTimeOffset Version,
    string AssociationStatus,
    string? InternalMaterialCode,
    decimal? ConversionFactor,
    bool CanReleaseReceiptToConference);
