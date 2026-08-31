import { expect, request as playwrightRequest, test } from '@playwright/test';
import { API, send, signIn, unique } from './api';

/**
 * Two taps on "Start", arriving together.
 *
 * The commonest thing a nervous person does on a phone, and the commonest thing
 * a flaky connection does on their behalf: the request is retried while the
 * first is still in flight.
 *
 * The code reads as if this were handled — it looks for a running attempt and
 * resumes it rather than making a second — and the database backs that up with
 * a unique index over unsubmitted attempts per link. But the look and the
 * insert are two statements, and two requests can pass the look before either
 * reaches the insert. Then the index does its job on whichever loses, and the
 * question this test exists to answer is **what that candidate sees**.
 *
 * The stakes are not cosmetic. The same method spends an attempt from the link,
 * and a link with one attempt that gets spent twice is a candidate locked out
 * of an exam they never sat.
 *
 * Live on purpose. This cannot be asked of the test database: SQLite serialises
 * writes, so the race the index exists for cannot happen there — a test that
 * passed against it would be proving nothing.
 */
test.describe('Two starts at once', () => {
  test('a double tap produces one sitting, not two', async () => {
    test.setTimeout(120_000);

    const { ctx } = await signIn();

    const category = await send<{ id: string }>(ctx, 'post', '/api/assessment/catalog/categories', {
      code: unique('race'),
      name: 'تزامن',
    });

    const exam = await send<{ id: string }>(ctx, 'post', '/api/assessment/exams', {
      title: 'ضغطتان معاً',
      timeLimitInMinutes: 30,
      passingPercentage: 50,
      categoryId: category.id,
    });

    await send(ctx, 'post', '/api/assessment/questions', {
      examId: exam.id,
      type: 'true-false',
      text: 'ضغطتُ مرّتين.',
      score: 1,
      payload: JSON.stringify({
        options: [
          { id: 'a', text: 'نعم', isCorrect: true },
          { id: 'b', text: 'لا', isCorrect: false },
        ],
      }),
    });

    await send(ctx, 'post', `/api/assessment/exams/${exam.id}/publish`);

    const candidate = await send<{ id: string }>(ctx, 'post', '/api/assessment/candidates', {
      fullName: 'ضغط مرّتين',
      email: unique('race') + '@example.test',
    });

    const assignment = await send<{ recipients: { url: string }[] }>(
      ctx,
      'post',
      '/api/assessment/assignments',
      {
        examId: exam.id,
        candidateId: candidate.id,
        expiresAt: new Date(Date.now() + 7 * 86_400_000).toISOString(),
        // One attempt, which is the setting that makes a double spend fatal.
        maxAttempts: 1,
        sendEmail: false,
      },
    );

    const token = assignment.recipients[0].url.split('/').pop()!;

    // Opened once, the way a candidate does. The session it mints is what both
    // taps would carry.
    const anon = await playwrightRequest.newContext({ ignoreHTTPSErrors: true, baseURL: API });
    const opened = await anon.get(`/api/assessment/take/${token}`);

    expect(opened.ok()).toBe(true);

    const session = (await opened.json()).sessionToken as string;

    // Fired together rather than one after the other. Sequentially the second
    // finds the first and resumes, which is the path already covered — the
    // interesting one is both arriving before either has written anything.
    const taps = await Promise.all(
      [0, 1, 2, 3, 4].map(() =>
        anon.post('/api/assessment/take/start', { headers: { 'X-Exam-Session': session } }),
      ),
    );

    const statuses = taps.map(r => r.status());

    // Nobody gets a server error. A candidate who taps twice at the moment they
    // begin an exam must not be shown a failure page: they cannot tell whether
    // their exam started, and the clock may already be running.
    expect(statuses.filter(s => s >= 500)).toEqual([]);

    const bodies = await Promise.all(
      taps.filter(r => r.ok()).map(async r => (await r.json()) as { attemptId: string }),
    );

    // One sitting. Every response that succeeded describes the same one.
    const distinct = new Set(bodies.map(b => b.attemptId));

    expect(bodies.length).toBeGreaterThan(0);
    expect(distinct.size).toBe(1);

    // And one attempt spent. A link good for one sitting that records two has
    // locked the candidate out of an exam they never sat — and nothing in the
    // product would explain why.
    const links = await send<{ items: { attemptsUsed: number }[] }>(
      ctx,
      'get',
      `/api/assessment/assignments/links/${exam.id}`,
    );

    expect(links.items[0].attemptsUsed).toBe(1);

    await anon.dispose();
  });
});
