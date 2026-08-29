import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubExams } from './support/exam-stub';

/**
 * Editing, publishing and deleting an exam from the list.
 *
 * The row is where someone acts on an exam, so the destructive action lives next
 * to the harmless ones. What earns the tests is the guarding around it: who may
 * see each action, and what a delete asks before it happens.
 */
test.describe('Exam row actions', () => {
  const DRAFT = 'Onboarding Safety Refresher';
  const PUBLISHED = 'Spanish B1 Placement';

  const row = (page: import('@playwright/test').Page, title: string) =>
    page.getByRole('row', { name: new RegExp(title) });

  test('offers publish on a draft and archive on a published exam', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    // A published exam cannot be published again, and a draft cannot be archived.
    // Offering either would be an action that only ever fails.
    await expect(row(page, DRAFT).getByRole('button', { name: /^Publish:/ })).toBeVisible();
    await expect(row(page, DRAFT).getByRole('button', { name: /^Archive:/ })).toHaveCount(0);

    await expect(row(page, PUBLISHED).getByRole('button', { name: /^Archive:/ })).toBeVisible();
    await expect(row(page, PUBLISHED).getByRole('button', { name: /^Publish:/ })).toHaveCount(0);
  });

  test('hides every action from someone who may only read', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ['Assessment.Exams', 'Assessment.Exams.View'] });
    await stubExams(page);
    await gotoApp(page, '/exams');

    // exact, because Playwright matches accessible names by substring: a loose
    // "Publish" also matches the "Published" status filter, which would report a
    // permission leak that is not there.
    await expect(page.getByRole('button', { name: /^Delete:/ })).toHaveCount(0);
    await expect(page.getByRole('link', { name: /^Edit:/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /^Publish:/ })).toHaveCount(0);
  });

  test('asks before deleting, and names the exam it would delete', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    await row(page, PUBLISHED).getByRole('button', { name: /^Delete:/ }).click();

    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toBeVisible();

    // "Delete this?" with no subject is how the wrong thing gets deleted.
    await expect(dialog).toContainText(PUBLISHED);

    // Deleting is soft on the server, and saying so is the difference between a
    // reversible action and one nobody dares take.
    await expect(dialog).toContainText('keep their results');
  });

  test('cancelling deletes nothing', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);

    let deleteCalls = 0;
    await page.route('**/api/assessment/exams/*', async route => {
      if (route.request().method() === 'DELETE') {
        deleteCalls++;
        return route.fulfill({ status: 204, body: '' });
      }
      return route.fallback();
    });

    await gotoApp(page, '/exams');
    await row(page, PUBLISHED).getByRole('button', { name: /^Delete:/ }).click();
    await page.getByRole('button', { name: 'Cancel' }).click();

    await expect(page.getByRole('alertdialog')).toHaveCount(0);
    expect(deleteCalls).toBe(0);
  });

  test('confirming deletes the exam the prompt named', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);

    const deleted: string[] = [];
    await page.route('**/api/assessment/exams/*', async route => {
      if (route.request().method() === 'DELETE') {
        deleted.push(new URL(route.request().url()).pathname.split('/').pop()!);
        return route.fulfill({ status: 204, body: '' });
      }
      return route.fallback();
    });

    await gotoApp(page, '/exams');
    await row(page, PUBLISHED).getByRole('button', { name: /^Delete:/ }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Delete', exact: true }).click();

    await expect.poll(() => deleted).toEqual(['11111111-1111-1111-1111-111111111111']);
  });

  test('says why an action failed rather than silently doing nothing', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);

    await page.route('**/api/assessment/exams/*/publish', route =>
      route.fulfill({
        status: 403,
        contentType: 'application/json',
        body: JSON.stringify({ error: { message: 'An exam cannot be published without questions.' } }),
      }),
    );

    await gotoApp(page, '/exams');
    await row(page, DRAFT).getByRole('button', { name: /^Publish:/ }).click();

    // The blocker is the only thing that tells the author what to do next.
    await expect(page.getByRole('alert')).toContainText('without questions');
  });

  test('the dialog is centred in Arabic too', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    await expect(page.getByRole('table')).toBeVisible();
    await page.locator('.row-action--danger').first().click();

    // Translate has no logical equivalent, so the RTL sign is written by hand —
    // the mistake that once parked a drawer two thirds of the way across a phone.
    const box = (await page.getByRole('alertdialog').boundingBox())!;
    const width = page.viewportSize()!.width;

    expect(box.x).toBeGreaterThan(0);
    expect(box.x + box.width).toBeLessThanOrEqual(width + 1);
  });
});
