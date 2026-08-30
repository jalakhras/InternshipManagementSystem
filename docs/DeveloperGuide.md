# دليل المطوّر | Developer Guide

**أسطرلاب** — منصّة التقييم والاختبارات. هذه الوثيقة للتشغيل محلّياً، ولمعرفة أين
يُضاف كلّ شيء. أمّا النشر والحاويات ومتغيّرات البيئة ففي `deployment.md`.

**Astrolabe**, an assessment platform. This guide is for running it on your own
machine and for knowing where things go. Containers, environment variables and
deployment live in `deployment.md`.

> **الاسم.** المستودع والمشاريع والنطاقات ما زالت تحمل `InternshipManagementSystem`
> من عمر المنتج الأوّل. لم تُهاجَر المُعرِّفات، وليس في الخطّة مهاجرتها الآن —
> إعادة تسمية تسع مشاريع وكلّ نطاق ومجلّد الترحيل تُنتج فرقاً هائلاً لا يغيّر
> سلوكاً. اقرأ الاسم القديم واعرف أنّه أسطرلاب.
>
> Every project, namespace and path still says `InternshipManagementSystem`. The
> identifiers have not been migrated and there is no plan to do it now: renaming
> nine projects, every namespace and the migrations folder produces an enormous
> diff that changes no behaviour.

---

## ١ · التشغيل المحلّيّ | Running it locally

### ما تحتاجه | Prerequisites

| | |
|---|---|
| .NET SDK | **10.0** (كلّ المشاريع على `net10.0`) |
| Node.js | لما يحتاجه إصدار Angular في `angular/package.json` |
| SQL Server | نسخة محلّيّة، أو حاوية `docker compose up -d db` |
| Docker | اختياريّ محلّياً، مطلوب لتشغيل الطقم كاملاً |
| ABP CLI | `dotnet tool install -g Volo.Abp.Studio.Cli` — تحتاجه خطوة صفر أدناه |

### الأسرار التي يجب ضبطها | The one required secret

```bash
cd src/InternshipManagementSystem.HttpApi.Host
dotnet user-secrets set "ExamSession:SigningKey" "<٣٢ محرفاً على الأقلّ>"
```

**الخادم يرفض الإقلاع بدون هذا المفتاح.** وهذا مقصود: كان هناك سابقاً احتياطيّ
صامت يوقّع كلّ رمز جلسة في كلّ بيئة بتجزئة النصّ الفارغ. مفتاحٌ افتراضيّ صامت أسوأ
من خادم لا يقوم.

**The host refuses to start without it**, deliberately: the previous silent
fallback signed every exam-session token in every environment with the SHA-256 of
the empty string.

سلسلة الاتّصال في `appsettings.json` تحت `ConnectionStrings:Default`. **أبقِ
`Max Pool Size=300`** — استنفد الافتراضيّ المجمّع عند ١٥٠ ممتحَناً متزامناً في
اختبار حِمل.

### الترتيب | The order

```bash
# ٠ · مكتبات صفحة الدخول — مرّةً بعد الاستنساخ
cd src/InternshipManagementSystem.HttpApi.Host && abp install-libs && cd ../..

# ١ · قاعدة البيانات: الترحيل، وبذر الصلاحيّات والأدوار، وتسجيل عملاء OpenIddict
dotnet run --project src/InternshipManagementSystem.DbMigrator

# ٢ · الخادم
dotnet run --project src/InternshipManagementSystem.HttpApi.Host    # https://localhost:44373

# ٣ · الواجهة
cd angular && npm ci && npm start                                   # http://localhost:4200
```

| | |
|---|---|
| الواجهة | http://localhost:4200 |
| الخادم | https://localhost:44373 |
| Swagger | https://localhost:44373/swagger |
| الفحص | https://localhost:44373/health |

`DbMigrator` آمنٌ للتشغيل مرّاراً. **أعِد تشغيله كلّما تغيّر عنوان الواجهة**،
لأنّ ذلك العنوان مخزون على عميل OAuth، وخطؤه يُفشل تسجيل الدخول في آخر خطوة
بـ «invalid redirect_uri» بعد أن يبدو كلّ شيء قد نجح.

### لماذا خطوة صفر | Why step zero

<div dir="rtl">

صفحات الدخول والتسجيل من ABP صفحات MVC، تُحمّل jQuery وBootstrap وسكربتات
السمة من `wwwroot/libs`. وذلك المجلّد **يُنصَّب ولا يُودَع** — فهو في
`.gitignore` — فالاستنساخ النظيف لا يحمل منه شيئاً، وكلّ وسم سكربت فيه يردّ ٤٠٤.

وما يجعل هذا يستحقّ فقرةً أنّ الصفحة **تظهر**: النموذج موجود والحقول موجودة،
ولا يقول سجلّ الخادم شيئاً. هي فقط بلا أنماط وبلا سلوك، وفي console المتصفّح
‏«jQuery is not defined» عشر مرّات. وهي أوّل شاشة يراها أيّ أحد من هذا المنتج.

والحاوية تُنصّبها في مرحلة البناء (`docker/api/Dockerfile`)، فلا يلزم شيء عند
النشر.

</div>

### بيانات يمكن النظر إليها | Data worth looking at

كلّ شاشة هنا تبدو سليمةً وهي فارغة، ولا تقول الحقيقة إلّا وفيها بيانات.

```bash
node tools/seed-tenants.js      # ثلاث جهات: أكاديميّة تداول، ومركز لغات، وشركة توظيف
node tools/seed-role-users.js   # حساب لكلّ دور في كلّ جهة
```

الأولى تبذر لكلّ جهة كتالوجها بالعربيّة، وبنك أسئلتها، واختبارات بنموذجين
مسمّيين، وشُعَباً على مستويات، وممتحَنين، وجلسات أُدّيت وصُحّحت. والثانية تُنتج
الحسابات التي تجعل الصلاحيّة **رفضاً** لا مجرّد منحة حاضرة دائماً. كلاهما قابل
لإعادة التشغيل.

Every screen here looks fine with no data in it and only tells the truth with data
in it. `seed-tenants.js` builds three organisations end to end;
`seed-role-users.js` gives each of them one account per role, which is what makes
a permission an actual refusal rather than a checkbox. Both are re-runnable.

---

## ٢ · بنية الحلّ | Solution layout

### الطبقات | The layers

```
src/
  ...Domain.Shared/        التعدادات، ورموز الأخطاء، وثوابت الصلاحيّات، وملفّات النصوص
  ...Domain/               الكيانات وقواعد الأعمال والمصحّحات
  ...Application.Contracts/ الـ DTOs وواجهات الخدمات
  ...Application/          الخدمات التطبيقيّة — هنا يعيش المنطق التطبيقيّ
  ...EntityFrameworkCore/  السياق، والتهيئة، والترحيلات
  ...HttpApi/              المتحكّمات
  ...HttpApi.Host/         الإقلاع والإعداد والوسائط البرمجيّة
  ...HttpApi.Client/       عميل مُولَّد
  ...DbMigrator/           تطبيق وحيد الغرض: يُرحّل ويبذر ويُسجّل عملاء OAuth
test/
  ...Domain.Tests/  ...Application.Tests/  ...EntityFrameworkCore.Tests/  ...TestBase/
angular/                   الواجهة
  src/app/core/            الخدمات المشتركة، والصلاحيّات، والتنقّل
  src/app/layout/          الغلاف
  src/app/features/<name>/ ميزة لكلّ مجلَّد، مع ملفّ مساراتها
  e2e/                     Playwright: desktop · mobile · live
docker/  tools/  docs/
```

### السياقات السبعة | The seven contexts

**بنية أحاديّة مُوحَّدة مقسّمة بالسياق المحدود، يحرسها اختبار — لا عدد المشاريع.**
مجلَّد لكلّ سياق داخل كلّ طبقة، واختبار بنية يُفشل البناء حين يمدّ سياقٌ يده في
داخليّات آخر. الاتّجاه في السهم واحد لا يعود.

| السياق | يملك |
|---|---|
| **Catalog** | المجالات، والمستويات، والموضوعات، ومصطلحات الجهة |
| **Exams** (التأليف) | الاختبارات، والأسئلة، ومجموعاتها، وقواعد المخطّط، والأقسام، والنماذج |
| **People** | الأشخاص والشُّعَب |
| **Delivery** | الإسنادات، والروابط، والمحاولات، والإجابات، ومؤشّرات النزاهة |
| **Grading** | مصحّح لكلّ نوع، والتقدير، والمراجعة اليدويّة |
| **Results** | الكشف، والتفصيل حسب الموضوع، وتحليل جودة الأسئلة |
| **Tenancy** | ما تُظهره الجهة لناسها: الاسم، والشعار، واللون |

التفصيل ومبرّراته في `architecture/modules.md`.

---

## ٣ · أين يُضاف كلّ شيء | Where things go

### نوع سؤال جديد

1. `Domain.Shared/…/QuestionTypes.cs` — الثابت.
2. `QuestionAppService.Descriptors[]` — الواصف: هل يُصحَّح آليّاً، وهل يقبل رفعاً،
   وهل له خيارات، وترتيبه في المُنتقي.
3. `Domain/Assessment/Grading/` — مصحّح يُنفّذ `IQuestionGrader`. يلتقطه
   `GraderResolver` من الحاوية بلا تسجيل يدويّ. لا مصحّح ⇒ **تصحيح يدويّ، لا صفر**.
4. `QuestionPayloadValidator` — ما الذي يجعل هذا النوع غير قابل للتصحيح. يُرفَض
   عند التأليف لا عند الامتحان.
5. `angular/…/features/questions/payload/` — محرّر التأليف، مُسجَّل في السجلّ.
6. `angular/…/features/take/answers/` — عنصر الإجابة.
7. `ar.json` و`en.json` — `QuestionType:<type>` وكلّ نصّ جديد.

> **القاعدة التي يكسرها هذا النوع من العمل**: نصفا الميزة — عنصر الإجابة والمصحّح
> — يُختبَران كلٌّ وحده فيَصحّان، والعطب في **الوصلة** بينهما. حدث ذلك ستّ مرّات
> في هذه الشيفرة، وكلّفت مرّةً إجابةً صحيحة تُحتسب صفراً. **الاختبار الذي يستحقّ
> الكتابة هو الذي يُمرّر خرج العنصر إلى المصحّح**، لا الذي يختبر أيّاً منهما.

### شاشة جديدة

1. `angular/src/app/features/<name>/<name>.routes.ts` + المكوّن، بتحميل كسول.
2. تسجيل المسار في `app.routes.ts` تحت الغلاف، مع `data: { requiredPolicy: '…' }`.
3. عنصر في `core/navigation.ts` مع صلاحيّته — والغلاف يُسقط ما لا يُمنح، ويُسقط
   القسم إن فرغ.
4. المفتاح في `core/permissions.ts` مطابقاً لثابت الخادم.
5. النصوص في `ar.json` **و**`en.json` معاً.
6. `python tools/check-localization.py` — المفتاح الناقص لا يُخطئ، إنّما يظهر في
   الشاشة مكان جملة.

### صلاحيّة جديدة

1. الثابت في `Domain.Shared/Permissions/InternshipManagementSystemPermissions.cs`.
2. العقدة في `InternshipManagementSystemPermissionDefinitionProvider` — **وانتبه
   للتعشيش**: الصلاحيّة المُعشَّشة تحت أخرى تُجمَع معها بـ AND، فمن يملك الورقة
   دون جذرها يُرفَض في كلّ طلب رغم أنّ شاشة الصلاحيّات تقرأ صحيحة.
3. `[Authorize]` على الدالّة، أو فحصٌ صريح حين يعتمد القرار على ما يطلبه الطلب.
4. الدور في `SeedAssessmentRolesAsync`، والمنطق في `business/roles.md`.
5. الترجمة `Permission:<Name>` باللغتين.

**فحصٌ ثابت يمنع الثلاثة الأخطاء المعروفة**: خدمة بلا `[Authorize]` على الصنف،
وسياسة مذكورة في سمة وغير مُعرَّفة، وصلاحيّة مُعرَّفة لا يفرضها شيء. الأخير كشف
`Administration.Access` في أوّل تشغيل له: كانت تُمنح وتحرس لا شيء، فحُذفت ولم
تُفرَض.

### كيان أو حقل جديد

كيان في `Domain/Assessment/<Context>/`، وتهيئة في `…DbContextModelCreatingExtensions`،
ثمّ:

```bash
dotnet ef migrations add <Name> --project src/InternshipManagementSystem.EntityFrameworkCore
dotnet run --project src/InternshipManagementSystem.DbMigrator
```

---

## ٤ · الاختبار | Testing

```bash
dotnet test                                  # الخادم كلّه
cd angular && npm run e2e                    # المتصفّح: desktop + mobile
cd angular && npx playwright test --project=live   # خادم حقيقيّ + قاعدة مبذورة
```

| الطبقة | ما تحرسه |
|---|---|
| **وحدة** | مصحّح، ومُحقِّق، وقاعدة نطاق، وقارئ الاستيراد، وبنّاء رسالة الدعوة |
| **تكامل** | خدمة تطبيقيّة مقابل قاعدة حقيقيّة، مع عزل الجهات — ثلاث جهات، كلٌّ ترى ما لها |
| **متصفّح** | Playwright على `desktop` و`mobile`، بالعربيّة، وهما يُوهِمان طبقة HTTP |
| **حيّ** | `angular/e2e/live` يقود API حقيقيّاً على قاعدة مبذورة، ويكتب صفوفاً |
| **ثابت** | فحوص على التجميعة: كلّ خدمة محروسة، وكلّ سياسة مُعرَّفة، وكلّ صلاحيّة مفروضة |

**ما لا تحرسه الاختبارات، وهو مكتوب هنا لأنّه لا يجدر أن يُكتشَف مرّتين:**

- **`AddAlwaysAllowAuthorization` في مضيف الاختبار** يعني أنّ **لا `[Authorize]`
  في هذا الحلّ يُنفَّذ في اختبار تكامليّ**. الفحوص الثابتة أعلاه تسدّ ما يمكن
  سدّه بلا طلب حقيقيّ، ولا تسدّ الباقي. ومشروع `live` هو المكان الذي يُختبَر فيه
  الرفض فعلاً.
- **مشروعا `desktop` و`mobile` يُوهِمان HTTP.** يُثبتان سلوك الشاشة ولا يُثبتان
  أنّ مساراً يُجيب. ولذلك وُجد `tools/smoke-routes.js`: أربع مرّات في هذا المشروع
  شُحنت خدمة تطبيقيّة كاملة ومختبَرة بلا متحكّم ولا شاشة، وكانت تُقرأ «منجزة» في
  كلّ جرد يعدّ الخدمات.
- **`live` خارج التكامل المستمرّ عمداً**، لأنّه يحتاج خادماً وقاعدةً لا يملكهما
  المُشغِّل.

`AddAlwaysAllowAuthorization` in the test host means **no `[Authorize]` in this
solution is executed by any integration test**. The static assembly checks close
what can be closed without a request; the `live` project is where a refusal is
actually asserted.

### أدوات تكشف ما لا يكشفه اختبار | Tools that find what tests do not

| الأداة | السؤال الذي تجيب عنه |
|---|---|
| `tools/smoke-routes.js` | هل كلّ مسار يطلبه العميل يُجيب فعلاً على خادم يعمل؟ |
| `tools/probe-round-trip.js` | هل يحتفظ التعديل فعلاً بما أُرسل إليه؟ (كُتب بعد أن ردّ تغيير كلمة المرور ٢٠٠ ولم يُغيّر شيئاً) |
| `tools/check-localization.py` | هل كلّ نصّ يطلبه العميل مُعرَّف عند الخادم؟ |
| `tools/load-test.js` | ما الذي يعيشه ممتحَنٌ واحد بينما يجلس تسعة وأربعون معه؟ |
| `tools/seed-tenants.js` | ثلاث جهات ببيانات حقيقيّة — الادّعاء الأكثر تكراراً في هذا المنتج، وأقلّه اختباراً |
| `tools/dedupe-identity.sql` · `purge-test-data.sql` | تنظيف ما خلّفته عيوب البذر |

---

## ٥ · قواعد تعرفها من الشيفرة لا من الوثائق | Rules the code enforces

- **لا مُدخَل يحتاج مهارة برمجيّة.** لا JSON، ولا تعبير نمطيّ، ولا إحداثيّات،
  ولا صياغة تُتعلَّم — لا للمؤلّف ولا للممتحَن. الاستثناء الوحيد سؤال الكود: لغته
  هي المادّة المُختبَرة لا الأداة.
- **الخدمة ليست ميزة.** الميزة مُنجَزة حين يمشيها إنسان من المتصفّح: شاشة، على
  مسار مُسجَّل، تنادي دالّةً تبلغ متحكّماً يبلغ خدمةً تعمل.
- **الأداة التي تُحفَظ ولا يقرؤها شيء أسوأ من غيابها.** الغياب يُخيّب، والأداة
  الميّتة تكذب. ما هو كذلك اليوم مُعدَّد في `README.md` §٣-ب.
- **ما يعجز المصحّح عن قراءته يذهب إلى إنسان، لا إلى صفر.**
- **إحصاءات لا تُفشل امتحاناً.** تُحسب خارج معاملة التسليم، وتُبتلَع أخطاؤها.
- **المؤقّت على الخادم**، والورقة سؤالاً سؤالاً، والمفتاح لا يُرسَل مع السؤال.
- **العربيّة أوّلاً.** كلّ نصّ باللغتين، والأرقام معزولة كي لا تنقلب.

---

## ٦ · مواضع التصادم المعروفة | Known traps

- **Bootstrap محمَّل عالميّاً.** اسم صنف عاديّ مثل `row` أو `progress` يلتقي بمكوّن
  في المكتبة بلا تحذير، فيصير شريط تقدّم ما ليس شريط تقدّم. سمِّ أصنافك ببادئة
  المكوّن (`sitting__progress`، `catalog__row`).
- **`.astro-numeric` تُوضَع على `<span>` داخل الخليّة لا على الخليّة.** على الخليّة
  تدفع المحاذاة إلى الحافّة المقابلة في اتّجاه من اليمين إلى اليسار، فيُقرأ الرقم
  تحت عنوان جاره. ولا تُوضَع على جملة أبداً: خطّها أُحاديّ العرض ولا عربيّة فيه.
- **السهام تُعكس مرّةً واحدة.** القاعدة العامّة تعكسها في الاتّجاه العربيّ، فالسهم
  المكتوب معكوساً أصلاً يُعكس مرّتين ويشير إلى الأمام في زرّ «رجوع».
- **لا تُدخِل شيئاً بين سمة `[Authorize]` والدالّة التي تحرسها.** حدث ذلك مرّتين
  في ليلة واحدة: مرّةً فقدت دالّةٌ حارسها، ومرّةً حملت اثنين فجُمعا بـ AND.
- **تعطيل مُرشِّح الجهات يجعل الطلب *يرى* صفّاً، ولا يجعله *ينتمي* إلى تلك الجهة.**
  هذا سوءُ فهمٍ كلّف عيبين حقيقيّين: صورة كلّ ورقة تردّ ٤٠٤ لغير المضيف، ومركز
  لغات يُعرَض لممتحَنيه اسم المنصّة بدل اسمه.

---

## ٧ · إلى أين بعد | Where to next

| | |
|---|---|
| ما هو المنتج وما ليس هو | `README.md` |
| المتطلّبات والقيود | `requirements.md` |
| الرحلات وحالتها | `use-cases.md` |
| القصص وشروط القبول ومصفوفة التتبّع | `user-stories.md` |
| النشر والحاويات ومتغيّرات البيئة | `deployment.md` |
| قرارات البنية | `architecture/` |
| الأدوار ومبرّراتها، وتدقيق الصلاحيّات | `business/roles.md` · `business/permissions-audit.md` |

## إذا «نجح» اختبارٌ بعد أن أزلت إصلاحه — When removing a fix does not fail the test

القاعدة في هذا المشروع أن كلّ إصلاح يُثبَت بإزالته والتأكّد من سقوط
اختباره. وفي ٣٠ أغسطس ٢٠٢٦ كذبت هذه الطريقة مرّتين، والسبب واحد:

**خادم التطوير يُقدّم نسخةً مخزّنة قديمة من المكوّنات المُحمَّلة كسولاً.**
`ng serve` يستعمل Vite، وهو يُقدّم الوحدات عند الطلب ويُخزّن ناتج
تحويلها. فتعديلٌ في مكوّنٍ لا يُحمَّل إلّا عند الحاجة قد لا يصل إلى
المتصفّح أصلاً، فيبقى الاختبار يفحص الكود القديم — ناجحاً أو ساقطاً
لأسبابٍ لا علاقة لها بما غيّرت.

وأسوأ ما فيه أنّه يُفسد الاتّجاهين: يُخفي إصلاحاً صحيحاً فتظنّه خاطئاً،
ويُبقي إصلاحاً محذوفاً فتظنّ اختبارك يحرسه وهو لا يحرس شيئاً.

**قبل أن تعدّ سقوط اختبارٍ برهاناً، تأكّد أنّ ما أزلته قد وصل فعلاً:**

- لتعديلٍ في `src/main.ts` أو أيّ شيء في الحزمة الأولى:
  `curl -s http://localhost:4200/main.js | grep -c "<اسم الدالّة>"`
- لمكوّنٍ يُحمَّل كسولاً — وأكثر شاشات هذا المشروع كذلك — لا يفيد ذلك،
  لأنّ الوحدة لا تدخل `main.js`. أوقف الخادم وامسح المخزنين ثمّ أعده:

```
Stop-Process -Name node            # خادم ng serve وحده
rm -rf angular/.angular/cache angular/node_modules/.vite
cd angular && npx ng serve
```

وهذا ما جعل إصلاح صورة سؤال التحديد يبدو فاشلاً وهو صحيح: `absolute()`
لم تكن تُستدعى لأنّ المكوّن المُقدَّم كان النسخة القديمة.

وللخادم الخلفيّ نظيرٌ أبسط وأقلّ خداعاً: لا يلتقط تغييراً إلّا ببناءٍ
وإعادة تشغيل، وهو يقفل ملفّاته أثناء عمله — فأوقفه قبل `dotnet build`.
