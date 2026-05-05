using System.Text.Json;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class MaterialReceiptWriter(
    ISupplyChainRepository repository,
    ISupplyOutbox outbox)
{
    public async Task<Supplier> ResolveOrCreateSupplierAsync(
        ParsedReceiptDocument parsed,
        IDictionary<string, Supplier> suppliersByFiscalId,
        CancellationToken cancellationToken)
    {
        if (suppliersByFiscalId.TryGetValue(parsed.SupplierFiscalId, out var cachedSupplier))
        {
            return cachedSupplier;
        }

        var supplier = await repository.GetSupplierByFiscalIdAsync(parsed.SupplierFiscalId, cancellationToken);
        if (supplier is null)
        {
            supplier = Supplier.Create(parsed.SupplierFiscalId, parsed.SupplierName);
            await repository.AddSupplierAsync(supplier, cancellationToken);
        }

        suppliersByFiscalId[parsed.SupplierFiscalId] = supplier;
        return supplier;
    }

    public async Task<MaterialReceipt> StageReceiptAsync(
        string tenantCode,
        string userIdentifier,
        string receiptNumber,
        Guid supplierId,
        string documentNumber,
        DateOnly receiptDate,
        IReadOnlyCollection<StageReceiptItemInput> items,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var existingReceipt = await repository.GetReceiptByReceiptNumberAsync(tenantCode, receiptNumber, cancellationToken);
        if (existingReceipt is not null)
        {
            throw new ReceiptAlreadyExistsException(receiptNumber);
        }

        var receipt = MaterialReceipt.Create(receiptNumber, supplierId, documentNumber, receiptDate, tenantCode);
        foreach (var item in items)
        {
            receipt.AddItem(item.MaterialCode, item.ExpectedQuantity, item.UnitOfMeasure);
        }

        await repository.AddReceiptAsync(receipt, cancellationToken);
        await repository.AddAuditEntryAsync(
            SupplyAuditEntry.Create(tenantCode, "receipt_created", userIdentifier, BuildAuditMetadata(receipt)),
            cancellationToken);

        foreach (var item in receipt.Items)
        {
            var payload = new
            {
                receiptId = receipt.Id,
                receiptItemId = item.Id,
                receiptNumber = receipt.ReceiptNumber,
                tenantCode,
                materialCode = item.MaterialCode,
                quantity = item.ExpectedQuantity,
                unitOfMeasure = item.UnitOfMeasure,
                source = "supply-chain"
            };

            await outbox.EnqueueAsync(tenantCode, "supply.receipt_item_registered", payload, correlationId, cancellationToken);
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

public sealed record StageReceiptItemInput(string MaterialCode, decimal ExpectedQuantity, string UnitOfMeasure);
