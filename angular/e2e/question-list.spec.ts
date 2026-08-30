import { Page, expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

const EXAM_ID = '11111111-1111-1111-1111-111111111111';
const LISTENING = '33333333-3333-3333-3333-333333333333';
const GRAMMAR = '44444444-4444-4444-4444-444444444444';

const section = (id: string, name: string, displayOrder: number) => ({
  id,
  examId: EXAM_ID,
  name,
  instructions: null,
  topicId: null,
  topicName: null,
  timeLimitInMinutes: null,
  minimumPercentage: null,
  questionsPerForm: null,
  isQualifying: false,
  displayOrder,
  questionCount: 0,
});

/** The exam's parts. Absent — the default here — means the column stays hidden. */
const stubSections = (page: Page, sections: unknown[]) =>
  page.route('**/api/assessment/exam-structure/sections/*', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sections) }),
  );

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

  // ── Which part of the paper each question is in ────────────────────────────

  test('names the part each question sits in, and says when it sits in none', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubList(page, [
      question({ id: 'heard', text: 'Heard on the recording', examSectionId: LISTENING }),
      question({ id: 'loose', text: 'Never filed anywhere', examSectionId: null }),
    ]);
    await stubSections(page, [section(LISTENING, 'Listening', 0), section(GRAMMAR, 'Grammar', 1)]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    await expect(page.getByRole('row', { name: /Heard on the recording/ })).toContainText('Listening');

    // Said, not left blank. On a sectioned paper an unfiled question is one no
    // part can draw, and across two hundred rows an empty cell reads as tidy.
    await expect(page.getByRole('row', { name: /Never filed anywhere/ })).toContainText('Unfiled');
  });

  test('leaves the part column out of an exam that was never split', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubList(page, [question()]);
    await stubSections(page, []);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    await expect(page.getByRole('columnheader', { name: 'Section' })).toHaveCount(0);
    await expect(page.getByLabel('Filter by section')).toHaveCount(0);
  });

  test('filtering by part asks the server for that part alone', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubList(page, [question({ examSectionId: LISTENING })]);
    await stubSections(page, [section(LISTENING, 'Listening', 0), section(GRAMMAR, 'Grammar', 1)]);

    // Registered last so it wins over the one stubList installed, and records
    // what the screen actually asked for.
    const asked: string[] = [];
    await page.route('**/api/assessment/questions?**', route => {
      asked.push(route.request().url());

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: 0, items: [] }),
      });
    });

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByLabel('Filter by section').selectOption(GRAMMAR);

    // Narrowed on the server, not in the browser: filtering a page of twenty
    // rows would show whichever of them happened to be drawn first.
    await expect.poll(() => asked.some(url => url.includes(`examSectionId=${GRAMMAR}`))).toBe(true);
  });

  test('does not scroll sideways on a phone in Arabic', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubList(page, [question({ examSectionId: LISTENING }), question({ id: 'q2', examId: null })]);

    // With the parts loaded, so the widest form of the table is the one measured.
    await stubSections(page, [section(LISTENING, 'الاستماع', 0), section(GRAMMAR, 'القواعد', 1)]);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
