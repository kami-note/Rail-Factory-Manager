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
        Guid supplierId,
        string supplierName,
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

        var receipt = MaterialReceipt.Create(receiptNumber, supplierId, documentNumber, accessKey, totalValue, rawXml, receiptDate);
        foreach (var item in items)
        {
            receipt.AddItem(item.MaterialCode, item.UnitOfMeasure, item.ExpectedQuantity, item.UnitPrice, item.OriginalDescription, item.Ncm, null, item.Ean);
        }

        await repository.AddReceiptAsync(receipt, cancellationToken);

        await repository.AddAuditEntryAsync(
            SupplyAuditEntry.Create("receipt_created", userIdentifier, JsonSerializer.Serialize(BuildAuditMetadata(receipt))),
            cancellationToken);

        foreach (var item in receipt.Items)
        {
            var integrationEvent = new ReceiptItemRegisteredIntegrationEvent(
                receipt.Id,
                item.Id,
                receipt.ReceiptNumber,
                supplierName,
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
