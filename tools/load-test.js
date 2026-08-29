// How many people can sit an exam at once, and what it costs them.
//
//   node tools/load-test.js [--candidates 50] [--tenant language-centre] [url]
//
// The thing worth measuring here is not requests per second. It is what one
// candidate experiences while forty-nine others are doing the same thing — a
// person under time pressure, on one attempt they cannot retake, watching a
// question load. So this reports percentiles per journey step rather than an
// aggregate throughput number, and it fails loudly if any candidate's journey
// breaks rather than averaging the failure away.
//
// It drives the real HTTP surface as a candidate does: open the link, start,
// fetch each question, save each answer, submit. No token, because a candidate
// has no account.
//
// Needs the API running and `node tools/seed-tenants.js` to have been run.

const https = require('https');
const http = require('http');
const { performance } = require('perf_hooks');

const args = process.argv.slice(2);

function flag(name, fallback) {
  const at = args.indexOf('--' + name);

  return at >= 0 && args[at + 1] ? args[at + 1] : fallback;
}

const CANDIDATES = Number(flag('candidates', '40'));
const TENANT = flag('tenant', 'language-centre');
const base = new URL(args.find(a => a.startsWith('http')) ?? 'https://localhost:44373');

const client = base.protocol === 'https:' ? https : http;

// Keep-alive, because a real cohort arrives on browsers that reuse connections,
// and measuring TLS handshakes fifty times would measure the test.
const agent = base.protocol === 'https:'
  ? new https.Agent({ rejectUnauthorized: false, keepAlive: true, maxSockets: 256 })
  : new http.Agent({ keepAlive: true, maxSockets: 256 });

const ADMIN = { username: 'admin', password: '1q2w3E*' };

function call(method, path, { token, body, form, tenant, session } = {}) {
  return new Promise((resolve, reject) => {
    const payload = form
      ? new URLSearchParams(form).toString()
      : body !== undefined
        ? JSON.stringify(body)
        : null;

    const headers = {};
    if (token) headers.Authorization = 'Bearer ' + token;
    if (tenant) headers.__tenant = tenant;
    if (session) headers['X-Exam-Session'] = session;
    if (form) headers['Content-Type'] = 'application/x-www-form-urlencoded';
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    if (payload) headers['Content-Length'] = Buffer.byteLength(payload);

    const started = performance.now();

    const req = client.request(
      { host: base.hostname, port: base.port, path, method, headers, agent },
      res => {
        let data = '';
        res.on('data', c => (data += c));
        res.on('end', () =>
          resolve({ status: res.statusCode, body: data, ms: performance.now() - started }));
      },
    );

    req.on('error', reject);
    if (payload) req.write(payload);
    req.end();
  });
}

async function json(method, path, options = {}) {
  const res = await call(method, path, options);

  if (res.status >= 400) {
    throw new Error(`${method} ${path} → ${res.status}: ${res.body.slice(0, 200)}`);
  }

  return { value: res.body ? JSON.parse(res.body) : undefined, ms: res.ms };
}

// ------------------------------------------------------------------ timings

const timings = new Map();

function record(step, ms) {
  const list = timings.get(step) ?? [];

  list.push(ms);
  timings.set(step, list);
}

function percentile(sorted, p) {
  if (sorted.length === 0) {
    return 0;
  }

  const index = Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1);

  return sorted[index];
}

// --------------------------------------------------------------- the journey

async function sit(linkToken) {
  const preview = await json('GET', `/api/assessment/take/${linkToken}`);
  record('open the link', preview.ms);

  if (!preview.value.isAccessible) {
    throw new Error('link not accessible: ' + (preview.value.blockReason ?? 'unknown'));
  }

  const started = await json('POST', '/api/assessment/take/start', {
    session: preview.value.sessionToken,
    body: {},
  });

  record('start the exam', started.ms);

  const session = started.value.sessionToken;

  for (let position = 0; position < started.value.totalQuestions; position++) {
    const question = await json('GET', `/api/assessment/take/question/${position}`, { session });
    record('load a question', question.ms);

    const chosen = question.value.options[0];

    const saved = await json('PUT', '/api/assessment/take/answer', {
      session,
      body: { questionId: question.value.id, response: JSON.stringify([chosen.id]) },
    });

    record('save an answer', saved.ms);
  }

  const submitted = await json('POST', '/api/assessment/take/submit', { session, body: {} });
  record('submit and mark', submitted.ms);
}

// ------------------------------------------------------------------- set-up

async function prepare() {
  const auth = await call('POST', '/connect/token', {
    tenant: TENANT,
    form: {
      grant_type: 'password',
      ...ADMIN,
      client_id: 'InternshipManagementSystem_App',
      scope: 'InternshipManagementSystem offline_access openid profile',
    },
  });

  if (auth.status !== 200) {
    throw new Error(
      `Could not sign in to "${TENANT}". Run: node tools/seed-tenants.js\n${auth.body.slice(0, 200)}`,
    );
  }

  const token = JSON.parse(auth.body).access_token;
  const as = { token, tenant: TENANT };

  const exams = await json('GET', '/api/assessment/exams?maxResultCount=10', as);
  const exam = exams.value.items.find(e => e.status === 1) ?? exams.value.items[0];

  if (!exam) {
    throw new Error(`No exam in "${TENANT}". Run: node tools/seed-tenants.js`);
  }

  // A fixed pool, reused between runs.
  //
  // This used to mint a fresh candidate per virtual sitter on every run, and
  // those candidates then sat an exam — which the product will not let you
  // delete, correctly, because a score has to belong to somebody. Six hundred
  // rows called "Load m3f2x-17" accumulated in one organisation's candidate list
  // before anybody noticed.
  //
  // Reusing them is also more honest: attempts are per link, and each run issues
  // a new link, so the same person can sit again without being refused.
  const existing = await json(
    'GET',
    '/api/assessment/candidates?filter=load-&maxResultCount=1000',
    as,
  );

  const pool = new Map(
    existing.value.items
      .filter(c => c.email.startsWith('load-'))
      .map(c => [c.email, c.id]),
  );

  const ids = [];

  for (let i = 0; i < CANDIDATES; i++) {
    const email = `load-${i}@example.test`;

    if (!pool.has(email)) {
      const created = await json('POST', '/api/assessment/candidates', {
        ...as,
        body: { fullName: `Load tester ${i + 1}`, email },
      });

      pool.set(email, created.value.id);
    }

    ids.push(pool.get(email));
  }

  const links = [];

  for (const candidateId of ids) {
    const sent = await json('POST', '/api/assessment/assignments', {
      ...as,
      body: {
        examId: exam.id,
        candidateId,
        expiresAt: new Date(Date.now() + 864e5).toISOString(),
        maxAttempts: 1,
        sendEmail: false,
      },
    });

    links.push(sent.value.recipients[0].url.split('/').pop());
  }

  return { exam, links };
}

// ---------------------------------------------------------------------- run

(async () => {
  console.log(`Preparing ${CANDIDATES} candidates in "${TENANT}"…`);

  const { exam, links } = await prepare();

  console.log(`\n${CANDIDATES} candidates sitting "${exam.title}" at the same time.\n`);

  const wall = performance.now();

  // All at once. A cohort in a room starts when the invigilator says start, not
  // spread politely over a minute — and the interesting question is precisely
  // what happens at that moment.
  const outcomes = await Promise.allSettled(links.map(sit));

  const elapsed = (performance.now() - wall) / 1000;
  const failed = outcomes.filter(o => o.status === 'rejected');

  const steps = [...timings.keys()];
  const width = Math.max(...steps.map(s => s.length));

  console.log('Step'.padEnd(width) + '   n      p50      p95      p99      max');
  console.log('-'.repeat(width + 40));

  for (const step of steps) {
    const sorted = timings.get(step).slice().sort((a, b) => a - b);

    console.log(
      step.padEnd(width) +
      String(sorted.length).padStart(4) +
      ms(percentile(sorted, 50)) +
      ms(percentile(sorted, 95)) +
      ms(percentile(sorted, 99)) +
      ms(sorted[sorted.length - 1]),
    );
  }

  const requests = [...timings.values()].reduce((sum, list) => sum + list.length, 0);

  console.log(
    `\n${requests} requests in ${elapsed.toFixed(1)}s ` +
    `(${(requests / elapsed).toFixed(0)}/s), ${CANDIDATES - failed.length}/${CANDIDATES} journeys completed.`,
  );

  if (failed.length > 0) {
    // Reported one by one rather than counted. A failure here is a person whose
    // exam did not work, and averaging it into a success rate is how that stops
    // being visible.
    console.log(`\n${failed.length} journeys failed:`);

    for (const failure of failed.slice(0, 10)) {
      console.log('  ' + failure.reason.message);
    }

    process.exit(1);
  }
})().catch(err => {
  console.error('\n' + err.message);
  process.exit(1);
});

function ms(value) {
  return (value.toFixed(0) + 'ms').padStart(9);
}
