# أسطرلاب · Astrolabe

**منصّة تقييم واختبارات عربيّة أوّلاً، لا تخصّ مجالاً بعينه.**
**An Arabic-first, domain-agnostic online assessment platform.**

جهةٌ تكتب أسئلتها بنفسها، وتعتمد الورقة قبل خروجها، وترسلها إلى شعبة، ثمّ تقرأ ما
عاد. هذا كلّ ما تفعله المنصّة، وهو ما تفعله جيّداً.

An organisation writes its own questions, approves the exact paper before it goes
out, sends it to a class, and reads what came back. That is the whole product.

> **عن اسم المستودع.** بدأ هذا المشروع باسم *Internship Management System*، وما
> زال الاسم في أسماء المشاريع والنطاقات ومسارات الملفّات وسلسلة الاتّصال. المنتج
> تغيّر؛ المُعرِّفات البرمجيّة لم تُهاجَر بعد. حيث ترى `InternshipManagementSystem`
> في شيفرة أو إعداد، اقرأها «أسطرلاب».
>
> **On the repository name.** This began as an *Internship Management System*, and
> that name still runs through the project files, the namespaces, the paths and the
> connection string. The product moved on; the identifiers have not been migrated.
> Read `InternshipManagementSystem` as "Astrolabe" wherever it appears.

---

## ١ · لمن هذه المنصّة | Who it is for

الجهات الثلاث التي يبذرها `tools/seed-tenants.js` على نشرٍ واحد هي الجهات التي
يقصدها المنتج:

The three organisations that `tools/seed-tenants.js` puts on a single deployment
are the three the product is built for:

- **أكاديميّة تدريب** — a training academy, assessing at levels.
- **مركز لغات** — a language school, placing and certifying by skill.
- **شركة توظيف** — a recruitment firm, screening applicants before an interview.

كلّ جهة لها كتالوجها وبنك أسئلتها ونتائجها، ولا ترى شيئاً من غيرها.

Each holds its own catalogue, question bank and results, and sees nothing
belonging to another.

---

## ٢ · ما تفعله المنصّة | What the product does

كلّ بند هنا يستطيع إنسانٌ أن يمشيه من المتصفّح اليوم. ما لا يستطيع، في القسم
الثالث.

Every item below can be walked from a browser today. What cannot is in §3.

### تعدّد الجهات | Multi-tenancy
نشرٌ واحد يخدم عدّة جهات (`MultiTenancyConsts.IsEnabled = true`). تُفصَل الجهات
بمُرشِّح بيانات ABP، ويُحدَّد المُستأجِر في الطلب بترويسة `__tenant` أو بادّعاء
الجهة داخل رمز الدخول. حاويات الملفّات لكلّ جهة على حدة. والممتحَن الذي يفتح
رابطه لا يحمل حساباً ولا ترويسة — فمسار الامتحان يُعطِّل المُرشِّح ويستعيض عنه
بتحقّق صريح: الجلسة تُطابَق بمعرّف الممتحَن ومعرّف الجهة معاً في
`LoadOwnAttemptAsync`، وإلّا رُفضت.

One deployment, several organisations, told apart by ABP's data filter. A request
names its tenant with the `__tenant` header or with the tenant claim in its access
token; BLOB containers are per tenant. A candidate carries neither, so the taking
path disables the filter and replaces it with an explicit check — the session must
match both the candidate id and the tenant id on the attempt.

### الكتالوج | The catalogue
المجالات، والمستويات داخلها، وشجرة الموضوعات (الكفاءات). المستوى أو الموضوع بلا
مجال ينطبق على كلّ المجالات. عليه تقوم أربعة أشياء: بنك الأسئلة المشترك، وتفصيل
النتيجة حسب الموضوع، ومخطّط الورقة، وربط الشعبة بمستوى. `‎/catalog‎`

Domains, the levels inside them, and a competency tree; a level or topic with no
domain applies to all of them. Four capabilities rest on it: the shared bank, the
topic breakdown on a result, blueprints, and a class sitting at a level.

### بنك الأسئلة | The question bank
ثلاثة عشر نوع سؤال، لكلٍّ محرّره الخاصّ ولا JSON في أيّ خانة: اختيار واحد، اختيار
متعدّد، صحّ أو خطأ، إجابة مكتوبة، إجابة رقميّة، كود، مطابقة، ترتيب، ملء الفراغات،
تحديد على صورة، مقياس متدرّج، رفع ملفّ، إجابة صوتيّة. السؤال يُملَك على مستوى
المجال والمستوى لا على مستوى اختبار واحد، فتسحبه كلّ اختبارات ذلك المستوى
(`Question.DrawableBy`). `‎/questions‎` و`‎/exams/:examId/questions‎`

Thirteen question types, each with its own editor and no raw JSON anywhere. A
question is owned at a domain and level rather than by one exam, so every exam at
that level can draw it.

### القطع والوسائط | Passages and media
قطعة واحدة — نصّ قراءة أو مقطع استماع أو صورة — تُبنى عليها عدّة أسئلة، ويراها
الممتحَن مرّة واحدة بجانب كلٍّ منها. والملفّ يُخدَم إلى متصفّح الممتحَن بمنحة
موقّعة تُسمّي ذلك الملفّ بعينه، فلا تُعاد على غيره. `‎/exams/:examId/structure‎`

### مخطّط الورقة والسحب | Blueprints and drawn papers
قاعدة لكلّ نوع وصعوبة وموضوع — «ستّ قواعد، أربع استماع، اثنتان منها صعبتان» —
والشاشة تقول لكلّ قاعدة كم سؤالاً في البنك يحقّقها. فيحصل كلّ ممتحَن على ورقة
مسحوبة مختلفة، متساوية التركيب مع غيرها، فتصير الدرجتان قابلتين للمقارنة. والخلط
يجري ببذرة محفوظة على المحاولة، فالورقة نفسها تُعاد بناؤها بعد شهور عند أيّ نزاع.
`‎/exams/:examId/blueprint‎`

A blueprint states how many questions of what type, difficulty and competency each
drawn paper carries, and the editor shows how many bank questions satisfy each
rule. Shuffling runs from a seed stored on the attempt, so the same paper can be
reproduced months later in a dispute.

### النماذج المسمّاة | Named forms
ورقة ثابتة بأسئلة ثابتة وترتيب ثابت — «النموذج ١»، «النموذج ٢» — تُعبَّأ من
المخطّط بالبنّاء نفسه الذي يبني ورقة الممتحَن، أو تُبنى يدوياً، ثمّ تُنشر. المنشور
لا يُعدَّل: من جلسوا له يجب أن يكونوا أجابوا الورقة نفسها؛ والمُستعمَل لا يُحذف بل
يُؤرشَف. والإسناد يختار نموذجاً بعينه أو **التدوير**، فيأخذ كلّ جلوس النموذج
التالي وتكون الإعادة ورقةً مختلفة فعلاً. `‎/exams/:examId/forms‎`

### الإرسال والرابط | Assignment and the link
**الرابط هو كامل بطاقة الممتحَن. لا حسابات للممتحَنين، ولا كلمات مرور، ولا رمز
يُكتب في شاشة.** كلّ شخص يحصل على رمزٍ خاصّ به (٢٥٦ بتاً عشوائيّة)، له تاريخ
انتهاء وعدد محاولات. الرمز يُخزَّن مُجزَّأً بـ SHA-256 وتُحفَظ منه ثمانية محارف
للتعريف فقط، فلا يمكن استرجاعه بعد عرضه: تُنسخ الروابط من لوحة تظهر مرّة واحدة،
أو تُرسَل بالبريد. وثلاثة أفعال بعد الإرسال: **الإلغاء** لمن تسرّب رابطه،
و**إعادة الإصدار** لمن فقده — عنوان جديد يُبطل القديم ولا يمنح محاولة إضافيّة —
و**التمديد** إلى الأمام فقط. `‎/assignments/:examId‎`

**The link is the candidate's entire credential. Candidates never have accounts,
passwords, or a code to type into a screen.** Each person gets their own 256-bit
token with an expiry and an attempt allowance. It is stored as a SHA-256 hash plus
its first eight characters, so it cannot be read back: links are copied from a
panel shown once, or emailed. A link can then be **revoked**, **reissued** (a new
address that kills the old one and buys no extra attempt) or **extended**,
forwards only.

### الجلوس للامتحان | Sitting the exam
`‎/exam/:token‎` خارج الغلاف وخارج المصادقة. الرمز يُبدَّل مرّةً برمز جلسة موقَّع
قصير العمر يبقى في الذاكرة وحدها ويُرسَل في ترويسة `X-Exam-Session`. **سؤال واحد
في كلّ مرّة** فلا تصل الورقة كاملةً إلى المتصفّح؛ **والمؤقّت على الخادم** يُحسب
من موعد مخزون، فإغلاق الصفحة أو انقطاع الاتّصال لا يوقفه؛ والحفظ تلقائيّ،
والتسليم ينتظر أيّ حفظ معلّق قبل أن يمضي. وفتح الرابط لا يستهلك محاولة — زرّ
البدء وحده يفعل. ومن انقضى وقته يُسلَّم امتحانه ويُصحَّح بعامل خلفيّ يمرّ كلّ
دقيقة، لا بمتصفّحه.

`/exam/:token` sits outside the shell and outside authentication. The URL token is
exchanged once for a short-lived signed session token held in memory only and sent
as `X-Exam-Session`. One question at a time, so the whole paper never reaches the
browser; the clock is computed from a stored deadline on the server; answers
autosave and submit waits for any save still in flight. Opening the link costs
nothing — only Start consumes an attempt. A background sweep submits and grades
anyone whose time ran out, rather than relying on their browser.

### التصحيح | Marking
**لكلّ نوع سؤال استراتيجيّة تصحيح خاصّة به**: `IQuestionGrader` يُختار عبر
`IGraderResolver` بحسب نوع السؤال. تسعة أنواع تُصحَّح آليّاً، وأربعة يقرؤها إنسان.
وثلاث حمايات مكتوبة في الشيفرة: نوعٌ بلا مصحّح مسجَّل يُحال إلى إنسان لا يُصفَّر؛
ومصحّحٌ يرمي استثناءً يُحوَّل إلى تصحيح يدويّ فلا يسقط التسليم كلّه؛ وإجابة لا
يستطيع مصحّحها قراءتها تُحال إلى إنسان. وطابور `‎/review‎` يعرض ما ينتظر الأقدم
أوّلاً، مع سلّم التقييم إن وُجد، ومفتاح الإجابة، وملاحظة تصل الممتحَن مع نتيجته.
ورصد درجة يُعيد حساب مجموع المحاولة فوراً.

**Each question type has its own grader strategy**, resolved by type. Nine types
are marked automatically and four are read by a person. Three safety nets are in
the code: a type with no registered grader goes to a human rather than to zero; a
grader that throws is converted to manual review so one bad answer cannot fail a
whole submission; and an answer a grader cannot parse goes to a human. The queue
at `/review` shows what is waiting, oldest first, with the rubric, the key, and a
comment field whose text reaches the candidate.

### مؤشّرات النزاهة — ملاحظات لا أحكام | Integrity signals, as observations
اللصق ومغادرة النافذة يُسجَّلان ويُعرضان للمصحّح بوصفهما **«ما لوحظ»**، لا حكماً.
لا يخصم النظام درجةً بسببهما ولا يُعلِّم أحداً بالغشّ. وهما محجوبان خلف صلاحيّة
منفصلة `Review.ViewIntegritySignals` لأنّهما بيانات سلوكيّة عن شخص — وكشف النتائج
نفسه يُصفّر عدّاد المؤشّرات لمن لا يملك تلك الصلاحيّة.

Paste and window-blur are recorded and shown to the marker as *what was observed*,
never as a verdict. The software deducts nothing and accuses nobody. They sit
behind their own permission because they are behavioural data about a person — and
the results roster zeroes the flag count for anyone who does not hold it.

### النتائج وتحليل جودة الأسئلة | Results and item analysis
كشفٌ بكلّ جلوس ودرجته وزمنه، فوقه ملخّص (متوسّط، وسيط، مدى، ناجحون، راسبون،
بانتظار التصحيح، ولم يبدأوا) محسوبٌ على ما رشّحته الشاشة لا على الاختبار كلّه.
وورقة إجابة سؤالاً بسؤال مع تفصيل حسب الموضوع. وتصدير CSV — يُنزَع فتيل أيّ خليّة
تبدأ بـ `=` أو `+` أو `-` أو `@` فلا تُنفَّذ صيغةً على جهاز المنسّق.
و**تحليل جودة الأسئلة** `‎/results/questions‎`: نسبة الإصابة وقوّة التمييز لكلّ
سؤال عبر كلّ الجلسات، والسؤال الذي لا تكفي جلساته أو الذي انقسمت فئتاه على ورقتين
مختلفتين يُقال عنه **«غير قابل للقياس»** لا يُعطى صفراً ولا يُتّهم مفتاحه.
و`‎/results/running‎` تعرض الجلسات الجارية وتُحدَّث من تلقائها، ومنها يُنهي المنسّق
جلسةً تعطّل متصفّحها **ويكتب السبب** — ويُرفض حذف محاولة صُحِّحت.

A roster with headline figures computed over the filter you are looking at, an
answer sheet with a competency breakdown, a CSV export that defuses formula
injection, per-question facility and discrimination that report *unmeasurable*
rather than a false zero, and a live monitor from which a coordinator can end a
stuck sitting and record why. A graded attempt cannot be deleted.

### الاستيراد بالجملة | Bulk import
- **الأشخاص** — تُلصق قائمة، شخص في كلّ سطر، من جدول بيانات مباشرةً: الفواصل
  والجدولات كلاهما يعمل، والبريد يُكتشف بموضعه أيًّا كان في السطر.
- **الأسئلة** — ملفّ CSV، سؤال في كلّ صفّ، بأعمدة تُطابَق بالعربيّة والإنجليزيّة:
  النوع، السؤال، خيار ١–٤، الإجابة الصحيحة، الدرجة، الصعوبة، التفسير. و«الإجابة
  الصحيحة» تقبل رقم الخيار، أو أرقاماً، أو الإجابة مكتوبةً كما هي.

كلاهما **يُعرَض قبل أن يُكتب**: تجربة جافّة تُظهر ما سيُنشأ وما هو خطأ بسطره
وعموده، وصفٌّ فاسد واحد لا يُكلّف الصفوف السليمة، والمكرّر يُترك كما هو. وكلّ صفّ
مستورَد يمرّ بالتحقّق نفسه الذي يمرّ به السؤال المكتوب يدوياً، فلا يصير الاستيراد
طريقاً حول ما يمنع سؤالاً غير قابل للتصحيح من بلوغ ممتحَن.

Both dry-run first, report per-row errors by line and column, leave good rows
unaffected by a bad one, skip what is already there, and put imported questions
through the same validation as hand-written ones.

### هويّة الجهة | Per-tenant branding
اسم الجهة وشعارها ولونها في `‎/settings‎`. **الاسم والشعار** يظهران للممتحَن على
شاشة فتح الرابط؛ **والاسم واللون** في رسالة الدعوة. من يفتح رابطاً ليؤدّي اختباراً
لا تربطه بنا علاقة، فما يراه هو الجهة لا نحن.

### العربيّة والاتّجاه | Arabic and RTL
العربيّة هي لغة المنتج الأولى لا ترجمةً له، والاتّجاه من اليمين إلى اليسار من
الدرجة الأولى. كلّ مفتاح نصّ يطلبه العميل مُعرَّف بالعربيّة والإنجليزيّة، ويحرس
ذلك فحصٌ في `tools/check-localization.py` — لأنّ المفتاح الناقص لا يُسقط شيئاً ولا
يُنبّه أحداً، إنّما يظهر في الشاشة مكان جملة. والأرقام والتواريخ والبُرد تُعزَل
داخل عناصر خاصّة بها كي لا تُعيد الفقرة العربيّة ترتيبها.

Arabic is the product's first language, not a translation of it, and RTL is
first-class. Every text key the client asks for is defined in both languages and a
check enforces it — a missing key throws nothing and warns nobody, it just appears
on screen where a sentence should be. Numbers, dates and email addresses are
isolated so an Arabic paragraph does not reorder them.

---

## ٣ · ما لا تفعله المنصّة | What the product does **not** do

هذا القسم مقصود. الوثيقة التي تَعِد بما ليس موجوداً أسوأ من الوثيقة الناقصة.

This section is deliberate. A document that promises what is not there is worse
than one that is merely incomplete.

### أ · غير موجود أصلاً | Absent outright

| | |
|---|---|
| **حسابات للممتحَنين** | لا حساب ولا كلمة مرور ولا سجلّ يدخل إليه الممتحَن. الرابط هو كلّ شيء. **قرارٌ، لا نقص.** *By design.* |
| **استيراد امتحان من Word أو Google Forms** | لا مُحلِّل ولا شاشة ولا مسار. استيراد الأسئلة من CSV هو الطريق الوحيد الداخل. *No document importer.* |
| **الشهادات** | لا شهادة ولا وثيقة نتيجة قابلة للطباعة. *No certificate.* |
| **المراقبة** | لا كاميرا، ولا مشاركة شاشة، ولا متصفّح مقفل. مؤشّرات النزاهة كلّ ما يوجد، وهي ملاحظات. *No proctoring of any kind.* |
| **تنفيذ الكود** | سؤال الكود تُقارَن مخرجاته المتوقّعة نصّاً سطراً بسطر؛ لا يُنفَّذ شيء. وبلا مخرجات متوقّعة يصير السؤال يدويّ التصحيح. *Nothing is executed.* |
| **التنصيب على خوادم العميل** | لا حزمة تنصيب محلّيّ. *No on-premises installation.* |
| **قائمة بكلّ الإسنادات** | الروابط تُقرأ لكلّ اختبار على حدة؛ لا نقطة تُرجع «كلّ ما أُرسل». *Links are listed per exam only.* |
| **طباعة النموذج** | لا تصدير الورقة إلى PDF ولا إلى ورق. *A named form cannot be printed.* |
| **مقارنة الممتحَنين، وتصدير بيانات الجهة كاملةً** | غير موجودين. *Neither exists.* |
| **إعادة فتح درجة، وتوزيع الطابور بين المصحّحين، وقياس اتّساق التصحيح** | غير موجودة. *No re-opening a mark, no queue assignment, no marker agreement.* |

### ب · يُحفَظ ولا يقرؤه شيء | Controls that save and are read by nothing

هذه أخطر ممّا سبق، لأنّ الشاشة تَعِد بها. كلّ سطر هنا مُحقَّق بقراءة الشيفرة:

These matter more than the absences, because a screen promises them. Each line was
verified by reading the code:

| ما يُحفَظ | من يقرؤه |
|---|---|
| **أقسام الاختبار** — وقتها الخاصّ، وحدّها الأدنى، و«يجب اجتيازه» | `ExamStructureAppService` و`QuestionAppService` فقط. لا التسليم ولا التصحيح ولا النتائج تعرف بوجود الأقسام. فلا نتيجة قسماً بقسم، ولا رسوبَ على قسم مهما كان الباقي. |
| **مصطلحات الجهة** (`CategorySet`) | شاشة الكتالوج نفسها فقط. لا شاشة أخرى تعرض كلمات الجهة بدل كلماتنا. |
| **خمسة إعدادات للجهة** — التسجيل الذاتيّ، واللغة الافتراضيّة، والمنطقة الزمنيّة، ونسبة النجاح الافتراضيّة، وجمع مؤشّرات النزاهة | شاشة الإعدادات نفسها فقط، ولا شيء غيرها. |
| **لون الهويّة** | رسالة الدعوة وحدها. لا الغلاف ولا شاشة الامتحان يقرآنه. |
| **«تسجيل مؤشّرات النزاهة» على الاختبار** (`Exam.CollectIntegritySignals`) | لا أحد. `RecordSignalAsync` تُسجّل دائماً؛ إطفاء المفتاح لا يُطفئ شيئاً. |

**Sections** are configured and nothing in delivery, grading or reporting knows
they exist — so there is no section-by-section result and no qualifying section.
The **tenant's vocabulary** is saved and no screen renders it. **Five tenant
settings** are written and read only back by their own screen. The **brand colour**
reaches the invitation email and nothing else. The per-exam **"record integrity
signals"** switch is never consulted.

### ج · ناقص نصفه | Half-built

- **أنواع مؤشّرات النزاهة** — التعداد يعرّف ستّة (`Paste`، `WindowBlur`،
  `ImplausibleSpeed`، `NoCorrections`، `DevToolsOpened`، `PageReloaded`)، وشاشة
  الامتحان لا تُبلّغ إلّا عن اثنين. للأربعة الباقية اسمٌ وترجمةٌ وعبارةٌ في تقرير
  المصحّح، ولا تُنتَج أبداً.
  *Six signal types are defined; the browser reports two.*
- **سؤال المقياس المتدرّج** — يُسجَّل ولا يُقيَّم: مصحّحه يمنح صفراً ولا يُحيله إلى
  إنسان. فهو تقديرٌ يُقرأ، لا سؤالٌ يُحتسب. مقصود، ويجدر أن يُقال قبل وضعه في
  ورقةٍ لها مجموع.
  *A `scale` question is recorded and scores zero; it is never routed to a marker.*
- **قاعدة المخطّط الجائعة** — القاعدة التي لا تجد أسئلة كافية **تُسهم بما تجد ولا
  تفشل**، فتخرج الورقة أقصر بصمت. الشاشة تقول ذلك قبل الحفظ؛ التسليم لا يقوله.
  *A starving blueprint rule contributes what it can, so the paper comes out
  shorter with no signal at delivery time.*
- **البريد** — يُبنى ويُرسَل، وفي نشرٍ بلا مُرحِّل بريد لا يصل شيء، وتبقى الروابط
  قابلةً للنسخ باليد. والشعار غير مُدرَج في الرسالة عمداً: مرفقه خلف منحة موقّعة،
  وقارئ البريد لا يحملها، فيصل صورةً مكسورة.
  *Invitations need an SMTP relay; the logo is deliberately left out of the email.*
- **تحليل جودة الأسئلة لا يمكن منحه للمُعِدّ** — `ViewItemAnalysis` مُعشَّشة تحت
  `Results.View` وتُجمَعان بـ AND، فمنحُ المُعِدّ إيّاها يعني منحه كشف النتائج
  بأسماء الممتحَنين ودرجاتهم. فلا يأخذ أيّاً منهما. الشرح في `business/roles.md`.
- **الأدوار بلا أسماء عربيّة** — `IdentityRole` تملك اسماً واحداً هو المفتاح
  والعنوان معاً، فأسماء الأدوار تظهر إنجليزيّة في شاشة المستخدمين.
- **`/settings` بلا حارس على المسار** — أيّ مستخدم مسجَّل يبلغها بكتابة العنوان،
  ويقرأ الإعدادات؛ والكتابة وحدها محروسة بـ `Administration.ManageSettings`.
  والقائمة لا تُظهر الرابط إلّا لمن يملكها.

---

## ٤ · الأدوار الخمسة | The five roles

تُبذَر لكلّ جهة. التفصيل الكامل ومبرّراته في `business/roles.md`.

Seeded per tenant. The full reasoning is in `business/roles.md`.

| الدور | Role | الصلاحيّات | ما يراه في القائمة |
|---|---|---|---|
| مدير النظام | `Admin` | ٦٥ — كلّ شيء | كلّ الشاشات، ومنها الإعدادات وحسابات الموظّفين |
| منسّق | `Coordinator` | ٢٥ | الاختبارات (قراءةً)، المتقدّمون، المجموعات، الإسنادات، الجلسات الجارية، النتائج |
| مُعِدّ الاختبارات | `Author` | ١٤ | الاختبارات، بنك الأسئلة، التصنيفات |
| مصحّح | `Marker` | ٤ | التصحيح اليدويّ وحده |
| مشاهد النتائج | `Observer` | ٦ | الاختبارات (قراءةً)، النتائج، تحليل جودة الأسئلة |

ثلاثة قرارات تستحقّ الذكر:

- **المصحّح يرى مؤشّرات النزاهة، والمنسّق لا يراها.** المصحّح هو الإنسان الوحيد
  الذي يقرأ إجابةً حرّة ويحكم إن كانت من صاحبها؛ حجبُها عنه يُنتج تقديراً أسوأ لا
  حمايةً أكثر.
- **المنسّق يُرسل اختباراً لا يستطيع قراءة أسئلته.** مفتاح الإجابة داخل السؤال،
  ومن يُرسل أربعين رابطاً هو آخر من ينبغي أن يقرأه.
- **المُعِدّ لا يرى نتيجة أحد.** مُعِدٌّ يعرف من رسب في سؤاله لديه سبب لتعديل
  السؤال بعد فوات الأوان.

---

## ٥ · الشاشات | The screens

| المسار | الشاشة | الحارس على المسار |
|---|---|---|
| `/` | الرئيسية — أربع خطوات للبدء | تسجيل الدخول فقط |
| `/exams` · `/exams/new` · `/exams/:id` | الاختبارات ومحرّرها | `Assessment.Exams.View` |
| `/exams/:examId/questions` (+ `new`, `:questionId`) | أسئلة الاختبار ومحرّرها | `Assessment.Exams.View` |
| `/exams/:examId/blueprint` | مخطّط الورقة | `Assessment.Exams.View` |
| `/exams/:examId/structure` | الأقسام والقطع | `Assessment.Exams.View` |
| `/exams/:examId/forms` | النماذج المسمّاة | `Assessment.Exams.View` |
| `/questions` (+ `new`, `:questionId`) | بنك الأسئلة | `Assessment.Questions.View` |
| `/candidates` | المتقدّمون واستيراد القائمة | `Assessment.Candidates.View` |
| `/groups` | المجموعات والشُّعَب | `Assessment.Groups.View` |
| `/assignments` · `/assignments/:examId` | اختيار الاختبار، ثمّ الإرسال والروابط | `Assessment.Assignments.View` |
| `/results` | كشف النتائج والملخّص والتصدير | `Assessment.Results.View` |
| `/results/questions` | جودة الأسئلة | `Assessment.Results.View` |
| `/results/running` | الجلسات الجارية | `Assessment.Results.View` (والقائمة تُظهرها على `Assessment.Attempts.View`) |
| `/results/:attemptId` | ورقة إجابة واحدة | `Assessment.Results.View` |
| `/review` · `/review/:attemptId` | طابور التصحيح، وشاشة رصد الدرجات | `Assessment.Review.ViewQueue` |
| `/catalog` | التصنيفات والمصطلحات | `Assessment.Catalog.View` |
| `/users` | حسابات الموظّفين وأدوارهم | `Assessment.IdentityManagement.Users.View` |
| `/settings` | إعدادات الجهة وهويّتها | بلا حارس؛ الكتابة على `Assessment.Administration.ManageSettings` |
| `/exam/:token` · `/sitting` · `/result` | شاشات الممتحَن | **بلا مصادقة** — الرابط هو البطاقة |

---

## ٦ · التقنيات | Technology

| | |
|---|---|
| الخادم | ASP.NET Core على .NET 10 مع إطار **ABP** — DDD، تعدّد جهات، صلاحيّات، إعدادات، توطين |
| قاعدة البيانات | SQL Server عبر **EF Core**؛ الترحيل حاوية منفصلة قصيرة العمر لا خطوة في إقلاع الـ API |
| هويّة الموظّفين | **OpenIddict** عبر ABP |
| بطاقة الممتحَن | رمز JWT موقَّع بـ HMAC-SHA256 مستقلّ عن OpenIddict، مفتاحه `ExamSession:SigningKey` |
| الواجهة | **Angular** — مكوّنات مستقلّة، إشارات، تحميل كسول، RTL من الدرجة الأولى |
| الملفّات | `Volo.Abp.BlobStoring` على نظام الملفّات، حاوية لكلّ جهة، وقائمة صيغ مسموحة |
| الاختبار | **xUnit** للخادم، **Playwright** للمتصفّح (`desktop`، `mobile`، و`live` على خادم حقيقيّ) |
| التشغيل | Docker Compose، nginx للواجهة، GitHub Actions للتكامل المستمرّ |
| أدوات | `tools/` — بذر ثلاث جهات، اختبار حِمل، مسبار حفظ الحقول، فحص النصوص، فحص المسارات |

---

## ٧ · الوثائق | Documentation

| الملفّ | ما فيه |
|---|---|
| `README.md` | هذه الوثيقة — ما هو المنتج، وما ليس هو |
| `requirements.md` | المتطلّبات الوظيفيّة وغير الوظيفيّة والقيود وما هو خارج النطاق |
| `use-cases.md` | ستّ عشرة رحلة يمشيها إنسان، لكلٍّ حالتها وشاشتها ودورها |
| `user-stories.md` | الملاحم والقصص وشروط القبول وخطّة الاختبار ومصفوفة التتبّع |
| `DeveloperGuide.md` | التشغيل محلّياً، وبنية الحلّ، وأين يُضاف كلّ شيء |
| `deployment.md` | الحاويات ومتغيّرات البيئة والترحيل والتكامل المستمرّ |
| `CHANGELOG.md` | ما تغيّر في هذه الوثائق ومتى |
| `architecture/` | قرارات البنية: الوحدات، والنماذج والأقسام، والإجابات الموزونة |
| `business/` | مراجعات العمل، والأدوار، وتدقيق الصلاحيّات، وتحليل الفجوات |
| `design/` | موجز التصميم ومراجعات الواجهة |

**الملفّات المُصدَّرة** (`.docx`، `.pdf`، `.html`) يحتفظ بها مالك المشروع، وقد
تتأخّر عن نُسخ Markdown. Markdown هو المصدر.

The `.docx`, `.pdf` and `.html` exports are maintained by the project owner and may
lag behind the Markdown, which is the source.

---

## ٨ · من أين تبدأ | Where to start

```bash
cp .env.example .env       # ثمّ املأ الأسرار الثلاثة التي يسمّيها
docker compose up --build
```

التفصيل كلّه في `deployment.md`؛ والتطوير المحلّيّ بلا حاويات في
`DeveloperGuide.md`.
