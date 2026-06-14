import { test, expect } from './fixtures';

test.describe('Frota de Veículos', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/fleet');
    await authedPage.waitForURL('**/app/fleet');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
  });

  test('exibe o cabeçalho e o botão de criação', async ({ authedPage }) => {
    await expect(authedPage.getByRole('button', { name: /novo veículo/i })).toBeVisible();
  });

  test('botão "Novo Veículo" abre a modal', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo veículo/i }).click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText('Novo Veículo')).toBeVisible();
    await expect(dialog.getByLabel('Placa')).toBeVisible();
    await expect(dialog.getByLabel('Chassi')).toBeVisible();
    await expect(dialog.getByLabel('RENAVAM')).toBeVisible();
    await expect(dialog.getByLabel(/carga máx/i)).toBeVisible();
    await expect(dialog.getByLabel(/volume máx/i)).toBeVisible();
    await expect(dialog.getByLabel(/vencimento crlv/i)).toBeVisible();
  });

  test('botão Cadastrar está desabilitado com campos vazios', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo veículo/i }).click();
    await expect(authedPage.getByRole('dialog').getByRole('button', { name: /cadastrar/i })).toBeDisabled();
  });

  test('fecha a modal ao clicar em Cancelar', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo veículo/i }).click();
    await authedPage.getByRole('dialog').getByRole('button', { name: /cancelar/i }).click();
    await expect(authedPage.getByRole('dialog')).toHaveCount(0);
  });

  test('placa é convertida para maiúsculo automaticamente', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo veículo/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await dialog.getByLabel('Placa').fill('abc-1234');
    await expect(dialog.getByLabel('Placa')).toHaveValue('ABC-1234');
  });

  test('chassi é convertido para maiúsculo automaticamente', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo veículo/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await dialog.getByLabel('Chassi').fill('9bwzzz377vt004251');
    await expect(dialog.getByLabel('Chassi')).toHaveValue('9BWZZZ377VT004251');
  });

  test('cria novo veículo com sucesso', async ({ authedPage }) => {
    const letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const digits = '0123456789';
    const chassisChars = 'ABCDEFGHJKLMNPRSTUVWXYZ0123456789'; // Excludes I, O, Q
    
    let plate = '';
    for (let i = 0; i < 3; i++) {
      plate += letters[Math.floor(Math.random() * letters.length)];
    }
    for (let i = 0; i < 4; i++) {
      plate += digits[Math.floor(Math.random() * digits.length)];
    }

    let chassis = '';
    for (let i = 0; i < 17; i++) {
      chassis += chassisChars[Math.floor(Math.random() * chassisChars.length)];
    }

    let renavam = '';
    for (let i = 0; i < 11; i++) {
      renavam += digits[Math.floor(Math.random() * digits.length)];
    }

    await authedPage.getByRole('button', { name: /novo veículo/i }).click();
    const dialog = authedPage.getByRole('dialog');

    await dialog.getByLabel('Placa').fill(plate);
    await dialog.getByLabel('Chassi').fill(chassis);
    await dialog.getByLabel('RENAVAM').fill(renavam);
    await dialog.getByLabel(/carga máx/i).fill('5000');
    await dialog.getByLabel(/volume máx/i).fill('20');

    // input[type="date"] — fill() define o valor diretamente no formato ISO YYYY-MM-DD
    await dialog.getByLabel(/vencimento crlv/i).fill('2027-12-31');

    await dialog.getByRole('button', { name: /cadastrar/i }).click();

    await expect(authedPage.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
    await expect(authedPage.getByRole('alert').filter({ hasText: /cadastrado/i })).toBeVisible();
    // A placa aparece na tabela formatada (com hífen)
    const formattedPlate = `${plate.slice(0, 3)}-${plate.slice(3)}`;
    await expect(authedPage.locator('td', { hasText: formattedPlate })).toBeVisible();
  });

  test('select de tipo tem as opções corretas', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /novo veículo/i }).click();
    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();

    // MUI Select usa div[role="combobox"]
    await dialog.locator('div[role="combobox"]').click();

    await expect(authedPage.getByRole('listbox')).toBeVisible({ timeout: 5_000 });
    const listbox = authedPage.getByRole('listbox');
    await expect(listbox.getByText(/carro/i)).toBeVisible();
    await expect(listbox.getByText(/caminhão/i)).toBeVisible();
    await expect(listbox.getByText(/van/i)).toBeVisible();
    await expect(listbox.getByText(/moto/i)).toBeVisible();

    await authedPage.keyboard.press('Escape');
  });

  test('modal de confirmação ao inativar veículo', async ({ authedPage }) => {
    const activeRow = authedPage.getByRole('row').filter({
      has: authedPage.getByRole('cell').filter({ hasText: /^ativo$/i }),
    }).first();

    if (await activeRow.count() === 0) { test.skip(); return; }

    await activeRow.getByRole('button').click();

    const dialog = authedPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/inativar veículo/i)).toBeVisible();
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
