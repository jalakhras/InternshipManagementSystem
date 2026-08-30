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
