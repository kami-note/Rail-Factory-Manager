using RailFactory.SupplyChain.Api.Infrastructure.Integration;
using Xunit;

namespace RailFactory.SupplyChain.Api.Tests;

public sealed class BasicXmlNfeProviderTests
{
    [Fact]
    public void Parse_accepts_signed_nfe_xml_with_namespace()
    {
        const string xml = """
            <NFe xmlns="http://www.portalfiscal.inf.br/nfe">
                <infNFe Id="NFe35080599999090910270550010000000015180051273" versao="1.10">
                    <total>
                        <ICMSTot>
                            <vNF>6000000.00</vNF>
                        </ICMSTot>
                    </total>
                    <ide>
                        <serie>1</serie>
                        <nNF>1</nNF>
                        <dEmi>2008-05-06</dEmi>
                    </ide>
                    <emit>
                        <CNPJ>99999090910270</CNPJ>
                        <xNome>NF-e Associacao NF-e</xNome>
                    </emit>
                    <det nItem="1">
                        <prod>
                            <cProd>00001</cProd>
                            <uCom>dz</uCom>
                            <qCom>1000000.0000</qCom>
                        </prod>
                    </det>
                    <det nItem="2">
                        <prod>
                            <cProd>00002</cProd>
                            <uCom>pack</uCom>
                            <qCom>5000000.0000</qCom>
                        </prod>
                    </det>
                </infNFe>
                <Signature xmlns="http://www.w3.org/2000/09/xmldsig#" />
            </NFe>
            """;

        var parsed = new BasicXmlNfeProvider().Parse(xml);

        Assert.Equal("NFE-35080599999090910270550010000000015180051273", parsed.ReceiptNumber);
        Assert.Equal("35080599999090910270550010000000015180051273", parsed.DocumentNumber);
        Assert.Equal(new DateOnly(2008, 5, 6), parsed.ReceiptDate);
        Assert.Equal("99999090910270", parsed.SupplierFiscalId);
        Assert.Equal("NF-e Associacao NF-e", parsed.SupplierName);
        Assert.Collection(
            parsed.Items,
            first =>
            {
                Assert.Equal("00001", first.MaterialCode);
                Assert.Equal(1000000.0000m, first.Quantity);
                Assert.Equal("dz", first.UnitOfMeasure);
            },
            second =>
            {
                Assert.Equal("00002", second.MaterialCode);
                Assert.Equal(5000000.0000m, second.Quantity);
                Assert.Equal("pack", second.UnitOfMeasure);
            });
    }

    [Fact]
    public void Parse_accepts_nfe_proc_4_xml_with_issue_datetime()
    {
        var xml = $$"""
            <nfeProc xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
                {{NfeXmlSamples.BuildOfficialNfe("35260599999090910270550010000000015180051273", "MAT-001")}}
                <protNFe>
                    <infProt />
                </protNFe>
            </nfeProc>
            """;

        var parsed = new BasicXmlNfeProvider().Parse(xml);

        Assert.Equal("NFE-35260599999090910270550010000000015180051273", parsed.ReceiptNumber);
        Assert.Equal("35260599999090910270550010000000015180051273", parsed.DocumentNumber);
        Assert.Equal("Purchase", parsed.OperationNature);
        Assert.Equal(new DateOnly(2026, 5, 3), parsed.ReceiptDate);
        Assert.Collection(parsed.Items, item =>
        {
            Assert.Equal("MAT-001", item.MaterialCode);
            Assert.Equal("Material MAT-001", item.OriginalDescription);
            Assert.Equal(1.0000000000m, item.UnitPrice);
            Assert.Equal(10.00m, item.TotalPrice);
            Assert.Equal("01012100", item.Ncm);
            Assert.Equal("5102", item.Cfop);
            Assert.Null(item.Ean);
        });
    }

    [Fact]
    public void Parse_rejects_invalid_nfe_schema()
    {
        const string xml = """
            <NFe xmlns="http://www.portalfiscal.inf.br/nfe">
                <infNFe versao="4.00">
                    <ide />
                </infNFe>
            </NFe>
            """;

        var error = Assert.Throws<InvalidOperationException>(() => new BasicXmlNfeProvider().Parse(xml));

        Assert.Contains("schema validation failed", error.Message);
    }

    [Fact]
    public void Parse_keeps_legacy_receipt_xml_contract()
    {
        const string xml = """
            <receipt>
                <receiptNumber>RCPT-XML-001</receiptNumber>
                <documentNumber>DOC-XML-001</documentNumber>
                <receiptDate>2026-05-03</receiptDate>
                <supplier>
                    <fiscalId>99887766000100</fiscalId>
                    <name>XML Supplier</name>
                </supplier>
                <items>
                    <item>
                        <materialCode>MAT-XML-001</materialCode>
                        <quantity>5.5</quantity>
                        <uom>UN</uom>
                    </item>
                </items>
            </receipt>
            """;

        var parsed = new BasicXmlNfeProvider().Parse(xml);

        Assert.Equal("RCPT-XML-001", parsed.ReceiptNumber);
        Assert.Equal("DOC-XML-001", parsed.DocumentNumber);
        Assert.Equal("99887766000100", parsed.SupplierFiscalId);
        Assert.Collection(parsed.Items, item => Assert.Equal(5.5m, item.Quantity));
    }
}
