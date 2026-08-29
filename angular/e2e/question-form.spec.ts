import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubQuestions } from './support/question-stub';

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
});
