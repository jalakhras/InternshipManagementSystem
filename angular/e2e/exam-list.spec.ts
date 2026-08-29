import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubExams } from './support/exam-stub';

/**
 * The exam list.
 *
 * Checks the behaviour that only exists once data is on screen: that the table
 * reverses with the page, that the empty state distinguishes "none yet" from
 * "nothing matches", and that an author without permission is not shown an action
 * they cannot take.
 */
test.describe('Exam list', () => {
  test('shows exams with their status and form summary', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    // exact, because each row also carries an "Edit: <title>" action whose
    // accessible name names the exam — which is the point of it, for anyone
    // hearing the row rather than seeing it.
    await expect(page.getByRole('link', { name: 'Spanish B1 Placement', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Technical Analysis — Level 2', exact: true })).toBeVisible();

    // 25 drawn from a bank of 120: the two numbers together are the anti-leak
    // mechanism, so both belong in the cell.
    await expect(page.getByText('25 / 120')).toBeVisible();
  });

  test('the table reverses with the page in Arabic', async ({ page, isMobile }) => {
    test.skip(!!isMobile, 'column order is a desktop-width concern');

    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    const first = page.locator('thead th').first();
    const last = page.locator('thead th').last();

    const firstBox = (await first.boundingBox())!;
    const lastBox = (await last.boundingBox())!;

    // In RTL the first column sits at the right. A table that keeps its columns
    // left-to-right under Arabic text is the same mirror error that put the exam
    // question on the wrong side in the generated design.
    expect(firstBox.x).toBeGreaterThan(lastBox.x);
  });

  test('distinguishes an empty bank from an empty search', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page, { items: [] });
    await gotoApp(page, '/exams');

    // Nothing yet: offer the action that fills the list.
    await expect(page.getByText('No exams yet')).toBeVisible();
    await expect(page.getByRole('link', { name: 'New exam' }).last()).toBeVisible();

    await page.getByPlaceholder('Search by title').fill('nothing matches this');
    await page.getByPlaceholder('Search by title').press('Enter');

    // A search with no results is a different situation and needs different words:
    // "none yet" would be a lie, and offering "create" is the wrong next step.
    await expect(page.getByText('Nothing matches')).toBeVisible();
  });

  test('hides the create action from someone who cannot create', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ['Assessment.Exams.View'] });
    await stubExams(page, { items: [] });
    await gotoApp(page, '/exams');

    await expect(page.getByText('No exams yet')).toBeVisible();
    await expect(page.getByRole('link', { name: 'New exam' })).toHaveCount(0);
  });

  test('shows the failure reason rather than a generic apology', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page, { failWith: 'The exam service is unavailable.' });
    await gotoApp(page, '/exams');

    // The reason is the only thing that tells the reader whether retrying helps.
    await expect(page.getByText('The exam service is unavailable.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible();
  });

  test('the list never scrolls the page sideways', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubExams(page);
    await gotoApp(page, '/exams');

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    // A seven-column table on a phone is exactly where a physical margin or a
    // missing overflow container shows up.
    expect(overflows).toBe(false);
  });
});
