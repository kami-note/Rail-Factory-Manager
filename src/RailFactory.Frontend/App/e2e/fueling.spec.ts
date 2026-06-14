import { test, expect } from './fixtures';

test.describe('Controle de Abastecimento', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/fleet');
    await authedPage.waitForURL('**/app/fleet');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
    await authedPage.getByRole('tab', { name: /abastecimento/i }).click();
  });

  test('exibe seletor de veículo e botão de registro', async ({ authedPage }) => {
    await expect(authedPage.locator('div[role="combobox"]')).toBeVisible();
    await expect(authedPage.getByRole('button', { name: /^registrar$/i })).toBeVisible();
  });

  test('modal de abastecimento abre e fecha', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    await authedPage.getByRole('listbox').getByRole('option').nth(1).click();
    await authedPage.getByRole('button', { name: /^registrar$/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/registrar abastecimento/i)).toBeVisible();
    await expect(dialog.getByLabel(/litros abastecidos/i)).toBeVisible();
    await expect(dialog.getByLabel(/preço por litro/i)).toBeVisible();

    await dialog.getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });

  test('botão Registrar desabilitado com campos vazios', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    await authedPage.getByRole('listbox').getByRole('option').nth(1).click();
    await authedPage.getByRole('button', { name: /^registrar$/i }).click();

    await expect(authedPage.getByRole('dialog').getByRole('button', { name: /^registrar$/i })).toBeDisabled();
  });

  test('registra abastecimento e aparece na tabela', async ({ authedPage }) => {
    await authedPage.locator('div[role="combobox"]').click();
    await authedPage.getByRole('listbox').getByRole('option').nth(1).click();
    await authedPage.getByRole('button', { name: /^registrar$/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await dialog.getByLabel(/data/i).fill('2026-05-28');
    await dialog.getByLabel(/litros abastecidos/i).fill('80.5');
    await dialog.getByLabel(/preço por litro/i).fill('6.50');
    await dialog.getByRole('button', { name: /^registrar$/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /registrado com sucesso/i })).toBeVisible();
    await expect(authedPage.locator('td', { hasText: '80.500' }).first()).toBeVisible();
  });
});
