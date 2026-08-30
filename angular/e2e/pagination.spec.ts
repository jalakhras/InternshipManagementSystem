import { Page, expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * Where you are in a list, and whether the list will tell you.
 *
 * The owner reported "لا يوجد pagination" while six screens had a working pager,
 * and both halves of that were true. Every one of those pagers was wrapped in
 * `@if (totalPages() > 1)`, so on a tenant of nine people the product never once
 * said how much there was; and four screens fetched a hundred or five hundred
 * rows and rendered every one of them with nothing to turn.
 *
 * So these tests are mostly about the sentence rather than the arrows. A list
 * that shows twenty of a hundred and forty-eight rows and says nothing looks
 * exactly like a list of twenty, and that is the defect the owner met.
 */
test.describe('Paging', () => {
  const EXAM = 'eeeeeeee-1111-1111-1111-111111111111';

  const resultRow = (n: number) => ({
    attemptId: `aaaaaaaa-0000-0000-0000-${String(n).padStart(12, '0')}`,
    candidateId: `cccccccc-0000-0000-0000-${String(n).padStart(12, '0')}`,
    candidateName: `Sitter ${n}`,
    candidateEmail: `sitter${n}@example.test`,
    examId: EXAM,
    examTitle: 'Placement',
    formName: null,
    startedAt: new Date(Date.now() - 600_000).toISOString(),
    submittedAt: new Date().toISOString(),
    isSubmitted: true,
    isGraded: true,
    needsManualReview: false,
    score: 7,
    maxScore: 10,
    scorePercentage: 70,
    isPassed: true,
    endReason: 'submitted',
    integrityFlagCount: 0,
    durationInMinutes: 30,
  });

  const page_of = (total: number, skip: number, take: number) => ({
    totalCount: total,
    items: Array.from({ length: Math.max(0, Math.min(take, total - skip)) }, (_, i) =>
      resultRow(skip + i + 1),
    ),
  });

  /** The exam picker every results screen fills its filter from. */
  const stubExams = (page: Page) =>
    page.route('**/api/assessment/exams**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCount: 1,
          items: [{ id: EXAM, title: 'Placement', status: 1, questionCount: 3 }],
        }),
      }),
    );

  /**
   * Every query string the app sent to a path, in order.
   *
   * Registered *after* the handler that answers, because Playwright matches
   * routes last-registered-first: this one sees the request, writes it down and
   * falls through to the stub underneath.
   */
  const record = async (page: Page, glob: string, sink: URLSearchParams[]) => {
    await page.route(glob, route => {
      sink.push(new URL(route.request().url()).searchParams);
      return route.fallback();
    });
  };

  // ------------------------------------------------------------------ results

  const stubResults = async (page: Page, total: number, take = 25) => {
    await stubExams(page);

    await page.route('**/api/assessment/candidates/groups**', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
    );

    await page.route('**/api/assessment/results/summary**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          sat: total,
          notStarted: 0,
          passed: total,
          failed: 0,
          awaitingMarking: 0,
          averagePercentage: 70,
          medianPercentage: 70,
        }),
      }),
    );

    await page.route('**/api/assessment/results?**', route => {
      const q = new URL(route.request().url()).searchParams;
      const skip = Number(q.get('skipCount') ?? 0);

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(page_of(total, skip, take)),
      });
    });
  };

  test('a list says how much there is, even when it all fits on one page', async ({ page }) => {
    // The owner's tenant, and the whole report. Nine sittings paged nowhere and
    // therefore said nothing at all, which reads as a list of nine that might be
    // a list of nine hundred.
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubResults(page, 9);

    await gotoApp(page, '/results');

    await expect(page.getByText('1–9 of 9')).toBeVisible();

    // No arrows, because there is nowhere to go. The sentence is the point.
    await expect(page.getByRole('button', { name: 'Next' })).toHaveCount(0);
  });

  test('turning a page moves the range and asks the server for it', async ({ page }) => {
    const asked: URLSearchParams[] = [];

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubResults(page, 53);
    await record(page, '**/api/assessment/results?**', asked);

    await gotoApp(page, '/results');

    await expect(page.getByText('1–25 of 53')).toBeVisible();

    await page.getByRole('button', { name: 'Next' }).click();

    await expect(page.getByText('26–50 of 53')).toBeVisible();
    expect(asked.at(-1)?.get('skipCount')).toBe('25');

    await page.getByRole('button', { name: 'Next' }).click();

    // The last page is short, and saying "51–75" would be a lie about a list the
    // reader cannot check.
    await expect(page.getByText('51–53 of 53')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  test('the pager is a named landmark and its buttons take a keyboard', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubResults(page, 53);

    await gotoApp(page, '/results');

    // Named, so a screen-reader user landing on it knows what it is. It used to
    // be labelled "Results", which describes the page rather than the control.
    const pager = page.getByRole('navigation', { name: 'Pagination' });
    await expect(pager).toBeVisible();

    const next = pager.getByRole('button', { name: 'Next' });

    await next.focus();
    await expect(next).toBeFocused();
    await page.keyboard.press('Enter');

    await expect(page.getByText('26–50 of 53')).toBeVisible();

    // Reachable by thumb as well as by pointer.
    const box = await next.boundingBox();
    expect(box!.height).toBeGreaterThanOrEqual(44);
    expect(box!.width).toBeGreaterThanOrEqual(44);
  });

  test('a pager fits the page it is on, at 390px and in Arabic', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });

    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubResults(page, 53);

    await gotoApp(page, '/results');

    const pager = page.locator('astro-pager nav');
    await expect(pager).toBeVisible();

    // A pager that renders outside the viewport is indistinguishable from one
    // that does not render, and this codebase has shipped that before.
    const box = (await pager.boundingBox())!;
    expect(box.x).toBeGreaterThanOrEqual(0);
    expect(box.x + box.width).toBeLessThanOrEqual(390);

    const scrollWidth = await page.evaluate(() => document.body.scrollWidth);
    expect(scrollWidth).toBeLessThanOrEqual(390);

    // Back points backwards. The icon is named chevron-left and mirrored by
    // [dir=rtl] .astro-flip, so it renders pointing right in Arabic — the five
    // inline pagers this component replaces name the other icon and flip that,
    // which leaves their Previous pointing forwards in both languages.
    const back = pager.getByRole('button', { name: 'السابق' }).locator('i');
    await expect(back).toHaveClass(/bi-chevron-left/);
    await expect(back).toHaveCSS('transform', 'matrix(-1, 0, 0, 1, 0, 0)');
  });

  // ----------------------------------------------------------- item analysis

  test('the item analysis pages a bank instead of pouring it onto the screen', async ({ page }) => {
    const rows = Array.from({ length: 60 }, (_, i) => ({
      questionId: `qqqqqqqq-0000-0000-0000-${String(i + 1).padStart(12, '0')}`,
      text: `Question number ${i + 1}`,
      type: 'single',
      topicName: null,
      timesAnswered: 40,
      facility: 0.5,
      discrimination: 0.2,
      flagKey: null,
    }));

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);

    await page.route('**/api/assessment/results/item-analysis/**', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(rows) }),
    );

    await gotoApp(page, '/results/questions');
    await page.getByRole('combobox').selectOption(EXAM);

    await expect(page.getByText('1–25 of 60')).toBeVisible();
    await expect(page.locator('table tbody tr')).toHaveCount(25);
    await expect(page.getByText('Question number 26')).toHaveCount(0);

    await page.getByRole('button', { name: 'Next' }).click();

    await expect(page.getByText('26–50 of 60')).toBeVisible();
    await expect(page.getByText('Question number 26')).toBeVisible();
  });

  // ---------------------------------------------------------- attempt monitor

  test('the monitor asks for one page of the room, not a hundred people', async ({ page }) => {
    const asked: URLSearchParams[] = [];

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExams(page);

    await page.route('**/api/assessment/attempts/running**', route => {
      const q = new URL(route.request().url()).searchParams;
      const skip = Number(q.get('skipCount') ?? 0);

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(page_of(60, skip, 25)),
      });
    });

    await record(page, '**/api/assessment/attempts/running**', asked);

    await gotoApp(page, '/results/running');

    // Sixty people are sitting; the screen used to show a hundred or nothing and
    // never said which.
    await expect(page.getByText('1–25 of 60')).toBeVisible();
    expect(asked[0].get('maxResultCount')).toBe('25');

    await page.getByRole('button', { name: 'Next' }).click();

    await expect(page.getByText('26–50 of 60')).toBeVisible();
    expect(asked.at(-1)?.get('skipCount')).toBe('25');
  });

  // -------------------------------------------------------------- exam forms

  test('the paper keeps a question chosen on one page of the bank', async ({ page }) => {
    // The regression a paged pool invites: the paper is held as ids and rendered
    // by resolving them, so turning the bank's page used to be enough to make a
    // chosen question disappear from the paper — and then from the save.
    const asked: URLSearchParams[] = [];

    const question = (n: number) => ({
      id: `qqqqqqqq-0000-0000-0000-${String(n).padStart(12, '0')}`,
      examId: EXAM,
      text: `Bank question ${n}`,
      type: 'single',
      difficulty: 2,
      score: 1,
    });

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    await page.route('**/api/assessment/exams/*', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: EXAM, title: 'Placement', status: 0, questionCount: 40 }),
      }),
    );

    await page.route('**/api/assessment/exam-structure/forms/by-exam/**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'ffffffff-1111-1111-1111-111111111111', examId: EXAM, name: 'Form 1', code: 'F1', status: 0, questionCount: 0, maxScore: 0, timesUsed: 0 },
        ]),
      }),
    );

    await page.route('**/api/assessment/exam-structure/forms/ffffffff*', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ffffffff-1111-1111-1111-111111111111',
          examId: EXAM,
          name: 'Form 1',
          code: 'F1',
          status: 0,
          questionCount: 0,
          maxScore: 0,
          timesUsed: 0,
          questions: [],
        }),
      }),
    );

    await page.route('**/api/assessment/questions?**', route => {
      const q = new URL(route.request().url()).searchParams;
      const skip = Number(q.get('skipCount') ?? 0);
      const take = Number(q.get('maxResultCount') ?? 15);

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCount: 40,
          items: Array.from({ length: Math.min(take, 40 - skip) }, (_, i) => question(skip + i + 1)),
        }),
      });
    });

    await record(page, '**/api/assessment/questions?**', asked);

    await gotoApp(page, `/exams/${EXAM}/forms`);

    await page.getByRole('button', { name: 'Open' }).click();

    await expect(page.getByText('1–15 of 40')).toBeVisible();

    // Asked for a page, not for the bank.
    expect(asked[0].get('maxResultCount')).toBe('15');

    await page.getByRole('checkbox').first().check();
    await expect(page.getByRole('list', { name: /.*/ }).first()).toBeVisible();

    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('16–30 of 40')).toBeVisible();

    // Still on the paper, two pages away from the row that put it there.
    await expect(page.locator('.chosen__text')).toHaveText('Bank question 1');
  });

  test('the bank is searched on the server rather than in the page', async ({ page }) => {
    const asked: URLSearchParams[] = [];

    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    await page.route('**/api/assessment/exams/*', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: EXAM, title: 'Placement', status: 0, questionCount: 40 }),
      }),
    );

    await page.route('**/api/assessment/exam-structure/forms/by-exam/**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'ffffffff-1111-1111-1111-111111111111', examId: EXAM, name: 'Form 1', code: 'F1', status: 0, questionCount: 0, maxScore: 0, timesUsed: 0 },
        ]),
      }),
    );

    await page.route('**/api/assessment/exam-structure/forms/ffffffff*', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ffffffff-1111-1111-1111-111111111111',
          examId: EXAM,
          name: 'Form 1',
          code: 'F1',
          status: 0,
          questionCount: 0,
          maxScore: 0,
          timesUsed: 0,
          questions: [],
        }),
      }),
    );

    await page.route('**/api/assessment/questions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: 0, items: [] }),
      }),
    );

    await record(page, '**/api/assessment/questions?**', asked);

    await gotoApp(page, `/exams/${EXAM}/forms`);
    await page.getByRole('button', { name: 'Open' }).click();

    await page.getByRole('searchbox', { name: 'Search the available questions' }).fill('reversal');
    await page.getByRole('searchbox', { name: 'Search the available questions' }).press('Enter');

    // The whole point: the five hundred and first question was unreachable while
    // this was a filter over whatever the dialog happened to be holding.
    await expect.poll(() => asked.at(-1)?.get('filter')).toBe('reversal');
    await expect(page.getByText('No question matches that search.')).toBeVisible();
  });
});
