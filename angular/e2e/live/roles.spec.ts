import { APIRequestContext, expect, request, test } from '@playwright/test';
import { API } from './api';

/**
 * What each role may do, and — for the first time in this project — what it may not.
 *
 * Every permission in this product had been granted and none had ever been
 * withheld. The only role was Admin, which holds all of them, and the integration
 * suite calls `AddAlwaysAllowAuthorization`, so no `[Authorize]` in the solution
 * was ever executed by a test. `AuthorizationCoverageTests` closes half of that
 * gap statically — every service is guarded, every policy is defined, every
 * defined permission is enforced somewhere — but a static check cannot tell you
 * that a coordinator is actually refused when they try to write a question. Only
 * a signed-in request can, and this is the file that makes them.
 *
 * The negative assertions are the point. A test that only checks the happy path
 * of a permission passes just as well when the permission is missing from the
 * service altogether, which is the defect that has shipped here twice.
 *
 * Needs the host running, `node tools/seed-tenants.js`, the DbMigrator (which
 * seeds the roles), and `node tools/seed-role-users.js` (which creates the
 * accounts).
 *
 *   npx playwright test --project=live roles
 */
test.describe('What each role can do, and what it is refused', () => {
  test.setTimeout(120_000);

  // One organisation is enough for a permission question — the roles are seeded
  // identically into all three, and tenancy.spec.ts is where isolation is
  // proved. The language centre, because it has the most seeded data behind it.
  const TENANT = 'language-centre';

  const ROLES = ['coordinator', 'author', 'marker', 'observer'] as const;

  type Role = (typeof ROLES)[number];

  /** A signed-in request context per role, plus the tenant admin for setup. */
  const as = new Map<Role | 'admin', APIRequestContext>();

  async function contextFor(username: string, password: string): Promise<APIRequestContext> {
    // The development certificate is self-signed, so this cannot use the default
    // request fixture.
    const anonymous = await request.newContext({ ignoreHTTPSErrors: true, baseURL: API });

    const auth = await anonymous.post('/connect/token', {
      headers: { __tenant: TENANT },
      form: {
        grant_type: 'password',
        username,
        password,
        client_id: 'InternshipManagementSystem_App',
        scope: 'InternshipManagementSystem offline_access openid profile',
      },
    });

    if (!auth.ok()) {
      throw new Error(
        `Could not sign in as "${username}" @ ${TENANT}. Run: node tools/seed-role-users.js\n`
          + (await auth.text()),
      );
    }

    return request.newContext({
      ignoreHTTPSErrors: true,
      baseURL: API,
      extraHTTPHeaders: {
        Authorization: `Bearer ${(await auth.json()).access_token}`,
        __tenant: TENANT,
      },
    });
  }

  test.beforeAll(async () => {
    as.set('admin', await contextFor('admin', '1q2w3E*'));

    for (const role of ROLES) {
      as.set(role, await contextFor(role, '1q2w3E*'));
    }
  });

  /**
   * Refused, and refused as a permission rather than as anything else.
   *
   * 403 specifically, never merely "not 2xx". A 401 would mean the token did not
   * arrive and the test proves nothing about permissions; a 500 is what ASP.NET
   * answers when an `[Authorize]` names a policy nobody defined, which is a
   * different defect wearing the same clothes and has shipped here before.
   */
  async function refused(
    role: Role,
    method: 'get' | 'post' | 'put' | 'delete',
    url: string,
    body?: unknown,
  ): Promise<void> {
    const res = await as.get(role)![method](url, body === undefined ? undefined : { data: body });

    expect(
      res.status(),
      `${role} was not refused ${method.toUpperCase()} ${url} — got ${res.status()}`,
    ).toBe(403);
  }

  /** Allowed. Not 403, and not 401 or 500 either. */
  async function allowed(
    role: Role | 'admin',
    method: 'get' | 'post' | 'put' | 'delete',
    url: string,
    body?: unknown,
  ) {
    const res = await as.get(role)![method](url, body === undefined ? undefined : { data: body });

    expect(
      res.status(),
      `${role} was refused ${method.toUpperCase()} ${url}: ${await res.text()}`,
    ).toBeLessThan(400);

    return res;
  }

  /** A suffix that keeps one run's rows apart from the last one's. */
  const unique = (prefix: string) =>
    `${prefix}-${Date.now().toString(36)}${Math.floor(Math.random() * 1e4).toString(36)}`;

  // ------------------------------------------------------------------- author

  test('an author writes exams and questions', async () => {
    const exam = await (
      await allowed('author', 'post', '/api/assessment/exams', {
        title: unique('اختبار الصلاحيات'),
        timeLimitInMinutes: 20,
        passingPercentage: 50,
        shuffleQuestions: false,
        shuffleOptions: false,
        oneQuestionAtATime: false,
        allowBackNavigation: true,
        collectIntegritySignals: false,
      })
    ).json();

    await allowed('author', 'post', '/api/assessment/questions', {
      examId: exam.id,
      type: 'single-choice',
      text: 'هل يستطيع المُعِدّ كتابة سؤال؟',
      score: 1,
      difficulty: 1,
      displayOrder: 0,
      isActive: true,
      payload: JSON.stringify({
        options: [
          { id: 'a', text: 'نعم', isCorrect: true },
          { id: 'b', text: 'لا', isCorrect: false },
        ],
      }),
    });

    // The catalogue is the author's own vocabulary: a question is tagged to a
    // topic and an exam sits at a level, so writing exams without it is writing
    // them untagged.
    await allowed('author', 'get', '/api/assessment/catalog/categories');

    // Taken away again, which exercises Exams.Delete and — more to the point —
    // leaves the organisation as this test found it. A draft exam left behind
    // sorts ahead of the seeded published one, and the next spec to reach for
    // "the first exam" gets a draft it cannot send. Cleaning up is not tidiness
    // here; a live suite that writes rows and leaves them is a suite that breaks
    // its neighbours on the second run.
    await allowed('author', 'delete', `/api/assessment/exams/${exam.id}`);
  });

  test('an author cannot read candidates or results', async () => {
    // The two facts that make an author an author. Someone who can see who
    // scored what on the question they wrote has a reason to change the question
    // after the fact, and a name attached to a score is the most sensitive row
    // this product holds.
    await refused('author', 'get', '/api/assessment/candidates?maxResultCount=10');
    await refused('author', 'get', '/api/assessment/candidates/groups');
    await refused('author', 'get', '/api/assessment/results?maxResultCount=10');
    await refused('author', 'get', '/api/assessment/results/summary');
  });

  test('an author cannot send anything, monitor a sitting, or mark one', async () => {
    // The links for one exam, which is the only shape Assignments.View guards:
    // there is no list-of-assignments route, and the exam id is one the author
    // is entitled to because authoring is what they do.
    const exams = await (
      await allowed('author', 'get', '/api/assessment/exams?maxResultCount=1')
    ).json();

    await refused('author', 'get', `/api/assessment/assignments/links/${exams.items[0].id}`);
    await refused('author', 'get', '/api/assessment/attempts/running');
    await refused('author', 'get', '/api/assessment/review/queue?maxResultCount=10');
  });

  // -------------------------------------------------------------- coordinator

  test('a coordinator runs the people, the sittings and the roster', async () => {
    await allowed('coordinator', 'get', '/api/assessment/candidates?maxResultCount=10');
    await allowed('coordinator', 'get', '/api/assessment/candidates/groups');
    await allowed('coordinator', 'get', '/api/assessment/attempts/running');
    await allowed('coordinator', 'get', '/api/assessment/results?maxResultCount=10');

    // Reads the exam list in order to choose what to send. This is the one grant
    // that looks like authoring and is not: the list carries an exam's shape, and
    // its questions live behind Questions.Default, which the coordinator is
    // refused below.
    const exams = await (
      await allowed('coordinator', 'get', '/api/assessment/exams?maxResultCount=1')
    ).json();

    // The links already sent for that exam — revoking one that leaked is the
    // coordinator's, and reading them is what Assignments.View guards.
    await allowed('coordinator', 'get', `/api/assessment/assignments/links/${exams.items[0].id}`);

    // Prefixed with the organisation, because tenancy.spec.ts proves isolation by
    // asserting that every address it can see belongs to the organisation it is
    // signed in to. A candidate named anything else reads there as a leak.
    const created = await (
      await allowed('coordinator', 'post', '/api/assessment/candidates', {
        fullName: 'مرشّح صلاحيات',
        email: `${TENANT}-${unique('roles')}@example.test`,
      })
    ).json();

    // And removed again, which exercises Candidates.Delete and keeps the roll the
    // size the seed made it.
    await allowed('coordinator', 'delete', `/api/assessment/candidates/${created.id}`);
  });

  test('a coordinator cannot create a question, or read the bank at all', async () => {
    const exams = await (
      await allowed('coordinator', 'get', '/api/assessment/exams?maxResultCount=1')
    ).json();

    expect(exams.items[0], 'the language centre has no exam to try').toBeTruthy();

    await refused('coordinator', 'post', '/api/assessment/questions', {
      examId: exams.items[0].id,
      type: 'single-choice',
      text: 'سؤال لا يُفترض أن يُكتب',
      score: 1,
      difficulty: 1,
      displayOrder: 0,
      isActive: true,
      payload: JSON.stringify({ options: [{ id: 'a', text: 'نعم', isCorrect: true }] }),
    });

    // Reading is refused as well as writing, and that is deliberate: the answer
    // key lives in the question payload, and the person who mails the links is
    // the last person who should hold it.
    await refused('coordinator', 'get', '/api/assessment/questions?maxResultCount=10');
  });

  test('a coordinator cannot author or publish an exam', async () => {
    await refused('coordinator', 'post', '/api/assessment/exams', {
      title: unique('لا يُفترض أن يُنشأ'),
      timeLimitInMinutes: 10,
      passingPercentage: 50,
    });
  });

  test('a coordinator cannot change the organisation\'s settings', async () => {
    const settings = await (await allowed('coordinator', 'get', '/api/assessment/settings')).json();

    // Reading is open to anybody signed in — the screen is read-only without the
    // permission, which the route and the service already agree on. Writing is
    // the administrator's.
    await refused('coordinator', 'put', '/api/assessment/settings', {
      ...settings,
      organizationName: 'اسم لا يُفترض أن يُحفظ',
    });
  });

  // ------------------------------------------------------------------- marker

  test('a marker has the review queue', async () => {
    await allowed('marker', 'get', '/api/assessment/review/queue?maxResultCount=10');
  });

  test('a marker can open an answer somebody uploaded, and nothing else', async () => {
    // Reading media was guarded by a question permission, and the Marker role
    // holds none of those — it holds the three Review permissions and no more.
    // So a candidate who answered by uploading a file or recording themselves
    // reached a marker who could not open either: the paperclip rendered, the
    // link had no address, clicking did nothing, and the marker was left to put
    // a number on work they had never seen.
    // Against a real uploaded answer, because the endpoint answers 404 for a
    // blob it will not serve *and* for one that is not there — so an assertion
    // about a made-up path passes whether the permission works or not. This
    // failed exactly that way on the first attempt.
    const exam = await (
      await allowed('admin', 'get', '/api/assessment/exams?maxResultCount=50')
    ).json();

    const published = exam.items.find((e: { status: number; questionCount: number }) => e.status === 1 && e.questionCount > 0);
    expect(published, 'no published exam to attach an answer to').toBeTruthy();

    const person = await (
      await allowed('admin', 'post', '/api/assessment/candidates', {
        fullName: `Attachment ${Date.now()}`,
        email: `language-centre-attach-${Date.now()}@example.test`,
      })
    ).json();

    const sent = await (
      await allowed('admin', 'post', '/api/assessment/assignments', {
        examId: published.id,
        candidateId: person.id,
        expiresAt: '2027-01-01T00:00:00Z',
        maxAttempts: 1,
        sendEmail: false,
      })
    ).json();

    const token = sent.recipients[0].url.split('/').pop();

    const opened = await (await as.get('admin')!.get(`/api/assessment/take/${token}`)).json();
    const started = await (
      await as.get('admin')!.post('/api/assessment/take/start', {
        headers: { 'X-Exam-Session': opened.sessionToken },
      })
    ).json();

    const uploaded = await as.get('admin')!.post('/api/assessment/media/answer', {
      headers: { 'X-Exam-Session': started.sessionToken },
      multipart: {
        file: {
          name: 'answer.png',
          mimeType: 'image/png',
          buffer: Buffer.from(
            'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk' +
              'YPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
            'base64',
          ),
        },
      },
    });

    expect(uploaded.status(), 'the candidate must be able to upload an answer').toBe(200);

    const { blobName: answerBlob } = await uploaded.json();

    const answer = await as.get('marker')!.get(`/api/assessment/media/${answerBlob}`);

    expect(
      answer.status(),
      'a marker must be able to open an answer somebody uploaded',
    ).toBe(200);

    // And the other half, which is why this was narrowed to `answers/` rather
    // than granted wholesale: the question bank stays shut. A marker who could
    // read any blob could read the model answers they are marking against.
    //
    // Against a real file, because a missing blob answers 404 either way and a
    // test that cannot tell a refusal from an absence proves nothing.
    const upload = await as.get('admin')!.post('/api/assessment/media', {
      multipart: {
        file: {
          name: 'question.png',
          mimeType: 'image/png',
          // The smallest valid PNG: an 1×1 transparent pixel.
          buffer: Buffer.from(
            'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk' +
              'YPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
            'base64',
          ),
        },
      },
    });

    expect(upload.status(), 'the administrator must be able to upload').toBe(200);

    const { blobName } = await upload.json();

    expect(await (await as.get('admin')!.get(`/api/assessment/media/${blobName}`)).status()).toBe(200);

    const bank = await as.get('marker')!.get(`/api/assessment/media/${blobName}`);

    // The same address, two roles, two outcomes — which is the whole assertion.
    // 404 and not 403 on purpose: the endpoint does not reveal whether a blob it
    // will not serve exists, so a refusal and an absence look alike from outside.
    // That is right, and it is why this is tested against a file the
    // administrator has just fetched successfully.
    expect(
      bank.status(),
      'a marker must not be able to read question media',
    ).not.toBe(200);
  });

  test('a marker may read the integrity signals on an attempt, and a coordinator may not', async () => {
    // Found as the administrator, because the marker deliberately cannot list
    // results and so has no way to name an attempt from outside their own queue.
    const results = await (
      await allowed('admin', 'get', '/api/assessment/results?maxResultCount=1')
    ).json();

    expect(results.items[0], 'the language centre has no sitting to try').toBeTruthy();

    const attemptId = results.items[0].attemptId;

    // Granted, and this is the one judgement call in the role table. The marker
    // is the only person who reads a free-text answer and decides whether it is
    // the candidate's own work; a paste event on a long written answer is the
    // single most relevant fact to that decision. Not 403 rather than exactly
    // 200, because an attempt with no signals recorded is a legitimate 404 and
    // says nothing about the permission.
    const report = await as
      .get('marker')!
      .get(`/api/assessment/review/attempts/${attemptId}/integrity`);

    expect(report.status(), 'the marker was refused the integrity report').not.toBe(403);

    // The coordinator watches sittings happen and never reads an answer, so a
    // flag is a number they cannot interpret and a behavioural record they have
    // no reason to hold.
    await refused('coordinator', 'get', `/api/assessment/review/attempts/${attemptId}/integrity`);
  });

  test('a marker cannot read the results roster', async () => {
    // The distinction the Marker role exists to draw: they see the answers they
    // are marking, one attempt at a time, and never the list of who scored what.
    await refused('marker', 'get', '/api/assessment/results?maxResultCount=10');
    await refused('marker', 'get', '/api/assessment/results/summary');
    await refused('marker', 'get', '/api/assessment/results/export');
  });

  test('a marker sees no candidates, no exams and no questions', async () => {
    await refused('marker', 'get', '/api/assessment/candidates?maxResultCount=10');
    await refused('marker', 'get', '/api/assessment/exams?maxResultCount=10');
    await refused('marker', 'get', '/api/assessment/questions?maxResultCount=10');

    // A well-formed id that exists, so a 403 is the permission answering rather
    // than the route failing to bind.
    const exams = await (
      await allowed('admin', 'get', '/api/assessment/exams?maxResultCount=1')
    ).json();

    await refused('marker', 'get', `/api/assessment/assignments/links/${exams.items[0].id}`);
  });

  // ----------------------------------------------------------------- observer

  test('an observer reads the roster and the item analysis', async () => {
    await allowed('observer', 'get', '/api/assessment/results?maxResultCount=10');
    await allowed('observer', 'get', '/api/assessment/results/summary');

    const exams = await (
      await allowed('observer', 'get', '/api/assessment/exams?maxResultCount=1')
    ).json();

    expect(exams.items[0], 'the language centre has no exam to try').toBeTruthy();

    // Item analysis is addressed by exam id and unreachable without one, which is
    // why the observer holds Exams.View at all.
    await allowed('observer', 'get', `/api/assessment/results/item-analysis/${exams.items[0].id}`);
  });

  test('an observer cannot send an assignment', async () => {
    const exams = await (
      await allowed('observer', 'get', '/api/assessment/exams?maxResultCount=1')
    ).json();

    // Refused before the request body matters. Sending is the act that reaches a
    // real person's inbox, and an observer is by definition someone who watches.
    await refused('observer', 'post', '/api/assessment/assignments', {
      examId: exams.items[0].id,
      expiresAt: new Date(Date.now() + 864e5).toISOString(),
      maxAttempts: 1,
      sendEmail: false,
    });
  });

  test('an observer changes nothing at all', async () => {
    await refused('observer', 'post', '/api/assessment/candidates', {
      fullName: 'لا يُفترض أن يُنشأ',
      email: `${unique('observer')}@example.test`,
    });

    await refused('observer', 'post', '/api/assessment/exams', {
      title: unique('لا يُفترض أن يُنشأ'),
      timeLimitInMinutes: 10,
      passingPercentage: 50,
    });

    await refused('observer', 'post', '/api/assessment/catalog/categories', {
      name: 'لا يُفترض أن يُنشأ',
      code: unique('nope'),
      displayOrder: 0,
      isActive: true,
    });
  });

  // -------------------------------------------------------- common to all four

  test('no role but the administrator reaches the staff accounts', async () => {
    // Users.ManageRoles is the escalation path this product already had once:
    // anybody who could edit a colleague could tick Admin on their own record.
    // None of the four gets near the screen.
    for (const role of ROLES) {
      await refused(role, 'get', '/api/app/users?maxResultCount=10&skipCount=0');
    }
  });

  test('no role but the administrator deletes a sitting', async () => {
    const results = await (
      await allowed('admin', 'get', '/api/assessment/results?maxResultCount=1')
    ).json();

    const attemptId = results.items[0].attemptId;

    // The coordinator holds ForceSubmit — closing out a sitting that hung is what
    // the attempt monitor is for — and not Delete. Ending a sitting and
    // destroying the evidence of it are not the same act.
    for (const role of ROLES) {
      await refused(role, 'delete', `/api/assessment/attempts/${attemptId}`);
    }
  });
});
