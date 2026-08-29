// One staff account per role, in each of the three seeded organisations.
//
//   node tools/seed-role-users.js [https://localhost:44373]
//
// Until now the only role in this product was Admin, which holds every
// permission, so no permission had ever been exercised as a *restriction*. A
// permission that is only ever granted is not a permission; it is a checkbox.
// This script produces the accounts that make the restriction real: a
// coordinator who runs sittings and cannot write an exam, an author who writes
// exams and has never seen a candidate's name, a marker who has the review queue
// and nothing else, an observer who reads results and can change none of them.
//
// The roles themselves are seeded by the server —
// InternshipManagementSystemDataSeedContributor — so run the DbMigrator before
// this. If a role is missing here, that is what is missing, and the script says
// so rather than creating an account with no role, which can sign in and see an
// empty application.
//
// Its output is the input to angular/e2e/live/roles.spec.ts, which signs in as
// each of these and asserts both halves: what the role can do succeeds, and what
// it cannot returns 403.
//
// Re-runnable: an account that already exists is reused rather than duplicated,
// and its role is repaired if it drifted.

const https = require('https');
const http = require('http');

const base = new URL(process.argv[2] ?? 'https://localhost:44373');
const client = base.protocol === 'https:' ? https : http;
const agent = base.protocol === 'https:' ? new https.Agent({ rejectUnauthorized: false }) : undefined;

// The three organisations tools/seed-tenants.js creates. Their administrator is
// "admin" in every one of them, which is what multi-tenancy means: the same
// username in three places, told apart by the __tenant header.
const TENANTS = ['trading-academy', 'language-centre', 'recruitment'];

const TENANT_ADMIN = { username: 'admin', password: '1q2w3E*' };

// Development only, and the same one every other seeded account gets, so nobody
// mistakes it for a real one.
const STAFF_PASSWORD = '1q2w3E*';

// The role names the data seed contributor creates, and the person each one is.
// The Arabic names are what these roles are called in the product; the English
// ones are what ABP stores, because an IdentityRole has a name and no display
// name, so the name is a database key rather than a label. See
// docs/business/roles.md.
//
// The phone numbers are not decoration. CreateUpdateUserDto declares
// `public string PhoneNumber` in a nullable-enabled project, so ASP.NET treats
// it as required and refuses the whole account without one; it is also capped at
// ten characters, which is a Saudi mobile and nothing longer.
const ROLES = [
  { role: 'Coordinator', arabic: 'منسّق', fullName: 'منسّق الاختبارات', phone: '0500000001' },
  { role: 'Author', arabic: 'مُعِدّ الاختبارات', fullName: 'مُعِدّ الاختبارات', phone: '0500000002' },
  { role: 'Marker', arabic: 'مصحّح', fullName: 'مصحّح الإجابات', phone: '0500000003' },
  { role: 'Observer', arabic: 'مشاهد النتائج', fullName: 'مشاهد النتائج', phone: '0500000004' },
];

function call(method, path, { token, body, form, tenant } = {}) {
  return new Promise((resolve, reject) => {
    const payload = form
      ? new URLSearchParams(form).toString()
      : body !== undefined
        ? JSON.stringify(body)
        : null;

    const headers = {};
    if (token) headers.Authorization = 'Bearer ' + token;
    if (tenant) headers.__tenant = tenant;
    if (form) headers['Content-Type'] = 'application/x-www-form-urlencoded';
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    if (payload) headers['Content-Length'] = Buffer.byteLength(payload);

    const req = client.request(
      { host: base.hostname, port: base.port, path, method, headers, agent },
      res => {
        let data = '';
        res.on('data', c => (data += c));
        res.on('end', () => resolve({ status: res.statusCode, body: data }));
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
    throw new Error(`${method} ${path} → ${res.status}: ${res.body.slice(0, 400)}`);
  }

  return res.body ? JSON.parse(res.body) : undefined;
}

async function signIn({ username, password, tenant }) {
  const form = {
    grant_type: 'password',
    username,
    password,
    client_id: 'InternshipManagementSystem_App',
    scope: 'InternshipManagementSystem offline_access openid profile',
  };

  // ABP resolves the tenant from this header on the token endpoint too, which is
  // what makes one username work in several organisations at once.
  const res = await call('POST', '/connect/token', { form, tenant });

  if (res.status !== 200) {
    throw new Error(
      `Could not sign in as ${username}${tenant ? ' @ ' + tenant : ''}: ${res.body.slice(0, 300)}`,
    );
  }

  return JSON.parse(res.body).access_token;
}

// ------------------------------------------------------------------ the work

/**
 * The account for one role, created if it is not there and repaired if it is.
 *
 * Existing accounts are found by username rather than created and caught,
 * because a 400 from a duplicate and a 400 from a rejected password read
 * identically from here and only one of them is a defect.
 */
async function ensureStaffAccount(as, tenant, spec, existing) {
  const userName = spec.role.toLowerCase();
  const email = `${userName}@${tenant}.test`;

  const found = existing.find(u => u.userName === userName);

  if (!found) {
    await json('POST', '/api/app/users', {
      ...as,
      body: {
        userName,
        email,
        password: STAFF_PASSWORD,
        fullName: spec.fullName,
        phoneNumber: spec.phone,
        roles: [spec.role],
      },
    });

    return { userName, email, state: 'created' };
  }

  // Already there. The role is still worth checking: an account whose role was
  // changed by hand, or one created before the role existed, signs in perfectly
  // well and then finds every screen refused — which reads as a broken product
  // rather than as a missing role.
  if (!found.roles.includes(spec.role)) {
    await json('PUT', `/api/app/users/${found.id}`, {
      ...as,
      body: {
        userName: found.userName,
        email: found.email || email,
        password: STAFF_PASSWORD,
        fullName: found.fullName || spec.fullName,
        phoneNumber: found.phoneNumber || spec.phone,
        roles: [spec.role],
      },
    });

    return { userName, email, state: 'role repaired' };
  }

  return { userName, email, state: 'already exists' };
}

async function seedTenant(tenant) {
  console.log(`\n── ${tenant} ──`);

  const token = await signIn({ ...TENANT_ADMIN, tenant });
  const as = { token, tenant };

  // The roles the server seeded. Checked before anything is created, because an
  // account created with a role that does not exist is refused by the identity
  // manager half way through, leaving a user behind with no role at all.
  const roles = await json('GET', '/api/app/users/roles', as);
  const missing = ROLES.filter(spec => !roles.includes(spec.role)).map(spec => spec.role);

  if (missing.length > 0) {
    throw new Error(
      `"${tenant}" has no role named ${missing.join(', ')}. The roles are seeded by the server: `
        + 'run the DbMigrator (dotnet run --project src/InternshipManagementSystem.DbMigrator) '
        + 'and try again.',
    );
  }

  const existing = (await json('GET', '/api/app/users?maxResultCount=200&skipCount=0', as)).items;

  const rows = [];

  for (const spec of ROLES) {
    const account = await ensureStaffAccount(as, tenant, spec, existing);

    // Signed in as, not merely created. A seeded account nobody has ever
    // authenticated as is a promise; the live suite that consumes this table
    // fails ten minutes later and blames the permission rather than the password.
    await signIn({ username: account.userName, password: STAFF_PASSWORD, tenant });

    rows.push({
      organisation: tenant,
      role: spec.role,
      'الدور': spec.arabic,
      username: account.userName,
      email: account.email,
      password: STAFF_PASSWORD,
      state: account.state,
    });

    console.log(`  ${spec.role.padEnd(12)} ${account.email.padEnd(34)} ${account.state}`);
  }

  return rows;
}

(async () => {
  console.log(`Seeding one staff account per role in three organisations, against ${base.origin}`);

  const summary = [];

  for (const tenant of TENANTS) {
    summary.push(...(await seedTenant(tenant)));
  }

  console.log('\nDone. Every account below was created or found, and signed in as.\n');
  console.table(summary);

  console.log(
    '\nEach account signs in with the __tenant header set to its organisation.\n'
      + 'What each role may and may not do: docs/business/roles.md\n'
      + 'Proof that it may and may not: cd angular && npx playwright test --project=live roles\n',
  );
})().catch(err => {
  console.error('\n' + err.message);
  process.exit(1);
});
