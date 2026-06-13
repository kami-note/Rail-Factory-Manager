## Diagram generation notes

Some diagram tooling (e.g., PlantUML / Java-based renderers) may generate a **JDK font cache** under `docs/diagram/**/.java/fonts/`.
That cache contains files with the header `*Do Not Edit*` and is **runtime-generated**; it must not be committed.

### Font requirements

To ensure consistent rendering, install the DejaVu font family:

- `DejaVu Sans`
- `DejaVu Serif`
- `DejaVu Sans Mono`
- `DejaVu Math TeX Gyre`

On Ubuntu:

```bash
sudo apt-get update
sudo apt-get install -y fonts-dejavu-core fonts-dejavu-extra
```

# Use Case Diagrams (PlantUML)

Diagramas de casos de uso extraídos de [phase0/USE-CASE-DIAGRAM.md](../phase0/USE-CASE-DIAGRAM.md).

## DEM (Modelagem)

Artefatos do **Documento de Especificacao de Modelagem (DEM)** ficam em `docs/diagram/dem/`:
- Diagramas em PlantUML (`.puml`) com sufixo de versao `-v2`
- Dicionario de dados em Markdown (`.md`)

## Versionamento

Os arquivos canônicos atuais seguem o sufixo **-v2**.

| Arquivo | Descrição |
|---------|-----------|
| `geral-use-case-v2.puml` | Diagrama geral — RAIL Factory com todos os pacotes (IAM, Production, Supply, Logistics, Fleet, HR, Dashboard). |
| `iam-use-case-v2.puml` | IAM (Identity & Access Management). |
| `production-use-case-v2.puml` | Production (Manufatura). |
| `supply-chain-use-case-v2.puml` | Supply Chain (Inbound). |
| `logistics-use-case-v2.puml` | Logistics (Outbound). |
| `fleet-use-case-v2.puml` | Fleet (Gestão de Frota). |
| `hr-use-case-v2.puml` | HR (Cadastro de Pessoas). |
| `dashboard-use-case-v2.puml` | Dashboard & Reporting. |

## TO-BE por Dominio

Foi adicionada uma visao **to-be** separada por dominio em `docs/diagram/to-be/`.

| Arquivo | Descricao |
|---------|-----------|
| `to-be/geral-use-case-to-be-v1.puml` | Diagrama geral to-be com todos os dominios agrupados. |
| `to-be/iam-use-case-to-be-v1.puml` | Dominio IAM (to-be). |
| `to-be/production-use-case-to-be-v1.puml` | Dominio Production (to-be). |
| `to-be/supply-chain-use-case-to-be-v1.puml` | Dominio Supply Chain (to-be). |
| `to-be/logistics-use-case-to-be-v1.puml` | Dominio Logistics (to-be). |
| `to-be/fleet-use-case-to-be-v1.puml` | Dominio Fleet (to-be). |
| `to-be/hr-use-case-to-be-v1.puml` | Dominio HR (to-be). |
| `to-be/dashboard-use-case-to-be-v1.puml` | Dominio Dashboard & Reporting (to-be). |

**Como usar:** Abra os `.puml` no VS Code/Cursor com a extensão [PlantUML](https://marketplace.visualstudio.com/items?itemName=jebbs.plantuml) para pré-visualizar e exportar PNG/SVG.

## Gerar imagens automaticamente

Na raiz do repositório:

```bash
./scripts/render-plantuml-diagrams.sh
```

- **Saída**: `docs/diagram/rendered/*.png`
- **SVG**: `FORMAT=svg ./scripts/render-plantuml-diagrams.sh`
- **Filtrar versão**: `VERSION_FILTER=v2 ./scripts/render-plantuml-diagrams.sh`

O script renderiza arquivos `**/*-v*.puml` de forma recursiva (incluindo `docs/diagram/dem/`).

Observacao: por padrao o PlantUML escreve as imagens em uma pasta `rendered/` ao lado do `.puml`. Ex.:

- Use cases: `docs/diagram/rendered/*.png`
- DEM: `docs/diagram/dem/rendered/*.png`
