# User stories

This replaces the backlog written when the product was pre-employment screening
for programmers. That document had four personas, no acceptance criteria, and no
story for the shared bank, weighted scoring, branding, blueprints, item health,
sections, named forms or the taking experience — which is most of what now exists.

## How to read this

**Actors are the tenant's words, not ours.** A recruiter, a training coordinator,
a teacher, a reviewer, a candidate, an administrator. `CategorySet` lets a tenant
rename these in its own UI; the backlog uses the commonest word for each role
rather than a platform term.

**Status is claimed against the code, not against intent.** Verified by reading
`src/InternshipManagementSystem.Domain/Assessment/`, the Application layer, the
EF configuration, `src/InternshipManagementSystem.HttpApi/` and
`angular/src/app/`. Where a claim was surprising, the file is named.

**Pinned to `75b534d`, and a warning about that.** The previous revision was pinned
to `0842cc9`; twenty-one commits landed between the two and closed eleven stories —
`ASG-03` (a branded invitation), `ASG-06` (reissue a link), `ASG-07` (extend an
expiry), `ASG-08` (end a sitting, now committed), `ADM-01` (five real roles),
`ADM-02` (the permission that enforced nothing, removed), `ADM-05` (the password
that answered 200 and changed nothing), `GRD-10` (an answer graded in the shape the
browser sent it), `RES-06` and `RES-07` (statistics that stop libelling correct
questions), and `TAK-05` (submit no longer races an in-flight save). Two stories
are new: `IMP-06` and `PLT-11`. **A status document for this repository has a shelf
life measured in hours.** Worth knowing before acting on any single row below; the
shape of the tables is more durable than the rows.

**Every story is traceable.** Each carries an identifier, a status, a priority and
an actor. The **screen** each one belongs to and the **role** that holds it are set
out for all of them in the traceability matrix at the end of this document — one
row per story, so a reader can go from a story to the route a person stands on and
the permission that lets them stand there. The five roles are `Admin`,
`Coordinator`, `Author`, `Marker` and `Observer`, defined in `business/roles.md`;
the candidate is deliberately not a role, because they have no account.

كلّ قصّة لها مُعرِّف وحالة وأولويّة وصاحب، **ولها شاشة ودور** في مصفوفة التتبّع في
آخر هذه الوثيقة.

| Status | Means |
|---|---|
| **BUILT** | The actor can complete this today, end to end, in the running product — a screen, on a registered route, calling a method that reaches a controller that reaches a service that does the work |
| **PARTIAL** | Real working code exists at some layers — a service, an API, an enforced domain rule with tests, sometimes even a screen — but the actor cannot complete the story. Each one names which half is missing |
| **NOT BUILT** | Nothing, or at most an entity and a column |

**The rule this revision applies most strictly.** A service is not a feature.
This project has four times shipped a finished, tested, permission-checked
application service with no controller and no screen, and each time it read as
done in an inventory that counted services. **A story is BUILT only if a person
can complete it from a browser.**

The failure has now inverted, and the new shape is worth naming because it is
harder to see: a control the author can set and save that **no mechanism ever
reads**. A section's time limit, a qualifying-section flag, a brand colour and a
staff password are all editable today, all persist, and all do nothing. Those are
marked PARTIAL rather than NOT BUILT, because the software is making a promise in
writing that it does not keep — which is worse than an absent feature, not
better. `EXM-07`, `EXM-09`, `BRD-02` and `ADM-05` are that shape.

**Priority** is MUST / SHOULD / COULD against the *first sellable release*
described in `docs/business/business-review.md` §3 — the Level Exam Pack for a
training academy. A COULD here is not unimportant; it is not in that release.

**Test layers.** The team's rule is that every story is covered from unit through
to end-to-end, and this document is where that is planned. Each story names the
layers it needs and why. Not every story needs all three — a pure domain
invariant does not need a browser, and a layout rule does not need a unit test —
and where a layer is omitted the reason is stated.

- **unit** — a class in isolation: a grader, a validator, an entity rule, a
  projector, a parser.
- **integration** — the application service against a real database, with
  permissions and the tenant filter on.
- **e2e** — Playwright, in a browser, in Arabic. Note that the `desktop` and
  `mobile` projects stub HTTP; `angular/e2e/live/journey.spec.ts` is the only
  suite that reaches a real backend, and it drives the API rather than the
  screens — which is why it can prove a blob round-trips and still not notice
  that no `<img>` in the product can fetch one.

---

## The authoring constraint

The product owner set one rule that cuts across every story in this document:

> **No input anywhere may require programming skill — not to write a question,
> and not to answer one.**

That rules out raw JSON, regular expressions, template placeholders, HTML
fragments, coordinate arithmetic and any syntax an author has to learn. It is
stated here once, as a testable rule, and referenced by the stories that are most
likely to break it.

**PLT-01** below is the enforcing story. Stories flagged **⚠ constraint** are ones
whose obvious implementation would violate it; each carries acceptance criteria
that close the specific hole.

**The authoring half of that rule is now kept.** Thirteen types, thirteen editors,
no raw box for any shipped type. That is done and it is good.

**The answering half is not, and it is worse than it looks.** Three of the
thirteen types have no answer control and fall back to a plain textarea, and a
fourth — `fill-in-the-blank` — is wired to a textarea that emits a bare string
while its grader reads a keyed dictionary, so the answer scores zero however
right it is. A candidate can only score on that type by typing JSON into an exam.
That is the constraint broken at the sharpest possible point. See `GRD-10`.

One exception, so it is not mistaken for a breach: the `code` question type asks
an author to write code. That is the subject matter, not the tool. The rule is
that the *product* never demands syntax, not that a programming exam can avoid
programming.

---

# Epic 1 — The catalogue and the tenant's vocabulary

*Catalog context. This epic went from nothing to almost everything in one commit.
`CatalogAppService`, `CatalogController` at `api/assessment/catalog`, and a real
screen at `/catalog` carrying categories, levels, a topic tree and a vocabulary
dialog. The unblocking is the important part: four features — the shared bank,
topic reporting, blueprints keyed on competency, and a class at a level — were
all dark for want of this one CRUD screen, and are now lit.*

*What remains is that the vocabulary is saved and read back by nobody, and that a
new tenant still starts with an empty catalogue and therefore no way to write a
bank question on its first day.*

#### CAT-01 · Name the vocabulary
**MUST · PARTIAL** — *the editor is built; nothing renders the result*

As an **administrator**, I want to set what we call our category axis, the people
we assess and the groups we assess them in, so that our staff and our students
read our words instead of the platform's.

**Acceptance**
1. Saving singular and plural labels for the axis, the subject and the group
   updates `CategorySet` for the tenant and no other tenant's row changes.
   *(Built — the vocabulary dialog on `/catalog` posts to `PUT
   api/assessment/catalog/vocabulary` and reaches
   `CatalogAppService.UpdateVocabularyAsync`.)*
2. Every screen that displays the axis renders the saved singular or plural label;
   with the labels set to "Language"/"Languages", no screen shows the word
   "Category". *(Not built. A grep of `angular/src` for `getVocabulary` returns
   hits only inside the catalogue feature itself. The sidebar still reads
   `::Nav:Candidates` and `::Nav:Groups` from `core/navigation.ts`, which resolve
   through the static localisation files and cannot know the tenant's words.)*
3. A tenant that has never configured this sees the defaults `Candidate` /
   `Candidates` / `Group` / `Groups` from the entity, not an empty label.
4. Exactly one `CategorySet` row exists per tenant; a second save updates the
   first rather than inserting. *(Built.)*

**Tests** — *integration*: one row per tenant, tenant isolation, defaults on
first read. *e2e*: rename to "Language"/"Student"/"Class", then assert those words
appear in the sidebar, the exam editor and the candidate screen in both Arabic and
English. That end-to-end assertion is the entire story and does not exist.

*"The tenant renames the vocabulary" is one of the four differentiators
`competitive-position.md` claims. It is now half true rather than false: a tenant
can type its words in and nothing shows them.*

#### CAT-02 · Manage categories
**MUST · BUILT** · ⚠ constraint

As a **training coordinator**, I want to create the domains we assess — our
tracks, languages or job roles — so that exams, questions and people can be filed
under something meaningful.

**Acceptance**
1. A category created with a name only is saved, and its `Code` is generated from
   the name; the author is never required to supply a code. *(Built — the screen
   suggests a code as a placeholder and substitutes it on save, so the field is
   visible and never mandatory. This satisfies the authoring constraint.)*
2. A generated code that collides with an existing one in the same tenant is
   suffixed automatically rather than rejected.
3. Saving a code by hand that already exists in the tenant fails with
   `IMS:Catalog:CodeAlreadyExists`, and the message names the category holding it.
4. `IsActive` false removes the category from every picker but leaves existing
   exams, questions and candidates referencing it intact and readable. *(Built —
   the service filters on `IsActive` unless asked otherwise, and both the question
   form and the question list call it with the default.)*
5. Categories are returned ordered by `DisplayOrder`, then name.

**Tests** — *unit*: code generation and collision suffixing. *integration*:
uniqueness per tenant, deactivation leaving references resolvable, permission
`Assessment.Catalog.Manage` required — `CatalogTests` covers the shape; the
permission assertion cannot run at all while the test base allows everything, see
PLT-07. *e2e*: create, reorder, deactivate; confirm a deactivated category is
absent from the exam editor's picker but still shown on an exam already using it.

#### CAT-03 · Manage levels within a category
**MUST · BUILT**

As a **training coordinator**, I want levels to belong to a category, so that a
centre teaching both English and welding is never offered "B1" under "Welding".

**Acceptance**
1. A level saved with a `CategoryId` appears only in pickers filtered to that
   category. *(Built — levels are nested under their category in the read model,
   and the question form narrows to the selected category.)*
2. A level saved with a null `CategoryId` appears under every category. *(Built —
   the form's "applies everywhere" tick.)*
3. Changing an exam's category clears a level selection scoped to the old
   category, and says so, rather than silently keeping an impossible pair.
   *(Clearing is built — `setCategory()` resets level and topic together. It does
   not say so, which is a one-line message away.)*
4. Levels are ordered by `DisplayOrder` ascending, which carries the ranking the
   names imply.

**Tests** — *integration*: scoped and unscoped filtering, the clearing rule.
*e2e*: with two categories configured, open the exam editor, switch category, and
assert the level picker's contents change and an invalid selection is cleared.

#### CAT-04 · Manage the competency tree
**MUST · PARTIAL** — *the screen is built; two of the four rules are not enforced*

As a **teacher**, I want to record the competencies my questions measure, as a
two-level tree, so that a result reads "listening 40%, reading 85%" instead of
"62%".

**Acceptance**
1. A topic may have a parent; a parent may not itself have a parent, and an
   attempt to nest three deep is refused with a named error. *(Not enforced.
   `CreateTopicAsync` and `UpdateTopicAsync` accept any `ParentId`, the parent
   picker offers every topic in the category, and the renderer merely stops
   drawing past four levels — so a five-deep tree is creatable and then partly
   invisible, which is the worst of both.)*
2. A topic cannot be its own parent, and a cycle of two is refused. *(Built.)*
3. Topics are scoped by `CategoryId` on the same rule as levels (CAT-03). *(Built.)*
4. Deleting a topic that questions reference is refused; deactivating it is
   offered instead, and the refusal names how many questions reference it.
   *(Half built. `IMS:Catalog:TopicInUse` is raised and reads "Questions are filed
   under this topic. Deactivate it instead." — with no number. Children are
   promoted rather than cascaded, which is the right choice and was not obvious.)*

**Tests** — *unit*: depth and cycle rules — the depth rule needs writing before it
can be tested. *integration*: referential refusal with the count in the message.
*e2e*: build a two-level tree, assign one to a question, attempt deletion, see the
count.

#### CAT-05 · See a catalogue value's usage before changing it
**SHOULD · PARTIAL** — *categories carry counts; levels and topics do not*

As a **training coordinator**, I want to see how many exams, questions and people
use a catalogue value, so that I do not deactivate something an exam is standing
on.

**Acceptance**
1. Each catalogue row shows counts of referencing exams, questions and candidates.
   *(Categories only. `CategoryDto` carries `ExamCount` and `QuestionCount` and the
   screen renders both, twice. `LevelDto` and `TopicDto` carry no usage field at
   all, so deactivating a level or a topic is still blind — and the topic is the
   one whose deactivation silently empties a result profile.)*
2. Counts are per tenant and exclude soft-deleted rows.
3. Deactivating a value with a non-zero count asks for confirmation naming the
   counts. *(Not built.)*

**Tests** — *integration*: counts correct across all three reference types, and
zero for another tenant's rows. *e2e*: the confirmation dialog shows the counts.

#### CAT-06 · Seed a new tenant with a usable starting catalogue
**COULD · NOT BUILT**

As an **administrator**, I want a new tenant to start with a small sensible
catalogue I can rename, so that the first exam does not require an hour of setup
before a question can be written.

**Acceptance**
1. Creating a tenant creates one `CategorySet` with defaults and at least one
   category and level.
2. Every seeded row is editable and deletable — nothing is marked as a system row.
3. Seeding runs once; re-running the seeder does not duplicate.

**Tests** — *integration*: seed once, assert idempotence on a second run.

*`InternshipManagementSystemDataSeedContributor` creates roles, users and
permissions and touches no catalogue entity. A new tenant's first act is
therefore to build a taxonomy before it can write one bank question — which is
the onboarding cost `business-review-2.md` §6 argues decides the unit economics.
This is a COULD by priority and a MUST by margin.*

---

# Epic 2 — The question bank

*Authoring context, and the strongest part of the product. Thirteen types with
thirteen editors, category/level/topic filing, a bank that genuinely reaches a
paper, and passages carrying several questions. The one thing still broken here
is the one thing a demo shows first: media.*

#### BNK-01 · Write a question of any shipped type
**MUST · BUILT** · ⚠ constraint

As a **teacher**, I want to write any of the question types the product supports
using controls, not code, so that writing an exam needs no training.

**Acceptance**
1. Every type in `QuestionTypes` resolves to a registered payload editor; the
   raw-payload textarea renders for none of them. *(Built —
   `features/questions/payload/payload-editor.ts` registers 13 keys across 9
   components, matching the 13 constants in `QuestionTypes` exactly.)*
2. The raw textarea is gated on `hasEditor()` being false, so it can only appear
   for a type served by a newer server than this client — which is correct
   behaviour, not a gap.
3. A type the build has never seen — a tenant-specific one — still saves through
   the raw field, and is reported as human-graded rather than refused. *(Built.)*
4. Switching type on an unsaved question warns before discarding a payload that
   has content. *(Not built — a remaining scratch, and cheap.)*

**Tests** — *unit*: a test enumerates `QuestionTypes` and asserts a registered
editor for each. *e2e*: `question-form.spec.ts` loops all 13 and asserts zero raw
payload fields.

*The answering side of the same rule is a separate story and is not done: see
`PLT-01`, `TAK-08` and `GRD-10`.*

#### BNK-02 · Refuse a question that cannot be graded
**MUST · BUILT**

As a **teacher**, I want to be told what is wrong with a question before I save
it, so that a broken key is caught by me rather than by forty candidates.

**Acceptance**
1. A choice question with no `IsCorrect` option reports `NoCorrectOption`.
2. A weighted question with an option missing a weight reports `WeightMissing`;
   one with a weight outside the allowed range reports `WeightOutOfRange`.
3. A weighted multi-select whose credited weights do not sum to full marks reports
   `WeightsDoNotSumToOne`; one where selecting every option reaches full marks
   reports `SelectingEverythingScoresFull`.
4. A rubric criterion with no score reports `RubricCriterionNeedsScore`; two
   criteria with the same name report `DuplicateRubricCriterion`.
5. Every error is a code that resolves to a localised sentence in Arabic and
   English — never a raw code shown to the author. *(Now enforced by
   `ErrorCodeCoverageTests`, which scans every `IMS:` literal in `src/` and
   asserts both language files carry it and carry identical key sets. The two
   `ExamForm` codes that were missing are present.)*

**Tests** — *unit*: `QuestionPayloadValidatorTests`. *e2e*: `question-form.spec.ts`
shows the message beside the offending control, in Arabic.

*Validation runs at save rather than at publish, because a question has no publish
step — see BNK-11. A dry-run route exists at `POST questions/validate-payload` and
has no Angular caller.*

#### BNK-03 · Format a prompt without writing HTML
**MUST · BUILT**

As a **teacher**, I want to bold a word or add a list in a question, so that a
prompt reads properly without my knowing any markup.

**Acceptance**
1. Formatting is applied by toolbar buttons; no field accepts typed markup.
2. Anything the sanitiser rejects — script tags, event handlers, `javascript:`
   URLs, style attributes — is stripped before storage, and the stripped value is
   what is stored, not merely what is displayed. *(Built — sanitised on the client
   into an inert document, and again server-side in `QuestionAppService` before it
   reaches the entity.)*
3. Arabic text keeps its direction and its letter joining after a formatting
   operation.

**Tests** — *unit*: `RichTextSanitiserTests`, 13 cases. *integration*: the
persisted column contains the sanitised value. *e2e*: bold an Arabic word and
assert the rendering, in RTL.

#### BNK-04 · Attach a chart, a recording or a clip to a question
**MUST · BUILT** — *fixed in `3923129`, after this revision first recorded it as PARTIAL*

As a **teacher**, I want to attach an image, audio or video to a question, so that
a question about a candlestick chart can show the chart.

**Acceptance**
1. A file is attached by clicking or by dropping; no URL is typed and no path is
   entered. *(Built.)*
2. An oversized file is refused with `IMS:File:TooLarge` and a disallowed type
   with `IMS:File:TypeNotAllowed`; both messages name the limit or the allowed
   types. *(Built.)*
3. Blob names are generated; a caller-supplied name never reaches the container,
   and traversal is rejected on read and on delete alike. *(Built.)*
4. The upload reaches a route that exists. *(Built at last —
   `AssessmentMediaController` at `api/assessment/media`, with a `{**blobName}`
   catch-all because real blob names contain a slash. The same work also wired the
   BLOB provider that had never been configured, so every read and every write had
   been throwing at container activation. Neither defect was visible to 187 green
   tests; a route smoke tool found both on its first run — see PLT-10.)*
5. The attached file previews in place. *(Built — and this was PARTIAL for most of
   this revision's life. Five places put a bare `/api/assessment/media/…` into an
   `src` attribute, and the app and the API are different origins with no proxy in
   either environment file, so every one resolved against the wrong server. Staff
   had a second, independent failure: a browser will not attach a bearer token to
   an `img src` however much the page would like it to.)*
6. The media reaches the candidate through a URL the projector builds, and the
   blob name itself is not required to be guessable. *(Built —
   `BuildMediaUrl` appends a signed grant naming one blob and expiring five
   minutes after the attempt's deadline, so a candidate with no account fetches
   exactly their own paper's media and nothing else.)*
7. Both kinds of caller are served correctly, and they need different answers.
   *(Built — `core/media.service.ts`. A candidate's paper already carries its
   grant, so it needs only the right origin; staff are signed in, so their files
   are fetched with the token and handed to the page as object URLs.)*

**Tests** — *unit*: the container name is a constant, not caller input.
*integration*: upload requires `Assessment.Questions.Edit`; size and type refusals;
`MediaGrantTests` covers the grant. *e2e*: `live/journey.spec.ts` now covers the
media round trip, and that an anonymous stranger holding a blob name gets 404
rather than the file.

*Recorded here because of how it survived two reviews rather than because it is
still broken: `question-form.spec.ts` stubbed this exact URL, so it asserted our own
mock was reachable, and the first live test fetched the blob with an API client
carrying a token, which no `img` tag can do. Both sides passed and neither crossed
the seam. See `PLT-09`.*

#### BNK-05 · Score an answer by how right it is
**MUST · BUILT**

As a **teacher**, I want an option to be worth part of the marks, or to cost
marks, so that "close the position" and "add to the losing position" are not both
simply wrong.

**Acceptance**
1. Weighted mode is switched on explicitly; a stray weight on an unweighted
   question changes no score.
2. On a single choice, the best answer scores full marks, an acceptable option
   scores its share, and a harmful option scores zero rather than a negative.
3. On a multi-select, the credited set and only the credited set reaches full
   marks; selecting every option does not.
4. A question never scores below what leaving it blank would score.
5. An unweighted question's stored JSON is byte-for-byte unchanged by this
   feature existing.
6. `IsCorrect` on a weighted answer means "reached full marks", so item statistics
   keep one definition.

**Tests** — *unit*: `WeightedChoiceGraderTests` (14) and `MultiSelectGraderTests`
(7). *e2e*: author a weighted question, confirm the four buckets render for the
reviewer.

#### BNK-06 · Own a question at the level rather than at one exam
**MUST · BUILT**

As a **training coordinator**, I want a question to belong to a domain and level
rather than to one exam, so that three forms for A1 cost three blueprints instead
of three copies of forty questions.

**Acceptance**
1. A question saved with no `ExamId` and no `CategoryId` is refused with
   `IMS:Question:BelongsNowhere`. *(Built, and mirrored on the client by
   `needsCategory`, so the author is stopped before the round trip.)*
2. Listing an exam's questions returns its own questions plus bank questions whose
   category matches and whose level is null or equal. *(Built.)*
3. A bank question corrected once is corrected in every exam that draws it; no
   copy exists to drift.
4. `BankOnly` filtering returns only questions with a null `ExamId`.
5. **There is a way to write one.** *(Built — `/questions/new` is registered with
   no `examId`, and the form carries category, level and topic selects. This was
   the missing half. Without it, every question had `CategoryId == null`,
   `DrawableBy` collapsed to "this exam's own questions", and the shared bank was
   a schema with five passing tests and no door.)*

**Tests** — *unit*: `Question.IsDrawableBy` across the matrix of exam / category /
level / active. *integration*: the widened list and the `BelongsNowhere` refusal.
*e2e*: a bank question appears in two exams' question lists and is edited once.

#### BNK-07 · Actually draw the bank into a candidate's paper
**MUST · BUILT**

As a **candidate**, I want the paper I sit to contain the bank questions my exam
is entitled to draw, so that the exam my coordinator sees in the editor is the
exam I take.

**Acceptance**
1. `ExamTakingAppService.StartAsync` selects the bank through `DrawableBy`, not
   `q.ExamId == exam.Id`. *(Built.)*
2. `ExamAppService.CheckPublishAsync` counts the same widened set, so the publish
   gate and the form builder never disagree about how many questions exist.
   *(Built.)*
3. An exam whose questions all live in the bank publishes and produces a full
   paper. *(Built, and reachable now that BNK-06 has a screen — the two together
   are what turned "correct in code, unreachable through the product" into a
   feature.)*
4. `Question.TimesServed` is incremented once per question per attempt started,
   and not on a validity check or a preview. *(Built — `RecordExposureAsync`, in
   one batched update, at assembly rather than at grading, because a candidate who
   skips a question has still read it.)*

**Tests** — *unit*: the builder given a mixed bank draws both kinds. *integration*:
start an attempt on a bank-only exam and assert the form length and the
`TimesServed` increments. *e2e*: author a bank question, assign, sit, and see it.

#### BNK-08 · Bind several questions to one passage, chart or recording
**MUST · BUILT**

As a **teacher**, I want to show one reading passage or play one recording and ask
six questions about it, so that the stimulus is not repeated six times and the
result can say how well the student read *that* passage.

**Acceptance**
1. A group carries instructions and a stimulus that is text, image, audio or
   video. *(Built end to end — authored on `/exams/:examId/structure`, saved
   through `POST api/assessment/questions/groups`.)*
2. A question is put on a passage from the question form's group picker. *(Built.)*
3. A group's questions stay together and in their authored order when the exam
   shuffles. *(Built — `ExamFormBuilder.ApplyOrdering`.)*
4. The stimulus renders once above its questions in the taker, and an audio
   stimulus is not restarted by moving between the questions on it. *(Built —
   including the media, since `3923129`. A listening passage was a dead player for
   as long as BNK-04 was broken, which is the whole reason a language centre would
   buy this.)*
5. A group with no questions cannot be saved, and the reason names the group.

**Tests** — *unit*: ordering keeps blocks intact under shuffle, with a fixed seed.
*integration*: group creation, stimulus media, empty-group refusal. *e2e*: author a
passage with three questions; sit it; assert the passage appears once and the
audio does not reset.

#### BNK-09 · Find a question in a bank of hundreds
**MUST · PARTIAL** — *the screen is built; the two filters a bank needs are absent*

As a **teacher**, I want to filter the bank by domain, level, competency, type and
difficulty, so that I can find what already exists instead of writing it twice.

**Acceptance**
1. Filters combine; each narrows the result. *(Built for free text, type,
   difficulty, category and level.)*
2. A level filter also returns questions with no level, because those suit every
   level in the domain. *(Built.)*
3. There is a screen. *(Built — `/questions` and `/exams/:examId/questions`, and a
   row opens its editor.)*
4. **A topic filter.** *(Not built in the UI. `QuestionAppService` honours
   `input.TopicId`; the list component never sends one. Filing by competency is
   the entire point of CAT-04, and the bank cannot be searched by it — so the
   teacher who wants "my listening questions" cannot ask for them.)*
5. **A status filter.** *(Not built, and cannot be until BNK-11 gives a question a
   status.)*
6. Each row shows type, difficulty, competency and marks without opening it, with
   an item-health chip.

**Tests** — *integration*: each filter and their combination; the null-level rule.
*e2e*: filter to one competency, open a question, edit it, return to the same
filter.

#### BNK-10 · Duplicate a question
**COULD · NOT BUILT**

As a **teacher**, I want to copy a question and change one thing, so that writing
a parallel item is not retyping.

**Acceptance**
1. The copy carries the payload, competency, difficulty and marks; it does not
   carry the original's item statistics or `TimesServed`.
2. The copy is created as inactive, so it cannot reach a candidate before it is
   reviewed.

**Tests** — *integration*: statistics are not copied, the copy is inactive.
*e2e*: duplicate, edit, activate.

#### BNK-11 · Control a question's life cycle
**SHOULD · NOT BUILT**

As a **reviewer**, I want a question to move from draft to approved to retired,
so that nothing reaches a candidate that nobody has read.

**Acceptance**
1. Only approved questions are eligible for blueprint selection and for a form.
2. A state change records who made it and when.
3. A retired question stays readable by results that reference it.
4. Editing the key, the options or the scoring of a question with accumulated
   statistics warns that the statistics describe a question that will no longer
   exist, names the number of responses affected, and requires confirmation.

**Tests** — *unit*: eligibility by state; what counts as a material change.
*integration*: the audit record; retired questions still resolve from results.
*e2e*: the confirmation dialog with the response count.

*`Question` has no `Status` member at all. The only lifecycle control is the
boolean `IsActive` — two states, no transitions, no audit. The whole "approve the
exact paper before it goes out" story is carried by `ExamForm` instead, which
works but reviews a paper rather than an item.*

#### BNK-12 · Keep item statistics bound to what was actually asked
**SHOULD · NOT BUILT**

As a **reviewer**, I want statistics to belong to the version of the question they
were gathered on, so that correcting a wrong key does not leave a difficulty index
that describes a question nobody sat.

**Acceptance**
1. A material change creates a new version; the previous version keeps its
   statistics.
2. A form binds to a question version, so a question edited mid-administration
   does not change under the candidates sitting it.
3. A result rendered months later shows the version that was served.
4. At minimum, and far cheaper than versioning: editing a key resets
   `TimesAnswered` and `DifficultyIndex` to null rather than carrying them.

**Tests** — *unit*: material-change classification. *integration*: an edit during a
live attempt does not change that attempt's paper. *e2e*: not required — no user
journey exercises this beyond BNK-11's dialog.

*This has moved from latent to accumulating. `AttemptGradingService.RecordOutcome`
maintains a lifetime running mean in `Question.DifficultyIndex`, and
`QuestionAppService.Apply` rewrites `Payload`, `Text`, `Score` and `Type` without
touching it. An author who fixes a wrong key keeps the wrong key's difficulty, and
`RES-07` then reports it to them as fact. Criterion 4 is a two-line fix and should
not wait for versioning.*

---
# Epic 3 — Getting existing exams in

*Still the emptiest epic, and the one `business-review-2.md` §5 calls the highest
leverage per developer-day in the backlog. The candidate roll importer shipped and
is good. The question importer — the one that decides whether a trial survives its
second week — has not been started.*

*What did change is that the destination now exists: IMP-04 has real category,
level and topic pickers to write into, which it did not before.*

#### IMP-01 · Paste an exam in
**MUST · NOT BUILT** · ⚠ constraint

As a **training coordinator**, I want to paste my existing exam as text and have
the questions recognised, so that switching does not mean retyping thirty
questions.

**Acceptance**
1. A block of text with numbered prompts and unindented option lines produces one
   draft question per prompt, with its options in order.
2. Given the reference file — thirty questions, 86 options, two to five each — all
   thirty are recognised with their options intact.
3. A line marked with the Google Forms correct-answer tick is proposed as the key;
   exactly one key per question in the reference file.
4. Nothing is imported until the author confirms; the proposed key is shown as a
   proposal, never saved silently.
5. Arabic text, Arabic-Indic digits and RTL punctuation survive the round trip
   byte-for-byte.
6. The author is never asked to describe the format, choose a delimiter, or write
   a pattern.

**Tests** — *unit*: the parser against the reference exam, asserting 30 questions,
86 options, 30 keys, 0 multi-key questions; and against malformed input, which
must degrade to "questions found, keys not found" rather than throwing.
*integration*: import creates draft questions in the right tenant, category and
level. *e2e*: paste, review, confirm, then find all thirty in the bank in Arabic.

#### IMP-02 · Upload a Google Forms export
**MUST · NOT BUILT**

As a **training coordinator**, I want to upload the `.docx` my exam was exported
as, so that I do not have to open it and copy it out.

**Acceptance**
1. A `.docx` is accepted and its paragraph text is extracted before IMP-01's
   parser runs.
2. The trailing form chrome — "back", "submit", "clear form", the required-field
   legend — is not imported as questions.
3. A file with no recognisable questions reports that plainly and imports nothing.

**Tests** — *unit*: extraction and chrome-stripping against the reference file.
*integration*: file size and type limits reuse the existing media rules. *e2e*:
upload, preview, import.

#### IMP-03 · Be told which questions lost their picture
**MUST · NOT BUILT**

As a **training coordinator**, I want to be told which imported questions refer to
an image that was not in the file, so that I attach the chart rather than
discovering a question about an invisible candle.

**Acceptance**
1. An import from a file containing no media reports every question whose text
   refers to a stimulus, listing them by number and prompt.
2. Against the reference file, the questions about the green candle and the
   impulse wave are among those listed.
3. Each listed question links to its editor with the media field focused.
4. An exam with unresolved media warnings can still be published; the warning
   appears in the publish panel alongside the existing ones.

**Tests** — *unit*: the detection heuristic, including its false-positive
behaviour — over-reporting is acceptable, silently missing one is not.
*integration*: the warning reaches `CheckPublishAsync`'s warning list. *e2e*:
import the reference file, see the list, attach an image to one, see the list
shorten.

#### IMP-04 · Map an imported exam to the catalogue
**MUST · NOT BUILT**

As a **training coordinator**, I want to say which domain and level an imported
exam belongs to, so that its questions land in the bank where the next exam can
draw them.

**Acceptance**
1. The import screen asks for category and level using the same pickers as the
   exam editor, obeying CAT-03's scoping. *(The pickers now exist to reuse.)*
2. Every imported question is created with that category and level and a null
   `ExamId`, so it enters the shared bank rather than one exam. *(The bank now
   accepts such a question — BNK-06.)*
3. Competency and difficulty may be set for the whole import and adjusted per
   question afterwards.

**Tests** — *integration*: imported questions are drawable by a new exam at that
category and level. *e2e*: import, then create a second exam and see the questions
available to it.

*There is no bulk edit of any kind in the product: category, level and topic are
settable one question at a time. Criterion 3 is therefore also the only route to
filing two hundred imported questions without two hundred round trips.*

#### IMP-05 · Import candidates from a list
**SHOULD · BUILT** · ⚠ constraint

As a **training coordinator**, I want to paste or upload my class list, so that
forty students are not typed in one at a time.

**Acceptance**
1. Columns are matched without a naming convention to learn and no template to
   download first. *(Built — comma or tab, and the email column is found rather
   than assumed.)*
2. Rows with a duplicate email within the tenant are reported and skipped, naming
   the row; the rest import, and a re-import of the same list is idempotent.
   *(Built.)*
3. A row missing a name or an email is reported with its line number, not
   silently dropped. *(Built.)*
4. The import is previewed with counts before anything is written. *(Built — a
   two-stage Check then Confirm, the first posting `dryRun: true` and writing
   nothing.)*

**Tests** — *unit*: the row parser, duplicate and missing-field handling.
*integration*: `CandidateImportTests` covers idempotence, per-line errors and the
dry run. *e2e*: paste a list with one duplicate and one blank email; confirm the
counts and the two named rows.

*One constraint remains, and it is commercial rather than technical: every row
needs a unique email address. A vocational academy where siblings share a family
address, or where under-16s have none, cannot import its roll at all. See PPL-01,
where the same rule blocks the manual route as well.*

#### IMP-06 · Import a question bank from a spreadsheet
**MUST · BUILT** — *new; landed in `9da7c46`* · ⚠ constraint

As a **teacher**, I want to bring our existing questions in from a spreadsheet, so
that trying this product does not begin with retyping two hundred questions.

**Acceptance**
1. **No JSON in any cell, and no syntax to learn.** Columns are Type, Question,
   Option 1–4, Correct answer, Marks, Difficulty, Explanation. Only two are
   required — Type and Question. *(Built. This is the authoring constraint applied
   to the one place it is most tempting to break: an import format is where "just
   put the payload in a column" usually wins.)*
2. Column headings are matched in **Arabic and English**, and in the obvious
   synonyms of each; unknown columns — a reference number, an author's name — are
   ignored rather than made a reason to reject the file. *(Built.)*
3. The correct-answer cell accepts the **three shapes people actually write**: the
   option number (`٢`), several numbers (`١،٣`), or the answer written out
   (`القاهرة`). True/false accepts صح/خطأ, نعم/لا, ١/٠, and the two options are
   generated **in the language the answer was written in**, so an Arabic bank does
   not end up with `True`/`False` options. *(Built.)*
4. **An ambiguous type is refused rather than guessed.** English "multiple choice"
   is rejected as ambiguous and the author is asked to say "single choice" or
   "multiple choice", because half of English speakers mean one answer and half
   mean several — and guessing produces a bank that marks wrongly and looks
   correct. Arabic «اختيار من متعدد» reads as one answer and is accepted. *(Built.)*
5. The file is read **as Excel writes it**, not as we wish it were: byte-order
   mark, comma or semicolon or tab depending on the machine's locale, all three
   line endings, and standard quoting — so a question containing a comma survives,
   and a question spanning two lines does not shift every row number after it.
   *(Built.)*
6. Arabic is normalised before any matching: alif forms, tāʾ marbūṭa, alif
   maqṣūra, hamza carriers, diacritics, tatwīl, and both sets of digits. *(Built —
   and this is also what makes duplicate detection work on a re-import.)*
7. **Nothing is written before it is shown.** A dry run reports what will be
   created and what is wrong; one bad row costs the good rows nothing; a duplicate
   is left as it is. Only a file with no Question column or no Type column is
   refused outright. *(Built.)*
8. An error carries **the row number as the spreadsheet shows it** and the column
   name, so the fix is one cell rather than nine. *(Built.)*
9. **Every imported row goes through the same validation a hand-written question
   does**, so importing is not a route around the checks that stop an ungradable
   question reaching a candidate. *(Built — and this is the criterion that makes
   the feature safe rather than merely convenient.)*
10. The sample file is generated **from the same localisation keys the reader
   matches**, with a test that feeds it back into the importer — so the file we
   hand out can never become a file we reject. *(Built.)*

**Tests** — 57 in total: 38 on the reader alone, one of which builds a spreadsheet
from the real localisation files in both languages and asserts the reader reads it;
2 pass the resulting payload through the grader, because the reader and the grader
are two halves of this feature and nothing structural joins them; 11 integration;
8 in the browser at two sizes.

**Known gaps** — four question types (single choice, multiple choice, true/false,
short answer); no media column; two megabytes and two thousand rows per file.

*This is the answer to Epic 3's central problem arriving by a different road than
IMP-01 planned. It does not read a Word document or a Google Forms export, and
those are still unbuilt — but most centres' banks are in a spreadsheet or can be
pasted into one, so the onboarding cost this epic exists to remove is largely
removed.*

*A pre-existing defect surfaced while building it and is worth recording:
`astro-page-header` does not project `slot="actions"` content unless it is wrapped
in a single-rooted `@if`, so adding a second button hid **both** — including the
existing "Add question".*

---

# Epic 4 — Exams, sections and publishing

*Exam authoring is finished and good. Sections are the opposite: a complete
authoring screen writing four fields that the delivery path never reads. The
retrofit the first review asked for — build the taker section-aware from the first
line — was not taken, and this epic is where the bill for that arrives.*

#### EXM-01 · Create and edit an exam
**MUST · BUILT**

As a **training coordinator**, I want to set an exam's title, domain, level, time
limit and pass mark, so that it can be written and then given out.

**Acceptance**
1. A new exam is created as `Draft` and cannot be assigned.
2. The pass mark is a percentage of the form's maximum, so an exam worth 200 marks
   is not judged against a threshold meant for one worth 100.
3. Editing is refused without `Assessment.Exams.Edit`, and the editor does not
   render the controls.
4. Category and level are chosen from the catalogue. *(Built — this was a read-only
   field with no control until the catalogue shipped.)*

**Tests** — *integration*: default status, permission enforcement.
*e2e*: `exam-form.spec.ts`.

#### EXM-02 · Find an exam
**MUST · BUILT**

As a **training coordinator**, I want to list and filter exams by status, domain
and level, so that I can find the one I am about to give out.

**Acceptance**
1. Filters combine and paging is stable across pages.
2. Each row shows status and the form length against the bank size, and flags when
   the form is smaller than the bank.
3. Category and level filters are offered. *(Server honours both; the screen
   exposes only title search and status. A minor, cheap gap.)*

**Tests** — *integration*: filtering and paging. *e2e*: `exam-list.spec.ts`.

#### EXM-03 · Be stopped from publishing something broken
**MUST · BUILT**

As a **training coordinator**, I want to be told everything wrong with an exam
before I publish it, so that I fix the list once instead of discovering problems
one refused click at a time.

**Acceptance**
1. An exam with no questions cannot be published, and the reason names the exam.
2. An exam whose `QuestionsPerForm` exceeds its bank cannot be published, and the
   reason gives both numbers.
3. An exam with a blueprint rule that cannot be filled cannot be published, and
   the reason names the rule that starved. *(Blocking is built; the naming is not
   — see BPR-05.)*
4. All blockers are returned in one response, not the first one found.
5. Publishing calls the same check, so the panel and the action can never
   disagree.

**Tests** — *unit*: `Exam.Publish` refusals. *integration*: the full blocker list
in one call; publish refuses when the check refuses. *e2e*: `exam-actions.spec.ts`
— open the panel, see three blockers, fix one, see two.

#### EXM-04 · Be warned about what will merely go badly
**SHOULD · BUILT**

As a **training coordinator**, I want to be warned about the things that will work
but that I probably did not intend, so that a paper is not quietly worthless.

**Acceptance**
1. An exam whose questions carry no competency warns that the result will be a
   bare number.
2. A practice exam with questions lacking explanations warns.
3. An exam where every candidate gets the same paper warns.
4. A bank too small to rotate against the form length warns.
5. An over-exposed bank warns. *(Now reachable: `TimesServed` is incremented at
   assembly, so the comparison is no longer against a column that was always
   zero. The threshold is still a compiled constant and the warning still does not
   name the questions — see RES-08.)*
6. Warnings never block publication, and render distinctly from blockers.

**Tests** — *integration*: each warning fires on its own condition and none blocks.
*e2e*: the warning list renders distinctly from the blocker list.

#### EXM-05 · Take an exam out of circulation
**SHOULD · BUILT**

As a **training coordinator**, I want to archive an exam, so that it stops being
assignable without destroying the attempts already sat on it.

**Acceptance**
1. An archived exam cannot be assigned, and cannot be started —
   `Exam.IsOpenAt` refuses anything not `Published`.
2. Attempts already under way finish normally and remain readable.
3. Archiving requires `Assessment.Exams.Publish`, not `Edit`.

**Tests** — *integration*: assignment refused, in-flight attempt unaffected,
permission. *e2e*: archive from the list, confirm the assign action disappears.

#### EXM-06 · Divide an exam into named parts
**MUST · PARTIAL** — *authoring is built; delivery, grading and reporting are not,
and there is no control that puts a question into a section*

As a **teacher**, I want an exam to have named sections — Listening, Reading,
Grammar, Writing — so that a result tells a coordinator which class to put the
student in.

**Acceptance**
1. Sections are created, named, reordered and deleted within an exam. *(Built end
   to end on `/exams/:examId/structure`.)*
2. A question, a group and a blueprint rule may each belong to a section.
   *(`ExamSectionId` is on all three entities and on the question DTOs. **No screen
   sets it on a question** — the field appears nowhere in `angular/src`, so a
   section is a named empty container. `QuestionGroup` can be filed from the
   structure screen.)*
3. An exam with no sections behaves exactly as it does today, and its paper is
   assembled as one implicit section. *(True, because sections never reach
   assembly at all.)*
4. Deleting a section with questions in it asks what should happen to them and
   never silently orphans them. *(The service nulls the reference rather than
   orphaning, which is safe; it does not ask.)*
5. **The paper a candidate sits is laid out by section.** *(Not built.
   `AttemptQuestion` carries no section id, `ExamFormBuilder.Project` records
   none, and the taker has no section concept anywhere — see TAK-09.)*
6. **The score is reported by section.** *(Not built —
   `AttemptGradingService.RecalculateAsync` computes one flat total. See RES-04.)*

**Tests** — *unit*: assembly with zero, one and several sections produces the same
result for the zero case as today. *integration*: cascade rules on delete.
*e2e*: create four sections, move questions between them, reorder, sit it, and read
a per-section result.

*Sections, per-section timing and per-section reporting are the largest remaining
build in the product, and the section id owed on `ExamFormQuestion` and
`AttemptQuestion` is the reason. Named forms and sections still do not compose.*

#### EXM-07 · Give a section its own clock
**SHOULD · PARTIAL** — *the author can set a time limit that nothing enforces*

As a **teacher**, I want a section to be timed separately, so that a candidate
cannot spend the whole hour on the essay and never reach the listening.

**Acceptance**
1. A section with a time limit closes when its own time runs out and the next
   begins; the candidate cannot return to it. *(Not built. The field is editable on
   the structure screen and persists; nothing in the delivery path ever reads it.
   `Attempt` carries exactly one deadline.)*
2. A section with no time limit shares the exam's clock.
3. The countdown shown is the section's when one is set, and the exam's otherwise,
   and both are computed from the server.
4. Section time is enforced server-side; a manipulated browser clock cannot extend
   it.

**Tests** — *unit*: remaining-time computation per section. *integration*: an
answer submitted to a closed section is refused. *e2e*: watch a section close and
the next begin.

*Marked PARTIAL rather than NOT BUILT deliberately. A coordinator can today set
"Listening: 20 minutes", save it, see it saved, and every candidate will get the
whole exam's clock — which is the specific failure `business-review-2.md` §8 named
as worse than an absent feature. Either enforce it or hide the field.*

#### EXM-08 · Fail an exam on one section however well the rest went
**SHOULD · PARTIAL** — *the domain rule is written and tested; grading never calls it*

As a **training coordinator**, I want a section to carry a minimum below which the
whole exam fails, so that passing overall while failing the safety module is not a
pass.

**Acceptance**
1. A section scored below its `MinimumPercentage` fails the attempt regardless of
   the total. *(`ExamSection.IsFailedAt` exists, is unit-tested in `ExamFormTests`,
   and is called by nothing. The floor is editable on the structure screen.)*
2. A section with no minimum only contributes to the total. *(Built and tested at
   the entity.)*
3. The result states which section caused the failure, not merely that the attempt
   failed.
4. Pass or fail is `attempt.ApplyScore(score, maxScore, exam.PassingPercentage)`
   alone today; nothing in the grading path knows a section exists.

**Tests** — *unit*: `ExamFormTests` covers the entity rule; extend to the attempt
scoring path. *integration*: an attempt above the pass mark but below a section
minimum is recorded as failed. *e2e*: the result page names the section.

#### EXM-09 · Turn a candidate away in thirty seconds
**SHOULD · PARTIAL** — *the flag is editable and read by nothing*

As a **recruiter**, I want an untimed pass/fail gate before the exam proper, so
that someone who does not qualify is not marked for an hour before we find out.

**Acceptance**
1. A section flagged `IsQualifying` is presented before every other section,
   untimed. *(The toggle is on the structure screen and the value is persisted. No
   code reads it — no gate, no early exit, no screener ordering.)*
2. Failing it ends the attempt immediately with a distinct end reason.
3. An attempt ended this way never enters the reviewer's queue.
4. The candidate is told they did not meet the entry requirement, without being
   shown which answer was wrong.

**Tests** — *unit*: the gate decision. *integration*: the ended attempt is absent
from `GetQueueAsync`. *e2e*: fail the gate, see the message, confirm no exam
questions were served.

*This is a recruiter story and the first customer is an academy, so it is the
cheapest of the section stories to defer. The toggle should be hidden until it
does something.*

#### EXM-10 · Choose whether everyone sits the same paper
**MUST · NOT BUILT**

As a **training coordinator**, I want to choose between drawing a paper per
candidate, using one approved paper, or rotating several, so that I can start with
a paper I have read and move on when I trust the system.

**Acceptance**
1. `DeliveryMode` offers the three options; `DrawPerCandidate` is the default and
   is what existing exams do.
2. `FixedForm` requires `FixedFormId` to point at a published form of this exam;
   publishing is refused otherwise, naming the exam.
3. `RotateForms` requires at least two published forms; publishing is refused
   otherwise, saying how many exist.
4. Changing mode on an exam with attempts in flight does not change those
   attempts' papers.

**Tests** — *unit*: the publish preconditions per mode. *integration*: attempts in
flight keep their frozen form. *e2e*: switch to `FixedForm` with no form and see
the refusal.

*`Exam.DeliveryMode` and `Exam.FixedFormId` remain declarations read by nothing —
not on any DTO, not on any screen. **This is now arguably the right outcome**: the
mechanism that actually shipped chooses a form per assignment
(`Assignment.ExamFormId`, see ASG-09 and FRM-04), which is the better primary
control because morning and afternoon sittings differ. The honest options are to
implement these as exam-level *defaults* or to delete the enum values. Leaving
three grantable-looking options that do nothing is the worse third choice.*

#### EXM-11 · Practise rather than be judged
**SHOULD · BUILT**

As a **candidate**, I want a practice exam to show me the right answer and the
explanation afterwards, so that I learn something rather than receiving a number.

**Acceptance**
1. In `Practice` mode the result reveals the correct answer and the explanation
   per question. *(Built — gated on `exam.Mode == Practice` and rendered on the
   taker's result screen.)*
2. In `Assessment` mode neither ever reaches the browser.
3. On a weighted question a learner is shown best / acceptable / not credited, and
   never the "penalised" bucket.
4. The mode is set from the exam editor. *(Built.)*

**Tests** — *unit*: `TakerQuestionProjectorTests` — the key never crosses the wire.
*integration*: mode governs what `GetResultAsync` returns. *e2e*: sit a practice
exam and read the explanation.

#### EXM-12 · Open an exam only within a window
**COULD · PARTIAL** — *enforced server-side; no control sets it*

As a **training coordinator**, I want an exam to be sittable only between two
instants, so that a cohort sits it together.

**Acceptance**
1. Outside the window, starting is refused with `IMS:Exam:OutsideSchedule`.
   *(Built — `Exam.IsOpenAt`, enforced in both `OpenLinkAsync` and `StartAsync`,
   with validation on the way in.)*
2. An attempt started inside the window may finish after it closes.
3. The window is shown to the candidate on the preview screen before they start.
4. **There is a control that sets it.** *(Not built. `isScheduled`,
   `scheduledStartTime` and `scheduledEndTime` exist in the exam form component's
   DTO plumbing and appear on no input in the template, so a coordinator can never
   open a window.)*

**Tests** — *unit*: `IsOpenAt` boundaries. *integration*: start refused outside,
in-flight attempt unaffected by the close. *e2e*: the preview shows the window.

---

# Epic 5 — Blueprints and per-candidate assembly

*Assembly itself is solid and seeded-reproducible. The recipe that drives it has a
complete API and no screen, which means in practice every paper is "the whole
bank, capped and shuffled".*

#### BPR-01 · Describe the paper as a recipe
**MUST · BUILT** — *fixed in `0842cc9`, after this revision first recorded it as PARTIAL*

As a **teacher**, I want to say "eight medium listening questions and six easy
grammar ones", so that every candidate's paper covers the same ground at the same
difficulty even though the questions differ.

**Acceptance**
1. A rule names a competency, a difficulty, a type and a count; any of the first
   three may be left as "any". *(Server built — `GetBlueprintAsync`,
   `SetBlueprintAsync`, and `ExamController` exposes both.)*
2. Each rule shows how many bank questions currently match it, so an unfillable
   rule is visible while it is being written, marked on its row and counted in the
   footer. *(Built — and this number has to be on screen while somebody is still
   looking at it, because the builder contributes what it can and never fails, so
   an unfillable blueprint produces a short paper silently and nobody finds out
   until a candidate has sat it.)*
3. There is a screen. *(Built — `/exams/:examId/blueprint`, with a `setBlueprint`
   client method that did not exist. Until it landed, the papers screen offered
   "fill from the blueprint" as the recommended way to build a form, there was
   nothing to fill from, and no way to say so.)*
4. Rules are ordered, and that order is the order their questions appear.

**Tests** — *integration*: set and re-read a blueprint; matching counts per rule.
*e2e*: add three rules, see one show zero matches, fix it.

*This is what makes two drawn papers comparable, and it is the argument for drawing
a paper at all rather than fixing one. It became worth building only once topics
could be created (CAT-04) and set on a question (BNK-06) — before that a rule
keyed on competency had nothing to key on.*

#### BPR-02 · Give every candidate a different but comparable paper
**MUST · BUILT**

As a **training coordinator**, I want two candidates to get different questions
covering the same ground, so that a leaked paper is worth little without any
surveillance.

**Acceptance**
1. Two attempts on the same exam with different seeds draw different question
   sets, and both satisfy every blueprint rule's count.
2. The same attempt rebuilt from its stored seed produces the identical paper,
   including option order.
3. A rule that cannot be filled contributes what it can rather than failing an
   attempt already under way.
4. An exam with no blueprint takes the whole bank, or a capped random subset.

**Tests** — *unit*: seeded reproducibility, per-rule counts, the partial-fill rule,
the flat-draw path. *integration*: a second attempt on the same link resumes the
first paper rather than drawing a new one. *e2e*: two candidates, two papers, both
the right length.

#### BPR-03 · Keep the answer out of the paper's shape
**MUST · BUILT**

As a **candidate**, I want the order of a matching or ordering question not to
give the answer away, so that the exam measures what I know.

**Acceptance**
1. Option order is recorded for `matching` and `ordering` regardless of the exam's
   shuffle setting, because for those types the stored order is the key. *(Built —
   `ExamFormBuilder.Project` forces it via `AlwaysOrdered`.)*
2. With shuffling off, a matching question's payload as received does not pair
   left[i] with right[i].
3. A candidate reloading mid-question sees the same order.
4. **Every path that builds a paper goes through the one place that records the
   order.** *(Built, and this is the interesting part. The named-form path once
   constructed `AttemptQuestion` by hand and omitted `OptionOrder`, which handed
   the candidate the key to every matching and ordering question. It now maps its
   slots to `PaperSlot` and delegates to `ExamFormBuilder.Project`, so the two
   paths share one implementation — which removes the class of defect rather than
   the instance.)*

**Tests** — *unit*: `AlwaysOrdered` types record an order under both settings.
*integration*: `NamedFormDeliveryTests` starts a real attempt on a named form
carrying a four-pair matching question and asserts the persisted `OptionOrder` is
not null. **Strengthen that assertion**: it currently checks non-null, and a
seeded shuffle can legitimately return the identity permutation, so a regression
that recorded an order without permuting it would still pass. Assert that the
served order differs from the authored one. *e2e*: reload and confirm the order is
stable.

#### BPR-04 · Compose a paper section by section
**MUST · NOT BUILT**

As a **teacher**, I want each section to draw its own questions to its own recipe,
so that a four-skills paper has the right number of questions in each skill.

**Acceptance**
1. A blueprint rule scoped to a section draws only from that section's questions.
2. A section's `QuestionsPerForm` caps its draw; null takes everything it holds.
3. Sections appear in `DisplayOrder`, and shuffling never moves a question across
   a section boundary.
4. An exam with no sections assembles exactly as it does today.

**Tests** — *unit*: sectioned assembly, cross-boundary shuffle prohibition,
unchanged behaviour for the unsectioned case. *integration*: a four-section exam
produces a paper with the right counts per section. *e2e*: sit it and confirm the
section order.

*`ExamBlueprintRule.ExamSectionId` is a column that is absent from both blueprint
DTOs, never set by `SetBlueprintAsync`, and never filtered on by the builder.*

#### BPR-05 · Fail loudly when a rule starves
**SHOULD · PARTIAL** — *it blocks; it does not say which rule*

As a **training coordinator**, I want to be told which rule could not be filled,
so that I know which competency to write more questions for.

**Acceptance**
1. `CheckPublishAsync` names the starved rule, not merely that the blueprint is
   unsatisfiable. *(It adds one generic `ExamBlueprintUnsatisfiable` and breaks out
   of the loop, so only the first is reported and it is unnamed.)*
2. Every starved rule is reported, not the first.
3. The message gives the required and the available counts. *(The per-rule
   available count is already computed; it is thrown away.)*
4. At delivery, a rule that starves takes fewer questions silently. It should
   record that it did, so a short paper is explicable afterwards.

**Tests** — *integration*: three starved rules produce three named messages with
both counts. *e2e*: the publish panel lists them.

#### BPR-06 · Prefer questions that have been seen least
**COULD · NOT BUILT**

As a **training coordinator**, I want the draw to favour questions that have been
served fewest times, so that exposure spreads across the bank instead of
concentrating.

**Acceptance**
1. Within a rule's eligible set, selection is weighted towards low `TimesServed`.
2. Reproducibility from the seed is preserved.
3. A per-question exposure ceiling, when set, excludes a question from selection
   and is reported if it starves a rule.

**Tests** — *unit*: distribution over many seeded draws; reproducibility retained.
*integration*: the ceiling starving a rule surfaces through BPR-05.

*Selection is an unweighted Fisher-Yates shuffle over the eligible set.
`TimesServed` is now genuinely written, so the input this needs exists.*

#### BPR-07 · Copy a blueprint to another exam
**COULD · NOT BUILT**

As a **teacher**, I want to reuse a blueprint on a second exam, so that the three
levels of one course are structured alike without being rebuilt.

**Acceptance**
1. Copying carries rule structure and counts, not the questions.
2. Rules referencing a competency absent from the target's category are flagged
   rather than silently dropped.

**Tests** — *integration*: the copy, and the flagging of unresolvable references.

---

# Epic 6 — Named forms

*The most improved epic in the document, and the only one with no PARTIAL stories.
A form is built, hand-picked or generated, published, retired, chosen at send time
and sat by the person it was chosen for. The loop closes.*

#### FRM-01 · Build a named paper
**MUST · BUILT**

As a **training coordinator**, I want to build "Form 1" as a fixed list of
questions, so that there is a paper a human can read before anybody sits it.

**Acceptance**
1. A form is created with a name and a code; the code is unique within its exam.
2. Questions are added from the exam's drawable bank, ordered, and given the marks
   they carry on this form. *(Built on `/exams/:examId/forms` — hand-pick, reorder
   up and down, save.)*
3. Marks are copied onto `ExamFormQuestion`, so raising a question's marks later
   does not change what a past candidate scored.
4. A form may be generated from the blueprint, and `WasGenerated` records that it
   was, so a later reviewer can tell which it was. *(Built — generate-from-bank is
   a button on the same screen.)*

**Tests** — *unit*: generation from a blueprint produces a form satisfying every
rule. *integration*: code uniqueness within an exam; marks copied not referenced.
*e2e*: build a form by hand, reorder it, save it.

#### FRM-02 · Freeze a form for use
**MUST · BUILT**

As a **reviewer**, I want to publish a form once I have read it, so that what I
approved is what candidates sit.

**Acceptance**
1. A form with no questions cannot be published, and the reason names the form.
2. A form carrying the same question twice cannot be published. *(The entity
   refuses it; the message still does not name the duplicate.)*
3. Publishing freezes `MaxScore`.
4. A published form's question list cannot be changed; an attempt to change it is
   refused, not silently ignored. *(Built — `RequireDraftAsync` guards both edit
   paths.)*
5. Both refusals show a sentence rather than a code. *(Now true —
   `IMS:ExamForm:NoQuestions` and `IMS:ExamForm:DuplicateQuestions` are in both
   language files, and a test fails the build if any code is not.)*

**Tests** — *unit*: `ExamFormTests` — extend for the naming in (2). *integration*:
an edit to a published form is refused. *e2e*: publish, then find the editing
controls gone.

#### FRM-03 · Retire a form without losing what was sat on it
**SHOULD · BUILT**

As a **training coordinator**, I want to take a form out of rotation, so that a
paper I think has leaked stops being served while old results still resolve.

**Acceptance**
1. A retired form is never selected for a new attempt — `IsUsable` is checked when
   an assignment names it.
2. Results referencing a retired form still render, including its name and code.
3. Deleting a form that has been sat is refused; retiring it is the way out.
   *(Built, and the guard can actually fire now that `TimesUsed` is written — a
   paper forty people sat used to be deletable outright.)*

**Tests** — *unit*: `IsUsable`. *integration*: results resolve; the delete refusal.
*e2e*: retire and confirm the assign action explains itself.

#### FRM-04 · Sit a fixed form
**MUST · BUILT**

As a **candidate**, I want to sit the paper my centre approved, so that everyone
in my sitting answered the same questions.

**Acceptance**
1. The attempt's `AttemptQuestion` rows come from `ExamFormQuestion` in its
   `DisplayOrder`, and the blueprint is not consulted. *(Built —
   `BuildFromNamedFormAsync`, selected because the assignment named a form.)*
2. `Attempt.MaxScore` equals the form's frozen `MaxScore`.
3. Option shuffling still applies where the type demands it; question order does
   not, because the form's order is the form. *(Built — and this is where the
   answer-key leak was, see BPR-03.)*
4. `ExamForm.TimesUsed` is incremented once per attempt started on it. *(Built.)*
5. A question deleted since the form was published is skipped rather than failing
   a candidate mid-sitting. *(Built.)*

**Tests** — *unit*: assembly from a form rather than the bank. *integration*:
`NamedFormDeliveryTests` — `MaxScore` matches, `TimesUsed` increments once per
start, the option order is recorded. *e2e*: two candidates on a fixed-form exam
receive the same questions.

#### FRM-05 · Spread a cohort across forms
**SHOULD · NOT BUILT**

As a **training coordinator**, I want candidates spread across the published
forms, so that what leaks at lunchtime is worth a fraction of the sitting.

**Acceptance**
1. Each new attempt takes the published form with the lowest `TimesUsed`; ties
   break deterministically.
2. Rotation never selects a draft or retired form.
3. A retake by the same candidate takes a form they have not sat, when one exists.

**Tests** — *unit*: the selection rule including ties and exhaustion. *integration*:
twenty attempts across three forms distribute evenly. *e2e*: two candidates,
different forms.

*An assignment carries exactly one form for the whole cohort, so spreading a class
across three papers means creating three assignments by hand — which works, and is
the workaround to describe to a pilot customer rather than build around.*

#### FRM-06 · Guarantee a retake differs
**SHOULD · NOT BUILT**

As a **training coordinator**, I want a resit to use a different paper, so that
"sit it again" is not a redraw that happens to repeat half the questions.

**Acceptance**
1. A second attempt by the same candidate on the same exam is assigned a different
   published form when one exists.
2. When none exists, the coordinator is warned at assignment time rather than the
   candidate discovering it.

**Tests** — *integration*: the second attempt's form differs. *e2e*: the warning at
assignment.

*Nothing compares a new attempt against a prior one. The related resit defect —
a second link to the same exam resolving to whichever row the database returned
first — is fixed: `ExamSessionClaims` now carries the link id.*

#### FRM-07 · Know how worn a paper is
**SHOULD · BUILT**

As a **training coordinator**, I want to see how many times each form has been
sat, so that I know when to write a new one.

**Acceptance**
1. Each form shows `TimesUsed`. *(Built — written at delivery, projected, and
   rendered on the forms screen.)*
2. A form past a tenant-set threshold is flagged in the list. *(Not built — no
   threshold exists for forms. Question-level exposure has one, compiled in; see
   RES-08.)*
3. The date it was last sat. *(Not built.)*

**Tests** — *integration*: the count. *e2e*: the list shows it.

#### FRM-08 · Print a form for review or for paper
**COULD · NOT BUILT**

As a **reviewer**, I want to read a form as a document, so that I can approve it
away from the screen, and so that a centre without reliable internet can still run
the exam on paper.

**Acceptance**
1. A form renders as a printable document with its questions in order, in Arabic
   with correct direction.
2. An answer key is a separate document, produced only with
   `Assessment.Questions.View`.
3. Media renders in the printed form; a question whose media is missing is flagged
   in the document.

**Tests** — *integration*: the key requires the permission. *e2e*: render both,
assert the key is absent from the candidate document.

---
# Epic 7 — People and cohorts

*People context, and one of the biggest movements in this revision: from "entities
and tables, no service, no route, `PlaceholderComponent`" to a working candidates
screen, a working classes screen and a paste importer. What is missing now is
small and specific — a person cannot be created or corrected by hand, only
imported.*

#### PPL-01 · Add a person to be assessed
**MUST · PARTIAL** — *the service and even the client method exist; nothing calls them*

As a **training coordinator**, I want to record a student's name and email, so
that an exam can be sent to them.

**Acceptance**
1. A candidate is created with name and email; no account, no password, no
   invitation to sign up. *(Server built — `CandidateAppService.CreateAsync` behind
   `POST api/assessment/candidates`, and `CandidateService.create()` exists on the
   client. **Nothing in the UI calls it.** Both primary buttons on the candidates
   screen open the import panel; there is no create draft state at all. Paste
   import is the only way a person enters the system.)*
2. A duplicate email within the tenant is refused with
   `IMS:Candidate:EmailAlreadyExists`, naming the existing person. *(Built.)*
3. The same email in a different tenant is accepted. *(Built.)*
4. The screen uses the tenant's own word for this person, from `CategorySet`.
   *(Not built — CAT-01.)*
5. **A person without an email address can be recorded.** *(Not built. Email is
   `[Required]` and unique per tenant. A vocational academy where siblings share
   an address, or where under-16s have none, cannot enter its roll at all — by
   import or by hand. Either allow a null email and require it only when a link is
   sent, or add a second identifier the tenant chooses.)*

**Tests** — *integration*: uniqueness per tenant, cross-tenant independence,
permission `Assessment.Candidates.Create`, and the null-email path once it exists.
*e2e*: create, see the tenant's vocabulary, attempt a duplicate.

#### PPL-02 · Group people into a class or a batch
**MUST · BUILT**

As a **training coordinator**, I want to put students into a class, so that an
exam is sent to forty people in one action.

**Acceptance**
1. A group is created and members added and removed; a person may belong to
   several groups. *(Built on `/groups` — and the roll is edited as a whole list
   in one save, which suits how a coordinator actually thinks about a class.)*
2. Removing a person from a group does not delete the person or their attempts.
3. A group's member count is shown wherever the group is selectable.
4. Assigning to an empty group is refused with `IMS:Assignment:GroupEmpty`.

**Tests** — *integration*: membership, the non-cascading removal, the empty-group
refusal. *e2e*: create a class, add five, assign, see five links.

#### PPL-03 · Find a person
**MUST · BUILT**

As a **training coordinator**, I want to search people by name, email and group,
so that I can find one among several hundred.

**Acceptance**
1. Search matches name, email and reference, case-insensitively.
2. Filtering by group combines with the search, and paging is stable.
3. Arabic text matches regardless of diacritics. *(Not verified — the server uses a
   plain contains, so this depends on the database collation and is untested.)*

**Tests** — *integration*: matching rules including the Arabic case. *e2e*: search
in Arabic and find the right person.

#### PPL-04 · See one person's history
**SHOULD · PARTIAL** — *the data is reachable by a search; there is no view*

As a **training coordinator**, I want to see every exam a student has sat and how
they did, so that I can advise them.

**Acceptance**
1. The list shows exam, date, score, pass/fail and whether review is outstanding.
   *(No per-candidate endpoint exists. The nearest route is typing the person's
   name into the results list's free-text filter, which works and is not a
   history.)*
2. An attempt still awaiting a human shows as pending, never as a provisional
   score. *(True wherever a result is shown.)*
3. Each row opens the answer sheet, subject to `Assessment.Attempts.View`.
4. The candidate row's attempt count is a link. *(It is a number, and not a link.)*

**Tests** — *integration*: pending attempts render as pending; permission on the
answer sheet. *e2e*: the history and one answer sheet.

*Cheap now: `ResultAppService.GetListAsync` already filters and projects everything
this needs. It is a candidate-id filter and a link.*

#### PPL-05 · Correct a person's details
**SHOULD · PARTIAL** — *the service, the route and the permission signal all exist; there is no form*

As a **training coordinator**, I want to fix a misspelt name or a wrong email, so
that an invitation reaches the right inbox.

**Acceptance**
1. Editing an email does not invalidate links already issued to that person.
2. Editing is refused without `Assessment.Candidates.Edit`. *(The component
   declares `canEdit = permissionSignal(P.Candidates.Edit)` and **never references
   it** — the row renders a delete button and nothing else.)*
3. The change is audited.

**Tests** — *integration*: existing links still resolve; permission; audit row.

*A coordinator who imports "Muhamed" cannot become "Muhammad" without deleting the
person and re-importing, which loses their attempts.*

#### PPL-06 · Remove a person
**COULD · BUILT**

As an **administrator**, I want to delete a person on request, so that we can
answer a data-protection request.

**Acceptance**
1. Deletion is confirmed before it happens. *(Built.)*
2. Deleting a person who has attempts is refused rather than cascading. *(Built —
   the safe choice, and it means criterion 3 below is the remaining work.)*
3. A full erasure removes the person, their attempts, answers, uploaded files and
   integrity signals, and states that aggregate item statistics are not recomputed
   backwards. *(Not built — refusal is not erasure, so a genuine deletion request
   still cannot be answered.)*

**Tests** — *integration*: the refusal; then, when built, every dependent row gone
including blobs. *e2e*: the confirmation.

#### PPL-07 · Run a class as an intake
**MUST · BUILT** — *new; shipped and never written down*

As a **training coordinator**, I want a class to sit at a level and run between
two dates, so that "Spring intake, B1" is a thing in the product and not a naming
convention in my head.

**Acceptance**
1. A class carries a level, chosen from the levels of a chosen category. *(Built —
   the category select drives the level select on the groups screen, and
   `CandidateGroup.LevelId` is persisted. This was unreachable until the catalogue
   shipped: the field existed and could never be set to anything.)*
2. A class carries a start date, an end date and an active flag.
3. An inactive class is not offered as an assignment target.
4. The class's level narrows which exams and bank questions are sensible for it.
   *(Not built — nothing uses the level for filtering yet, which is the next
   increment and the reason the field is worth having.)*

**Tests** — *integration*: the level scoping, the active flag. *e2e*: create a
class at a level, add a roll, assign to it.

*Recorded here because it shipped without a story. Note what was deliberately
removed alongside it: `CandidateGroupForm` — "the ordered list of papers this class
will sit" — was built, tested and then deleted, because nothing consumed a form at
delivery time and its unique indexes forbade the morning/afternoon split the
feature existed for. The same guarantee is now delivered by one nullable column on
`Assignment`. That deletion was correct and the reasoning is worth keeping: build
the mechanism before the control surface.*

---

# Epic 8 — Assignment and links

*Sending works, and the link machinery is careful — a token per person, hashed at
rest, per-recipient failure reporting, revocation. The gaps are the ordinary
operational ones a coordinator hits in week one: resend, extend, and fixing a
mistake.*

#### ASG-01 · Send an exam to one person
**MUST · PARTIAL** — *the server accepts one person; the screen only offers a class*

As a **recruiter**, I want to send an exam to a candidate with an expiry and a
number of attempts, so that they can sit it without an account.

**Acceptance**
1. Exactly one of candidate or group must be supplied; neither is
   `IMS:Assignment:TargetMissing`, both is `TargetAmbiguous`. *(Built.)*
2. An expiry in the past is refused with `IMS:Assignment:ExpiryInPast`. *(Built.)*
3. An unpublished exam cannot be assigned, and the reason is
   `IMS:Exam:NotPublished`. *(Built.)*
4. One `ExamLink` is created per person, each with its own token. *(Built.)*
5. **There is a person picker.** *(Not built. `candidateId` exists on the client
   DTO; the send panel never sets it, and the confirm button is disabled without a
   group. Sending to one person means making a group of one.)*

**Tests** — *integration*: each refusal; one link per recipient.
*e2e*: assign to one person and read back the URL.

#### ASG-02 · Send an exam to a whole class
**MUST · BUILT**

As a **training coordinator**, I want to send an exam to a group in one action, so
that forty links are not created by hand.

**Acceptance**
1. Assigning to a group creates one link per member, each individually revocable.
2. `LinkCount` records how many were produced.
3. An empty group is refused.
4. The screen is reachable. *(Built — from the exam list's row action, which
   carries the exam id. The sidebar's bare `/assignments` entry is **dead**: the
   route file declares only `:examId`, so it backtracks to the wildcard and lands
   on the dashboard. See ADM-02.)*

**Tests** — *integration*: link count matches membership; individual revocation
does not affect the rest. *e2e*: assign to five, revoke one, three remain valid
plus the untouched one.

#### ASG-03 · Deliver the invitation
**MUST · BUILT** — *the message is now the centre's; it still needs an SMTP relay to
arrive* — closed in `4e59b1a`

*Was PARTIAL: the only message that reaches a person with no account and no prior
relationship with us carried no organisation name, no logo and no support address —
"an exam has been assigned to you" and a link to a domain they had never seen,
which is precisely the shape of message people are taught not to open. The name was
sitting in the tenant's own settings, read by nothing the candidate could see. It
now carries the organisation's name in the subject and in both language bodies, and
a start button in the organisation's colour; a tenant that has not named itself
gets a sentence that reads correctly with no name rather than a placeholder. The
colour is validated before it reaches a `style` attribute — a colour field that
accepts any text accepts `red; background-image:url(...)`, i.e. a tracking pixel
sent over our signature — and names and titles are escaped before entering HTML.
The message is built by a pure function separate from sending, so what reaches a
candidate is verifiable without a mail server: ten tests. **The logo is
deliberately omitted**: it is served behind a signed grant a mail client does not
carry, so it would arrive broken, which is worse for trust than no image.*

As a **candidate**, I want to receive the link by email, so that I can find it
when I am ready to sit.

**Acceptance**
1. The email carries the candidate's own URL and the expiry. *(Built.)*
2. One unreachable address does not abandon the other links; the failure is
   reported per recipient with the address, and the link stays usable. *(Built, and
   rendered well — the panel lists each failure with a copyable link.)*
3. `EmailSentAt` is recorded per link on success. *(Built.)*
4. **The email carries the tenant's name, logo and support address, not ours.**
   *(Not built. `SendInvitationAsync` is a hardcoded bilingual HTML string
   interpolating candidate name, exam title, duration, expiry and URL —
   `TenantSettingsAppService` is never injected. This is the "reads as phishing"
   failure: an unbranded message with a long token link, sent to a teenager, from
   nobody they recognise.)*
5. The email is in the candidate's language and renders right to left in Arabic.
   *(Bilingual — both languages in one body, which is a reasonable interim.)*

**Tests** — *integration*: partial failure leaves other links sent and reports the
failed address; `EmailSentAt` set only on success; the rendered body carries the
tenant's name and support address. *e2e*: not applicable — assert the body in an
integration test against a captured message instead.

*Also note the deployment reality: SMTP points at `127.0.0.1:25` with no
credentials, so no invitation has ever been delivered.*

#### ASG-04 · See the state of every link
**MUST · BUILT**

As a **training coordinator**, I want to see who has opened their link, who has
started and who has finished, so that I know who to chase.

**Acceptance**
1. Each row shows the token prefix, expiry, attempts used against allowed, first
   opened, email sent, and revocation, resolved into one of six states. *(Built.)*
2. The full token is never returned by this endpoint, and only the prefix is
   displayed. *(Built — and the consequence is ASG-06: a lost link cannot be
   recovered, only replaced.)*
3. Rows are filterable by state.

**Tests** — *integration*: the response contains no field from which a working
token can be derived. *e2e*: the list and its filters.

#### ASG-05 · Kill a link that leaked
**MUST · BUILT**

As a **training coordinator**, I want to revoke a link, so that one sent to the
wrong person stops working immediately.

**Acceptance**
1. A revoked link reports itself as revoked, not as invalid.
2. Revocation does not end an attempt already in progress on it; ending that is
   ASG-08.
3. Revocation requires `Assessment.Assignments.Revoke`.

**Tests** — *unit*: `ExamLink.GetBlockReason` returns the specific reason for each
of revoked, expired and exhausted. *integration*: permission; in-flight attempt
unaffected. *e2e*: revoke, then open the link and read the reason.

#### ASG-06 · Resend an invitation
**SHOULD · BUILT** — *as reissue, not resend* — closed in `a2fbf91`

*The token is stored hashed and cannot be recovered, so "send them the same link
again" is not available and should not be made available. The honest answer is the
ability to issue another: `ReissueLinkAsync` mints a new address, invalidates the
previous one, clears the first-opened stamp, and **does not reset the attempts
already used** — somebody who lost their link is not somebody who wants to sit the
exam again. Four tests: the new one works, the old one stops, no attempt is bought,
and a deliberately revoked link is not quietly resurrected.*

As a **training coordinator**, I want to resend a link, so that a student who
deleted the email is not blocked.

**Acceptance**
1. Resending reuses the existing link and does not mint a new token.
2. `EmailSentAt` is updated and a resend count recorded.
3. Resending a revoked or expired link is refused with the reason.

**Tests** — *integration*: the token hash is unchanged after a resend; refusals.
*e2e*: resend and see the timestamp change.

*The permission `Assessment.Assignments.SendEmail` is declared and enforces
nothing. Because the plaintext token is returned exactly once at creation and only
hashed thereafter, a resend must re-send the stored hash's link — which means
either storing it encrypted rather than only hashed, or accepting that a lost link
is replaced rather than resent. That is a decision, not a task.*

#### ASG-07 · Extend an expiry
**SHOULD · BUILT** — closed in `7dd405e`

*`ExtendLinkAsync` moves a deadline **forwards only**. Forwards, because pulling it
backwards ends a sitting under somebody in the middle of it with no warning and no
appeal — closing an exam early is what revoke is for, and revoke tells the person
holding the link what happened. A date already in the past is refused by name,
because somebody who mistypes the year deserves to be told rather than left
watching a link fail. The address does not change: the link is still in the
candidate's inbox and the problem is a deadline, not an address. And extending does
not restore a spent attempt — a coordinator fixing a date must not hand out retakes
without knowing it. Four tests, one of which **ages** the link rather than creating
it expired, because `CreateAsync` rightly refuses a past date: what needed
simulating was Friday arriving, not a coordinator typing last week. The dialog
pre-fills a week from today rather than from the old date, which may be months
gone.*

*Before this, the only tool a coordinator had for somebody who missed the deadline
was reissue — which does not touch the expiry, so it handed them a **new address
that was already expired**, and nothing on the screen said so.*

As a **training coordinator**, I want to push back an expiry, so that a student
who was ill is not made to start again.

**Acceptance**
1. Extending updates the link and, if set, the assignment.
2. An expiry cannot be moved into the past.
3. The change is audited with who and when.

**Tests** — *integration*: past-date refusal; audit. *e2e*: extend and confirm the
link works again.

#### ASG-08 · End someone's attempt
**SHOULD · BUILT** — committed in `719c1e0`; seven integration tests

As an **administrator**, I want to end an attempt that is stuck or was started in
error, so that it can be graded or discarded rather than sitting open.

**Acceptance**
1. Sittings in progress are listed, under Results, because "how is this going" is
   the same question asked a few minutes earlier. *(Built —
   `/results/running`, with its own nav entry gated on `Attempts.View`.)*
2. Force-submitting records who ended it and why, and grades the attempt. *(Built —
   with a confirmation and a reason, and a migration recording the ender.)*
3. An attempt started in error can be discarded outright. *(Built, behind a second
   confirmation and `Attempts.Delete`.)*
4. Each action carries its own permission — view, force-submit and delete are
   separable. *(Built.)*
5. The candidate's session for that attempt stops accepting answers immediately.
   *(Follows from the attempt being submitted; needs a test.)*

**Tests** — *integration*: end reason, grading runs, each permission. *e2e*: force
submit while a taker session is open and confirm the next save is refused.

*It retired three permissions the previous revision had listed as
declared-but-dead — `Attempts.View`, `.ForceSubmit` and `.Delete`. Two decisions in
it are worth keeping: **ending a sitting marks everything answered up to that
moment in full**, because the candidate did that work and the reason they stopped is
not their score's problem; and the reason is recorded **in the coordinator's own
words** on the attempt, because ending somebody's exam early gets questioned weeks
later — by the candidate, by an auditor, by the coordinator's own manager — and
"the system did it" is not an answer anybody can defend. Attempts past their
deadline are hidden by default: they close themselves within a minute, and listing
them invites somebody to intervene where they need not.*

#### ASG-09 · Choose which form a sitting uses
**SHOULD · BUILT**

As a **training coordinator**, I want to say which named form this assignment
uses, so that the morning group and the afternoon group sit different papers.

**Acceptance**
1. The assignment may name a published form, and the picker appears only when
   published forms exist. *(Built.)*
2. Naming a draft or retired form is refused, with the reason. *(Built — ownership
   and `IsUsable` are both checked.)*
3. The chosen form is recorded on the assignment and used by every attempt started
   from it. *(Built — `StartAsync` reads `Assignment.ExamFormId`, records it on the
   `Attempt`, and builds from the form.)*

**Tests** — *integration*: refusals; the attempt uses the named form.
`NamedFormDeliveryTests` covers the happy path through the real link and start.
*e2e*: two assignments, two forms, two papers.

*This is the sentence "approve the exact paper before it goes out" becoming true.
It was worth zero to a customer for two increments — the authoring API shipped
first and moved nobody — and is worth something now only because the delivery
branch and this picker landed together.*

---

# Epic 9 — Sitting the exam

*Was "the whole server side is built, the whole client side is a placeholder".
Now the strongest screen in the product: a server-authoritative clock, one
question at a time, debounced autosave, resume, auto-submit, and answer components
that are keyboard-operable by construction. Three defects remain and one of them
silently costs a candidate marks.*

#### TAK-01 · Open a link and see what I am about to sit
**MUST · BUILT**

As a **candidate**, I want to see the exam's name, length and rules before I
start, so that I do not begin a timed exam by accident.

**Acceptance**
1. Opening a link shows the exam title, question count, time limit and attempts
   remaining, and does not start the clock. *(Built.)*
2. `AttemptsUsed` is not incremented by opening — it moves in `StartAsync`.
   *(Built, and this was a real defect once.)*
3. The page carries the tenant's name and logo, not ours. *(Built — the logo URL is
   minted with a signed grant and, since `3923129`, resolves against the right
   origin.)*
4. Starting is a deliberate action, and the page says the clock begins on it.
   *(Built.)*

**Tests** — *integration*: opening twice does not consume an attempt.
*e2e*: open, read, start, and assert the countdown only then begins.

#### TAK-02 · Be told why a link does not work
**MUST · BUILT**

As a **candidate**, I want to be told whether my link expired, was revoked or is
used up, so that I know whether to ask for a new one.

**Acceptance**
1. Each of expired, revoked and exhausted produces its own message, never a
   generic failure — as do "not published" and "outside the schedule". *(Built,
   with real sentences in both language files.)*
2. An unknown token produces `IMS:ExamLink:Invalid` and reveals nothing about
   whether that token ever existed.
3. Every message is shown in the candidate's language and offers the tenant's
   support address. *(The support address is stored and not shown.)*

**Tests** — *unit*: the reasons. *integration*: an unknown token is
indistinguishable from a wrong one in both response and timing.
*e2e*: three links in three states, three messages, in Arabic.

#### TAK-03 · Start, and resume if I am interrupted
**MUST · BUILT**

As a **candidate**, I want to come back to the same paper if my connection drops,
so that a network failure does not cost me my exam.

**Acceptance**
1. Starting twice on one link resumes the running attempt rather than creating a
   second. *(Built, and enforced by a unique index as well as in code.)*
2. The resumed paper is identical — same questions, same positions, same option
   order. *(Built — the paper is persisted, not regenerated.)*
3. The countdown on resume is computed from the stored deadline, not restarted.
4. A reload with no in-memory session returns to the entry screen to mint a new
   one rather than failing. *(Built.)*
5. `AttemptsUsed` increments once, on the real start.

**Tests** — *unit*: seeded rebuild produces an identical paper. *integration*:
concurrent double-start creates one attempt. *e2e*: start, reload, confirm the same
question and a countdown that did not reset.

#### TAK-04 · Answer one question at a time
**MUST · BUILT**

As a **candidate**, I want to see one question at a time with clear progress, so
that a long paper is not overwhelming.

**Acceptance**
1. One question is fetched per position; the whole paper is never in the browser
   at once. *(Built.)*
2. Progress shows answered against total.
3. Back navigation is available only when the exam allows it, and the control is
   absent rather than disabled when it does not.
4. Requesting a position not on this candidate's form is refused with
   `IMS:Attempt:QuestionNotOnForm`. *(Built.)*
5. **The position the screen asks for is the position the server means.** *(Built,
   since `08f0eb6`. It was not: the sitting screen counts from one — "question 3 of
   20" — and the paper counts from zero, and nothing converted between them, so
   every candidate was served the second question first, the first was unreachable,
   and the last answered "not on this paper". Live, for every candidate, on the one
   screen somebody uses once under time pressure and cannot retry.)*

**Tests** — *integration*: the out-of-form refusal; back navigation honoured.
*e2e*: page through, confirm progress, confirm the control's absence.

*The off-by-one is the cleanest example in this document of why a stub is not a
test. The browser suite could not see it because the stub echoed back whatever
position it was asked for, so it agreed with any client. **A stub that answers
anything proves nothing.** It now refuses an out-of-range position the way the
service does.*

#### TAK-05 · Not lose work
**MUST · BUILT**

As a **candidate**, I want my answer saved as I go, so that a dropped connection
late in the exam does not cost me the work already done.

**Acceptance**
1. An answer is written on save, not only at submit. *(Built — 800 ms debounce,
   plus an explicit flush before navigating.)*
2. Re-answering updates the existing row rather than inserting a second.
3. The saved response is returned when the question is reopened. *(Built.)*
4. A failed save is visible to the candidate and retried, never silently dropped.
   *(Built — a failed save re-queues the response rather than discarding it.)*
5. **Submit does not race a save that is still in flight.** *(Built in `75b534d`.
   Saves are fired and not awaited, which is right while the candidate is moving
   between questions — nobody should wait on the network to turn a page — and wrong
   at the one moment there is no going back. "Finish" sent the save, then sent the
   submit straight after; the two raced, and the submit could arrive first, grading
   the exam **without the answer just written**. Submit now waits for the pending
   save, and **if the save fails there is no submit**: completing an irreversible
   action knowing an answer is missing turns a failed request into a lost mark. The
   answer is held in memory, so another press retries. Auto-submit at time-up waits
   for nothing: the session is over at the server regardless, and delaying the
   result screen helps nobody.)*
6. The unanswered count is honest. *(Built in `a2fbf91` — it used to wait for the
   server's reply, and the submit dialog opened before that arrived, so a candidate
   who had answered everything was told "you have 1 unanswered question". A question
   is now counted as answered the moment it is answered: the candidate knows they
   answered it, and it is not our place to doubt them because our request has not
   come back.)*

**Tests** — *integration*: one `Answer` row per question per attempt after repeated
saves. *e2e*: answer, navigate away, return, see the answer; simulate a failed save
and see the indicator.

*Criterion 5 is recorded as a **reasoned defensive change, not a proven fix**, and
that distinction is the honest part. No test could be built that failed without it:
Playwright serialises its interception handlers and each action takes long enough
for the save to complete before the next press, so the window in which the race
occurs never opens under test. A test that passes with and without the fix is worse
than no test, so it was deleted rather than left to imply a guard it does not
provide. What exists is an indicator, not proof: `journey.spec` had been failing
intermittently at 66.67% — two of three, i.e. the last answer lost — and has not
failed in eighteen re-runs since. The flake may yet have another cause.*

#### TAK-06 · Trust the clock
**MUST · BUILT**

As a **candidate**, I want the countdown to be the real one, so that a slow
machine or a wrong system clock does not cost me time.

**Acceptance**
1. Remaining time is computed from `Attempt.DeadlineAt` on the server and returned
   with every save; the browser assigns what it is told rather than counting its
   own. *(Built.)*
2. Changing the device clock does not change the remaining time.
3. At zero the browser submits; if it does not, the server does — the timeout
   worker sweeps every sixty seconds and is registered. *(Built.)*
4. A browser-side timeout records `TimedOutInBrowser`; a server-side one records
   `TimedOutOnServer`. *(Built.)*

**Tests** — *unit*: `SecondsRemaining` and `IsExpired` at the boundaries.
*integration*: the worker submits and grades an abandoned attempt with the right
reason. *e2e*: move the browser clock forward and confirm the countdown does not
move with it.

#### TAK-07 · Never receive the answer
**MUST · BUILT**

As a **training coordinator**, I want the answer key never to reach the candidate's
browser, so that an exam cannot be passed by reading the network traffic.

**Acceptance**
1. The projected question carries id, text and media URL only; `isCorrect` and
   `weight` are absent from the wire.
2. A rubric's criteria names and weights are sent so the candidate knows what is
   being marked; the reviewer guidance is not.
3. In `Assessment` mode the explanation is not sent before submission.
4. An order-carrying type is never served in its authored order. *(BPR-03.)*
5. Adding a new answer-bearing field to a payload without adding it to the
   projector's explicit copy list leaves it invisible to a taker by default.

**Tests** — *unit*: `TakerQuestionProjectorTests` — 10 cases, asserting on the
serialised wire format rather than on the object. *integration*:
`ContractBoundaryTests` — a DTO reachable by a taker cannot reference the domain.
*e2e*: intercept the network response and assert the absent fields.

#### TAK-08 · Answer with a file or a recording
**MUST · NOT BUILT**

As a **candidate**, I want to upload a document or record a spoken answer, so that
a question that cannot be answered by clicking can still be answered.

**Acceptance**
1. An upload is attached to the answer with its original filename kept for the
   reviewer. *(The fields exist on `Answer`, the save DTO carries `answerBlobName`
   and `answerFileName`, and the sitting screen never sets either.)*
2. Recording uses the browser's microphone with an explicit permission prompt, a
   visible level meter, and the ability to re-record before saving.
3. Size and type limits produce the same named errors as authoring media.
4. An upload that fails does not lose the rest of the attempt.

**Tests** — *integration*: the blob, the filename, the limits. *e2e*: upload a
file; record, play back, re-record, save.

*`ANSWER_INPUTS` registers ten of the thirteen types. `file-upload`,
`audio-response` and `hotspot` fall through to a plain textarea — so the product
ships a 288-line hotspot region editor for the author and a text box for the
person answering. `business-review-2.md` §7 recommends deleting `hotspot`
outright and parking the other two; that recommendation still stands and would
close this story by removing it.*

#### TAK-09 · Sit a sectioned paper
**MUST · NOT BUILT**

As a **candidate**, I want to move through named sections, so that I know where I
am in a four-skills exam.

**Acceptance**
1. Section name and instructions are shown before its first question.
2. Progress is shown within the section and across the exam.
3. Where a section is timed, its countdown replaces the exam's and closing it
   moves to the next.
4. An exam with no sections shows no section chrome at all.

**Tests** — *integration*: section boundaries in the served form. *e2e*: sit a
four-section exam; sit an unsectioned one and assert no section chrome.

*`AttemptStateDto` has no section field, the sitting screen paginates over a flat
list, and the word does not appear anywhere in the delivery folder. This is the
retrofit EXM-06 describes, seen from the candidate's side.*

#### TAK-10 · Pass or fail a gate before the exam
**SHOULD · NOT BUILT**

As a **candidate**, I want to be told immediately if I do not meet the entry
requirement, so that I do not spend an hour on an exam I was never eligible for.

**Acceptance**
1. A qualifying section is presented first and untimed.
2. Failing it ends the attempt with a message that does not disclose the right
   answer.
3. No further question is served after the gate is failed.

**Tests** — *integration*: no `AttemptQuestion` beyond the gate is served.
*e2e*: fail the gate and confirm the exam questions were never fetched.

#### TAK-11 · Submit and be told what happens next
**MUST · BUILT**

As a **candidate**, I want to know whether I have a result or whether someone has
to mark it, so that I am not left refreshing a page.

**Acceptance**
1. Submitting an already-submitted attempt is refused with
   `IMS:Attempt:AlreadySubmitted`. *(Built.)*
2. The confirm dialog names how many questions are unanswered. *(Built.)*
3. When any answer needs a human, the result reports that marking is pending and
   returns no score at all — a provisional score is worse than none. *(Built.)*
4. When grading is complete the result shows score, percentage, pass/fail and the
   competency breakdown. *(Built, and the breakdown is no longer always empty now
   that a question can carry a topic.)*
5. A candidate cannot read another candidate's result by changing an id. *(Built —
   the result is loaded through the session's own attempt, and the attempt is
   re-checked against the session's candidate and tenant.)*

**Tests** — *integration*: the pending case returns no score; the cross-candidate
attempt fails. *e2e*: submit an exam with a written question and read the pending
message; submit an all-objective one and read the score.

#### TAK-12 · Learn from a practice attempt
**SHOULD · BUILT**

As a **candidate**, I want to see what the right answer was and why, so that
practice teaches me something.

**Acceptance**
1. In `Practice` mode each question shows my answer, the correct answer and the
   explanation. *(Built.)*
2. On a weighted question I am told whether I chose the best answer or an
   acceptable one; I am never shown the "penalised" label.
3. None of this is available in `Assessment` mode, at any endpoint. *(Built — the
   gate is on the mode, server-side.)*

**Tests** — *unit*: the three learner buckets. *integration*: mode gates the
endpoint. *e2e*: practice reveals, assessment does not.

#### TAK-13 · Be observed honestly, or not at all
**SHOULD · PARTIAL** — *the payload binds now; the candidate is still not told, and
the off switch still switches nothing*

As a **candidate**, I want to know what is being recorded about how I answer, so
that I am not surveilled without being told.

**Acceptance**
1. When `CollectIntegritySignals` is on, the preview screen says plainly what is
   recorded — pastes, focus loss, timing — before the attempt starts. *(Not built.
   The rules list on the entry screen carries exactly three items: the timer,
   autosave, and one attempt. Nothing mentions observation.)*
2. Signals are recorded and labelled correctly. *(**Built.** The client posts
   `{ type, questionId, magnitude }` against a DTO declaring exactly those, with a
   numeric enum mirroring the server's, so a window-blur is stored as a
   window-blur. It previously posted `{ kind, detail }` — **no field bound**, `Type`
   took its default of enum zero, and every focus loss in the product was stored as
   a paste. Nothing rendered the raw list, which is the only reason it went
   unnoticed.)*
2a. **Paste is noted, not double-reported.** *(Built, and the reasoning is worth
   keeping: the paste flag travels with the next save, where the server records a
   signal only if the pasted text was long enough to be an imported answer.
   Reporting every paste from the browser as well filed a second, unconditional
   observation for somebody pasting a single word — exactly the noise the threshold
   exists to keep out of a marker's report.)*
3. No signal ever ends an attempt, changes a score or blocks an action. *(True, and
   this is the story's whole point.)*
4. When the exam has signals off, nothing is recorded and nothing is claimed.
   *(**Not built**, at either level. `RecordSignalAsync` never consults
   `Exam.CollectIntegritySignals`, and nothing anywhere consults the tenant setting
   of the same name. Both switches are offered, both save, and neither switches
   anything off. A tenant that turned observation off is still observed.)*
5. **Only signal types the product actually produces are declared.** *(Not built.
   Six types are defined — `Paste`, `WindowBlur`, `ImplausibleSpeed`,
   `NoCorrections`, `DevToolsOpened`, `PageReloaded` — each with a name, a
   translation and a plain-language sentence in the marker's report, and **two are
   ever produced**. A marker reading "what was observed" is reading paste and
   tab-switching, and an enum that suggests otherwise is a promise about how
   closely somebody is being watched.)*

**Tests** — *unit*: the DTO binds what the client sends — a contract test, because
this was a contract failure. *integration*: signals off records nothing; no code
path lets a signal change a score. *e2e*: the notice appears before starting.

*Two of the three broken promises remain, and both are consent rather than
mechanics: **the candidate is never told what is recorded**, and **the off switch
does nothing at either level**. In some jurisdictions the second of those is not a
preference. See GRD-06, which renders the report.*

#### TAK-14 · Sit the exam in Arabic
**MUST · PARTIAL** — *the rendering is right; the candidate cannot choose*

As a **candidate**, I want the exam to read correctly in Arabic, so that I am
reading the question rather than decoding the layout.

**Acceptance**
1. Every taker screen renders right to left with logical properties, at a phone
   viewport, with no horizontal page scroll. *(Built — direction and language are
   set at the root before the first route paints, and the document ships
   `lang="ar" dir="rtl"`.)*
2. Numbers, timers and progress indicators read correctly in an RTL context.
3. A mixed Arabic-and-Latin prompt renders without reordering the sentence.
4. Letter-spacing is not applied to Arabic text.
5. **The candidate can choose the language, or the tenant chooses for them.**
   *(Not built. The language switcher lives in the staff shell, and `/exam/**` is
   deliberately outside the shell. The preview response carries no language, and
   the tenant's `DefaultLanguage` setting is stored and never applied to the taker
   — so a candidate gets whatever the browser resolves to and cannot change it.)*

**Tests** — *e2e*: the whole taker journey run in Arabic at a phone viewport, in
the same harness that already catches this for the staff screens; and a second run
asserting the tenant's default language is honoured. No unit or integration layer
— this is a rendering property.

#### TAK-15 · Sit the exam without a mouse or with a screen reader
**MUST · PARTIAL** — *built well by construction; not verified, and three types are unanswerable* · ⚠ constraint

As a **candidate**, I want to complete the exam by keyboard and to hear it read
aloud, so that a disability does not decide my score.

**Acceptance**
1. Every question type is completable by keyboard alone. *(Mostly built and
   deliberately so: real radios and checkboxes inside labels, `fieldset`/`legend`
   grouping, per-item `aria-label` on matching selects, and ordering done with
   arrow **buttons** rather than drag-and-drop — which is the accessible choice and
   was not the easy one. But `hotspot`, `file-upload` and `audio-response` are not
   completable by any input device, keyboard or mouse — see TAK-08.)*
2. Each screen passes an automated accessibility check with no critical or serious
   violations. *(Not built. There is no axe dependency and no accessibility suite
   anywhere in the repository; keyboard operability is only incidentally exercised
   by `getByRole` locators.)*
3. Focus is placed on the question when a new one loads, and the countdown is
   announced politely rather than on every tick. *(Built — `role="timer"` with
   `aria-live="polite"`.)*
4. The page is usable at 400% zoom without horizontal scrolling.
5. The scale control is a single tab stop with arrow-key movement between options.
   *(Not built — each button in the radiogroup is separately tabbable.)*

**Tests** — *e2e*: axe assertions on every taker screen in both languages; a
keyboard-only pass through each question type. No unit layer.

#### TAK-16 · Be given the time I am entitled to
**COULD · NOT BUILT**

As a **candidate**, I want an agreed extra-time allowance applied automatically,
so that an accommodation is not administered by hand.

**Acceptance**
1. A per-candidate or per-link multiplier extends the deadline at start and is
   recorded on the attempt.
2. The applied allowance is visible to the reviewer on the attempt.
3. The candidate is not told they are being treated differently beyond seeing
   their own correct countdown.

**Tests** — *unit*: the deadline computation with a multiplier. *integration*: the
recorded allowance. *e2e*: two candidates, two countdowns.

*The deadline is `now.AddMinutes(exam.TimeLimitInMinutes)` for everyone. A public
buyer's accessibility checklist asks about this by name; see PLT-03.*

---
# Epic 10 — Grading and the reviewer's queue

*Automatic grading is solid and resilient. The queue and the marking screen exist
and work. What remains is a marker who cannot see the model answer, a mark that
cannot be changed once given, and one question type whose answers are graded
wrong.*

#### GRD-01 · Mark what a machine can mark
**MUST · BUILT**

As a **training coordinator**, I want objective questions marked the moment an
exam is submitted, so that a class of forty does not wait on me.

**Acceptance**
1. Every registered grader runs over the attempt in one pass; questions are loaded
   in one query, not one per answer.
2. All thirteen types resolve to a grader — nine objective, four routed to a
   person — and an unregistered type falls to manual rather than to zero.
3. An unanswered question scores zero without a grader or a reviewer.
4. The total is the sum over this candidate's own form, so a shorter form is not
   judged against a longer one's maximum.
5. Passing is a percentage comparison against the exam's `PassingPercentage`.

**Tests** — *unit*: each grader against valid and hostile input, including
Arabic-Indic digits. *integration*: a submitted attempt is scored and closed in one
transaction.

#### GRD-02 · Never lose an answer to a broken grader
**MUST · BUILT**

As a **candidate**, I want a grader that fails to send my answer to a person, so
that a defect in the software does not score me zero.

**Acceptance**
1. A question type with no registered grader is routed to manual review, not
   scored zero.
2. A grader that throws is caught and converted to a manual result, and the
   failure is logged with the answer id and the type.
3. Neither case rolls back the submission; the attempt is submitted, graded as far
   as possible, and present in the queue.
4. No response a candidate can type can leave an attempt submitted, ungraded and
   in nobody's queue.

**Tests** — *unit*: `GradingResilienceTests`. *integration*: a hostile numeric
answer leaves the attempt submitted and queued, not stuck.

*Note the gap this does not cover, and which GRD-10 exists for: a grader that
neither throws nor is missing, but that cannot read the shape the client sends,
returns a confident **wrong** — and a confident wrong is invisible to every
protection in this story.*

#### GRD-03 · Work through what needs a person
**MUST · BUILT**

As a **reviewer**, I want a queue of attempts waiting on me, so that I know what
is outstanding.

**Acceptance**
1. The queue lists only attempts with `NeedsManualReview`, oldest first by default.
   *(Built — and this is now the correct behaviour rather than a hole, because an
   auto-graded attempt has somewhere else to be seen: `/results`. When it did not,
   this filter was the reason a fully automatic exam reached nobody.)*
2. Each row shows exam, candidate, submitted-at and how many answers are pending.
3. It requires `Assessment.Review.ViewQueue`.
4. There is a screen. *(Built — `/review`, and a row opens the marking screen.)*

**Tests** — *integration*: only pending attempts appear; permission; tenant
isolation. *e2e*: the queue, its ordering, and a row opening the marking screen.

#### GRD-04 · Mark against a rubric
**MUST · BUILT**

As a **reviewer**, I want to score each criterion separately with a comment, so
that two reviewers reach the same mark and the candidate can be told why.

**Acceptance**
1. The rubric's criteria are shown with their maximum marks. *(Built — authored in
   the rubric editor, which serves text, file-upload and audio-response.)*
2. Per-criterion marks are stored on the answer as `RubricScores`, and the awarded
   total cannot exceed the question's marks — re-validated server-side rather than
   trusted from the client. *(Built.)*
3. Saving a mark recomputes the attempt total and clears the pending flag when
   nothing is left. *(Built.)*
4. A reviewed attempt leaves the queue.

**Tests** — *unit*: the total cannot exceed the maximum. *integration*: the
recompute and the queue exit — the bug this replaced left every reviewed attempt
in the queue forever, so this needs a regression test. *e2e*: mark, see the total
change, see the row leave the queue.

#### GRD-05 · See what the right answer was
**SHOULD · PARTIAL** — *the server sends it; the marking screen never renders it*

As a **reviewer**, I want the answer key rendered beside the candidate's answer,
so that I am not opening the question in another tab.

**Acceptance**
1. The key is rendered for the question's type. *(Server built —
   `CorrectAnswerRenderer` fills `CorrectAnswer`, and the explanation with it. Both
   are typed on the Angular review model.)*
2. **The marking screen displays them.** *(Not built. `correctAnswer` appears
   exactly twice in `angular/src`: the review service's interface, and the
   *candidate's* practice result screen. `review-attempt.component.html` binds
   neither, in 164 lines.)*
3. On a weighted question it is rendered in four buckets: best, acceptable, not
   credited, penalised.
4. It is never rendered into anything a candidate can reach in `Assessment` mode.

**Tests** — *unit*: rendering per type, and the four buckets. *e2e*: the marking
screen shows the key beside the answer.

*The whole cost of this story is a template binding. The renderer, the DTO, the
transport and the client type are all done — which is exactly the failure shape
this document is trying to catch, one layer higher than usual.*

#### GRD-06 · See how the answer was produced
**SHOULD · PARTIAL** — *the screen is right; what it shows is not true*

As a **reviewer**, I want to see that an answer arrived by paste, or in four
seconds, so that I can weigh it — without being told what to conclude.

**Acceptance**
1. The integrity report lists signals with type, time and magnitude. *(The report
   is built, fetched and rendered as prose observations, with per-answer paste,
   timing and keystroke notes beside each answer. But per TAK-13 every signal
   arrives with its type defaulted to `Paste` and its magnitude null, so the
   reviewer is shown pastes that were window-blurs. The screen is honest; its
   input is not.)*
2. It requires `Assessment.Review.ViewIntegritySignals`, held separately from
   `Grade`, because these are behavioural data about a person. *(Built.)*
3. The screen states that these are observations, not conclusions, and offers no
   action that acts on them automatically. *(Built — there is an explicit lede
   saying so, and no action.)*

**Tests** — *integration*: the separate permission is enforced; and a signal
reported as a focus loss is stored and displayed as a focus loss. *e2e*: a reviewer
with `Grade` but not `ViewIntegritySignals` sees the marking screen without the
report.

*This is the most quietly damaging PARTIAL in the document, because the screen
looks finished and a reviewer has no way to know the labels are wrong. Fix TAK-13
first or take the type off the display.*

#### GRD-07 · Reopen a mark
**SHOULD · NOT BUILT**

As a **reviewer**, I want to change a mark I gave, so that a mistake can be
corrected before a result is issued.

**Acceptance**
1. Re-marking replaces the previous mark and recomputes the total.
2. The previous mark, its author and its timestamp are retained.
3. Re-marking an attempt whose result has been exported flags that the export is
   stale.

**Tests** — *integration*: the recompute and the retained history. *e2e*: change a
mark and see the total move.

*Worse than absent: it is one-way. Marking sets `NeedsManualReview = false`, and
both the queue and the answers endpoint filter on that flag — so a marked attempt
returns an empty answer list and its marking screen renders blank. The component
even contains a `seed()` step commented "so a reopened attempt shows its marks",
which can never run. A marker who mistypes a score has no route back.*

#### GRD-08 · Share out the queue
**COULD · NOT BUILT**

As a **training coordinator**, I want to assign attempts to particular reviewers,
so that two people do not mark the same essay.

**Acceptance**
1. An attempt may be claimed, and a claimed attempt is hidden from other
   reviewers' queues.
2. A claim expires after a set period so nothing is stranded.

**Tests** — *integration*: claim, expiry, visibility. *e2e*: two reviewers, one
attempt.

#### GRD-09 · Know how consistent the marking is
**COULD · NOT BUILT**

As a **training coordinator**, I want to see how closely my markers agree, so that
I can train them.

**Acceptance**
1. Where two people mark the same answer, agreement is reported per reviewer pair.
2. The figure is shown to the tenant rather than kept internal.
3. Nothing is scored by a model without a person confirming it.

**Tests** — *unit*: the agreement statistic. *integration*: a double-marked set
produces the expected figure.

#### GRD-10 · Grade what the browser actually sent
**MUST · BUILT** — *the fill-in-the-blank case is fixed and the safety net is
in place; the general matrix test is not* — `4a43679` · ⚠ constraint

As a **candidate**, I want my answer to be read by the grader in the shape the
exam screen produced it, so that a correct answer is not scored zero because two
halves of the product disagree about a format.

**Acceptance**
1. Every shipped question type has an answer component whose emitted shape is the
   shape its grader parses, asserted by a test that pairs the two.
2. `fill-in-the-blank` is answered through a control that emits a map of blank id
   to typed text. *(**Built.** There is now an input with one box per blank. Before
   it, the type was registered to `text-answer.component`, which emits a bare
   string; `FillInTheBlankGrader` parses a `Dictionary<string, string>`, got null
   from `PayloadJson.Read`, and returned **Wrong** — not manual review. A candidate
   could only score on this type by typing JSON into an exam, which is the
   authoring constraint broken at the worst possible place, and nobody was ever
   going to notice because it did not ask for a person either.)*
3. A response a grader cannot parse is routed to a person, never scored zero.
   *(**Built** — and this is the half that matters more than the specific fix. It
   is the safety net GRD-02 provides for a *thrown* grader and did not provide for a
   *confidently wrong* one. With it in place, the next instance of this shape costs
   a marker's time rather than a candidate's marks.)*
4. No answer component falls back to a textarea for a type the product ships.
   *(Not verified in this revision.)*

**Tests** — *unit*: a matrix test enumerating `QuestionTypes` and asserting, for
each, that the registered answer component's emitted shape parses in the registered
grader — the pairing is the whole point, and neither side alone catches this.
*integration*: a fill-in-the-blank attempt answered through the UI scores full
marks. *e2e*: author one, sit it, answer it correctly, read a non-zero score.

*This is the same failure shape as the missing media route and the unbound
integrity signal: each side is tested in isolation and correct, and the seam
between them is where the product lives. It is the third instance in this
codebase, which makes it a pattern rather than a bug.*

*Still open: **the matrix test in criterion 1 does not exist.** The specific defect
is fixed and the net beneath it is real, but nothing structurally pairs an answer
component with the grader that reads it, so the next divergence will be found the
same way this one was — by a person reading code. That is the whole of what remains
of this story.*

---

# Epic 11 — Results, item health and export

*The largest change in this revision. This epic had no application service at all
and three permission strings with nothing behind them; it now has a roster with
summary figures, an answer sheet with a competency breakdown, a real item-analysis
screen and a CSV export. The break that `business-review-2.md` §2.1 called "the
sharpest on the sellable path" — an auto-graded exam whose result reached nobody
but the candidate — is closed.*

*What is left is one download link that cannot work, statistics that are computed
but not kept, and nothing per section.*

#### RES-01 · See how a class did
**MUST · BUILT**

As a **training coordinator**, I want a roster of everyone assigned an exam and
where they got to, so that I can see the whole class at once.

**Acceptance**
1. Rows cover everyone assigned, distinguishing not started, in progress,
   awaiting review and complete. *(Built.)*
2. Complete rows show score, percentage, pass/fail and an integrity flag count.
3. Filtering by group, exam, form and state combine, with paging. *(Built.)*
4. It requires `Assessment.Results.View`. *(Built and enforced at the service.)*
5. The integrity flag count is shown only to someone entitled to it. *(Built since
   `3923129` — "this candidate pasted four times" is an accusation, and it was
   reaching anybody who could read a score, through both this roster and the CSV.)*
5. The date column shows submitted-at. *(It shows started-at; `SubmittedAt` is on
   the DTO and unused. A one-word fix.)*

**Tests** — *integration*: every state appears, including never-started;
permission; tenant isolation — `ResultsTests` covers the shape. *e2e*: the roster
with the four states.

#### RES-02 · Read one candidate's paper
**MUST · BUILT**

As a **reviewer**, I want to see exactly what one candidate was asked and
answered, so that I can defend the result if it is challenged.

**Acceptance**
1. The sheet shows every question on that candidate's form in the order served,
   with their answer, correctness, awarded against maximum, and any reviewer
   comment. *(Built at `/results/:attemptId`.)*
2. It reflects the paper as served, even after the bank has been edited since.
   *(Built — the paper is persisted per attempt with its frozen scores.)*
3. It shows which named form or which seed produced the paper.
4. It requires `Assessment.Attempts.View`. *(It requires `Results.View`;
   `Attempts.View` is declared and enforces nothing anywhere.)*

**Tests** — *integration*: editing a question after the attempt does not change the
sheet. *e2e*: sit, edit the question, reopen the sheet, confirm it is unchanged.

#### RES-03 · Read a result as a profile
**MUST · BUILT**

As a **training coordinator**, I want a result broken down by competency, so that
I know what to teach rather than only who passed.

**Acceptance**
1. The result shows a percentage per competency, on the staff answer sheet and on
   the candidate's own result. *(Built — two implementations, one each side.)*
2. **It returns data.** *(Built, and this is the change. Both keyed off
   `Question.TopicId`, which was `null` on every question in the product until the
   catalogue and the question form's topic select shipped together. The breakdown
   was correct code returning an empty list on every attempt.)*
3. A question with no competency is counted in the total and excluded from the
   breakdown, and the exclusion is stated. *(There is an explicit empty state; the
   partial-coverage case should say what share is uncategorised.)*

**Tests** — *integration*: the breakdown sums correctly and the uncategorised
remainder is stated. *e2e*: the breakdown on both result screens.

#### RES-04 · Read a result section by section
**MUST · NOT BUILT**

As a **training coordinator**, I want a score per section, so that a placement
result says which class to put the student in.

**Acceptance**
1. Each section shows its own score, maximum and percentage.
2. A section below its minimum is marked as the reason the attempt failed.
3. The overall figure is still shown, secondary to the profile.

**Tests** — *unit*: per-section totals and the minimum rule. *integration*:
persisted per-section scores survive a re-mark. *e2e*: a four-section result.

*No section appears in any results DTO. This is the last link of the EXM-06 chain
and the one a placement buyer actually pays for. The competency breakdown (RES-03)
is now a usable substitute for many purposes and should be sold as the profile
until sections land.*

#### RES-05 · Get the results out
**MUST · BUILT** — *fixed in `3923129`, after this revision first recorded it as PARTIAL*

As a **training coordinator**, I want to export results as a spreadsheet, so that
I can put them where my centre already keeps records.

**Acceptance**
1. Export produces one row per attempt with candidate, exam, form, dates, score,
   percentage and pass/fail. *(Built.)*
2. Arabic text survives the export and opens correctly in a spreadsheet
   application without a manual encoding step. *(Built and deliberately — a UTF-8
   BOM is emitted, fields containing a comma, quote or newline are escaped, and the
   response is `text/csv; charset=utf-8` as a download rather than a JSON string
   for the front end to reassemble.)*
3. It requires `Assessment.Results.Export`, and the button is hidden without it.
   *(Both built.)*
4. Clicking the button downloads the file. *(Built. It was a bare `<a [href]>`
   pointing at an origin-relative path, so the primary action on that screen sent
   the coordinator to the dashboard and lost their filters. It now fetches with the
   token and saves.)*
5. An export of an attempt still awaiting review is marked as such rather than
   showing a partial score.
6. Integrity flag counts are not in the file for anyone who may only read a score.
   *(Built — "this candidate pasted four times" is an accusation, and it was
   leaking through both the roster and the CSV to anybody who could see a
   percentage.)*
7. Per-competency columns. *(Not built — the breakdown exists on screen and not in
   the file. The last remaining item on this story.)*

**Tests** — *unit*: encoding and the pending-marking rule. *integration*:
permission; column set. *e2e*: `live/journey.spec.ts` now reads the CSV at the end
of a real journey. The assertion still worth adding is the one in a **browser** —
click the button and assert a file arrives — because that is the half that was
broken while the endpoint was correct.

#### RES-06 · Compute the item statistics
**MUST · PARTIAL** — *both indices exist; one is never persisted and the other is never reset*

As a **training coordinator**, I want to know which of my questions are working,
so that I can retire the ones that measure nothing.

**Acceptance**
1. `TimesAnswered` and `DifficultyIndex` are updated from graded attempts.
   *(Built — a running mean maintained in `AttemptGradingService.RecordOutcome`.)*
2. `TimesServed` is incremented at form assembly, not at grading, because exposure
   is who saw it rather than who answered it. *(Built.)*
3. `DiscriminationIndex` is computed and stored. *(Half. Discrimination **is**
   computed — top and bottom quartile by total score, per exam, inside
   `GetItemAnalysisAsync` — and it is computed at read time and thrown away.
   `Question.DiscriminationIndex` is assigned nowhere in the solution and is
   permanently null, so `QuestionDto.discriminationIndex` reaches the Angular model
   carrying nothing, and the item-health chip on the question list classifies from
   difficulty alone — which is precisely the pair's one job, telling "hard" apart
   from "the key is wrong".)*
4. A question with too few responses reports "not enough data" rather than a
   meaningless index. *(Built — flags are suppressed under twenty answers, which
   is the right call and is stated in the code.)*
4a. **A group that never answered a question is not scored as though it got it
   wrong.** *(Built in `4a574ab`, and this was a live defect that told authors
   correctly-keyed questions were mis-keyed. Discrimination compares the top and
   bottom quarter of candidates; named papers are assigned per class, so the top
   quarter is routinely everybody who sat Form A — and every Form B question then
   showed strongly negative discrimination, was flagged "nearly always a wrong
   answer key", and was sorted to the top of the list. A whole form's worth. The
   share for a group that answered nothing is now **null**, and discrimination is
   reported as unmeasurable rather than as zero.)*
4b. **The analysis refuses to measure at all when the quartile split means
   nothing.** *(Built — when the cohort's totals sit within five percentage points
   of each other, top and bottom are row order wearing a statistic's clothes.
   Unmeasurable rows sort last rather than first.)*
5. Statistics are per tenant and never aggregate across tenants.
6. Recomputation is idempotent, and a material edit to a question resets its
   statistics. *(Not built — see BNK-12, where the running mean is now
   accumulating across key corrections.)*

**Tests** — *unit*: both indices against a hand-worked example; the small-sample
rule. *integration*: idempotence, tenant separation, and that `TimesServed` moves
on assembly rather than on grading.

*The cheapest correct move is probably to stop pretending the column exists:
persist what `GetItemAnalysisAsync` computes, or delete
`Question.DiscriminationIndex` and let the analysis screen own the number. What
should not continue is a DTO field the UI reads that is always null. **Re-verified
at this revision: `Question.DiscriminationIndex` is still read in four places and
assigned in none.***

*One more thing this epic learned the hard way, worth carrying: **a statistic that
is confidently wrong is worse than one that declines to answer**, because an author
acts on it. The fixed version says "unmeasurable" in three separate situations, and
each of those is a place where the previous version said a number.*

#### RES-07 · See which questions are not measuring anything
**SHOULD · PARTIAL** — *the screen is built and good; a row does not open the question*

As a **teacher**, I want a list of my weakest questions in plain language, so that
I can fix them without knowing what a discrimination index is.

**Acceptance**
1. Questions whose discrimination is at or below zero are listed first, with a
   sentence saying the strongest candidates got this wrong more often than the
   weakest. *(Built — checked before anything else, deliberately, because it nearly
   always means a wrong key. The four `IMS:ItemAnalysis:*` messages exist in both
   languages.)*
2. Questions everyone gets right, everyone gets wrong, or that discriminate weakly
   are listed with their own plain-language reason. *(Built. Over-exposure is not
   among the flags — see RES-08.)*
3. **Each row opens the question's editor.** *(Not built — there is no link on a
   row. The screen tells a teacher which six questions to fix and gives them no way
   to reach one, which is most of the value.)*
4. Nothing is auto-retired; every action is the author's. *(Built.)*
5. The screen shows nothing rather than zeros when there is not enough data.
   *(Built, and strengthened in `4a574ab`: a question whose discrimination cannot
   be measured now says **"غير قابل للقياس"** on the screen and sorts last, rather
   than showing a zero that reads as a verdict.)*
6. Rows are ordered worst first. *(Built, and stated in the code: the point of the
   screen is the questions to fix, and alphabetical order is how they stay
   unfixed.)*

**Tests** — *integration*: each flag fires on its condition; the empty state when
no statistics exist. *e2e*: the list, the sentences in Arabic, and a row opening
the editor.

*`competitive-position.md` calls "these six questions are not measuring anything"
the most credible sentence we can say to an assessment professional. We can now
say it. Criterion 3 is what makes it actionable.*

#### RES-08 · Watch a paper wear out
**SHOULD · PARTIAL** — *both counters are written; neither is a rate, and one reaches no screen*

As a **training coordinator**, I want to know when a form or a question has been
seen by too many people, so that I write a replacement before it stops measuring.

**Acceptance**
1. Exposure is reported as a rate — times served over candidates — not only as a
   raw count. *(Not built. Both counters are single cumulative integers with no
   time dimension, so "wearing out" cannot be distinguished from "used a lot, long
   ago".)*
2. The ceiling is a tenant setting rather than a constant. *(Not built —
   `OverExposedAfterServings = 500` is compiled in. The settings screen already
   exists to host it; see ADM-06.)*
3. Crossing it appears as a publish-time warning naming the questions. *(The
   warning fires — and now genuinely, since `TimesServed` is written — but names no
   question.)*
4. Per-question exposure is visible somewhere. *(Not built — `timesServed` appears
   nowhere in `angular/src`. Form-level `TimesUsed` does show, on the forms screen;
   see FRM-07.)*

**Tests** — *integration*: the setting is read from configuration; the warning
names the items. *e2e*: the warning in the publish panel.

#### RES-09 · Compare candidates
**SHOULD · NOT BUILT**

As a **recruiter**, I want several candidates on one exam side by side, by
section, so that I can decide between them.

**Acceptance**
1. Candidates are ranked by percentage, with per-section columns.
2. Attempts still awaiting review are shown as pending and are not ranked.
3. The view is exportable.

**Tests** — *integration*: pending attempts are excluded from ranking.
*e2e*: the comparison and its export.

*A recruiter story, and the first customer is an academy. The roster (RES-01) with
its summary figures already answers most of it for a class.*

#### RES-10 · Issue a certificate
**COULD · NOT BUILT**

As a **candidate**, I want a document confirming what I passed, so that I can show
it to someone.

**Acceptance**
1. The certificate carries the tenant's name, logo and `CertificateFooter`, and
   ours nowhere.
2. It is issued only for a passed, fully graded attempt.
3. It carries a verifiable reference resolving to the attempt.
4. It renders correctly in Arabic.

**Tests** — *integration*: refused for a failed or pending attempt; the reference
resolves. *e2e*: issue and render in Arabic.

*`TenantBranding.CertificateFooter` is a column with a comment and no consumer.
For a vocational academy this is the artefact the student's employer actually
asks for, which makes it a stronger COULD than its priority suggests.*

#### RES-11 · Take my data with me
**SHOULD · NOT BUILT**

As an **administrator**, I want to export my whole question bank and results, so
that the promise that this is our data is real.

**Acceptance**
1. The export contains every question with its payload, media references,
   competency and statistics, plus every attempt and answer.
2. It is scoped to the requesting tenant and contains no other tenant's row.
3. It is offered without an upgrade, a support ticket or a fee.

**Tests** — *integration*: completeness and tenant scoping — the scoping assertion
belongs alongside `TenantIsolationTests`.

*"The tenant owns the bank" is a differentiator we assert in the schema and cannot
demonstrate. The only export in the product is the results CSV, and RES-05 says
that one does not download.*

#### RES-12 · See a sitting's headline numbers
**SHOULD · BUILT** — *new; shipped and never written down*

As a **training coordinator**, I want the figures for a whole sitting at the top
of the roster, so that I can answer "how did they do" without reading forty rows.

**Acceptance**
1. The roster shows how many sat, how many passed, how many failed, how many are
   awaiting marking and how many never started. *(Built — a summary endpoint
   distinct from the list, so the figures cover the whole filtered set rather than
   the current page.)*
2. It shows the mean and the median percentage. *(Built — the median matters here
   and its presence is a good sign: a mean alone hides a bimodal class.)*
3. The figures obey the same filters as the roster beneath them.
4. Awaiting-marking attempts are excluded from the averages rather than counted as
   zero. *(Assumed; needs a test.)*

**Tests** — *integration*: the figures against a hand-built set with one pending
attempt, asserting it is excluded from the mean. *e2e*: filter the roster and watch
the tiles follow.

---

# Epic 12 — The tenant's own face

*Was a table with no service, no endpoint and no screen. Now a settings screen
writing nine values through a real endpoint — of which the exam entry page reads
two and nothing reads the other seven. This epic is the clearest example in the
document of a control surface that outran its mechanisms.*

#### BRD-01 · Put our name on it
**MUST · BUILT** — *fixed in `3923129`, after this revision first recorded it as PARTIAL*

As an **administrator**, I want to set our organisation's name and logo, so that
the people we invite see us rather than a platform they have never heard of.

**Acceptance**
1. Name, alternate-language name, logo, icon, brand colour, certificate footer and
   support email are saved as one record per tenant. *(Built — `/settings`, through
   `PUT api/assessment/settings`, correctly scoped tenant-or-global.)*
2. An organisation operating in one language is not required to invent a second
   name.
3. A tenant with no branding falls back to a neutral default and never shows
   another tenant's. *(Built — the product name stands in, and a failure to load
   costs the branding and nothing else.)*
4. The name appears in the staff shell. *(Built.)*
5. The logo appears. *(Built — the shell now fetches it through `MediaService` with
   the token and binds an object URL. It was a broken-image icon in the top-left
   corner of every staff screen the moment a tenant uploaded one.)*

**Tests** — *integration*: one row per tenant; fallback; isolation. *e2e*: set a
name and logo and assert both render — the logo assertion is the one that matters
and still does not exist.

*The brand colour is a separate story and is still applied to nothing: see
BRD-02.*

#### BRD-02 · Refuse a colour that will fail silently
**MUST · PARTIAL** — *and the colour fails silently in the strongest sense: it is applied to nothing*

As an **administrator**, I want a bad colour rejected when I enter it, so that I
do not end up looking unbranded with no explanation.

**Acceptance**
1. Only `#rgb` or `#rrggbb` is accepted. *(**Built** — `TenantSettingsDto` carries
   a `[RegularExpression]` on `BrandColor`, so the write is refused at the model
   boundary rather than stored and discovered later.)*
1a. **The colour is validated again before it reaches an HTML attribute.**
   *(Built in `4e59b1a`, and this one is a security property rather than a tidiness
   one. `InvitationEmail` re-checks the value against a hex pattern and falls back
   to a default if it does not match, because a colour field that accepts arbitrary
   text accepts `red; background-image:url(...)` — a tracking pixel delivered over
   our signature to the inbox of somebody we have never met. The tenant's
   administrator is trusted with their own organisation, not with their
   candidates' mail.)*
2. Rejection is at the point of entry, with a message, not a silent fallback.
   *(Partly — the server refuses, and the native colour picker cannot produce a bad
   value, so the message has not been needed. A hand-typed value still fails at the
   API rather than in the field.)*
3. The colour is chosen from a picker as well as typeable. *(Built — a native
   colour input, which is also why criterion 1 has not bitten yet: the control
   cannot easily produce a bad value, so the validation gap is latent rather than
   live.)*
4. Derived hover, active and subtle variants keep their contrast ratios whatever
   colour is chosen, with a test at a very light and a very dark brand colour.
   *(Not built — no contrast derivation exists anywhere.)*
5. **The saved colour changes how the product looks.** *(Not built, and re-checked
   at this revision. `brandColor` appears in `angular/src` only in the settings
   feature and its interface; no CSS custom property is ever set from it. The one
   place the colour is now read is the invitation email's start button. So an
   administrator picks a colour, saves it, sees "saved", and the only thing that
   changes is a button in a message they will never see.)*

**Tests** — *unit*: `IsUsableColor` across valid, short, long, non-hex and null;
the contrast derivation at both extremes. *integration*: the service rejects.
*e2e*: set a brand colour and assert a token-driven surface actually changes.

#### BRD-03 · Carry the branding to where it matters
**MUST · PARTIAL** — *the exam entry page and the invitation now carry it; the
result page and the shell do not*

As a **candidate**, I want the exam page and the invitation to carry the
organisation that invited me, so that it does not read as a phishing attempt.

**Acceptance**
1. The link preview and the exam page carry the tenant's name and logo. *(Built,
   and done properly: the logo URL is minted as a signed media grant, so an
   anonymous candidate fetches it without an account and without opening the
   container.)*
2. **The invitation email carries them.** *(**Built** in `4e59b1a` — the name in
   the subject and in both language bodies, and a start button in the tenant's
   colour. A tenant that has not named itself gets a sentence that reads correctly
   with no name, rather than a placeholder standing in for one. Built as a pure
   function separate from sending, so what reaches a candidate is verifiable without
   a mail server: ten tests. **The logo is deliberately excluded**: it is served
   behind a signed grant a mail client does not carry, so it would arrive as a
   broken image — worse for trust than no image. The name and the colour need no
   authorisation to render.)*
3. The result page and the certificate carry them. *(Not built; there is no
   certificate at all.)*
4. The brand colour flows through the token layer to all of them. *(Not built — the
   colour reaches the invitation email and nothing else; see BRD-02 criterion 5.)*
5. The support address shown during an exam is the tenant's, not ours. *(Stored,
   not shown.)*
6. None of these surfaces requires a login to render the branding correctly.
   *(Correct by design, and the grant mechanism is the reason.)*

**Tests** — *integration*: the rendered email body carries the tenant's name and
support address. *e2e*: open a link as an anonymous visitor and assert the logo,
the name and the colour.

#### BRD-04 · Speak the tenant's language everywhere
**SHOULD · PARTIAL** — *the words are editable and rendered nowhere*

As a **candidate**, I want the exam to use my centre's vocabulary, so that a
student is not addressed as a candidate.

**Acceptance**
1. Taker-facing text uses `CategorySet`'s subject vocabulary.
2. Staff screens do the same — the sidebar, the candidate list, the groups screen.
3. Where the tenant has set only one language's labels, the other falls back to
   that one rather than to the platform default.

**Tests** — *integration*: the fallback. *e2e*: rename to "Student" and assert both
the staff screens and the taker screens follow.

*Same evidence as CAT-01, from the reader's side rather than the editor's: the
vocabulary is reachable only from the catalogue feature, and every other screen
resolves fixed localisation keys. A centre that renames "Candidates" to "Students"
sees the change nowhere.*

#### BRD-05 · Preview the branding before it is live
**COULD · NOT BUILT**

As an **administrator**, I want to see what a candidate will see, so that I can
check the logo and colour before anyone is invited.

**Acceptance**
1. A preview renders the link page and the invitation with the unsaved values.
2. The preview cannot send an email.

**Tests** — *e2e*: change a colour, preview, confirm nothing was saved or sent.

*Worth noting the ordering: a preview of a colour that is applied to nothing and a
logo that does not load would be a preview of the defects rather than of the
brand. BRD-01, BRD-02 and BRD-03 come first.*

---
# Epic 13 — Access and administration

*The users screen and the settings screen both shipped, which retired the last two
dead navigation links. Two things in this epic report success and do nothing: a
staff password change, and seven of the nine tenant settings.*

#### ADM-01 · Give people only what they need
**MUST · BUILT**

As an **administrator**, I want permissions grouped by what a person does, so that
a marker is not given the answer keys to the whole bank.

**Acceptance**
1. The tree covers exams, questions, candidates, groups, assignments, attempts,
   review, results, catalogue, users and administration.
2. `Questions.View` is a real privilege because answer keys live behind it.
3. `Review.ViewIntegritySignals` is separable from `Review.Grade`.
4. `Exams.Publish` is separable from `Exams.Edit`.
5. The seeded administrator role is granted by walking the definition tree rather
   than a hand-written list, so adding a permission cannot silently lock the
   administrator out of a new screen. *(Built, and a good decision — the last
   release shipped a service authorising against a policy name nobody had defined,
   and ASP.NET answers an undefined policy with a 500, so a permission mistake
   presented as a broken screen.)*

**Tests** — *integration*: each permission gates its own endpoint. *e2e*: log in
as a role holding only `Review.Grade` and confirm the exam screens are absent.

6. **Five roles exist and each is a different job**, seeded per tenant by
   `SeedAssessmentRolesAsync`: `Admin` (65 permissions), `Coordinator` (25),
   `Author` (14), `Marker` (4), `Observer` (6). *(Built in `816b0b2`.)*
7. A role holding a leaf permission also holds every permission it hangs from,
   because ASP.NET **ANDs** class-level and method-level `[Authorize]`. The seeder
   expands each leaf by walking the definition tree rather than by splitting on
   dots — `Assessment.IdentityManagement.Users.View` has three dotted prefixes and
   only two of them are permissions. *(Built.)*
8. A refusal is observable. *(Built — `angular/e2e/live/roles.spec.ts` is the first
   test in this project that watches a role be **refused**.)*

*Six declared permissions enforced nothing when the previous revision opened. All
six are now resolved. Five were closed rather than deleted, which was the better
outcome: `Users.ManageRoles` is now checked — and only when the role list actually
changes, since anyone who could edit a colleague's phone number could otherwise
make themselves an administrator; `Assignments.SendEmail` now behaves like a
permission; and `Attempts.View`, `.ForceSubmit` and `.Delete` are enforced by the
attempt monitor (ASG-08). The sixth, `Administration.Access`, was **removed**: it
promised "may reach the staff application" and guarded nothing, because everybody
who can sign in is staff and being signed in is what the shell already requires. A
permission that enforces nothing is a promise the administration screen makes and
the product does not keep.*

*Until `816b0b2` there was one role — `Admin`, holding everything — which means no
permission in this product had ever been exercised as a **restriction**, only ever
as a grant that was always present. A permission that is only ever granted is not a
permission; it is a checkbox. Two real defects surfaced the moment somebody tried
to seed a role per tenant: `IdentityRole` leaves the tenant id null and nothing
filled it in, so every role created inside `ICurrentTenant.Change` was written as a
**host** role that the guard checking for it could not then see — 19 duplicates
each of two roles and four accounts accumulated in the host while every tenant
stayed role-less; and `/api/assessment/results/export` answered **500 instead of
403**, because it returns `IActionResult` and ABP's exception filter converts an
authorisation exception to 403 only for object results. A test asserting merely
"not 2xx" would have passed.*

*Also closed in an earlier pass: the seeder re-granted every permission on every
start, which looked idempotent and was not — a deliberately revoked permission came
back after the next deployment. Each is now offered once and remembered.*

*What the permission tree could not express is recorded in `business/roles.md`:
item analysis cannot be separated from the roster, a role cannot carry an Arabic
name, `Assignments.SendEmail` cannot be separated from `Create` at the route, and
there is no list-of-assignments endpoint.*

*Also closed in the same pass: the seeder re-granted every permission on every
start, which looked idempotent and was not — a deliberately revoked permission came
back after the next deployment. Each is now offered once and remembered.*

#### ADM-02 · Do not offer what cannot be opened
**MUST · BUILT** — *seven dead links became two, then none, in `3923129`*

As a **reviewer**, I want the navigation to show only what I can reach, so that I
am not sent to a dead end.

**Acceptance**
1. A navigation entry the user lacks permission for is not rendered, and a section
   with nothing visible in it is dropped rather than left as an empty heading.
   *(Built.)*
2. Every rendered entry resolves to a registered route. *(Built. Seven of eleven
   sidebar destinations went nowhere a month ago; two survived into this revision
   and were fixed while it was being written — `/assignments` gained an index route
   that asks which exam, which is the question the route implies, and the user
   menu's "My profile" pointed at a module deliberately not registered and is
   gone.)*
3. **Permissions are read as they arrive, not once at construction.** *(Built, and
   this was the subtler half. In a zoneless application the sidebar read the
   permission set once, at construction, so a user whose configuration had not
   landed yet saw nothing but Dashboard — permanently. The same bug hid the exam
   form's Save button and the dashboard's cards.)*
4. A role that passes the route guard can use the screen it reaches. *(Built. Class
   and method `[Authorize]` combine with AND rather than override, so a "manage the
   classes" role passed the guard, watched the screen mount, and had every request
   refused. The catalogue and results permissions are now nested so that a workable
   role can be expressed at all.)*
5. The gating is consistent between the menu and the route. *(Still not, in one
   place: the Settings **link** is hidden without `Administration.ManageSettings`
   while the Settings **route** is deliberately ungated so it can render read-only.
   The read-only mode is unreachable from the menu by exactly the people it was
   built for. A one-line fix, and the only thing keeping this story from being
   unqualified.)*

**Tests** — *e2e*: enumerate every rendered navigation link and every user-menu
link and assert each navigates somewhere that is not the wildcard redirect. This is
the story's whole point, so the assertion must be exhaustive rather than sampled —
had it existed, it would have failed on seven links a month ago and on two last
week.

#### ADM-03 · Keep tenants apart
**MUST · BUILT**

As an **administrator**, I want to be certain another organisation cannot see our
data, so that the product can be sold to two competitors at once.

**Acceptance**
1. Every entity under `Assessment` implements `IMultiTenant`, asserted by
   reflection so a new entity cannot forget.
2. A query as one tenant returns no row belonging to another, including through
   `ExamLink` — written as tenant A and read as tenant B through the real
   repositories, so the filter is proved attached rather than merely declared.
3. Where the filter must be disabled for an anonymous taker, the attempt is loaded
   through the session's own claims and re-checked against the session's candidate
   and tenant, never by a caller-supplied id.

**Tests** — *integration*: `TenantIsolationTests`, extended for each new entity —
`ExamSection`, `ExamForm`, `ExamFormQuestion`, `Category`, `Level`, `Topic` and
`TenantBranding` should each be added as they land.

*This is the most honourably covered area in the codebase and the only one where
the tests would catch a regression that mattered commercially.*

#### ADM-04 · Keep the contexts apart
**SHOULD · BUILT**

As an **administrator**, I want the module boundaries enforced by the build, so
that the structure survives contact with a deadline.

**Acceptance**
1. A cross-context entity reference that points upward fails the build, checked by
   reflection against an allow-list that defaults to deny.
2. A contract referencing the domain fails the build.
3. The failure names the offending type and the rule.

**Tests** — *unit*: `ModuleBoundaryTests` and `ContractBoundaryTests`.

#### ADM-05 · Manage staff accounts
**SHOULD · BUILT** — *the password no-op is closed; two smaller gaps remain* —
`b07d970`

As an **administrator**, I want to create staff users and set their roles, so that
a new coordinator can start work.

**Acceptance**
1. Users are created with roles set before the first save, so an account is never
   briefly role-less. *(Built at `/users`, and the roles are on the form rather
   than behind a second screen, which suits how the decision is actually made.)*
2. Roles are edited as a whole list rather than a diff, and changing them requires
   `Users.ManageRoles` — checked only when the list actually changes, so editing a
   colleague's phone number is not a route to making yourself an administrator.
   *(Built, and this was an open privilege-escalation hole until `3923129`.)*
3. Users are deleted. *(Built.)*
4. **A password can be reset.** *(Built. Blank means "leave it alone"; a value
   means `RemovePasswordAsync` then `AddPasswordAsync`, both with their results
   checked. The old password stops working, which was the broken half.)*
5. **Editing anything else does not require re-typing a password.** *(Built. The
   `[Required]` lived on a DTO carrying both create and update, so correcting a
   colleague's phone number answered "the password field is required" — for a field
   the screen itself labels "leave blank to keep the current one". The screen was
   telling the truth and the server was lying. The rule moved into `CreateAsync`
   where it belongs.)*
6. **A phone number with a country code is accepted.** *(Built. It was capped at
   ten characters beside a comment claiming sixteen — `+966501234567` is thirteen —
   and it was implicitly *required*, because nullable reference types are on and
   ASP.NET Core reads a non-nullable `string` as a required field with no attribute
   to warn you.)*
7. **An account can be deactivated.** *(Not built — no `IsActive`, no lockout. The
   only removal is a hard delete, which is the wrong tool for "this person has
   left".)*
8. An administrator cannot remove their own last administrative role. *(Not
   built — no self-lockout guard exists.)*

**Tests** — *integration*: four exist — a new account is refused with no password,
an edit with no password keeps the old one, an edit with a password replaces it
**and the old one stops working**, and a number with a country code is accepted.
Still needed: the self-lockout refusal. *e2e*: create a user, assign a role, log in
as them.

*Criterion 4 is the reason `tools/probe-round-trip.js` exists, and the reason it is
worth reading about even if you never run it. The password was read from the form,
passed validation, carried in the DTO to `UpdateAsync` — and then used by nobody.
No failure, no log line, **and a 200**. This class of defect is invisible to the
tests that usually guard a write, because those check the status code and the
status code was correct; only sending a value and reading it back finds it. The
probe does exactly that for every field a coordinator can edit, and reports every
one that came back unchanged. It reports and does not judge — a field may be
deliberately unreadable — but it narrows "somewhere among forty fields" to three
lines worth looking at. Its current count is zero.*

#### ADM-06 · Configure the tenant
**COULD · PARTIAL** — *nine settings are written; four are read*

As an **administrator**, I want tenant-wide settings in one place, so that
thresholds are not compiled into the product.

**Acceptance**
1. Settings are saved through **one** endpoint, scoped to the tenant or to the
   host, and written with an invariant culture so a decimal does not change meaning
   with the server's locale. *(Built — the culture detail is the kind of thing that
   only shows up in production, and it was thought about. Two rival services,
   `SystemGeneralSettingsAppService` and `SelfRegistrationSettingAppService`, were
   deleted rather than guarded: the first carried no `[Authorize]` at all and ABP
   generates a conventional controller for every application service, so
   `PUT /api/app/system-general-settings` was **an anonymous write that let anybody
   rename the organisation without signing in**. A duplicate source of truth is how
   one of them ends up forgotten; the route smoke test now fails if either comes
   back.)*
2. The screen is readable by anyone signed in and writable only with
   `Administration.ManageSettings`, enforced on both sides. *(Built — every input
   carries a disabled binding and the service carries the attribute. Undermined by
   the menu hiding the link from the read-only audience; see ADM-02.)*
3. **A changed setting takes effect.** *(Four now do. `OrganizationName` is read by
   the exam entry page **and** the invitation email; `LogoBlobName` by the exam
   entry page; `BrandColor` by the invitation email and nothing else. The remaining
   five — `DefaultLanguage`, `TimeZone`, `DefaultPassingPercentage`,
   `ShowResultToCandidate` and `EnableSelfRegistration` — are persisted and read by
   nothing outside the settings screen itself. Verified by grepping each constant:
   the only hits are `TenantSettingsAppService` reading its own writes.*

   *`CollectIntegritySignals` deserves naming separately, because it is not merely
   unread — it is unread **twice**. The tenant-level switch is read by nothing, and
   the per-exam switch (`Exam.CollectIntegritySignals`) is persisted, offered on the
   exam editor with a hint explaining what it does, and never consulted by
   `RecordSignalAsync`. Turning it off, at either level, turns nothing off. That is
   a consent problem rather than a configuration one; see TAK-13.)*
4. The exposure ceiling (RES-08) and the file size limit are settings rather than
   constants. *(Not built — both are compiled in, and this screen is where they
   belong.)*
5. Each has a documented default used when unset.

**Tests** — *integration*: for each setting, a test that the code which uses it
reads it — which is the test that turns this list from seven dead values into
seven features. *e2e*: change the exposure ceiling and see the publish warning
change.

---

# Epic 14 — How the product behaves everywhere

*Cross-cutting. Three stories here are the ones that keep catching the rest of the
document: PLT-07 on what the tests do not reach, and the two new ones — PLT-09 on
the URLs the browser fetches for itself, and PLT-10 on the routes a client calls.*

#### PLT-01 · Never make anyone learn syntax
**MUST · PARTIAL** — *the authoring half is kept; the answering half is broken* · ⚠ constraint

As a **teacher**, I want to operate the whole product without knowing any code, so
that I am not dependent on someone technical to set an exam.

**Acceptance**
1. A test enumerates `QuestionTypes` and asserts each resolves to a registered
   payload editor. *(Passes — 13 of 13.)*
2. The raw-payload textarea does not render for any shipped type. *(Built.)*
3. No field in the product accepts a regular expression, a JSON document, an HTML
   fragment or a template placeholder as author input. *(Built.)*
4. Hotspot regions are drawn on the image; `X`, `Y`, `Width` and `Height` are never
   rendered as inputs. *(Built — the editor does the coordinate arithmetic
   internally.)*
5. A fill-in-the-blank blank is created by selecting a word and pressing a button;
   no placeholder syntax is typed. *(Built for the author.)*
6. An ordering question's correct order is set by dragging or by arrow buttons;
   `CorrectPosition` is never typed. *(Built.)*
7. A matching question's pairs are entered as adjacent rows; no identifier is
   typed. *(Built.)*
8. Catalogue codes are generated from the name and never required. *(Built.)*
9. **The same rule holds for the person answering.** *(Broken. Three of thirteen
   types — `hotspot`, `file-upload`, `audio-response` — have no answer control and
   fall back to a plain textarea; and `fill-in-the-blank` is registered to a
   textarea whose bare string its grader cannot read, so the only way to score on
   it is to type JSON into an exam. See TAK-08 and GRD-10.)*

**Tests** — *unit*: the enumeration in (1), and a matching enumeration over answer
components. *e2e*: one spec per question type authoring through controls only and
asserting no raw field, no coordinate field and no typed placeholder appears
anywhere on the screen; then one spec per type **answering** the same way. This
story's value is entirely in the assertions being exhaustive rather than
representative, and the answering half of that sweep has never been run.

#### PLT-02 · Read correctly in Arabic
**MUST · BUILT**

As a **teacher**, I want the whole product to work in Arabic, so that it is not an
English product with Arabic pasted in.

**Acceptance**
1. Layout uses logical properties throughout; no screen scrolls the page
   horizontally at a phone viewport in Arabic. *(Built — 112 logical-property
   declarations against two physical ones, and no-horizontal-scroll assertions on
   the assignments, candidates, question-list and review screens.)*
2. The font stack is Arabic-first, not an English stack with a fallback.
3. Letter-spacing is not applied to Arabic text.
4. Switching language changes the text as well as the direction, in one operation —
   the session, the document direction and the translations together, because doing
   two of the three was the bug this replaced.
5. Every screen is covered, not only the ones built first. *(The Playwright config
   sets the locale to Arabic for every project, so a new screen is covered by
   default rather than by remembering.)*

**Tests** — *e2e*: the RTL-at-phone-viewport pass, extended to each new screen as
it lands. Three real defects were found this way, including a table that scrolled
the whole page sideways, so the harness stays authoritative.

*This remains the strongest differentiator in the product and the only one no
competitor can retrofit cheaply. Nobody has shown us a competitor whose tests run
in Arabic.*

#### PLT-03 · Meet the accessibility standard the buyer names
**MUST · PARTIAL** — *the implementation is careful; there is no verification at all*

As an **administrator**, I want to answer a public-sector accessibility question
truthfully, so that we are not disqualified from a bid.

**Acceptance**
1. Every screen passes an automated check with no critical or serious violations,
   in both languages. *(Not built. There is no axe dependency and no accessibility
   suite in the repository — a grep for `axe`, `wcag` or `a11y` returns nothing.)*
2. Keyboard operation covers every interactive control. *(Largely true by
   construction rather than by test: a skip link to a focusable `main`,
   `role="menu"` on the user menu, `role="status"`/`role="alert"` on the shared
   data-state component, `role="toolbar"` and `role="textbox"` on the rich-text
   editor, native controls in the answer components. Three question types are not
   operable by anything — TAK-08.)*
3. The product is usable at 400% zoom without horizontal scrolling.
4. The compliance page names EN 301 549 / WCAG 2.1 AA, because that is the phrase
   on the buyer's checklist, while the build targets 2.2 AA. *(Not built.)*

**Tests** — *e2e*: axe assertions across every route in both languages; a
keyboard-only traversal. The work here is one dependency and one loop over the
route table; the effort is small and the claim it licenses is worth a bid.

#### PLT-04 · Tell people what went wrong
**MUST · BUILT**

As a **candidate**, I want to be told what happened in words, so that I know what
to do next.

**Acceptance**
1. Every business failure raises a named error code, not a status code.
2. Every code resolves to a localised message in Arabic and English.
3. No raw code is ever displayed to a user.
4. A new code without a localisation entry fails a test. *(Built —
   `ErrorCodeCoverageTests` scans every `IMS:` literal under `src/` and checks it
   against both files, and a second test asserts the two files carry identical key
   sets. The two `ExamForm` codes that were missing are now present.)*

**Tests** — *unit*: the two coverage tests above. *e2e*: trigger three failures and
read three sentences.

*This is the model the rest of the document should copy: a rule stated, then a
test that fails the build when the rule is broken, rather than a review that finds
it later.*

#### PLT-05 · Never hand over the answer
**MUST · BUILT**

As a **training coordinator**, I want a structural guarantee that answer keys stay
on the server, so that the exam is worth setting.

**Acceptance**
1. No DTO reachable by a taker references the domain.
2. The projector copies an explicit field list; a new payload field is invisible
   to a taker unless it is added deliberately.
3. Recorded option order is stored for every type whose order carries the answer,
   regardless of the exam's shuffle setting, on **every** path that builds a paper.
   *(Now true of the named-form path too — see BPR-03.)*
4. The tests assert on the serialised wire format, not on the object, because the
   object is not what leaks.

**Tests** — *unit*: `TakerQuestionProjectorTests`. *integration*:
`ContractBoundaryTests`. *e2e*: intercept every taker network response across a
whole attempt and assert no forbidden field appears in any of them.

#### PLT-06 · Survive a hostile answer
**MUST · BUILT**

As a **training coordinator**, I want no answer a candidate can type to break
grading, so that a candidate who knows they have failed cannot make the attempt
disappear.

**Acceptance**
1. Every grader handles null, empty, malformed, oversized and numerically extreme
   input without throwing.
2. Arabic-Indic digits are accepted where digits are expected.
3. A grader that does throw sends the answer to a human and leaves the attempt
   submitted and queued.
4. Candidate free text is never rendered as HTML — the review screen renders it in
   a `pre`, and every `innerHTML` binding in the app carries server-sanitised
   authored text.

**Tests** — *unit*: thirteen graders against a shared battery of hostile inputs.
*integration*: the attempt's end state after a grader failure.

*What this story does not cover, and GRD-10 does: an input that is neither hostile
nor malformed, merely the wrong shape, which produces a confident wrong answer
rather than a failure.*

#### PLT-07 · Cover every story from unit to end to end
**MUST · PARTIAL**

As an **administrator**, I want the test pyramid to actually exist, so that this
document's plan is real.

**Acceptance**
1. Every story in this document names its layers, and every named layer exists
   before the story is called done.
2. The critical services have tests. *(Improved. `ExamTakingAppService` and
   `AssignmentAppService` are now exercised through real journeys in
   `NamedFormDeliveryTests` and `ResultsTests`; `ExamSessionTokenService` has
   `MediaGrantTests`; `ResultAppService` and `CatalogAppService` have their own.
   **Still untested:** `ReviewAppService`, `AttemptGradingService`,
   `AssessmentMediaAppService`, `TenantSettingsAppService` and `UserAppService` —
   and the marking path and the grading recompute are not places to be relaxed.)*
3. **Permissions are exercised by something.** *(Not built.
   `InternshipManagementSystemTestBaseModule` still calls
   `AddAlwaysAllowAuthorization()`, so not one `[Authorize]` attribute in the
   solution is executed by any .NET test. Every "requires permission X" claim in
   this document is unverified, and six declared permissions turn out to enforce
   nothing — which is exactly what an unexercised authorisation layer looks like
   from the inside.)*
4. At least one spec per epic runs against a real backend. *(Substantially built,
   and it earned its keep immediately. `angular/e2e/live/journey.spec.ts` is an
   opt-in Playwright project — `--project=live` — because it needs the host up and
   it writes rows. It is deliberately one long journey rather than several small
   ones: catalogue, exam, questions, publish, named paper, candidate, link, **sit it
   in the browser**, submit, then read the roster, the topic breakdown and the CSV.
   The value is in the joins, because that is where this project's defects have
   been — and on its first run it found that every candidate was being served the
   second question first (TAK-04).)*
5. **A stub must be able to fail.** *(Learned and applied. The stubbed suite could
   not see the off-by-one because the stub echoed back whatever position it was
   asked for, so it agreed with any client. A stub that answers anything proves
   nothing; it now refuses an out-of-range position the way the service does, and
   numbers its questions the way the server numbers them.)*
6. There are no Angular unit tests. *(Zero `.spec.ts` files under `angular/src`.
   Given the component logic now carrying real decisions — item health chips,
   derived link states, the answer input registry — this is a growing gap.)*
7. Something runs the tests. *(Not built. No CI configuration, no Dockerfile, no
   compose file, no deployment manifest and no installer exist anywhere in the
   repository, though the Playwright config already branches on a `CI` environment
   variable that nothing sets. The route smoke tool (PLT-10) has the same
   problem.)*

**Tests** — this story is the test plan; its acceptance is measured by the coverage
of the others.

*Roughly 154 test attributes across 22 .NET files, twelve browser spec files and
one live journey, and they are good tests. The problem has never been their
quality; it is that until the live suite landed they all sat on one side of the
seams where this product keeps failing. That is now half-fixed, and the half that
remains is authorisation.*

#### PLT-08 · Be honest about what a score means
**SHOULD · PARTIAL** — *the numbers shipped; the caveats did not*

As a **training coordinator**, I want the product to state the limits of its own
numbers, so that I do not claim more for a result than it can carry.

**Acceptance**
1. Where forms are not equated, the result and the export both state that scores
   are comparable within a form and not across forms. *(Not built — and now
   material, because a class can genuinely sit two different named forms.)*
2. An item statistic computed on too few responses is labelled as provisional
   rather than shown as a number. *(Half built — flags are suppressed under twenty
   responses, but the facility and discrimination figures themselves are still
   printed for a question three people answered.)*
3. An integrity signal is never presented as a conclusion. *(Built — the review
   screen carries an explicit lede saying these are observations. Its labels are
   nonetheless wrong; see GRD-06.)*
4. A pass/fail band is shown with the pass mark it was judged against.

**Tests** — *integration*: the statement appears on the export and the result.
*e2e*: the statement is visible, not hidden behind a tooltip.

#### PLT-09 · Serve a stored file to the browser that renders it
**MUST · BUILT** — *new in this revision, recorded as PARTIAL, and fixed in `3923129` before it was published*

As a **candidate, a teacher and a coordinator**, I want the images, recordings,
logos and downloads the product shows me to actually arrive, so that the product
is not a set of broken-image icons.

**Acceptance**
1. Every URL the product builds for the browser to fetch **directly** — in an
   `img`, `audio`, `video` or a download — resolves against the API rather than
   against the application, the same way `RestService` prefixes every XHR. *(Built.
   Seven places built origin-relative `/api/...` URLs: the media field's preview,
   the taker's question media and stimulus, the taker entry page's tenant logo, the
   shell's tenant logo, the hotspot editor's image, the reviewer's uploaded-answer
   link and the results export. Both `environment.ts` and `environment.prod.ts` put
   the app and the API on different origins with no proxy, so all seven asked the
   wrong server.)*
2. Every such fetch carries its own credential, because a browser-initiated media
   or download request carries no `Authorization` header whatever the interceptor
   does. *(Built, and the two callers correctly get different answers: a
   candidate's paper already carries a signed grant naming one blob and expiring
   with the attempt, so it needed only the right origin; staff are signed in, so
   `core/media.service.ts` fetches their files with the token and hands the page an
   object URL. The export does the same and saves.)*
3. A file the caller is not entitled to returns 404 rather than 403, because
   whether a blob exists is itself worth not saying. *(Built.)*
4. Content type is decided from the stored extension, never echoed from the
   uploader, and `.svg` is served as an octet-stream rather than as an image a
   browser will run script from. *(Built, and well reasoned.)*

**Tests** — *e2e*: `live/journey.spec.ts` covers the media round trip and asserts
that an anonymous stranger holding a blob name gets 404 rather than the file.
**Still worth adding, because it is the assertion whose absence caused this:** a
browser test that renders a question with an image and asserts the image request
returned 200, and one that clicks Export and asserts a file arrives. Every test
that existed proved one side — the component tests stubbed the URL, and the live
suite fetched the blob with an API client carrying a token, which no `img` tag can
do.

*Kept as a story rather than deleted, because the defect class is the point. One
mistake produced seven symptoms across four features, it survived two reviews and
187 passing tests, and it was found by a person reading a URL string. It broke the
opening sales argument (their Google Form lost the chart, and we keep it), the
listening exam a language centre would buy, the tenant's logo, and the results file
the coordinator paid for.*

#### PLT-10 · Prove every route the client calls answers
**SHOULD · PARTIAL** — *new; the tool exists and nothing runs it*

As an **administrator**, I want a check that every route the product calls exists
on a running server, so that a finished service with no controller is caught by
the build rather than by a customer.

**Acceptance**
1. A check authenticates against a running host and calls each route the client
   uses, failing on anything a client would treat as broken. *(Built —
   `tools/smoke-routes.js`. It found two defects on its first run: every media read
   and upload throwing because no BLOB provider had ever been configured, and
   `/api/app/users` returning 500 because a class-level `[Authorize]` named a policy
   that was never defined.)*
2. A route expected to refuse says so, because a 404 from a missing route and a 404
   from a missing row look identical from outside. *(Built — the media check states
   which it expects.)*
3. The route list is complete. *(It covers eleven routes. `api/assessment/take/*`,
   `review/*`, `exam-structure/*`, `assignments/*` and the results export are not
   among them.)*
4. **Something runs it.** *(Still not built, but the reason has narrowed. CI now
   exists (`PLT-11`) and runs the .NET suite and the browser suites — but this tool
   needs a running host and a seeded database, which the job does not have, so it
   is in the same position as the `live` Playwright project. It runs when a person
   remembers, which is the same failure mode it was written to replace, one level
   up.)*

**Tests** — this story is a test. Its acceptance is that it is complete and that it
runs unattended.

*Written because one defect kept recurring: a finished application service with no
HTTP controller reads as done in any inventory that counts services rather than
journeys. It happened four times here — assignments, review, media, and the whole
catalogue — and each time a person reading code found it. The tool is the right
response. PLT-09 is the same lesson one layer further out, and is not yet covered
by anything.*

#### PLT-11 · Run somewhere other than the machine it was written on
**MUST · BUILT** — *new; landed in `41c97d3`*

As an **operator**, I want to deploy this without editing source, so that a
customer's exams do not depend on one developer's laptop.

**Acceptance**
1. **Nothing environment-specific is compiled in.** The exam-session issuer and
   audience, the media-grant issuer and audience, the attachment path and the null
   mail sender are configuration with local defaults, so local development is
   unchanged. *(Built — eight string literals and one `#if DEBUG` removed. The
   attachment path was being computed from `GetCurrentDirectory` while its
   `appsettings.json` key sat dead and unread.)*
2. **The SPA is one image promoted between environments**, not an image per
   environment: it reads `assets/config.json` at boot and overlays it on the values
   compiled in. *(Built.)*
3. Migrations run as their own short-lived container, before the API and not
   inside its startup — with more than one replica, migrate-on-start races itself.
   *(Built.)*
4. **A missing secret stops the stack, naming itself.** *(Built. `ExamSession:SigningKey`
   is required and at least 32 characters, and the host refuses to construct
   without it — the previous `??` fallback signed every token in every environment
   with the SHA-256 of the empty string. A stack that comes up with a blank
   exam-session signing key is worse than one that does not come up.)*
5. Keys that did not exist at all now do: an OpenIddict signing certificate
   (without which tokens die on every restart and two replicas reject each other's),
   forwarded-headers configuration, a data-protection key path, and `/health`.
   *(Built.)*
6. A list separated by commas is trimmed. *(Built — CORS origins and allowed
   redirect URLs were split on the comma with no trim, so
   `"http://a, http://b"` in an environment variable produced an origin with a
   leading space that matched nothing, and nobody said anything.)*
7. `/exam/` is served with `Cache-Control: no-store` and
   `Referrer-Policy: no-referrer`, because the token in the path is the candidate's
   whole credential and the page pulls fonts from a third party. *(Built, in
   `docker/angular/nginx.conf`.)*
8. **Continuous integration builds and tests both halves.** *(Built —
   `.github/workflows/ci.yml`. The `live` Playwright project is excluded, with the
   reason written in the file rather than left unsaid: it needs a running host and
   a seeded database.)*
9. `Max Pool Size=300` is preserved and justified in both places it appears, having
   been raised after a load test exhausted the pool at 150 concurrent candidates.
   *(Built.)*

**Tests** — this story is largely configuration; its assertion is that the compose
stack comes up and `/health` answers, and that CI is green.

**Known gaps, stated rather than claimed as done.** The images were never built —
Docker is not installed on the machine this was written on. And what a real
deployment still needs is enumerated in `deployment.md` §6 rather than implied to be
finished: TLS termination, a real signing certificate, a managed database, shared
storage before a second replica, and a mail relay.

---

# Summary

## By status

| Status | Stories |
|---|---|
| **BUILT** | 69 |
| **PARTIAL** | 31 |
| **NOT BUILT** | 27 |
| **Total** | **127** |

*Pinned to `75b534d`. Five statuses moved to BUILT since the previous revision
(`0842cc9`): `ASG-03` and `ADM-05` from PARTIAL, and `ASG-06`, `ASG-07` and
`GRD-10` from NOT BUILT. Two stories are new, for work that shipped and had never
been written down: `IMP-06` (import a question bank from a spreadsheet) and
`PLT-11` (run somewhere other than the machine it was written on).*

*Five stories were new in the previous revision and remain: `PPL-07` (a class at a
level, in a term), `RES-12` (the roster's headline figures), `GRD-10` (grade what
the browser actually sent), `PLT-09` (serve a stored file to the browser that
renders it) and `PLT-10` (prove every route the client calls answers).*

## By status and priority

| | MUST | SHOULD | COULD | Total |
|---|---|---|---|---|
| **BUILT** | 54 | 14 | 1 | **69** |
| **PARTIAL** | 14 | 15 | 2 | **31** |
| **NOT BUILT** | 9 | 8 | 10 | **27** |
| **Total** | **77** | **37** | **13** | **127** |

## By epic

| Epic | Stories | BUILT | PARTIAL | NOT BUILT |
|---|---|---|---|---|
| 1 · The catalogue and the tenant's vocabulary | 6 | 2 | 3 | 1 |
| 2 · The question bank | 12 | 8 | 1 | 3 |
| 3 · Getting existing exams in | 6 | 2 | 0 | 4 |
| 4 · Exams, sections and publishing | 12 | 6 | 5 | 1 |
| 5 · Blueprints and per-candidate assembly | 7 | 3 | 1 | 3 |
| 6 · Named forms | 8 | 5 | 0 | 3 |
| 7 · People and cohorts | 7 | 4 | 3 | 0 |
| 8 · Assignment and links | 9 | 8 | 1 | 0 |
| 9 · Sitting the exam | 16 | 9 | 3 | 4 |
| 10 · Grading and the reviewer's queue | 10 | 5 | 2 | 3 |
| 11 · Results, item health and export | 12 | 5 | 3 | 4 |
| 12 · The tenant's own face | 5 | 1 | 3 | 1 |
| 13 · Access and administration | 6 | 5 | 1 | 0 |
| 14 · How the product behaves everywhere | 11 | 6 | 5 | 0 |

## What the shape of those tables says

**Fifty-four of the seventy-seven MUST stories are now BUILT**, against fifty at
the last revision and seventeen at the one before. The month closed the catalogue,
the shared bank's route in, named forms end to end, people and classes, the review
queue, the results roster, item analysis, the blueprint editor, staff accounts,
tenant settings, the media path and the export. This revision adds a question-bank
importer, a branded invitation, the three link repairs a coordinator actually
reaches for, five real roles, and a deployable stack.

**PARTIAL has changed character, and this is the finding that matters.** Last time
it meant "the service is finished and there is no screen", and the missing layer
was always Angular. That shape is now gone — the last two instances, the blueprint
editor and the results export, closed while this was being written. What PARTIAL
means today is the opposite: **a screen that is finished and a mechanism that is
not.** A section's clock nothing reads, a qualifying flag nothing consults, a brand
colour applied to nothing, seven tenant settings nobody consumes, a password field
that reports success and changes nothing, an integrity payload that binds to no
field, and a model answer the server sends that the marking screen never renders.
**Ten of the thirty-three PARTIAL stories are a control the user can operate that
does nothing at all.** That is worse than an absent feature: an absent feature
disappoints, and a dead control lies.

**The defects that cost somebody something are all seam defects, and they are one
defect wearing different clothes.** Six instances now, in this codebase:

| Instance | Both sides correct | Nothing crossed |
|---|---|---|
| The media route that no controller declared | service, five callers | no route test |
| The BLOB container with no provider | container, writer | nothing activated it |
| An `[Authorize]` naming an undefined policy | service, permission tree | nothing ran the policy |
| Origin-relative media and export URLs (`PLT-09`) | grant minting, controller | stub answered our own URL |
| Question positions off by one (`TAK-04`) | screen counts from 1, server from 0 | stub echoed any position |
| The blank-filling answer shape (`GRD-10`) | answer control, grader | no test pairs the two |
| The integrity payload's field names (`TAK-13`) | client, DTO | nothing sent one and read the other |
| A staff password read, carried, and never used (`ADM-05`) | form, validation, DTO | tests asserted the status code, which was correct |

Most were found by a person reading code or by a tool that speaks to a running
server; the off-by-one was found by the live suite on its first run, which is the
correct answer to this whole class. **Both `GRD-10` and `TAK-13` are now closed at
the specific instance and open at the general one**: no test structurally pairs an
answer component with the grader that reads it, or a client payload with the DTO it
binds to, so the next divergence will be found the same way these were.

*The reply to this pattern is a family of tools rather than a single test:
`smoke-routes.js` (does every route the client calls answer?),
`probe-round-trip.js` (does an update keep what you sent it?),
`check-localization.py` (is every text key the client asks for defined?), and
`load-test.js` (what does one candidate experience while forty-nine others do the
same thing?). Each was written the day after a defect that no unit test could have
had an opinion about.*

**Epic 12 has one BUILT story out of five**, which is startling for an epic that
shipped a whole settings screen. Everything it writes is real; almost nothing reads
it — and the invitation email, which now reads two of the nine settings, is the
first thing outside that screen ever to read any of them.

**No `[Authorize]` attribute anywhere is exercised by any integration test**,
because the test base allows everything. Three static assembly checks now stand
where the dynamic ones cannot: every application service carries a class-level
`[Authorize]` (the candidate's path being the one exception, named individually
with its reason); every policy named in an attribute is defined; and every defined
permission is enforced somewhere. The third found `Administration.Access` on its
first run — grantable, and guarding nothing — which was removed rather than
enforced. `angular/e2e/live/roles.spec.ts` is where a refusal is actually asserted
against a running server, and it is the first test in this project to do so.

---

# Traceability matrix | مصفوفة التتبّع

Every story, the **screen** a person stands on to exercise it, and the **role**
that holds it. Routes are Angular routes; roles are the five seeded per tenant
(`business/roles.md`). "—" in the role column means the candidate, who has no
account and no role: their link is their entire credential.

كلّ قصّة، والشاشة التي تُمارَس منها، والدور الذي يملكها. و«—» تعني الممتحَن: لا
حساب له ولا دور، ورابطه هو بطاقته.

Where a story is NOT BUILT, the screen column names the screen it *would* belong
to, marked *(none)* when no screen exists at all.

## Epic 1 — The catalogue and the tenant's vocabulary

| Story | Screen | Role |
|---|---|---|
| CAT-01 · Name the vocabulary | `/catalog` (vocabulary dialog) | `Author` · `Admin` |
| CAT-02 · Manage categories | `/catalog` | `Author` (`Catalog.Manage`) |
| CAT-03 · Manage levels within a category | `/catalog` | `Author` (`Catalog.Manage`) |
| CAT-04 · Manage the competency tree | `/catalog` | `Author` (`Catalog.Manage`) |
| CAT-05 · See a value's usage before changing it | `/catalog` | `Author` |
| CAT-06 · Seed a new tenant with a usable catalogue | *(none)* — tenant provisioning | `Admin` |

## Epic 2 — The question bank

| Story | Screen | Role |
|---|---|---|
| BNK-01 · Write a question of any shipped type | `/questions/new` · `/exams/:examId/questions/new` | `Author` (`Questions.Create`) |
| BNK-02 · Refuse a question that cannot be graded | the question editor | `Author` |
| BNK-03 · Format a prompt without writing HTML | the question editor | `Author` |
| BNK-04 · Attach a chart, a recording or a clip | the question editor · `/exam/:token/sitting` | `Author` (`Questions.Edit`) · — |
| BNK-05 · Score an answer by how right it is | the question editor | `Author` |
| BNK-06 · Own a question at the level, not one exam | `/questions` | `Author` |
| BNK-07 · Draw the bank into a candidate's paper | `/exam/:token/sitting` (server-side assembly) | — |
| BNK-08 · Bind several questions to one passage | `/exams/:examId/structure` | `Author` (`Exams.Edit`) |
| BNK-09 · Find a question in a bank of hundreds | `/questions` | `Author` |
| BNK-10 · Duplicate a question | `/questions` | `Author` |
| BNK-11 · Control a question's life cycle | `/questions` | `Author` |
| BNK-12 · Keep item statistics bound to what was asked | `/results/questions` | `Observer` |

## Epic 3 — Getting existing exams in

| Story | Screen | Role |
|---|---|---|
| IMP-01 · Paste an exam in | *(none)* | would be `Author` |
| IMP-02 · Upload a Google Forms export | *(none)* | would be `Author` |
| IMP-03 · Be told which questions lost their picture | *(none)* | would be `Author` |
| IMP-04 · Map an imported exam to the catalogue | *(none)* | would be `Author` |
| IMP-05 · Import candidates from a list | `/candidates` (paste panel) | `Coordinator` (`Candidates.Create`) |
| IMP-06 · Import a question bank from a spreadsheet | `/questions` · `/exams/:examId/questions` (import panel) | `Author` (`Questions.Create`) |

## Epic 4 — Exams, sections and publishing

| Story | Screen | Role |
|---|---|---|
| EXM-01 · Create and edit an exam | `/exams/new` · `/exams/:id` | `Author` (`Exams.Create`, `.Edit`) |
| EXM-02 · Find an exam | `/exams` | `Author` · `Coordinator` · `Observer` (`Exams.View`) |
| EXM-03 · Be stopped from publishing something broken | `/exams/:id` (publish dialog) | `Author` (`Exams.Publish`) |
| EXM-04 · Be warned about what will merely go badly | `/exams/:id` (publish dialog) | `Author` |
| EXM-05 · Take an exam out of circulation | `/exams` · `/exams/:id` | `Author` (`Exams.Publish`, `.Delete`) |
| EXM-06 · Divide an exam into named parts | `/exams/:examId/structure` | `Author` (`Exams.Edit`) |
| EXM-07 · Give a section its own clock | `/exams/:examId/structure` | `Author` |
| EXM-08 · Fail an exam on one section | `/exams/:examId/structure` | `Author` |
| EXM-09 · Turn a candidate away in thirty seconds | `/exams/:examId/structure` · `/exam/:token/sitting` | `Author` · — |
| EXM-10 · Choose whether everyone sits the same paper | `/exams/:id` | `Author` |
| EXM-11 · Practise rather than be judged | `/exams/:id` (mode) | `Author` |
| EXM-12 · Open an exam only within a window | `/exams/:id` (schedule) | `Author` |

## Epic 5 — Blueprints and per-candidate assembly

| Story | Screen | Role |
|---|---|---|
| BPR-01 · Describe the paper as a recipe | `/exams/:examId/blueprint` | `Author` (`Exams.Edit`) |
| BPR-02 · Give every candidate a different but comparable paper | `/exam/:token/sitting` (assembly) | — |
| BPR-03 · Keep the answer out of the paper's shape | `/exam/:token/sitting` (assembly) | — |
| BPR-04 · Compose a paper section by section | `/exams/:examId/blueprint` | `Author` |
| BPR-05 · Fail loudly when a rule starves | `/exams/:examId/blueprint` · assembly | `Author` · — |
| BPR-06 · Prefer questions that have been seen least | assembly | — |
| BPR-07 · Copy a blueprint to another exam | `/exams/:examId/blueprint` | `Author` |

## Epic 6 — Named forms

| Story | Screen | Role |
|---|---|---|
| FRM-01 · Build a named paper | `/exams/:examId/forms` | `Author` (`Exams.Edit`) |
| FRM-02 · Freeze a form for use | `/exams/:examId/forms` | `Author` (`Exams.Publish`) |
| FRM-03 · Retire a form without losing what was sat | `/exams/:examId/forms` | `Author` (`Exams.Publish`) |
| FRM-04 · Sit a fixed form | `/exam/:token/sitting` | — |
| FRM-05 · Spread a cohort across forms | `/assignments/:examId` | `Coordinator` (`Assignments.Create`) |
| FRM-06 · Guarantee a retake differs | `/assignments/:examId` (rotate) | `Coordinator` |
| FRM-07 · Know how worn a paper is | `/exams/:examId/forms` | `Author` |
| FRM-08 · Print a form for review or for paper | *(none)* | would be `Author` |

## Epic 7 — People and cohorts

| Story | Screen | Role |
|---|---|---|
| PPL-01 · Add a person to be assessed | `/candidates` | `Coordinator` (`Candidates.Create`) |
| PPL-02 · Group people into a class or a batch | `/groups` | `Coordinator` (`Groups.Create`) |
| PPL-03 · Find a person | `/candidates` | `Coordinator` (`Candidates.View`) |
| PPL-04 · See one person's history | *(none)* | would be `Coordinator` |
| PPL-05 · Correct a person's details | `/candidates` | `Coordinator` (`Candidates.Edit`) |
| PPL-06 · Remove a person | `/candidates` | `Coordinator` (`Candidates.Delete`) |
| PPL-07 · Run a class as an intake | `/groups` | `Coordinator` (`Groups.Edit`) |

## Epic 8 — Assignment and links

| Story | Screen | Role |
|---|---|---|
| ASG-01 · Send an exam to one person | `/assignments/:examId` | `Coordinator` (`Assignments.Create`) |
| ASG-02 · Send an exam to a whole class | `/assignments/:examId` | `Coordinator` (`Assignments.Create`) |
| ASG-03 · Deliver the invitation | `/assignments/:examId` → the candidate's inbox | `Coordinator` (`Assignments.SendEmail`) |
| ASG-04 · See the state of every link | `/assignments/:examId` | `Coordinator` (`Assignments.View`) |
| ASG-05 · Kill a link that leaked | `/assignments/:examId` | `Coordinator` (`Assignments.Revoke`) |
| ASG-06 · Resend an invitation (as reissue) | `/assignments/:examId` | `Coordinator` (`Assignments.Create`) |
| ASG-07 · Extend an expiry | `/assignments/:examId` | `Coordinator` (`Assignments.Create`) |
| ASG-08 · End someone's attempt | `/results/running` | `Coordinator` (`Attempts.ForceSubmit`); discarding is `Admin` (`Attempts.Delete`) |
| ASG-09 · Choose which form a sitting uses | `/assignments/:examId` | `Coordinator` (`Assignments.Create`) |

## Epic 9 — Sitting the exam

**Screen for every story in this epic:** `/exam/:token` → `/exam/:token/sitting` →
`/exam/:token/result`, outside the shell and outside authentication.
**Role: none — the candidate has no account.**

| Story | Which of the three screens |
|---|---|
| TAK-01 · Open a link and see what I am about to sit | `/exam/:token` |
| TAK-02 · Be told why a link does not work | `/exam/:token` |
| TAK-03 · Start, and resume if I am interrupted | `/exam/:token` → `/sitting` |
| TAK-04 · Answer one question at a time | `/exam/:token/sitting` |
| TAK-05 · Not lose work | `/exam/:token/sitting` |
| TAK-06 · Trust the clock | `/exam/:token/sitting` |
| TAK-07 · Never receive the answer | `/exam/:token/sitting` |
| TAK-08 · Answer with a file or a recording | `/exam/:token/sitting` |
| TAK-09 · Sit a sectioned paper | `/exam/:token/sitting` |
| TAK-10 · Pass or fail a gate before the exam | `/exam/:token/sitting` |
| TAK-11 · Submit and be told what happens next | `/exam/:token/sitting` → `/result` |
| TAK-12 · Learn from a practice attempt | `/exam/:token/result` |
| TAK-13 · Be observed honestly, or not at all | `/exam/:token` (the notice) · `/sitting` (the recording) |
| TAK-14 · Sit the exam in Arabic | all three |
| TAK-15 · Sit it without a mouse or with a screen reader | all three |
| TAK-16 · Be given the time I am entitled to | `/exam/:token/sitting` |

## Epic 10 — Grading and the reviewer's queue

| Story | Screen | Role |
|---|---|---|
| GRD-01 · Mark what a machine can mark | server-side, at submission | — |
| GRD-02 · Never lose an answer to a broken grader | server-side, at submission | — |
| GRD-03 · Work through what needs a person | `/review` | `Marker` (`Review.ViewQueue`) |
| GRD-04 · Mark against a rubric | `/review/:attemptId` | `Marker` (`Review.Grade`) |
| GRD-05 · See what the right answer was | `/review/:attemptId` | `Marker` |
| GRD-06 · See how the answer was produced | `/review/:attemptId` | `Marker` (`Review.ViewIntegritySignals`) |
| GRD-07 · Reopen a mark | `/review/:attemptId` | `Marker` |
| GRD-08 · Share out the queue | `/review` | would be `Admin` |
| GRD-09 · Know how consistent the marking is | *(none)* | would be `Observer` |
| GRD-10 · Grade what the browser actually sent | `/exam/:token/sitting` ↔ the grader | — |

## Epic 11 — Results, item health and export

| Story | Screen | Role |
|---|---|---|
| RES-01 · See how a class did | `/results` | `Coordinator` · `Observer` (`Results.View`) |
| RES-02 · Read one candidate's paper | `/results/:attemptId` | `Coordinator` · `Observer` |
| RES-03 · Read a result as a profile | `/results/:attemptId` | `Coordinator` · `Observer` |
| RES-04 · Read a result section by section | `/results/:attemptId` | `Coordinator` · `Observer` |
| RES-05 · Get the results out | `/results` (export) | `Coordinator` · `Observer` (`Results.Export`) |
| RES-06 · Compute the item statistics | server-side, at grading | — |
| RES-07 · See which questions are not measuring | `/results/questions` | `Observer` (`Results.ViewItemAnalysis`) |
| RES-08 · Watch a paper wear out | `/exams/:id` (publish warning) | `Author` |
| RES-09 · Compare candidates | *(none)* | would be `Observer` |
| RES-10 · Issue a certificate | *(none)* | would be `Coordinator` |
| RES-11 · Take my data with me | *(none)* | would be `Admin` |
| RES-12 · See a sitting's headline numbers | `/results` (summary strip) | `Coordinator` · `Observer` |

## Epic 12 — The tenant's own face

| Story | Screen | Role |
|---|---|---|
| BRD-01 · Put our name on it | `/settings` | `Admin` (`Administration.ManageSettings`) |
| BRD-02 · Refuse a colour that will fail silently | `/settings` | `Admin` |
| BRD-03 · Carry the branding to where it matters | `/exam/:token` · the invitation email | — (the candidate is who sees it) |
| BRD-04 · Speak the tenant's language everywhere | every screen | all roles |
| BRD-05 · Preview the branding before it is live | `/settings` | `Admin` |

## Epic 13 — Access and administration

| Story | Screen | Role |
|---|---|---|
| ADM-01 · Give people only what they need | `/users` · the permission tree | `Admin` (`Users.ManageRoles`) |
| ADM-02 · Do not offer what cannot be opened | the shell's navigation, every route | all roles |
| ADM-03 · Keep tenants apart | every screen and every request | all roles, and the candidate |
| ADM-04 · Keep the contexts apart | the solution's structure | — (an architecture test) |
| ADM-05 · Manage staff accounts | `/users` | `Admin` (`Users.Create`, `.Edit`, `.Delete`) |
| ADM-06 · Configure the tenant | `/settings` | `Admin` (`Administration.ManageSettings`) |

## Epic 14 — How the product behaves everywhere

| Story | Screen | Role |
|---|---|---|
| PLT-01 · Never make anyone learn syntax | every authoring screen and every answer control | `Author` · — |
| PLT-02 · Read correctly in Arabic | every screen | all roles, and the candidate |
| PLT-03 · Meet the accessibility standard the buyer names | every screen | all roles, and the candidate |
| PLT-04 · Tell people what went wrong | every screen | all roles, and the candidate |
| PLT-05 · Never hand over the answer | `/exam/:token/sitting` | — |
| PLT-06 · Survive a hostile answer | `/exam/:token/sitting` → `/review/:attemptId` → the CSV | — · `Marker` · `Coordinator` |
| PLT-07 · Cover every story from unit to end to end | the test suites | — |
| PLT-08 · Be honest about what a score means | `/results/*` · `/exam/:token/result` | `Coordinator` · `Observer` · — |
| PLT-09 · Serve a stored file to the browser that renders it | `/exam/:token/sitting` · `/review/:attemptId` | — · `Marker` |
| PLT-10 · Prove every route the client calls answers | `tools/smoke-routes.js` | — |
| PLT-11 · Run somewhere other than the machine it was written on | the compose stack and CI | — |
