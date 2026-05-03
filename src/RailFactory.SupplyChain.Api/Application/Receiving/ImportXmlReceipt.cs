using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Application.Suppliers;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class ImportXmlReceipt(
    INfeProvider nfeProvider,
    ISupplyChainRepository repository,
    CreateSupplier createSupplier,
    CreateManualReceipt createManualReceipt)
{
    public async Task<Guid> ExecuteAsync(
        string tenantCode,
        string userIdentifier,
        string xmlContent,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var parsed = nfeProvider.Parse(xmlContent);

        var existingSupplier = await repository.GetSupplierByFiscalIdAsync(parsed.SupplierFiscalId, cancellationToken);
        var supplierId = existingSupplier?.Id ?? (await createSupplier.ExecuteAsync(parsed.SupplierFiscalId, parsed.SupplierName, cancellationToken)).Id;

        var receipt = await createManualReceipt.ExecuteAsync(
            tenantCode,
            userIdentifier,
            parsed.ReceiptNumber,
            supplierId,
            parsed.DocumentNumber,
            parsed.ReceiptDate,
            parsed.Items.Select(x => new CreateManualReceiptItemInput(x.MaterialCode, x.Quantity, x.UnitOfMeasure)).ToList(),
            correlationId,
            cancellationToken);

        return receipt.Id;
    }
}
