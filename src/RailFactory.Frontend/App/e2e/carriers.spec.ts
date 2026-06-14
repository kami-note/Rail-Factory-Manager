import { test, expect } from './fixtures';

function generateCNPJ(): string {
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
  return `${clean.slice(0, 2)}.${clean.slice(2, 5)}.${clean.slice(5, 8)}/${clean.slice(8, 12)}-${clean.slice(12, 14)}`;
}

test.describe('Transportadoras', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/logistics/carriers');
    await authedPage.waitForURL('**/app/logistics/carriers');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
  });

  test('exibe o botão "Nova Transportadora"', async ({ authedPage }) => {
    await expect(authedPage.getByRole('button', { name: /nova transportadora/i })).toBeVisible();
  });

  test('modal de criação abre com todos os campos', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova transportadora/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/nova transportadora/i)).toBeVisible();
    await expect(dialog.getByLabel(/nome/i)).toBeVisible();
    await expect(dialog.getByLabel(/cnpj/i)).toBeVisible();
    await expect(dialog.getByLabel(/taxa por kg/i)).toBeVisible();
    await expect(dialog.getByLabel(/taxa por m³/i)).toBeVisible();
  });

  test('botão Cadastrar desabilitado com campos vazios', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova transportadora/i }).click();
    await expect(authedPage.getByRole('dialog').getByRole('button', { name: /cadastrar/i })).toBeDisabled();
  });

  test('fecha modal ao clicar em Cancelar', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova transportadora/i }).click();
    await authedPage.getByRole('dialog').getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });

  test('cria transportadora e aparece na tabela', async ({ authedPage }) => {
    const unique = Date.now().toString().slice(-6);
    const docNumber = generateCNPJ();

    await authedPage.getByRole('button', { name: /nova transportadora/i }).click();
    const dialog = authedPage.getByRole('dialog');

    await dialog.getByLabel(/nome/i).fill(`Transportadora PW-${unique}`);
    await dialog.getByLabel(/cnpj/i).fill(docNumber);
    await dialog.getByLabel(/taxa por kg/i).fill('0.085');
    await dialog.getByLabel(/taxa por m³/i).fill('45');
    await dialog.getByRole('button', { name: /cadastrar/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /cadastrada com sucesso/i })).toBeVisible();
    await expect(authedPage.locator('td', { hasText: `Transportadora PW-${unique}` })).toBeVisible();
  });

  test('ConfirmDialog aparece ao tentar inativar transportadora ativa', async ({ authedPage }) => {
    const activeRow = authedPage.getByRole('row').filter({
      has: authedPage.locator('.MuiChip-root').filter({ hasText: /ativ/i }),
    }).first();

    if (await activeRow.count() === 0) { test.skip(); return; }

    await activeRow.getByRole('button').click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole('heading', { name: /inativar transportadora/i })).toBeVisible();
  });

  test('cancela inativação via ConfirmDialog', async ({ authedPage }) => {
    const activeRow = authedPage.getByRole('row').filter({
      has: authedPage.locator('.MuiChip-root').filter({ hasText: /ativ/i }),
    }).first();

    if (await activeRow.count() === 0) { test.skip(); return; }

    await activeRow.getByRole('button').click();
    await authedPage.getByRole('dialog').getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });
});
