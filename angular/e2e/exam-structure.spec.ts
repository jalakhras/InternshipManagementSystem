import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubExamDetail } from './support/exam-stub';

/**
 * Laying an exam out in parts.
 *
 * The screen's hardest job here is not a control: it is telling the truth about
 * which of its controls do anything. Three of them — the section's own clock,
 * its minimum percentage and its "must be passed" flag — save and are read by
 * nothing. A control that stores a rule nobody enforces is worse than an absent
 * one, because the author stops watching for the thing they believe the software
 * is watching for, and finds out after a cohort has sat the paper.
 *
 * So these tests assert on sentences rather than on behaviour. That is the
 * point: the sentence is the feature.
 */
test.describe('Exam structure', () => {
  const EXAM = '11111111-1111-1111-1111-111111111111';

  /** Sections and passages, with all three unenforced controls set. */
  async function stubStructure(page: import('@playwright/test').Page): Promise<void> {
    await page.route('**/api/assessment/exam-structure/sections/**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 's1',
            examId: EXAM,
            name: 'Listening',
            instructions: 'You will hear each recording once.',
            timeLimitInMinutes: 20,
            minimumPercentage: 60,
            questionsPerForm: 8,
            isQualifying: true,
            displayOrder: 0,
            questionCount: 24,
            creationTime: '2026-08-01T09:00:00Z',
          },
        ]),
      }),
    );

    await page.route('**/api/assessment/questions/groups/**', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
    );
  }

  test('says plainly which section rules are not enforced yet', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubStructure(page);

    await gotoApp(page, `/exams/${EXAM}/structure`);

    // Said once, at the top, in words — not left for an author to work out from
    // a result that came back wrong.
    await expect(page.getByText('Three of these are saved but not yet enforced')).toBeVisible();
    await expect(page.getByText('no section can fail an exam on its own')).toBeVisible();

    // And what does work, in the same breath. A warning with no other half
    // reads as "sections do nothing", which is now untrue and would stop an
    // author using the part that works.
    await expect(page.getByText('the paper is ordered section by section')).toBeVisible();
  });

  test('marks the three dead controls on the row where their values are shown', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubStructure(page);

    await gotoApp(page, `/exams/${EXAM}/structure`);

    // The chip is the only place these numbers are ever shown back. An unmarked
    // "20 min" beside a section reads as a clock that is running.
    await expect(page.getByText('20 min · not enforced')).toBeVisible();
    await expect(page.getByText('min 60% · not enforced')).toBeVisible();
    await expect(page.getByText('Must be passed · not enforced')).toBeVisible();

    // The count is honoured, so it carries no such mark.
    await expect(page.getByText('8 questions')).toBeVisible();
    await expect(page.getByText('8 questions · not enforced')).toHaveCount(0);
  });

  test('says a passage carries no section of its own, and offers no picker for one', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubStructure(page);

    await gotoApp(page, `/exams/${EXAM}/structure`);

    // Sections and passages sit on one screen, so an author has every reason to
    // assume a passage can be put in a part. QuestionGroup.ExamSectionId once
    // said it could — a column no DTO carried, no screen wrote and the form
    // builder never read, so a passage filed into Reading contributed nothing.
    // The column is gone, and the screen has to say what replaces it rather than
    // leaving the question unanswered on the screen that raises it.
    await expect(page.getByText('A passage belongs to no part of the exam by itself')).toBeVisible();
    await expect(page.getByText('File each of its questions into the same section')).toBeVisible();

    // And the passage editor agrees: no section control, so the sentence above
    // is not contradicted by the form directly beneath it.
    await page.getByRole('button', { name: 'Add passage' }).click();

    await expect(page.getByLabel('Instructions')).toBeVisible();
    await expect(page.getByLabel('Section')).toHaveCount(0);
    await expect(page.getByLabel('Part')).toHaveCount(0);
  });

  test('warns again on the three fields themselves while they are being filled in', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await stubStructure(page);

    await gotoApp(page, `/exams/${EXAM}/structure`);
    await page.getByRole('button', { name: 'Add section' }).click();

    // Beside the field, at the moment somebody types into it. A notice at the
    // top of a screen is read once; this is read when it matters.
    await expect(page.getByText('Saved, but nothing enforces this yet.')).toHaveCount(3);

    // Instructions are delivered, and the hint says where they will appear —
    // the same honesty in the other direction.
    await expect(
      page.getByText('Shown to the candidate on the first question of this part'),
    ).toBeVisible();
  });
});
