using System.Text.Json;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class CreateManualReceipt(
    ISupplyChainRepository repository,
    ISupplyOutbox outbox)
{
    public async Task<MaterialReceipt> ExecuteAsync(
        string tenantCode,
        string userIdentifier,
        string receiptNumber,
        Guid supplierId,
        string documentNumber,
        DateOnly receiptDate,
        IReadOnlyCollection<CreateManualReceiptItemInput> items,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var supplier = await repository.GetSupplierByIdAsync(supplierId, cancellationToken);
        if (supplier is null || !supplier.IsActive)
        {
            throw new InvalidOperationException("Supplier is invalid or inactive.");
        }

        var receipt = MaterialReceipt.Create(receiptNumber, supplierId, documentNumber, receiptDate, tenantCode);
        foreach (var item in items)
        {
            receipt.AddItem(item.MaterialCode, item.ExpectedQuantity, item.UnitOfMeasure);
        }

        await repository.AddReceiptAsync(receipt, cancellationToken);

        var auditMetadata = JsonSerializer.Serialize(new
        {
            receiptId = receipt.Id,
            receipt.ReceiptNumber,
            receipt.DocumentNumber,
            itemCount = receipt.Items.Count
        });

        await repository.AddAuditEntryAsync(
            SupplyAuditEntry.Create(tenantCode, "receipt_created", userIdentifier, auditMetadata),
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

        await repository.SaveChangesAsync(cancellationToken);
        return receipt;
    }
}

public sealed record CreateManualReceiptItemInput(string MaterialCode, decimal ExpectedQuantity, string UnitOfMeasure);
