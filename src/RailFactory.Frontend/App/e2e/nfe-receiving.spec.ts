import { test, expect, TENANT_CODE } from './fixtures';
import * as fs from 'fs';
import * as path from 'path';

test.describe('End-to-End NFe Receiving Flow', () => {
  const tmpPathA = path.join('/tmp', 'NFE_TESTE_A.xml');
  const tmpPathB = path.join('/tmp', 'NFE_TESTE_B.xml');

  let skuCabo = '';
  let skuConect = '';
  let skuDisj = '';

  function buildSignedNfe(params: {
    accessKey: string;
    nNF: string;
    cnpj: string;
    xNome: string;
    items: Array<{
      cProd: string;
      xProd: string;
      uCom: string;
      qCom: number;
      vUnCom: number;
      nItem: number;
      ncm: string;
      gtin?: string;
    }>;
  }) {
    const totalProd = params.items.reduce((sum, item) => sum + item.qCom * item.vUnCom, 0);
    
    const detElements = params.items.map(item => {
      const vProd = (item.qCom * item.vUnCom).toFixed(2);
      const gtin = item.gtin || 'SEM GTIN';
      return `      <det nItem="${item.nItem}">
        <prod>
          <cProd>${item.cProd}</cProd>
          <cEAN>${gtin}</cEAN>
          <xProd>${item.xProd}</xProd>
          <NCM>${item.ncm}</NCM>
          <CFOP>5101</CFOP>
          <uCom>${item.uCom}</uCom>
          <qCom>${item.qCom.toFixed(4)}</qCom>
          <vUnCom>${item.vUnCom.toFixed(10)}</vUnCom>
          <vProd>${vProd}</vProd>
          <cEANTrib>${gtin}</cEANTrib>
          <uTrib>${item.uCom}</uTrib>
          <qTrib>${item.qCom.toFixed(4)}</qTrib>
          <vUnTrib>${item.vUnCom.toFixed(10)}</vUnTrib>
          <indTot>1</indTot>
        </prod>
        <imposto>
          <ICMS>
            <ICMS00>
              <orig>0</orig>
              <CST>00</CST>
              <modBC>3</modBC>
              <vBC>${vProd}</vBC>
              <pICMS>18.0000</pICMS>
              <vICMS>${(item.qCom * item.vUnCom * 0.18).toFixed(2)}</vICMS>
            </ICMS00>
          </ICMS>
          <PIS>
            <PISAliq>
              <CST>01</CST>
              <vBC>${vProd}</vBC>
              <pPIS>1.6500</pPIS>
              <vPIS>${(item.qCom * item.vUnCom * 0.0165).toFixed(2)}</vPIS>
            </PISAliq>
          </PIS>
          <COFINS>
            <COFINSAliq>
              <CST>01</CST>
              <vBC>${vProd}</vBC>
              <pCOFINS>7.6000</pCOFINS>
              <vCOFINS>${(item.qCom * item.vUnCom * 0.076).toFixed(2)}</vCOFINS>
            </COFINSAliq>
          </COFINS>
        </imposto>
      </det>`;
    }).join('\n');

    const vNF = totalProd.toFixed(2);
    const totalBC = totalProd.toFixed(2);
    const totalICMS = (totalProd * 0.18).toFixed(2);
    const totalPIS = (totalProd * 0.0165).toFixed(2);
    const totalCOFINS = (totalProd * 0.076).toFixed(2);

    return `<?xml version="1.0" encoding="UTF-8"?>
<nfeProc versao="4.00" xmlns="http://www.portalfiscal.inf.br/nfe">
  <NFe>
    <infNFe Id="NFe${params.accessKey}" versao="4.00">
      <ide>
        <cUF>35</cUF>
        <cNF>12345678</cNF>
        <natOp>Venda de mercadoria</natOp>
        <mod>55</mod>
        <serie>1</serie>
        <nNF>${params.nNF}</nNF>
        <dhEmi>2026-06-04T10:00:00-03:00</dhEmi>
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
        <CNPJ>${params.cnpj}</CNPJ>
        <xNome>${params.xNome}</xNome>
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
${detElements}
      <total>
        <ICMSTot>
          <vBC>${totalBC}</vBC>
          <vICMS>${totalICMS}</vICMS>
          <vICMSDeson>0.00</vICMSDeson>
          <vFCP>0.00</vFCP>
          <vBCST>0.00</vBCST>
          <vST>0.00</vST>
          <vFCPST>0.00</vFCPST>
          <vFCPSTRet>0.00</vFCPSTRet>
          <vProd>${vNF}</vProd>
          <vFrete>0.00</vFrete>
          <vSeg>0.00</vSeg>
          <vDesc>0.00</vDesc>
          <vII>0.00</vII>
          <vIPI>0.00</vIPI>
          <vIPIDevol>0.00</vIPIDevol>
          <vPIS>${totalPIS}</vPIS>
          <vCOFINS>${totalCOFINS}</vCOFINS>
          <vOutro>0.00</vOutro>
          <vNF>${vNF}</vNF>
        </ICMSTot>
      </total>
      <transp>
        <modFrete>9</modFrete>
      </transp>
      <pag>
        <detPag>
          <tPag>01</tPag>
          <vPag>${vNF}</vPag>
        </detPag>
      </pag>
    </infNFe>
    <Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
      <SignedInfo>
        <CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
        <SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" />
        <Reference URI="#NFe${params.accessKey}">
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
</nfeProc>`;
  }

  test.beforeAll(() => {
    const ts = Date.now().toString().slice(-7); // Get a unique 7-digit suffix
    const nNF_A = `10${ts}`; // e.g. 101234567 (9 digits)
    const nNF_B = `20${ts}`; // e.g. 201234567 (9 digits)

    const accessKeyA = `3526056491338241525455001${nNF_A.padStart(9, '0')}1256570814`.slice(0, 44);
    const accessKeyB = `3526059876543200019155001${nNF_B.padStart(9, '0')}1256570815`.slice(0, 44);

    skuCabo = `MAT-CABO-FLEX-4MM-${ts}`;
    skuConect = `MAT-CONECT-RJ45-${ts}`;
    skuDisj = `MAT-DISJ-63A-${ts}`;

    // Generate mock XML for Supplier A
    const xmlA = buildSignedNfe({
      accessKey: accessKeyA,
      nNF: nNF_A,
      cnpj: '64913382415254',
      xNome: 'Metalurgica Horizonte Ltda',
      items: [
        {
          cProd: 'MAT-ACO-1020',
          xProd: 'Chapa de Aco Carbono SAE 1020 - 1/4 pol',
          uCom: 'KG',
          qCom: 500,
          vUnCom: 12.50,
          nItem: 1,
          ncm: '72085100'
        },
        {
          cProd: 'MAT-LUB-ISO68',
          xProd: 'Oleo Lubrificante Industrial ISO VG 68',
          uCom: 'L',
          qCom: 200,
          vUnCom: 25.00,
          nItem: 2,
          ncm: '27101932',
          gtin: '7891234567890'
        }
      ]
    });

    // Generate mock XML for Supplier B
    const xmlB = buildSignedNfe({
      accessKey: accessKeyB,
      nNF: nNF_B,
      cnpj: '98765432000191',
      xNome: 'Eletro Componentes Brasil Ltda',
      items: [
        {
          cProd: `CABO-FLEX-4MM-${ts}`,
          xProd: 'Cabo Flexivel 4mm2 750V',
          uCom: 'RL',
          qCom: 10,
          vUnCom: 85.00,
          nItem: 1,
          ncm: '85444999',
          gtin: `7891${ts}001`
        },
        {
          cProd: `CONECT-RJ45-${ts}`,
          xProd: 'Conector RJ45 Cat6 Blindado Pct50',
          uCom: 'PCT',
          qCom: 4,
          vUnCom: 32.00,
          nItem: 2,
          ncm: '85366990',
          gtin: `7891${ts}002`
        },
        {
          cProd: `DISJ-63A-${ts}`,
          xProd: 'Disjuntor Bipolar 63A Curva C',
          uCom: 'UN',
          qCom: 5,
          vUnCom: 45.00,
          nItem: 3,
          ncm: '85362010',
          gtin: `7891${ts}003`
        }
      ]
    });

    fs.writeFileSync(tmpPathA, xmlA);
    fs.writeFileSync(tmpPathB, xmlB);
  });

  async function uploadXml(page, filePath) {
    const importBtn = page.getByRole('button', { name: /Importar XML/i });
    await expect(importBtn).toBeVisible();
    await importBtn.click();
    
    // Check if the step indicator is present
    await expect(page.getByText('PASSO 1 DE 2: UPLOAD')).toBeVisible();

    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles(filePath);
    
    // Check if step 2 indicator is present
    await expect(page.getByText('PASSO 2 DE 2: PRÉ-VISUALIZAÇÃO DA NOTA')).toBeVisible();

    const confirmBtn = page.getByRole('button', { name: /Confirmar e Importar/i });
    await expect(confirmBtn).toBeVisible();
    await confirmBtn.click();

    await expect(page.getByText(/XML importado com sucesso/i)).toBeVisible();
    // Close the modal by clicking outside or close button
    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);
  }

  test('executes the full NFe receiving flow', async ({ authedPage: page }) => {
    test.setTimeout(120000); // Allow enough time for the full flow

    // Phase 1 & 2: Import NFe A and B
    await test.step('Import NFe A and B', async () => {
      await page.goto('/app/receipts');
      await page.waitForURL('**/app/receipts');
      
      await uploadXml(page, tmpPathA);
      await page.reload(); // Refresh the list
      await uploadXml(page, tmpPathB);
      await page.reload();
    });

    // Phase 3: Conference NFe A
    await test.step('Conference NFe A', async () => {
      // Find the row for NFe A and click Iniciar
      const cardA = page.locator('.MuiCard-root', { hasText: 'Metalurgica Horizonte' }).first();
      const startBtn = cardA.getByRole('button', { name: /Iniciar/i });
      await expect(startBtn).toBeVisible();
      await startBtn.click();

      // Blind conference (Intentionally diverging)
      const inputs = page.locator('input[type="number"]');
      await expect(inputs).toHaveCount(2);
      await inputs.nth(0).fill('480'); // 500 expected
      await inputs.nth(1).fill('200');

      const finalizeBtn = page.getByRole('button', { name: /Finalizar Conferência/i });
      await finalizeBtn.click();

      // Ensure it's done
      await expect(page.getByRole('heading', { name: 'Recebimentos' })).toBeVisible();
    });

    // Phase 4: Association NFe B
    await test.step('SKU Association for NFe B', async () => {
      const cardB = page.locator('.MuiCard-root', { hasText: 'Eletro Componentes' }).first();
      const resolveBtn = cardB.getByRole('button', { name: /Resolver/i });
      await expect(resolveBtn).toBeVisible();
      await resolveBtn.click();

      // Should be in association workbench
      await expect(page.getByText('BANCADA DE ASSOCIAÇÃO')).toBeVisible();

      // Associate item 1
      await page.getByText('Cabo Flexivel 4mm2 750V', { exact: false }).first().click();
      await page.getByRole('tab', { name: /Criar Novo/i }).or(page.getByRole('button', { name: /Criar Novo/i })).first().click();
      await page.getByLabel('SKU Interno').fill(skuCabo);
      await page.getByRole('button', { name: /Criar e Vincular/i }).click();
      // Optimistic UI check
      await expect(page.getByText(skuCabo, { exact: true }).first()).toBeVisible();

      // Associate item 2
      await page.getByText('Conector RJ45 Cat6 Blindado Pct50', { exact: false }).first().click();
      await page.getByRole('tab', { name: /Criar Novo/i }).or(page.getByRole('button', { name: /Criar Novo/i })).first().click();
      await page.getByLabel('SKU Interno').fill(skuConect);
      await page.getByRole('button', { name: /Criar e Vincular/i }).click();
      // Optimistic UI check
      await expect(page.getByText(skuConect, { exact: true }).first()).toBeVisible();

      // Associate item 3
      await page.getByText('Disjuntor Bipolar 63A Curva C', { exact: false }).first().click();
      await page.getByRole('tab', { name: /Criar Novo/i }).or(page.getByRole('button', { name: /Criar Novo/i })).first().click();
      await page.getByLabel('SKU Interno').fill(skuDisj);
      await page.getByRole('button', { name: /Criar e Vincular/i }).click();
      // Optimistic UI check
      await expect(page.getByText(skuDisj, { exact: true }).first()).toBeVisible();

      // Release
      const releaseBtn = page.getByRole('button', { name: /Liberar/i }).first();
      await expect(releaseBtn).toBeEnabled();
      await releaseBtn.click();
    });

    // Phase 5: Conference NFe B
    await test.step('Conference NFe B', async () => {
      await page.goto('/app/receipts');
      
      const cardB = page.locator('.MuiCard-root', { hasText: 'Eletro Componentes' }).first();
      const startBtn = cardB.getByRole('button', { name: /Iniciar|Retomar/i });
      await expect(startBtn).toBeVisible();
      await startBtn.click();

      const inputs = page.locator('input[type="number"]');
      await expect(inputs).toHaveCount(3);
      await inputs.nth(0).fill('10');
      await inputs.nth(1).fill('4');
      await inputs.nth(2).fill('5');

      const finalizeBtn = page.getByRole('button', { name: /Finalizar Conferência/i });
      await finalizeBtn.click();
    });

    // Phase 6: Inventory Check
    await test.step('Inventory Check', async () => {
      await page.goto('/app/inventory');
      await page.getByRole('button', { name: /tabela/i }).click();
      await expect(page.locator('table')).toBeVisible();

      // Check for available items
      await expect(page.locator('tr', { hasText: skuCabo }).locator('text=Disponível')).toBeVisible();
      await expect(page.locator('tr', { hasText: skuConect }).locator('text=Disponível')).toBeVisible();
      await expect(page.locator('tr', { hasText: skuDisj }).locator('text=Disponível')).toBeVisible();
      
      // The NFe A item should be blocked due to divergence
      await expect(page.locator('tr', { hasText: 'MAT-ACO-1020' }).locator('text=Bloqueado').first()).toBeVisible();
    });
  });
});
