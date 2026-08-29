import { APIRequestContext, expect, request, test } from '@playwright/test';
import { API } from './api';

/**
 * Three organisations on one deployment, and what each of them can see.
 *
 * Multi-tenancy is the claim this product makes most often and the one nothing
 * verified. That gap cost real defects: every image on a paper 404'd for any
 * tenant but the host, because disabling the data filter lets a request *see* a
 * row without making the request *be* that tenant — and a language centre's
 * candidates were shown the platform's name instead of the centre's, for the
 * same reason. Neither was visible to a suite that only ever had one tenant.
 *
 * Needs the host running and `node tools/seed-tenants.js` to have been run.
 *
 *   npx playwright test --project=live tenancy
 */
test.describe('Three organisations on one deployment', () => {
  test.setTimeout(120_000);

  const TENANTS = ['trading-academy', 'language-centre', 'recruitment'] as const;

  const contexts = new Map<string, APIRequestContext>();

  test.beforeAll(async () => {
    for (const tenant of TENANTS) {
      // The development certificate is self-signed, so this cannot use the
      // default request fixture.
      const anonymous = await request.newContext({ ignoreHTTPSErrors: true, baseURL: API });

      const auth = await anonymous.post('/connect/token', {
        headers: { __tenant: tenant },
        form: {
          grant_type: 'password',
          username: 'admin',
          password: '1q2w3E*',
          client_id: 'InternshipManagementSystem_App',
          scope: 'InternshipManagementSystem offline_access openid profile',
        },
      });

      if (!auth.ok()) {
        throw new Error(
          `Could not sign in to "${tenant}". Run: node tools/seed-tenants.js\n` + (await auth.text()),
        );
      }

      contexts.set(
        tenant,
        await request.newContext({
          ignoreHTTPSErrors: true,
          baseURL: API,
          extraHTTPHeaders: {
            Authorization: `Bearer ${(await auth.json()).access_token}`,
            __tenant: tenant,
          },
        }),
      );
    }
  });

  test('each organisation sees its own name, not the platform\'s', async () => {
    const names = new Set<string>();

    for (const tenant of TENANTS) {
      const settings = await contexts.get(tenant)!.get('/api/assessment/settings');

      expect(settings.ok()).toBe(true);

      const { organizationName } = await settings.json();

      expect(organizationName, `${tenant} has no name of its own`).toBeTruthy();
      names.add(organizationName);
    }

    // Three organisations, three names. One shared name would mean the setting is
    // resolving globally rather than per tenant, which is what happened on the
    // candidate's screen.
    expect(names.size).toBe(TENANTS.length);
  });

  test('each organisation sees only its own catalogue', async () => {
    const codes = new Map<string, string[]>();

    for (const tenant of TENANTS) {
      const categories = await contexts.get(tenant)!.get('/api/assessment/catalog/categories');

      expect(categories.ok()).toBe(true);

      codes.set(tenant, (await categories.json()).map((c: { code: string }) => c.code).sort());
    }

    // A language centre teaches English and French; a trading academy does not.
    expect(codes.get('language-centre')).toContain('english');
    expect(codes.get('trading-academy')).toContain('tech-analysis');

    expect(codes.get('language-centre')).not.toContain('tech-analysis');
    expect(codes.get('trading-academy')).not.toContain('english');
    expect(codes.get('recruitment')).not.toContain('english');
  });

  test('each organisation sees only its own people and classes', async () => {
    for (const tenant of TENANTS) {
      const people = await contexts.get(tenant)!.get('/api/assessment/candidates?maxResultCount=200');
      const emails = (await people.json()).items.map((c: { email: string }) => c.email);

      // Everybody this organisation can see belongs to it. The seed gives every
      // person an address prefixed with their organisation, so a leak is visible
      // rather than merely a count that looks plausible.
      const strangers = emails.filter(
        (email: string) => !email.startsWith(tenant) && !email.startsWith('load-'),
      );

      expect(strangers, `${tenant} can see people from another organisation`).toEqual([]);

      const groups = await contexts.get(tenant)!.get('/api/assessment/candidates/groups');

      expect((await groups.json()).length).toBeGreaterThan(0);
    }
  });

  test('one organisation cannot read another\'s exam by its id', async () => {
    const theirs = await contexts.get('language-centre')!.get('/api/assessment/exams?maxResultCount=5');
    const exam = (await theirs.json()).items[0];

    expect(exam, 'the language centre has no exam to try').toBeTruthy();

    // The same id, asked for by a different organisation. Guessing an id is not
    // hard; the isolation has to be in the query, not in the obscurity of the
    // identifier.
    const stolen = await contexts.get('trading-academy')!.get(`/api/assessment/exams/${exam.id}`);

    expect(stolen.status()).toBe(404);
  });

  test('one organisation cannot read another\'s results', async () => {
    const theirs = await contexts.get('language-centre')!.get('/api/assessment/results?maxResultCount=5');
    const row = (await theirs.json()).items[0];

    expect(row, 'the language centre has no result to try').toBeTruthy();

    const stolen = await contexts.get('recruitment')!.get(`/api/assessment/results/${row.attemptId}`);

    expect(stolen.status()).toBe(404);
  });

  test('a candidate of one organisation sits that organisation\'s exam', async () => {
    const centre = contexts.get('language-centre')!;

    const exams = await centre.get('/api/assessment/exams?maxResultCount=5');
    const exam = (await exams.json()).items[0];

    const candidate = await centre.post('/api/assessment/candidates', {
      data: {
        fullName: 'Tenancy Check',
        email: `tenancy-${Date.now().toString(36)}@example.test`,
      },
    });

    const sent = await centre.post('/api/assessment/assignments', {
      data: {
        examId: exam.id,
        candidateId: (await candidate.json()).id,
        expiresAt: new Date(Date.now() + 864e5).toISOString(),
        maxAttempts: 1,
        sendEmail: false,
      },
    });

    const linkToken = (await sent.json()).recipients[0].url.split('/').pop();

    // As the candidate: no token, no tenant header, nothing but the link. This is
    // the path that had never been exercised for any tenant but the host, and the
    // one where the branding and every image were being read from the wrong
    // organisation.
    const anonymous = await request.newContext({ ignoreHTTPSErrors: true, baseURL: API });

    const preview = await anonymous.get(`/api/assessment/take/${linkToken}`);

    expect(preview.ok()).toBe(true);

    const opened = await preview.json();

    expect(opened.isAccessible).toBe(true);
    expect(opened.examTitle).toBe(exam.title);

    // Their centre's name, resolved from the link alone.
    expect(opened.organizationName).toBe('مركز النور للغات');
  });
});
