// Three real organisations, with real data, on one deployment.
//
//   node tools/seed-tenants.js [https://localhost:44373]
//
// A trading academy, a language centre and a recruitment firm — the three
// audiences this product claims to serve. Each gets its own catalogue in its own
// vocabulary, its own question bank, exams with named papers, classes at levels,
// candidates, and sittings that have actually been taken and marked.
//
// It exists for two reasons. The first is that every screen in this product
// looks fine with no data in it and only tells the truth with data in it: an
// empty results roster proves nothing, and a topic breakdown with one topic
// proves less. The second is that multi-tenancy is the claim this product makes
// most often and had never once been exercised — a defect found this week meant
// every image on a paper 404'd for any tenant but the host, and nothing in the
// suite could have seen it because the suite only ever had one tenant.
//
// Re-runnable: tenants that already exist are reused rather than duplicated.

const https = require('https');
const http = require('http');

const base = new URL(process.argv[2] ?? 'https://localhost:44373');
const client = base.protocol === 'https:' ? https : http;
const agent = base.protocol === 'https:' ? new https.Agent({ rejectUnauthorized: false }) : undefined;

const HOST_ADMIN = { username: 'admin', password: '1q2w3E*' };

// The password every seeded tenant's administrator gets. Development only, and
// obvious enough that nobody mistakes it for a real one.
const TENANT_ADMIN_PASSWORD = '1q2w3E*';

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
    throw new Error(`Could not sign in as ${username}${tenant ? ' @ ' + tenant : ''}: ${res.body.slice(0, 300)}`);
  }

  return JSON.parse(res.body).access_token;
}

// ---------------------------------------------------------------- the tenants

const TENANTS = [
  {
    name: 'trading-academy',
    organisation: 'أكاديمية المسار للتداول',
    vocabulary: {
      singularName: 'مسار',
      pluralName: 'المسارات',
      subjectSingularName: 'متدرّب',
      subjectPluralName: 'المتدرّبون',
      groupSingularName: 'دفعة',
      groupPluralName: 'الدفعات',
    },
    categories: [
      {
        code: 'tech-analysis',
        name: 'التحليل الفني',
        levels: [
          { code: 'ta-1', name: 'المستوى الأول' },
          { code: 'ta-2', name: 'المستوى الثاني' },
          { code: 'ta-3', name: 'المستوى الثالث' },
        ],
        topics: ['قراءة الشموع', 'الدعم والمقاومة', 'المؤشّرات', 'إدارة المخاطر'],
      },
      {
        code: 'risk',
        name: 'إدارة رأس المال',
        levels: [{ code: 'risk-1', name: 'أساسي' }],
        topics: ['حجم الصفقة', 'وقف الخسارة'],
      },
    ],
    exams: [
      {
        title: 'اختبار نهاية المستوى الأول — التحليل الفني',
        category: 'tech-analysis',
        level: 'ta-1',
        minutes: 45,
        pass: 60,
        questions: [
          ['ما الذي تعنيه الشمعة ذات الجسم الصغير والظلّين الطويلين؟', 'تردّد بين المشترين والبائعين', 'اتجاه صاعد مؤكَّد', 'قراءة الشموع'],
          ['متى يتحوّل مستوى المقاومة إلى دعم؟', 'بعد اختراقه صعوداً وإعادة اختباره', 'عند إغلاق السوق', 'الدعم والمقاومة'],
          ['ماذا يقيس مؤشّر القوة النسبية؟', 'سرعة تغيّر السعر وحجمه', 'عدد المتداولين في السوق', 'المؤشّرات'],
          ['ما الحدّ الأقصى المعقول للمخاطرة في صفقة واحدة؟', 'واحد إلى اثنين بالمئة من رأس المال', 'خمسون بالمئة من رأس المال', 'إدارة المخاطر'],
          ['ما وظيفة أمر وقف الخسارة؟', 'إغلاق الصفقة تلقائياً عند حدّ خسارة محدّد', 'مضاعفة حجم الصفقة عند الخسارة', 'إدارة المخاطر'],
          ['ماذا يعني حجم التداول المرتفع عند كسر مستوى؟', 'قوّة في الحركة تدعم استمرارها', 'أن الحركة كاذبة دائماً', 'الدعم والمقاومة'],
        ],
      },
    ],
    groups: [
      { name: 'دفعة يناير — المستوى الأول', category: 'tech-analysis', level: 'ta-1' },
      { name: 'دفعة مارس — المستوى الثاني', category: 'tech-analysis', level: 'ta-2' },
    ],
    candidates: [
      'أحمد الشمري', 'نورة العتيبي', 'خالد الدوسري', 'ريم القحطاني',
      'سلطان الحربي', 'دانة المطيري', 'فهد الزهراني', 'لمياء السبيعي',
    ],
  },
  {
    name: 'language-centre',
    organisation: 'مركز النور للغات',
    vocabulary: {
      singularName: 'لغة',
      pluralName: 'اللغات',
      subjectSingularName: 'طالب',
      subjectPluralName: 'الطلاب',
      groupSingularName: 'شعبة',
      groupPluralName: 'الشُّعَب',
    },
    categories: [
      {
        code: 'english',
        name: 'الإنجليزية',
        levels: [
          { code: 'en-a1', name: 'A1' },
          { code: 'en-a2', name: 'A2' },
          { code: 'en-b1', name: 'B1' },
          { code: 'en-b2', name: 'B2' },
        ],
        topics: ['القواعد', 'الاستماع', 'القراءة', 'الكتابة', 'المفردات'],
      },
      {
        code: 'french',
        name: 'الفرنسية',
        levels: [
          { code: 'fr-a1', name: 'A1' },
          { code: 'fr-a2', name: 'A2' },
        ],
        topics: ['القواعد', 'المفردات'],
      },
    ],
    exams: [
      {
        title: 'اختبار تحديد المستوى — الإنجليزية',
        category: 'english',
        level: 'en-a1',
        minutes: 40,
        pass: 50,
        questions: [
          ['She ____ to school every day.', 'goes', 'go', 'القواعد'],
          ['I have lived here ____ 2019.', 'since', 'for', 'القواعد'],
          ['Choose the word closest in meaning to "difficult".', 'hard', 'simple', 'المفردات'],
          ['They ____ watching a film when I called.', 'were', 'was', 'القواعد'],
          ['The opposite of "always" is ____.', 'never', 'often', 'المفردات'],
          ['If it ____ tomorrow, we will stay at home.', 'rains', 'will rain', 'القواعد'],
          ['Which sentence is correct?', 'He does not like coffee.', 'He do not likes coffee.', 'القواعد'],
          ['A synonym for "begin" is ____.', 'start', 'finish', 'المفردات'],
        ],
      },
    ],
    groups: [
      { name: 'شعبة A1 — مساء الثلاثاء', category: 'english', level: 'en-a1' },
      { name: 'شعبة A2 — صباح السبت', category: 'english', level: 'en-a2' },
      { name: 'شعبة B1 — مساء الأحد', category: 'english', level: 'en-b1' },
    ],
    candidates: [
      'سارة إبراهيم', 'يوسف منصور', 'هدى كمال', 'عمر الخطيب',
      'ليان الأحمد', 'زياد فارس', 'مريم سعيد', 'طارق الحلبي',
      'جنى نصّار', 'باسل الرفاعي', 'رنا الخوري', 'وسيم داود',
    ],
  },
  {
    name: 'recruitment',
    organisation: 'شركة آفاق للتوظيف',
    vocabulary: {
      singularName: 'مجال',
      pluralName: 'المجالات',
      subjectSingularName: 'مرشّح',
      subjectPluralName: 'المرشّحون',
      groupSingularName: 'دفعة توظيف',
      groupPluralName: 'دفعات التوظيف',
    },
    categories: [
      {
        code: 'software',
        name: 'تطوير البرمجيات',
        levels: [
          { code: 'sw-junior', name: 'مبتدئ' },
          { code: 'sw-mid', name: 'متوسط' },
          { code: 'sw-senior', name: 'خبير' },
        ],
        topics: ['أساسيات البرمجة', 'قواعد البيانات', 'حلّ المشكلات'],
      },
      {
        code: 'accounting',
        name: 'المحاسبة',
        levels: [{ code: 'acc-junior', name: 'مبتدئ' }],
        topics: ['القيود', 'التقارير المالية'],
      },
    ],
    exams: [
      {
        title: 'اختبار فرز — مطوّر برمجيات مبتدئ',
        category: 'software',
        level: 'sw-junior',
        minutes: 30,
        pass: 55,
        questions: [
          ['ما ناتج تنفيذ حلقة تكرارية تبدأ من صفر وتنتهي قبل خمسة؟', 'خمس دورات', 'ست دورات', 'أساسيات البرمجة'],
          ['ما الغرض من الفهرس في قاعدة البيانات؟', 'تسريع البحث عن الصفوف', 'تقليل حجم الملف دائماً', 'قواعد البيانات'],
          ['أي بنية بيانات تعمل بمبدأ الداخل أولاً خارج أخيراً؟', 'المكدّس', 'الطابور', 'أساسيات البرمجة'],
          ['ما الفرق بين المفتاح الأساسي والمفتاح الأجنبي؟', 'الأساسي يعرّف الصف والأجنبي يشير إلى جدول آخر', 'لا فرق بينهما', 'قواعد البيانات'],
          ['كيف تتعامل مع خطأ يظهر عند المستخدم ولا يظهر عندك؟', 'أجمع خطوات إعادة الإنتاج والسجلّات قبل التعديل', 'أغيّر الكود حتى يختفي', 'حلّ المشكلات'],
        ],
      },
    ],
    groups: [
      { name: 'دفعة التوظيف — الربع الأول', category: 'software', level: 'sw-junior' },
    ],
    candidates: [
      'محمد العلي', 'أسماء بن صالح', 'كريم حدّاد', 'ندى الياسين',
      'رامي شعبان', 'ملك الصايغ',
    ],
  },
];

// ------------------------------------------------------------------ the work

async function ensureTenant(hostToken, name) {
  const existing = await json('GET', '/api/multi-tenancy/tenants?maxResultCount=100', { token: hostToken });
  const found = existing.items.find(t => t.name === name);

  if (found) {
    console.log(`  tenant "${name}" already exists`);
    return found.id;
  }

  const created = await json('POST', '/api/multi-tenancy/tenants', {
    token: hostToken,
    body: {
      name,
      adminEmailAddress: `admin@${name}.test`,
      adminPassword: TENANT_ADMIN_PASSWORD,
    },
  });

  console.log(`  created tenant "${name}"`);

  return created.id;
}

async function seedTenant(spec) {
  console.log(`\n── ${spec.organisation} ──`);

  const hostToken = await signIn(HOST_ADMIN);
  const tenantId = await ensureTenant(hostToken, spec.name);

  const token = await signIn({ username: 'admin', password: TENANT_ADMIN_PASSWORD, tenant: spec.name });
  const as = { token, tenant: spec.name };

  // Its own words, first: everything else is read through them.
  await json('PUT', '/api/assessment/settings', {
    ...as,
    body: {
      organizationName: spec.organisation,
      defaultLanguage: 'ar',
      timeZone: 'Asia/Riyadh',
      defaultPassingPercentage: 60,
      showResultToCandidate: true,
      collectIntegritySignals: true,
      enableSelfRegistration: false,
    },
  });

  await json('PUT', '/api/assessment/catalog/vocabulary', { ...as, body: spec.vocabulary });

  // ---------------------------------------------------------- the catalogue
  const catalogue = new Map();

  for (const category of spec.categories) {
    const existing = await json('GET', '/api/assessment/catalog/categories?includeInactive=true', as);
    let row = existing.find(c => c.code === category.code);

    if (!row) {
      row = await json('POST', '/api/assessment/catalog/categories', {
        ...as,
        body: { name: category.name, code: category.code, displayOrder: 0, isActive: true },
      });
    }

    const levels = new Map();
    const topics = new Map();

    for (const [index, level] of category.levels.entries()) {
      const known = (await json('GET', '/api/assessment/catalog/categories', as))
        .find(c => c.id === row.id);

      let existingLevel = known?.levels.find(l => l.code === level.code);

      if (!existingLevel) {
        existingLevel = await json('POST', '/api/assessment/catalog/levels', {
          ...as,
          body: {
            categoryId: row.id,
            name: level.name,
            code: level.code,
            displayOrder: index + 1,
            isActive: true,
          },
        });
      }

      levels.set(level.code, existingLevel.id);
    }

    for (const [index, topic] of category.topics.entries()) {
      const code = `${category.code}-t${index + 1}`;
      const known = (await json('GET', '/api/assessment/catalog/categories', as))
        .find(c => c.id === row.id);

      let existingTopic = known?.topics.find(t => t.code === code);

      if (!existingTopic) {
        existingTopic = await json('POST', '/api/assessment/catalog/topics', {
          ...as,
          body: { categoryId: row.id, name: topic, code, displayOrder: index, isActive: true },
        });
      }

      topics.set(topic, existingTopic.id);
    }

    catalogue.set(category.code, { id: row.id, levels, topics });
  }

  console.log(`  catalogue: ${spec.categories.length} domains`);

  // ---------------------------------------------------------------- people
  const candidates = [];

  for (const [index, fullName] of spec.candidates.entries()) {
    const email = `${spec.name}-${index + 1}@example.test`;
    const existing = await json(
      'GET',
      `/api/assessment/candidates?filter=${encodeURIComponent(email)}&maxResultCount=5`,
      as,
    );

    const row = existing.items[0] ?? await json('POST', '/api/assessment/candidates', {
      ...as,
      body: { fullName, email },
    });

    candidates.push(row);
  }

  console.log(`  ${candidates.length} ${spec.vocabulary.subjectPluralName}`);

  // --------------------------------------------------------------- classes
  const groups = await json('GET', '/api/assessment/candidates/groups', as);
  const created = [];

  for (const group of spec.groups) {
    const domain = catalogue.get(group.category);
    let row = groups.find(g => g.name === group.name);

    if (!row) {
      row = await json('POST', '/api/assessment/candidates/groups', {
        ...as,
        body: {
          name: group.name,
          categoryId: domain.id,
          levelId: domain.levels.get(group.level),
        },
      });
    }

    created.push(row);
  }

  // Everybody in the first class; the rest spread across the others, so the
  // results screen has a class filter that actually separates people.
  for (const [index, group] of created.entries()) {
    const members = candidates
      .filter((_, i) => index === 0 || i % created.length === index)
      .map(c => c.id);

    await json('PUT', `/api/assessment/candidates/groups/${group.id}/members`, {
      ...as,
      body: { candidateIds: members },
    });
  }

  console.log(`  ${created.length} ${spec.vocabulary.groupPluralName}`);

  // ----------------------------------------------------------------- exams
  const exams = [];

  for (const examSpec of spec.exams) {
    const domain = catalogue.get(examSpec.category);

    const existing = await json('GET', '/api/assessment/exams?maxResultCount=100', as);
    let exam = existing.items.find(e => e.title === examSpec.title);

    if (!exam) {
      exam = await json('POST', '/api/assessment/exams', {
        ...as,
        body: {
          title: examSpec.title,
          timeLimitInMinutes: examSpec.minutes,
          passingPercentage: examSpec.pass,
          categoryId: domain.id,
          levelId: domain.levels.get(examSpec.level),
          shuffleQuestions: true,
          shuffleOptions: true,
          oneQuestionAtATime: true,
          allowBackNavigation: true,
          collectIntegritySignals: true,
        },
      });

      for (const [index, [text, right, wrong, topic]] of examSpec.questions.entries()) {
        await json('POST', '/api/assessment/questions', {
          ...as,
          body: {
            examId: exam.id,
            topicId: domain.topics.get(topic),
            type: 'single-choice',
            text,
            score: 1,
            difficulty: 1,
            displayOrder: index,
            isActive: true,
            payload: JSON.stringify({
              options: [
                { id: 'a', text: right, isCorrect: true },
                { id: 'b', text: wrong, isCorrect: false },
              ],
            }),
          },
        });
      }

      await json('POST', `/api/assessment/exams/${exam.id}/publish`, as);
    }

    exams.push({ exam, spec: examSpec, domain });
  }

  console.log(`  ${exams.length} exams, published`);

  // ------------------------------------------------------- papers per exam
  for (const { exam } of exams) {
    const forms = await json(`GET`, `/api/assessment/exam-structure/forms/by-exam/${exam.id}`, as);

    if (forms.length > 0) {
      continue;
    }

    const questions = await json(
      'GET',
      `/api/assessment/questions?examId=${exam.id}&maxResultCount=100`,
      as,
    );

    const ids = questions.items.map(q => q.id);
    const half = Math.max(2, Math.floor(ids.length / 2));

    // Two papers, drawn from opposite halves, so a retake is a genuinely
    // different set of questions rather than a redraw that repeats most of it.
    for (const [index, chosen] of [ids.slice(0, half), ids.slice(half)].entries()) {
      if (chosen.length === 0) {
        continue;
      }

      const form = await json('POST', '/api/assessment/exam-structure/forms', {
        ...as,
        body: { examId: exam.id, name: `النموذج ${index + 1}`, code: `F${index + 1}` },
      });

      await json('PUT', `/api/assessment/exam-structure/forms/${form.id}/questions`, {
        ...as,
        body: { questionIds: chosen },
      });

      await json('POST', `/api/assessment/exam-structure/forms/${form.id}/publish`, as);
    }
  }

  console.log('  2 named papers per exam');

  // ------------------------------------------------------------- sittings
  let sat = 0;

  for (const { exam } of exams) {
    const already = await json(`GET`, `/api/assessment/results?examId=${exam.id}&maxResultCount=1`, as);

    if (already.totalCount > 0) {
      continue;
    }

    const sent = await json('POST', '/api/assessment/assignments', {
      ...as,
      body: {
        examId: exam.id,
        candidateGroupId: created[0].id,
        rotateForms: true,
        expiresAt: new Date(Date.now() + 30 * 864e5).toISOString(),
        maxAttempts: 2,
        sendEmail: false,
      },
    });

    // Most of them sit it; a couple never open the link, because "never
    // started" is a real number a coordinator chases and a roster where
    // everybody turned up is not a roster anybody has ever seen.
    const sitting = sent.recipients.slice(0, Math.max(1, sent.recipients.length - 2));

    for (const [index, recipient] of sitting.entries()) {
      const linkToken = recipient.url.split('/').pop();

      // As the candidate, with no token at all: their link is the whole
      // credential, and this is the path that had never been exercised for any
      // tenant but the host.
      const preview = await json('GET', `/api/assessment/take/${linkToken}`);
      const started = await json('POST', '/api/assessment/take/start', {
        session: preview.sessionToken,
        body: {},
      });

      const session = started.sessionToken;

      // Answered with a spread of ability, so the roster is not a column of
      // hundreds and the item statistics have something to separate.
      const strength = [1, 0.85, 0.7, 0.55, 0.4][index % 5];

      for (let position = 0; position < started.totalQuestions; position++) {
        const question = await json(
          'GET',
          `/api/assessment/take/question/${position}`,
          { session },
        );

        // Every seeded question is written with the right answer as option "a".
        // The order arrives shuffled and correctness is stripped — as it must be —
        // so the id is the only thing that survives to answer against.
        const correct = question.options.find(o => o.id === 'a') ?? question.options[0];
        const other = question.options.find(o => o.id !== correct.id) ?? correct;
        const answer = (position + 1) / started.totalQuestions <= strength ? correct : other;

        await json('PUT', '/api/assessment/take/answer', {
          session,
          body: {
            questionId: question.id,
            response: JSON.stringify([answer.id]),
            timeSpentSeconds: 20 + (position * 7) % 40,
          },
        });
      }

      await json('POST', '/api/assessment/take/submit', { session, body: {} });
      sat++;
    }
  }

  console.log(`  ${sat} sittings taken and marked`);

  return {
    organisation: spec.organisation,
    exams: exams.length,
    people: candidates.length,
    classes: created.length,
    sittings: sat,
  };
}

(async () => {
  console.log(`Seeding three organisations against ${base.origin}\n`);

  const summary = [];

  for (const spec of TENANTS) {
    summary.push(await seedTenant(spec));
  }

  console.log('\nDone.\n');
  console.table(summary);
})().catch(err => {
  console.error('\n' + err.message);
  process.exit(1);
});
