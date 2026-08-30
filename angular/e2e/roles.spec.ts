import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * Roles, and what each one may do.
 *
 * The product ships five roles the business decided on, and until now they were
 * the only five there could ever be: the API to create another has always been
 * there and nothing reached it. An organisation whose shape does not match ours
 * — an invigilator who may only monitor sittings — had no way to say so.
 */
test.describe('Roles', () => {
  // ALL_POLICIES collects this product's own permission tree. Roles belong to
  // ABP's identity module and are guarded by its permissions, so they have to be
  // named here — which is itself the thing the last test checks.
  const MAY_MANAGE_ROLES = [
    ...ALL_POLICIES,
    'AbpIdentity.Roles',
    'AbpIdentity.Roles.Create',
    'AbpIdentity.Roles.Update',
    'AbpIdentity.Roles.Delete',
    'AbpIdentity.Roles.ManagePermissions',
  ];

  const role = (over: Record<string, unknown> = {}) => ({
    id: 'r1',
    name: 'Marker',
    isDefault: false,
    isStatic: true,
    isPublic: true,
    ...over,
  });

  const stubRoles = async (page: import('@playwright/test').Page, roles: unknown[]) => {
    await page.route('**/api/identity/roles**', route => {
      if (route.request().method() !== 'GET') {
        return route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
      }

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: roles, totalCount: roles.length }),
      });
    });

    await page.route('**/api/permission-management/permissions**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          entityDisplayName: 'Marker',
          groups: [
            {
              name: 'Assessment',
              displayName: 'Assessment',
              permissions: [
                {
                  name: 'Assessment.Review',
                  displayName: 'Review',
                  parentName: null,
                  isGranted: true,
                  grantedProviders: [],
                },
                {
                  name: 'Assessment.Review.Grade',
                  displayName: 'Grade an answer',
                  parentName: 'Assessment.Review',
                  isGranted: false,
                  grantedProviders: [],
                },
              ],
            },
          ],
        }),
      }),
    );
  };

  test('lists the roles and says which are built in', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE_ROLES });
    await stubRoles(page, [role(), role({ id: 'r2', name: 'Invigilator', isStatic: false })]);

    await gotoApp(page, '/roles');

    await expect(page.getByText('Marker')).toBeVisible();
    await expect(page.getByText('Invigilator')).toBeVisible();

    // A built-in role's name is a key other code depends on, so it cannot be
    // renamed or deleted — and the row says which kind it is in words rather
    // than by a colour or by the absence of a button.
    await expect(page.getByText('Built in')).toBeVisible();
    await expect(page.getByText('Added')).toBeVisible();
  });

  test('a built-in role cannot be renamed or deleted', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE_ROLES });
    await stubRoles(page, [role()]);

    await gotoApp(page, '/roles');

    await expect(page.getByRole('button', { name: /Delete: Marker/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Edit: Marker/ })).toHaveCount(0);

    // Its permissions are still editable: what a built-in role may do is the
    // organisation's decision, even though its name is not.
    await expect(page.getByRole('button', { name: 'Permissions' })).toBeVisible();
  });

  test('ticking a permission ticks the one it sits under', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: MAY_MANAGE_ROLES });
    await stubRoles(page, [role()]);

    await gotoApp(page, '/roles');
    await page.getByRole('button', { name: 'Permissions' }).click();

    const parent = page.getByLabel('Review', { exact: true });
    const child = page.getByLabel('Grade an answer');

    await expect(parent).toBeChecked();
    await expect(child).not.toBeChecked();

    // Unticking the parent has to take the children with it: the server does not
    // honour a child grant whose parent is off, so a screen that let the two
    // disagree would report a permission the role does not have.
    await parent.uncheck();
    await expect(child).not.toBeChecked();

    await child.check();
    await expect(parent).toBeChecked();
  });

  test('somebody who may not manage roles never reaches the screen', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubRoles(page, [role()]);

    // Every one of this product's own permissions, and still not this screen:
    // the guard is ABP's, because the identity module owns roles. Inventing a
    // second name for the same authority is how two guards end up disagreeing
    // about who may do what.
    await page.goto('/roles');
    await page.waitForURL('**/');

    await expect(page.getByText('Built in')).toHaveCount(0);
  });
});
