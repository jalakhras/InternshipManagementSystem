import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * The reviewer's work.
 *
 * Two things earn tests here. The rubric adds up rather than asking somebody to
 * do arithmetic under time pressure — a wrong total looks exactly like a
 * considered one. And behavioural observations are described, never scored.
 */
test.describe('Review', () => {
  const ATTEMPT = 'aaaaaaaa-1111-1111-1111-111111111111';

  const answer = (over: Record<string, unknown> = {}) => ({
    answerId: 'ans1',
    questionId: 'q1',
    questionText: 'Explain why the trend reversed.',
    questionType: 'text',
    maxScore: 10,
    response: 'Because the volume dried up at the high.',
    rubric: [],
    wasPasted: false,
    keystrokeCount: 0,
    backspaceCount: 0,
    ...over,
  });

  const stubReview = async (
    page: import('@playwright/test').Page,
    answers: unknown[],
    queue: unknown[] = [],
    observations: string[] = [],
    onGrade?: (body: unknown) => void,
  ) => {
    await page.route('**/api/assessment/review/queue**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: queue.length, items: queue }),
      }),
    );

    await page.route('**/api/assessment/review/grade', route => {
      onGrade?.(route.request().postDataJSON());
      return route.fulfill({ status: 204, body: '' });
    });

    await page.route('**/api/assessment/review/attempts/*/integrity', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ attemptId: ATTEMPT, signals: [], observations }),
      }),
    );

    await page.route('**/api/assessment/review/attempts/*', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(answers) }),
    );
  };

  test('the queue shows how long each attempt has been waiting', async ({ page }) => {
    const old = new Date(Date.now() - 5 * 86_400_000).toISOString();

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [], [
      { attemptId: ATTEMPT, candidateName: 'Layla Hassan', examTitle: 'Spanish B1', submittedAt: old, pendingCount: 3, provisionalScore: 12, maxScore: 20, integrityFlagCount: 2 },
    ]);

    await gotoApp(page, '/review');

    // A queue without this hides its own backlog.
    await expect(page.getByText('5 day')).toBeVisible();
    await expect(page.getByRole('cell', { name: '3' })).toBeVisible();
  });

  test('the queue is empty when nothing is waiting, and says so', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [], []);

    await gotoApp(page, '/review');

    await expect(page.getByText('Nothing waiting')).toBeVisible();
  });

  test('a rubric adds itself up and is capped at the question marks', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [
      answer({
        maxScore: 10,
        rubric: [
          { id: 'r1', name: 'Identifies the cause', maxScore: 6 },
          { id: 'r2', name: 'Explains the consequence', maxScore: 4 },
        ],
      }),
    ]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    await page.getByLabel('Identifies the cause').fill('5');
    await page.getByLabel('Explains the consequence').fill('3');

    // Added here rather than by the reviewer: arithmetic under time pressure puts
    // mistakes into people's results, and a wrong total is invisible.
    await expect(page.locator('.rubric__total')).toContainText('8');

    // A slip into a criterion is not carried into a result.
    await page.getByLabel('Identifies the cause').fill('60');
    await expect(page.locator('.rubric__total')).toContainText('10');
  });

  test('the awarded total is what gets sent', async ({ page }) => {
    const graded: unknown[] = [];

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(
      page,
      [answer({ maxScore: 10, rubric: [{ id: 'r1', name: 'Reasoning', maxScore: 10 }] })],
      [],
      [],
      body => graded.push(body),
    );

    await gotoApp(page, `/review/${ATTEMPT}`);

    await page.getByLabel('Reasoning').fill('7');
    await page.getByLabel('Feedback for the candidate').fill('Good, but say why.');
    await page.getByRole('button', { name: 'Save this mark' }).click();

    await expect.poll(() => graded.length).toBe(1);
    expect((graded[0] as { awardedScore: number }).awardedScore).toBe(7);
    expect((graded[0] as { comment: string }).comment).toBe('Good, but say why.');
  });

  test('says the comment reaches the candidate', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [answer()]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    // A reviewer who thinks this is an internal note writes a different sentence.
    await expect(page.getByText('shown to them with their result')).toBeVisible();
  });

  test('observations are described rather than judged', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [answer({ wasPasted: true, timeSpentSeconds: 90 })], [], [
      'The window lost focus twice during this attempt.',
    ]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    await expect(page.getByText('The window lost focus twice during this attempt.')).toBeVisible();
    await expect(page.getByText('not what it means')).toBeVisible();

    // A fact about how the answer arrived, with no verdict attached.
    await expect(page.getByText('Pasted in')).toBeVisible();
  });

  test('an answer left blank says so rather than showing nothing', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [answer({ response: undefined })]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    await expect(page.getByText('Left blank')).toBeVisible();
  });

  test('does not scroll sideways on a phone in Arabic', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [answer({ rubric: [{ id: 'r1', name: 'السبب', maxScore: 5 }] })]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
