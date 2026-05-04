using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class CreateManualReceipt(
    ISupplyChainRepository repository,
    MaterialReceiptWriter receiptWriter)
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

        var receipt = await receiptWriter.StageReceiptAsync(
            tenantCode,
            userIdentifier,
            receiptNumber,
            supplierId,
            documentNumber,
            receiptDate,
            items,
            correlationId,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return receipt;
    }
}

public sealed record CreateManualReceiptItemInput(string MaterialCode, decimal ExpectedQuantity, string UnitOfMeasure);
