using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class ImportXmlReceiptBatch(
    XmlReceiptBatchParser batchParser,
    ISupplyChainRepository repository,
    MaterialReceiptWriter receiptWriter,
    ISupplyChainTransactionRunner transactionRunner)
{
    public async Task<IReadOnlyList<ImportedReceiptResult>> ExecuteAsync(
        string tenantCode,
        string userIdentifier,
        IReadOnlyCollection<ImportXmlReceiptBatchDocument> documents,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            throw new ImportXmlReceiptBatchValidationException(
            [
                new ImportXmlReceiptBatchError("request", "At least one XML document is required.")
            ]);
        }

        var parsedDocuments = batchParser.Parse(documents);
        await ValidateDuplicatesAsync(tenantCode, parsedDocuments, cancellationToken);

        var imported = new List<ImportedReceiptResult>();
        var suppliersByFiscalId = new Dictionary<string, Supplier>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await transactionRunner.ExecuteAsync(async ct =>
            {
                foreach (var document in parsedDocuments)
                {
                    var supplier = await receiptWriter.ResolveOrCreateSupplierAsync(document.Parsed, suppliersByFiscalId, ct);
                    var receipt = await receiptWriter.StageReceiptAsync(
                        tenantCode,
                        userIdentifier,
                        document.Parsed.ReceiptNumber,
                        supplier.Id,
                        document.Parsed.DocumentNumber,
                        document.Parsed.ReceiptDate,
                        document.Parsed.Items
                            .Select(x => new CreateManualReceiptItemInput(x.MaterialCode, x.Quantity, x.UnitOfMeasure))
                            .ToList(),
                        correlationId,
                        ct);

                    imported.Add(new ImportedReceiptResult(
                        document.FileName,
                        receipt.Id,
                        receipt.ReceiptNumber,
                        receipt.DocumentNumber));
                }

                await repository.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (ReceiptAlreadyExistsException ex)
        {
            throw new ImportXmlReceiptBatchValidationException(
            [
                new ImportXmlReceiptBatchError("request", ex.Message)
            ]);
        }

        return imported;
    }

    private async Task ValidateDuplicatesAsync(string tenantCode, IReadOnlyCollection<ParsedBatchDocument> parsedDocuments, CancellationToken cancellationToken)
    {
        var errors = new List<ImportXmlReceiptBatchError>();

        foreach (var document in parsedDocuments)
        {
            var existingReceipt = await repository.GetReceiptByReceiptNumberAsync(tenantCode, document.Parsed.ReceiptNumber, cancellationToken);
            if (existingReceipt is not null)
            {
                errors.Add(new ImportXmlReceiptBatchError(
                    document.FileName,
                    $"Receipt number '{document.Parsed.ReceiptNumber}' already exists."));
            }
        }

        if (errors.Count > 0)
        {
            throw new ImportXmlReceiptBatchValidationException(errors);
        }
    }
}

public sealed record ImportXmlReceiptBatchDocument(string FileName, string XmlContent);

public sealed record ImportedReceiptResult(string FileName, Guid ReceiptId, string ReceiptNumber, string DocumentNumber);

public sealed record ImportXmlReceiptBatchError(string FileName, string Message);

public sealed class ImportXmlReceiptBatchValidationException(IReadOnlyCollection<ImportXmlReceiptBatchError> errors) : InvalidOperationException("XML batch import is invalid.")
{
    public IReadOnlyCollection<ImportXmlReceiptBatchError> Errors { get; } = errors;
}

public sealed record ParsedBatchDocument(string FileName, ParsedReceiptDocument Parsed);
