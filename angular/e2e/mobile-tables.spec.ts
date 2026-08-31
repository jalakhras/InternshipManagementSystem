import { Page, expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';

/**
 * Every list table, on a phone.
 *
 * A table with eight columns cannot show its action column at 390px, and until
 * the cards rule in `_base.scss` landed it did not try: the column sat outside
 * the scroll box entirely, behind a sideways drag with no fade, no shadow and no
 * hint of any kind. Measured on 2026-08-29 the exams action cell was 0% visible
 * in both languages, and the same for users, candidates and groups. That is a
 * coordinator on a phone who cannot edit, publish, assign or delete anything.
 *
 * So these are geometry tests, not appearance tests. They ask the only question
 * that matters — is the Delete button inside the piece of the world the person is
 * looking at — and they ask it in both languages, because the direction decides
 * which edge the column hides behind and English runs 20–27% wider than Arabic.
 *
 * The second assertion is the invariant that must survive the fix: the document
 * itself never scrolls sideways. It was 0 on every screen before, thanks to
 * `contain: paint` on `.astro-scroll-x`, and turning rows into cards must not
 * trade one overflow for another.
 */

const EXAM_ID = '11111111-1111-1111-1111-111111111111';

/** The narrowest phone the product is designed for, and the width 2.4 measured. */
const PHONE = { width: 390, height: 844 };

/** Wide enough that the cards rule is off and the tables are tables again. */
const DESKTOP = { width: 1280, height: 900 };

/**
 * Roles and organisations are guarded by ABP's own modules rather than by this
 * product's permission tree, so `ALL_POLICIES` does not reach them and their
 * action buttons would simply not render.
 */
const EVERY_POLICY = [
  ...ALL_POLICIES,
  'AbpIdentity.Roles',
  'AbpIdentity.Roles.Create',
  'AbpIdentity.Roles.Update',
  'AbpIdentity.Roles.Delete',
  'AbpIdentity.Roles.ManagePermissions',
  'AbpTenantManagement.Tenants',
  'AbpTenantManagement.Tenants.Create',
  'AbpTenantManagement.Tenants.Update',
  'AbpTenantManagement.Tenants.Delete',
];

const json = (body: unknown) => ({
  status: 200,
  contentType: 'application/json',
  body: JSON.stringify(body),
});

/**
 * One fixture for all twelve screens.
 *
 * Per-screen stubs would be tidier to read and would triple the length of this
 * file for no gain: nothing here is about the data. Every row is deliberately
 * long-ish — a real candidate name, a real exam title — because a table that
 * fits at 390px only because its cells are empty is not a table that fits.
 *
 * Registration order is load-bearing. Playwright matches routes
 * last-registered-first, so the broad patterns go in first and the specific ones
 * that must win go in last.
 */
async function stubEveryList(page: Page): Promise<void> {
  const started = new Date(Date.now() - 20 * 60_000).toISOString();
  const submitted = new Date(Date.now() - 5 * 86_400_000).toISOString();

  await page.route('**/api/assessment/categories**', route => route.fulfill(json([])));
  await page.route('**/api/assessment/levels**', route => route.fulfill(json([])));
  await page.route('**/api/assessment/topics**', route => route.fulfill(json([])));
  await page.route('**/api/assessment/exam-structure/sections/*', route => route.fulfill(json([])));

  await page.route('**/api/assessment/exams**', route =>
    route.fulfill(
      json({
        totalCount: 2,
        items: [
          {
            id: EXAM_ID,
            title: 'Spanish B1 Placement',
            categoryName: 'Spanish',
            levelName: 'B1',
            status: 1,
            mode: 0,
            timeLimitInMinutes: 45,
            passingPercentage: 60,
            questionCount: 30,
            creationTime: '2026-08-01T09:00:00Z',
          },
          {
            id: '22222222-2222-2222-2222-222222222222',
            title: 'Technical Analysis — Level 2',
            categoryName: 'Trading',
            levelName: 'Advanced',
            status: 1,
            mode: 0,
            timeLimitInMinutes: 60,
            passingPercentage: 70,
            questionsPerForm: 25,
            questionCount: 120,
            creationTime: '2026-08-10T09:00:00Z',
          },
        ],
      }),
    ),
  );

  // After the list pattern above, so it wins for `/exams/{id}`.
  await page.route('**/api/assessment/exams/*', route =>
    route.fulfill(
      json({
        id: EXAM_ID,
        title: 'Spanish B1 Placement',
        status: 1,
        mode: 0,
        timeLimitInMinutes: 45,
        passingPercentage: 60,
      }),
    ),
  );

  await page.route('**/api/assessment/questions?**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            id: 'q1',
            examId: null,
            type: 'single-choice',
            text: 'Which of these levels is acting as support on the weekly chart?',
            payload: '{}',
            difficulty: 1,
            score: 2,
            displayOrder: 0,
            isActive: true,
            timesAnswered: 0,
            timesServed: 0,
            difficultyIndex: null,
            creationTime: '2026-01-01T00:00:00Z',
          },
        ],
      }),
    ),
  );

  await page.route('**/api/assessment/questions/types', route =>
    route.fulfill(
      json([
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
    ),
  );

  await page.route('**/api/assessment/candidates?**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            id: 'c1',
            fullName: 'Layla Hassan',
            email: 'layla.hassan@example.com',
            reference: 'STU-20416',
            status: 0,
            groupNames: ['Evening A1'],
            attemptCount: 3,
            creationTime: '2026-01-01T00:00:00Z',
          },
        ],
      }),
    ),
  );

  await page.route('**/api/assessment/candidates/groups', route =>
    route.fulfill(
      json([
        {
          id: 'g1',
          name: 'Evening A1',
          description: 'Tuesdays and Thursdays',
          categoryName: 'Spanish',
          levelName: 'A1',
          startsOn: '2026-09-01T00:00:00Z',
          endsOn: '2026-12-15T00:00:00Z',
          memberCount: 24,
          creationTime: '2026-01-01T00:00:00Z',
        },
      ]),
    ),
  );

  await page.route('**/api/app/users?**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            id: 'u1',
            userName: 'l.hassan',
            fullName: 'Layla Hassan',
            email: 'layla.hassan@example.com',
            roles: ['coordinator'],
          },
        ],
      }),
    ),
  );

  await page.route('**/api/app/users/roles', route => route.fulfill(json(['admin', 'coordinator'])));

  await page.route('**/api/assessment/assignments/links/**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            id: 'l1',
            examId: EXAM_ID,
            candidateId: 'c1',
            candidateName: 'Layla Hassan',
            tokenPrefix: 'a1b2c3',
            expiresAt: '2027-01-01T00:00:00Z',
            maxAttempts: 2,
            attemptsUsed: 1,
            isRevoked: false,
          },
        ],
      }),
    ),
  );

  await page.route('**/api/assessment/review/queue**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            attemptId: 'aaaaaaaa-1111-1111-1111-111111111111',
            candidateName: 'Layla Hassan',
            examTitle: 'Spanish B1 Placement',
            submittedAt: submitted,
            pendingCount: 3,
            provisionalScore: 12,
            maxScore: 20,
            integrityFlagCount: 2,
          },
        ],
      }),
    ),
  );

  await page.route('**/api/assessment/attempts/running**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            attemptId: 'bbbbbbbb-1111-1111-1111-111111111111',
            candidateName: 'Layla Hassan',
            candidateEmail: 'layla.hassan@example.com',
            examTitle: 'Spanish B1 Placement',
            formName: 'Form A',
            startedAt: started,
            durationInMinutes: 45,
            score: 0,
            maxScore: 20,
            scorePercentage: 0,
            isGraded: false,
            isPassed: false,
            integrityFlagCount: 0,
          },
        ],
      }),
    ),
  );

  await page.route('**/api/assessment/attempts?**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            attemptId: 'cccccccc-1111-1111-1111-111111111111',
            candidateName: 'Layla Hassan',
            candidateEmail: 'layla.hassan@example.com',
            examTitle: 'Spanish B1 Placement',
            formName: 'Form A',
            startedAt: started,
            durationInMinutes: 42,
            score: 14,
            maxScore: 20,
            scorePercentage: 70,
            isGraded: true,
            isPassed: true,
            integrityFlagCount: 1,
          },
        ],
      }),
    ),
  );

  await page.route('**/api/assessment/results?**', route =>
    route.fulfill(
      json({
        totalCount: 1,
        items: [
          {
            attemptId: 'cccccccc-1111-1111-1111-111111111111',
            candidateName: 'Layla Hassan',
            candidateEmail: 'layla.hassan@example.com',
            examTitle: 'Spanish B1 Placement',
            formName: 'Form A',
            startedAt: started,
            durationInMinutes: 42,
            score: 14,
            maxScore: 20,
            scorePercentage: 70,
            isGraded: true,
            isPassed: true,
            integrityFlagCount: 1,
          },
        ],
      }),
    ),
  );

  await page.route('**/api/assessment/results/summary**', route =>
    route.fulfill(
      json({
        sat: 24,
        notStarted: 3,
        passed: 18,
        failed: 6,
        awaitingMarking: 2,
        averageScorePercentage: 68,
        highestScorePercentage: 94,
        lowestScorePercentage: 31,
        medianScorePercentage: 70,
      }),
    ),
  );

  // An array, not a page: item analysis is one exam's whole paper or nothing.
  await page.route('**/api/assessment/results/item-analysis/*', route =>
    route.fulfill(
      json([
        {
          questionId: 'q1',
          text: 'Which of these levels is acting as support on the weekly chart?',
          type: 'single-choice',
          topicName: 'Support and resistance',
          timesAnswered: 48,
          facility: 0.42,
          discrimination: 0.31,
          flagKey: null,
        },
      ]),
    ),
  );

  await page.route('**/api/identity/roles**', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        totalCount: 1,
        items: [{ id: 'r1', name: 'Coordinator', isDefault: false, isStatic: false, isPublic: true }],
      }),
    }),
  );

  await page.route('**/api/permission-management/permissions**', route =>
    route.fulfill(json({ entityDisplayName: 'Coordinator', groups: [] })),
  );

  await page.route('**/api/multi-tenancy/tenants**', route =>
    route.fulfill(json({ totalCount: 1, items: [{ id: 't1', name: 'trading-academy' }] })),
  );
}

interface Screen {
  /** The name that appears in the test title, and in the measurement log. */
  name: string;
  path: string;
  /** False for the two tables that have no action column at all. */
  hasActions: boolean;

  /** Item analysis draws nothing until an exam is named, so it has to be named. */
  prepare?: (page: Page) => Promise<void>;
}

/**
 * All twelve. Results and item analysis carry no action column and so cannot
 * fail the containment check, but they are carded by the same rule and still owe
 * the no-sideways-scroll invariant.
 */
const SCREENS: Screen[] = [
  { name: 'exams', path: '/exams', hasActions: true },
  { name: 'questions', path: '/questions', hasActions: true },
  { name: 'candidates', path: '/candidates', hasActions: true },
  { name: 'groups', path: '/groups', hasActions: true },
  { name: 'assignments', path: `/assignments/${EXAM_ID}`, hasActions: true },
  { name: 'review queue', path: '/review', hasActions: true },
  { name: 'attempt monitor', path: '/results/running', hasActions: true },
  { name: 'users', path: '/users', hasActions: true },
  { name: 'roles', path: '/roles', hasActions: true },
  { name: 'organisations', path: '/organisations', hasActions: true },
  { name: 'results', path: '/results', hasActions: false },
  {
    name: 'item analysis',
    path: '/results/questions',
    hasActions: false,
    prepare: async page => {
      await page.locator('.filters select').selectOption(EXAM_ID);
    },
  },
];

interface Geometry {
  /** Fraction of the action cell inside its scroll container at scroll 0. */
  insideContainer: number;
  /** Fraction of the action cell inside the viewport. */
  insideViewport: number;
  /** How much wider the table is than the box meant to hold it. */
  containerOverflow: number;
  /** The invariant: anything but 0 is the page scrolling sideways. */
  pageOverflow: number;
  /** `table` above the breakpoint, `block` below it. */
  tableDisplay: string;
}

/**
 * The probe from the fix list, widened to answer the viewport question too.
 *
 * Container containment and viewport containment are not the same thing and the
 * second is the one a thumb cares about, so both are reported: a cell can sit
 * inside its scroll box and still be under the sidebar or off the screen.
 */
async function measure(page: Page, hasActions: boolean): Promise<Geometry> {
  return page.evaluate(hasActionsInPage => {
    const table = document.querySelector('table.table')!;
    const box = (table.closest('.astro-scroll-x, .table-wrap') ?? table.parentElement) as HTMLElement;
    const row = table.querySelector('tbody tr')!;

    const overlap = (a: DOMRect, start: number, end: number) =>
      a.width === 0 ? 0 : Math.max(0, Math.min(a.right, end) - Math.max(a.left, start)) / a.width;

    const boxRect = box.getBoundingClientRect();
    const cell = hasActionsInPage
      ? ([...row.children].pop() as HTMLElement).getBoundingClientRect()
      : new DOMRect(0, 0, 1, 1);

    return {
      insideContainer: hasActionsInPage ? overlap(cell, boxRect.left, boxRect.right) : 1,
      insideViewport: hasActionsInPage ? overlap(cell, 0, window.innerWidth) : 1,
      containerOverflow: box.scrollWidth - box.clientWidth,
      pageOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      tableDisplay: getComputedStyle(table).display,
    };
  }, hasActions);
}

for (const culture of ['ar', 'en'] as const) {
  test.describe(`Tables at ${PHONE.width}px — ${culture}`, () => {
    test.use({ viewport: PHONE });

    for (const screen of SCREENS) {
      test(`${screen.name}: the row's actions are on the screen`, async ({ page }) => {
        await stubAbp(page, { culture, grantedPolicies: EVERY_POLICY });
        await stubEveryList(page);
        await gotoApp(page, screen.path);
        await screen.prepare?.(page);

        await expect(page.locator('table.table tbody tr').first()).toBeVisible();

        const seen = await measure(page, screen.hasActions);

        // Every assertion carries the whole measurement, because one number on
        // its own does not say which way a regression went: an action cell half
        // off the screen and one under a 500px overflow look identical until the
        // container figure is beside them.
        const all = JSON.stringify(seen);

        // Below 40rem the row is a card, so there is nothing left to scroll past.
        expect(seen.tableDisplay, all).toBe('block');

        if (screen.hasActions) {
          // The whole of it, not most of it: a Delete button with a third of
          // itself off the screen is a Delete button somebody mis-taps.
          expect(seen.insideViewport, all).toBeCloseTo(1, 2);
          expect(seen.insideContainer, all).toBeCloseTo(1, 2);
        }

        // Nothing overflows its own box any more, so nothing needs a gesture.
        expect(seen.containerOverflow, all).toBe(0);

        // The invariant. It was already 0 everywhere; it must stay 0.
        expect(seen.pageOverflow, all).toBe(0);
      });
    }

    test('a card names each column in the page language', async ({ page }) => {
      await stubAbp(page, { culture, grantedPolicies: EVERY_POLICY });
      await stubEveryList(page);
      await gotoApp(page, '/exams');

      // Stacking without labels turns the exams row into "30 / 45 / 60", which is
      // why `data-label` exists at all. Read from ::before rather than from the
      // attribute so this fails if the stylesheet is the thing that went missing.
      const label = await page
        .locator('table.table tbody tr')
        .first()
        .locator('td')
        .nth(1)
        .evaluate(td => getComputedStyle(td, '::before').content);

      // textContent, not innerText: the header row is uppercased by the
      // stylesheet, and the label is the string itself rather than how a
      // desktop header happens to be drawn.
      const heading = await page
        .locator('thead th')
        .nth(1)
        .evaluate(th => (th.textContent ?? '').trim());

      expect(heading.length).toBeGreaterThan(0);
      expect(label).toBe(JSON.stringify(heading));

      // And it is the server's string for this culture, not an English word typed
      // into the template — which is the whole reason it is bound.
      if (culture === 'ar') {
        expect(heading).toMatch(/[؀-ۿ]/);
      }
    });
  });
}

test.describe(`Tables at ${DESKTOP.width}px`, () => {
  test.use({ viewport: DESKTOP });

  for (const screen of SCREENS) {
    test(`${screen.name}: still a table`, async ({ page }) => {
      await stubAbp(page, { culture: 'ar', grantedPolicies: EVERY_POLICY });
      await stubEveryList(page);
      await gotoApp(page, screen.path);
      await screen.prepare?.(page);

      await expect(page.locator('table.table tbody tr').first()).toBeVisible();

      const seen = await measure(page, screen.hasActions);

      // The cards rule is a phone rule. Above the breakpoint the header row is
      // back, the columns line up, and nothing generated is showing.
      expect(seen.tableDisplay).toBe('table');
      await expect(page.locator('thead th').first()).toBeVisible();

      const generated = await page
        .locator('table.table tbody tr')
        .first()
        .locator('td')
        .first()
        .evaluate(td => getComputedStyle(td, '::before').content);

      expect(['none', 'normal', '""']).toContain(generated);

      expect(seen.pageOverflow).toBe(0);
    });
  }
});
