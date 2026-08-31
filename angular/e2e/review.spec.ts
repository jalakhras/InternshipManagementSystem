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
    finishedQueue?: unknown[],
  ) => {
    // The stub honours ?Finished= rather than answering both tabs with the same
    // rows. A stub that ignored it would report the two tabs as working whichever
    // way the screen behaved, which is the defect this file now covers.
    await page.route('**/api/assessment/review/queue**', route => {
      const wants = new URL(route.request().url()).searchParams.get('finished') === 'true';
      const rows = wants ? (finishedQueue ?? []) : queue;

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: rows.length, items: rows }),
      });
    });

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

  test('the marker is given the model answer, not left to estimate', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [
      answer({
        // Deliberately not a substring of the candidate's own words, so the
        // assertion proves the key is on screen rather than matching their
        // answer by accident.
        correctAnswer: 'Falling participation into resistance',
        reviewerGuidance: 'Full marks only if they mention volume.',
        explanation: 'Reversals on falling volume are the standard case.',
      }),
    ]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    // The server has always sent all three; the screen rendered only the
    // guidance. So somebody marking a free-text answer had the answer and
    // nothing to measure it against, and was estimating a mark that decides
    // whether a candidate passed.
    await expect(page.getByText('Falling participation into resistance')).toBeVisible();
    await expect(page.getByText('Full marks only if they mention volume.')).toBeVisible();
    await expect(page.getByText('Reversals on falling volume are the standard case.')).toBeVisible();
  });

  test('a question with no key shows the marker no empty headings', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [answer()]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    // An essay has no model answer, and a "Model answer:" label with nothing
    // after it reads as one that failed to load.
    await expect(page.getByText('Model answer:')).toHaveCount(0);
    await expect(page.getByText('Explanation:')).toHaveCount(0);
  });

  // ----------------------------------------------------------- the marked tab
  //
  // `1c2a5fd` gave the queue a second tab so a marker who typed 7 meaning 17 had
  // a route back to that sitting, and the screen behind every row of it was
  // blank: GetAnswersAsync filtered on NeedsManualReview, which grading clears.
  // The server half is covered by RemarkTests; these are the screen's half.

  const marked = (over: Record<string, unknown> = {}) =>
    answer({
      awardedScore: 7,
      reviewComment: 'Meant seventeen.',
      reviewedAt: '2026-08-30T10:15:00',
      ...over,
    });

  test('a sitting already marked says so, and offers to replace the mark', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [marked()]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    // Reached from the marked tab, an already-judged card looked identical to an
    // unmarked one but for a green edge — so a marker could not tell whether the
    // number in the box was their earlier judgement or a default.
    await expect(page.getByText('Marked on', { exact: false })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Replace this mark' })).toBeVisible();
    await expect(page.getByText('Every answer on this attempt has been marked.')).toBeVisible();
  });

  test('a marked rubric reopens with the marks that produced the total', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [
      marked({
        maxScore: 10,
        awardedScore: 8,
        rubric: [
          { id: 'r1', name: 'Identifies the cause', maxScore: 6 },
          { id: 'r2', name: 'Explains the consequence', maxScore: 4 },
        ],
        rubricScores: { r1: 5, r2: 3 },
      }),
    ]);

    await gotoApp(page, `/review/${ATTEMPT}`);

    // Seeded from what was awarded. Left at zero, the screen showed a considered
    // eight as an empty rubric and the next save would have replaced it with
    // nothing — the one journey this tab exists for, quietly destroying the mark
    // it was opened to correct.
    await expect(page.getByLabel('Identifies the cause')).toHaveValue('5');
    await expect(page.getByLabel('Explains the consequence')).toHaveValue('3');
    await expect(page.locator('.rubric__total')).toContainText('8');
  });

  test('the marked tab asks the server for marked sittings and counts them', async ({ page }) => {
    const asked: string[] = [];

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(
      page,
      [],
      [],
      [],
      undefined,
      [
        {
          attemptId: ATTEMPT,
          candidateName: 'Layla Hassan',
          examTitle: 'Spanish B1',
          submittedAt: '2026-08-30T09:00:00Z',
          pendingCount: 0,
          markedCount: 3,
          provisionalScore: 18,
          maxScore: 20,
          integrityFlagCount: 0,
        },
      ],
    );

    page.on('request', request => {
      if (request.url().includes('/review/queue')) {
        asked.push(request.url());
      }
    });

    await gotoApp(page, '/review');
    await page.getByRole('button', { name: 'Already marked' }).click();

    await expect.poll(() => asked.some(url => url.includes('finished=true'))).toBe(true);

    // "To mark" is zero on every row of this tab by definition, so the column
    // that means anything here is how much of the sitting a person judged.
    await expect(page.getByRole('columnheader', { name: 'Marked' })).toBeVisible();

    // By data-label rather than by cell role: at phone width the table restacks
    // into labelled rows and the accessible name of the cell picks up the label.
    await expect(page.locator('td[data-label="Marked"]')).toContainText('3');
    await expect(page.getByRole('link', { name: 'Review the mark' })).toBeVisible();
  });

  test('an empty marked tab does not claim everything has been marked', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubReview(page, [], [], [], undefined, []);

    await gotoApp(page, '/review');
    await page.getByRole('button', { name: 'Already marked' }).click();

    // "Every submitted attempt has been marked" is true of an empty waiting
    // queue and the opposite of the truth on this one.
    await expect(page.getByText('Nothing has been marked yet')).toBeVisible();
    await expect(page.getByText('Every submitted attempt has been marked.')).toHaveCount(0);
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
