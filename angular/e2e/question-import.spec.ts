import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

const EXAM_ID = '11111111-1111-1111-1111-111111111111';

/**
 * Bringing a question bank in from a spreadsheet.
 *
 * The reason the screen has this at all: an author's questions are already in a
 * file, and retyping eighty of them with four options each is why authoring
 * stops on the first evening.
 *
 * What earns tests here is not the upload. It is that nothing is written until
 * somebody has seen what the file said — the preview names the answer it read
 * out of their columns, and a bad row names the row number *and* the column, so
 * the fix is one cell rather than nine.
 */
test.describe('Question import', () => {
  const SHEET =
    'Type,Question,Option 1,Option 2,Option 3,Option 4,Correct answer,Marks,Difficulty,Explanation\n' +
    'single choice,What is the capital of Egypt?,Cairo,Alexandria,Aswan,Tanta,1,1,Easy,\n' +
    'single choice,The key names nothing,A,B,C,D,Zebra,1,,\n';

  interface ImportCall {
    dryRun?: boolean;
    content?: string;
    examId?: string;
  }

  const stubImport = async (
    page: import('@playwright/test').Page,
    calls: ImportCall[],
    onImport?: (body: ImportCall) => unknown,
  ) => {
    await page.route('**/api/assessment/questions/types', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            type: 'single-choice',
            nameKey: '::QuestionType:single-choice',
            descriptionKey: '',
            icon: 'bi-ui-radios',
            isAutoGraded: true,
            hasOptions: true,
            acceptsUpload: false,
          },
        ]),
      }),
    );

    await page.route('**/api/assessment/exams/*', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: EXAM_ID,
          title: 'Egypt, generally',
          status: 0,
          mode: 0,
          timeLimitInMinutes: 30,
          passingPercentage: 60,
        }),
      }),
    );

    // Registered before the list route: Playwright matches last-registered-first,
    // and `**/questions?**` would otherwise never be reached.
    await page.route('**/api/assessment/questions/import/template', route =>
      route.fulfill({
        status: 200,
        contentType: 'text/csv; charset=utf-8',
        headers: { 'content-disposition': 'attachment; filename=questions-template.csv' },
        body: '﻿Type,Question\nsingle choice,What is the capital of Egypt?\n',
      }),
    );

    await page.route('**/api/assessment/questions/import', route => {
      const body = route.request().postDataJSON() as ImportCall;

      calls.push(body);

      const result = onImport
        ? onImport(body)
        : { created: 0, alreadyPresent: 0, preview: [], problems: [] };

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(result),
      });
    });

    await page.route('**/api/assessment/questions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: 0, items: [] }),
      }),
    );
  };

  /** The result a two-row sheet with one wrong row would come back with. */
  const readSheet = () => ({
    created: 1,
    alreadyPresent: 0,
    preview: [
      {
        line: 2,
        text: 'What is the capital of Egypt?',
        type: 'single-choice',
        score: 1,
        difficulty: 0,
        options: ['Cairo', 'Alexandria', 'Aswan', 'Tanta'],
        correctAnswers: ['Cairo'],
      },
    ],
    problems: [
      {
        line: 3,
        column: 'QuestionImport:Column:Correct',
        reason: 'IMS:QuestionImport:AnswerIsNotOneOfTheOptions',
        content: 'single choice | The key names nothing | A | B | C | D | Zebra | 1',
      },
    ],
  });

  const chooseSheet = async (page: import('@playwright/test').Page) => {
    await page.getByLabel('The spreadsheet').setInputFiles({
      name: 'bank.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from(SHEET, 'utf8'),
    });
  };

  test('the empty bank offers the spreadsheet rather than a blank form', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubImport(page, []);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    // Somebody looking at an empty bank wants to get their file in, not to type
    // the first of eighty questions.
    await expect(page.getByText('No questions yet')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Import from a spreadsheet' })).toHaveCount(2);
  });

  test('checks the file and reports every row before writing anything', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    const calls: ImportCall[] = [];
    await stubImport(page, calls, readSheet);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByRole('button', { name: 'Import from a spreadsheet' }).first().click();

    await chooseSheet(page);
    await page.getByRole('button', { name: 'Check the file' }).click();

    // Nothing written yet: the first call is a dry run, and it carries the file.
    await expect.poll(() => calls.length).toBe(1);
    expect(calls[0].dryRun).toBe(true);
    expect(calls[0].examId).toBe(EXAM_ID);
    expect(calls[0].content?.length).toBeGreaterThan(0);

    // The preview names the answer this read out of their columns. The mistake
    // worth catching is a key one row off, and a number looks exactly as right
    // when it is wrong.
    await expect(page.getByText('What is the capital of Egypt?')).toBeVisible();
    await expect(page.getByText('Cairo · Alexandria · Aswan · Tanta')).toBeVisible();
    await expect(page.getByText('Correct: Cairo')).toBeVisible();

    // The bad row names its number and its column, so the fix is one cell.
    await expect(page.getByText('Row 3')).toBeVisible();
    await expect(page.getByText('Correct answer', { exact: true })).toBeVisible();
    await expect(
      page.getByText('The correct answer does not match any option on this row.'),
    ).toBeVisible();
  });

  test('committing sends the same file for real', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    const calls: ImportCall[] = [];
    await stubImport(page, calls, readSheet);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByRole('button', { name: 'Import from a spreadsheet' }).first().click();

    await chooseSheet(page);
    await page.getByRole('button', { name: 'Check the file' }).click();

    await expect(page.getByRole('button', { name: 'Add these questions' })).toBeEnabled();
    await page.getByRole('button', { name: 'Add these questions' }).click();

    await expect.poll(() => calls.length).toBe(2);
    expect(calls[1].dryRun).toBeFalsy();
    expect(calls[1].content).toBe(calls[0].content);

    await expect(page.getByText('added', { exact: false }).first()).toBeVisible();
  });

  test('a file with nothing addable in it cannot be committed', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    const calls: ImportCall[] = [];

    await stubImport(page, calls, () => ({
      created: 0,
      alreadyPresent: 0,
      preview: [],
      problems: [
        {
          line: 2,
          column: 'QuestionImport:Column:Type',
          reason: 'IMS:QuestionImport:AmbiguousType',
          content: 'multiple choice | Pick one',
        },
      ],
    }));

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByRole('button', { name: 'Import from a spreadsheet' }).first().click();

    await chooseSheet(page);
    await page.getByRole('button', { name: 'Check the file' }).click();

    // Offering a button that would write nothing is offering somebody a way to
    // believe the import worked.
    await expect(page.getByRole('button', { name: 'Add these questions' })).toBeDisabled();
    await expect(page.getByText('Nothing in this file can be added yet')).toBeVisible();

    // And the ambiguous word is named rather than guessed at: it means one
    // answer to half the world and several to the other half.
    await expect(page.getByText('could mean one answer or several')).toBeVisible();
  });

  test('the check button waits until the file has actually been read', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubImport(page, []);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByRole('button', { name: 'Import from a spreadsheet' }).first().click();

    // Reading the file is asynchronous. A button enabled before it finishes
    // posts an empty file, and the server answers "that file is empty" about a
    // file that plainly is not.
    await expect(page.getByRole('button', { name: 'Check the file' })).toBeDisabled();

    await chooseSheet(page);

    await expect(page.getByRole('button', { name: 'Check the file' })).toBeEnabled();
  });

  test('the example spreadsheet is fetched rather than linked to', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubImport(page, []);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByRole('button', { name: 'Import from a spreadsheet' }).first().click();

    const request = page.waitForRequest(url =>
      url.url().includes('/api/assessment/questions/import/template'),
    );

    await page.getByRole('button', { name: 'Download an example' }).click();

    // A plain anchor would resolve against the application rather than the API —
    // different origins — and would carry no token even when it did not.
    await expect(request).resolves.toBeTruthy();
  });

  test('hides the import from someone who may only read', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      grantedPolicies: [
        'Assessment.Exams',
        'Assessment.Exams.View',
        'Assessment.Questions',
        'Assessment.Questions.View',
      ],
    });
    await stubImport(page, []);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);

    await expect(page.getByRole('button', { name: 'Import from a spreadsheet' })).toHaveCount(0);
  });

  test('reads and reports in Arabic without scrolling sideways on a phone', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });

    const calls: ImportCall[] = [];
    await stubImport(page, calls, readSheet);

    await gotoApp(page, `/exams/${EXAM_ID}/questions`);
    await page.getByRole('button', { name: 'استورد من جدول بيانات' }).first().click();

    await page.getByLabel('ملف الجدول').setInputFiles({
      name: 'bank.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from(SHEET, 'utf8'),
    });

    await page.getByRole('button', { name: 'افحص الملف' }).click();

    // The reason and the column read in Arabic, because that is the language the
    // person fixing the spreadsheet is working in.
    await expect(page.getByText('الإجابة الصحيحة لا تطابق أي خيار في هذا الصف.')).toBeVisible();
    await expect(page.getByText('الصف 3')).toBeVisible();

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
