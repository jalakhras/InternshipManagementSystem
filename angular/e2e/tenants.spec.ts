import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * The organisations sharing this deployment.
 *
 * Adding one was an HTTP call somebody had to make by hand, which meant the
 * product could not be sold to a second customer without an engineer present.
 */
test.describe('Organisations', () => {
  const MAY_MANAGE = [
    ...ALL_POLICIES,
    'AbpTenantManagement.Tenants',
    'AbpTenantManagement.Tenants.Create',
    'AbpTenantManagement.Tenants.Update',
    'AbpTenantManagement.Tenants.Delete',
  ];

  const stubTenants = async (page: import('@playwright/test').Page) => {
    await page.route('**/api/multi-tenancy/tenants**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{ id: 't1', name: 'trading-academy' }],
          totalCount: 1,
        }),
      }),
    );
  };

  test('an organisation is looked for on the server, not among the rows on screen', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE });

    let asked = '';

    await page.route('**/api/multi-tenancy/tenants**', route => {
      const url = new URL(route.request().url());

      asked = url.searchParams.get('filter') ?? '';

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: asked ? [{ id: 't2', name: 'language-centre' }] : [{ id: 't1', name: 'trading-academy' }],
          totalCount: asked ? 1 : 140,
        }),
      });
    });

    await gotoApp(page, '/organisations');

    await expect(page.getByText('trading-academy')).toBeVisible();

    await page.getByLabel('Search organisations').fill('language');
    await page.getByLabel('Search organisations').press('Enter');

    // The term reaches the server. It used to ask for a hundred rows and no
    // term at all, so a deployment with a hundred and one customers had one
    // nobody could rename or reach — and there is no other screen in the
    // product that lists organisations.
    await expect.poll(() => asked).toBe('language');
    await expect(page.getByText('language-centre')).toBeVisible();
  });

  test('a search that matches nothing does not read as an empty deployment', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE });

    await page.route('**/api/multi-tenancy/tenants**', route => {
      const url = new URL(route.request().url());
      const filter = url.searchParams.get('filter') ?? '';

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: filter ? [] : [{ id: 't1', name: 'trading-academy' }],
          totalCount: filter ? 0 : 1,
        }),
      });
    });

    await gotoApp(page, '/organisations');

    await page.getByLabel('Search organisations').fill('nobody');
    await page.getByLabel('Search organisations').press('Enter');

    // "No organisations yet" after a search says the deployment is empty, which
    // is a different and alarming sentence for the person who runs it.
    await expect(page.getByText('No organisation matches "nobody"')).toBeVisible();
  });

  test('a long list of organisations is paged, and the page is asked for', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE });

    let skipped = '';

    await page.route('**/api/multi-tenancy/tenants**', route => {
      const url = new URL(route.request().url());

      skipped = url.searchParams.get('skipCount') ?? '';

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{ id: 't1', name: 'page-' + skipped }],
          totalCount: 140,
        }),
      });
    });

    await gotoApp(page, '/organisations');

    await expect(page.getByText('page-0')).toBeVisible();

    await page.getByRole('button', { name: 'Next' }).click();

    // Asked of the server rather than sliced from what the browser holds: the
    // hundred-and-first organisation has to be reachable, and it is the one a
    // fixed cap makes invisible.
    await expect.poll(() => skipped).toBe('25');
  });

  test('a new organisation cannot be created without a way in', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE });
    await stubTenants(page);

    await gotoApp(page, '/organisations');
    await page.getByRole('button', { name: 'New organisation' }).click();

    await page.getByLabel('Name').fill('language-centre');

    // An organisation with no administrator is worse than none: it exists, it
    // holds a name somebody chose, and nobody can reach it.
    await expect(page.getByRole('button', { name: 'Save' })).toBeDisabled();

    await page.getByLabel(/administrator/).fill('head@centre.test');
    await page.getByLabel('Their password').fill('1q2w3E*');

    await expect(page.getByRole('button', { name: 'Save' })).toBeEnabled();
  });

  test('deleting asks for the name to be typed back', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE });
    await stubTenants(page);

    await gotoApp(page, '/organisations');
    await page.getByRole('button', { name: /Delete: trading-academy/ }).click();

    // Everything the organisation owns goes with it. A dialog somebody can
    // dismiss by reflex is not a confirmation for that.
    await expect(page.getByRole('button', { name: 'Delete', exact: true })).toBeDisabled();

    await page.getByLabel(/Type/).fill('trading-acadmy');
    await expect(page.getByRole('button', { name: 'Delete', exact: true })).toBeDisabled();

    await page.getByLabel(/Type/).fill('trading-academy');
    await expect(page.getByRole('button', { name: 'Delete', exact: true })).toBeEnabled();
  });

  test('an organisation cannot see that other organisations exist', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubTenants(page);

    // Every one of this product's own permissions and still not this screen.
    // A tenant is never granted the host's authority, and that is most of what
    // multi-tenancy means here.
    await page.goto('/organisations');
    await page.waitForURL('**/');

    await expect(page.getByText('trading-academy')).toHaveCount(0);
  });
});
