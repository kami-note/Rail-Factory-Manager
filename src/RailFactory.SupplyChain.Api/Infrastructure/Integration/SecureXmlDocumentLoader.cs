using System.Xml;
using System.Xml.Linq;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

internal sealed class SecureXmlDocumentLoader
{
    public XDocument Load(string xmlContent)
    {
        try
        {
            using var stringReader = new StringReader(xmlContent);
            using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            return XDocument.Load(xmlReader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException($"Invalid XML: {ex.Message}", ex);
        }
    }
}
