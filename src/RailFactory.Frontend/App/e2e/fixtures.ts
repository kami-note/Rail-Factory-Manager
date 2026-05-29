import { test as base, expect, type Page } from '@playwright/test';

const DEV_USER = 'yurinote666@gmail.com';
const TENANT_CODE = 'dev';
const TENANT_STORAGE_KEY = 'rail_factory_tenant_code';

/**
 * Sets up the dev auth context:
 * 1. Adds X-Dev-User header to all requests (bypasses Google OAuth)
 * 2. Sets the tenant code in localStorage before page load
 */
async function setupDevAuth(page: Page) {
  // Inject X-Dev-User into every request the browser makes
  await page.setExtraHTTPHeaders({ 'X-Dev-User': DEV_USER });

  // Pre-seed localStorage so tenant selector is skipped
  await page.addInitScript((key: string) => {
    localStorage.setItem(key, 'dev');
  }, TENANT_STORAGE_KEY);
}

/**
 * Extended test fixture with dev auth pre-configured.
 * Use `authedPage` in your tests instead of `page` to get a logged-in browser.
 */
export const test = base.extend<{ authedPage: Page }>({
  authedPage: async ({ page }, use) => {
    await setupDevAuth(page);
    await use(page);
  },
});

export { expect };
export { TENANT_CODE };
