import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Page } from '@playwright/test';

/**
 * Stubs the taking API.
 *
 * The candidate's journey has no OAuth in it — the whole credential is the token
 * in the URL, exchanged once for a session header — so this stub is much smaller
 * than the staff one and does not touch the shell.
 */
export interface TakeStubOptions {
  culture?: 'ar' | 'en';
  accessible?: boolean;
  blockReason?: string;
  totalQuestions?: number;
  secondsRemaining?: number;
  resumable?: boolean;
  isFinal?: boolean;
  isPassed?: boolean;

  /**
   * Lay the paper out in parts, the way a placement test is.
   *
   * Each entry is a section name and how many of the paper's questions sit in
   * it; the counts are laid over the paper in order. Off by default, because
   * most exams are one undivided paper and the rest of this suite is about
   * those.
   */
  sections?: { name: string; questions: number; instructions?: string }[];
}

export interface TakeStub {
  /** Every answer the browser sent, so a test can assert what was saved. */
  saved: { questionId: string; response?: string }[];
  submitted: () => boolean;
  expireNow: () => void;
}

export async function stubTake(page: Page, options: TakeStubOptions = {}): Promise<TakeStub> {
  // The application still boots through ABP, which blocks the first render until
  // its configuration and localisation answer. A candidate has no account, so
  // this is the smallest configuration that lets the app start: no user, no
  // permissions, and the strings the taker screens read.
  await stubMinimalAbp(page, options.culture ?? 'en');

  const total = options.sections
    ? options.sections.reduce((sum, part) => sum + part.questions, 0)
    : options.totalQuestions ?? 3;
  const saved: { questionId: string; response?: string }[] = [];

  let submitted = false;
  let secondsRemaining = options.secondsRemaining ?? 1800;
  const answered = Array.from({ length: total }, () => false);

  await page.route('**/api/assessment/take/**', async route => {
    const url = new URL(route.request().url());
    const path = url.pathname.replace('/api/assessment/take', '');
    const method = route.request().method();

    const json = (body: unknown, status = 200) =>
      route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });

    if (method === 'POST' && path === '/start') {
      return json(state());
    }

    if (method === 'GET' && path === '/state') {
      return json(state());
    }

    if (method === 'GET' && path.startsWith('/question/')) {
      const position = Number(path.split('/').pop());

      // Refused the way the real service refuses it, rather than inventing a
      // question that does not exist on this paper.
      if (!Number.isInteger(position) || position < 0 || position >= total) {
        return route.fulfill({
          status: 403,
          contentType: 'application/json',
          body: JSON.stringify({
            error: { code: 'IMS:Attempt:QuestionNotOnForm', message: 'Not on this paper.' },
          }),
        });
      }

      return json(question(position));
    }

    if (method === 'PUT' && path === '/answer') {
      const body = route.request().postDataJSON() as { questionId: string; response?: string };
      saved.push({ questionId: body.questionId, response: body.response });

      const index = Number(body.questionId.replace('q', '')) - 1;
      if (index >= 0 && index < answered.length) {
        answered[index] = true;
      }

      return json({
        savedAt: new Date().toISOString(),
        secondsRemaining,
        isExpired: secondsRemaining <= 0,
      });
    }

    if (method === 'POST' && path === '/signal') {
      return route.fulfill({ status: 204, body: '' });
    }

    if (method === 'POST' && path === '/submit') {
      submitted = true;
      return json(result());
    }

    if (method === 'GET' && path === '/result') {
      return json(result());
    }

    // Opening the link: /take/{token}
    return json({
      isAccessible: options.accessible !== false,
      blockReason: options.blockReason,
      examTitle: 'Spanish B1 Placement',
      description: 'Reading, listening and grammar.',
      candidateName: 'Layla',
      timeLimitInMinutes: 30,
      questionCount: total,
      attemptsAllowed: 2,
      attemptsUsed: options.resumable ? 1 : 0,
      expiresAt: '2026-12-31T23:59:00Z',
      mode: 0,
      resumableAttemptId: options.resumable ? 'a1' : undefined,
      sessionToken: options.accessible === false ? undefined : 'e2e-session',
    });
  });

  function state() {
    return {
      attemptId: 'a1',
      secondsRemaining,
      totalQuestions: total,
      answeredCount: answered.filter(Boolean).length,
      answered: [...answered],
      isSubmitted: submitted,
      allowBackNavigation: true,
      oneQuestionAtATime: true,
    };
  }

  /**
   * One question, at a zero-based position — the same numbering the real API
   * uses.
   *
   * It used to echo whatever number it was asked for, which made the stub agree
   * with any client. That is how a real off-by-one survived: the sitting screen
   * sent its own one-based display position straight through, so against the
   * real server every candidate was served the second question first and could
   * never reach the first. A stub that answers anything proves nothing.
   */
  function question(position: number) {
    const number = position + 1;

    return {
      id: `q${number}`,
      position,
      totalQuestions: total,
      text: `Question ${number}: which level is support?`,
      type: 'single-choice',
      score: 1,
      section: sectionAt(position),
      options: [
        { id: 'a', text: 'The level price failed to fall below' },
        { id: 'b', text: 'The level price failed to rise above' },
      ],
      display: {},
      savedResponse: undefined,
    };
  }

  /**
   * Which part a position falls in, shaped the way the server shapes it.
   *
   * The instructions are attached to the section's first question and to no
   * other, because that is the server's rule — they are written to be read
   * before a part begins. A stub that attached them everywhere would let a
   * client that shows them on every question pass.
   */
  function sectionAt(position: number) {
    if (!options.sections) {
      return undefined;
    }

    let start = 0;

    for (const part of options.sections) {
      if (position < start + part.questions) {
        const within = position - start + 1;

        return {
          id: part.name.toLowerCase(),
          name: part.name,
          instructions: within === 1 ? part.instructions : undefined,
          position: within,
          questionCount: part.questions,
          isFirstQuestion: within === 1,
        };
      }

      start += part.questions;
    }

    return undefined;
  }

  function result() {
    return {
      attemptId: 'a1',
      examTitle: 'Spanish B1 Placement',
      isFinal: options.isFinal !== false,
      score: 8,
      maxScore: 10,
      scorePercentage: 80,
      isPassed: options.isPassed !== false,
      submittedAt: '2026-08-29T10:00:00Z',
      topicBreakdown: [
        { topicId: 't1', topicName: 'Reading', score: 4, maxScore: 5, percentage: 80 },
        { topicId: 't2', topicName: 'Listening', score: 4, maxScore: 5, percentage: 80 },
      ],
      // The parts the candidate actually sat, when the paper had parts. Numbers
      // deliberately unlike the topic ones, so a screen reading the wrong array
      // is visible rather than coincidentally right.
      sectionBreakdown: (options.sections ?? []).map((part, index) => ({
        sectionId: part.name.toLowerCase(),
        sectionName: part.name,
        questionCount: part.questions,
        score: index === 0 ? 3 : 1,
        maxScore: part.questions,
        percentage: index === 0 ? 95 : 35,
      })),
      review: [],
    };
  }

  return {
    saved,
    submitted: () => submitted,
    expireNow: () => {
      secondsRemaining = 0;
    },
  };
}

/**
 * Enough ABP for the application to boot without a signed-in user.
 */
async function stubMinimalAbp(page: Page, culture: string): Promise<void> {
  const texts = readTexts(culture);

  await page.route('**/api/abp/application-configuration*', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        localization: {
          currentCulture: {
            cultureName: culture,
            name: culture,
            displayName: culture === 'ar' ? 'العربية' : 'English',
            twoLetterIsoLanguageName: culture,
            isRightToLeft: culture === 'ar',
            dateTimeFormat: {},
          },
          languages: [
            { cultureName: 'ar', uiCultureName: 'ar', displayName: 'العربية' },
            { cultureName: 'en', uiCultureName: 'en', displayName: 'English' },
          ],
          values: { InternshipManagementSystem: texts },
          resources: { InternshipManagementSystem: { texts, baseResources: [] } },
          defaultResourceName: 'InternshipManagementSystem',
          languagesMap: {},
          languageFilesMap: {},
        },
        auth: { grantedPolicies: {}, policies: {} },
        currentUser: { isAuthenticated: false, id: null, userName: null, roles: [] },
        setting: { values: {} },
        features: { values: {} },
        globalFeatures: { enabledFeatures: [] },
        multiTenancy: { isEnabled: true },
        currentTenant: { id: null, name: null, isAvailable: false },
        timing: { timeZone: { iana: { timeZoneName: 'Asia/Riyadh' }, windows: {} } },
        clock: { kind: 'Local' },
        objectExtensions: { modules: {}, enums: {} },
      }),
    }),
  );

  await page.route('**/api/abp/application-localization*', route => {
    const asked = new URL(route.request().url()).searchParams.get('cultureName') ?? culture;
    const resource = readTexts(asked);

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        resources: { InternshipManagementSystem: { texts: resource, baseResources: [] } },
      }),
    });
  });
}

/**
 * The server's own resource files, read from disk.
 *
 * Reading them rather than restating them: a stub that drifts from the real
 * strings turns a passing test into a statement about the stub.
 */
function readTexts(culture: string): Record<string, string> {
  const path = join(
    __dirname,
    '../../../src/InternshipManagementSystem.Domain.Shared/Localization/InternshipManagementSystem',
    `${culture}.json`,
  );

  return JSON.parse(readFileSync(path, 'utf8')).texts as Record<string, string>;
}
