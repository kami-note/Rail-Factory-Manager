import { test, expect } from './fixtures';

test.describe('Work Centers', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/production/work-centers');
    await authedPage.waitForURL('**/app/production/work-centers');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
  });

  test('exibe o cabeçalho e o botão de criação', async ({ authedPage }) => {
    // ModuleHeader usa Typography caption (não <h>), verificamos via texto + botão
    await expect(authedPage.getByText('CENTROS DE TRABALHO', { exact: true })).not.toHaveCount(0);
    await expect(authedPage.getByRole('button', { name: /novo centro/i })).toBeVisible();
  });

  test('botão "Novo Centro" abre a modal', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo centro/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText('Novo Centro de Trabalho')).toBeVisible();
    await expect(dialog.getByLabel('Código')).toBeVisible();
    await expect(dialog.getByLabel('Nome')).toBeVisible();
  });

  test('botão Criar está desabilitado com campos vazios', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo centro/i }).click();
    await expect(authedPage.getByRole('dialog').getByRole('button', { name: /^criar$/i })).toBeDisabled();
  });

  test('fecha a modal ao clicar em Cancelar', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo centro/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });

  test('cria novo centro de trabalho com sucesso', async ({ authedPage }) => {
    const code = `TEST-${Date.now()}`;
    const name = `Centro de Teste ${Date.now()}`;

    await authedPage.getByRole('button', { name: /novo centro/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await dialog.getByLabel('Código').fill(code);
    await dialog.getByLabel('Nome').fill(name);
    await dialog.getByRole('button', { name: /^criar$/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /criado com sucesso/i })).toBeVisible();
    await expect(authedPage.getByRole('cell', { name: code })).toBeVisible();
  });

  test('modal de confirmação aparece ao clicar em desativar', async ({ authedPage }) => {
    const activeRow = authedPage.getByRole('row').filter({
      has: authedPage.getByRole('cell').filter({ hasText: /^ativo$/i }),
    }).first();

    if (await activeRow.count() === 0) { test.skip(); return; }

    await activeRow.getByRole('button').click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/desativar centro de trabalho/i)).toBeVisible();
    await expect(dialog.getByRole('button', { name: /desativar/i })).toBeVisible();
    await expect(dialog.getByRole('button', { name: /cancelar/i })).toBeVisible();
  });

  test('cancela a confirmação de desativação', async ({ authedPage }) => {
    const activeRow = authedPage.getByRole('row').filter({
      has: authedPage.getByRole('cell').filter({ hasText: /^ativo$/i }),
    }).first();

    if (await activeRow.count() === 0) { test.skip(); return; }

    await activeRow.getByRole('button').click();
    await authedPage.getByRole('dialog').getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });
});
