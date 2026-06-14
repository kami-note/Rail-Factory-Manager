import { test, expect } from './fixtures';

test.describe('Navegação e Sidebar', () => {
  test.beforeEach(async ({ authedPage }) => {
    await authedPage.goto('/app/production/work-centers');
    await authedPage.waitForURL('**/app/production/work-centers');
    await expect(authedPage.getByRole('progressbar')).toHaveCount(0, { timeout: 10_000 });
  });

  test('sidebar exibe botão PESSOAS com permissão hr.read', async ({ authedPage }) => {
    // ListItemButton — renderiza como role="button"
    await expect(authedPage.getByRole('button', { name: /^funcionários$/i })).toBeVisible();
  });

  test('sidebar exibe botão FROTA com permissão fleet.read', async ({ authedPage }) => {
    await expect(authedPage.getByRole('button', { name: /^frota$/i })).toBeVisible();
  });

  test('navegação para /app/hr/people via sidebar', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /^funcionários$/i }).click();
    await authedPage.waitForURL('**/app/hr/people', { timeout: 10_000 });
    // Confirma que a página carregou: o botão Nova Pessoa é único nesta página
    await expect(authedPage.getByRole('button', { name: /nova pessoa/i })).toBeVisible();
  });

  test('navegação para /app/fleet/vehicles via sidebar', async ({ authedPage }) => {
    await authedPage.getByRole('button', { name: /^frota$/i }).click();
    await authedPage.waitForURL('**/app/fleet', { timeout: 10_000 });
    await expect(authedPage.getByRole('button', { name: /novo veículo/i })).toBeVisible();
  });
});
