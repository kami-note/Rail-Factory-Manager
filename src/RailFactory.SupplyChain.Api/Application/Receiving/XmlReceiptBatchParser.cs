using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class XmlReceiptBatchParser(INfeProvider nfeProvider)
{
    public IReadOnlyList<ParsedBatchDocument> Parse(IReadOnlyCollection<ImportXmlReceiptBatchDocument> documents)
    {
        var parsedDocuments = new List<ParsedBatchDocument>();
        var errors = new List<ImportXmlReceiptBatchError>();

        foreach (var document in documents)
        {
            try
            {
                parsedDocuments.Add(new ParsedBatchDocument(document.FileName, nfeProvider.Parse(document.XmlContent)));
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException)
            {
                errors.Add(new ImportXmlReceiptBatchError(document.FileName, ex.Message));
            }
        }

        errors.AddRange(parsedDocuments
            .GroupBy(x => x.Parsed.ReceiptNumber, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(x => new ImportXmlReceiptBatchError(
                x.FileName,
                $"Receipt number '{group.Key}' is duplicated in this batch."))));

        if (errors.Count > 0)
        {
            throw new ImportXmlReceiptBatchValidationException(errors);
        }

        return parsedDocuments;
    }
}
