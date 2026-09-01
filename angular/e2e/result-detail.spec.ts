import { expect, test, type Page } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * How a sitting ended, on the screen a coordinator reads a result from.
 *
 * None of it was shown. The reason crossed the wire and no template rendered
 * it, so a paper that was cut short — the room evacuated, the browser frozen,
 * the clock run out — read exactly like one somebody finished and handed in.
 * A low score means two different things in those two cases, and the screen
 * gave no way to tell them apart.
 *
 * The note is the second half. The monitor asks whoever ends a sitting for a
 * reason, under a label that says it is recorded, and it was: into a column no
 * screen and no endpoint ever read back. On the day it is disputed, nobody can
 * find it.
 */
test.describe('How a sitting ended', () => {
  const ATTEMPT = 'aaaaaaaa-0000-0000-0000-000000000001';

  async function openResult(
    page: Page,
    summary: { endReason: string; endedByReason?: string },
  ): Promise<void> {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });

    await page.route(`**/api/assessment/results/${ATTEMPT}`, route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          summary: {
            attemptId: ATTEMPT,
            candidateId: 'cccccccc-0000-0000-0000-000000000001',
            candidateName: 'Sitter One',
            candidateEmail: 'one@example.test',
            examId: 'eeeeeeee-0000-0000-0000-000000000001',
            examTitle: 'Placement',
            formName: null,
            startedAt: new Date(Date.now() - 600_000).toISOString(),
            submittedAt: new Date().toISOString(),
            isSubmitted: true,
            isGraded: true,
            needsManualReview: false,
            score: 4,
            maxScore: 10,
            scorePercentage: 40,
            isPassed: false,
            integrityFlagCount: 0,
            durationInMinutes: 10,
            ...summary,
          },
          answers: [],
          byTopic: [],
          bySection: [],
          feedback: [],
        }),
      }),
    );

    await gotoApp(page, `/results/${ATTEMPT}`);
  }

  test('a sitting the organisation ended says so, and shows what they wrote', async ({ page }) => {
    await openResult(page, {
      endReason: 'EndedByAdministrator',
      endedByReason: 'The room was evacuated.',
    });

    await expect(page.getByText('Ended by the organisation')).toBeVisible();

    // Written by one member of staff for another, and the only place it can be
    // read. Forty per cent on a paper that stopped early is not forty per cent
    // on a paper somebody finished.
    await expect(page.getByText('The room was evacuated.')).toBeVisible();
  });

  test('a sitting the clock ended says so too', async ({ page }) => {
    await openResult(page, { endReason: 'TimedOutOnServer' });

    await expect(
      page.getByText('Time ran out and it was submitted automatically'),
    ).toBeVisible();
  });

  test('a paper the candidate handed in is not explained', async ({ page }) => {
    await openResult(page, { endReason: 'SubmittedByCandidate' });

    // The ordinary ending needs no note, and one on every result is noise on the
    // screen a coordinator reads most — which is how the note that matters gets
    // skipped.
    await expect(page.locator('.stat__note')).toHaveCount(0);
    await expect(page.getByText('The candidate submitted it')).toHaveCount(0);
  });
});
