import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * Getting a roll of people into the product.
 *
 * The import is the reason this screen exists in the shape it does: a centre's
 * students are already in a spreadsheet, and retyping forty names is why a trial
 * stops on the first evening.
 */
test.describe('Candidates', () => {
  const person = (over: Record<string, unknown> = {}) => ({
    id: 'c1',
    fullName: 'Layla Hassan',
    email: 'layla@example.com',
    status: 0,
    groupNames: [],
    attemptCount: 0,
    creationTime: '2026-01-01T00:00:00Z',
    ...over,
  });

  const stubPeople = async (
    page: import('@playwright/test').Page,
    items: unknown[],
    onImport?: (body: { dryRun?: boolean }) => unknown,
  ) => {
    await page.route('**/api/assessment/candidates/groups', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'g1', name: 'Evening A1', memberCount: 4, creationTime: '2026-01-01T00:00:00Z' },
        ]),
      }),
    );

    await page.route('**/api/assessment/candidates/import', route => {
      const body = route.request().postDataJSON() as { dryRun?: boolean };
      const result = onImport
        ? onImport(body)
        : { created: 2, alreadyPresent: 0, addedToGroup: 0, problems: [] };

      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(result) });
    });

    await page.route('**/api/assessment/candidates?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: items.length, items }),
      }),
    );
  };

  test('lists people with their groups and how many attempts they have sat', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubPeople(page, [
      person({ groupNames: ['Evening A1'], attemptCount: 2, reference: 'STU-14' }),
    ]);

    await gotoApp(page, '/candidates');

    await expect(page.getByText('Layla Hassan')).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Evening A1' })).toBeVisible();
    await expect(page.getByText('STU-14')).toBeVisible();
  });

  test('the empty state offers the paste rather than a blank form', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubPeople(page, []);

    await gotoApp(page, '/candidates');

    // Somebody looking at an empty roll wants to get their list in, not to type
    // the first of forty names.
    await expect(page.getByText('Nobody here yet')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Paste a list' })).toHaveCount(2);
  });

  test('checks a pasted list before writing anything', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    const calls: { dryRun?: boolean }[] = [];

    await stubPeople(page, [], body => {
      calls.push(body);

      return {
        created: 2,
        alreadyPresent: 1,
        addedToGroup: 0,
        problems: [{ line: 3, content: 'broken row', reason: 'IMS:Import:NotAnEmail' }],
      };
    });

    await gotoApp(page, '/candidates');
    await page.getByRole('button', { name: 'Paste a list' }).first().click();

    await page.getByLabel('Names and email addresses').fill('a, a@x.com\nb, b@x.com\nbroken row');
    await page.getByRole('button', { name: 'Check the list' }).click();

    // Nothing written yet: the first call is a dry run.
    await expect.poll(() => calls.length).toBe(1);
    expect(calls[0].dryRun).toBe(true);

    // The problem is reported with its line number, at the moment somebody can
    // still do something about it.
    await expect(page.getByText('Nothing on this line looks like an email address.')).toBeVisible();
    await expect(page.getByText('3')).toBeVisible();
    await expect(page.getByText('already on your list')).toBeVisible();
  });

  test('committing sends the same list for real', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    const calls: { dryRun?: boolean }[] = [];

    await stubPeople(page, [], body => {
      calls.push(body);
      return { created: 2, alreadyPresent: 0, addedToGroup: 0, problems: [] };
    });

    await gotoApp(page, '/candidates');
    await page.getByRole('button', { name: 'Paste a list' }).first().click();

    await page.getByLabel('Names and email addresses').fill('a, a@x.com\nb, b@x.com');
    await page.getByRole('button', { name: 'Check the list' }).click();

    await expect(page.getByRole('button', { name: 'Add them' })).toBeEnabled();
    await page.getByRole('button', { name: 'Add them' }).click();

    await expect.poll(() => calls.length).toBe(2);
    expect(calls[1].dryRun).toBeFalsy();

    await expect(page.getByText('added', { exact: false })).toBeVisible();
  });

  test('says a person who has sat an exam cannot be removed', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubPeople(page, [person({ attemptCount: 3 })]);

    await gotoApp(page, '/candidates');
    await page.getByRole('button', { name: /^Delete:/ }).click();

    await expect(page.getByRole('alertdialog')).toContainText('their result refers to them');
  });

  test('hides removal from someone who may only read', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      grantedPolicies: ['Assessment.Candidates', 'Assessment.Candidates.View'],
    });
    await stubPeople(page, [person()]);

    await gotoApp(page, '/candidates');

    await expect(page.getByRole('button', { name: /^Delete:/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Paste a list' })).toHaveCount(0);
  });

  test('does not scroll sideways on a phone in Arabic', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubPeople(page, [person({ groupNames: ['Evening A1'] })]);

    await gotoApp(page, '/candidates');

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
