using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Orchestrates the creation of a new material in Inventory and its association with a receipt item.
/// </summary>
public sealed class CreateMaterialAndAssociate(
    ISupplyChainRepository repository,
    IInventoryMaterialService inventoryMaterialService,
    ISupplyChainTransactionRunner transactionRunner,
    ISupplyOutbox outbox)
{
    public async Task<AssociateReceiptItemResponse> ExecuteAsync(
        Guid receiptId,
        Guid itemId,
        DateTimeOffset expectedVersion,
        CreateMaterialInput materialInput,
        decimal conversionFactor,
        string actor,
        CancellationToken cancellationToken)
    {
        if (conversionFactor <= 0)
        {
            throw new AssociationValidationException("association.invalid_conversion_factor", "Conversion factor must be greater than zero.");
        }

        // 1. Create the material in Inventory (or ensure it exists)
        // This is an external call, done outside the SupplyChain transaction.
        var material = await inventoryMaterialService.CreateMaterialAsync(materialInput, cancellationToken);
        var freshMaterial = await inventoryMaterialService.GetMaterialByCodeFreshAsync(material.MaterialCode, cancellationToken)
            ?? material;

        if (string.IsNullOrWhiteSpace(freshMaterial.UnitOfMeasure))
        {
            throw new AssociationValidationException(
                "association.material_unit_invalid",
                $"Internal material '{freshMaterial.MaterialCode}' has no valid unit of measure.");
        }

        AssociateReceiptItemResponse? result = null;

        // 2. Perform the association within a local transaction
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
                        MaterialCode.From(freshMaterial.MaterialCode),
                        freshMaterial.UnitOfMeasure,
                        item.SupplierUnitOfMeasure,
                        conversionFactor,
                        EmailAddress.From(actor)),
                    ct);
            }
            else
            {
                existingMapping.CorrectMapping(
                    MaterialCode.From(freshMaterial.MaterialCode), 
                    freshMaterial.UnitOfMeasure,
                    conversionFactor, 
                    EmailAddress.From(actor));
            }

            // Integration event
            var integrationEvent = new SupplierMaterialMappingCreatedEvent(
                FiscalId.From(supplier.FiscalId),
                item.SupplierProductCode,
                MaterialCode.From(freshMaterial.MaterialCode));
            
            await outbox.EnqueueAsync("supply.supplier_material_mapping_created", integrationEvent, Guid.NewGuid().ToString(), ct);

            // Update the current item state (Now with the real internal unit)
            item.MapToNewMaterial(freshMaterial.MaterialCode, freshMaterial.UnitOfMeasure, conversionFactor, actor);
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
