using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class ImportXmlReceiptBatch(
    XmlReceiptBatchParser batchParser,
    ISupplyChainRepository repository,
    MaterialReceiptWriter receiptWriter,
    ISupplyChainTransactionRunner transactionRunner)
{
    public async Task<BatchImportSummary> ExecuteAsync(
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
        // We still validate global duplicates, but we could also move this into the resilient loop
        await ValidateDuplicatesAsync(parsedDocuments, cancellationToken);

        var successful = new List<ImportedReceiptResult>();
        var failed = new List<ImportXmlReceiptBatchError>();
        var suppliersByFiscalId = new Dictionary<string, Supplier>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in parsedDocuments)
        {
            try
            {
                await transactionRunner.ExecuteAsync(async ct =>
                {
                    var supplier = await receiptWriter.ResolveOrCreateSupplierAsync(document.Parsed, suppliersByFiscalId, ct);
                    var receipt = await receiptWriter.StageReceiptAsync(
                        userIdentifier,
                        document.Parsed.ReceiptNumber,
                        supplier,
                        document.Parsed.DocumentNumber,
                        document.Parsed.AccessKey,
                        document.Parsed.TotalValue,
                        document.XmlContent,
                        document.Parsed.ReceiptDate,
                        document.Parsed.Items
                            .Select(x => new StageReceiptItemInput(x.MaterialCode, x.Quantity, x.UnitOfMeasure, x.UnitPrice, x.OriginalDescription, x.Ncm, x.Ean))
                            .ToList(),
                        correlationId,
                        ct,
                        fiscalEnvironment: document.Parsed.FiscalEnvironment);

                    await repository.SaveChangesAsync(ct);

                    successful.Add(new ImportedReceiptResult(
                        document.FileName,
                        receipt.Id,
                        receipt.ReceiptNumber,
                        receipt.DocumentNumber));
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                failed.Add(new ImportXmlReceiptBatchError(document.FileName, ex.Message));
            }
        }

        return new BatchImportSummary(successful, failed);
    }

    private async Task ValidateDuplicatesAsync(IReadOnlyCollection<ParsedBatchDocument> parsedDocuments, CancellationToken cancellationToken)
    {
        var errors = new List<ImportXmlReceiptBatchError>();

        foreach (var document in parsedDocuments)
        {
            var existingReceipt = await repository.GetReceiptByReceiptNumberAsync(document.Parsed.ReceiptNumber, cancellationToken);
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

public sealed record ParsedBatchDocument(string FileName, string XmlContent, ParsedReceiptDocument Parsed);

public sealed record BatchImportSummary(
    IReadOnlyList<ImportedReceiptResult> SuccessfulImports, 
    IReadOnlyList<ImportXmlReceiptBatchError> FailedImports);
