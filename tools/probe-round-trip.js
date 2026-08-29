// Does an update actually keep what you sent it?
//
//   node tools/probe-round-trip.js [https://localhost:44373]
//
// Written after finding that changing a staff account's password answered 200
// and changed nothing. The field was read from the form, passed validation,
// carried through the DTO, and then simply not used. Nothing failed. Nothing
// logged. An administrator resetting a password for somebody locked out was
// told it had worked, and went and read the new password out to them.
//
// That class of defect is invisible to the tests that usually guard a write —
// they assert the status code, and the status code was right. The only thing
// that catches it is sending a value and reading it back.
//
// So: for each entity a coordinator edits, create one, change every field to a
// value distinguishable from the old one, read it back, and report each field
// that came back unchanged. Then delete what was created.
//
// It reports rather than asserts. A field can legitimately be write-only or
// derived, and this cannot tell the difference — a person has to read the list
// and say which of them is a bug. That is the point: it narrows "somewhere in
// forty endpoints" down to a handful worth looking at.

const https = require('https');
const http = require('http');

const base = new URL(process.argv[2] ?? 'https://localhost:44373');
const client = base.protocol === 'https:' ? https : http;
const agent = base.protocol === 'https:'
  ? new https.Agent({ rejectUnauthorized: false })
  : undefined;

const TENANT = 'trading-academy';
const ADMIN = { username: 'admin', password: '1q2w3E*' };

function call(method, path, { token, body, form } = {}) {
  return new Promise((resolve, reject) => {
    const payload = form
      ? new URLSearchParams(form).toString()
      : body !== undefined ? JSON.stringify(body) : null;

    const headers = { __tenant: TENANT };

    if (payload !== null) {
      headers['Content-Type'] = form ? 'application/x-www-form-urlencoded' : 'application/json';
      headers['Content-Length'] = Buffer.byteLength(payload);
    }
    if (token) headers.Authorization = 'Bearer ' + token;

    const req = client.request(
      { hostname: base.hostname, port: base.port, path, method, headers, agent },
      res => {
        let data = '';
        res.on('data', chunk => (data += chunk));
        res.on('end', () => resolve({ status: res.statusCode, body: data }));
      });

    req.on('error', reject);
    if (payload !== null) req.write(payload);
    req.end();
  });
}

async function signIn() {
  const res = await call('POST', '/connect/token', {
    form: {
      grant_type: 'password',
      username: ADMIN.username,
      password: ADMIN.password,
      client_id: 'InternshipManagementSystem_App',
      scope: 'InternshipManagementSystem offline_access',
    },
  });

  if (res.status !== 200) {
    throw new Error(`sign-in ${res.status}: ${res.body.slice(0, 300)}`);
  }

  return JSON.parse(res.body).access_token;
}

const json = res => {
  try {
    return JSON.parse(res.body);
  } catch {
    return null;
  }
};

const stamp = Date.now().toString(36);

// Each subject says how to make one, what to change it to, and where to look
// for the change. `after` is what the update should leave behind; anything in it
// that does not come back is reported.
const SUBJECTS = [
  {
    name: 'candidate',
    path: '/api/assessment/candidates',
    create: {
      fullName: 'قبل التعديل',
      email: `probe-cand-${stamp}@example.test`,
    },
    after: {
      fullName: 'بعد التعديل',
      email: `probe-cand-${stamp}-b@example.test`,
      phoneNumber: '+966501234567',
      nationalId: 'ID-1234567',
      notes: 'ملاحظة',
    },
  },
  {
    name: 'class',
    path: '/api/assessment/candidates/groups',
    create: { name: `probe-group-${stamp}` },
    after: {
      name: `probe-group-${stamp}-b`,
      description: 'وصف جديد',
      startsOn: '2026-09-01',
      endsOn: '2026-12-01',
    },
  },
];

// The password is the case that motivated all of this, and the one the pattern
// above cannot see: it is never read back, by design. So it is checked the only
// way it can be — by trying to sign in with it.
async function probeStaffPassword(token) {
  const as = { token };
  const path = '/api/app/users';

  const userName = `probe-staff-${stamp}`;
  const first = '1q2w3E*';
  const second = 'Zx9!qwErTy';

  const account = {
    userName,
    email: `${userName}@example.test`,
    fullName: 'قياس',
    phoneNumber: '+966501234567',
    roles: [],
  };

  const created = await call('POST', path, { ...as, body: { ...account, password: first } });

  if (created.status >= 300) {
    return { name: 'staff password', skipped: `create → ${created.status} ${created.body.slice(0, 160)}` };
  }

  const id = (json(created) || {}).id;
  const problems = [];

  // Editing a detail without retyping a password. This answered 400 — for a
  // field the screen itself calls optional when editing.
  const detail = await call('PUT', `${path}/${id}`, {
    ...as, body: { ...account, fullName: 'اسم مُصحَّح' },
  });

  if (detail.status >= 300) {
    problems.push(`editing without a password → ${detail.status} ${detail.body.slice(0, 120)}`);
  }

  const changed = await call('PUT', `${path}/${id}`, {
    ...as, body: { ...account, password: second },
  });

  if (changed.status >= 300) {
    problems.push(`setting a new password → ${changed.status} ${changed.body.slice(0, 120)}`);
  } else {
    if (!await canSignIn(userName, second)) {
      problems.push('the new password was accepted with 200 and does not work');
    }
    if (await canSignIn(userName, first)) {
      problems.push('the old password still works after being replaced');
    }
  }

  await call('DELETE', `${path}/${id}`, as);

  return { name: 'staff password', dropped: problems, absent: [] };
}

async function canSignIn(username, password) {
  const res = await call('POST', '/connect/token', {
    form: {
      grant_type: 'password',
      username,
      password,
      client_id: 'InternshipManagementSystem_App',
      scope: 'InternshipManagementSystem offline_access',
    },
  });

  return res.status === 200;
}

async function probe(token, subject) {
  const as = { token };

  const created = await call('POST', subject.path, { ...as, body: subject.create });

  if (created.status >= 300) {
    return { name: subject.name, skipped: `create → ${created.status} ${created.body.slice(0, 160)}` };
  }

  const id = (json(created) || {}).id;

  if (!id) {
    return { name: subject.name, skipped: 'create returned no id' };
  }

  // Everything the create needed, plus everything being changed. A PUT that
  // replaces rather than patches must still receive the required fields.
  const sent = { ...subject.create, ...subject.after };

  const updated = await call('PUT', `${subject.path}/${id}`, { ...as, body: sent });

  if (updated.status >= 300) {
    await call('DELETE', `${subject.path}/${id}`, as);
    return { name: subject.name, skipped: `update → ${updated.status} ${updated.body.slice(0, 200)}` };
  }

  const read = json(await call('GET', `${subject.path}/${id}`, as)) || json(updated) || {};

  const dropped = Object.entries(subject.after)
    .filter(([key, want]) => {
      const got = read[key];

      if (got === undefined) return false;          // not part of this shape
      if (typeof got === 'string' && typeof want === 'string') {
        return !got.startsWith(want.slice(0, 10));  // dates come back with a time
      }
      return got !== want;
    })
    .map(([key, want]) => `${key}: sent ${JSON.stringify(want)}, read back ${JSON.stringify(read[key])}`);

  const absent = Object.keys(subject.after).filter(key => read[key] === undefined);

  await call('DELETE', `${subject.path}/${id}`, as);

  return { name: subject.name, dropped, absent };
}

(async () => {
  const token = await signIn();

  console.log(`round-trip probe against ${base.origin}, tenant ${TENANT}\n`);

  let suspect = 0;

  const results = [];

  for (const subject of SUBJECTS) {
    results.push(await probe(token, subject));
  }

  results.push(await probeStaffPassword(token));

  for (const result of results) {
    if (result.skipped) {
      console.log(`  ${result.name}: skipped — ${result.skipped}`);
      continue;
    }

    if (result.dropped.length === 0) {
      console.log(`  ${result.name}: every field came back as sent`);
    } else {
      suspect += result.dropped.length;
      console.log(`  ${result.name}: ${result.dropped.length} problem(s)`);
      result.dropped.forEach(line => console.log(`      ${line}`));
    }

    if (result.absent.length > 0) {
      // Not a defect on its own — the read shape may simply be narrower than
      // the write shape — but it is where a dropped field hides.
      console.log(`      (not visible on read: ${result.absent.join(', ')})`);
    }
  }

  console.log(`\n${suspect} field(s) worth a look.`);
})().catch(err => {
  console.error(err.message);
  process.exit(1);
});
