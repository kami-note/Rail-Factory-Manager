using System.Globalization;
using System.Xml.Linq;
using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

internal sealed class LegacyReceiptXmlParser
{
    public static bool CanParse(XElement root) =>
        string.Equals(root.Name.LocalName, "receipt", StringComparison.Ordinal) && string.IsNullOrEmpty(root.Name.NamespaceName);

    public ParsedReceiptDocument Parse(XElement root)
    {
        var supplier = root.Element("supplier") ?? throw new InvalidOperationException("XML supplier section is required.");
        var items = root.Element("items")?.Elements("item").ToList() ?? [];
        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least one XML item is required.");
        }

        var parsedItems = items.Select(item => new ParsedReceiptItem(
            RequiredValue(item, "materialCode"),
            ParseDecimal(RequiredValue(item, "quantity")),
            RequiredValue(item, "uom"),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null)).ToList();

        return new ParsedReceiptDocument(
            RequiredValue(root, "receiptNumber"),
            RequiredValue(root, "documentNumber"),
            null,
            null,
            null,
            ParseDate(RequiredValue(root, "receiptDate")),
            RequiredValue(supplier, "fiscalId"),
            RequiredValue(supplier, "name"),
            parsedItems);
    }

    private static string RequiredValue(XElement parent, string localName)
    {
        var value = parent.Element(localName)?.Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{localName} is required.") : value;
    }

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static DateOnly ParseDate(string value) =>
        DateOnly.Parse(value[..10], CultureInfo.InvariantCulture);
}
