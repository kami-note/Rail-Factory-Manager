using System.Globalization;
using System.Xml.Linq;
using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

internal sealed class NfeXmlParser
{
    public ParsedReceiptDocument Parse(XElement nfe)
    {
        var infNfe = nfe.Elements().FirstOrDefault(x => NfeXmlLocator.HasLocalName(x, "infNFe"))
            ?? throw new InvalidOperationException("NF-e infNFe section is required.");

        var ide = infNfe.Elements().FirstOrDefault(x => NfeXmlLocator.HasLocalName(x, "ide"))
            ?? throw new InvalidOperationException("NF-e ide section is required.");
        var emit = infNfe.Elements().FirstOrDefault(x => NfeXmlLocator.HasLocalName(x, "emit"))
            ?? throw new InvalidOperationException("NF-e emit section is required.");
        var total = infNfe.Elements().FirstOrDefault(x => NfeXmlLocator.HasLocalName(x, "total"))
            ?? throw new InvalidOperationException("NF-e total section is required.");
        var icmsTot = total.Elements().FirstOrDefault(x => NfeXmlLocator.HasLocalName(x, "ICMSTot"))
            ?? throw new InvalidOperationException("NF-e ICMSTot section is required.");

        var issuedAt = OptionalChildValue(ide, "dEmi") ?? OptionalChildValue(ide, "dhEmi")
            ?? throw new InvalidOperationException("NF-e issue date is required.");

        var supplierFiscalId = OptionalChildValue(emit, "CNPJ") ?? OptionalChildValue(emit, "CPF")
            ?? throw new InvalidOperationException("NF-e emitter fiscal id is required.");
        var supplierName = RequiredChildValue(emit, "xNome");

        var detailItems = infNfe.Elements().Where(x => NfeXmlLocator.HasLocalName(x, "det")).ToList();
        if (detailItems.Count == 0)
        {
            throw new InvalidOperationException("At least one NF-e item is required.");
        }

        var parsedItems = detailItems.Select(ParseItem).ToList();
        var accessKey = NormalizeAccessKey(infNfe.Attribute("Id")?.Value);
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            throw new InvalidOperationException("NF-e access key is required.");
        }

        var totalValueStr = OptionalChildValue(icmsTot, "vNF");
        var totalValue = totalValueStr != null ? ParseDecimal(totalValueStr) : (decimal?)null;

        return new ParsedReceiptDocument(
            $"NFE-{accessKey}",
            accessKey,
            accessKey,
            totalValue,
            OptionalChildValue(ide, "natOp"),
            ParseDate(issuedAt),
            supplierFiscalId,
            supplierName,
            parsedItems);
    }

    private static ParsedReceiptItem ParseItem(XElement detail)
    {
        var product = detail.Elements().FirstOrDefault(x => NfeXmlLocator.HasLocalName(x, "prod"))
            ?? throw new InvalidOperationException("NF-e product section is required.");

        var unitPriceStr = OptionalChildValue(product, "vUnCom");
        var unitPrice = unitPriceStr != null ? ParseDecimal(unitPriceStr) : (decimal?)null;
        var totalPriceStr = OptionalChildValue(product, "vProd");
        var totalPrice = totalPriceStr != null ? ParseDecimal(totalPriceStr) : (decimal?)null;
        var originalDescription = OptionalChildValue(product, "xProd");
        var purchaseOrderItem = OptionalChildValue(product, "nItemPed");

        return new ParsedReceiptItem(
            RequiredChildValue(product, "cProd"),
            ParseDecimal(RequiredChildValue(product, "qCom")),
            RequiredChildValue(product, "uCom"),
            unitPrice,
            totalPrice,
            originalDescription,
            OptionalChildValue(product, "NCM"),
            OptionalChildValue(product, "CFOP"),
            OptionalEan(product, "cEAN"),
            OptionalChildValue(product, "xPed"),
            int.TryParse(purchaseOrderItem, out var parsedItem) ? parsedItem : null);
    }

    private static string RequiredChildValue(XElement parent, string localName) =>
        OptionalChildValue(parent, localName) ?? throw new InvalidOperationException($"{localName} is required.");

    private static string? OptionalChildValue(XElement parent, string localName)
    {
        var value = parent.Elements().FirstOrDefault(x => NfeXmlLocator.HasLocalName(x, localName))?.Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? OptionalEan(XElement parent, string localName)
    {
        var value = OptionalChildValue(parent, localName);
        // SEFAZ uses "SEM GTIN" as a sentinel for products without a barcode
        return value == null || value.Equals("SEM GTIN", StringComparison.OrdinalIgnoreCase) ? null : value;
    }

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static DateOnly ParseDate(string value) =>
        DateOnly.Parse(value[..10], CultureInfo.InvariantCulture);

    private static string? NormalizeAccessKey(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return id.StartsWith("NFe", StringComparison.OrdinalIgnoreCase) ? id[3..] : id;
    }
}
