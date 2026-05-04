using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

public sealed class BasicXmlNfeProvider : INfeProvider
{
    private readonly SecureXmlDocumentLoader documentLoader;
    private readonly LegacyReceiptXmlParser legacyReceiptParser;
    private readonly NfeXmlParser nfeParser;
    private readonly NfeSchemaValidator nfeSchemaValidator;

    public BasicXmlNfeProvider()
        : this(new SecureXmlDocumentLoader(), new LegacyReceiptXmlParser(), new NfeXmlParser(), new NfeSchemaValidator())
    {
    }

    internal BasicXmlNfeProvider(
        SecureXmlDocumentLoader documentLoader,
        LegacyReceiptXmlParser legacyReceiptParser,
        NfeXmlParser nfeParser,
        NfeSchemaValidator nfeSchemaValidator)
    {
        this.documentLoader = documentLoader;
        this.legacyReceiptParser = legacyReceiptParser;
        this.nfeParser = nfeParser;
        this.nfeSchemaValidator = nfeSchemaValidator;
    }

    public ParsedReceiptDocument Parse(string xmlContent)
    {
        var document = documentLoader.Load(xmlContent);
        var root = document.Root ?? throw new InvalidOperationException("Invalid XML root.");

        if (LegacyReceiptXmlParser.CanParse(root))
        {
            return legacyReceiptParser.Parse(root);
        }

        var nfe = NfeXmlLocator.FindNfeElement(root)
            ?? throw new InvalidOperationException("XML document must be a legacy receipt or an NF-e document.");

        var version = NfeXmlLocator.ResolveNfeVersion(root, nfe);
        if (version == NfeXmlConstants.OfficialVersion)
        {
            nfeSchemaValidator.Validate(nfe);
        }
        else if (version != NfeXmlConstants.LegacyCompatibleVersion)
        {
            throw new InvalidOperationException(
                $"NF-e XML version '{version}' is not supported. Supported versions: {NfeXmlConstants.LegacyCompatibleVersion}, {NfeXmlConstants.OfficialVersion}.");
        }

        return nfeParser.Parse(nfe);
    }
}
