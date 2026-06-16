import { test, expect } from './fixtures';
import * as fs from 'fs';
import * as path from 'path';

// Helper to generate a valid CPF (digits only)
function generateCPF(): string {
  const base = Math.floor(100000000 + Math.random() * 900000000).toString().slice(0, 9);
  
  let sum = 0;
  for (let i = 0; i < 9; i++) {
    sum += parseInt(base[i], 10) * (10 - i);
  }
  let d1 = 11 - (sum % 11);
  if (d1 === 10 || d1 === 11) d1 = 0;

  sum = 0;
  const baseWithD1 = base + d1.toString();
  for (let i = 0; i < 10; i++) {
    sum += parseInt(baseWithD1[i], 10) * (11 - i);
  }
  let d2 = 11 - (sum % 11);
  if (d2 === 10 || d2 === 11) d2 = 0;

  return base + d1.toString() + d2.toString();
}

// Helper to generate a valid CNPJ (formatted for UI, or unformatted for XML)
function generateCNPJ(formatted = true): string {
  const base = (Math.floor(10000000 + Math.random() * 90000000).toString().slice(0, 8) + '0001').slice(0, 12);
  
  let sum = 0;
  let weight = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  for (let i = 0; i < 12; i++) {
    sum += parseInt(base[i], 10) * weight[i];
  }
  let d1 = sum % 11 < 2 ? 0 : 11 - (sum % 11);

  sum = 0;
  weight = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const baseWithD1 = base + d1.toString();
  for (let i = 0; i < 13; i++) {
    sum += parseInt(baseWithD1[i], 10) * weight[i];
  }
  let d2 = sum % 11 < 2 ? 0 : 11 - (sum % 11);

  const clean = base + d1.toString() + d2.toString();
  if (!formatted) return clean;
  return `${clean.slice(0, 2)}.${clean.slice(2, 5)}.${clean.slice(5, 8)}/${clean.slice(8, 12)}-${clean.slice(12, 14)}`;
}

// Helper to generate NF-e XML layout
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
        <cNF>98765432</cNF>
        <natOp>Venda de mercadoria</natOp>
        <mod>55</mod>
        <serie>1</serie>
        <nNF>${params.nNF}</nNF>
        <dhEmi>2026-06-15T10:00:00-03:00</dhEmi>
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
          <xLgr>Avenida Industrial</xLgr>
          <nro>4500</nro>
          <xBairro>Distrito Industrial</xBairro>
          <cMun>3550308</cMun>
          <xMun>Sao Paulo</xMun>
          <UF>SP</UF>
          <CEP>01002000</CEP>
          <cPais>1058</cPais>
          <xPais>BRASIL</xPais>
          <fone>1132009000</fone>
        </enderEmit>
        <IE>987654321012</IE>
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

test.describe.serial('Presentation Data Seeding via Playwright', () => {
  const nfePath = path.join('/tmp', 'NFE_ACOS_RIO.xml');
  const uniqueId = Date.now().toString().slice(-6);
  
  test.beforeAll(() => {
    // Generate NF-e file
    const nNF = `98${uniqueId}`;
    const accessKey = `3526061234567800010055001${nNF.padStart(9, '0')}1256570811`.slice(0, 44);
    
    const xml = buildSignedNfe({
      accessKey,
      nNF,
      cnpj: '12345678000100',
      xNome: 'Aços Especiais Rio S/A',
      items: [
        {
          cProd: 'ACO-GALV-2MM',
          xProd: 'Chapa de Aço Galvanizado 2mm',
          uCom: 'KG',
          qCom: 3000,
          vUnCom: 10.50,
          nItem: 1,
          ncm: '72085100'
        },
        {
          cProd: 'ACO-PERFIL-I',
          xProd: 'Perfil I de Aço Carbono 6 pol',
          uCom: 'M',
          qCom: 200,
          vUnCom: 48.00,
          nItem: 2,
          ncm: '72163300'
        },
        {
          cProd: 'PRO-TR-100',
          xProd: 'Trilho Ferroviário TR-100',
          uCom: 'UN',
          qCom: 15,
          vUnCom: 180.00,
          nItem: 3,
          ncm: '73021010'
        }
      ]
    });
    fs.writeFileSync(nfePath, xml);
  });

  test.afterAll(() => {
    try {
      fs.unlinkSync(nfePath);
    } catch {}
  });

  test('1. Setup HR Employees', async ({ authedPage: page }) => {
    const people = [
      { name: 'Carlos Henrique Silva', type: 'Colaborador', email: 'carlos.silva@railfactory.com.br' },
      { name: 'Marcos Aurélio Souza', type: 'Motorista', email: 'marcos.souza@railfactory.com.br' },
      { name: 'Fernanda Ferreira Lima', type: 'Colaborador', email: 'fernanda.lima@railfactory.com.br' },
    ];

    await page.goto('/app/hr/people');
    await page.waitForURL('**/app/hr/people');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    for (const p of people) {
      await page.getByRole('button', { name: /nova pessoa/i }).click();
      const dialog = page.getByRole('dialog').filter({ hasText: 'Nova Pessoa' });
      await dialog.getByLabel('Nome completo').fill(p.name);
      await dialog.getByLabel(/cpf/i).fill(generateCPF());
      await dialog.getByLabel(/e-mail/i).fill(p.email);
      
      await dialog.locator('div[role="combobox"]').click();
      await page.getByRole('listbox').getByText(p.type, { exact: true }).click();
      
      await dialog.getByRole('button', { name: /cadastrar/i }).click();
      await expect(dialog).toHaveCount(0, { timeout: 10_000 });
      await expect(page.getByRole('alert').filter({ hasText: /cadastrada com sucesso/i })).toBeVisible();
      // Dismiss alert if it blocks UI
      await page.keyboard.press('Escape');
    }
  });

  test('2. Setup Fleet & Assign Driver', async ({ authedPage: page }) => {
    // 2.1 Create Truck
    await page.goto('/app/fleet');
    await page.waitForURL('**/app/fleet');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    await page.getByRole('button', { name: /novo veículo/i }).click();
    const vehicleDialog = page.getByRole('dialog').filter({ hasText: 'Novo Veículo' });
    await vehicleDialog.getByLabel('Placa').fill('RFX1020');
    
    const chars = 'ABCDEFGHJKLMNPRSTUVWXYZ0123456789';
    let chassis = '';
    for (let i = 0; i < 17; i++) chassis += chars[Math.floor(Math.random() * chars.length)];
    await vehicleDialog.getByLabel('Chassi').fill(chassis);

    let renavam = '';
    for (let i = 0; i < 11; i++) renavam += Math.floor(Math.random() * 10).toString();
    await vehicleDialog.getByLabel('RENAVAM').fill(renavam);

    await vehicleDialog.locator('div[role="combobox"]').click();
    await page.getByRole('listbox').getByText('Caminhão', { exact: true }).click();

    await vehicleDialog.getByLabel(/carga máx/i).fill('15000');
    await vehicleDialog.getByLabel(/volume máx/i).fill('60');
    await vehicleDialog.getByLabel(/vencimento crlv/i).fill('2027-06-30');

    await vehicleDialog.getByRole('button', { name: /cadastrar/i }).click();
    await expect(vehicleDialog).toHaveCount(0, { timeout: 10_000 });
    await expect(page.getByRole('alert').filter({ hasText: /cadastrado/i })).toBeVisible();
    await page.keyboard.press('Escape');

    // 2.2 Allocate Driver (Marcos Aurélio Souza) to RFX-1020
    const row = page.getByRole('row').filter({ hasText: 'RFX-1020' }).first();
    await expect(row).toBeVisible();
    await row.click(); // Open details drawer

    const drawer = page.getByRole('presentation').locator('.MuiDrawer-paper');
    await expect(drawer).toBeVisible();
    await drawer.getByRole('button', { name: /nova alocação/i }).click();

    const assignDialog = page.getByRole('dialog').filter({ hasText: 'Alocar Motorista' });
    await assignDialog.locator('div[role="combobox"]').click();
    await page.getByRole('listbox').getByText('Marcos Aurélio Souza', { exact: false }).click();
    await assignDialog.getByRole('button', { name: /alocar/i }).click();

    await expect(assignDialog).toHaveCount(0, { timeout: 10_000 });
    await page.keyboard.press('Escape');
    await expect(drawer).toHaveCount(0, { timeout: 5000 }).catch(async () => {
      await page.locator('.MuiDrawer-paper').locator('button').first().click({ force: true });
      await expect(drawer).toHaveCount(0, { timeout: 5000 });
    });
  });

  test('3. Setup Fleet Maintenance & Fueling', async ({ authedPage: page }) => {
    await page.goto('/app/fleet');
    await page.waitForURL('**/app/fleet');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    // 3.1 Maintenance
    await page.getByRole('tab', { name: /manutenção/i }).click();
    await page.locator('div[role="combobox"]').click();
    await page.getByRole('listbox').getByRole('option').nth(1).click();
    await page.getByRole('button', { name: /^agendar$/i }).click();

    const maintenanceDialog = page.getByRole('dialog').filter({ hasText: 'Agendar Manutenção' });
    await maintenanceDialog.getByLabel(/descrição/i).fill('Revisão Preventiva de 50.000 km e Troca de Filtros');
    await maintenanceDialog.getByLabel(/data agendada/i).fill('2026-06-20');
    await maintenanceDialog.getByRole('button', { name: /^agendar$/i }).click();
    await expect(maintenanceDialog).toHaveCount(0, { timeout: 10_000 });
    await expect(page.getByRole('alert').filter({ hasText: /agendada com sucesso/i })).toBeVisible();
    await page.keyboard.press('Escape');

    // 3.2 Fueling
    await page.getByRole('tab', { name: /abastecimento/i }).click();
    await page.locator('div[role="combobox"]').click();
    await page.getByRole('listbox').getByRole('option').nth(1).click();
    await page.getByRole('button', { name: /^registrar$/i }).click();

    const fuelingDialog = page.getByRole('dialog').filter({ hasText: 'Registrar Abastecimento' });
    await fuelingDialog.getByLabel(/data/i).fill('2026-06-15');
    await fuelingDialog.getByLabel(/litros abastecidos/i).fill('320.00');
    await fuelingDialog.getByLabel(/preço por litro/i).fill('6.15');
    await fuelingDialog.getByRole('button', { name: /^registrar$/i }).click();
    await expect(fuelingDialog).toHaveCount(0, { timeout: 10_000 });
    await expect(page.getByRole('alert').filter({ hasText: /registrado com sucesso/i })).toBeVisible();
  });

  test('4. Setup Production Work Centers', async ({ authedPage: page }) => {
    const wcs = [
      { code: 'WC-CNC-01', name: 'Corte a Laser CNC' },
      { code: 'WC-SOL-02', name: 'Estação de Solda Robotizada' },
      { code: 'WC-MON-03', name: 'Linha de Montagem de Trilhos' },
    ];

    await page.goto('/app/production/work-centers');
    await page.waitForURL('**/app/production/work-centers');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    for (const wc of wcs) {
      await page.getByRole('button', { name: /novo centro/i }).click();
      const wcDialog = page.getByRole('dialog').filter({ hasText: 'Novo Centro de Trabalho' });
      await wcDialog.getByLabel('Código').fill(wc.code);
      await wcDialog.getByLabel('Nome').fill(wc.name);
      await wcDialog.getByRole('button', { name: /^criar$/i }).click();
      
      await expect(wcDialog).toHaveCount(0, { timeout: 10_000 });
      await expect(page.getByRole('alert').filter({ hasText: /criado com sucesso/i })).toBeVisible();
      await page.keyboard.press('Escape');
    }
  });

  test('5. Setup Logistics Carriers', async ({ authedPage: page }) => {
    const carriers = [
      { name: 'Rodoviário Express Rail Ltda', kgRate: '0.12', cbmRate: '35', webhook: 'https://webhook.site/dummy-carrier' },
      { name: 'Rápido TransLog S/A', kgRate: '0.08', cbmRate: '42', webhook: '' },
    ];

    await page.goto('/app/logistics/carriers');
    await page.waitForURL('**/app/logistics/carriers');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    for (const c of carriers) {
      await page.getByRole('button', { name: /nova transportadora/i }).click();
      const carrierDialog = page.getByRole('dialog').filter({ hasText: 'Nova Transportadora' });
      await carrierDialog.getByLabel(/nome/i).fill(c.name);
      await carrierDialog.getByLabel(/cnpj/i).fill(generateCNPJ());
      await carrierDialog.getByLabel(/taxa por kg/i).fill(c.kgRate);
      await carrierDialog.getByLabel(/taxa por m³/i).fill(c.cbmRate);
      if (c.webhook) {
        await carrierDialog.getByLabel(/url de webhook/i).fill(c.webhook);
      }
      await carrierDialog.getByRole('button', { name: /cadastrar/i }).click();
      
      await expect(carrierDialog).toHaveCount(0, { timeout: 10_000 });
      await expect(page.getByRole('alert').filter({ hasText: /cadastrada com sucesso/i })).toBeVisible();
      await page.keyboard.press('Escape');
    }
  });

  test('6. Supply Chain - Import and Map NF-e', async ({ authedPage: page }) => {
    // 6.1 Upload XML
    await page.goto('/app/receipts');
    await page.waitForURL('**/app/receipts');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    await page.getByRole('button', { name: /importar xml/i }).click();
    let dialog = page.getByRole('dialog');
    await expect(page.getByText('PASSO 1 DE 2: UPLOAD')).toBeVisible();
    await page.locator('input[type="file"]').setInputFiles(nfePath);
    await expect(page.getByText('PASSO 2 DE 2: PRÉ-VISUALIZAÇÃO DA NOTA')).toBeVisible();
    await page.getByRole('button', { name: /confirmar e importar/i }).click();
    await expect(page.getByText(/xml importado com sucesso/i)).toBeVisible();
    await page.keyboard.press('Escape');

    // 6.2 Association Workbench
    await page.goto('/app/supply-chain/association');
    await page.waitForURL('**/app/supply-chain/association');
    await page.waitForLoadState('networkidle');

    // Map Item 1: ACO-GALV-2MM
    await page.getByText('Chapa de Aço Galvanizado 2mm', { exact: false }).first().click();
    await page.getByRole('button', { name: /criar novo/i }).click();
    await page.getByLabel('SKU Interno').fill('MAT-ACO-GALV-2MM');
    await page.getByLabel('Nome Oficial').fill('Chapa de Aço Galvanizado 2mm');
    await page.getByLabel('Categoria').click();
    await page.getByRole('listbox').getByText('Matéria-Prima', { exact: true }).click();
    await page.getByLabel('NCM').fill('72085100');
    await page.getByRole('button', { name: /criar e vincular/i }).click();
    await expect(page.getByText('MAT-ACO-GALV-2MM', { exact: true }).first()).toBeVisible();

    // Map Item 2: ACO-PERFIL-I
    await page.getByText('Perfil I de Aço Carbono 6 pol', { exact: false }).first().click();
    await page.getByRole('button', { name: /criar novo/i }).click();
    await page.getByLabel('SKU Interno').fill('MAT-ACO-PERFIL-I');
    await page.getByLabel('Nome Oficial').fill('Perfil I de Aço Carbono 6 polegadas');
    await page.getByLabel('Categoria').click();
    await page.getByRole('listbox').getByText('Matéria-Prima', { exact: true }).click();
    await page.getByLabel('NCM').fill('72163300');
    await page.getByRole('button', { name: /criar e vincular/i }).click();
    await expect(page.getByText('MAT-ACO-PERFIL-I', { exact: true }).first()).toBeVisible();

    // Map Item 3: PRO-TR-100
    await page.getByText('Trilho Ferroviário TR-100', { exact: false }).first().click();
    await page.getByRole('button', { name: /criar novo/i }).click();
    await page.getByLabel('SKU Interno').fill('PRO-TR-100');
    await page.getByLabel('Nome Oficial').fill('Trilho Ferroviário Padrão TR-100');
    await page.getByLabel('Categoria').click();
    await page.getByRole('listbox').getByText('Produto Acabado', { exact: true }).click();
    await page.getByLabel('NCM').fill('73021010');
    await page.getByRole('button', { name: /criar e vincular/i }).click();
    await expect(page.getByText('PRO-TR-100', { exact: true }).first()).toBeVisible();

    // Release Workbench mapping
    const releaseBtn = page.getByRole('button', { name: /liberar/i }).first();
    await expect(releaseBtn).toBeEnabled();
    await releaseBtn.click();
    await expect(page.getByText('Bancada Vazia')).toBeVisible({ timeout: 15_000 });

    // 6.3 Conference
    await page.goto('/app/receipts');
    await page.waitForURL('**/app/receipts');
    
    const card = page.locator('.MuiCard-root', { hasText: 'Aços Especiais Rio S/A' }).first();
    await card.getByRole('button', { name: /iniciar/i }).click();

    const qtyInputs = page.locator('input[type="number"]');
    await expect(qtyInputs).toHaveCount(3);
    await qtyInputs.nth(0).fill('3000');
    await qtyInputs.nth(1).fill('200');
    await qtyInputs.nth(2).fill('15');

    await page.getByRole('button', { name: /finalizar conferência/i }).click();
    await expect(page.getByRole('heading', { name: 'Recebimentos' })).toBeVisible({ timeout: 10_000 });
  });

  test('7. Production - Create & Activate BOM', async ({ authedPage: page }) => {
    await page.goto('/app/production/boms');
    await page.waitForURL('**/app/production/boms');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    await page.getByRole('button', { name: /nova bom/i }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByRole('combobox').click();
    await page.getByRole('listbox').getByText('PRO-TR-100', { exact: false }).click();
    await dialog.getByLabel(/lote padrão/i).fill('1.0');
    await dialog.getByRole('button', { name: /criar bom/i }).click();

    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });

    // BOM details card is expanded automatically. Let's add items
    // Item 1: MAT-ACO-GALV-2MM (15.5 kg, loss factor 0.05)
    await page.getByLabel(/componente/i).click();
    await page.getByRole('listbox').getByText('MAT-ACO-GALV-2MM', { exact: false }).click();
    await page.getByLabel(/quantidade/i).fill('15.5');
    await page.getByLabel(/fator de perda/i).fill('0.05');
    await page.getByRole('button', { name: /adicionar item/i }).click();
    await expect(page.locator('tr', { hasText: 'MAT-ACO-GALV-2MM' })).toBeVisible();

    // Item 2: MAT-ACO-PERFIL-I (2.0 m, loss factor 0.02)
    await page.getByLabel(/componente/i).click();
    await page.getByRole('listbox').getByText('MAT-ACO-PERFIL-I', { exact: false }).click();
    await page.getByLabel(/quantidade/i).fill('2.0');
    await page.getByLabel(/fator de perda/i).fill('0.02');
    await page.getByRole('button', { name: /adicionar item/i }).click();
    await expect(page.locator('tr', { hasText: 'MAT-ACO-PERFIL-I' })).toBeVisible();

    // Activate BOM
    await page.getByRole('button', { name: /ativar/i }).first().click();
    await expect(page.getByRole('button', { name: /ativar/i })).toHaveCount(0, { timeout: 10_000 });
  });

  test('8. Production - Complete Flow of a Production Order', async ({ authedPage: page }) => {
    await page.goto('/app/production/orders');
    await page.waitForURL('**/app/production/orders');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    // 8.1 Create Production Order
    await page.getByRole('button', { name: /nova ordem/i }).click();
    let dialog = page.getByRole('dialog');
    
    // Select BOM
    await dialog.locator('div[role="combobox"]').first().click();
    await page.getByRole('listbox').getByText('PRO-TR-100', { exact: false }).click();

    // Select Work Center
    await dialog.locator('div[role="combobox"]').last().click();
    await page.getByRole('listbox').getByText('Linha de Montagem de Trilhos', { exact: true }).click();

    await dialog.getByLabel(/qtd planejada/i).fill('5');
    await dialog.getByRole('button', { name: /criar ordem/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });

    // Find the newly created Draft order and click it
    const row = page.getByRole('row').filter({ hasText: 'PRO-TR-100' }).filter({ hasText: 'Rascunho' }).first();
    await row.click();

    // 8.2 Release Order
    dialog = page.getByRole('dialog');
    await dialog.getByRole('button', { name: /liberar ordem/i }).click();
    await expect(dialog.getByRole('button', { name: /iniciar execução/i })).toBeVisible({ timeout: 10_000 });

    // 8.3 Start Execution
    await dialog.getByRole('button', { name: /iniciar execução/i }).click();
    await expect(dialog.getByRole('tab', { name: /concluir/i })).toBeVisible({ timeout: 10_000 });

    // 8.4 Log Scrap
    await dialog.getByRole('tab', { name: /ajustes/i }).click();
    const scrapSection = dialog.locator('div', { hasText: 'Registrar Scrap' }).locator('xpath=..');
    
    // Click quick fill chip in scrap section
    await scrapSection.locator('.MuiChip-root', { hasText: 'MAT-ACO-GALV-2MM' }).click();
    await scrapSection.getByLabel(/quantidade/i).fill('1.5');
    await scrapSection.getByLabel(/motivo do scrap/i).fill('Rebarbas residuais no corte angular.');
    await scrapSection.getByRole('button', { name: /registrar scrap/i }).click();
    await expect(scrapSection.getByText(/registrado/i)).toBeVisible();

    // 8.5 Quality Inspection & Complete
    await dialog.getByRole('tab', { name: /concluir/i }).click();
    await dialog.getByRole('button', { name: /^aprovado$/i }).click();
    await dialog.getByLabel(/observações/i).fill('Lote verificado dimensionalmente. Resistência e acabamento 100% conformes.');
    await dialog.getByRole('button', { name: /aprovar e concluir ordem/i }).click();

    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 15_000 });
  });

  test('9. Logistics - Create Shipment Order', async ({ authedPage: page }) => {
    await page.goto('/app/logistics/shipment-orders');
    await page.waitForURL('**/app/logistics/shipment-orders');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    await page.getByRole('button', { name: /nova ordem/i }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel(/destinatário/i).fill('Consórcio Ferroviário do Sul');
    await dialog.getByLabel(/endereço/i).fill('Rodovia BR-116, Km 410 - Porto Alegre/RS');
    await dialog.getByLabel(/observações/i).fill('Entrega prioritária para expansão de malha.');
    await dialog.getByRole('button', { name: /criar/i }).click();

    // Add items to Shipment Order
    await dialog.getByLabel(/material/i).fill('PRO-TR-100');
    await page.getByRole('listbox').getByText('PRO-TR-100', { exact: false }).click();
    await dialog.getByLabel(/quantidade/i).fill('8');
    await dialog.getByRole('button', { name: /adicionar/i }).click();
    await expect(dialog.locator('.MuiChip-root', { hasText: 'PRO-TR-100' })).toBeVisible();

    // Conclude order
    await dialog.getByRole('button', { name: /concluir/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(page.getByRole('alert').filter({ hasText: /criada com sucesso/i })).toBeVisible();
    await page.keyboard.press('Escape');

    // Transition Shipment Order Draft -> Picking -> ReadyToShip
    // 9.1 Draft -> Picking
    const row = page.getByRole('row').filter({ hasText: 'Consórcio Ferroviário' }).first();
    await row.getByRole('button', { name: /separar/i }).click();
    let confirmDialog = page.getByRole('dialog');
    await confirmDialog.getByRole('button', { name: /confirmar/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });

    // 9.2 Picking -> ReadyToShip
    await row.getByRole('button', { name: /embalar/i }).or(row.getByRole('button', { name: /concluir/i })).click();
    confirmDialog = page.getByRole('dialog');
    await confirmDialog.getByRole('button', { name: /confirmar/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
  });

  test('10. Logistics - Create and Execute Dispatch', async ({ authedPage: page }) => {
    await page.goto('/app/logistics/dispatches');
    await page.waitForURL('**/app/logistics/dispatches');
    await expect(page.getByRole('progressbar')).toHaveCount(0, { timeout: 15_000 });

    // 10.1 Create Dispatch
    await page.getByRole('button', { name: /novo despacho/i }).click();
    let dialog = page.getByRole('dialog');

    // Select Shipment Order
    await dialog.locator('div[role="combobox"]').first().click();
    await page.getByRole('listbox').getByText('Consórcio Ferroviário', { exact: false }).click();

    // Select Carrier
    await dialog.locator('div[role="combobox"]').nth(1).click();
    await page.getByRole('listbox').getByText('Rodoviário Express Rail Ltda', { exact: false }).click();

    // Select Vehicle
    await dialog.locator('div[role="combobox"]').last().click();
    await page.getByRole('listbox').getByText('RFX-1020', { exact: false }).click();

    // Wait for driver lookup
    await expect(dialog.getByText('Marcos Aurélio Souza')).toBeVisible({ timeout: 10_000 });
    
    await dialog.getByRole('button', { name: /criar despacho/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });

    // 10.2 Conference Dispatch
    const row = page.getByRole('row').filter({ hasText: 'Consórcio Ferroviário' }).first();
    await row.getByRole('button', { name: /conferir/i }).click();

    dialog = page.getByRole('dialog');
    await dialog.getByRole('checkbox').click(); // Check all items
    await dialog.getByRole('button', { name: /confirmar conferência/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });

    // 10.3 Dispatch (Pending -> InTransit)
    await row.getByRole('button', { name: /despachar/i }).click();
    let confirmDialog = page.getByRole('dialog');
    await confirmDialog.getByRole('button', { name: /confirmar/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });

    // 10.4 Deliver (InTransit -> Delivered)
    await expect(row.getByRole('button', { name: /entregue/i })).toBeVisible({ timeout: 10_000 });
    await row.getByRole('button', { name: /entregue/i }).click();
    confirmDialog = page.getByRole('dialog');
    await confirmDialog.getByRole('button', { name: /confirmar/i }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
  });
});
