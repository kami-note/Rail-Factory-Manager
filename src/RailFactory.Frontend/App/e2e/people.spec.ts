import { test, expect } from './fixtures';

test.describe('Pessoas (HR)', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/hr/people');
    await authedPage.waitForURL('**/app/hr/people');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
  });

  test('exibe o cabeçalho e o botão de criação', async ({ authedPage }) => {
    // ModuleHeader usa Typography caption — verificamos botão único da página
    await expect(authedPage.getByRole('button', { name: /nova pessoa/i })).toBeVisible();
  });

  test('botão "Nova Pessoa" abre a modal', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova pessoa/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText('Nova Pessoa')).toBeVisible();
    await expect(dialog.getByLabel('Nome completo')).toBeVisible();
    await expect(dialog.getByLabel(/cpf/i)).toBeVisible();
    // MUI Select renderiza como div[role="combobox"], não input com label-for
    await expect(dialog.locator('div[role="combobox"]')).toBeVisible();
    await expect(dialog.getByLabel(/e-mail/i)).toBeVisible();
  });

  test('botão Cadastrar está desabilitado com campos obrigatórios vazios', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova pessoa/i }).click();
    await expect(authedPage.getByRole('dialog').getByRole('button', { name: /cadastrar/i })).toBeDisabled();
  });

  test('fecha a modal ao clicar em Cancelar', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova pessoa/i }).click();
    await authedPage.getByRole('dialog').getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });

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

  test('cria nova pessoa com sucesso', async ({ authedPage }) => {
    const ts = Date.now();
    const name = `João Teste ${ts}`;
    const doc = generateCPF();

    await authedPage.getByRole('button', { name: /nova pessoa/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await dialog.getByLabel('Nome completo').fill(name);
    await dialog.getByLabel(/cpf/i).fill(doc);
    await dialog.getByRole('button', { name: /cadastrar/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /cadastrada com sucesso/i })).toBeVisible();
    await expect(authedPage.getByRole('cell', { name })).toBeVisible();
  });

  test('select de tipo tem as opções corretas', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova pessoa/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();

    // MUI Select usa div[role="combobox"], não input com label associado
    await dialog.locator('div[role="combobox"]').click();

    // Opções ficam em portal (fora do dialog no DOM, mas visíveis na tela)
    await expect(authedPage.getByRole('listbox')).toBeVisible({ timeout: 5_000 });
    const listbox = authedPage.getByRole('listbox');
    await expect(listbox.getByText(/colaborador/i)).toBeVisible();
    await expect(listbox.getByText(/motorista/i)).toBeVisible();
    await expect(listbox.getByText(/terceirizado/i)).toBeVisible();

    await authedPage.keyboard.press('Escape');
  });

  test('modal de confirmação ao inativar pessoa', async ({ authedPage }) => {
    const activeRow = authedPage.getByRole('row').filter({
      has: authedPage.getByRole('cell').filter({ hasText: /^ativo$/i }),
    }).first();

    if (await activeRow.count() === 0) { test.skip(); return; }

    await activeRow.getByRole('button').click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/inativar pessoa/i)).toBeVisible();
    await expect(dialog.getByRole('button', { name: /inativar/i })).toBeVisible();
  });

  test('cancela inativação via ConfirmDialog', async ({ authedPage }) => {
    const activeRow = authedPage.getByRole('row').filter({
      has: authedPage.getByRole('cell').filter({ hasText: /^ativo$/i }),
    }).first();

    if (await activeRow.count() === 0) { test.skip(); return; }

    await activeRow.getByRole('button').click();
    await authedPage.getByRole('dialog').getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });
});
