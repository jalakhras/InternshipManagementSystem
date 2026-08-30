import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * The first screen of the product, for each role that has one.
 *
 * It offered four cards, all of them steps in setting an exam up, each gated on
 * a permission only the people who set exams up hold. So a marker signing in for
 * the first time was shown a welcome, then *"four steps between here and your
 * first exam in someone's hands"* under a heading reading "Getting started" —
 * with nothing beneath it. Two of the five roles the business defines landed on
 * a page that promised four things and showed none.
 */
test.describe('Dashboard', () => {
  test('a marker is shown the queue they came here for', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      grantedPolicies: ['Assessment.Review', 'Assessment.Review.ViewQueue'],
    });

    await gotoApp(page, '/');

    await expect(page.getByText('Mark what is waiting')).toBeVisible();

    // And not told they are four steps from an exam they will never create.
    await expect(page.getByText('Four steps between here')).toHaveCount(0);
    await expect(page.getByText('What is waiting for you')).toBeVisible();
  });

  test('an observer is shown the results', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      grantedPolicies: ['Assessment.Results', 'Assessment.Results.View'],
    });

    await gotoApp(page, '/');

    await expect(page.getByText('Read the results')).toBeVisible();
  });

  test('somebody with nothing granted is told so, not left on an empty page', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: [] });

    await gotoApp(page, '/');

    // An account with no role can sign in, and this is what it sees. A heading
    // over blank space reads as a broken page; the sentence names who can fix it.
    await expect(page.getByText('Nothing has been assigned to you yet')).toBeVisible();
  });

  test('an administrator still gets the setup journey', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    await gotoApp(page, '/');

    // The half that keeps the change honest: adding cards for two roles must not
    // take the original four away from the role they were written for.
    await expect(page.getByText('Four steps between here')).toBeVisible();
    await expect(page.getByText('Name what you measure')).toBeVisible();
    await expect(page.getByText('Create an exam')).toBeVisible();
    await expect(page.getByText('Add people')).toBeVisible();
    await expect(page.getByText('Send it out')).toBeVisible();
  });
});
