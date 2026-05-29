import { test, expect } from './fixtures';

test.describe('Manutenção de Veículos', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/fleet/maintenance');
    await authedPage.waitForURL('**/app/fleet/maintenance');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
  });

  test('exibe seletor de veículo sem botão de agendar', async ({ authedPage }) => {
    await expect(authedPage.getByRole('combobox')).toBeVisible();
    await expect(authedPage.getByRole('button', { name: /agendar manutenção/i })).toHaveCount(0);
  });

  test('botão agendar aparece após selecionar veículo', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    const listbox = authedPage.getByRole('listbox');
    await expect(listbox).toBeVisible({ timeout: 5_000 });
    await listbox.getByRole('option').first().click();

    await expect(authedPage.getByRole('button', { name: /agendar manutenção/i })).toBeVisible();
  });

  test('modal de agendamento abre e fecha', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    await authedPage.getByRole('listbox').getByRole('option').first().click();
    await authedPage.getByRole('button', { name: /agendar manutenção/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/agendar manutenção/i)).toBeVisible();
    await expect(dialog.getByLabel(/descrição/i)).toBeVisible();
    await expect(dialog.getByLabel(/data agendada/i)).toBeVisible();

    await dialog.getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });

  test('botão Agendar desabilitado com campos vazios', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    await authedPage.getByRole('listbox').getByRole('option').first().click();
    await authedPage.getByRole('button', { name: /agendar manutenção/i }).click();

    await expect(authedPage.getByRole('dialog').getByRole('button', { name: /^agendar$/i })).toBeDisabled();
  });

  test('agenda manutenção preventiva e aparece na tabela', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    await authedPage.getByRole('listbox').getByRole('option').first().click();
    await authedPage.getByRole('button', { name: /agendar manutenção/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await dialog.getByLabel(/descrição/i).fill('Revisão PW teste');
    await dialog.getByLabel(/data agendada/i).fill('2026-07-01');
    await dialog.getByRole('button', { name: /^agendar$/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /agendada com sucesso/i })).toBeVisible();
    await expect(authedPage.locator('td', { hasText: 'Revisão PW teste' }).first()).toBeVisible();
  });

  test('botões de concluir e cancelar visíveis em linha Agendada', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    await authedPage.getByRole('listbox').getByRole('option').first().click();

    const scheduledRow = authedPage.getByRole('row').filter({
      has: authedPage.locator('[data-testid="chip"]').or(authedPage.locator('.MuiChip-root')).filter({ hasText: /agendada/i }),
    }).first();

    if (await scheduledRow.count() === 0) { test.skip(); return; }
    await expect(scheduledRow.getByRole('button')).toHaveCount(2);
  });
});
