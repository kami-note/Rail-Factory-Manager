using System.Xml.Linq;
using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

public sealed class BasicXmlNfeProvider : INfeProvider
{
    public ParsedReceiptDocument Parse(string xmlContent)
    {
        var document = XDocument.Parse(xmlContent);
        var root = document.Root ?? throw new InvalidOperationException("Invalid XML root.");

        var supplier = root.Element("supplier") ?? throw new InvalidOperationException("XML supplier section is required.");
        var items = root.Element("items")?.Elements("item").ToList() ?? [];
        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least one XML item is required.");
        }

        var parsedItems = items.Select(item => new ParsedReceiptItem(
            item.Element("materialCode")?.Value ?? throw new InvalidOperationException("materialCode is required."),
            decimal.Parse(item.Element("quantity")?.Value ?? throw new InvalidOperationException("quantity is required.")),
            item.Element("uom")?.Value ?? throw new InvalidOperationException("uom is required."))).ToList();

        return new ParsedReceiptDocument(
            root.Element("receiptNumber")?.Value ?? throw new InvalidOperationException("receiptNumber is required."),
            root.Element("documentNumber")?.Value ?? throw new InvalidOperationException("documentNumber is required."),
            DateOnly.Parse(root.Element("receiptDate")?.Value ?? throw new InvalidOperationException("receiptDate is required.")),
            supplier.Element("fiscalId")?.Value ?? throw new InvalidOperationException("supplier fiscalId is required."),
            supplier.Element("name")?.Value ?? throw new InvalidOperationException("supplier name is required."),
            parsedItems);
    }
}
