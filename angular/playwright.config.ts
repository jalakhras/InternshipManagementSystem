import { defineConfig, devices } from '@playwright/test';

/**
 * Browser-level tests for the Angular app.
 *
 * These check the things unit tests structurally cannot: that the document is
 * actually right-to-left, that a colour resolves to the dark token in dark mode,
 * that a menu item disappears when the permission is withheld. Every one of those
 * is a property of the rendered page, not of a class.
 *
 * The app is served by `ng serve` and the ABP configuration endpoints are stubbed
 * per test, so the suite runs without a database, a login or a backend. Full
 * end-to-end runs against the real API are a separate concern, and are only worth
 * their setup cost for the exam-taking journey.
 */
export default defineConfig({
  testDir: './e2e',

  // The countdown assertions wait on real seconds; anything tighter is flaky.
  timeout: 30_000,
  expect: { timeout: 5_000 },

  // Serial locally so a failure is readable; parallel in CI where nobody watches.
  fullyParallel: !!process.env['CI'],
  workers: process.env['CI'] ? undefined : 1,

  // A test that only passes on a retry is a test that failed. Retries in CI only,
  // where they distinguish a flaky network from a broken build.
  retries: process.env['CI'] ? 2 : 0,

  forbidOnly: !!process.env['CI'],

  reporter: process.env['CI'] ? [['github'], ['html', { open: 'never' }]] : [['list']],

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    // Arabic first, because that is what a first visitor gets and what most of
    // these tests are about.
    locale: 'ar',
  },

  projects: [
    {
      name: 'desktop',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      // Candidates sit exams on phones. A layout that only works at 1440px is a
      // layout that does not work.
      name: 'mobile',
      use: { ...devices['Pixel 7'] },
    },
  ],

  webServer: {
    command: 'npm start -- --no-open --port 4200',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env['CI'],
    timeout: 180_000,
  },
});
