import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * The application shell.
 *
 * Everything here is a property of the rendered document rather than of a class:
 * whether the page is genuinely right-to-left, whether a colour resolves to the
 * dark token, whether a menu item is absent when the permission is withheld. None
 * of those can be established by a unit test, and all of them are the kind of
 * thing that breaks quietly and is noticed by a customer.
 */
test.describe('Shell', () => {
  test('renders right-to-left in Arabic', async ({ page }) => {
    await stubAbp(page, { culture: 'ar' });
    await gotoApp(page);

    // dir has to be on <html>: the browser resolves scrollbar side, logical
    // properties and form control rendering from it before layout. Set it lower
    // down and the page is only half mirrored.
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar');
  });

  test('the sidebar sits on the right in Arabic and the left in English', async ({ page, isMobile }) => {
    test.skip(!!isMobile, 'docked layout only — the phone drawer has its own test');

    await stubAbp(page, { culture: 'ar' });
    await gotoApp(page);

    const sidebar = page.locator('nav.sidebar');
    await expect(sidebar).toBeVisible();

    const viewport = page.viewportSize()!;
    const rtlBox = (await sidebar.boundingBox())!;

    // In RTL the navigation belongs on the side the reader starts from.
    expect(rtlBox.x).toBeGreaterThan(viewport.width / 2);

    await page.getByRole('button', { name: 'English' }).click();

    await expect(page.locator('html')).toHaveAttribute('dir', 'ltr');

    const ltrBox = (await sidebar.boundingBox())!;
    expect(ltrBox.x).toBeLessThan(viewport.width / 2);
  });


  test("an organisation that runs in English starts its staff in English", async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });

    await page.route('**/api/assessment/settings', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          organizationName: 'Northern Language Centre',
          defaultLanguage: 'en',
          timeZone: 'Asia/Riyadh',
          defaultPassingPercentage: 60,
          showResultToCandidate: true,
          collectIntegritySignals: true,
        }),
      }),
    );

    await gotoApp(page, '/');

    // The setting was saved, read back, and applied nowhere — so a centre that
    // chose English watched every member of its staff land in Arabic and had to
    // tell each of them to switch.
    await expect(page.locator('html')).toHaveAttribute('lang', 'en');
    await expect(page.locator('html')).toHaveAttribute('dir', 'ltr');
  });

  test("a person's own choice outlives the organisation's default", async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });

    await page.route('**/api/assessment/settings', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          organizationName: 'Northern Language Centre',
          defaultLanguage: 'en',
          timeZone: 'Asia/Riyadh',
          defaultPassingPercentage: 60,
          showResultToCandidate: true,
          collectIntegritySignals: true,
        }),
      }),
    );

    // This person picked Arabic on a previous visit.
    await page.addInitScript(() => localStorage.setItem('astro.language', 'ar'));

    await gotoApp(page, '/');

    // The half that decides whether the other half is worth having. A default
    // that overrode somebody's own choice would be worse than no default: an
    // English-speaking administrator at an Arabic centre would be switched back
    // on every single visit.
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar');
  });

  test('switching language changes the text, not only the direction', async ({ page }) => {
    await stubAbp(page, { culture: 'ar' });
    await gotoApp(page);

    await expect(page.getByRole('heading', { name: 'أهلاً بك' })).toBeVisible();

    await page.getByRole('button', { name: 'English' }).click();

    // The bug this guards: ABP's setLanguage patches the session store and sets
    // the lang attribute, and nothing listens. Its own themes had a language
    // component that re-fetched the translations; this app replaced that theme, so
    // the layout mirrored while every string stayed Arabic until the next full
    // page load. Asserting on dir alone would have passed the whole time.
    await expect(page.getByRole('heading', { name: 'Welcome' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'أهلاً بك' })).toHaveCount(0);
  });

  test('switching language does not lose the current route', async ({ page }) => {
    await stubAbp(page, { culture: 'ar' });
    await gotoApp(page, '/exams');

    await page.getByRole('button', { name: 'English' }).click();

    // A language switch that returns you to the home page throws away whatever
    // you were doing, which is worse than not offering the switch.
    await expect(page).toHaveURL(/\/exams$/);
    await expect(page.locator('html')).toHaveAttribute('dir', 'ltr');
  });

  test('hides navigation the viewer has no permission for', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      // A reviewer: they mark answers and see results, nothing else.
      grantedPolicies: ['Assessment.Review.ViewQueue', 'Assessment.Results.View'],
    });
    await gotoApp(page);

    const nav = page.locator('nav.sidebar');

    await expect(nav.getByRole('link', { name: 'Manual review' })).toBeVisible();
    await expect(nav.getByRole('link', { name: 'Results' })).toBeVisible();

    // Not security — the server enforces that — but showing a link that will be
    // refused teaches people the product is unreliable.
    await expect(nav.getByRole('link', { name: 'Exams' })).toHaveCount(0);
    await expect(nav.getByRole('link', { name: 'Candidates' })).toHaveCount(0);
  });

  test('drops a section whose every item is hidden', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ['Assessment.Review.ViewQueue'] });
    await gotoApp(page);

    const nav = page.locator('nav.sidebar');

    // A heading with nothing under it reads as a section that failed to load.
    await expect(nav.getByRole('heading', { name: 'Configuration' })).toHaveCount(0);
    await expect(nav.getByRole('heading', { name: 'Results' })).toBeVisible();
  });

  test('theme toggle moves through system, light and dark', async ({ page }) => {
    await stubAbp(page, { culture: 'en' });
    await gotoApp(page);

    const html = page.locator('html');
    const toggle = page.getByRole('button', { name: 'Switch appearance' });

    // 'system' deliberately stamps nothing, so the tokens fall through to
    // prefers-color-scheme — which is what most visitors actually get.
    await expect(html).not.toHaveAttribute('data-theme', /.+/);

    await toggle.click();
    await expect(html).toHaveAttribute('data-theme', 'light');

    await toggle.click();
    await expect(html).toHaveAttribute('data-theme', 'dark');
  });

  test('dark mode actually repaints the surface', async ({ page }) => {
    await stubAbp(page, { culture: 'en' });
    await gotoApp(page);

    const background = () =>
      page.evaluate(() => getComputedStyle(document.body).backgroundColor);

    const light = await background();

    await page.getByRole('button', { name: 'Switch appearance' }).click(); // light
    await page.getByRole('button', { name: 'Switch appearance' }).click(); // dark

    // Waited for before the colour is read. The app is zoneless, so the signal
    // effect that stamps the attribute lands in a later microtask than the click
    // resolves — reading computed style straight after the click races it and
    // reports the old colour.
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');

    const dark = await background();

    // The failure this guards is a token defined only inside a media query: the
    // attribute flips, nothing repaints, and the page renders one theme's text on
    // the other theme's ground.
    expect(dark).not.toBe(light);
  });

  test('the skip link is the first thing a keyboard reaches', async ({ page }) => {
    await stubAbp(page, { culture: 'en' });
    await gotoApp(page);

    await page.keyboard.press('Tab');

    const focused = page.locator(':focus');
    await expect(focused).toHaveAttribute('href', '#astro-main');
    // Hidden until focused, then visible — otherwise it is a link nobody can use.
    await expect(focused).toBeVisible();
  });

  test('every interactive control shows a focus ring', async ({ page }) => {
    await stubAbp(page, { culture: 'en' });
    await gotoApp(page);

    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    const outline = await page.evaluate(() => {
      const el = document.activeElement as HTMLElement | null;
      return el ? getComputedStyle(el).outlineWidth : '0px';
    });

    // Removing the ring to tidy a design makes the product unusable without a
    // mouse, and the exam screen has to be operable that way.
    expect(parseFloat(outline)).toBeGreaterThan(0);
  });

  test('the page never scrolls sideways', async ({ page }) => {
    await stubAbp(page, { culture: 'ar' });
    await gotoApp(page);

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    // Horizontal scroll on the body is always a bug, and in RTL it is the usual
    // symptom of a physical margin that should have been logical.
    expect(overflows).toBe(false);
  });

  test('shows the four starting steps to someone who can act on them', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await gotoApp(page);

    await expect(page.getByRole('heading', { name: 'Welcome' })).toBeVisible();
    await expect(page.getByRole('link', { name: /Name what you measure/ })).toBeVisible();
    await expect(page.getByRole('link', { name: /Create an exam/ })).toBeVisible();
    await expect(page.getByRole('link', { name: /Add people/ })).toBeVisible();
    await expect(page.getByRole('link', { name: /Send it out/ })).toBeVisible();
  });

  test('offers a reviewer only the steps they can take', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ['Assessment.Review.ViewQueue'] });
    await gotoApp(page);

    // Suggesting "create an exam" to someone who cannot is worse than an empty
    // dashboard: it is an instruction they will follow into a refusal.
    await expect(page.getByRole('link', { name: /Create an exam/ })).toHaveCount(0);
  });
});

test.describe('Shell on a phone', () => {
  test.skip(({ isMobile }) => !isMobile, 'drawer behaviour is mobile-only');

  test('the sidebar is a drawer that opens and closes', async ({ page }) => {
    await stubAbp(page, { culture: 'ar' });
    await gotoApp(page);

    const sidebar = page.locator('nav.sidebar');
    const viewport = page.viewportSize()!;

    // Off-screen to start: on a phone the content matters more than the menu.
    const box = await sidebar.boundingBox();
    expect(box === null || box.x >= viewport.width - 1).toBe(true);

    await page.getByRole('button', { name: 'إظهار القائمة أو إخفاؤها' }).click();

    // Polled rather than measured once: the drawer slides over 200ms, and a single
    // reading straight after the click catches it mid-transition and reports a
    // position that is true for a frame and wrong as a conclusion.
    await expect
      .poll(async () => (await sidebar.boundingBox())?.x ?? viewport.width)
      .toBeLessThan(viewport.width - 100);

    // Tapping away closes it, which is what a drawer is expected to do. Clicked
    // near the far edge rather than at the scrim's centre, which sits underneath
    // the open drawer — a real finger lands on the exposed part.
    await page.locator('.scrim').click({ position: { x: 20, y: 200 } });
    await expect(sidebar).not.toHaveClass(/sidebar--open/);
  });

  test('touch targets are large enough to hit', async ({ page }) => {
    await stubAbp(page, { culture: 'ar' });
    await gotoApp(page);

    const menuButton = page.getByRole('button', { name: 'إظهار القائمة أو إخفاؤها' });
    const box = (await menuButton.boundingBox())!;

    // 44px is the point below which people miss — and a candidate who mis-taps
    // under a countdown loses marks to our layout.
    expect(box.width).toBeGreaterThanOrEqual(44);
    expect(box.height).toBeGreaterThanOrEqual(44);
  });
});
