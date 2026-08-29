import { Page } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/**
 * Stubs the ABP endpoints the app needs before it will render anything.
 *
 * The alternative is a live backend, a seeded database and a login for every run,
 * which turns a two-second check on text direction into a two-minute one and makes
 * the suite something nobody runs locally. What is under test here is the browser
 * behaviour — direction, tokens, permission filtering — and none of it needs a
 * real server to be exercised honestly.
 *
 * The exam-taking journey is the exception, and gets its own end-to-end suite
 * against the real API, because there the interesting behaviour *is* the server:
 * the deadline, the frozen form, the auto-submit.
 */
export interface StubOptions {
  /** Policies to report as granted. Everything absent is denied. */
  grantedPolicies?: string[];

  /** Culture the server reports as current. Drives `dir` and `lang`. */
  culture?: 'ar' | 'en';

  /** Pretend nobody is signed in, to check the guard redirects. */
  anonymous?: boolean;
}

/**
 * Loaded from the server's own resource files rather than copied here.
 *
 * A hand-maintained copy drifts, and the way it fails is quietly: a screen renders
 * raw keys, the assertion looks for the English text, and the test reports a bug in
 * the app. That happened twice before this was changed. Reading the real files also
 * means a missing translation fails a test, which is the right place to find out.
 */
const LOCALE_DIR = join(
  __dirname,
  '..',
  '..',
  '..',
  'src',
  'InternshipManagementSystem.Domain.Shared',
  'Localization',
  'InternshipManagementSystem',
);

function loadTexts(culture: 'ar' | 'en'): Record<string, string> {
  const raw = readFileSync(join(LOCALE_DIR, culture + '.json'), 'utf8');
  return JSON.parse(raw).texts as Record<string, string>;
}

const AR_TEXTS = loadTexts('ar');
const EN_TEXTS = loadTexts('en');

/** Everything a signed-in administrator would hold. */
export const ALL_POLICIES = [
  'Assessment.Exams.View',
  'Assessment.Exams.Create',
  'Assessment.Exams.Edit',
  'Assessment.Exams.Publish',
  'Assessment.Questions.View',
  'Assessment.Questions.Create',
  'Assessment.Candidates.View',
  'Assessment.Candidates.Create',
  'Assessment.Groups.View',
  'Assessment.Assignments.View',
  'Assessment.Assignments.Create',
  'Assessment.Review.ViewQueue',
  'Assessment.Results.View',
  'Assessment.Catalog.View',
  'Assessment.Catalog.Manage',
  'Assessment.IdentityManagement.Users.View',
  'Assessment.Administration.ManageSettings',
];

export async function stubAbp(page: Page, options: StubOptions = {}): Promise<void> {
  const culture = options.culture ?? 'ar';
  const granted = options.grantedPolicies ?? ALL_POLICIES;
  const texts = culture === 'ar' ? AR_TEXTS : EN_TEXTS;

  const policies: Record<string, boolean> = {};
  for (const policy of granted) {
    policies[policy] = true;
  }

  // Registered first on purpose: Playwright matches routes last-registered-first,
  // so this catch-all sits underneath every specific handler below. Anything that
  // reaches it is a call a test forgot to stub, and failing it loudly beats a
  // silent hang while the app waits on a response that never comes.
  await page.route('**/localhost:44373/**', route =>
    route.fulfill({ status: 404, contentType: 'application/json', body: '{}' }),
  );

  if (!options.anonymous) {
    // ABP 10 keeps OAuth tokens in memory (MemoryTokenStorageService), so a session
    // cannot be seeded from outside the page — there is no storage to write to.
    //
    // Instead the flow is stubbed at the protocol boundary: the authorisation
    // endpoint redirects straight back with a code, and the token endpoint answers
    // it. The real client code runs, which is what makes this a test of the app
    // rather than of a bypass.
    //
    // No id_token is returned. With one, the client would verify its signature
    // against a JWKS we would then have to sign for; without one it accepts the
    // access token and reports a valid session, which is all the route guard asks.
    await page.route('**/connect/authorize*', route => {
      const state = new URL(route.request().url()).searchParams.get('state') ?? '';

      route.fulfill({
        status: 302,
        headers: {
          location: `http://localhost:4200/?code=e2e-authorization-code&state=${encodeURIComponent(state)}`,
        },
        body: '',
      });
    });

    await page.route('**/connect/token', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          access_token: 'e2e-access-token',
          token_type: 'Bearer',
          expires_in: 3600,
          scope: 'openid offline_access InternshipManagementSystem',
        }),
      }),
    );
  }

  await page.route('**/api/abp/application-configuration*', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        localization: {
          currentCulture: {
            cultureName: culture,
            name: culture,
            displayName: culture === 'ar' ? 'العربية' : 'English',
            englishName: culture === 'ar' ? 'Arabic' : 'English',
            twoLetterIsoLanguageName: culture,
            isRightToLeft: culture === 'ar',
            dateTimeFormat: {},
          },
          // Both languages offered, so the switcher has something to switch to.
          languages: [
            { cultureName: 'ar', uiCultureName: 'ar', displayName: 'العربية' },
            { cultureName: 'en', uiCultureName: 'en', displayName: 'English' },
          ],
          values: { InternshipManagementSystem: texts },
          resources: {
            InternshipManagementSystem: { texts, baseResources: [] },
          },
          defaultResourceName: 'InternshipManagementSystem',
          languagesMap: {},
          languageFilesMap: {},
        },
        auth: { grantedPolicies: policies, policies },
        currentUser: options.anonymous
          ? { isAuthenticated: false, id: null, userName: null, roles: [] }
          : {
              isAuthenticated: true,
              id: '11111111-1111-1111-1111-111111111111',
              userName: 'admin',
              name: 'Admin',
              email: 'admin@example.com',
              roles: ['admin'],
            },
        setting: { values: {} },
        features: { values: {} },
        globalFeatures: { enabledFeatures: [] },
        multiTenancy: { isEnabled: true },
        currentTenant: { id: null, name: null, isAvailable: false },
        timing: { timeZone: { iana: { timeZoneName: 'Asia/Riyadh' }, windows: {} } },
        clock: { kind: 'Local' },
        objectExtensions: { modules: {}, enums: {} },
      }),
    }),
  );

  // ABP 10 fetches localisation from its own endpoint rather than only inside the
  // application configuration, and blocks the first render until it answers.
  // Answers for the culture the request asks for, not the one this stub was built
  // with. A stub that always returned the same language would report a language
  // switch as working while the app still showed the old strings — which is
  // exactly the bug this suite exists to catch, so the stub has to be honest.
  await page.route('**/api/abp/application-localization*', route => {
    const requested = new URL(route.request().url()).searchParams.get('cultureName') ?? culture;
    const base = requested.split('-')[0] === 'ar' ? 'ar' : 'en';
    const body = base === 'ar' ? AR_TEXTS : EN_TEXTS;

    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        resources: {
          InternshipManagementSystem: { texts: body, baseResources: [] },
        },
        currentCulture: {
          cultureName: base,
          name: base,
          displayName: base === 'ar' ? 'العربية' : 'English',
          englishName: base === 'ar' ? 'Arabic' : 'English',
          twoLetterIsoLanguageName: base,
          isRightToLeft: base === 'ar',
          dateTimeFormat: {},
        },
      }),
    });
  });

  // ABP asks for these on boot; an unhandled request would leave it waiting.
  await page.route('**/api/abp/api-definition*', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ modules: {} }) }),
  );

  // The OIDC client fetches the discovery document before anything renders. Left
  // unstubbed it reaches a real server over a self-signed certificate, fails, and
  // the app never boots — which is what these tests were actually failing on.
  await page.route('**/.well-known/openid-configuration', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        issuer: 'https://localhost:44373/',
        authorization_endpoint: 'https://localhost:44373/connect/authorize',
        token_endpoint: 'https://localhost:44373/connect/token',
        userinfo_endpoint: 'https://localhost:44373/connect/userinfo',
        end_session_endpoint: 'https://localhost:44373/connect/logout',
        jwks_uri: 'https://localhost:44373/.well-known/jwks',
        response_types_supported: ['code'],
        grant_types_supported: ['authorization_code', 'refresh_token'],
        scopes_supported: ['openid', 'profile', 'offline_access'],
        code_challenge_methods_supported: ['S256'],
      }),
    }),
  );

  await page.route('**/.well-known/jwks*', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ keys: [] }) }),
  );

}

/**
 * Navigates and waits for the app to settle.
 *
 * `page.goto` resolves when the document loads, but the app then bounces through
 * the authorisation endpoint and back with a code before it renders anything.
 * Asserting in that window finds an empty page and reports a bug in the app that
 * is really a bug in the test — which is exactly what happened the first time this
 * suite ran.
 *
 * Waits for the shell to be in the DOM rather than for a duration, so it is as
 * fast as the app is and does not become flaky on a slower machine.
 */
export async function gotoApp(page: Page, path = '/'): Promise<void> {
  await page.goto(path);
  await page.locator('astro-shell').waitFor({ state: 'attached', timeout: 15_000 });
  await page.locator('#astro-main').waitFor({ state: 'visible', timeout: 15_000 });
}
