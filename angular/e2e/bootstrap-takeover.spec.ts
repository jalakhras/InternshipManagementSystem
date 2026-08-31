import { Page, expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubExams } from './support/exam-stub';
import { contrastRatio } from './support/contrast';

/**
 * The four things Bootstrap still owned after the token layer took the rest.
 *
 * Every assertion here is a measurement of the rendered page, because that is
 * the only place these defects existed: the stylesheets read correctly in all
 * four cases. `.table` declared a 12px radius that `border-collapse` made a
 * no-op; the component `tr:hover` rule was painted over by a Bootstrap cell
 * rule with a longer selector; `--bs-border-radius` was simply never named; and
 * `.alert-danger` was a colour nobody had looked at since it moved inside a
 * dialog. A test that reads the source would have passed on all four.
 */

/** A colour token resolved by the browser, so it can be compared to a computed value. */
const resolve = (page: Page, token: string) =>
  page.evaluate(name => {
    const probe = document.createElement('div');
    probe.style.color = `var(${name})`;
    document.body.appendChild(probe);
    const value = getComputedStyle(probe).color;
    probe.remove();
    return value;
  }, token);

const styleOf = (page: Page, selector: string, property: string) =>
  page.evaluate(
    ([sel, prop]) => {
      const element = document.querySelector(sel);
      return element ? getComputedStyle(element)[prop as never] as string : null;
    },
    [selector, property] as const,
  );

/** Puts the failed-action banner on the exam list, which is where it lives on ~20 screens. */
async function showErrorBanner(page: Page): Promise<void> {
  await stubExams(page);

  await page.route('**/api/assessment/exams/*/publish', route =>
    route.fulfill({
      status: 403,
      contentType: 'application/json',
      body: JSON.stringify({ error: { message: 'An exam cannot be published without questions.' } }),
    }),
  );

  await gotoApp(page, '/exams');
  await page
    .getByRole('row', { name: /Onboarding Safety Refresher/ })
    .getByRole('button', { name: /^Publish:/ })
    .click();

  await expect(page.locator('.alert-danger')).toBeVisible();
}

test.describe('The error banner', () => {
  test('is painted from the failure tokens, not from Bootstrap own pink', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await showErrorBanner(page);

    const banner = page.locator('.alert-danger');

    // rgb(248, 215, 218) on rgb(88, 21, 28) is what this measured before: a red
    // from a different family than every other failure in the product speaks in.
    await expect(banner).toHaveCSS('background-color', await resolve(page, '--status-fail-bg'));
    await expect(banner).toHaveCSS('color', await resolve(page, '--status-fail-text'));

    expect(await contrastRatio(banner)).toBeGreaterThanOrEqual(4.5);
  });

  test.describe('in dark mode', () => {
    test.use({ colorScheme: 'dark' });

    test('turns over with the theme instead of staying pink', async ({ page }) => {
      await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
      await showErrorBanner(page);

      const banner = page.locator('.alert-danger');
      const background = await banner.evaluate(el => getComputedStyle(el).backgroundColor);

      // The literal failure: a pale pink slab across a near-black page, and now
      // across the top third of a near-black dialog.
      expect(background).not.toBe('rgb(248, 215, 218)');
      await expect(banner).toHaveCSS('background-color', await resolve(page, '--status-fail-bg'));

      expect(await contrastRatio(banner)).toBeGreaterThanOrEqual(4.5);
    });
  });
});

test.describe('The form layer geometry', () => {
  test('sits on the 4 / 8 / 12 scale rather than on Bootstrap 6px', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    // 0.375rem is Bootstrap's default and is not a step this product has.
    await expect(page.locator('input[type=search]')).toHaveCSS('border-radius', '8px');
  });
});

test.describe('The list table', () => {
  // Below 40rem every row becomes its own card and the shared frame is
  // deliberately removed, so the frame and the hover are desktop properties.
  test.use({ viewport: { width: 1280, height: 900 }, isMobile: false, hasTouch: false });

  test('paints the hovered row on the cells, where it can be seen', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    const row = page.getByRole('row', { name: /Spanish B1 Placement/ });
    await row.hover();

    const [rowBackground, cellBackground] = await row.evaluate(tr => [
      getComputedStyle(tr).backgroundColor,
      getComputedStyle(tr.querySelector('td')!).backgroundColor,
    ]);

    // Measured before: tr rgb(236, 239, 243) and td still rgb(255, 255, 255).
    // Bootstrap paints every cell opaque over the row, so the one affordance
    // telling a coordinator which row they are about to delete never rendered.
    expect(cellBackground).toBe(rowBackground);
    expect(cellBackground).toBe(await resolve(page, '--surface-sunken'));
    expect(cellBackground).not.toBe(await resolve(page, '--surface-raised'));
  });

  test('carries its frame on the element that can round it', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    await expect(page.getByRole('table')).toBeVisible();

    const wrapper = page.locator('.astro-scroll-x', { has: page.locator('table.table') });

    await expect(wrapper).toHaveCSS('border-radius', '12px');
    await expect(wrapper).toHaveCSS('border-top-width', '1px');

    // A radius on a border-collapse table is a no-op, so leaving it there is how
    // twelve screens came to show square corners beside 12px-rounded cards.
    expect(await styleOf(page, 'table.table', 'borderTopWidth')).toBe('0px');
  });
});
