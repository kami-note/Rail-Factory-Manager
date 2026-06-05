using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class ImportXmlReceipt(
    INfeProvider nfeProvider,
    ISupplyChainRepository repository,
    MaterialReceiptWriter receiptWriter)
{
    public async Task<Guid> ExecuteAsync(
        string userIdentifier,
        string xmlContent,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var parsed = nfeProvider.Parse(xmlContent);

        var supplier = await receiptWriter.ResolveOrCreateSupplierAsync(
            parsed,
            new Dictionary<string, Supplier>(StringComparer.OrdinalIgnoreCase),
            cancellationToken);

        var receipt = await receiptWriter.StageReceiptAsync(
            userIdentifier,
            parsed.ReceiptNumber,
            supplier,
            parsed.DocumentNumber,
            parsed.AccessKey,
            parsed.TotalValue,
            xmlContent,
            parsed.ReceiptDate,
            parsed.Items.Select(x => new StageReceiptItemInput(x.MaterialCode, x.Quantity, x.UnitOfMeasure, x.UnitPrice, x.OriginalDescription, x.Ncm, x.Ean)).ToList(),
            correlationId,
            cancellationToken,
            fiscalEnvironment: parsed.FiscalEnvironment);

        await repository.SaveChangesAsync(cancellationToken);
        return receipt.Id;
    }
}
