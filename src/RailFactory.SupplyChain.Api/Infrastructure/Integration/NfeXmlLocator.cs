using System.Xml.Linq;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

internal static class NfeXmlLocator
{
    public static XElement? FindNfeElement(XElement root)
    {
        if (IsNfeElement(root))
        {
            return root;
        }

        return root.Descendants().FirstOrDefault(IsNfeElement);
    }

    public static string ResolveNfeVersion(XElement root, XElement nfe)
    {
        var infNfe = nfe.Elements().FirstOrDefault(x => HasLocalName(x, "infNFe"))
            ?? throw new InvalidOperationException("NF-e infNFe section is required.");

        var version = infNfe.Attribute("versao")?.Value.Trim();
        if (string.IsNullOrWhiteSpace(version) && HasLocalName(root, "nfeProc"))
        {
            version = root.Attribute("versao")?.Value.Trim();
        }

        return string.IsNullOrWhiteSpace(version)
            ? throw new InvalidOperationException("NF-e XML version is required.")
            : version;
    }

    public static bool HasLocalName(XElement element, string localName) =>
        string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal);

    private static bool IsNfeElement(XElement element) =>
        HasLocalName(element, "NFe") && element.Name.NamespaceName == NfeXmlConstants.Namespace;
}
