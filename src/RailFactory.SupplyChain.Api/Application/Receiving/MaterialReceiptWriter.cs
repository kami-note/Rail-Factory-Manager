using System.Text.Json;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class MaterialReceiptWriter(
    ISupplyChainRepository repository,
    ISupplyOutbox outbox,
    ILogger<MaterialReceiptWriter> logger)
{
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

    public async Task<MaterialReceipt> StageReceiptAsync(
        string userIdentifier,
        string receiptNumber,
        Guid supplierId,
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
            receipt.AddItem(item.MaterialCode, item.UnitOfMeasure, item.ExpectedQuantity, item.UnitPrice, item.OriginalDescription);
        }

        await repository.AddReceiptAsync(receipt, cancellationToken);
        await repository.AddAuditEntryAsync(
            SupplyAuditEntry.Create("receipt_created", userIdentifier, BuildAuditMetadata(receipt)),
            cancellationToken);

        foreach (var item in receipt.Items)
        {
            var payload = new
            {
                receiptId = receipt.Id,
                receiptItemId = item.Id,
                receiptNumber = receipt.ReceiptNumber,
                materialCode = item.MaterialCode,
                quantity = item.ExpectedQuantity,
                unitOfMeasure = item.UnitOfMeasure,
                unitPrice = item.UnitPrice,
                originalDescription = item.OriginalDescription,
                accessKey = receipt.AccessKey,
                source = "supply-chain"
            };

            await outbox.EnqueueAsync("supply.receipt_item_registered", payload, correlationId, cancellationToken);
        }

        return receipt;
    }

    private static string BuildAuditMetadata(MaterialReceipt receipt) =>
        JsonSerializer.Serialize(new
        {
            receiptId = receipt.Id,
            receipt.ReceiptNumber,
            receipt.DocumentNumber,
            itemCount = receipt.Items.Count
        });
}

public sealed record StageReceiptItemInput(string MaterialCode, decimal ExpectedQuantity, string UnitOfMeasure, decimal? UnitPrice, string? OriginalDescription);
