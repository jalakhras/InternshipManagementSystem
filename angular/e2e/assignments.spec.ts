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

  const person = (over: Record<string, unknown> = {}) => ({
    id: 'c9',
    fullName: 'Rana Aziz',
    email: 'rana@example.com',
    status: 0,
    groupNames: [],
    attemptCount: 0,
    creationTime: '2026-01-01T00:00:00Z',
    ...over,
  });

  /**
   * The people search, and the group's roll, which share one endpoint.
   *
   * Matched on the path rather than by glob so it cannot swallow
   * `/candidates/groups`, which the panel needs answered separately and which
   * this route would otherwise shadow — Playwright gives the last route
   * registered the first refusal.
   */
  const stubPeople = async (page: import('@playwright/test').Page, people: unknown[]) => {
    await page.route(
      url => url.pathname === '/api/assessment/candidates',
      route =>
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ totalCount: people.length, items: people }),
        }),
    );
  };

  /** The body of the one POST that creates the links. */
  const sentBody = (page: import('@playwright/test').Page) =>
    page.waitForRequest(
      request =>
        request.method() === 'POST' && request.url().endsWith('/api/assessment/assignments'),
    );

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

  test('sends to one person, with no class to hold them', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, [], {
      assignmentId: 'as1',
      linksCreated: 1,
      emailsSent: 1,
      emailsFailed: 0,
      recipients: [
        { candidateId: 'c9', candidateName: 'Rana Aziz', email: 'rana@example.com', url: 'https://exam.test/exam/tok-9', emailSent: true },
      ],
    });
    await stubPeople(page, [person()]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'Send this exam' }).first().click();

    await page.getByRole('button', { name: 'One person' }).click();

    // A coordinator who has added one student could send them nothing until they
    // had invented a class to put them in — and on a new organisation, with no
    // classes at all, the picker was empty and the button never enabled.
    await expect(page.getByRole('button', { name: 'Create the links' })).toBeDisabled();

    await page.getByLabel('Which person').fill('Rana');
    await page.getByRole('button', { name: /Rana Aziz/ }).click();

    await expect(page.getByRole('button', { name: 'Create the links' })).toBeEnabled();

    const posted = sentBody(page);

    await page.getByRole('button', { name: 'Create the links' }).click();

    const body = (await posted).postDataJSON();

    // One target, never two. The server refuses a request naming both, so a
    // leftover class here would be a send that fails after the coordinator has
    // already been told it is on its way.
    expect(body.candidateId).toBe('c9');
    expect(body.candidateGroupId).toBeUndefined();

    // And everything downstream still runs: the link comes back and is shown
    // once, in the panel that is the only chance to copy it.
    await expect(page.getByText('shown once')).toBeVisible();
    await expect(page.getByText('https://exam.test/exam/tok-9')).toBeVisible();
  });

  test('a class still goes to the whole class', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, []);
    await stubPeople(page, [
      person({ id: 'c1', fullName: 'Layla Hassan', email: 'layla@example.com' }),
      person({ id: 'c2', fullName: 'Omar Nasser', email: 'omar@example.com' }),
    ]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'Send this exam' }).first().click();

    await page.getByRole('button', { name: 'A class' }).click();
    await page.getByLabel('Who gets it').selectOption('g1');

    // Still shown before the button, because a link once sent is a link somebody
    // has.
    await expect(page.getByText('Layla Hassan')).toBeVisible();

    const posted = sentBody(page);

    await page.getByRole('button', { name: 'Create the links' }).click();

    const body = (await posted).postDataJSON();

    expect(body.candidateGroupId).toBe('g1');
    expect(body.candidateId).toBeUndefined();

    // The paper, the deadline, the attempts and the email toggle all still ride
    // along, and the panel of created links still appears.
    expect(body.maxAttempts).toBe(1);
    expect(body.sendEmail).toBe(true);
    expect(typeof body.expiresAt).toBe('string');

    await expect(page.getByText('https://exam.test/exam/tok-1')).toBeVisible();
  });

  test('a class and a person can never both be chosen', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, []);
    await stubPeople(page, [person()]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'Send this exam' }).first().click();

    await page.getByRole('button', { name: 'One person' }).click();
    await page.getByLabel('Which person').fill('Rana');
    await page.getByRole('button', { name: /Rana Aziz/ }).click();

    // Switching sides drops what the other side held. If it did not, the button
    // would stay enabled here and the request would carry two targets — which is
    // the one thing the server will not take.
    await page.getByRole('button', { name: 'A class' }).click();

    await expect(page.getByRole('button', { name: 'Create the links' })).toBeDisabled();
    await expect(page.getByText('rana@example.com')).toHaveCount(0);
  });

  test('the person picker fits a 390px phone in Arabic', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });

    await stubAbp(page, { culture: 'ar', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, []);
    await stubPeople(page, [person()]);

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'أرسل هذا الامتحان' }).first().click();

    const onePerson = page.getByRole('button', { name: 'شخص واحد' });

    // 44px, because a finger is not a mouse pointer.
    const box = await onePerson.boundingBox();

    expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);

    await onePerson.click();
    await page.getByLabel('أيّ شخص').fill('Rana');
    await expect(page.getByRole('button', { name: /Rana Aziz/ })).toBeVisible();

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });

  test('the empty state offers both a class and one person', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubAssignments(page, []);

    await gotoApp(page, `/assignments/${EXAM_ID}`);

    // The line read "Choose a group and each person gets their own link", which
    // was the whole story until one person could be sent to on their own and half
    // of it afterwards — on the one screen whose only job is to say what to do
    // next. A new key rather than an edit to the old one, so a stale translation
    // of the old sentence cannot answer this question.
    await expect(page.getByText('or to one named person')).toBeVisible();
    await expect(page.getByText('Choose a group and each person gets')).toHaveCount(0);
  });

  test('says which permission the person search needs, rather than that it failed', async ({ page }) => {
    // A coordinator on a custom role: they may send an exam and see what was
    // sent, and they may not read the candidate roll.
    await stubAbp(page, {
      culture: 'en',
      grantedPolicies: [
        'Assessment.Assignments',
        'Assessment.Assignments.View',
        'Assessment.Assignments.Create',
        'Assessment.Groups',
        'Assessment.Groups.View',
      ],
    });
    await stubAssignments(page, []);

    // Answers the way the server does for this role, so a search that ran would
    // land on the localised "the search could not run" this test is here to
    // replace.
    let searched = 0;

    await page.route(
      url => url.pathname === '/api/assessment/candidates',
      route => {
        searched += 1;

        return route.fulfill({
          status: 403,
          contentType: 'application/json',
          body: JSON.stringify({ error: { message: 'Forbidden' } }),
        });
      },
    );

    await gotoApp(page, `/assignments/${EXAM_ID}`);
    await page.getByRole('button', { name: 'Send this exam' }).first().click();
    await page.getByRole('button', { name: 'One person' }).click();

    // Named, so somebody knows what to ask an administrator for.
    await expect(page.getByText('Candidates — View')).toBeVisible();
    await expect(page.getByText('The search could not run')).toHaveCount(0);

    // And no dead box to type into. Offering a search that cannot run and then
    // reporting a failure is what made this read as a broken server.
    await expect(page.getByLabel('Which person')).toHaveCount(0);

    // The class side is untouched, which is what the message tells them to use.
    await page.getByRole('button', { name: 'A class' }).click();
    await page.getByLabel('Who gets it').selectOption('g1');
    await expect(page.getByRole('button', { name: 'Create the links' })).toBeEnabled();

    // "This class has nobody in it yet" was a lie told by the same missing
    // permission — the roll could not be read, so it looked empty.
    await expect(page.getByText('This class has nobody in it yet')).toHaveCount(0);
    await expect(page.getByText('The names in this class cannot be shown')).toBeVisible();

    // Nothing was ever asked of an endpoint this account cannot use.
    expect(searched).toBe(0);
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
