// Does every route the client calls actually answer on a running server?
//
//   node tools/smoke-routes.js [https://localhost:44373]
//
// This exists because one defect kept recurring and no test could see it: a
// finished application service with no HTTP controller reads as done in any
// inventory that counts services rather than journeys. It happened four times
// here — assignments, review, media, and the whole catalogue — and each time it
// was found by a person reading code rather than by the suite, because the
// tests call the service directly and never cross the routing table.
//
// It also catches the two failures that only exist in a wired-up application:
// an [Authorize] naming a policy nobody defined (ASP.NET answers 500, not 403,
// so the screen looks broken rather than forbidden), and a container with no
// provider configured behind it.
//
// Run it after any change that adds a controller, a permission, or a module
// dependency. It needs the API running and the database seeded.

const https = require('https');
const http = require('http');

const base = new URL(process.argv[2] ?? 'https://localhost:44373');
const client = base.protocol === 'https:' ? https : http;

// A development server with a self-signed certificate.
const agent = base.protocol === 'https:'
  ? new https.Agent({ rejectUnauthorized: false })
  : undefined;

const CREDENTIALS = {
  grant_type: 'password',
  username: process.env.SMOKE_USER ?? 'admin',
  password: process.env.SMOKE_PASSWORD ?? '1q2w3E*',
  client_id: 'InternshipManagementSystem_App',
  scope: 'InternshipManagementSystem offline_access openid profile',
};

function request(method, path, { token, body, form } = {}) {
  return new Promise((resolve, reject) => {
    const payload = form
      ? new URLSearchParams(form).toString()
      : body
        ? JSON.stringify(body)
        : null;

    const headers = {};
    if (token) headers.Authorization = 'Bearer ' + token;
    if (form) headers['Content-Type'] = 'application/x-www-form-urlencoded';
    if (body) headers['Content-Type'] = 'application/json';
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

// Each entry is [method, path, accept]. `accept` defaults to "anything under
// 400"; a route that is expected to refuse says so, because 404 from a missing
// route and 404 from a missing row look identical from here and only one of
// them is a defect.
const ROUTES = [
  ['GET', '/api/assessment/exams'],
  ['GET', '/api/assessment/questions/types'],
  ['GET', '/api/assessment/catalog/categories'],
  ['GET', '/api/assessment/catalog/vocabulary'],
  ['GET', '/api/assessment/candidates'],
  ['GET', '/api/assessment/candidates/groups'],
  ['GET', '/api/assessment/results'],
  ['GET', '/api/assessment/results/summary'],
  ['GET', '/api/assessment/settings'],
  ['GET', '/api/app/users'],
  ['GET', '/api/app/users/roles'],

  // No such blob. 404 here proves the route resolves and the container
  // activates — the failure this replaced was a 500 from a BLOB provider that
  // was never configured, which no amount of uploading would have fixed.
  ['GET', '/api/assessment/media/nobody/none.png', s => s === 404],

  // Gone on purpose. Two legacy settings services wrote the same values as
  // /api/assessment/settings and one of them had no [Authorize] at all, so
  // anybody could rename the organisation without signing in. If either route
  // answers again, a duplicate has come back.
  ['GET', '/api/app/system-general-settings', s => s === 404],
  ['GET', '/api/app/self-registration-setting', s => s === 404],
];

(async () => {
  const auth = await request('POST', '/connect/token', { form: CREDENTIALS });

  if (auth.status !== 200) {
    console.error('could not authenticate:', auth.status, auth.body.slice(0, 300));
    process.exit(1);
  }

  const token = JSON.parse(auth.body).access_token;
  let failures = 0;

  for (const [method, path, accept] of ROUTES) {
    const res = await request(method, path, { token });
    const ok = accept ? accept(res.status) : res.status < 400;

    if (!ok) {
      failures++;
      console.log(`FAIL ${res.status}  ${method} ${path}`);
      console.log('      ' + res.body.slice(0, 220).replace(/\s+/g, ' '));
    } else {
      console.log(`ok   ${res.status}  ${method} ${path}`);
    }
  }

  console.log(failures === 0 ? '\nall routes answer' : `\n${failures} route(s) failed`);
  process.exit(failures === 0 ? 0 : 1);
})().catch(err => {
  console.error(err.message);
  process.exit(1);
});
