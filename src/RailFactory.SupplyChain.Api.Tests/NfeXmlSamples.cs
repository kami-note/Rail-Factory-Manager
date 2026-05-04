namespace RailFactory.SupplyChain.Api.Tests;

internal static class NfeXmlSamples
{
    public static string BuildOfficialNfe(string accessKey, string materialCode) =>
        $$"""
        <NFe xmlns="http://www.portalfiscal.inf.br/nfe">
            <infNFe Id="NFe{{accessKey}}" versao="4.00">
                <ide>
                    <cUF>35</cUF>
                    <cNF>12345678</cNF>
                    <natOp>Purchase</natOp>
                    <mod>55</mod>
                    <serie>1</serie>
                    <nNF>1</nNF>
                    <dhEmi>2026-05-03T12:30:00-03:00</dhEmi>
                    <tpNF>1</tpNF>
                    <idDest>1</idDest>
                    <cMunFG>3550308</cMunFG>
                    <tpImp>1</tpImp>
                    <tpEmis>1</tpEmis>
                    <cDV>3</cDV>
                    <tpAmb>2</tpAmb>
                    <finNFe>1</finNFe>
                    <indFinal>0</indFinal>
                    <indPres>9</indPres>
                    <procEmi>0</procEmi>
                    <verProc>RailFactory</verProc>
                </ide>
                <emit>
                    <CNPJ>99999090910270</CNPJ>
                    <xNome>NF-e Associacao NF-e</xNome>
                    <enderEmit>
                        <xLgr>Main Street</xLgr>
                        <nro>100</nro>
                        <xBairro>Centro</xBairro>
                        <cMun>3550308</cMun>
                        <xMun>Sao Paulo</xMun>
                        <UF>SP</UF>
                        <CEP>01001000</CEP>
                        <cPais>1058</cPais>
                        <xPais>BRASIL</xPais>
                        <fone>1133334444</fone>
                    </enderEmit>
                    <IE>123456789012</IE>
                    <CRT>3</CRT>
                </emit>
                <det nItem="1">
                    <prod>
                        <cProd>{{materialCode}}</cProd>
                        <cEAN>SEM GTIN</cEAN>
                        <xProd>Material {{materialCode}}</xProd>
                        <NCM>01012100</NCM>
                        <CFOP>5102</CFOP>
                        <uCom>UN</uCom>
                        <qCom>10.0000</qCom>
                        <vUnCom>1.0000000000</vUnCom>
                        <vProd>10.00</vProd>
                        <cEANTrib>SEM GTIN</cEANTrib>
                        <uTrib>UN</uTrib>
                        <qTrib>10.0000</qTrib>
                        <vUnTrib>1.0000000000</vUnTrib>
                        <indTot>1</indTot>
                    </prod>
                    <imposto>
                        <ICMS>
                            <ICMS00>
                                <orig>0</orig>
                                <CST>00</CST>
                                <modBC>3</modBC>
                                <vBC>10.00</vBC>
                                <pICMS>18.0000</pICMS>
                                <vICMS>1.80</vICMS>
                            </ICMS00>
                        </ICMS>
                        <PIS>
                            <PISAliq>
                                <CST>01</CST>
                                <vBC>10.00</vBC>
                                <pPIS>1.6500</pPIS>
                                <vPIS>0.17</vPIS>
                            </PISAliq>
                        </PIS>
                        <COFINS>
                            <COFINSAliq>
                                <CST>01</CST>
                                <vBC>10.00</vBC>
                                <pCOFINS>7.6000</pCOFINS>
                                <vCOFINS>0.76</vCOFINS>
                            </COFINSAliq>
                        </COFINS>
                    </imposto>
                </det>
                <total>
                    <ICMSTot>
                        <vBC>10.00</vBC>
                        <vICMS>1.80</vICMS>
                        <vICMSDeson>0.00</vICMSDeson>
                        <vFCP>0.00</vFCP>
                        <vBCST>0.00</vBCST>
                        <vST>0.00</vST>
                        <vFCPST>0.00</vFCPST>
                        <vFCPSTRet>0.00</vFCPSTRet>
                        <vProd>10.00</vProd>
                        <vFrete>0.00</vFrete>
                        <vSeg>0.00</vSeg>
                        <vDesc>0.00</vDesc>
                        <vII>0.00</vII>
                        <vIPI>0.00</vIPI>
                        <vIPIDevol>0.00</vIPIDevol>
                        <vPIS>0.17</vPIS>
                        <vCOFINS>0.76</vCOFINS>
                        <vOutro>0.00</vOutro>
                        <vNF>10.00</vNF>
                    </ICMSTot>
                </total>
                <transp>
                    <modFrete>9</modFrete>
                </transp>
                <pag>
                    <detPag>
                        <tPag>01</tPag>
                        <vPag>10.00</vPag>
                    </detPag>
                </pag>
            </infNFe>
            <Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
                <SignedInfo>
                    <CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
                    <SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" />
                    <Reference URI="#NFe{{accessKey}}">
                        <Transforms>
                            <Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" />
                            <Transform Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
                        </Transforms>
                        <DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" />
                        <DigestValue>AA==</DigestValue>
                    </Reference>
                </SignedInfo>
                <SignatureValue>AA==</SignatureValue>
                <KeyInfo>
                    <X509Data>
                        <X509Certificate>AA==</X509Certificate>
                    </X509Data>
                </KeyInfo>
            </Signature>
        </NFe>
        """;
}
