import { expect, request, test } from '@playwright/test';
import { API, Coordinator, send, signIn, unique } from './api';

/**
 * The whole product, once, against a real server.
 *
 * Every defect found on 2026-08-29 was invisible to 187 unit and integration
 * tests and to the stubbed browser suite, and each for the same reason: it did
 * not live inside a layer. A session credential that was never replaced after
 * the start. A media route that no controller declared. A BLOB container with no
 * provider behind it. An `[Authorize]` naming a policy nobody had defined. Those
 * are properties of a wired-up application, and only a wired-up application has
 * them.
 *
 * So this test is deliberately one long journey rather than several small ones.
 * The value is in the joins.
 *
 * Run with the host and the Angular server up:
 *
 *   npx playwright test --project=live
 */
test.describe('A cohort sits an exam, end to end', () => {
  // A real exam with a real database behind it; the stubbed suite's five seconds
  // does not apply.
  test.setTimeout(180_000);

  let admin: Coordinator;

  test.beforeAll(async () => {
    admin = await signIn();
  });

  test('from an empty catalogue to a result a coordinator can read', async ({ page, context }) => {
    const ctx = admin.ctx;
    const code = unique('live');

    // ---------------------------------------------------------- the catalogue
    // First, because everything else is filed against it. Until this screen
    // existed nothing could put a row in these tables, and the shared bank —
    // whose whole rule is "same domain, same level" — was unreachable.
    const category = await send<{ id: string }>(ctx, 'post', '/api/assessment/catalog/categories', {
      name: 'Live English',
      code,
      displayOrder: 0,
      isActive: true,
    });

    const level = await send<{ id: string }>(ctx, 'post', '/api/assessment/catalog/levels', {
      categoryId: category.id,
      name: 'A1',
      code: `${code}-a1`,
      displayOrder: 1,
      isActive: true,
    });

    const topic = await send<{ id: string }>(ctx, 'post', '/api/assessment/catalog/topics', {
      categoryId: category.id,
      name: 'Grammar',
      code: `${code}-grammar`,
      displayOrder: 0,
      isActive: true,
    });

    // ---------------------------------------------------------------- the exam
    const exam = await send<{ id: string }>(ctx, 'post', '/api/assessment/exams', {
      title: `Live placement ${code}`,
      timeLimitInMinutes: 30,
      passingPercentage: 50,
      categoryId: category.id,
      levelId: level.id,
      shuffleQuestions: false,
      shuffleOptions: true,
      oneQuestionAtATime: true,
      allowBackNavigation: true,
    });

    const questionIds: string[] = [];

    for (let i = 0; i < 3; i++) {
      const question = await send<{ id: string }>(ctx, 'post', '/api/assessment/questions', {
        examId: exam.id,
        topicId: topic.id,
        type: 'single-choice',
        text: `Live question ${i + 1}`,
        score: 1,
        difficulty: 1,
        displayOrder: i,
        isActive: true,
        payload: JSON.stringify({
          options: [
            { id: 'a', text: 'Right', isCorrect: true },
            { id: 'b', text: 'Wrong', isCorrect: false },
          ],
        }),
      });

      questionIds.push(question.id);
    }

    await send(ctx, 'post', `/api/assessment/exams/${exam.id}/publish`);

    // --------------------------------------------------------------- the paper
    // Two candidates on one paper is the whole reason a named form exists: two
    // scores mean the same thing only if the papers behind them did.
    const form = await send<{ id: string }>(ctx, 'post', '/api/assessment/exam-structure/forms', {
      examId: exam.id,
      name: 'Form 1',
      code: `${code}-f1`,
    });

    await send(ctx, 'put', `/api/assessment/exam-structure/forms/${form.id}/questions`, {
      questionIds,
    });

    await send(ctx, 'post', `/api/assessment/exam-structure/forms/${form.id}/publish`);

    // ------------------------------------------------------------ the sitting
    const candidate = await send<{ id: string }>(ctx, 'post', '/api/assessment/candidates', {
      fullName: 'Live Candidate',
      email: `${code}@example.test`,
    });

    const assignment = await send<{ recipients: { url: string }[] }>(
      ctx,
      'post',
      '/api/assessment/assignments',
      {
        examId: exam.id,
        examFormId: form.id,
        candidateId: candidate.id,
        expiresAt: new Date(Date.now() + 7 * 864e5).toISOString(),
        maxAttempts: 1,
        sendEmail: false,
      },
    );

    const linkToken = assignment.recipients[0].url.split('/').pop()!;

    // ---------------------------------------------------- the candidate's turn
    // In the browser, because this half is about the browser: whether the entry
    // screen renders, whether the start hands back a usable session, whether
    // each question loads after it.
    await page.goto(`/exam/${linkToken}`);

    await expect(page.getByRole('button', { name: /ابدأ|Start/ })).toBeVisible();

    // Nothing has been consumed by looking.
    await expect(page.getByRole('timer')).toHaveCount(0);

    await page.getByRole('button', { name: /ابدأ|Start/ }).click();

    // The defect this catches: StartAsync did not issue a credential for the
    // attempt it created, so every request after this one asked about attempt
    // 00000000-0000-… and the first question never arrived. The stubbed suite
    // could not see it — it answers the requests itself.
    await expect(page.getByText('Live question 1')).toBeVisible({ timeout: 20_000 });

    for (let i = 0; i < 3; i++) {
      await expect(page.getByText(`Live question ${i + 1}`)).toBeVisible();

      await page.getByRole('radio', { name: 'Right' }).click();

      if (i < 2) {
        await page.getByRole('button', { name: /^(التالي|Next)$/ }).click();
      }
    }

    // "Finish" opens the confirmation; "Submit" inside it is the irreversible
    // one. Two steps on purpose — a mis-click here costs somebody their exam.
    await page.getByRole('button', { name: /إنهاء|Finish/ }).click();
    await page.getByRole('button', { name: /^(تسليم|Submit)$/ }).click();

    // Every question answered correctly, so the candidate's own result says so —
    // and says which skill, which is the part a training centre acts on.
    const score = page.locator('.score');

    await expect(score).toBeVisible({ timeout: 20_000 });
    await expect(score).toContainText('100%');

    // The pass verdict as a class rather than as text, so this assertion does not
    // move every time somebody rewords a label in two languages.
    await expect(score).toHaveClass(/score--passed/);

    // And the skill breakdown, which is the part a training centre acts on.
    await expect(page.locator('.breakdown')).toContainText('Grammar');

    // ------------------------------------------------------ what staff can read
    // The half the product was missing entirely: the review queue lists only
    // sittings that need a person, so an all-multiple-choice paper was marked in
    // milliseconds and then appeared on no screen at all.
    const results = await send<{ items: Record<string, unknown>[]; totalCount: number }>(
      ctx,
      'get',
      `/api/assessment/results?examId=${exam.id}`,
    );

    expect(results.totalCount).toBe(1);

    const row = results.items[0] as {
      candidateEmail: string;
      isPassed: boolean;
      isGraded: boolean;
      formName: string | null;
      scorePercentage: number;
    };

    expect(row.candidateEmail).toBe(`${code}@example.test`);
    expect(row.isGraded).toBe(true);
    expect(row.isPassed).toBe(true);
    expect(row.scorePercentage).toBe(100);

    // Which paper was served, recorded at the time rather than inferred later.
    expect(row.formName).toBe('Form 1');

    // The topic breakdown: the part a training centre acts on.
    const detail = await send<{ byTopic: { topicName: string }[] }>(
      ctx,
      'get',
      `/api/assessment/results/${(results.items[0] as { attemptId: string }).attemptId}`,
    );

    expect(detail.byTopic.map(t => t.topicName)).toContain('Grammar');

    // And the export, because the next thing that happens to a set of results is
    // that somebody puts them in a spreadsheet.
    const csv = await ctx.get(`/api/assessment/results/export?examId=${exam.id}`);

    expect(csv.ok()).toBe(true);
    expect(await csv.text()).toContain('Live Candidate');
  });

  test('a stored file is actually served', async () => {
    // Not a screen test. Every image, listening clip and uploaded answer in the
    // product goes through this one route, and for a while nothing did: no
    // controller declared it, and behind that the BLOB container had no provider
    // configured. Both failures are invisible to a test that calls the service.
    const ctx = admin.ctx;

    const upload = await ctx.post('/api/assessment/media', {
      multipart: {
        file: {
          name: 'pixel.png',
          mimeType: 'image/png',
          // A one-pixel PNG, small enough to inline and real enough to store.
          buffer: Buffer.from(
            'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
            'base64',
          ),
        },
      },
    });

    expect(upload.ok(), await upload.text()).toBe(true);

    const { blobName } = (await upload.json()) as { blobName: string };

    const fetched = await ctx.get(`/api/assessment/media/${blobName}`);

    expect(fetched.status()).toBe(200);
    expect(fetched.headers()['content-type']).toContain('image/png');

    // Told exactly what it is, and not allowed to guess otherwise: these files
    // are shown inside an exam that other people's answers pass through.
    expect(fetched.headers()['x-content-type-options']).toBe('nosniff');
  });

  test('an anonymous stranger cannot read a stored file', async () => {
    const ctx = admin.ctx;

    const upload = await ctx.post('/api/assessment/media', {
      multipart: {
        file: {
          name: 'secret.png',
          mimeType: 'image/png',
          buffer: Buffer.from(
            'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
            'base64',
          ),
        },
      },
    });

    const { blobName } = (await upload.json()) as { blobName: string };

    // A context with no Authorization header at all. The development server's
    // certificate is self-signed, so this cannot use Playwright's default
    // request fixture.
    const anonymous = await request.newContext({ ignoreHTTPSErrors: true, baseURL: API });

    // No token and no grant. 404 rather than 403 on purpose: whether a
    // particular blob exists is itself worth not saying.
    const stolen = await anonymous.get(`/api/assessment/media/${blobName}`);

    expect(stolen.status()).toBe(404);
  });
});
