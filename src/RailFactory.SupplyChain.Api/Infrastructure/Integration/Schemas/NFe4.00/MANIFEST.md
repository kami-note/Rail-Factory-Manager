# NF-e 4.00 XML Schemas

Source: Portal da Nota Fiscal Eletronica, "Esquema XML NF-e/NFC-e - Pacote de Liberacao no 010c (NT 2022.002 v.1.30) (ZIP)", published on 2026-03-26.

Downloaded from:

`https://www.nfe.fazenda.gov.br/POrtal/exibirArquivo.aspx?conteudo=%20ZqiLFb5FGE=`

Package filename from HTTP response:

`PL_010c_NT2022_002v1.30.zip`

Package SHA-256:

`aceb8df3ba5235aea4dcac2502a68ef00c6cd34f1d26cb3f6eebae9d675cd0fc`

Included files and SHA-256:

- `DFeTiposBasicos_v1.00.xsd`: `c1c1f700de03da50c82f3fbf23db7e98929b5d1ee1bdedb4d546e33efa498ee6`
- `leiauteNFe_v4.00.xsd`: `7d8af488538fe78809088f9c494cb7521ae856982210fd13a25496ccf429c8c1`
- `nfe_v4.00.xsd`: `66a117aaa78687fdb1355fc32a380ec859f3a98e850d7c4de5a07935eb1a6030`
- `tiposBasico_v4.00.xsd`: `63d393d69fb63568e39277d9794348fbe107e1c15d2dbce32fb63b6e41472c6d`
- `xmldsig-core-schema_v1.01.xsd`: `f56744a5f51c03f027de13f39f869307091781a9ef1d91b1ebe14719ce28e1ac`

Operational note: this official current package validates the `NFe` element. It does not include `procNFe_v4.00.xsd`; when a received XML is wrapped in `nfeProc`, the importer validates the contained `NFe` element against the official schema and then extracts receipt data. Digital signature and certificate-chain validation are intentionally out of scope for the current parser/XSD implementation.
