import { test, expect } from './fixtures';

test.describe('Ordens de Expedição', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/logistics/shipment-orders');
    await authedPage.waitForURL('**/app/logistics/shipment-orders');
    await authedPage.waitForLoadState('networkidle');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
  });

  test('exibe o botão "Nova Ordem"', async ({ authedPage }) => {
    await expect(authedPage.getByRole('button', { name: /nova ordem/i })).toBeVisible();
  });

  test('modal de criação abre e fecha', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova ordem/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/nova ordem/i)).toBeVisible();

    await dialog.getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });

  test('cria ordem e aparece na tabela com número EXP-', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova ordem/i }).click();
    const dialog = authedPage.getByRole('dialog');

    await dialog.getByLabel(/observações/i).fill('PW teste P8');
    await dialog.getByRole('button', { name: /criar/i }).click();

    // Adicionar item
    await dialog.getByLabel(/material/i).fill('MAT-ACO-1020');
    await dialog.getByLabel(/quantidade/i).fill('10');
    await dialog.getByRole('button', { name: /adicionar/i }).click();
    await expect(dialog.locator('.MuiChip-root', { hasText: 'MAT-ACO-1020' })).toBeVisible();

    await dialog.getByRole('button', { name: /concluir/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /criada com sucesso/i })).toBeVisible();
    await expect(authedPage.locator('td', { hasText: /^EXP-/ }).first()).toBeVisible();
  });

  test('nova ordem aparece com status Rascunho', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /nova ordem/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await dialog.getByRole('button', { name: /criar/i }).click();

    // Adicionar item
    await dialog.getByLabel(/material/i).fill('MAT-ACO-1020');
    await dialog.getByLabel(/quantidade/i).fill('10');
    await dialog.getByRole('button', { name: /adicionar/i }).click();
    await expect(dialog.locator('.MuiChip-root', { hasText: 'MAT-ACO-1020' })).toBeVisible();

    await dialog.getByRole('button', { name: /concluir/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    const draftRow = authedPage.getByRole('row').filter({
      has: authedPage.locator('.MuiChip-root').filter({ hasText: /rascunho/i }),
    }).first();
    await expect(draftRow).toBeVisible();
  });

  test('botão Separar disponível em ordem Rascunho', async ({ authedPage }) => {
    const draftRow = authedPage.getByRole('row').filter({
      has: authedPage.locator('.MuiChip-root').filter({ hasText: /rascunho/i }),
    }).first();

    if (await draftRow.count() === 0) { test.skip(); return; }
    await expect(draftRow.getByRole('button', { name: /separar/i })).toBeVisible();
  });

  test('transição Draft → Picking com ConfirmDialog', async ({ authedPage }) => {
    // Criar uma ordem nova para teste
    await authedPage.getByRole('button', { name: /nova ordem/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await dialog.getByRole('button', { name: /criar/i }).click();

    // Adicionar item
    await dialog.getByLabel(/material/i).fill('MAT-ACO-1020');
    await dialog.getByLabel(/quantidade/i).fill('10');
    await dialog.getByRole('button', { name: /adicionar/i }).click();
    await expect(dialog.locator('.MuiChip-root', { hasText: 'MAT-ACO-1020' })).toBeVisible();

    await dialog.getByRole('button', { name: /concluir/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });

    // Pegar a primeira linha Draft
    const draftRow = authedPage.getByRole('row').filter({
      has: authedPage.locator('.MuiChip-root').filter({ hasText: /rascunho/i }),
    }).first();
    await draftRow.getByRole('button', { name: /separar/i }).click();

    // ConfirmDialog
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole('heading', { name: /confirmar/i })).toBeVisible();
    await dialog.getByRole('button', { name: /confirmar/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /realizada/i })).toBeVisible();
  });

  test('cancelar ordem via ConfirmDialog', async ({ authedPage }) => {
    const draftRow = authedPage.getByRole('row').filter({
      has: authedPage.locator('.MuiChip-root').filter({ hasText: /rascunho/i }),
    }).first();

    if (await draftRow.count() === 0) { test.skip(); return; }

    // Botão cancelar (XCircle) é o IconButton
    await draftRow.locator('button[data-testid]').or(draftRow.getByRole('button').last()).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });
});
