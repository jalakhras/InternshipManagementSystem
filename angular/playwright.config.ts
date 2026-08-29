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
      testIgnore: '**/live/**',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      // Candidates sit exams on phones. A layout that only works at 1440px is a
      // layout that does not work.
      name: 'mobile',
      testIgnore: '**/live/**',
      use: { ...devices['Pixel 7'] },
    },
    {
      // Against a real API and a real database.
      //
      // Opt-in — `npx playwright test --project=live` — because it needs the
      // host running and the database seeded, and because it writes rows.
      //
      // It exists because every defect found on 2026-08-29 was invisible to the
      // stubbed suite and to 187 unit and integration tests: a session token
      // never replaced after the start, a media route that did not exist, a BLOB
      // container with no provider, an [Authorize] naming an undefined policy.
      // None of those live inside a layer. They live between layers, and only a
      // wired-up application has those.
      name: 'live',
      testMatch: '**/live/**/*.spec.ts',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'npm start -- --no-open --port 4200',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env['CI'],
    timeout: 180_000,
  },
});
