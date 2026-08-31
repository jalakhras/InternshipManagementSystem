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

  /** Papers that refuse it exist, and the map must behave differently on them. */
  allowBackNavigation?: boolean;
  secondsRemaining?: number;
  resumable?: boolean;
  isFinal?: boolean;
  isPassed?: boolean;

  /** Sentences a marker wrote for this candidate to read. */
  feedback?: string[];

  /**
   * Refuse the result with this status and no readable body.
   *
   * What a server actually does when a candidate comes back to a result page
   * after their session has ended: a bare 401, with nothing in it a person
   * could read. The screen has to supply the sentence, because nobody else
   * will.
   */
  resultFailsWith?: number;

  /**
   * Lay the paper out in parts, the way a placement test is.
   *
   * Each entry is a section name and how many of the paper's questions sit in
   * it; the counts are laid over the paper in order. Off by default, because
   * most exams are one undivided paper and the rest of this suite is about
   * those.
   */
  sections?: { name: string; questions: number; instructions?: string }[];

  /** Serve free-text questions instead of choices, so there is a box to type in. */
  freeText?: boolean;

  /** Serve hotspot questions: an image to point at, and no regions. */
  hotspot?: boolean;

  /**
   * The address the server puts on the picture.
   * <p>
   * Defaults to an inline `data:` URI so most tests need no network. That
   * default is also why a real defect hid here for weeks: a `data:` URI needs no
   * resolving, so a binding that failed to make the path absolute still worked
   * in every test. Pass the server-relative form to exercise that.
   * </p>
   */
  hotspotImageUrl?: string;

  /** Serve file-upload questions, whose answer is a file rather than text. */
  fileUpload?: boolean;

  /**
   * Serve code questions, with whatever the author wrote on them.
   *
   * `expectsOutput` is the server's word for "this one is marked by comparing
   * text with what the program should print", which is a different question
   * from "write the program" and has to be said to the candidate.
   */
  code?: { language?: string; starterTemplate?: string; expectsOutput?: boolean };

  /**
   * What a human marker will score the answer on.
   *
   * Sent by the server for every free-text, upload and audio question that has
   * one, alongside the question — names and weights only, never the guidance
   * written for the marker.
   */
  rubric?: { name: string; maxScore: number }[];

  /** The centre whose exam this is, as the candidate's first screen shows it. */
  organization?: { name?: string; logoUrl?: string; supportEmail?: string };

  /**
   * Serve one named question type, with a display payload shaped the way the
   * server shapes it.
   *
   * Written so every type this product claims to support can be put in front of
   * a real browser at a real width. Until now four of the thirteen had ever
   * been rendered by a test, which is how a code question came to be answered
   * in an essay box for as long as the type existed.
   */
  ofType?: string;
}

export interface TakeStub {
  /** Every answer the browser sent, so a test can assert what was saved. */
  saved: { questionId: string; response?: string; answerBlobName?: string; answerFileName?: string }[];
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
  const saved: { questionId: string; response?: string; answerBlobName?: string; answerFileName?: string }[] = [];

  let submitted = false;
  let secondsRemaining = options.secondsRemaining ?? 1800;
  const answered = Array.from({ length: total }, () => false);

  await page.route('**/api/assessment/media/answer', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        blobName: 'tenant/answers/a1/stored-file.pdf',
        originalFileName: 'my-work.pdf',
        sizeInBytes: 1024,
      }),
    }),
  );

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
      const body = route.request().postDataJSON() as {
        questionId: string;
        response?: string;
        answerBlobName?: string;
        answerFileName?: string;
      };
      saved.push({
        questionId: body.questionId,
        response: body.response,
        answerBlobName: body.answerBlobName,
        answerFileName: body.answerFileName,
      });

      const index = Number(body.questionId.replace('q', '')) - 1;
      if (index >= 0 && index < answered.length) {
        answered[index] = true;
      }

      const expired = secondsRemaining <= 0;

      return json({
        savedAt: new Date().toISOString(),
        secondsRemaining,
        isExpired: expired,

        // What the real server does: past the deadline nothing is written unless
        // the save carries only a file that was already on its way. `saved` and
        // `isExpired` are not opposites, and the screen used to assume they were.
        saved: !expired || Boolean(body?.answerBlobName && !body?.response),
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
      if (options.resultFailsWith) {
        return route.fulfill({
          status: options.resultFailsWith,
          contentType: 'text/plain',
          body: '',
        });
      }

      return json(result());
    }

    // Opening the link: /take/{token}
    return json({
      isAccessible: options.accessible !== false,
      blockReason: options.blockReason,
      examTitle: 'Spanish B1 Placement',
      description: 'Reading, listening and grammar.',
      candidateName: 'Layla',
      organizationName: options.organization?.name,
      organizationLogoUrl: options.organization?.logoUrl,
      organizationSupportEmail: options.organization?.supportEmail,
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
      allowBackNavigation: options.allowBackNavigation ?? true,
      oneQuestionAtATime: true,
      organizationSupportEmail: options.organization?.supportEmail,
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
    const id = `q${number}`;

    return {
      id,
      position,
      totalQuestions: total,
      text: `Question ${number}: which level is support?`,
      type: options.ofType
        ? options.ofType
        : options.code
        ? 'code'
        : options.fileUpload
          ? 'file-upload'
          : options.hotspot
            ? 'hotspot'
            : options.freeText
              ? 'text'
              : 'single-choice',
      score: 1,
      section: sectionAt(position),
      options: options.ofType
        ? (['single-choice', 'multi-select', 'true-false'].includes(options.ofType)
            ? [
                { id: 'a', text: 'الخيار الأوّل، وهو نصٌّ عربيٌّ طويلٌ بما يكفي ليلتفّ على شاشة هاتف' },
                { id: 'b', text: 'الخيار الثاني' },
              ]
            : [])
        : options.freeText
        ? []
        : [
            { id: 'a', text: 'The level price failed to fall below' },
            { id: 'b', text: 'The level price failed to rise above' },
          ],
      // A 400x300 chart, inline, so the test needs no network and the frame has
      // a real area to point within — a one-pixel image gives the click nothing
      // to be a percentage of.
      display: options.ofType
        ? displayFor(options.ofType)
        : options.rubric
        ? { criteria: options.rubric }
        : options.code
        ? {
            language: options.code.language,
            starterTemplate: options.code.starterTemplate,
            expectsOutput: options.code.expectsOutput === true,
          }
        : options.hotspot
          ? { imageUrl: options.hotspotImageUrl ?? 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI0MDAiIGhlaWdodD0iMzAwIj48cmVjdCB3aWR0aD0iNDAwIiBoZWlnaHQ9IjMwMCIgZmlsbD0iI2RmZTZlYyIvPjxsaW5lIHgxPSIwIiB5MT0iMjIwIiB4Mj0iNDAwIiB5Mj0iMjIwIiBzdHJva2U9IiMzNTYiIHN0cm9rZS13aWR0aD0iMyIvPjwvc3ZnPg==' }
          : {},
      // What this candidate has already written here.
      //
      // It used to be hard-coded empty, which meant no test in the suite could
      // check that an answer survives being navigated away from — the stub had
      // no way to say "you already answered this". The real server has always
      // returned it; the stub simply could not represent it, so a whole
      // behaviour was untestable and therefore untested.
      savedResponse: saved.filter(a => a.questionId === id).at(-1)?.response,
      savedFileName: saved.filter(a => a.questionId === id).at(-1)?.answerFileName,
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
      // What the marker wrote. Empty unless a test asks for it, because a
      // candidate nobody wrote to must not see an empty heading where feedback
      // would be.
      feedback: options.feedback ?? [],

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

/**
 * The display payload the server sends for one type, in the same shape.
 *
 * Arabic text on purpose: this product is Arabic first, and a control that
 * only ever meets Latin text in a test has never been checked in the direction
 * nearly every candidate reads.
 */
function displayFor(type: string): Record<string, unknown> {
  switch (type) {
    case 'ordering':
      return {
        items: [
          { id: 'i1', text: 'افتح الرسالة' },
          { id: 'i2', text: 'تحقّق من المرسِل' },
          { id: 'i3', text: 'اضغط الرابط' },
        ],
      };

    case 'matching':
      return {
        left: [
          { id: 'l1', text: 'الرياض' },
          { id: 'l2', text: 'القاهرة' },
        ],
        right: [
          { id: 'r1', text: 'مصر' },
          { id: 'r2', text: 'السعوديّة' },
        ],
      };

    case 'scale':
      return { min: 1, max: 5, minLabel: 'لا أوافق إطلاقاً', maxLabel: 'أوافق تماماً' };

    case 'numeric':
      return { unit: 'ريال' };

    case 'fill-in-the-blank':
      return { blankIds: ['b1', 'b2'] };

    case 'code':
      return { language: 'Python', starterTemplate: 'def total(prices):\n    return 0', expectsOutput: false };

    case 'hotspot':
      return {
        imageUrl:
          'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI0MDAiIGhlaWdodD0iMzAwIj48cmVjdCB3aWR0aD0iNDAwIiBoZWlnaHQ9IjMwMCIgZmlsbD0iI2RmZTZlYyIvPjwvc3ZnPg==',
      };

    default:
      return {};
  }
}
