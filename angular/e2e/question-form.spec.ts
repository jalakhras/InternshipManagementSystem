import { Page, expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubQuestions } from './support/question-stub';

const EXAM_ID = '11111111-1111-1111-1111-111111111111';
const QUESTION_ID = '22222222-2222-2222-2222-222222222222';
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

/** The exam's parts, which decide whether the filing picker appears at all. */
const stubSections = (page: Page, sections: unknown[], status = 200) =>
  page.route('**/api/assessment/exam-structure/sections/*', route =>
    route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(sections) }),
  );

/**
 * One stored question, and whatever the form sends back for it.
 *
 * Registered before stubQuestions on purpose: Playwright matches routes
 * last-registered-first, and this pattern would otherwise swallow the type
 * catalogue that stubQuestions serves from the same path.
 */
async function stubOneQuestion(page: Page, stored: Record<string, unknown>): Promise<unknown[]> {
  const sent: unknown[] = [];

  await page.route(`**/api/assessment/questions/${QUESTION_ID}`, route => {
    if (route.request().method() === 'PUT') {
      sent.push(route.request().postDataJSON());
    }

    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(stored) });
  });

  return sent;
}

const storedQuestion = (over: Record<string, unknown> = {}) => ({
  id: QUESTION_ID,
  examId: EXAM_ID,
  categoryId: null,
  levelId: null,
  examSectionId: null,
  text: 'Whcih level is support?',
  type: 'single-choice',
  payload: JSON.stringify({
    options: [
      { id: 'a', text: 'Support', isCorrect: true },
      { id: 'b', text: 'Resistance', isCorrect: false },
    ],
  }),
  difficulty: 1,
  score: 2,
  displayOrder: 0,
  isActive: true,
  timesAnswered: 0,
  ...over,
});

/**
 * Writing a question.
 *
 * The behaviour under test is the thing that makes thirteen types tolerable: one
 * frame, one changing slot, and warnings that arrive while typing rather than at
 * save time.
 */
test.describe('Question builder', () => {
  test('opens on the type picker, saying which types a person must mark', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await expect(page.getByRole('button', { name: /Single choice/ })).toBeVisible();

    // Whether a machine or a person marks it decides the work this question
    // creates later, so it is said at the moment of choosing rather than
    // discovered when the review queue fills up.
    const written = page.getByRole('button', { name: /Written answer/ });
    await expect(written).toContainText('Marked by a person');

    const single = page.getByRole('button', { name: /Single choice/ });
    await expect(single).toContainText('Marked automatically');
  });

  test('choosing a type swaps only the answer slot', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Single choice/ }).click();

    // The frame is the same for every type: prompt, marks, difficulty, timer,
    // explanation. Only the middle changes.
    await expect(page.getByLabel('Question text')).toBeVisible();
    await expect(page.getByRole('spinbutton', { name: 'Marks' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Medium' })).toBeVisible();

    // And the slot now holds the choice editor.
    await expect(page.getByRole('button', { name: 'Add option' })).toBeVisible();
  });

  test('warns while typing that no option is marked correct', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Single choice/ }).click();

    // The server refuses this too, but discovering it after writing four options
    // is a worse experience than seeing it while typing.
    await expect(page.getByText('No correct option is marked')).toBeVisible();
  });

  test('marking a correct option clears the others on a single-choice question', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Single choice/ }).click();

    const marks = page.getByLabel('Mark as correct');
    await marks.first().check();
    await marks.last().check();

    // Two correct options on a single-choice question means nobody can pass it.
    // The editor makes that state unreachable rather than warning about it.
    await expect(marks.first()).not.toBeChecked();
    await expect(marks.last()).toBeChecked();
    await expect(page.getByText('No correct option is marked')).toHaveCount(0);
  });

  test('multi-select warns when every option is correct', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Multiple answers/ }).click();

    const marks = page.getByLabel('Mark as correct');
    await marks.first().check();
    await marks.last().check();

    // Selecting everything would be right, so the question measures nothing —
    // and it is the exact shape the old scoring bug rewarded with full marks.
    await expect(page.getByText('Every option is correct')).toBeVisible();
  });

  test('the numeric editor shows the range it accepts', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Numeric answer/ }).click();

    await page.getByLabel('Correct value').fill('1250');
    await page.getByLabel('Tolerance').fill('0.5');

    // The author sees what they built rather than computing it from two fields.
    await expect(page.getByText('1249.5 — 1250.5')).toBeVisible();
  });

  test('the rubric flags a total that does not match the question marks', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Written answer/ }).click();

    await page.getByRole('spinbutton', { name: 'Marks' }).fill('10');
    await page.getByRole('button', { name: 'Add criterion' }).click();

    // Not an error — a reviewer can still award within the total — but it usually
    // means a criterion was added and the marks not adjusted, which is far cheaper
    // to notice here than in the review queue.
    await expect(page.getByText('does not add up to the question', { exact: false })).toBeVisible();
  });

  test('does not scroll sideways on a phone', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });

  test('scoring by degree of correctness is offered, and seeds itself sensibly', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Single choice/ }).click();

    const marks = page.getByLabel('Mark as correct');
    await marks.first().check();

    await page.getByText('Score by degree of correctness').click();

    // Seeded from what the author already said: the option marked correct becomes
    // the best answer. Turning it on to four validation warnings would be worse
    // than not offering it.
    await expect(page.getByText('Best answer')).toBeVisible();
    await expect(page.getByText('Not credited')).toBeVisible();
  });

  test('an option priced between zero and one reads as acceptable', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Single choice/ }).click();
    await page.getByLabel('Mark as correct').first().check();
    await page.getByText('Score by degree of correctness').click();

    // Priced in the question's own marks: three of five, not "0.6". The share is
    // what gets stored, so raising the question to ten later keeps this answer at
    // sixty per cent of it rather than freezing it at three.
    await page.getByRole('spinbutton', { name: 'Marks' }).fill('5');
    await page.getByLabel('Weight 2').fill('3');

    await expect(page.getByText('Acceptable')).toBeVisible();
  });

  test('a weighted multi-select warns until something is priced below zero', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    await page.getByRole('button', { name: /Multiple answers/ }).click();
    await page.getByLabel('Mark as correct').first().check();
    await page.getByText('Score by degree of correctness').click();

    // Seeded so the toggle does not open onto a page of warnings: the correct
    // options divide the question between them and one wrong option is priced
    // below zero, because otherwise ticking every box scores full marks.
    await expect(page.getByText('Penalised')).toBeVisible();

    // Raising the penalty to zero brings the total back to the whole question,
    // and selecting everything would score full marks again.
    await page.getByLabel('Weight 2').fill('0');

    await expect(page.getByText('Selecting every option would score full marks')).toBeVisible();
  });

  test('a question can carry a chart, a recording or a clip', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);

    await page.route('**/api/assessment/media', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          blobName: 'tenant/abc.png',
          originalFileName: 'eurusd-4h.png',
          mediaType: 'image',
          sizeInBytes: 1234,
        }),
      }),
    );

    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');
    await page.getByRole('button', { name: /Single choice/ }).click();

    // One control, and no URL to paste. The exam that prompted this was written
    // by a trading coach, not a developer.
    await expect(page.getByText('Choose a file, or drop one here')).toBeVisible();

    await page.setInputFiles('input[type="file"]', {
      name: 'eurusd-4h.png',
      mimeType: 'image/png',
      buffer: Buffer.from(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
        'base64',
      ),
    });

    // The server decides what kind of thing it is, and the form shows it rather
    // than naming a file the author has to take on trust.
    await expect(page.locator('.preview__image')).toBeVisible();
    await expect(page.getByText('eurusd-4h.png')).toBeVisible();
  });

  test('an attached file can be taken off again', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);

    await page.route('**/api/assessment/media', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ blobName: 'tenant/abc.png', originalFileName: 'chart.png', mediaType: 'image', sizeInBytes: 1 }),
      }),
    );

    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');
    await page.getByRole('button', { name: /Single choice/ }).click();

    await page.setInputFiles('input[type="file"]', {
      name: 'chart.png',
      mimeType: 'image/png',
      buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64'),
    });

    await expect(page.locator('.preview__image')).toBeVisible();

    await page.getByRole('button', { name: 'Remove', exact: true }).click();

    // Back to the empty control, not to a broken image.
    await expect(page.getByText('Choose a file, or drop one here')).toBeVisible();
  });

  // ── Filing into a part of the paper ──────────────────────────────────────

  test('offers the parts of the paper once the exam has been split', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubSections(page, [section(LISTENING, 'Listening', 0), section(GRAMMAR, 'Grammar', 1)]);
    await stubQuestions(page);

    await gotoApp(page, `/exams/${EXAM_ID}/questions/new`);
    await page.getByRole('button', { name: /Single choice/ }).click();

    const picker = page.getByLabel('Section');

    // Unfiled to start with. A question is filed deliberately or not at all —
    // defaulting to the first part would put grammar questions in listening for
    // every author who did not notice the control.
    await expect(picker).toHaveValue('');
    await expect(picker.getByRole('option')).toHaveText(['Unfiled', 'Listening', 'Grammar']);
  });

  test('does not offer the picker on an exam that was never split', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubSections(page, []);
    await stubQuestions(page);

    await gotoApp(page, `/exams/${EXAM_ID}/questions/new`);
    await page.getByRole('button', { name: /Single choice/ }).click();

    // One part means nowhere to file anything, and a control with a single
    // option is a question the author has to answer for no reason.
    await expect(page.getByLabel('Section')).toHaveCount(0);
  });

  test('fixing a typo does not unfile the question from its part of the paper', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubSections(page, [section(LISTENING, 'Listening', 0), section(GRAMMAR, 'Grammar', 1)]);
    const sent = await stubOneQuestion(page, storedQuestion({ examSectionId: LISTENING }));
    await stubQuestions(page);

    await gotoApp(page, `/exams/${EXAM_ID}/questions/${QUESTION_ID}`);

    // Opens showing where the question already lives, rather than on "Unfiled" —
    // which would have been the author's only clue that saving was about to
    // move it.
    await expect(page.getByLabel('Section')).toHaveValue(LISTENING);

    await page.getByRole('spinbutton', { name: 'Marks' }).fill('3');
    await page.getByRole('button', { name: 'Save' }).click();

    // The server assigns the section from whatever the body carries, so a form
    // that omits it does not leave the section alone — it clears it, and the
    // listening part quietly loses a question to somebody correcting a spelling.
    await expect.poll(() => sent.length).toBe(1);
    expect(sent[0]).toMatchObject({ examSectionId: LISTENING, score: 3 });
  });

  test('keeps the question filed even when the parts cannot be loaded', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubSections(page, [], 500);
    const sent = await stubOneQuestion(page, storedQuestion({ examSectionId: GRAMMAR }));
    await stubQuestions(page);

    await gotoApp(page, `/exams/${EXAM_ID}/questions/${QUESTION_ID}`);

    await page.getByRole('spinbutton', { name: 'Marks' }).fill('4');
    await page.getByRole('button', { name: 'Save' }).click();

    // The picker is gone, because nothing can name the parts. Losing the picker
    // is a degraded screen; losing the filing would be data destroyed by an
    // unrelated outage.
    await expect(page.getByLabel('Section')).toHaveCount(0);
    await expect.poll(() => sent.length).toBe(1);
    expect(sent[0]).toMatchObject({ examSectionId: GRAMMAR });
  });

  test('moving a question to another part sends the new one', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubSections(page, [section(LISTENING, 'Listening', 0), section(GRAMMAR, 'Grammar', 1)]);
    const sent = await stubOneQuestion(page, storedQuestion({ examSectionId: LISTENING }));
    await stubQuestions(page);

    await gotoApp(page, `/exams/${EXAM_ID}/questions/${QUESTION_ID}`);

    await page.getByLabel('Section').selectOption(GRAMMAR);
    await page.getByRole('button', { name: 'Save' }).click();

    await expect.poll(() => sent.length).toBe(1);
    expect(sent[0]).toMatchObject({ examSectionId: GRAMMAR });
  });

  test('a bank author is told why there is no part to choose', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);

    await gotoApp(page, '/questions/new');

    // A bank question is filed into no one part, and until now the author saw
    // no field and no reason for its absence — which looks exactly like a
    // question somebody forgot to file. The sentence is also the honest
    // description of what the paper actually does: a section fills itself from
    // what it measures, so the topic is the thing to set.
    // The type is chosen first; the form only exists once it is.
    await page.getByRole('button', { name: /Single choice/ }).click();

    await expect(page.getByText(/serves every exam at its level/)).toBeVisible();
  });
});
