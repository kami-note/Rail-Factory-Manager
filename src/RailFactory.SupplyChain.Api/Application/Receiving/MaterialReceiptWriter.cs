using System.Text.Json;
using RailFactory.SupplyChain.Api.Application.Integration;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Domain service for staging and persisting material receipts.
/// </summary>
public sealed class MaterialReceiptWriter(
    ISupplyChainRepository repository,
    ISupplyOutbox outbox)
{
    /// <summary>
    /// Resolves an existing supplier or creates a new one based on the fiscal document.
    /// </summary>
    public async Task<Supplier> ResolveOrCreateSupplierAsync(
        ParsedReceiptDocument parsed,
        IDictionary<string, Supplier> suppliersByFiscalId,
        CancellationToken cancellationToken)
    {
        if (suppliersByFiscalId.TryGetValue(parsed.SupplierFiscalId, out var supplier))
        {
            return supplier;
        }

        supplier = await repository.GetSupplierByFiscalIdAsync(parsed.SupplierFiscalId, cancellationToken);
        if (supplier is null)
        {
            supplier = Supplier.Create(parsed.SupplierFiscalId, parsed.SupplierName);
            await repository.AddSupplierAsync(supplier, cancellationToken);
        }

        suppliersByFiscalId[parsed.SupplierFiscalId] = supplier;
        return supplier;
    }

    /// <summary>
    /// Stages a new material receipt and enqueues integration events.
    /// </summary>
    public async Task<MaterialReceipt> StageReceiptAsync(
        string userIdentifier,
        string receiptNumber,
        Supplier supplier,
        string documentNumber,
        string? accessKey,
        decimal? totalValue,
        string? rawXml,
        DateOnly receiptDate,
        IReadOnlyCollection<StageReceiptItemInput> items,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var existingReceipt = await repository.GetReceiptByReceiptNumberAsync(receiptNumber, cancellationToken);
        if (existingReceipt is not null)
        {
            throw new ReceiptAlreadyExistsException(receiptNumber);
        }

        var receipt = MaterialReceipt.Create(receiptNumber, supplier.Id, documentNumber, accessKey, totalValue, rawXml, receiptDate);
        bool requiresAssociation = false;

        foreach (var item in items)
        {
            var mapping = await repository.GetSupplierMaterialMappingAsync(supplier.FiscalId, item.MaterialCode, cancellationToken);
            
            if (mapping is not null)
            {
                if (string.IsNullOrWhiteSpace(mapping.InternalUnitOfMeasure))
                {
                    throw new InvalidOperationException(
                        $"Supplier mapping for supplier '{supplier.FiscalId}' and product '{item.MaterialCode}' has no internal unit of measure configured.");
                }

                // Apply the Conversion Factor mathematics (Standardizing on 4 decimal places for unit prices)
                var convertedQuantity = item.ExpectedQuantity * mapping.ConversionFactor;
                var convertedPrice = item.UnitPrice.HasValue
                    ? Math.Round(item.UnitPrice.Value / mapping.ConversionFactor, PrecisionConstants.DefaultDecimalPlaces, MidpointRounding.AwayFromZero)
                    : (decimal?)null;
                receipt.AddMappedItem(
                    item.MaterialCode,
                    mapping.InternalMaterialCode.Value,
                    item.ExpectedQuantity,
                    item.UnitOfMeasure,
                    mapping.InternalUnitOfMeasure,
                    convertedQuantity,
                    convertedPrice,
                    item.OriginalDescription,
                    item.Ncm,
                    null,
                    item.Ean,
                    mapping.ConversionFactor);
            }
            else
            {
                // Unknown product code from supplier. Add as-is but flag for block.
                requiresAssociation = true;
                receipt.AddPendingAssociationItem(item.MaterialCode, item.UnitOfMeasure, item.ExpectedQuantity, item.UnitPrice, item.OriginalDescription, item.Ncm, null, item.Ean);
            }
        }

        if (requiresAssociation)
        {
            receipt.BlockForAssociation();
        }

        await repository.AddReceiptAsync(receipt, cancellationToken);

        await repository.AddAuditEntryAsync(
            SupplyAuditEntry.Create("receipt_created", userIdentifier, JsonSerializer.Serialize(BuildAuditMetadata(receipt))),
            cancellationToken);

        // Only enqueue outbox messages if not pending association.
        if (!requiresAssociation)
        {
            foreach (var item in receipt.Items)
            {
                var integrationEvent = new ReceiptItemRegisteredIntegrationEvent(
                    receipt.Id,
                    item.Id,
                    receipt.ReceiptNumber,
                    supplier.Name,
                    item.MaterialCode,
                    item.ExpectedQuantity,
                    item.UnitOfMeasure,
                    item.UnitPrice,
                    item.OriginalDescription,
                    receipt.AccessKey,
                    "supply-chain",
                    item.Ncm,
                    item.Ean);

                await outbox.EnqueueAsync("supply.receipt_item_registered", integrationEvent, correlationId, cancellationToken);
            }
        }

        return receipt;
    }

    private static object BuildAuditMetadata(MaterialReceipt receipt) =>
        new
        {
            receiptId = receipt.Id,
            receipt.ReceiptNumber,
            receipt.DocumentNumber,
            itemCount = receipt.Items.Count
        };
}

/// <summary>
/// Input data for staging a receipt item.
/// </summary>
public sealed record StageReceiptItemInput(
    string MaterialCode, 
    decimal ExpectedQuantity, 
    string UnitOfMeasure, 
    decimal? UnitPrice, 
    string? OriginalDescription,
    string? Ncm = null,
    string? Ean = null);
