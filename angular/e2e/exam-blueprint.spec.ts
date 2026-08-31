import { expect, Page, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubExamDetail } from './support/exam-stub';

/**
 * Saying which part of the paper a rule fills.
 *
 * The blueprint is the recipe a drawn paper is built from, and until now every
 * rule filled the paper as a whole. So an exam laid out in parts could not draw
 * from the shared bank at all: a part served only what was filed into it by
 * hand, and a bank question belongs to every exam at its level and cannot be
 * filed into one exam's part.
 *
 * "Ten listening from the bank and ten reading" is the first thing a language
 * centre asks for, and this box is where it gets written down.
 */
test.describe('Exam blueprint', () => {
  const EXAM = '11111111-1111-1111-1111-111111111111';

  const SECTIONS = [
    { id: 's1', examId: EXAM, name: 'Listening', displayOrder: 0, questionCount: 0, creationTime: '2026-08-01T09:00:00Z' },
    { id: 's2', examId: EXAM, name: 'Reading', displayOrder: 1, questionCount: 0, creationTime: '2026-08-01T09:00:00Z' },
  ];

  const RULE = {
    id: 'r1',
    examSectionId: 's1',
    examSectionName: 'Listening',
    topicId: 't1',
    topicName: 'Listening',
    questionCount: 10,
    displayOrder: 0,
    availableCount: 24,
  };

  async function stubBlueprint(page: Page, options: { sections?: unknown[]; rules?: unknown[] } = {}): Promise<void> {
    await page.route('**/api/assessment/exam-structure/sections/**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(options.sections ?? SECTIONS),
      }),
    );

    await page.route('**/api/assessment/exams/*/blueprint', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(options.rules ?? [RULE]),
      }),
    );

    await page.route('**/api/assessment/catalog/categories**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'c1', name: 'Spanish', code: 'es', topics: [{ id: 't1', name: 'Listening' }] },
        ]),
      }),
    );

    await page.route('**/api/assessment/questions/types', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
    );
  }

  test('offers the exam its own parts, and the whole paper', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubBlueprint(page);

    await gotoApp(page, `/exams/${EXAM}/blueprint`);

    const part = page.getByLabel('Which part');

    await expect(part).toBeVisible();

    // Both parts of this exam, and the option to fill the paper as a whole —
    // which is what every rule did before there was a choice, and still the
    // right answer for an exam that is one undivided paper.
    await expect(part.locator('option')).toHaveText(['The whole paper', 'Listening', 'Reading']);

    // The rule's own part comes back selected, not reset to the first entry. A
    // screen that quietly re-points a rule at the whole paper on every visit
    // rewrites the recipe of anyone who opens it to read.
    await expect(part).toHaveValue('s1');
  });

  test('explains what a part draws from, once, under the list', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubBlueprint(page);

    await gotoApp(page, `/exams/${EXAM}/blueprint`);

    // Because the precedence is not guessable from the control. An author who
    // has filed two questions into Listening and asks for ten needs to know
    // they will get two — and why.
    await expect(page.getByText('chosen afresh at every sitting')).toBeVisible();
    await expect(page.getByText('Questions filed into a part by hand come first')).toBeVisible();
  });

  test('does not ask which part when the exam has none', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubBlueprint(page, { sections: [] });

    await gotoApp(page, `/exams/${EXAM}/blueprint`);

    // Most exams are one undivided paper. A box whose every answer is the same
    // answer is a question that should not be asked.
    await expect(page.getByLabel('Which part')).toHaveCount(0);
    await expect(page.getByText('chosen afresh at every sitting')).toHaveCount(0);
  });

  test('sends the chosen part when the blueprint is saved', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubBlueprint(page);

    let saved: Array<{ examSectionId?: string | null }> = [];

    await page.route('**/api/assessment/exams/*/blueprint', route => {
      if (route.request().method() === 'PUT') {
        saved = route.request().postDataJSON();

        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([RULE]),
        });
      }

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([RULE]),
      });
    });

    await gotoApp(page, `/exams/${EXAM}/blueprint`);

    await page.getByLabel('Which part').selectOption('s2');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByRole('status')).toBeVisible();

    // The whole point of the field. A control that changes what is on screen
    // and not what is sent is the failure this product keeps finding.
    expect(saved[0]?.examSectionId).toBe('s2');
  });
});
