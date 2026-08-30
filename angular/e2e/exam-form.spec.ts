import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubExamDetail } from './support/exam-stub';

/**
 * The exam editor, and the publish gate in particular.
 *
 * Publishing is the point where a draft becomes something a real person sits, so
 * the behaviour under test is that the author is told everything at once — and
 * that a blocked exam cannot be pushed through anyway.
 */
test.describe('Exam editor', () => {
  test('explains what each assembly switch does', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111');

    // Four unexplained switches would be four decisions nobody can make. Each
    // carries the sentence that makes it decidable.
    await expect(page.getByText('The whole paper never reaches the browser')).toBeVisible();
    await expect(page.getByText('one leaked paper is worth nothing')).toBeVisible();
    await expect(
      page.getByText('The system never judges on them by itself'),
    ).toBeVisible();
  });

  test("a new exam takes the organisation's pass mark, not a number in the code", async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    await page.route('**/api/assessment/settings', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          organizationName: 'Trading Academy',
          defaultLanguage: 'ar',
          timeZone: 'Asia/Riyadh',
          defaultPassingPercentage: 75,
          showResultToCandidate: true,
          collectIntegritySignals: true,
          enableSelfRegistration: false,
        }),
      }),
    );

    await gotoApp(page, '/exams/new');

    // The setting's own hint says "applied to a new exam unless its author
    // changes it", and the number was hardcoded at 60 — so an organisation that
    // set 75 watched every new exam come back at 60 and either corrected each
    // one by hand or did not notice.
    await expect(page.getByLabel(/Pass mark/)).toHaveValue('75');
  });

  test('says why the pass mark is a percentage', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111');

    // The reason a fixed mark is wrong is not obvious, and getting it wrong is
    // how an exam out of 200 ends up with a threshold meant for one out of 100.
    await expect(
      page.getByText('forms differ in length', { exact: false }),
    ).toBeVisible();
  });

  test('shows every blocker at once and refuses to publish', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page, {
      publishCheck: {
        canPublish: false,
        blockers: ['IMS:Exam:NoQuestions', 'IMS:Exam:FormLargerThanBank'],
        warnings: ['IMS:Exam:NoTopicsAssigned'],
        questionCount: 0,
        totalScore: 0,
        formLength: 10,
      },
    });
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111');

    await page.getByRole('button', { name: 'Publish' }).click();

    // Both blockers, not the first one. Someone walked through three refusals
    // stops reading the fourth.
    await expect(page.getByText('An exam cannot be published without questions.')).toBeVisible();
    await expect(page.getByText('The form asks for more questions than the bank holds.')).toBeVisible();

    // Warnings are shown alongside, but described differently: they do not block.
    await expect(page.getByText('No competencies assigned', { exact: false })).toBeVisible();

    await expect(page.getByRole('button', { name: 'Publish now' })).toBeDisabled();
  });

  test('allows publishing when only warnings stand', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page, {
      publishCheck: {
        canPublish: true,
        blockers: [],
        warnings: ['IMS:Exam:EveryoneGetsTheSameForm'],
        questionCount: 30,
        totalScore: 30,
        formLength: 30,
      },
    });
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111');

    await page.getByRole('button', { name: 'Publish' }).click();

    // A warning is a judgement the author is allowed to make. It is stated
    // plainly and then got out of the way.
    await expect(page.getByText('one leak is everybody', { exact: false })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Publish now' })).toBeEnabled();
  });

  test('hides publish from someone who may edit but not publish', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      grantedPolicies: ['Assessment.Exams.View', 'Assessment.Exams.Edit'],
    });
    await stubExamDetail(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111');

    // Publishing is a separate permission because it is a separate act.
    await expect(page.getByRole('button', { name: 'Save' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Publish' })).toHaveCount(0);
  });

  test('does not scroll sideways on a phone', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubExamDetail(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111');

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
