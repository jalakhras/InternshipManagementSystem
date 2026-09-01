# البنك والأقسام: أين يعيش «أيّ جزء من الورقة»؟
# The bank and the sections: where does "which part of the paper" live?

**السؤال الذي طلب مالك المنتج تسويته:** أيحقّ لسؤالٍ في البنك المشترك أن يملأ قسماً
من اختبار؟ **الجواب المستخلص من ثمانية منتجات ومعيارٍ واحد: نعم — ولكن ليس بأن
يُصنَّف السؤال في القسم.**

The product owner asked whether a shared-bank question should be able to fill a
section of an exam. The answer from eight products and one interchange standard
is **yes — but never by filing the question into the section.**

تحرٍّ فقط. لم تُعدَّل شيفرة، ولم يُلمَس ملفٌّ غير هذا.
Read-only. No source was modified; this is the only file written.

**عند** `0bc6e06`.

---

## قاعدة القراءة | How to read the sources

كلّ ادّعاء عن منافس يحمل مصدره ووسمه:

- **[موثَّق]** — قُرئت صفحة البائع نفسها مباشرةً، والاقتباس منها حرفيّ.
- **[مقتطف]** — رفض الجلب المباشر (403)، والادّعاء من مقتطف محرّك بحثٍ **لصفحةٍ
  رسميّةٍ مسمّاة**.
- **[شيفرة]** — قُرئ المخطّط أو الشيفرة المصدريّة نفسها، لا وثيقة عنها.
- **[غير موثَّق]** — قيلت صراحةً. ادّعاءٌ واثقٌ خاطئ عن منافس أسوأ من سكوتٍ عنه.

Every claim carries a label: **[verified]** the vendor's own page was fetched and
quoted; **[extract]** direct fetch was refused (403) and the claim comes from a
search extract *of the named official page*; **[source]** the schema or source
code itself was read; **[not verified]** stated plainly rather than guessed.

**عائقان يجب تسجيلهما.** `support.examsoft.com` و`questionmark.com` و
`help.questionmark.com` و`support.testgorilla.com` و`assess.com` ترفض القراءة
الآليّة بـ403، فكلّ ما يخصّها أدناه **[مقتطف]** لا **[موثَّق]**. ووثائق المنتجات
العربيّة رقيقةٌ علناً إلى حدٍّ يمنع التحقّق من الآليّة في أكثرها.

ExamSoft, Questionmark, TestGorilla and assess.com all refuse automated fetches
with 403; everything from them below is **[extract]**, not **[verified]**. MENA
products publish too little to verify mechanism.

---

## ٠ · الخلاصة في فقرة | The finding in one paragraph

**لا منتجٌ واحدٌ ممّا فُحص يضع «أيّ قسم» على السؤال.** الحقيقة تعيش على البنية —
على القسم، أو على مرجعٍ يملكه القسم، أو على قاعدة اختيارٍ يملكها القسم — وتختار
تلك القاعدةُ على **تصنيفٍ أو موضوعٍ أو وسمٍ أو صعوبة**، أي على صفاتٍ يحملها السؤال
أصلاً ولا تخصّ ورقةً بعينها. وهذا المنتج يملك تلك القاعدة **مبنيّةً بالفعل**:
`ExamBlueprintRule.ExamSectionId` موجودٌ في المجال، ومخزَّنٌ في قاعدة البيانات،
ويقرؤه `DrawByRules` — ولا شيء يكتبه، و`DrawBySection` يضيّق البِركة على
`q.ExamSectionId == section.Id` **قبل** أن يُطبّق القاعدة، فيحرمها من البنك.
فالفجوة ليست نموذجاً ناقصاً؛ هي ثلاثة أسطرٍ في البنّاء وحقلان في نموذجَي نقل.

**Not one of the products examined puts "which section" on the item.** The fact
lives on the structure — on the section, on a reference the section owns, or on a
selection rule the section owns — and that rule selects on a **category, topic,
tag or difficulty**: properties the item already carries and which belong to no
particular paper. This product *already has that rule*:
`ExamBlueprintRule.ExamSectionId` exists in the domain, exists in the database,
and is honoured by `DrawByRules`. Nothing can write it, and `DrawBySection`
narrows the pool to `q.ExamSectionId == section.Id` **before** the rule runs,
which starves it of the bank. The gap is not a missing model. It is three lines
in the builder and two fields on two DTOs.

---

## ١ · ما تفعله هذه الشيفرة اليوم | What this codebase does today

كلّ ما في هذا القسم **[شيفرة]** — قُرئ من المصدر عند `0bc6e06`.

### ١.١ الحقائق الأربع التي تحكم كلّ ما بعدها

**١. التصنيف عمودٌ واحدٌ على السؤال.**
`Question.ExamSectionId` — `Domain/Assessment/Exams/Question.cs:62`.

**٢. مسار الكتابة يرفض كلّ سؤال بنك، بالتصميم.**
`QuestionAppService.RequireSectionBelongsToAsync` (`:704`) يرمي
`IMS:Question:SectionNotInExam` حين يكون القسم مفقوداً **أو `examId` معدوماً**
أو `section.ExamId != examId`. وسؤال البنك `ExamId == null` دائماً، فكلّ قسمٍ في
الدنيا «يخصّ اختباراً آخر». الحارس صادق، ولا يجيب عن السؤال.

**٣. وهذه هي العقدة الحقيقيّة:** `ExamFormBuilder.DrawBySection` يضيّق أوّلاً ثمّ
يُطبّق القاعدة:

```csharp
// ExamFormBuilder.cs:170
var pool = bank.Where(q => q.IsActive && q.ExamSectionId == section.Id).ToList();
// ExamFormBuilder.cs:177
var rules = exam.Blueprint.Where(r => r.ExamSectionId == section.Id).ToList();
// ExamFormBuilder.cs:181
var picked = rules.Count > 0 ? DrawByRules(rules, pool, taken, random) : ...
```

فالقاعدة الموجَّهة إلى قسمٍ **لا تختار من البنك، بل من الأسئلة المصنَّفة في ذلك
القسم سلفاً**. حتّى لو كُتب `ExamSectionId` على القاعدة اليوم، ما بلغت سؤال بنكٍ
واحداً. This is the actual blocker, and it is one `Where` clause.

**٤. ولا شيء يكتب `ExamBlueprintRule.ExamSectionId`.** الحقل في المجال
(`ExamBlueprintRule.cs:28`)، والعمود في قاعدة البيانات
(`InternshipManagementSystemDbContextModelSnapshot.cs:934`) — لكن
`BlueprintRuleDto` و`CreateUpdateBlueprintRuleDto` (`ExamDtos.cs:92,110`) لا
يحملانه، و`GetBlueprintAsync` لا يُسقطه، و`SetBlueprintAsync:379` يبني القواعد
بدونه. عمودٌ حيٌّ في المخطّط، ميّتٌ في المنتج.

### ١.٢ ما يعمل ولا ينبغي كسره

- `Question.DrawableBy(examId, categoryId, levelId)` — البِركة الحقيقيّة: أسئلة
  الاختبار **زائداً** أسئلة البنك المطابقة للتصنيف والمستوى. تُستدعى في تسع مواضع
  (`ExamTakingAppService:308,765`؛ `ExamAppService:193,341,436`؛
  `ExamStructureAppService:78,269,298`). فالبنك **يصل بالفعل** إلى الورقة — كلّ ما
  يعجز عنه هو أن يُنسب إلى قسم.
- `AttemptQuestion.ExamSectionId` يُجمَّد عند البناء من `row.Question.ExamSectionId`
  (`ExamFormBuilder.cs:122`)، وهو **المستهلك الوحيد في اتّجاه التسليم**: يقرؤه
  `ExamTakingAppService:766,824,839,1016‑1033` و`ResultAppService:471‑485`.
  وهذه بشرى: أيّ آليّةٍ تختار القسم تستطيع أن تختم هذا الصفّ، والعمود على السؤال
  ليس ضروريّاً لها.
- `ExamStructureAppService.DeleteSectionAsync:135` يُفرّغ التصنيف بدل حذف الأسئلة.
- ٢٢ اختباراً خضراء (`ExamFormBuilderSectionTests` ١٢، `QuestionSectionFilingTests` ٦،
  `SectionDeliveryTests` ١٠ تقريباً).

### ١.٣ ما يراه المؤلّف اليوم — وهو لا شيء

في `question-form.component.ts:171‑188` تُحمَّل الأقسام **فقط إذا كان `examId`
موجوداً**، وفي `question-form.component.html:130` تُعرض القائمة **فقط إذا كانت
`sections().length > 0`**. فمؤلّف سؤالٍ في البنك لا يرى حقلاً ولا يرى تفسيراً:
لا رفض، ولا رسالة، ولا شيء. والرفض في `RequireSectionBelongsToAsync` لا يبلغه
أحدٌ عبر الشاشة أصلاً — هو حارس واجهةٍ برمجيّةٍ فقط.

The bank author sees no field and no explanation. Silence, not a refusal.

### ١.٤ عيبٌ قائمٌ يستحقّ التسجيل

`ExamAppService.GetBlueprintAsync:360` يحسب `AvailableCount` على **كامل البنك
القابل للسحب**، متجاهلاً الأقسام. وفي اختبارٍ مقسَّمٍ اليوم يُطبَّق كلّ قاعدةٍ على
بِركة قسمها فقط — فالرقم المعروض للمؤلّف **يبالغ أصلاً**، قبل أيّ تغيير.

---

## ٢ · ما يفعله السوق | What the market does

### ٢.١ Moodle — القسم مدىً من الخانات، والاختيار استعلامٌ مخزَّن

**[شيفرة]** قُرئ المخطّط من `MOODLE_500_STABLE`.

جدول `quiz_sections` — تعليق الجدول نفسه: *"Stores sections of a quiz with section
name (heading), from slot-number N and whether the question order should be
shuffled."* وحقل `firstslot`: *"Number of the first slot in the section. The
section runs from here to the start of the next section, or the end of the quiz."*
([install.xml](https://github.com/moodle/moodle/blob/MOODLE_500_STABLE/mod/quiz/db/install.xml))

**لا `sectionid` على `quiz_slots`، ولا حقل قسمٍ على أيّ جدول أسئلة.** عضويّة القسم
تُشتقّ بمقارنة رقم الخانة بـ`firstslot`.

**والسؤال لا يملكه اختبار.** السلسلة:
`question_categories(contextid)` → `question_bank_entries` → `question_versions`
→ `question`. والاختبار يرتبط **بالمرجع** لا بالملكيّة، عبر جدولين:

- `question_references` — *"Records where a specific question is used."* —
  `usingcontextid`, `component`, `questionarea`, `itemid`, `questionbankentryid`,
  `version` (*"NULL means use the latest non-draft version"*).
- `question_set_references` — *"Records where groups of questions are used."* —
  وفيه الحقل الحاسم `filtercondition`: **"Filter expression in json format"**
  مع `questionscontextid` (*"Context questions come from"*).

**أي أنّ خانة السؤال العشوائيّ تخزّن استعلاماً، لا سؤالاً.** وفي
[`mod/quiz/classes/structure.php`](https://github.com/moodle/moodle/blob/MOODLE_500_STABLE/mod/quiz/classes/structure.php)،
دالّة `add_random_questions`:

```php
if (!isset($filtercondition['filter']['category'])) {
    throw new \invalid_parameter_exception('$filtercondition must contain at least a category filter.');
}
```

التصنيف **إلزاميّ** ويبقى المرساة؛ وكلّ ما عداه مرشّحٌ إضافيّ. والوسوم مرشّحٌ
معتمَد: `qbank_tagquestion\tag_condition` يبني شرط `WHERE` عبر
`build_query_from_filter()`، والتوقيع القديم `get_next_question_id(...$tagids)`
**مهجورٌ صراحةً** لصالح `get_next_filtered_question_id(array $filters)`.
([qbank filters](https://moodledev.io/docs/5.0/apis/plugintypes/qbank/filters))

**والدليل الأقوى تاريخيّ.** حقول `quiz_slots` عبر الإصدارات **[شيفرة]**:

| الفرع | حقول `quiz_slots` |
|---|---|
| `MOODLE_39_STABLE` | id, slot, quizid, page, requireprevious, **questionid, questioncategoryid, includingsubcategories**, maxmark |
| `MOODLE_401_STABLE` | id, slot, quizid, page, requireprevious, maxmark |
| `MOODLE_500_STABLE` | id, slot, quizid, page, displaynumber, requireprevious, maxmark, quizgradeitemid |

**حذفت Moodle في ٤٫٠ المؤشّر المباشر إلى السؤال وعمودَي التصنيف من الخانة.**
هذا بالضبط الاتّجاه المعاكس لما يفعله `Question.ExamSectionId` هنا.

بنوك مستقلّة منذ ٤٫٥/٥٫٠: `mod_qbank` وحدةُ نشاطٍ في مقرّر، ولكلّ بنكٍ سياقُ وحدة.
والمصطلحات للمستخدم: *"A question bank allows a teacher to create, preview and
edit questions in a dedicated space"*؛ *"Course shared question bank … where
questions can be reused and shared across courses"*؛ *"A quiz question bank …
where questions can only be used in the quiz"*.
([Question bank](https://docs.moodle.org/500/en/Question_bank))

### ٢.٢ QTI 3.0 — القسم يعدّد مراجعه، ولا بنك في النموذج

**[موثَّق]** قُرئ نصّ المواصفة كاملاً.

السلسلة: `qti-assessment-test` → `qti-test-part` → `qti-assessment-section`
(متداخل، أو `qti-assessment-section-ref`) → `qti-assessment-item-ref`.

**§4.2** حرفيّاً: *"An assessment section groups together individual item
references and/or sub-sections … **A section can only reference an item using a
qti-assessment-item-ref object** but it may contain or reference other sections."*

**§5.129 Selection** حرفيّاً: *"The selection class specifies the rules used to
select **the child elements of a section** for each test session."* وخصائصه
**كلّها**: `select` (عدد)، `with-replacement` (منطقيّ)، `extension`، وابنٌ
`extensions`. **لا `class`، ولا `href`، ولا `bank`، ولا `category`، ولا استعلام.**
و`qti-ordering shuffle` يأتي بعد الاختيار.

**والنتيجة الحاسمة لهذا المنتج:** المسرد في §3 من نموذج المعلومات يقول حرفيّاً:
*"**Object Bank** — An object bank is a collection of objects used in assessment …
**Object banks are not represented directly in this information model.**"* وفي
[ربط XSD §8](https://www.imsglobal.org/spec/qti/v3p0/bind): *"objectbanks are
bound to content packages for interchange"* — أي وعاء شحن، لا هدف استعلام. ولا
وجود لـ`sourceBank` ولا `bankRef` ولا `qti-item-bank` في نصّ المواصفة كلّه.
و[النظرة العامّة §2.1](https://www.imsglobal.org/spec/qti/v3p0/oview) تجعل البنك
**نظاماً/دوراً**: *"Item Bank — A system for collecting and managing collections of
assessment items"*؛ *"Test Constructor … The items are typically drawn from an
item bank."* أي أنّ السحب من البنك يقع **قبل** وجود وثيقة QTI.

مخرجان معتمدان فقط:

- **`qti-selection` كنقطة توسعة** — ودليل التطبيق يحذّر حرفيّاً: *"usage of the
  qti-selection extension point should be avoided, as any extension point reduces
  content interoperability. Implementers should only use this as a last resort."*
  وأيّ محرّكٍ لا يفهم التوسعة *"should ignore the qti-selection entirely"* — أي
  **يسلّم الورقة كاملةً بلا سحب**، وهو أسوأ فشلٍ ممكن في اختبارٍ حقيقيّ.
- **`qti-adaptive-selection`** (جديد في 3.0) — يفوّض كلّ الاختيار إلى محرّك CAT
  خارجيّ عبر REST، بحمولةٍ *"proprietary to the adaptive engine."*

**وتحذيرٌ يسهل الوقوع فيه:** خاصّة `category` على `qti-assessment-item-ref`
**ليست** للاختيار. §5.6.6 حرفيّاً: *"Categories are used to allow custom sets of
item outcomes to be aggregated during outcomes processing."* أي للتجميع في
النتائج، لا للسحب.

**ما يعنيه هذا لتصديرٍ مستقبليّ (وهو اتّجاهٌ معلَن في `business-review.md:485`):**
قاعدةُ اختيارٍ لا تنجو من التصدير كما هي. إمّا تُصفَّى إلى قائمةٍ محدّدة عند
التصدير (فتضيع العشوائيّة)، وإمّا تُصدَّر البِركة كاملةً مع
`qti-selection select="N"` (فيُسرَّب البنك كلّه داخل الحزمة). **وهذا قيدٌ على حجم
البِركة التي نسمح بها لقاعدة، ويجب أن يُقرَّر مبكّراً.** ولاحظ أنّ العمود الحالي
على السؤال **لا ينجو أصلاً**: QTI لا تعرف حقلاً على السؤال يقول أيّ قسمٍ يخصّه.

### ٢.٣ Canvas New Quizzes — كتلةٌ مربوطةٌ ببنك، بلا مؤشّرٍ عكسيّ

**[موثَّق]** من وثائق Instructure.

كائن `QuizItem` يحمل `position` و`entry_type` — *"One of `Item`, `Stimulus`,
`BankEntry`, or `Bank`."* وكائن `BankItem` يحمل `id`, `title`, `archived`,
`entry_count`, `item_entry_count` فقط: **لا مؤشّر من البنك أو من عنصره إلى أيّ
اختبار.** ([New Quiz Items API](https://canvas.instructure.com/doc/api/new_quiz_items.html))
والسلوك يؤكّده: *"Edits will display in any quiz that uses the item."*
([تحرير عنصر بنك](https://community.instructure.com/en/kb/articles/661081-how-do-i-edit-an-item-bank-item-in-new-quizzes))

**تصحيحٌ لما ورد في التكليف:** New Quizzes **لا تملك «Item Groups» ولا أقساماً**.
الاختبار قائمةٌ مرتّبةٌ مسطّحة. وأقرب شيءٍ إلى «جزء» هو كتلةٌ مربوطةٌ ببنك تُنشئها
`Add All/Random`، وواجهتها: `Select the destination bank` · `Use all questions` ·
`Randomly select questions` · `Number of questions` · `Points per question`.
([السحب من بنك](https://community.instructure.com/en/kb/articles/661082-how-do-i-add-all-items-or-a-random-set-from-an-item-bank-to-a-quiz-in-new-quizzes))

**وتختار على البنك كلّه ولا شيء أدقّ.** الوسوم موجودة (`Tags and Metadata`)
لكنّ الوثائق صريحة أنّها **للبحث في البنك** لا للسحب: *"Tags can be used to search
for items within item banks"*؛ *"Search results can be filtered by tags or item
type."*

والفصل الصحيح للمسؤوليّات مذكورٌ نصّاً: *"After a question has been added to an
item bank, all question properties **other than point value and certain
options** must be edited in the item bank."* أي أنّ المحتوى على عنصر البنك،
و**حقائق الاستعمال (الدرجة، الموضع) على مدخل الاختبار** — وهو الانقسام نفسه الذي
يعيشه `PaperSlot` هنا.

**وللمقارنة، Classic Quizzes كانت أغنى:** `New Question Group` باسم، و«كم سؤالاً
يُنتقى عشوائيّاً»، و`Link to a Question Bank`؛ والسلوك: *"Canvas will reference
your chosen bank of questions as each student takes the quiz. Each student will
get a specified number of questions, pulled from the bank at random."*
([مجموعة مربوطة ببنك](https://community.instructure.com/en/kb/articles/661000-how-do-i-create-a-quiz-with-a-question-group-linked-to-a-question-bank))
**المجموعةُ المسمّاة والمربوطة ببنك هي بالضبط ما نوصي به أدناه، وInstructure
تراجعت عنها ومجتمعها يشتكي.** لا تُكرَّر تلك التراجعة.

### ٢.٤ TAO — الإعدادات على القسم، والبِركة قائمةٌ مجمّدة

**[موثَّق]** من دليل المستخدم الرسميّ.

المسرد: *"A section of a Test, which can be managed independently. **Settings for
item selection and ordering are defined at section level.**"*
([المسرد](https://userguide.taotesting.com/user-documentation/latest/public/glossary))

ولوحة الاختيار حرفيّاً: *"The **Selection** panel asks if the delivered test
section should include only some of the items assigned to it (**Enable
selection**), and if so, how many (**Select**). This option ensures that test
takers will not all receive an identical test."*
([إعدادات القسم](https://userguide.taotesting.com/user-documentation/latest/public/test-section-settings))

**وهذا فرقٌ جوهريّ عن Moodle: البِركة قائمةٌ عيّنها المؤلّف يدويّاً، لا استعلامٌ
حيّ.** والمؤلّف يبحث في بنك المحتوى (`Search by properties`) ثمّ يضيف
(`Add selected item(s) here`).

**وحذارِ:** `Category` في TAO موجودٌ على مستوى الجزء والقسم ومرجع السؤال، لكنّه
**للتجميع في النتائج**: *"Categories are used (mostly) for aggregating the scores
for the various questions and responses in terms of the learning outcomes that
are being measured."*
([التقدير](https://userguide.taotesting.com/user-documentation/latest/public/scoring-tests))
**[غير موثَّق]** ما تقوله دعاية `taotesting.com` عن «اختيارٍ مدفوعٍ بالبيانات
الوصفيّة» — لم يُعثر عليه في دليل المستخدم، ولا يُبنى عليه.

### ٢.٥ Surpass — أقرب شكلٍ إلى ما نوصي به، ومُسمّىً صراحةً

**[موثَّق]** من `help.surpass.com` مباشرةً. **هذا أقوى شاهدٍ في الوثيقة كلّها.**

خصائص القسم تحمل حقلاً اسمه **`Content rules`** بقيمٍ أربع:
**Fixed · Dynamic · External Optimiser · Adaptive**؛ إلى جانب `Name`,
`Description`, `Total items`, `Randomise items`, `Pass Mark`, `Total marks`,
`Number of items to mark`, `Section duration`, `Forward only section`,
`Items require response`.
([إضافة أقسام](https://help.surpass.com/documentation/test-creation/adding-sections-to-a-test-form/))

والقسم الديناميّ حرفيّاً *"contains rules that select test items from an item
pool"*؛ ولكلّ قاعدة **`Minimum number of items`** و**`Maximum number of items`**،
و*"the number of items specified by all rules in a dynamic section match the
**Total items** setting in **Section Properties**."*
([قواعد القسم الديناميّ](https://help.surpass.com/documentation/test-creation/adding-rules-to-a-dynamic-section/))

وما تختار عليه القاعدة (`Select Reference`): `MarkingType`, `Folder reference`,
`Learning Outcomes / Units / Keywords`, `Item name`, `Item type`,
`Marked metadata`, `Custom tag groups`, `TotalMark`, **`P value`**,
`Question type`, `Specific item`, `Date reference`, **`Workflow status`**.
([معاملات البحث](https://help.surpass.com/documentation/test-creation/about-test-form-search-parameters/))

وفي القسم الثابت، العلاقة على النموذج لا على السؤال: *"To add an item to a test,
choose your item and select **Add** or drag the item into the test."*
([إضافة أسئلة](https://help.surpass.com/documentation/test-creation/adding-items-to-a-test-form/))

**قيدٌ يستحقّ النقل:** *"Friend and enemy relationships do not work with dynamic
rules."* أي أنّ Surpass نفسها تعترف بأنّ القاعدة الديناميّة تُضعف ضماناتٍ معيّنة
حول ما يُسحب معاً — وهو التوتّر نفسه بين القاعدة والكتلة (`QuestionGroup`) هنا.

### ٢.٦ ExamSoft — تصنيفاتٌ متعدّدةٌ على السؤال، ومخطّطٌ يصمّم كلّ قسمٍ بتصنيف

**[مقتطف]** — `support.examsoft.com` يردّ 403؛ ما يلي مقتطفاتٌ لصفحاتٍ رسميّةٍ
مسمّاة.

**الأقسام** — *"Enterprise Portal: Overview of Exam Sections"*: *"Sections create a
unique structure for an assessment. The assessment is graded as one exam and
appears in reports as one exam; however, the exam-taking experience is split up
into smaller units. Compare it to an exam with multiple exam booklets…"* مرتّبة،
ولا رجوع بعد التسليم، ولكلٍّ وقتها، والخلط داخل القسم لا عبره.
([الأقسام](https://support.examsoft.com/hc/en-us/articles/11167893702157-Enterprise-Portal-Overview-of-Exam-Sections))

**المخطّط** — *"Enterprise Portal: Create an Assessment Blueprint"*: *"you might
set up a blueprint that requires five questions from each category in Bloom's
Taxonomy"*؛ و**«With this approach, you design each section of the assessment by
selecting a category, and then add the desired number of questions from each
sub-category.»**
([المخطّط](https://support.examsoft.com/hc/en-us/articles/11168066027661-Enterprise-Portal-Create-an-Assessment-Blueprint))
**هذه أوضح صياغةٍ وجدناها لما نوصي به: القسم يُصمَّم بتصنيفٍ وعدد.**

**وكيف يتجنّبون المؤشّر الواحد؟ بجعل التصنيف متعدّداً على السؤال**: *"You can tag
individual exam items to as many different parent categories and/or
sub-categories as you wish"*؛ ومع تحذيرٍ مهمّ: *"Tagging a question that includes
a sub-category does not automatically tag the question to the super-categories
above it."*
([إسناد التصنيفات](https://support.examsoft.com/hc/en-us/articles/11147598207757-Enterprise-Portal-Assign-Categories-to-Questions))
**لاحظ أنّ التعدّد على التصنيف — وهو مستقلٌّ عن الاختبار — لا على القسم.**

### ٢.٧ Cirrus — مرحلتان: بنك ← بِركة تقييم ← نموذج

**[موثَّق]** من `help.cirrusassessment.com`.

الخطوة ٣ اختيارٌ يدويٌّ من البنك بمرشّحات: *"collection, marking type, taxonomy,
item purpose, difficulty, question type, labels, learning objectives, and
topics"*، بأزرار `Include Items` و`Exclude Items`. و«العرض بالمخطّط» **للقراءة
فقط**: *"Show Blueprint … Although you may not choose items via a blueprint."*
([الاختيار اليدويّ](https://help.cirrusassessment.com/docs/manual-question-selection))

ثمّ النموذج يسحب من تلك البِركة: **Fixed** (*"generated in advance. All candidates
receive the same set of questions"*) أو **Random** (*"Linear On the Fly Test
generation (LOFT) … each candidate receives a different set of questions"*)، مع
*"define the weighting across the three difficulty levels (Easy, Medium, Hard)."*
([النماذج](https://help.cirrusassessment.com/docs/step-4-forms))

**والمخطّط عندهم مصفوفة**: «هدف تعلّم × مستوى تصنيفيّ»، بحدٍّ أدنى وأقصى في كلّ
خليّة ([blueprint](https://help.cirrusassessment.com/docs/blueprint.md)) — وهو
أقرب تطبيقٍ غربيٍّ عامل لـ**جدول المواصفات** العربيّ (§٤). لكنّه في الخطوة ٣
**للقراءة فقط**، ولا يُنتقى به.

**وتحذيرٌ مصطلحيّ مهمّ:** «Section» في Cirrus **ليست** جزءاً من الورقة بالمعنى
المقصود هنا: *"You can create Sections in Cirrus to combine multiple Items in one
case study"* و*"You can also use a section to create an assessment within the
assessment, including its own duration time and pass mark."*
([القسم](https://help.cirrusassessment.com/docs/section))
أي أنّها تخلط بين `QuestionGroup` و`ExamSection`. **وفصلهما هنا اختيارٌ صحيح
يجب التمسّك به** — ودليله في هذه الشيفرة أنّ `QuestionGroup.ExamSectionId` حُذف
بالأمس لأنّه كان ميّتاً على الجانبين (`Drop_Section_From_Passage`).

### ٢.٨ Questionmark — الكتلة قسم، والسحب من موضوع

**[مقتطف]** و**[ثانويّ]** — كلّ نطاقات Questionmark ردّت 403.

**[مقتطف بائع]** `questionmark.com/platform/flexible-authoring/`: *"organize
questions into question blocks and sections, set delivery rules like
randomization, and configure time limits to match your exam blueprint"*؛ و*"organize
your questions by topic, utilize randomized delivery or use a question in various
test formats within the item bank."*

**[مقتطف بائع]** `questionmark.com/new-tools-for-building-questions-and-assessments/`:
الواجهة الجديدة تضيف *"single questions, entire topics, and **random pull from a
topic**"*، وخيارٌ اسمه `All questions from topic`.

**[ثانويّ]** موادّ تدريبٍ جامعيّة (Warwick، Edinburgh): *"Blocks can be used to
create sections of the assessment that examine different topics. All assessments
must have at least 1 question block."* و`Jump Block` يتفرّع على درجة الكتلة
السابقة.

**[غير موثَّق]** هل يقع سؤال Questionmark في موضوعٍ **واحد** بالضبط (شجرة أحاديّة
الأب) أم في عدّة مواضيع؟ **لم يُتحقَّق، ولا يُدَّعى.** وهذا سؤالٌ مهمّ لنا لأنّه
الفرق بين نموذج ExamSoft (تعدّد) وشجرةٍ أحاديّة.

### ٢.٩ Mercer Mettl — القسم اسمٌ يُعطى لقواعد السحب، ولا وجود له سواه

**[موثَّق]** من `support.mettl.com` مباشرةً. **وهذا أطرف شاهدٍ في الوثيقة وأشدّها
دلالة.**

**الدليل في مخطّط واجهتهم البرمجيّة نفسه.** «إنشاء تقييم» في Mettl يوثّق البنية:

```json
"sections": [{
  "name", "duration", "instructions", "allQuestionsMandatory", "randomizeQuestions",
  "skills": [{ "name", "level", "questionCount", "questionPooling",
               "questionType", "correctGrade", "incorrectGrade" }]
}]
```
([Create Assessment API](https://support.mettl.com/portal/en/kb/articles/create-assessment-api))

**القسم يحوي مصفوفة `skills`، لا مصفوفة أسئلة. ولا حقل في الواجهة كلّها يربط
معرّف سؤالٍ بقسم.** ولاحظ أنّ القسم مع ذلك يحمل `duration` و`instructions`
و`randomizeQuestions` — أي **كلّ ما يحمله `ExamSection` هنا** — دون أن يملك سؤالاً
واحداً. وهذا يهدم مسبقاً الاعتراض «لكنّ قسمنا يحتاج أن يملك أسئلته ليحمل وقته
وتعليماته».

وفي الواجهة، المؤلّف يضيف «مهارات/بنوك أسئلة» (`skills / Question Banks`)، ويحدّد
لكلٍّ منها كم سؤالاً يُعرض عشوائيّاً، ثمّ **يسمّي القسم بالنقر المزدوج على اسمه
الافتراضيّ**. وحرفيّاً:

> *"We can set the same section name for multiple `skills / Question Banks` that
> has been added. Thus, when the test is started questions from these skill sets
> would be presented under one 'Section Name'."*

والعدد: النظام *"will choose the number of questions that we want to randomly
display in the test (from the available questions in the selected
skills/topic.)"*؛ ومع ذلك *"if you wish to pick & select a specific set of
questions, you can do so by clicking on the total available questions."*
([تخصيص مخطّط الاختبار](https://support.mettl.com/portal/en/kb/articles/customization-of-blue-print-for-the-test-basic-23-3-2023))

**أي أنّ القسم في Mettl ليس إلّا مفتاح تجميعٍ على قواعد السحب.** وهو الشكل الذي
نوصي به، مدفوعاً إلى نهايته المنطقيّة: البنك ← قاعدة (بنك + عدد) ← اسم قسم.
ولاحظ الازدواج المسموح صراحةً: عشوائيّاً **أو** انتقاءً يدويّاً، وهو ما يقابل
`Fixed`/`Dynamic` عند Surpass و«المصنَّف/القاعدة» عندنا.

**وقيدٌ يقابل بنيتنا مباشرةً، موثَّقٌ حرفيّاً:** *"Please note that **one question
can have only one topic name**, however questions for varying topics can be
uploaded simultaneously."* وحقل الموضوع نفسه يُشرَح بأنّه *"Enter the '**Question
Bank**' under which you want the question to be saved on the platform"*؛ وإن لم
يوجد الاسم *"new topic will be created under 'My Questions'."*
([إنشاء سؤال اختيار من متعدّد](https://support.mettl.com/portal/en/kb/articles/how-to-create-multiple-choice-questions-mcq-27-3-2023))

**أي أنّ «الموضوع» في Mettl هو بنك الأسئلة نفسه، والسؤال في واحدٍ منه بالضبط** —
شجرةٌ أحاديّة الأب، تماماً كما `Question.TopicId` هنا. **فالنموذج الذي نوصي به
لا يحتاج تعدّداً على أيّ طرف، وأكبرُ منافسٍ في السوق يبرهن ذلك بشحنه.**

### ٢.١٠ ClassMarker — لا أقسام أصلاً؛ التصنيف هو القسم

**[موثَّق]** من `classmarker.com` مباشرةً.

*"Parent and Sub Categories are used to organize your Questions, Tests and
surveys."* ([التصنيفات](https://www.classmarker.com/online-testing/manual/categories))

وفي إضافة الأسئلة العشوائيّة: *"**Option 2 allows you to select Categories and to
define how many Questions per Category are to be Randomly selected.**"*
([الأسئلة العشوائيّة](https://www.classmarker.com/online-testing/manual/autoquestions))

والوضعان اثنان لا ثالث لهما: **الأوّل** عددٌ إجماليٌّ من البنك كلّه، **والثاني**
*"Define a specific number of Questions per selected Category"* مع إعادة ترتيب
التصنيفات بالسحب. **وفوقهما** *"Choose where you would like these Questions to be
displayed on your Test"* — أي أنّ الأسئلة الثابتة والمسحوبة **تتعايش في اختبارٍ
واحد، والمؤلّف يقرّر أين تُدرَج المسحوبة.**

**وهذا يحسم سؤالاً مفتوحاً عندنا** (§٧): حين يجتمع في قسمٍ واحدٍ أسئلةٌ مصنَّفةٌ
وقاعدةُ سحب، الجمع بينهما **سلوكٌ مقصودٌ في السوق لا حالةٌ شاذّة** — والذي يجب
اختياره هو موضع الإدراج، لا منع الاجتماع.

**ولا تذكر وثائق ClassMarker ميزةَ «أقسام» بتاتاً.** بنية الاختبار عندهم هي
التصنيف نفسه، وقاعدة الاختيار «كم سؤالاً من كلّ تصنيف». وهذا هو حلّ المشكلة
بحذف الطرف الخطأ منها: بدل عمودِ قسمٍ على السؤال، يُستعمل التصنيفُ الذي يحمله
السؤال أصلاً — وهو بالضبط منطق الخطوة (أ) في §٥.

والموضع الوحيد الذي تقول فيه ClassMarker «قسم» غيرُ رسميّ، ويصف شعور الممتحَن:
*"you can display category names beside questions during a test … This will assist
in helping Test-takers better comprehend the context of your questions or see
which section they are in."*
([إنشاء التصنيفات](https://www.classmarker.com/online-testing/blog/How-to-Create-Question-Categories))

**[غير موثَّق]** هل يقع سؤال ClassMarker في أكثر من تصنيف؟ صفحةٌ تقول *"categories
each question is included in"* بالجمع، ولا شيء يحسمها.

### ٢.١١ Amp-up.io

**[غير موثَّق] — لم يُراجَع.** وردت في مسوّدةٍ لهذه الوثيقة نتيجةٌ سالبةٌ عن
amp-up.io («مسلِّمٌ لا مؤلِّف»، واعتمادٌ في نطاق Delivery فقط)، **ثمّ سُحبت لأنّ
قائلها اعترف بأنّه لم يجلب موقعهم ولا وثائقهم أصلاً.** فلا يُدَّعى عنهم شيء، لا
إيجاباً ولا سلباً. ومن أراد أن يرى كيف تُعرَض `qti-selection` على مؤلِّفٍ بشريّ
فمرجعه TAO (§٢.٤)، وهو موثَّق.

### ٢.١٢ TestGorilla — البنيةُ نفسها وحدةٌ قابلةٌ لإعادة الاستعمال

**[مقتطف]** — `support.testgorilla.com` يردّ 403. المقتطف: *"An assessment can
contain a maximum of 5 tests, and 10 or 20 custom questions (depending on your
plan)"*. أي أنّ «الاختبار» هو القسم، وهو نفسه وحدةٌ جاهزةٌ من مكتبةٍ مشتركة —
معمارٌ ثالثٌ لا يناسبنا (أقسامنا يؤلّفها العميل)، لكنّه يبيّن أنّ **إعادة
الاستعمال ترتفع دائماً إلى مستوى البنية، لا تنزل إلى مستوى السؤال**.

### ٢.١٣ الجدول | The table

| المنتج | أين تعيش «أيّ قسم» | تختار القاعدة على | هل على السؤال مؤشّرُ قسم؟ |
|---|---|---|---|
| **Moodle** | `quiz_sections.firstslot` (مدى خانات) | تصنيف **إلزاميّ** + مرشّحات إضافيّة (وسوم، حقول مخصّصة) في `filtercondition` JSON | **لا** |
| **QTI 3.0** | احتواء XML في `qti-assessment-section` | لا شيء — `select="N"` من الأبناء المعدَّدين فقط | **لا** |
| **Canvas NQ** | `position` + `entry_type` على مدخل الاختبار | بنكٌ كامل فقط (لا وسوم، لا صعوبة) | **لا** |
| **TAO** | قائمة مراجع القسم | لا شيء — `Select N` من المعيَّن للقسم | **لا** |
| **Surpass** | `Content rules` على القسم | مجلّد، مجموعات وسوم، نوع، `P value`، حالة سير العمل… | **لا** |
| **ExamSoft** | القسم، والمخطّط يصمّمه | تصنيف/تصنيف فرعيّ + عدد | **لا** (التصنيفات متعدّدة على السؤال) |
| **Cirrus** | بِركة التقييم ثمّ النموذج | تصنيف/موضوع/صعوبة عند البناء؛ ووزن صعوبةٍ في LOFT | **لا** |
| **Mettl** | **اسمٌ يُعطى لقواعد السحب** — لا كيان قسم | مهارة/بنك + عدد (أو انتقاءٌ يدويّ) | **لا** |
| **ClassMarker** | **لا أقسام** — التصنيف هو البنية | تصنيف + «كم سؤالاً من كلّ تصنيف» | **لا** |
| **Questionmark** | الكتلة | موضوع (`random pull from a topic`) | **[غير موثَّق]** |
| **هذا المنتج اليوم** | **عمودٌ على السؤال** | (القاعدة موجودة ومعزولة عن البنك) | **نعم — وحده** |

**عشرةٌ من عشرة تضع الحقيقة على البنية. وواحدٌ يضعها على السؤال، وهو نحن.**

---

## ٣ · الأسئلة الخمسة، مُجابةً | The five questions, answered

**١. أين تعيش الحقيقة؟** على البنية، في تسعةٍ من تسعة. إمّا مرجعٌ يملكه القسم
(Moodle، QTI، Canvas، TAO، Surpass الثابت)، وإمّا قاعدةٌ يملكها القسم
(Surpass الديناميّ، ExamSoft، Questionmark، Moodle العشوائيّ). **ولا أحد يضعها على
السؤال.**

**٢. وإن عاشت على السؤال، كيف يمنعون خطأها لسؤالٍ مشترك؟** لا أحد يواجه هذا
السؤال، لأنّ لا أحد يضعها هناك. وأقرب شيء: **ExamSoft يجعل التصنيف متعدّداً على
السؤال** — وهو ينجح لأنّ التصنيف **مستقلٌّ عن الاختبار**؛ «Bloom: تطبيق» صحيحةٌ في
كلّ ورقة، بينما «قسم الاستماع في اختبار س» ليست صحيحةً إلّا في اختبار س.
**وهذا هو الفرق الذي يُبطل خيار الربط متعدّد-إلى-متعدّد بين السؤال والقسم.**

**٣. على ماذا تختار القاعدة؟** موضوع/تصنيف أوّلاً وقبل كلّ شيء (إلزاميّ في Moodle،
محور المخطّط في ExamSoft، أساس Questionmark)، ثمّ وسوم (Moodle، Surpass)، ثمّ
صعوبة أو `P value` (Surpass، Cirrus)، ثمّ نوع السؤال، ثمّ حالة سير العمل
(Surpass). **وهذه القائمة تطابق `ExamBlueprintRule` الحاليّ تقريباً حرفاً بحرف:
`TopicId` + `Difficulty` + `QuestionType`.**

**٤. وQTI؟** لا تعبّر عنها إطلاقاً. البنك ليس في نموذج المعلومات، والقسم يعدّد
مراجعه، والاختيار من الأبناء فقط. فأيّ قاعدةٍ نبنيها **لن تُصدَّر كقاعدة** — تُصفَّى
أو تُعدَّد. **لكنّ هذا ليس حجّةً ضدّ القاعدة: العمود الحاليّ على السؤال لا يُصدَّر
أصلاً، لأنّ QTI لا تعرف حقلاً كهذا على السؤال بتاتاً.** فالقاعدة أسوأ حالاً في
التصدير بمقدار صفر.

**٥. ماذا يسمّونها؟** بالإنجليزيّة: `question bank` / `item bank`، `category` /
`topic` / `folder` / `tag` (وفي Mettl **الموضوع هو البنك نفسه**)، `section`
(Moodle: `section heading`؛ QTI: `qti-assessment-section`؛ Surpass: `Section`
بـ`Content rules`؛ Mettl: `Section Name` فوق `skills[]`)، `blueprint`،
`random question` / `dynamic section` / `LOFT`. **وبالعربيّة — وهي مسألةٌ أكبر
ممّا تبدو — انظر §٤، وفيه أنّ لا منتجٍ عربيٍّ موثَّقٍ يملك كلمةً لـ«قسم اختبار»
أصلاً.**

---

## ٤ · المنتجات العربيّة والمصطلحات | MENA products and Arabic vocabulary

### ٤.١ الاكتشاف الأهمّ في هذا القسم

**لا منتجَ عربيٍّ أمكن التحقّق منه يعرض «قسماً له قاعدة اختيار». كلّها تستعمل
«جدول المواصفات» بنيةً أساسيّةً بدلاً من ذلك.** وهذا ليس نقصاً في ميزاتها؛ هو
اختلافٌ حقيقيٌّ في الممارسة: جدول المواصفات راسخٌ في القياس التربويّ العربيّ،
وهو ما يفكّر به المعلّم أصلاً.

**وهذا يقوّي التوصية لا يضعفها**: `ExamBlueprintRule` هنا **هو** صفٌّ من جدول
مواصفات (موضوع × صعوبة × نوع → عدد)، وإسناده إلى قسمٍ يجعله جدول المواصفات
الذي يعرفه العميل العربيّ. أقربُ تطبيقٍ غربيٍّ عاملٍ لذلك هو مصفوفة Cirrus
(§٢.٧).

### ٤.٢ Qorrect (مصر) — «متعدّد الأقسام» عندهم **عرضٌ لا اختيار**

**[موثَّق]** الوحدتان `QBank` و`QAssemble`؛ والصفحة الإنجليزيّة: *"Design
blueprints that align questions with learning outcomes, cognitive levels, and
difficulty"* ([qorrectassess.com/en](https://qorrectassess.com/en/)). والميزات
المسمّاة: «Regular & Sectioned Exams»، «Exam Blueprint»، «Test Forms»
([الميزات](https://qorrectassess.com/en/assessment-system-features)).

**[مقتطف]** وصفحة إدارة الاختبارات تعرّف «متعدّد الأقسام» تعريفاً **عرضيّاً لا
انتقائيّاً**: *"The multiple sectioned exam means your exam can show one question
at a time and this way you can limit students' getting distracted"* (الجلب
المباشر يردّ 404؛ النصّ من فهرس البحث للصفحة المسمّاة). **فإن صحّ، فـ«أقسام»
Qorrect ليست أقسامنا أصلاً.** وإعادة استعمال السؤال بالمرجع مؤكّدة: التقارير
تُظهر *"how many times they were previously used and in which exams."*

**⚠️ تحذيرٌ منهجيّ يجب تسجيله.** صفحات Qorrect العربيّة تُقرأ هنا عبر **مُلخِّص**،
وقد أنتج المُلخِّص في إحدى القراءات عبارةً **ليست على الصفحة**. فما في الجدول
أدناه من مصطلحات Qorrect **يجب أن يتحقّق منه إنسانٌ بعينه قبل أن يُنقَل إلى
`ar.json`.** ولم يُعثر على أيّ كلمةٍ عربيّةٍ لـ«قسم» في صفحات Qorrect العربيّة.

### ٤.٣ اختبار «همزة» — أكاديميّة الملك سلمان — **أفضل مرجعٍ عربيٍّ لهذا المنتج**

**[موثَّق]** من الصفحة الرسميّة:
[ksaa.gov.sa/ar/initiatives/50969](https://ksaa.gov.sa/ar/initiatives/50969-اختبار-همزة-الأكاديمي)

اختبار كفاية في العربيّة على مقياس CEFR من A2 إلى C1 — أي **اختبار تحديد مستوىً
لغويّ، وهو حالة استعمال هذا المنتج بعينها**:

- أجزاء الاختبار تحت عنوان **«مكونات الاختبار»** — وهذا هو المصطلح الرسميّ، لا
  «محاور» ولا «أجزاء».
- المجالات: **فهم المسموع** (٣٠ فقرةً) · **استيعاب المقروء** (٤٠ فقرةً) ·
  **الكتابة** (فقرة واحدة) · **التحدُّث** (٤ فقرات).
- **ووحدة العدّ «فقرة» لا «سؤال».**

### ٤.٤ الباقي

- **مدرستي / وزارة التعليم السعوديّة** — **[موثَّق]**: *«بنك الأسئلة خدمة تتيح
  للمعلم من إضافة أسئلة للطلاب في الاختبارات والواجبات»*
  ([moe.gov.sa](https://moe.gov.sa/ar/knowledgecenter/eservices/pages/questionsbank.aspx)).
  **«بنك الأسئلة» هو المصطلح الحكوميّ السعوديّ الرسميّ.** وما وراء ذلك من بنيةٍ
  (تصنيفات، أقسام، قواعد) غير موثّق.
- **Classera** — **[موثَّق]** النصّ العربيّ: *«اختر من بنك الأسئلة أو المناهج
  الدراسية، حدد نوع الأسئلة»* ([classera.com/ar](https://classera.com/ar/products/classera-lms/))؛
  وعناوين الدليل: «Add Questions from Q-Banks»، «بنوك الأسئلة»
  ([manual.classera.com](https://manual.classera.com/docs/question-banks/)).
  ظاهرها **انتقاءٌ ونسخ** لا قاعدة، **[غير موثَّق]** لأنّ صفحة التفصيل ترد 404.
- **منصّة مجد** (السعوديّة) — **[موثَّق]** تستعمل: **بنوك الأسئلة**، **تصنيف
  الأسئلة**، **جدول المواصفات**، **مستويات التفكير**، **مستوى معرفي**، **الأهداف
  التعليمية**؛ **ولا تَرِد «أقسام» ولا «عشوائي» حرفيّاً**
  ([majd.edu.sa](https://majd.edu.sa/ar/blog/تصميم-اختبار-الكتروني-وتصحيحه-تلقائي/)).
- **SwiftAssess** — **[غير موثَّق]. كلّ عناوينها ترد 403** (الإنجليزيّ والعربيّ
  والصفحات القديمة وقائمة Microsoft Marketplace). الموثَّق منها **عنوانا صفحتين
  فقط** من فهرس البحث: «إدارة بنك الأسئلة الشامل»
  ([question-bank](https://swiftassess.com/ar/features/question-bank/)) و«أدوات
  تأليف الاختبارات القوية»
  ([test-authoring](https://swiftassess.com/ar/features/test-authoring/)) — أي
  **بنك الأسئلة** و**تأليف الاختبارات**. وكلمتُهم لـ«قسم» لم تُستخرج.
- **هيئة تقويم التعليم والتدريب / قياس** — **`etec.gov.sa` غير قابلٍ للوصول من
  هذه البيئة** (رفض اتّصالٍ مؤكّدٌ مرّتين). **[ثانويّ من الفهرس]** أنّ «نافس»
  يوصف بـ*«أطُرٍ مرجعيَّةٍ مُعْتمَدةٍ في **مجالات** التعلّم»* — أي **مجال** للنطاق
  المحتوائيّ. و«أقسام» قدرات (خمسة أقسام، كمّيّ/لفظيّ) **من مواقع تحضيرٍ خارجيّة
  لا من الهيئة**. لا يُبنى عليه.
- **Edaà / إدارة** — **لم يُعثر عليه**. لا منتج تقييمٍ بهذا الاسم في الفهرس العلنيّ
  تحت أيّ هجاءٍ جُرِّب.
- **نظام نور** نظام معلومات طلّاب لا أداة تأليف — لا بنك ولا أقسام.
  **تطوير** و**المنهل** — لا شيء قابلٌ للتحقّق (المنهل يردّ 403).

### ٤.٥ جدول المصطلحات | The terminology table

| المفهوم | العربيّة | الحال والمصدر |
|---|---|---|
**تصحيحٌ يجب أن يُقرأ أوّلاً.** حملت مسوّدةٌ لهذه الوثيقة مصطلحاتٍ منسوبةً إلى
مسرد **المجلس الأعلى للجامعات المصريّ** (وفيه تفريقٌ بين «بنك الأسئلة = Item
Bank» و«مستودع الأسئلة = Item Pool»)، **ثمّ سُحبت كلّها** لأنّ قائلها اعترف بأنّه
لم يبلغ الصفحة (ردّت 404). **فحُذفت من الجدول أدناه، ولا يُبنى عليها.** وهي
تفرقةٌ لو صحّت لكانت مفيدة، فتستحقّ أن يتحقّق منها إنسانٌ يوماً.

| المفهوم | العربيّة | الحال والمصدر |
|---|---|---|
| question bank | **بنك الأسئلة** | ✅ **موثَّقٌ بقوّة، من مصادر مستقلّة** — Moodle `ar/quiz.php` `$string['questionbank'] = 'من بنك الأسئلة'`؛ وزارة التعليم السعوديّة *«بنك الأسئلة خدمة تتيح للمعلم…»*؛ Qorrect؛ Classera؛ SwiftAssess؛ مجد |
| bank management | **إدارة بنك الأسئلة** | ✅ Moodle `questionbankmanagement` |
| question | **سؤال / الأسئلة** | ✅ في كلّ مصدر |
| item (وحدةَ عدٍّ قياسيّة) | **فقرة / فقرات** | ✅ **رسميّ سعوديّ** — همزة: «(٣٠) فقرةً»، «(٤٠) فقرةً»، «فقرةً واحدة»، «(٤) فقرات» |
| item (بديل) | **مفردة** · **بند** | ⚠️ **[غير موثَّق]** — وردا في مسوّدةٍ منسوبين إلى أدبيّاتٍ قياسيّة، ثمّ سُحب الإسناد. لا يُبنى عليهما |
| category | **صنف** و**تصنيف** معاً | ⚠️ **حزمة Moodle العربيّة متناقضةٌ داخل الملفّ الواحد** [شيفرة]: `randomfromcategory` = «سؤال عشوائي من **الصنف**:»، و`randomcatwithsubcat` = «{$a} و**أصنافه الفرعيّة**»، بينما `addrandomfromcategory` = «إضافة أسئلة عشوائية من **تصنيف**» ونصّ المساعدة «اختيار سؤال عشوائي من **التصنيف**» |
| random question | **سؤال عشوائي** | ✅ [شيفرة] Moodle: `random`، `addarandomquestion`، `addrandomquestion` = «أضف سؤال عشوائي»، `addrandom2` = «أسئلة عشوائية» |
| **section (قسم اختبار)** | **قسم** | ✅ [شيفرة] Moodle `$string['addasection'] = '**عنوان قسم جديد**'` — **موثَّقٌ مباشرةً من الملفّ الخام** |
| section heading | **رأس القسم** | ✅ [شيفرة] Moodle `confirmremovesectionheading` = «إزالة **رأس القسم**»، و`cannotremoveallsectionslots` = «تحت **رأس القسم**» |
| section (تناقضٌ داخليّ) | **مقاطع** | ⚠️ [شيفرة] سلاسل الأحداث في Moodle تستعمل «**فاصل مقاطع**» و«خلط **المقاطع**» و«عنوان **المقاطع**» — أي أنّ الحزمة نفسها تترجم section بكلمتين |
| shuffle | **خلط** | ✅ [شيفرة] Moodle `eventsectionshuffleupdated` = «**خلط** المقاطع تم تحديثه» |
| أجزاء الاختبار (عنواناً) | **مكونات الاختبار** | ✅ **رسميّ سعوديّ** — همزة/أكاديميّة الملك سلمان |
| section/domain | **محور / محاور** | ❌ **[غير موثَّق]** — في ويكيبيديا وتغطيةٍ صحفيّة، **وليس** على صفحة همزة الرسميّة (وهي تقول «مكونات الاختبار») |
| domain (محتوىً) | **مجال / مجالات** | ⚠️ **[مقتطف]** فقط — `etec.gov.sa` غير قابلٍ للوصول |
| section | **جزء** | ❌ **[غير موثَّق] — لا مصدر واحد يستعمله. لا يُستعمل.** |
| table of specifications | **جدول المواصفات** | ✅ **موثَّقٌ بقوّة** — Qorrect *«تصميم إطار عام ينظم عملية توزيع أسئلة الاختبار»*؛ مجد؛ محاضراتٌ جامعيّةٌ عراقيّة ([ديالى](https://basicedu.uodiyala.edu.iq/جدول-المواصفات-أو-الخارطة-الاختيارية/)، [بغداد](https://copew.uobaghdad.edu.iq/wp-content/uploads/sites/23/2023/01/محاضرة-9-جدول-المواصفات.pdf)) |
| مكوّنات جدول المواصفات | **الوزن النسبي** · **مستويات الأهداف التعليمية (تذكر – فهم – تطبيق – تحليل)** · **توزيع الدرجات** | ✅ Qorrect، [مقالة جدول المواصفات](https://qorrectassess.com/ar/blog/بناء-جدول-المواصفات-للاختبارات-التحص/) |
| thinking levels | **مستويات التفكير** · **مستوى معرفي** | ✅ مجد |
| difficulty | **مستوى الصعوبة** | ✅ Qorrect |
| classification | **تصنيف الأسئلة** | ✅ Qorrect: *«تصنيف الأسئلة حسب المواضيع وأهداف التعلم ومستوى الصعوبة»*؛ مجد |
| test authoring | **تأليف الاختبارات** | ✅ عنوان صفحة SwiftAssess العربيّة |
| listening (مجالاً) | **فهم المسموع** (والمهارة: الاستماع) | ✅ همزة الرسميّة |
| reading (مجالاً) | **استيعاب المقروء** (والمهارة: القراءة) | ✅ همزة الرسميّة |
| writing / speaking | **الكتابة / التحدُّث** | ✅ همزة الرسميّة |
| question pool · random block | **مخازن الأسئلة** · **الكتل العشوائية** | ⚠️ **[مقتطف]** — عناوين صفحاتٍ عربيّةٍ لـBlackboard من فهرس البحث؛ العناوين الحيّة تحوّل إلى الإنجليزيّة، فلم يُقرأ متنها |
| «**اختيار عشوائي**» مصطلحاً | — | ⚠️ لا يَرِد اسماً مستقلّاً في أيّ منتج. المُثبَت [شيفرة] هو **الصفة** (سؤال عشوائي) و**المصدر داخل جملة**: Moodle «…يؤدّي إلى **اختيار سؤال عشوائي** من التصنيف» |

**مصدر سلاسل Moodle العربيّة هنا [شيفرة]:** جُلب الملفّ الخام كاملاً (١١٠ كيلوبايت)
وقُرئ حرفيّاً، لا عبر مُلخِّص:
[`ar/quiz.php`](https://github.com/projectestac/moodle-langpacks/blob/master/ar/quiz.php).
وهو **مرآة** لا `lang.moodle.org` (خلف تسجيل دخول). **ولا وجود لتوثيق Moodle
بالعربيّة أصلاً** — `docs.moodle.org/all/ar/` يحوّل إلى الإنجليزيّة.

**والخلاصة المصطلحيّة الأهمّ:** لا منتجٍ تجاريٍّ عربيٍّ أمكن التحقّق منه يعرض
كلمةً لـ«قسم اختبار». الكلمة الوحيدة الموثَّقة في **برمجيّة** هي **«قسم»** في
حزمة Moodle العربيّة، والوحيدة الموثَّقة في **جهةٍ حكوميّةٍ** هي **«مكونات
الاختبار»** عند أكاديميّة الملك سلمان. **وهذا يعني أنّ هذا المنتج يختار مصطلحاً
لا ينقله — فليختره بعناية، و«القسم» الذي يستعمله اليوم هو الخيار الصحيح.**

### ٤.٦ أربع توصياتٍ مصطلحيّةٍ لهذا المنتج

**١. «جزء» يجب أن يُحذف من واجهة المنتج.** لا مصدر عربيٍّ واحدٍ — منتجاً ولا حكومةً
ولا حزمةَ ترجمة — يستعمله بمعنى قسم الاختبار. وهو اليوم في موضعين على الأقلّ في
`ar.json`: `IMS:Question:SectionNotInExam` («هذا **الجزء** يخصّ اختباراً آخر»)
و`Question:Section:Hint` («**الجزء** الذي ينتمي إليه هذا السؤال»)، بينما
`Results:Detail:BySection` («حسب **أقسام** الاختبار») و`Section:Title`
(«الأقسام») صحيحتان. **وحّدها كلّها على «القسم»** — وهو الموثَّق في حزمة Moodle
العربيّة (`addasection` = «عنوان قسم جديد»).

وحيث يلزم «عنوان القسم» فالمقابل الموثَّق **«رأس القسم»** لا «العنوان الرأسيّ».
**ولا تُستعمل «مقاطع»** رغم ورودها في Moodle: هي تناقضٌ في حزمتهم لا مصطلحٌ
مقصود.

**٢. أضف «جدول المواصفات» إلى عنوان شاشة المخطّط.** `Blueprint:Title` اليوم «شكل
الورقة» — وهي عبارةٌ سليمةٌ ومفهومة، لكنّ منسّق مركزٍ لغويٍّ عربيّ **يعرف «جدول
المواصفات» ولا يعرف «شكل الورقة»**، وهو المصطلح الوحيد في هذه الوثيقة الموثَّق
في مصدرٍ محكَّمٍ وفي منتجٍ وفي جامعةٍ معاً. اقترح: «شكل الورقة (جدول المواصفات)».

**٣. المصطلح الوحيد الموثَّق لوحدة القياس هو «فقرة».** أكاديميّة الملك سلمان تعدّ
بالفقرات لا بالأسئلة. وإن أُريد تمييزٌ بين الوحدة القياسيّة وما يراه المستخدم،
فالزوج المدعوم **فقرة** في النموذج و**سؤال** في الواجهة. وأمّا «مفردة» و«بند»
فقد سُحب إسنادهما ولم يُتحقَّق منهما، **فلا يُدخَلان الواجهة على أساس هذه الوثيقة**.

وعلى أيّ حال، وحتّى لو ثبتت لاحقاً، **«مفردة» خطرةٌ في هذا المنتج بالذات**: في
أدبيات الاختبارات «المفردات» اسمُ اختبارٍ فرعيٍّ للحصيلة اللغويّة، ومنتجٌ **لتقييم
اللغة** يستعمل «مفردة» بمعنى item سيصطدم بـ«مفردات» بمعنى vocabulary. وهذا خطرٌ
خاصٌّ بنا لا يشترك فيه منتجٌ عامّ.

**٤. «سؤال عشوائي» صفةً، لا «اختيار عشوائي» اسماً.** المُثبَت في حزمة Moodle
العربيّة هو النمط الوصفيّ. فإن أُضيفت شاشةٌ لقاعدةٍ ديناميّة، فعنوانها «أسئلة
عشوائية» أو «سحب عشوائي»، لا «اختيار عشوائي» — وهي عبارةٌ لم يُعثر عليها اسماً في
أيّ منتج.

---

## ٥ · التوصية | The recommendation

### ٥.١ الشكل

**نعم، يجب أن يستطيع البنك ملء قسم — بقاعدةٍ يملكها القسم، لا بعمودٍ على السؤال.
ويُبقى على مسار التصنيف الذي شُحن بالأمس كما هو، حرفاً بحرف.**

بعبارة Surpass: **يصير للقسم `Content rules` بقيمتين — ثابت وديناميّ.** والقسم
الثابت هو ما يعمل اليوم بالضبط. والديناميّ هو القاعدة.

ثلاث خطوات، مرتّبةً بالقيمة على الكلفة:

---

**الخطوة أ — «الاستماع من موضوع الاستماع». صفر كيان، صفر عمود، صفر هجرة.**

`ExamSection.TopicId` **موجودٌ بالفعل** ولا يُقرأ إلّا للعرض. و
`ExamSection.QuestionsPerForm` موجودٌ بالفعل. فليصر القسم الذي **لا قاعدة له ولا
سؤال مصنَّفاً فيه** قادراً على السحب من البنك القابل للسحب بموضوعه هو:

```
pool = filed.Any() ? filed
     : section.TopicId is {} t ? bank.Where(q => q.TopicId == t && q.ExamSectionId is null)
     : []
```

وهذا **يحلّ حالة العميل المذكورة في التكليف بالكامل**: «اسحب ١٠ استماع من البنك،
و١٠ قراءة». تغييرٌ واحدٌ في `DrawBySection`، ولا هجرة، ولا نموذج نقلٍ جديد،
و`ExamSection.TopicId` يصير له قارئٌ لأوّل مرّة.

الثمن: `TopicId` يؤدّي دورين — لافتةً في التقرير ومنتقياً في السحب. وهذا مقبول
لأنّ الدورين يتّفقان دائماً: القسم الذي يقيس الاستماع يسحب أسئلة الاستماع. لكن
لا مزيج صعوباتٍ فيه.

---

**الخطوة ب — أطلق القاعدة الموجودة. صفر هجرة أيضاً.**

`ExamBlueprintRule.ExamSectionId` في المجال وفي قاعدة البيانات
(`ModelSnapshot:934`) — **العمود مشحونٌ فعلاً**. المطلوب ثلاثة أشياء:

1. **اقلب ترتيب التضييق في `DrawBySection`.** حين يكون للقسم قواعد، شغّلها على
   البنك القابل للسحب **ناقص المأخوذ ناقص المصنَّف في قسمٍ آخر** — لا على البِركة
   المصنَّفة. حين لا قواعد له، لا يتغيّر شيء.
2. **احمل `ExamSectionId` في `BlueprintRuleDto` و`CreateUpdateBlueprintRuleDto`**،
   وأسقِطه في `GetBlueprintAsync`، واكتبه في `SetBlueprintAsync:379`.
3. **اختم القسم من القسم الساحب لا من السؤال.** أي `PaperSlot(Question, Score,
   Guid? SectionId)`، و`Project` يقرأ `slot.SectionId` بدل
   `row.Question.ExamSectionId`. ثلاثة مواضع استدعاء فقط
   (`ExamFormBuilder:76`, `ExamTakingAppService:1230`، والنداء الداخليّ).

**والنقطة الثالثة إصلاحٌ واجبٌ بذاته**، لأنّها تجعل الورقة المسلَّمة سجلّاً لما
حدث فعلاً — وهو ما تفترضه شاشات النتائج أصلاً حين تجمّع على
`AttemptQuestion.ExamSectionId`.

---

**الخطوة ج — قل الحقيقة على الشاشة (§٧). تُنفَّذ أوّلاً، لا أخيراً.**

---

### ٥.٢ ما لا يُفعل، ولماذا

**لا رابطة متعدّد-إلى-متعدّد بين السؤال والقسم (`QuestionExamSection`).** هذا
جواب «صنّفه في أقسامٍ كثيرة»، ولا يفعله أحد. سببه في الوثائق أعلاه: تعدّد
ExamSoft على **التصنيف** لأنّه مستقلٌّ عن الاختبار؛ والقسم ليس كذلك. وعمليّاً
ينمو الجدول بحاصل ضرب الأسئلة في الاختبارات، ويصير معنى سؤالٍ في البنك دالّةً على
كلّ اختبارٍ استعمله يوماً.

**ولا يُرخّى الحارس.** `RequireSectionBelongsToAsync` صحيحٌ ويبقى. تصنيف سؤال بنكٍ
في قسم اختبارٍ واحد يربط المشترك بورقةٍ واحدة، وهو ما يقوله تعليق الحارس نفسه.

---

## ٦ · الكلفة | The cost

| | أ (موضوع القسم) | ب (قاعدة للقسم) |
|---|---|---|
| **كيانات جديدة** | لا شيء | لا شيء |
| **هجرات** | **لا شيء** | **لا شيء** — العمود مشحون |
| **نماذج نقل** | لا شيء | حقلان: `BlueprintRuleDto.ExamSectionId`، `CreateUpdateBlueprintRuleDto.ExamSectionId` |
| **خدمة التطبيق** | لا شيء | إسقاطٌ وكتابةٌ في `GetBlueprintAsync` و`SetBlueprintAsync` |
| **البنّاء** | شرطٌ واحد في `DrawBySection` | قلب ترتيب التضييق + `PaperSlot.SectionId` (٣ مواضع استدعاء) |
| **شاشات** | لا شيء | قائمةُ «القسم» في صفّ القاعدة (`exam-blueprint.component`) — لا تحمل أيّ ذكرٍ للأقسام اليوم |
| **ترجمة** | لا شيء | مفتاحان: `Blueprint:Section`، `Blueprint:AnySection` |
| **اختبارات** | ٢–٣ | ٥–٦ |

**وعيبٌ قائمٌ يجب إصلاحه مع (ب) وإلّا صار كذباً أعلى صوتاً:** `AvailableCount` في
`ExamAppService:360` يُحسب على البنك كلّه بلا وعيٍ بالأقسام، وهو **مبالغٌ أصلاً**
في اختبارٍ مقسَّم. مع قاعدةٍ موجَّهةٍ إلى قسم يصير الرقم ذا معنىً حقيقيّ لأوّل مرّة،
لكن يجب أن يُحسب على البِركة نفسها التي ستسحب منها القاعدة.

**تحذيرٌ من Surpass يُنقَل حرفيّاً:** *"Friend and enemy relationships do not work
with dynamic rules."* المقابل هنا: **`DrawByRules` لا يعرف الكتل.** `Draw`
(المسار الحاليّ) يسحب بالكتل فلا تُشطر قطعة قراءة، أمّا `DrawByRules` فيسحب
بالسؤال. فقاعدةٌ موجَّهةٌ إلى قسمٍ فيه قطع قراءةٍ **ستشطرها**. هذا عيبٌ **قائمٌ
اليوم** في المسار غير المقسَّم، ولا تُحدثه هذه التوصية — لكنّها توسّع مداه، فيجب
إمّا إصلاح `DrawByRules` ليسحب بالكتل، وإمّا قول ذلك على الشاشة.

---

## ٧ · ما ينكسر، وما يُهاجَر | What breaks

**لا شيء ينكسر. ولا صفّ يُهاجَر.** وهذا ليس تفاؤلاً؛ هو نتيجة أنّ التوصية إضافيّة:

- **الاختبارات التي تستعمل التصنيف اليوم** لا قواعد موجَّهةً إلى أقسامها (لأنّه
  لا سبيل إلى صنعها)، فتسلك مسار «لا قواعد» وتُبنى بـ`Draw(pool,
  section.QuestionsPerForm)` تماماً كما تُبنى الآن.
- **`Question.ExamSectionId` يبقى ويبقى معناه**: «هذا السؤال، الذي يملكه هذا
  الاختبار، مكانه هذا الجزء». وهو الطريق الصحيح والأبسط للاختبار المكتوب مرّةً
  واحدةً بيده، وأغلب الاختبارات كذلك.
- **٢٢ اختباراً خضراء تبقى خضراء**: كلّها تضع `ExamSectionId` على السؤال ثمّ تؤكّد
  على `AttemptQuestion.ExamSectionId`. وحين يُختم القسم من القسم الساحب، يبقى
  الساحبُ هو المصنَّف فيه، فتتطابق النتيجة.
- **الأوراق المسلَّمة** مجمَّدةٌ على `AttemptQuestion`، فلا تتأثّر محاولةٌ سابقة.

**والخطر الحقيقيّ الوحيد ازدواجٌ في العدّ:** قسمٌ له قاعدةٌ **وأسئلةٌ مصنَّفةٌ فيه
معاً**. القرار المقترح: **القاعدة تسحب من البنك، والمصنَّف يُضاف إليها، و
`taken` يمنع التكرار — ولا يُطبَّق `QuestionsPerForm` فوقهما**، تماماً كما يرفض
`DrawBySection` اليوم تطبيق `QuestionsPerForm` الخاصّ بالاختبار فوق الأقسام،
وللسبب نفسه المكتوب في تعليقه: سقفٌ يقصّ ما طلبه المؤلّف بصمت. **ويُقال هذا على
الشاشة لا في تعليق.**

---

## ٨ · الجواب الأمين البديل: يُترك ويُقال | "Leave it, and say so"

**هل الجواب الأمين هو «اتركه كما هو»؟ لا — لكنّ نصفه نعم، ويجب أن يُنفَّذ اليوم
مهما تقرّر في الباقي.**

الحجّة للترك: العمود على السؤال يعمل، والحارس صحيح، والقاعدة كلفتها ليست صفراً.
والحجّة ضدّه أنّ الأقسام والبنك **أقوى ميزتين في هذا المنتج** (بشهادة
`business-review.md`)، وهما اليوم **متنافيتان**: من فعّل الأقسام خسر البنك، ومن
اعتمد البنك خسر الأقسام. وحالة العميل المذكورة في التكليف — «١٠ استماع من البنك،
١٠ قراءة» — هي **حالة مركز اللغة النموذجيّة**، لا حالةً هامشيّة. وثمن الخطوة (أ)
وحدها صفر هجرة وشرطٌ واحد.

**لكنّ الصمت الحاليّ غير مقبول بأيّ حال.** مؤلّف سؤال بنكٍ لا يرى القائمة، ولا
يرى سبب غيابها، ولا يُخبَر أنّ سؤاله لن يخدم أيّ قسمٍ في أيّ اختبارٍ مقسَّم. وهذا
المنتج **يملك سابقةً ممتازةً لقول هذا**: لافتة `Section:NotEnforced:*` على شاشة
الأقسام، التي تقول للمؤلّف بصراحةٍ أيّ الحقول تُحفظ ولا تُطبَّق
(`exam-structure.component.html:45‑47`).

### ما يجب أن تقوله الشاشة

**١. في استمارة السؤال، حين يكون السؤال في البنك** — بدل غياب القائمة، تُعرض
القائمةُ معطّلةً مع سببها. مفاتيح جديدة:

> `Question:Section:BankQuestion` — **ar:** «سؤال البنك لا يُصنَّف في قسم. القسم جزءٌ
> من اختبارٍ بعينه، وهذا السؤال تسحبه كلّ اختبارات هذا التصنيف والمستوى. ولكي يصل
> إلى قسمٍ ما، أعطِ ذلك القسم موضوعاً يطابق موضوع هذا السؤال.»
> **en:** "A bank question is not filed into a section. A section belongs to one
> exam, and this question is drawn by every exam at this category and level. To
> get it into a section, give that section a topic matching this question's."

الجملة الأخيرة تصير صحيحةً بعد الخطوة (أ). **وقبلها يجب أن تُحذف**، وتُستبدل بـ:
«ولا يستطيع اختبارٌ مقسَّمٌ أن يسحبه إلى قسم بعد.»

**٢. على شاشة الأقسام** — سطرٌ إلى جوار عدّاد «{0} متاح»، حين يكون في البنك أسئلةٌ
قابلةٌ للسحب لا يعدّها أيّ قسم:

> `Section:BankNotCounted` — **ar:** «{0} سؤالاً في البنك يستطيع هذا الاختبار سحبها،
> ولا يعدّها أيّ قسم. الأقسام تُملأ من أسئلة هذا الاختبار وحدها، وما بقي من البنك
> يُلحق بآخر الورقة غير مصنَّف.»
> **en:** "{0} bank questions this exam can draw are counted by no section.
> Sections are filled from this exam's own questions; the rest of the bank is
> appended unfiled at the end of the paper."

**وهذه الجملة الثانية صحيحةٌ حرفيّاً اليوم** — انظر ذيل `DrawBySection:197‑232` —
ولم تُقَل لأحدٍ قطّ. وهي على الأرجح أنفع سطرٍ في هذه الوثيقة كلّها، لأنّ مؤلّفاً
يقرؤها يفهم فوراً لماذا خرجت ورقته بما لم يتوقّع.

**٣. على شاشة المخطّط** — بعد الخطوة (ب)، تُضاف قائمةُ «القسم» في صفّ القاعدة،
وتُعاد صياغة `Blueprint:CannotFill` لتقول من أيّ بِركةٍ عُدَّ الرقم.

---

## ٩ · ما لم يُتحقَّق منه | Not verified

يُقال صراحةً، ولا يُبنى عليه:

**ادّعاءاتٌ سُحبت أثناء إعداد هذه الوثيقة** — تُسجَّل لأنّ سحبها جزءٌ من نتيجتها:

1. **مسرد المجلس الأعلى للجامعات المصريّ** (تفريق «بنك الأسئلة = Item Bank» عن
   «مستودع الأسئلة = Item Pool»، والثاني غير مُعايَرٍ بـIRT) — **سُحب**؛ الصفحة
   ردّت 404 ولم تُقرأ. لو صحّ لكان مفيداً؛ يستحقّ تحقّقاً بشريّاً.
2. **amp-up.io** «مسلِّمٌ لا مؤلِّف» — **سُحب**؛ لم يُجلب موقعهم أصلاً.
3. **«مفردة» و«بند»** لـitem — **سُحب إسنادهما**؛ من ملخّصات بحثٍ لا من مصدرٍ
   مقروء.
4. وفي إحدى قراءات صفحة Qorrect العربيّة **أنتج المُلخِّص عبارةً ليست على الصفحة**.
   وهذا سببٌ كافٍ لأن **يتحقّق إنسانٌ من كلّ مصطلحٍ عربيٍّ قبل نقله إلى `ar.json`**.
   (سلاسل Moodle العربيّة في §٤٫٥ استثناء: جُلب ملفّها الخام وقُرئ حرفيّاً.)

**ما لم يُتحقَّق منه أصلاً:**

5. **Questionmark**: كلّ ما ورد **[مقتطف]** أو **[ثانويّ]**؛ النطاقات كلّها 403.
   وتحديداً **لم يُتحقَّق** هل السؤال في موضوعٍ واحدٍ أم عدّة.
6. **ExamSoft**: كلّه **[مقتطف]** لصفحاتٍ رسميّةٍ مسمّاة. والآليّة الدقيقة
   (أيُسحب لكلّ ممتحَنٍ عشوائيّاً أم مرّةً واحدةً عند البناء؟) **لم تُتحقَّق**.
7. **Qorrect**: أسماء الميزات موثّقة، **وآليّتها لا**. وتعريفهم لـ«متعدّد الأقسام»
   بأنّه عرضُ سؤالٍ واحدٍ في الشاشة **[مقتطف]** لا **[موثَّق]** (الصفحة ترد 404).
8. **SwiftAssess**: **كلّ عناوينها ترد 403.** الموثَّق عنوانا صفحتين لا أكثر. وكلّ
   ما يُقال عن مخطّطاتها وLOFT و«أقسامها المؤقّتة» **غير مؤكّد**. تحتاج عرضاً حيّاً.
9. **هيئة تقويم التعليم والتدريب (قياس)**: `etec.gov.sa` **غير قابلٍ للوصول من هذه
   البيئة** (رفض اتّصالٍ مؤكّد). ومصطلحاتها المنشورة هي المرجع العربيّ الأوثق لو
   أمكن بلوغه — **يحتاج شبكةً أخرى**. وهذا العائق نفسه سُجّل في
   `research-2026-08-competitors.md`.
10. **Edaà / إدارة** — **لم يُعثر عليه** تحت أيّ هجاء. **المنهل** يردّ 403.
    **تطوير** لا شيء. **نور** نظام معلومات طلّابٍ لا أداة تأليف.
11. **ClassMarker**: هل يقع السؤال في أكثر من تصنيف؟ **لم يُحسم.**
12. **Mettl**: كيف تُمثَّل الأسئلة المنتقاة يدويّاً (`Select Question`) تحت
    البنية القائمة على القواعد؟ **لم يُتحقَّق.**
13. **TAO**: دعاية `taotesting.com` عن «اختيارٍ مدفوعٍ بالبيانات الوصفيّة» **لم
    يُعثر لها على مقابلٍ في دليل المستخدم**، ولا يُبنى عليها.
14. **Cirrus**: **لم توثّق** الوثائق أين تُخزَّن العضويّة (على النموذج أم على
    السؤال)؛ القول بأنّها على النموذج **استنتاج**، لا اقتباس.
15. **Moodle**: مستويات السياق الأربعة **مستنبطةٌ** من
    `question_categories.contextid` ونظام السياقات العامّ، لا مقتبسةٌ من صفحة بنك
    الأسئلة. ولم تُراجَع ملاحظات إصدار ٤٫٣ لتثبيت الإصدار الذي أدخل المرشّحات —
    تُحُقّق من **الآليّة** في مصدر ٥٫٠ بدلاً من ذلك، وهو أقوى دليلاً وأضعف تأريخاً.
16. **حزمة Moodle العربيّة** من **مرآة** `projectestac/moodle-langpacks` لا من
    `lang.moodle.org` (خلف تسجيل دخول). **ولا توثيق لـMoodle بالعربيّة أصلاً.**
17. **«محور/محاور»** لقسم الاختبار: في ويكيبيديا وتغطيةٍ صحفيّةٍ فقط، **وليس** على
    صفحة همزة الرسميّة. **لا يُستعمل على هذا الأساس.**
