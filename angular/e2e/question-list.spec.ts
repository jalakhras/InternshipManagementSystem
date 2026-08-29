import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

const EXAM_ID = '11111111-1111-1111-1111-111111111111';

/**
 * An exam's questions.
 *
 * What earns the tests here is not the table — it is the two things the list
 * says that the data alone does not: which half of the paper came from the
 * shared bank, and which questions have stopped measuring anything.
 */
test.describe('Question list', () => {
  const question = (over: Record<string, unknown> = {}) => ({
    id: 'q1',
    examId: EXAM_ID,
    type: 'single-choice',
    text: 'Which level is support?',
    payload: '{}',
    difficulty: 1,
    score: 2,
    displayOrder: 0,
    isActive: true,
    timesAnswered: 0,
    timesServed: 0,
    difficultyIndex: null,
    creationTime: '2026-01-01T00:00:00Z',
    ...over,
  });

  const stubList = async (page: import('@playwright/test').Page, items: unknown[]) => {
    await page.route('**/api/assessment/questions/types', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { type: 'single-choice', nameKey: '::QuestionType:single-choice', descriptionKey: '', icon: 'bi-ui-radios', isAutoGraded: true, hasOptions: true, acceptsUpload: false },
        ]),
      }),
    );

    await page.route('**/api/assessment/exams/*', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: EXAM_ID, title: 'Spanish B1 Placement', status: 0, mode: 0, timeLimitInMinutes: 30, passingPercentage: 60 }),
      }),
    );

    await page.route('**/api/assessment/questions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: items.length, items }),
      }),
    );
  };

  test('names the exam and lists what it can draw', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubList(page, [question(), question({ id: 'q2', text: 'Which is resistance?' })]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    await expect(page.getByRole('heading', { name: 'Spanish B1 Placement' })).toBeVisible();
    await expect(page.getByRole('row')).toHaveCount(3);
  });

  test('marks which questions came from the shared bank', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubList(page, [
      question({ id: 'own', text: 'Written into this exam' }),
      question({ id: 'bank', examId: null, categoryId: 'c1', text: 'Shared across the level' }),
    ]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    // An author who cannot see the bank half will write it again.
    const bankRow = page.getByRole('row', { name: /Shared across the level/ });
    await expect(bankRow.getByText('From the bank')).toBeVisible();

    await expect(page.getByRole('row', { name: /Written into this exam/ }).getByText('From the bank'))
      .toHaveCount(0);
  });

  test('flags a question almost nobody answers correctly', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubList(page, [
      question({ id: 'broken', text: 'Probably has a wrong key', timesAnswered: 120, difficultyIndex: 0.04 }),
      question({ id: 'fine', text: 'Measuring properly', timesAnswered: 120, difficultyIndex: 0.62 }),
      question({ id: 'new', text: 'Not enough answers yet', timesAnswered: 3, difficultyIndex: 0 }),
    ]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    // The one worth catching: it reads as a hard question until somebody looks.
    await expect(page.getByRole('row', { name: /wrong key/ }).getByText('Almost everyone wrong')).toBeVisible();
    await expect(page.getByRole('row', { name: /Measuring properly/ })
      .getByText('Measuring', { exact: true })).toBeVisible();

    // Three answers is not evidence of anything, so it claims nothing.
    await expect(page.getByRole('row', { name: /Not enough answers/ }).getByText('Too few answers yet')).toBeVisible();
  });

  test('says a bank question leaves every exam at the level, not just this one', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubList(page, [question({ id: 'bank', examId: null, text: 'Shared question' })]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByRole('button', { name: 'Delete' }).click();

    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toContainText('every exam at this level');
  });

  test('hides editing from someone who may only read', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      // Exams.View as well: the shell needs one navigable section to render, and
      // this test is about the row actions rather than about reaching the page.
      grantedPolicies: [
        'Assessment.Exams',
        'Assessment.Exams.View',
        'Assessment.Questions',
        'Assessment.Questions.View',
      ],
    });
    await stubList(page, [question()]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    await expect(page.getByRole('button', { name: 'Delete' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Edit' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Add question' })).toHaveCount(0);
  });

  test('does not scroll sideways on a phone in Arabic', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubList(page, [question(), question({ id: 'q2', examId: null })]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
