import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E test configuration.
 *
 * Authentication strategy:
 * - Uses X-Dev-User header (dev-only bypass) to skip Google OAuth
 * - Tenant code is set via localStorage ('rail_factory_tenant_code')
 * - Both are injected in the global test setup via fixtures (see e2e/fixtures.ts)
 *
 * Base URL: Vite dev server (http://localhost:5082) — proxy forwards /api to BFF.
 * The app must be running (via aspire) before running these tests.
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: { timeout: 8_000 },
  fullyParallel: false,
  retries: 1,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],

  use: {
    baseURL: 'http://localhost:5082',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
