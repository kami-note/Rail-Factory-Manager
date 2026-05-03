namespace RailFactory.SupplyChain.Api.Application.Ports;

public interface INfeProvider
{
    ParsedReceiptDocument Parse(string xmlContent);
}

public sealed record ParsedReceiptDocument(
    string ReceiptNumber,
    string DocumentNumber,
    DateOnly ReceiptDate,
    string SupplierFiscalId,
    string SupplierName,
    IReadOnlyCollection<ParsedReceiptItem> Items);

public sealed record ParsedReceiptItem(string MaterialCode, decimal Quantity, string UnitOfMeasure);
