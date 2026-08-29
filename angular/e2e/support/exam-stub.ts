import { Page } from '@playwright/test';

/**
 * Stubs the exam endpoints.
 *
 * Filtering is applied here rather than always returning the same rows, because
 * one of the tests is specifically about the list distinguishing "nothing yet"
 * from "nothing matches" — a stub that ignored the filter would report that
 * distinction as working whichever way the screen behaved.
 */
export interface ExamStubOptions {
  items?: ExamRow[];

  /** Reject with this message, to check the error state carries the reason. */
  failWith?: string;
}

export interface ExamRow {
  id: string;
  title: string;
  categoryName?: string;
  levelName?: string;
  status: number;
  mode: number;
  timeLimitInMinutes: number;
  passingPercentage: number;
  questionsPerForm?: number;
  questionCount: number;
  creationTime: string;
}

const DEFAULT_ROWS: ExamRow[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    title: 'Spanish B1 Placement',
    categoryName: 'Spanish',
    levelName: 'B1',
    status: 1,
    mode: 0,
    timeLimitInMinutes: 45,
    passingPercentage: 60,
    // Everyone sits the same paper: no draw, so the cell shows one number.
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
    // Drawn from a larger bank, so two candidates get different papers.
    questionsPerForm: 25,
    questionCount: 120,
    creationTime: '2026-08-10T09:00:00Z',
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    title: 'Onboarding Safety Refresher',
    categoryName: 'Compliance',
    status: 0,
    mode: 1,
    timeLimitInMinutes: 20,
    passingPercentage: 80,
    questionCount: 12,
    creationTime: '2026-08-20T09:00:00Z',
  },
];

export async function stubExams(page: Page, options: ExamStubOptions = {}): Promise<void> {
  const rows = options.items ?? DEFAULT_ROWS;

  await page.route('**/api/assessment/exams**', route => {
    if (options.failWith) {
      return route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: { message: options.failWith } }),
      });
    }

    const url = new URL(route.request().url());
    const filter = (url.searchParams.get('filter') ?? '').toLowerCase();
    const status = url.searchParams.get('status');

    let matched = rows;

    if (filter) {
      matched = matched.filter(r => r.title.toLowerCase().includes(filter));
    }

    if (status !== null && status !== '') {
      matched = matched.filter(r => r.status === Number(status));
    }

    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ totalCount: matched.length, items: matched }),
    });
  });
}

/** Options for the single-exam endpoints. */
export interface ExamDetailStubOptions {
  exam?: Partial<ExamRow>;
  publishCheck?: {
    canPublish: boolean;
    blockers: string[];
    warnings: string[];
    questionCount: number;
    totalScore: number;
    formLength: number;
  };
}

/**
 * Stubs the detail, publish-check and publish endpoints.
 *
 * Registered before the list route matters: Playwright matches last-registered
 * first, and `**\/api/assessment/exams**` would otherwise swallow `/exams/{id}`.
 */
export async function stubExamDetail(page: Page, options: ExamDetailStubOptions = {}): Promise<void> {
  const exam: ExamRow & Record<string, unknown> = {
    id: '11111111-1111-1111-1111-111111111111',
    title: 'Spanish B1 Placement',
    description: 'Placement test for the autumn intake.',
    categoryName: 'Spanish',
    levelName: 'B1',
    status: 0,
    mode: 0,
    timeLimitInMinutes: 45,
    passingPercentage: 60,
    questionCount: 30,
    creationTime: '2026-08-01T09:00:00Z',
    shuffleQuestions: true,
    shuffleOptions: true,
    oneQuestionAtATime: true,
    allowBackNavigation: true,
    collectIntegritySignals: true,
    isScheduled: false,
    ...options.exam,
  };

  const check = options.publishCheck ?? {
    canPublish: true,
    blockers: [],
    warnings: [],
    questionCount: 30,
    totalScore: 30,
    formLength: 30,
  };

  await page.route('**/api/assessment/exams/*/publish-check', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(check) }),
  );

  await page.route('**/api/assessment/exams/*/publish', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ ...exam, status: 1 }),
    }),
  );

  await page.route('**/api/assessment/exams/*', route => {
    if (route.request().method() !== 'GET') {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(exam) });
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(exam) });
  });
}
