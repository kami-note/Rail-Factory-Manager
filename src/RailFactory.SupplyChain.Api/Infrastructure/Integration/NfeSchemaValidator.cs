using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

internal sealed class NfeSchemaValidator
{
    private const string SchemaFileName = "nfe_v4.00.xsd";
    private readonly Lazy<XmlSchemaSet> schemaSet = new(LoadSchemas);

    public void Validate(XElement nfe)
    {
        var validationErrors = new List<string>();
        var isolatedNfeDocument = new XDocument(new XElement(nfe));
        isolatedNfeDocument.Validate(schemaSet.Value, (_, args) => validationErrors.Add(args.Message));

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException($"NF-e XML schema validation failed: {string.Join("; ", validationErrors)}");
        }
    }

    private static XmlSchemaSet LoadSchemas()
    {
        var schemas = new XmlSchemaSet
        {
            XmlResolver = new XmlUrlResolver()
        };

        using var schemaReader = XmlReader.Create(ResolveSchemaPath(), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });

        schemas.Add(NfeXmlConstants.Namespace, schemaReader);
        schemas.Compile();
        return schemas;
    }

    private static string ResolveSchemaPath()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure",
            "Integration",
            "Schemas",
            "NFe4.00",
            SchemaFileName);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"NF-e XSD schema file '{SchemaFileName}' was not found.");
        }

        return path;
    }
}
