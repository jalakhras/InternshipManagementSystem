import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

const EXAM_ID = '11111111-1111-1111-1111-111111111111';

/**
 * Sending an exam out.
 *
 * The two behaviours worth guarding are both about what happens when something
 * goes wrong: a link is shown once and cannot be recovered, and a failed email
 * is not a failed link.
 */
test.describe('Assignments', () => {
  const link = (over: Record<string, unknown> = {}) => ({
    id: 'l1',
    examId: EXAM_ID,
    candidateId: 'c1',
    candidateName: 'Layla Hassan',
    tokenPrefix: 'a1b2c3',
    expiresAt: '2027-01-01T00:00:00Z',
    maxAttempts: 2,
    attemptsUsed: 0,
    isRevoked: false,
    ...over,
  });

  const stubAssignments = async (
    page: import('@playwright/test').Page,
    links: unknown[],
    result?: unknown,
  ) => {
    await page.route('**/api/assessment/exams/*', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: EXAM_ID, title: 'Spanish B1 Placement', status: 1, mode: 0, timeLimitInMinutes: 30, passingPercentage: 60 }),
      }),
    );

    await page.route('**/api/assessment/candidates/groups', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{ id: 'g1', name: 'Evening A1', memberCount: 2, creationTime: '2026-01-01T00:00:00Z' }]),
      }),
    );

    await page.route('**/api/assessment/assignments', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(
          result ?? {
            assignmentId: 'as1',
            linksCreated: 2,
            emailsSent: 2,
            emailsFailed: 0,
            recipients: [
              { candidateId: 'c1', candidateName: 'Layla Hassan', email: 'layla@example.com', url: 'https://exam.test/exam/tok-1', emailSent: true },
              { candidateId: 'c2', candidateName: 'Omar Nasser', email: 'omar@example.com', url: 'https://exam.test/exam/tok-2', emailSent: true },
            ],
          },
        ),
      }),
    );

    await page.route('**/api/assessment/assignments/links/**', route => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 204, body: '' });
      }

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalCount: links.length, items: links }),
      });
    });
  };

  test('shows what happened to each link rather than only that it exists', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, [
      link({ id: 'l1', candidateName: 'Never opened' }),
      link({ id: 'l2', candidateName: 'Started it', firstOpenedAt: '2026-08-01T10:00:00Z' }),
      link({ id: 'l3', candidateName: 'Used both', attemptsUsed: 2 }),
      link({ id: 'l4', candidateName: 'Killed', isRevoked: true }),
    ]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);

    // Six states, and the difference between them is what a coordinator chasing
    // people actually needs.
    await expect(page.getByRole('row', { name: /Never opened/ }).locator('.state-chip')).toHaveText('Not sent');
    await expect(page.getByRole('row', { name: /Started it/ }).locator('.state-chip')).toHaveText('Started');
    await expect(page.getByRole('row', { name: /Used both/ }).locator('.state-chip')).toHaveText('Finished');
    await expect(page.getByRole('row', { name: /Killed/ }).locator('.state-chip')).toHaveText('Revoked');
  });

  test('the deadline can be extended without reissuing first', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, [link({ candidateName: 'Missed Friday' })]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);

    // Guards a nesting mistake rather than a missing feature: the extend dialog
    // was once written inside the "new link" panel's own @if, so it rendered
    // only after a link had just been reissued — which is the one moment nobody
    // needs it. The whole feature was unreachable and everything still compiled,
    // because a template that never runs is a template that never complains.
    await page.getByRole('button', { name: /Extend the deadline/ }).click();

    const dialog = page.getByRole('dialog');

    await expect(dialog).toBeVisible();

    // Scoped to the dialog: the name is in the table behind it too, and an
    // unscoped match would pass with no dialog on screen at all.
    await expect(dialog.getByText('Missed Friday')).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'Extend' })).toBeVisible();
  });

  test('a revoked link stays revoked whatever else is true of it', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, [
      link({ candidateName: 'Killed mid-attempt', isRevoked: true, firstOpenedAt: '2026-08-01T10:00:00Z' }),
    ]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);

    // A killed link is not "in progress" however far somebody had got.
    await expect(page.locator('.state-chip')).toHaveText('Revoked');
  });

  test('says the links are shown once and cannot be recovered', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, []);

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'Send this exam' }).first().click();

    await page.getByLabel('Who gets it').selectOption('g1');
    await page.getByRole('button', { name: 'Create the links' }).click();

    // Said before somebody closes the panel, not after they discover it.
    await expect(page.getByText('shown once')).toBeVisible();
    await expect(page.getByText('https://exam.test/exam/tok-1')).toBeVisible();
  });

  test('a failed email still shows a working link', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, [], {
      assignmentId: 'as1',
      linksCreated: 2,
      emailsSent: 1,
      emailsFailed: 1,
      recipients: [
        { candidateId: 'c1', candidateName: 'Layla Hassan', email: 'layla@example.com', url: 'https://exam.test/exam/tok-1', emailSent: true },
        { candidateId: 'c2', candidateName: 'Omar Nasser', email: 'bad@nowhere', url: 'https://exam.test/exam/tok-2', emailSent: false, emailError: 'Mailbox unavailable' },
      ],
    });

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'Send this exam' }).first().click();
    await page.getByLabel('Who gets it').selectOption('g1');
    await page.getByRole('button', { name: 'Create the links' }).click();

    // Creating the links never depends on the mail server, so the one that failed
    // to send is still usable and still on screen to be passed on.
    await expect(page.getByText('could not be sent')).toBeVisible();
    await expect(page.getByText('Mailbox unavailable')).toBeVisible();
    await expect(page.getByText('https://exam.test/exam/tok-2')).toBeVisible();
  });

  test('sending needs a group chosen', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, []);

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'Send this exam' }).first().click();

    // "Send to nobody" is not a thing to allow and then explain.
    await expect(page.getByRole('button', { name: 'Create the links' })).toBeDisabled();

    await page.getByLabel('Who gets it').selectOption('g1');
    await expect(page.getByRole('button', { name: 'Create the links' })).toBeEnabled();
  });

  test('hides revoking and sending from someone who may only read', async ({ page }) => {
    await stubAbp(page, {
      culture: 'en',
      grantedPolicies: ['Assessment.Assignments', 'Assessment.Assignments.View'],
    });
    await stubAssignments(page, [link()]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);

    await expect(page.getByRole('button', { name: /^Revoke:/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Send this exam' })).toHaveCount(0);
  });

  test('does not scroll sideways on a phone in Arabic', async ({ page }) => {
    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, [link()]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
