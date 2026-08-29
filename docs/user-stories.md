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
EF configuration and `angular/src/app/`. Where a claim was surprising, the file is
named.

| Status | Means |
|---|---|
| **BUILT** | The actor can complete this today, end to end, in the running product |
| **PARTIAL** | Real working code exists at some layers — a service, an API, an enforced domain rule with tests — but the actor cannot complete the story |
| **NOT BUILT** | Nothing, or at most an entity and a column |

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
- **integration** — an app service against a real DbContext and a real permission
  check: authorisation, tenant filtering, transactions, queries.
- **e2e** — Playwright against the running Angular app: what the person actually
  sees and clicks, in both languages and both directions.

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

One exception, so it is not mistaken for a breach: the `code` question type asks
an author to write code. That is the subject matter, not the tool. The rule is
that the *product* never demands syntax, not that a programming exam can avoid
programming.

---

# Epic 1 — The catalogue and the tenant's vocabulary

*Catalog context. Nothing here has an application service, a DTO or a route. The
four entities exist and are migrated; a tenant cannot presently create a single
category except by writing SQL, which blocks every other epic.*

#### CAT-01 · Name the vocabulary
**MUST · NOT BUILT**

As an **administrator**, I want to set what we call our category axis, the people
we assess and the groups we assess them in, so that our staff and our students
read our words instead of the platform's.

**Acceptance**
1. Saving singular and plural labels for the axis, the subject and the group
   updates `CategorySet` for the tenant and no other tenant's row changes.
2. Every screen that displays the axis renders the saved singular or plural label;
   with the labels set to "Language"/"Languages", no screen shows the word
   "Category".
3. A tenant that has never configured this sees the defaults `Candidate` /
   `Candidates` / `Group` / `Groups` from the entity, not an empty label.
4. Exactly one `CategorySet` row exists per tenant; a second save updates the
   first rather than inserting.

**Tests** — *integration*: one row per tenant, tenant isolation, defaults on
first read. *e2e*: rename to "Language"/"Student"/"Class", then assert those
words appear in the sidebar, the exam editor and the candidate screen in both
Arabic and English. No unit layer — there is no logic here beyond persistence.

#### CAT-02 · Manage categories
**MUST · NOT BUILT** · ⚠ constraint

As a **training coordinator**, I want to create the domains we assess — our
tracks, languages or job roles — so that exams, questions and people can be filed
under something meaningful.

**Acceptance**
1. A category created with a name only is saved, and its `Code` is generated from
   the name; the author is never required to supply a code.
2. A generated code that collides with an existing one in the same tenant is
   suffixed automatically rather than rejected.
3. Saving a code by hand that already exists in the tenant fails with
   `IMS:Catalog:CodeAlreadyExists`, and the message names the category holding it.
4. `IsActive` false removes the category from every picker but leaves existing
   exams, questions and candidates referencing it intact and readable.
5. Categories are returned ordered by `DisplayOrder`, then name.

**Tests** — *unit*: code generation and collision suffixing. *integration*:
uniqueness per tenant, deactivation leaving references resolvable, permission
`Assessment.Catalog.Manage` required. *e2e*: create, reorder, deactivate; confirm
a deactivated category is absent from the exam editor's picker but still shown on
an exam already using it.

#### CAT-03 · Manage levels within a category
**MUST · NOT BUILT**

As a **training coordinator**, I want levels to belong to a category, so that a
centre teaching both English and welding is never offered "B1" under "Welding".

**Acceptance**
1. A level saved with a `CategoryId` appears only in pickers filtered to that
   category.
2. A level saved with a null `CategoryId` appears under every category.
3. Changing an exam's category clears a level selection that is scoped to the old
   category, and says so, rather than silently keeping an impossible pair.
4. Levels are ordered by `DisplayOrder` ascending, which carries the ranking the
   names imply.

**Tests** — *integration*: scoped and unscoped filtering, the clearing rule.
*e2e*: with two categories configured, open the exam editor, switch category, and
assert the level picker's contents change and an invalid selection is cleared.

#### CAT-04 · Manage the competency tree
**MUST · NOT BUILT**

As a **teacher**, I want to record the competencies my questions measure, as a
two-level tree, so that a result reads "listening 40%, reading 85%" instead of
"62%".

**Acceptance**
1. A topic may have a parent; a parent may not itself have a parent, and an
   attempt to nest three deep is refused with a named error.
2. A topic cannot be its own parent, and a cycle of two is refused.
3. Topics are scoped by `CategoryId` on the same rule as levels (CAT-03).
4. Deleting a topic that questions reference is refused; deactivating it is
   offered instead, and the refusal names how many questions reference it.

**Tests** — *unit*: depth and cycle rules. *integration*: referential refusal with
the count in the message. *e2e*: build a two-level tree, assign one to a question,
attempt deletion, see the count.

#### CAT-05 · See a catalogue value's usage before changing it
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to see how many exams, questions and people
use a catalogue value, so that I do not deactivate something an exam is standing
on.

**Acceptance**
1. Each catalogue row shows counts of referencing exams, questions and candidates.
2. Counts are per tenant and exclude soft-deleted rows.
3. Deactivating a value with a non-zero count asks for confirmation naming the
   counts.

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

---

# Epic 2 — The question bank

*Authoring context. The strongest part of the product, and the place the owner's
constraint bites hardest.*

#### BNK-01 · Write a question of any shipped type
**MUST · PARTIAL** · ⚠ constraint

As a **teacher**, I want to write any of the question types the product supports
using controls, not code, so that writing an exam needs no training.

**Acceptance**
1. Every type in `QuestionTypes` resolves to a registered payload editor; the
   raw-payload textarea in `question-form.component.html` renders for none of them.
2. Today it renders for `matching`, `ordering`, `hotspot`, `fill-in-the-blank`,
   `code` and `scale`. This story is not done while any of those six is true.
3. A type the build has never seen — a tenant-specific one — still saves through
   the raw field, and is reported as human-graded rather than refused.
4. Switching type on an unsaved question warns before discarding a payload that
   has content.

**Tests** — *unit*: a test enumerates `QuestionTypes` and asserts a registered
editor for each. *e2e*: one spec per type authoring a valid question through
controls only, asserting the raw textarea is absent from the DOM.

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
   English — never a raw code shown to the author.

**Tests** — *unit*: `QuestionPayloadValidatorTests` — 19 cases today, extend as
types gain editors. *e2e*: `question-form.spec.ts` shows the message beside the
offending control, in Arabic.

#### BNK-03 · Format a prompt without writing HTML
**MUST · BUILT**

As a **teacher**, I want to bold a word or add a list in a question, so that a
prompt reads properly without my knowing any markup.

**Acceptance**
1. Formatting is applied by toolbar buttons; no field accepts typed markup.
2. Anything the sanitiser rejects — script tags, event handlers, `javascript:`
   URLs, style attributes — is stripped before storage, and the stripped value is
   what is stored, not merely what is displayed.
3. Arabic text keeps its direction and its letter joining after a formatting
   operation.

**Tests** — *unit*: `RichTextSanitiserTests`, 13 cases. *integration*: the
persisted column contains the sanitised value. *e2e*: bold an Arabic word and
assert the rendering, in RTL.

#### BNK-04 · Attach a chart, a recording or a clip to a question
**MUST · BUILT**

As a **teacher**, I want to attach an image, audio or video to a question, so that
a question about a candlestick chart can show the chart.

**Acceptance**
1. A file is attached by clicking or by dropping; no URL is typed and no path is
   entered.
2. The attached file previews in place — image, audio player or video player
   according to type.
3. An oversized file is refused with `IMS:File:TooLarge` and a disallowed type
   with `IMS:File:TypeNotAllowed`; both messages name the limit or the allowed
   types.
4. Blob names are generated; a caller-supplied name never reaches the container.
5. The media reaches the candidate through a URL the projector builds, and the
   blob name itself is not required to be guessable.

**Tests** — *unit*: the container name is a constant, not caller input.
*integration*: upload requires `Assessment.Questions.Edit`; size and type refusals.
*e2e*: `question-form.spec.ts` — attach, preview, remove.

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
**MUST · PARTIAL**

As a **training coordinator**, I want a question to belong to a domain and level
rather than to one exam, so that three forms for A1 cost three blueprints instead
of three copies of forty questions.

**Acceptance**
1. A question saved with no `ExamId` and no `CategoryId` is refused with
   `IMS:Question:BelongsNowhere`.
2. Listing an exam's questions returns its own questions plus bank questions whose
   category matches and whose level is null or equal. *(Built —
   `QuestionAppService.GetListAsync`.)*
3. A bank question corrected once is corrected in every exam that draws it; no
   copy exists to drift.
4. `BankOnly` filtering returns only questions with a null `ExamId`.

**Tests** — *unit*: `Question.IsDrawableBy` across the matrix of exam/category/
level/active. *integration*: `QuestionAuthoringTests` extended for the widened
list and the `BelongsNowhere` refusal. *e2e*: a bank question appears in two
exams' question lists and is edited once.

#### BNK-07 · Actually draw the bank into a candidate's paper
**MUST · NOT BUILT**

As a **candidate**, I want the paper I sit to contain the bank questions my exam
is entitled to draw, so that the exam my coordinator sees in the editor is the
exam I take.

**Acceptance**
1. `ExamTakingAppService.StartAsync` selects the bank through
   `Question.IsDrawableBy`, not `q.ExamId == exam.Id`. *(Today it does the
   latter, and `IsDrawableBy` is called from nowhere.)*
2. `ExamAppService.CheckPublishAsync` counts the same widened set, so the publish
   gate and the form builder never disagree about how many questions exist.
3. An exam whose questions all live in the bank publishes and produces a full
   paper. *(Today it is blocked with `ExamHasNoQuestions`.)*
4. `Question.TimesServed` is incremented once per question per attempt started,
   and not on a validity check or a preview.

**Tests** — *unit*: the builder given a mixed bank draws both kinds. *integration*:
start an attempt on a bank-only exam and assert the form length and the
`TimesServed` increments. *e2e*: author a bank question, assign, sit, and see it.

#### BNK-08 · Bind several questions to one passage, chart or recording
**MUST · PARTIAL**

As a **teacher**, I want to show one reading passage or play one recording and ask
six questions about it, so that the stimulus is not repeated six times and the
result can say how well the student read *that* passage.

**Acceptance**
1. A group carries instructions and a stimulus that is text, image, audio or
   video. *(Schema built — `QuestionGroup.StimulusText`, `StimulusBlobName`,
   `StimulusMediaType`.)*
2. A group's questions stay together and in their authored order when the exam
   shuffles. *(Built — `ExamFormBuilder.ApplyOrdering`.)*
3. The stimulus renders once above its questions in the taker, and an audio
   stimulus is not restarted by moving between the questions on it.
4. A group with no questions cannot be saved, and the reason names the group.

**Tests** — *unit*: ordering keeps blocks intact under shuffle, with a fixed seed.
*integration*: group creation, stimulus media, empty-group refusal. *e2e*: author a
passage with three questions; sit it; assert the passage appears once and the
audio does not reset.

#### BNK-09 · Find a question in a bank of hundreds
**MUST · PARTIAL**

As a **teacher**, I want to filter the bank by domain, level, competency, type and
difficulty, so that I can find what already exists instead of writing it twice.

**Acceptance**
1. Filters combine; each narrows the result. *(Built in the API.)*
2. A level filter also returns questions with no level, because those suit every
   level in the domain. *(Built.)*
3. There is a screen. *(There is not — no route lists questions; nothing links to
   `:examId/questions/:questionId`, so an authored question cannot be reopened.)*
4. Each row shows type, difficulty, competency and marks without opening it.

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

**Tests** — *unit*: material-change classification. *integration*: an edit during a
live attempt does not change that attempt's paper. *e2e*: not required — no user
journey exercises this beyond BNK-11's dialog.

---

# Epic 3 — Getting existing exams in

*Nothing here exists. `docs/business/business-review.md` §7.1 argues this is the
cheapest large thing in the backlog, because the file the first customer holds —
a Google Forms export — turns out to be almost fully machine-readable.*

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
   exam editor, obeying CAT-03's scoping.
2. Every imported question is created with that category and level and a null
   `ExamId`, so it enters the shared bank rather than one exam.
3. Competency and difficulty may be set for the whole import and adjusted per
   question afterwards.

**Tests** — *integration*: imported questions are drawable by a new exam at that
category and level. *e2e*: import, then create a second exam and see the questions
available to it.

#### IMP-05 · Import candidates from a list
**SHOULD · NOT BUILT** · ⚠ constraint

As a **training coordinator**, I want to paste or upload my class list, so that
forty students are not typed in one at a time.

**Acceptance**
1. Columns are matched by picking them on screen; no header naming convention has
   to be learned and no template must be downloaded first.
2. Rows with a duplicate email within the tenant are reported and skipped, naming
   the row; the rest import.
3. A row missing a name or an email is reported with its line number, not
   silently dropped.
4. The import is previewed with counts before anything is written.

**Tests** — *unit*: the row parser, duplicate and missing-field handling.
*integration*: partial import leaves valid rows written and invalid rows absent.
*e2e*: paste a list with one duplicate and one blank email; confirm the counts and
the two named rows.

---

# Epic 4 — Exams, sections and publishing

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
   the reason names the rule that starved.
4. All blockers are returned in one response, not the first one found.
5. Publishing calls the same check, so the panel and the action can never
   disagree.

**Tests** — *unit*: `Exam.Publish` refusals. *integration*: the full blocker list
in one call; publish refuses when the check refuses. *e2e*: `exam-actions.spec.ts`
— open the panel, see three blockers, fix one, see two.

#### EXM-04 · Be warned about what will merely go badly
**SHOULD · PARTIAL**

As a **training coordinator**, I want to be warned about the things that will work
but that I probably did not intend, so that a paper is not quietly worthless.

**Acceptance**
1. An exam whose questions carry no competency warns that the result will be a
   bare number.
2. A practice exam with questions lacking explanations warns.
3. An exam where every candidate gets the same paper warns.
4. A bank too small to rotate against the form length warns.
5. An over-exposed bank warns. *(Present in code and unreachable: nothing
   increments `TimesServed`. This story is not done until BNK-07 lands.)*
6. Warnings never block publication.

**Tests** — *integration*: each warning fires on its own condition and none blocks.
*e2e*: the warning list renders distinctly from the blocker list.

#### EXM-05 · Take an exam out of circulation
**SHOULD · BUILT**

As a **training coordinator**, I want to archive an exam, so that it stops being
assignable without destroying the attempts already sat on it.

**Acceptance**
1. An archived exam cannot be assigned.
2. Attempts already under way finish normally and remain readable.
3. Archiving requires `Assessment.Exams.Publish`, not `Edit`.

**Tests** — *integration*: assignment refused, in-flight attempt unaffected,
permission. *e2e*: archive from the list, confirm the assign action disappears.

#### EXM-06 · Divide an exam into named parts
**MUST · PARTIAL**

As a **teacher**, I want an exam to have named sections — Listening, Reading,
Grammar, Writing — so that a result tells a coordinator which class to put the
student in.

**Acceptance**
1. Sections are created, named, reordered and deleted within an exam. *(Entity
   built — `ExamSection`; no service, no API, no screen.)*
2. A question, a group and a blueprint rule may each belong to a section.
   *(Optional `ExamSectionId` present on all three.)*
3. An exam with no sections behaves exactly as it does today, and its paper is
   assembled as one implicit section.
4. Deleting a section with questions in it asks what should happen to them and
   never silently orphans them.

**Tests** — *unit*: assembly with zero, one and several sections produces the same
result for the zero case as today. *integration*: cascade rules on delete.
*e2e*: create four sections, move questions between them, reorder.

#### EXM-07 · Give a section its own clock
**SHOULD · PARTIAL**

As a **teacher**, I want a section to be timed separately, so that a candidate
cannot spend the whole hour on the essay and never reach the listening.

**Acceptance**
1. A section with a time limit closes when its own time runs out and the next
   begins; the candidate cannot return to it.
2. A section with no time limit shares the exam's clock.
3. The countdown shown is the section's when one is set, and the exam's otherwise,
   and both are computed from the server.
4. Section time is enforced server-side; a manipulated browser clock cannot extend
   it.

**Tests** — *unit*: remaining-time computation per section. *integration*: an
answer submitted to a closed section is refused. *e2e*: watch a section close and
the next begin.

#### EXM-08 · Fail an exam on one section however well the rest went
**SHOULD · PARTIAL**

As a **training coordinator**, I want a section to carry a minimum below which the
whole exam fails, so that passing overall while failing the safety module is not a
pass.

**Acceptance**
1. A section scored below its `MinimumPercentage` fails the attempt regardless of
   the total. *(Domain rule built and tested — `ExamSection.IsFailedAt`,
   `ExamFormTests`.)*
2. A section with no minimum only contributes to the total. *(Built and tested.)*
3. The result states which section caused the failure, not merely that the attempt
   failed.
4. Nothing computes this into `Attempt.IsPassed` yet.

**Tests** — *unit*: `ExamFormTests` covers the entity rule; extend to the attempt
scoring path. *integration*: an attempt above the pass mark but below a section
minimum is recorded as failed. *e2e*: the result page names the section.

#### EXM-09 · Turn a candidate away in thirty seconds
**SHOULD · PARTIAL**

As a **recruiter**, I want an untimed pass/fail gate before the exam proper, so
that someone who does not qualify is not marked for an hour before we find out.

**Acceptance**
1. A section flagged `IsQualifying` is presented before every other section,
   untimed. *(Flag exists on the entity; nothing reads it.)*
2. Failing it ends the attempt immediately with a distinct end reason.
3. An attempt ended this way never enters the reviewer's queue.
4. The candidate is told they did not meet the entry requirement, without being
   shown which answer was wrong.

**Tests** — *unit*: the gate decision. *integration*: the ended attempt is absent
from `GetQueueAsync`. *e2e*: fail the gate, see the message, confirm no exam
questions were served.

#### EXM-10 · Choose whether everyone sits the same paper
**MUST · PARTIAL**

As a **training coordinator**, I want to choose between drawing a paper per
candidate, using one approved paper, or rotating several, so that I can start with
a paper I have read and move on when I trust the system.

**Acceptance**
1. `DeliveryMode` offers the three options; `DrawPerCandidate` is the default and
   is what existing exams do. *(Enum and property built; nothing reads them.)*
2. `FixedForm` requires `FixedFormId` to point at a published form of this exam;
   publishing is refused otherwise, naming the exam.
3. `RotateForms` requires at least two published forms; publishing is refused
   otherwise, saying how many exist.
4. Changing mode on an exam with attempts in flight does not change those
   attempts' papers.

**Tests** — *unit*: the publish preconditions per mode. *integration*: attempts in
flight keep their frozen form. *e2e*: switch to `FixedForm` with no form and see
the refusal.

#### EXM-11 · Practise rather than be judged
**SHOULD · PARTIAL**

As a **candidate**, I want a practice exam to show me the right answer and the
explanation afterwards, so that I learn something rather than receiving a number.

**Acceptance**
1. In `Practice` mode the result reveals the correct answer and the explanation
   per question. *(Server built — `CorrectAnswerRenderer`, `PracticeReviewItemDto`.)*
2. In `Assessment` mode neither ever reaches the browser.
3. On a weighted question a learner is shown best / acceptable / not credited, and
   never the "penalised" bucket.
4. There is no taker screen to display any of it.

**Tests** — *unit*: `TakerQuestionProjectorTests` — the key never crosses the wire.
*integration*: mode governs what `GetResultAsync` returns. *e2e*: sit a practice
exam and read the explanation.

#### EXM-12 · Open an exam only within a window
**COULD · PARTIAL**

As a **training coordinator**, I want an exam to be sittable only between two
instants, so that a cohort sits it together.

**Acceptance**
1. Outside the window, starting is refused with `IMS:Exam:OutsideSchedule`.
   *(Built — `Exam.IsOpenAt`, enforced in `StartAsync`.)*
2. An attempt started inside the window may finish after it closes.
3. The window is shown to the candidate on the preview screen before they start.

**Tests** — *unit*: `IsOpenAt` boundaries. *integration*: start refused outside,
in-flight attempt unaffected by the close. *e2e*: the preview shows the window.

---

# Epic 5 — Blueprints and per-candidate assembly

#### BPR-01 · Describe the paper as a recipe
**MUST · PARTIAL**

As a **teacher**, I want to say "eight medium listening questions and six easy
grammar ones", so that every candidate's paper covers the same ground at the same
difficulty even though the questions differ.

**Acceptance**
1. A rule names a competency, a difficulty, a type and a count; any of the first
   three may be left as "any". *(Server built — `GetBlueprintAsync`,
   `SetBlueprintAsync`, and `ExamController` exposes both.)*
2. Each rule shows how many bank questions currently match it, so an unfillable
   rule is visible while it is being written.
3. There is a screen. *(There is not — `exam.service.ts` has `getBlueprint` and no
   `setBlueprint`; nothing in Angular edits rules.)*
4. Rules are ordered, and that order is the order their questions appear.

**Tests** — *integration*: set and re-read a blueprint; matching counts per rule.
*e2e*: add three rules, see one show zero matches, fix it.

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
   shuffle setting, because for those types the stored order is the key.
2. With shuffling off, a matching question's payload as received does not pair
   left[i] with right[i].
3. A candidate reloading mid-question sees the same order.

**Tests** — *unit*: `AlwaysOrdered` types record an order under both settings.
*integration*: the served payload for a shuffle-off matching question does not
encode the pairing. *e2e*: reload and confirm the order is stable.

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

#### BPR-05 · Fail loudly when a rule starves
**SHOULD · PARTIAL**

As a **training coordinator**, I want to be told which rule could not be filled,
so that I know which competency to write more questions for.

**Acceptance**
1. `CheckPublishAsync` names the starved rule, not merely that the blueprint is
   unsatisfiable. *(Today it adds `ExamBlueprintUnsatisfiable` and breaks out of
   the loop, so only the first is reported and it is unnamed.)*
2. Every starved rule is reported, not the first.
3. The message gives the required and the available counts.

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

*`ExamForm`, `ExamFormQuestion`, `ExamFormStatus` — entities, EF configuration, a
migration and six domain tests. No application service, no API, no screen.
`docs/business/business-review.md` §6 argues this is the best cost-to-revenue
ratio in the backlog.*

#### FRM-01 · Build a named paper
**MUST · PARTIAL**

As a **training coordinator**, I want to build "Form 1" as a fixed list of
questions, so that there is a paper a human can read before anybody sits it.

**Acceptance**
1. A form is created with a name and a code; the code is unique within its exam.
2. Questions are added from the exam's drawable bank, ordered, and given the marks
   they carry on this form.
3. Marks are copied onto `ExamFormQuestion`, so raising a question's marks later
   does not change what a past candidate scored.
4. A form may be generated from the blueprint, and `WasGenerated` records that it
   was, so a later reviewer can tell which it was.

**Tests** — *unit*: generation from a blueprint produces a form satisfying every
rule. *integration*: code uniqueness within an exam; marks copied not referenced.
*e2e*: build a form by hand, reorder it, save it.

#### FRM-02 · Freeze a form for use
**MUST · PARTIAL**

As a **reviewer**, I want to publish a form once I have read it, so that what I
approved is what candidates sit.

**Acceptance**
1. A form with no questions cannot be published, and the reason names the form.
   *(Domain rule built and tested.)*
2. A form carrying the same question twice cannot be published, and the reason
   names the duplicate. *(Built and tested — the entity refuses it; the message
   does not yet name the question.)*
3. Publishing freezes `MaxScore`. *(Built and tested.)*
4. A published form's question list cannot be changed; an attempt to change it is
   refused, not silently ignored.
5. Both refusals show a sentence rather than a code. *(Neither
   `IMS:ExamForm:NoQuestions` nor `IMS:ExamForm:DuplicateQuestions` is present in
   `en.json` or `ar.json` — see PLT-04.)*

**Tests** — *unit*: `ExamFormTests` — extend for the naming in (2) and the
immutability in (4). *integration*: an edit to a published form is refused.
*e2e*: publish, then find the editing controls gone.

#### FRM-03 · Retire a form without losing what was sat on it
**SHOULD · PARTIAL**

As a **training coordinator**, I want to take a form out of rotation, so that a
paper I think has leaked stops being served while old results still resolve.

**Acceptance**
1. A retired form is never selected for a new attempt. *(`IsUsable` built and
   tested.)*
2. Results referencing a retired form still render, including its name and code.
3. Retiring the form an exam names as its `FixedFormId` blocks new attempts with a
   message naming the form, rather than serving an empty paper.

**Tests** — *unit*: `IsUsable`. *integration*: results resolve; the fixed-form
exam refuses cleanly. *e2e*: retire and confirm the assign action explains itself.

#### FRM-04 · Sit a fixed form
**MUST · NOT BUILT**

As a **candidate**, I want to sit the paper my centre approved, so that everyone
in my sitting answered the same questions.

**Acceptance**
1. With `DeliveryMode.FixedForm`, the attempt's `AttemptQuestion` rows come from
   `ExamFormQuestion` in its `DisplayOrder`, and the blueprint is not consulted.
2. `Attempt.MaxScore` equals the form's frozen `MaxScore`.
3. Option shuffling still applies if the exam asks for it; question order does
   not, because the form's order is the form.
4. `ExamForm.TimesUsed` is incremented once per attempt started on it.

**Tests** — *unit*: assembly from a form rather than the bank. *integration*:
`MaxScore` matches, `TimesUsed` increments once per start and not on a preview.
*e2e*: two candidates on a fixed-form exam receive the same questions.

#### FRM-05 · Spread a cohort across forms
**SHOULD · NOT BUILT**

As a **training coordinator**, I want candidates spread across the published
forms, so that what leaks at lunchtime is worth a fraction of the sitting.

**Acceptance**
1. With `RotateForms`, each new attempt takes the published form with the lowest
   `TimesUsed`; ties break deterministically.
2. Rotation never selects a draft or retired form.
3. A retake by the same candidate takes a form they have not sat, when one exists.

**Tests** — *unit*: the selection rule including ties and exhaustion. *integration*:
twenty attempts across three forms distribute evenly. *e2e*: two candidates,
different forms.

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

#### FRM-07 · Know how worn a paper is
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to see how many times each form has been
sat, so that I know when to write a new one.

**Acceptance**
1. Each form shows `TimesUsed` and the date it was last sat.
2. A form past a tenant-set threshold is flagged in the list.
3. The threshold is a setting, not a constant compiled in.

**Tests** — *integration*: the count and the flag. *e2e*: the list shows both.

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

*People context. Entities and tables exist. No application service, no DTOs, no
route — `candidates/` loads `PlaceholderComponent`.*

#### PPL-01 · Add a person to be assessed
**MUST · NOT BUILT**

As a **training coordinator**, I want to record a student's name and email, so
that an exam can be sent to them.

**Acceptance**
1. A candidate is created with name and email; no account, no password, no
   invitation to sign up.
2. A duplicate email within the tenant is refused with
   `IMS:Candidate:EmailAlreadyExists`, naming the existing person.
3. The same email in a different tenant is accepted.
4. The screen uses the tenant's own word for this person, from `CategorySet`.

**Tests** — *integration*: uniqueness per tenant, cross-tenant independence,
permission `Assessment.Candidates.Create`. *e2e*: create, see the tenant's
vocabulary, attempt a duplicate.

#### PPL-02 · Group people into a class or a batch
**MUST · NOT BUILT**

As a **training coordinator**, I want to put students into a class, so that an
exam is sent to forty people in one action.

**Acceptance**
1. A group is created and members added and removed; a person may belong to
   several groups.
2. Removing a person from a group does not delete the person or their attempts.
3. A group's member count is shown wherever the group is selectable.
4. Assigning to an empty group is refused with `IMS:Assignment:GroupEmpty`.

**Tests** — *integration*: membership, the non-cascading removal, the empty-group
refusal. *e2e*: create a class, add five, assign, see five links.

#### PPL-03 · Find a person
**MUST · NOT BUILT**

As a **training coordinator**, I want to search people by name, email and group,
so that I can find one among several hundred.

**Acceptance**
1. Search matches name and email, case-insensitively, and matches Arabic text
   regardless of diacritics.
2. Filtering by group and by category combine.
3. Paging is stable.

**Tests** — *integration*: matching rules including the Arabic case. *e2e*: search
in Arabic and find the right person.

#### PPL-04 · See one person's history
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to see every exam a student has sat and how
they did, so that I can advise them.

**Acceptance**
1. The list shows exam, date, score, pass/fail and whether review is outstanding.
2. An attempt still awaiting a human shows as pending, never as a provisional
   score.
3. Each row opens the answer sheet, subject to `Assessment.Attempts.View`.

**Tests** — *integration*: pending attempts render as pending; permission on the
answer sheet. *e2e*: the history and one answer sheet.

#### PPL-05 · Correct a person's details
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to fix a misspelt name or a wrong email, so
that an invitation reaches the right inbox.

**Acceptance**
1. Editing an email does not invalidate links already issued to that person.
2. Editing is refused without `Assessment.Candidates.Edit`.
3. The change is audited.

**Tests** — *integration*: existing links still resolve; permission; audit row.

#### PPL-06 · Remove a person
**COULD · NOT BUILT**

As an **administrator**, I want to delete a person and their data on request, so
that we can answer a data-protection request.

**Acceptance**
1. Deletion removes the person, their attempts, answers, uploaded files and
   integrity signals.
2. Aggregate item statistics are not recomputed backwards, and the export states
   that.
3. Deletion requires `Assessment.Candidates.Delete` and is confirmed by typing the
   person's name.

**Tests** — *integration*: every dependent row is gone, including blobs.
*e2e*: the confirmation.

---

# Epic 8 — Assignment and links

*Delivery, staff side. `AssignmentAppService` is complete and unexercised by any
test or screen.*

#### ASG-01 · Send an exam to one person
**MUST · PARTIAL**

As a **recruiter**, I want to send an exam to a candidate with an expiry and a
number of attempts, so that they can sit it without an account.

**Acceptance**
1. Exactly one of candidate or group must be supplied; neither is
   `IMS:Assignment:TargetMissing`, both is `TargetAmbiguous`. *(Built.)*
2. An expiry in the past is refused with `IMS:Assignment:ExpiryInPast`. *(Built.)*
3. An unpublished exam cannot be assigned, and the reason is
   `IMS:Exam:NotPublished`. *(Built.)*
4. One `ExamLink` is created per person, each with its own token. *(Built.)*
5. There is a screen. *(There is not.)*

**Tests** — *integration*: each refusal; one link per recipient.
*e2e*: assign to one person and read back the URL.

#### ASG-02 · Send an exam to a whole class
**MUST · PARTIAL**

As a **training coordinator**, I want to send an exam to a group in one action, so
that forty links are not created by hand.

**Acceptance**
1. Assigning to a group creates one link per member, each individually revocable.
2. `LinkCount` records how many were produced.
3. An empty group is refused.

**Tests** — *integration*: link count matches membership; individual revocation
does not affect the rest. *e2e*: assign to five, revoke one, three remain valid
plus the untouched one.

#### ASG-03 · Deliver the invitation
**MUST · PARTIAL**

As a **candidate**, I want to receive the link by email, so that I can find it
when I am ready to sit.

**Acceptance**
1. The email carries the candidate's own URL and the expiry. *(Built.)*
2. One unreachable address does not abandon the other links; the failure is
   reported per recipient with the address. *(Built.)*
3. `EmailSentAt` is recorded per link on success. *(Built.)*
4. The email carries the tenant's name, logo and support address, not ours. *(Not
   built — depends on BRD-03.)*
5. The email is in the candidate's language and renders right to left in Arabic.

**Tests** — *integration*: partial failure leaves other links sent and reports the
failed address; `EmailSentAt` set only on success. *e2e*: not applicable — assert
the rendered body in an integration test against a captured message instead.

#### ASG-04 · See the state of every link
**MUST · PARTIAL**

As a **training coordinator**, I want to see who has opened their link, who has
started and who has finished, so that I know who to chase.

**Acceptance**
1. Each row shows the token prefix, expiry, attempts used against allowed, first
   opened, email sent, and revocation. *(Server built.)*
2. The full token is never returned by this endpoint, and only the prefix is
   displayed.
3. Rows are filterable by state.

**Tests** — *integration*: the response contains no field from which a working
token can be derived. *e2e*: the list and its filters.

#### ASG-05 · Kill a link that leaked
**MUST · PARTIAL**

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
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to resend a link, so that a student who
deleted the email is not blocked.

**Acceptance**
1. Resending reuses the existing link and does not mint a new token.
2. `EmailSentAt` is updated and a resend count recorded.
3. Resending a revoked or expired link is refused with the reason.

**Tests** — *integration*: the token hash is unchanged after a resend; refusals.
*e2e*: resend and see the timestamp change.

#### ASG-07 · Extend an expiry
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to push back an expiry, so that a student
who was ill is not made to start again.

**Acceptance**
1. Extending updates the link and, if set, the assignment.
2. An expiry cannot be moved into the past.
3. The change is audited with who and when.

**Tests** — *integration*: past-date refusal; audit. *e2e*: extend and confirm the
link works again.

#### ASG-08 · End someone's attempt
**SHOULD · NOT BUILT**

As an **administrator**, I want to end an attempt that is stuck or was started in
error, so that it can be graded or discarded rather than sitting open.

**Acceptance**
1. Force-submitting records `AttemptEndReason.EndedByAdministrator` and grades the
   attempt.
2. It requires `Assessment.Attempts.ForceSubmit`.
3. The candidate's session for that attempt stops accepting answers immediately.

**Tests** — *integration*: end reason, grading runs, permission. *e2e*: force
submit while a taker session is open and confirm the next save is refused.

#### ASG-09 · Choose which form a sitting uses
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to say which named form this assignment
uses, so that the morning group and the afternoon group sit different papers.

**Acceptance**
1. When the exam's mode is `FixedForm` or `RotateForms`, the assignment may name a
   published form.
2. Naming a draft or retired form is refused, with the reason.
3. The chosen form is recorded on the link and used by every attempt started from
   it.

**Tests** — *integration*: refusals; the attempt uses the named form. *e2e*: two
assignments, two forms, two papers.

---

# Epic 9 — Sitting the exam

*The whole server side is built and careful. The whole client side is
`PlaceholderComponent`. This epic is the product.*

#### TAK-01 · Open a link and see what I am about to sit
**MUST · PARTIAL**

As a **candidate**, I want to see the exam's name, length and rules before I
start, so that I do not begin a timed exam by accident.

**Acceptance**
1. Opening a link shows the exam title, question count, time limit and attempts
   remaining, and does not start the clock. *(Server built — `OpenLinkAsync`.)*
2. `AttemptsUsed` is not incremented by opening. *(Built — it moves in
   `StartAsync`.)*
3. The page carries the tenant's name and logo, not ours. *(Not built — BRD-03.)*
4. Starting is a deliberate action, and the page says the clock begins on it.

**Tests** — *integration*: opening twice does not consume an attempt.
*e2e*: open, read, start, and assert the countdown only then begins.

#### TAK-02 · Be told why a link does not work
**MUST · PARTIAL**

As a **candidate**, I want to be told whether my link expired, was revoked or is
used up, so that I know whether to ask for a new one.

**Acceptance**
1. Each of expired, revoked and exhausted produces its own message, never a
   generic failure. *(Built — `GetBlockReason`.)*
2. An unknown token produces `IMS:ExamLink:Invalid` and reveals nothing about
   whether that token ever existed.
3. Every message is shown in the candidate's language and offers the tenant's
   support address.

**Tests** — *unit*: the three reasons. *integration*: an unknown token is
indistinguishable from a wrong one in both response and timing.
*e2e*: three links in three states, three messages, in Arabic.

#### TAK-03 · Start, and resume if I am interrupted
**MUST · PARTIAL**

As a **candidate**, I want to come back to the same paper if my connection drops,
so that a network failure does not cost me my exam.

**Acceptance**
1. Starting twice on one link resumes the running attempt rather than creating a
   second. *(Built, and enforced by a unique index as well as in code.)*
2. The resumed paper is identical — same questions, same positions, same option
   order.
3. The countdown on resume is computed from the stored deadline, not restarted.
4. `AttemptsUsed` increments once, on the real start.

**Tests** — *unit*: seeded rebuild produces an identical paper. *integration*:
concurrent double-start creates one attempt. *e2e*: start, reload, confirm the
same question and a countdown that did not reset.

#### TAK-04 · Answer one question at a time
**MUST · PARTIAL**

As a **candidate**, I want to see one question at a time with clear progress, so
that a long paper is not overwhelming.

**Acceptance**
1. One question is fetched per position; the whole paper is never in the browser
   at once when the exam asks for that. *(Server built — `GetQuestionAsync`.)*
2. Progress shows answered against total.
3. Back navigation is available only when the exam allows it, and the control is
   absent rather than disabled when it does not.
4. Requesting a position not on this candidate's form is refused with
   `IMS:Attempt:QuestionNotOnForm`. *(Built.)*

**Tests** — *integration*: the out-of-form refusal; back navigation honoured.
*e2e*: page through, confirm progress, confirm the control's absence.

#### TAK-05 · Not lose work
**MUST · PARTIAL**

As a **candidate**, I want my answer saved as I go, so that a dropped connection
late in the exam does not cost me the work already done.

**Acceptance**
1. An answer is written on save, not only at submit. *(Built — `SaveAnswerAsync`.)*
2. Re-answering updates the existing row rather than inserting a second.
3. The saved response is returned when the question is reopened. *(Built.)*
4. A failed save is visible to the candidate and retried, never silently dropped.

**Tests** — *integration*: one `Answer` row per question per attempt after repeated
saves. *e2e*: answer, navigate away, return, see the answer; simulate a failed save
and see the indicator.

#### TAK-06 · Trust the clock
**MUST · PARTIAL**

As a **candidate**, I want the countdown to be the real one, so that a slow
machine or a wrong system clock does not cost me time.

**Acceptance**
1. Remaining time is computed from `Attempt.DeadlineAt` on the server and returned
   with every save. *(Built.)*
2. Changing the device clock does not change the remaining time.
3. At zero the browser submits; if it does not, the server does. *(Built —
   `AttemptTimeoutWorker`.)*
4. A browser-side timeout records `TimedOutInBrowser`; a server-side one records
   `TimedOutOnServer`. *(Built.)*

**Tests** — *unit*: `SecondsRemaining` and `IsExpired` at the boundaries.
*integration*: the worker submits and grades an abandoned attempt, and records the
right reason. *e2e*: move the browser clock forward and confirm the countdown does
not move with it.

#### TAK-07 · Never receive the answer
**MUST · BUILT**

As a **training coordinator**, I want the answer key never to reach the candidate's
browser, so that an exam cannot be passed by reading the network traffic.

**Acceptance**
1. The projected question carries id, text and media URL only; `isCorrect` and
   `weight` are absent from the wire.
2. A rubric is not sent to a candidate.
3. In `Assessment` mode the explanation is not sent before submission.
4. Adding a new answer-bearing field to a payload without adding it to the
   projector's deny list fails a test.

**Tests** — *unit*: `TakerQuestionProjectorTests` — 10 cases. *integration*:
`ContractBoundaryTests` — a DTO reachable by a taker cannot reference the domain.
*e2e*: intercept the network response and assert the absent fields.

#### TAK-08 · Answer with a file or a recording
**MUST · PARTIAL**

As a **candidate**, I want to upload a document or record a spoken answer, so that
a question that cannot be answered by clicking can still be answered.

**Acceptance**
1. An upload is attached to the answer with its original filename kept for the
   reviewer. *(Fields built on `Answer`.)*
2. Recording uses the browser's microphone with an explicit permission prompt, a
   visible level meter, and the ability to re-record before saving.
3. Size and type limits produce the same named errors as authoring media.
4. An upload that fails does not lose the rest of the attempt.

**Tests** — *integration*: the blob, the filename, the limits. *e2e*: upload a
file; record, play back, re-record, save.

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
**MUST · PARTIAL**

As a **candidate**, I want to know whether I have a result or whether someone has
to mark it, so that I am not left refreshing a page.

**Acceptance**
1. Submitting an already-submitted attempt is refused with
   `IMS:Attempt:AlreadySubmitted`. *(Built.)*
2. When any answer needs a human, the result reports that marking is pending and
   returns no score at all — a provisional score is worse than none. *(Built.)*
3. When grading is complete the result shows score, percentage, pass/fail and the
   competency breakdown. *(Built — `BuildTopicBreakdownAsync`.)*
4. A candidate cannot read another candidate's result by changing an id. *(Built —
   the result is loaded through the session's own attempt.)*

**Tests** — *integration*: the pending case returns no score; the cross-candidate
attempt fails. *e2e*: submit an exam with a written question and read the pending
message; submit an all-objective one and read the score.

#### TAK-12 · Learn from a practice attempt
**SHOULD · PARTIAL**

As a **candidate**, I want to see what the right answer was and why, so that
practice teaches me something.

**Acceptance**
1. In `Practice` mode each question shows my answer, the correct answer and the
   explanation.
2. On a weighted question I am told whether I chose the best answer or an
   acceptable one; I am never shown the "penalised" label.
3. None of this is available in `Assessment` mode, at any endpoint.

**Tests** — *unit*: the three learner buckets. *integration*: mode gates the
endpoint. *e2e*: practice reveals, assessment does not.

#### TAK-13 · Be observed honestly, or not at all
**SHOULD · PARTIAL**

As a **candidate**, I want to know what is being recorded about how I answer, so
that I am not surveilled without being told.

**Acceptance**
1. When `CollectIntegritySignals` is on, the preview screen says plainly what is
   recorded — pastes, focus loss, timing — before the attempt starts.
2. Signals are recorded and counted. *(Built — `ReportSignalAsync`.)*
3. No signal ever ends an attempt, changes a score or blocks an action.
4. When the exam has signals off, nothing is recorded and nothing is claimed.

**Tests** — *integration*: signals off records nothing; no code path lets a signal
change a score. *e2e*: the notice appears before starting.

#### TAK-14 · Sit the exam in Arabic
**MUST · NOT BUILT**

As a **candidate**, I want the exam to read correctly in Arabic, so that I am
reading the question rather than decoding the layout.

**Acceptance**
1. Every taker screen renders right to left with logical properties, at a phone
   viewport, with no horizontal page scroll.
2. Numbers, timers and progress indicators read correctly in an RTL context.
3. A mixed Arabic-and-Latin prompt — a chart label inside an Arabic sentence —
   renders without reordering the sentence.
4. Letter-spacing is not applied to Arabic text.

**Tests** — *e2e*: the whole taker journey run in Arabic at a phone viewport,
asserting no horizontal overflow, in the same harness that already catches this
for the staff screens. No unit or integration layer — this is a rendering
property.

#### TAK-15 · Sit the exam without a mouse or with a screen reader
**MUST · NOT BUILT** · ⚠ constraint

As a **candidate**, I want to complete the exam by keyboard and to hear it read
aloud, so that a disability does not decide my score.

**Acceptance**
1. Every question type is completable by keyboard alone, including matching,
   ordering and hotspot.
2. Each screen passes an automated accessibility check with no critical or serious
   violations.
3. Focus is placed on the question when a new one loads, and the countdown is
   announced politely rather than on every tick.
4. The page is usable at 400% zoom without horizontal scrolling.

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

---

# Epic 10 — Grading and the reviewer's queue

#### GRD-01 · Mark what a machine can mark
**MUST · BUILT**

As a **training coordinator**, I want objective questions marked the moment an
exam is submitted, so that a class of forty does not wait on me.

**Acceptance**
1. Every registered grader runs over the attempt in one pass; questions are loaded
   in one query, not one per answer.
2. An unanswered question scores zero without a grader or a reviewer.
3. The total is the sum over this candidate's own form, so a shorter form is not
   judged against a longer one's maximum.
4. Passing is a percentage comparison against the exam's `PassingPercentage`.

**Tests** — *unit*: each of the thirteen graders against valid and hostile input,
including Arabic-Indic digits. *integration*: a submitted attempt is scored and
closed in one transaction.

#### GRD-02 · Never lose an answer to a broken grader
**MUST · BUILT**

As a **candidate**, I want a grader that fails to send my answer to a person, so
that a defect in the software does not score me zero.

**Acceptance**
1. A question type with no registered grader is routed to manual review, not
   scored zero.
2. A grader that throws is routed to manual review, and the failure is logged with
   the answer id and the type.
3. Neither case rolls back the submission; the attempt is submitted, graded as far
   as possible, and present in the queue.
4. No response a candidate can type can leave an attempt submitted, ungraded and
   in nobody's queue.

**Tests** — *unit*: `GradingResilienceTests`. *integration*: a hostile numeric
answer leaves the attempt submitted and queued, not stuck.

#### GRD-03 · Work through what needs a person
**MUST · PARTIAL**

As a **reviewer**, I want a queue of attempts waiting on me, so that I know what
is outstanding.

**Acceptance**
1. The queue lists only attempts with `NeedsManualReview`, oldest first by default.
   *(Server built — `GetQueueAsync`.)*
2. Each row shows exam, candidate, submitted-at and how many answers are pending.
3. It requires `Assessment.Review.ViewQueue`. *(Built.)*
4. There is a screen. *(There is not — `review/` is a placeholder.)*

**Tests** — *integration*: only pending attempts appear; permission; tenant
isolation. *e2e*: the queue, its ordering, and a row opening the marking screen.

#### GRD-04 · Mark against a rubric
**MUST · PARTIAL**

As a **reviewer**, I want to score each criterion separately with a comment, so
that two reviewers reach the same mark and the candidate can be told why.

**Acceptance**
1. The rubric's criteria are shown with their maximum marks. *(Server built.)*
2. Per-criterion marks are stored on the answer as `RubricScores`, and the awarded
   total cannot exceed the question's marks.
3. Saving a mark recomputes the attempt total and clears the pending flag when
   nothing is left. *(Built — `RecalculateAsync`.)*
4. A reviewed attempt leaves the queue.

**Tests** — *unit*: the total cannot exceed the maximum. *integration*: the
recompute and the queue exit — the specific bug this replaced left every reviewed
attempt in the queue forever, so this needs a regression test.
*e2e*: mark, see the total change, see the row leave the queue.

#### GRD-05 · See what the right answer was
**SHOULD · BUILT**

As a **reviewer**, I want the answer key rendered beside the candidate's answer,
so that I am not opening the question in another tab.

**Acceptance**
1. The key is rendered for the question's type. *(Built —
   `CorrectAnswerRenderer`.)*
2. On a weighted question it is rendered in four buckets: best, acceptable, not
   credited, penalised.
3. It is never rendered into anything a candidate can reach.

**Tests** — *unit*: rendering per type, and the four buckets. *e2e*: the marking
screen shows the buckets.

#### GRD-06 · See how the answer was produced
**SHOULD · PARTIAL**

As a **reviewer**, I want to see that an answer arrived by paste, or in four
seconds, so that I can weigh it — without being told what to conclude.

**Acceptance**
1. The integrity report lists signals with type, time and magnitude. *(Server
   built — `GetIntegrityReportAsync`.)*
2. It requires `Assessment.Review.ViewIntegritySignals`, held separately from
   `Grade`, because these are behavioural data about a person. *(Built.)*
3. The screen states that these are observations, not conclusions, and offers no
   action that acts on them automatically.

**Tests** — *integration*: the separate permission is enforced. *e2e*: a reviewer
with `Grade` but not `ViewIntegritySignals` sees the marking screen without the
report.

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

---

# Epic 11 — Results, item health and export

*Nothing in this epic has an application service. `Assessment.Results.View`,
`.Export` and `.ViewItemAnalysis` are permission strings with no code behind
them.*

#### RES-01 · See how a class did
**MUST · NOT BUILT**

As a **training coordinator**, I want a roster of everyone assigned an exam and
where they got to, so that I can see the whole class at once.

**Acceptance**
1. Rows cover everyone assigned, including those who never started, distinguishing
   not started, in progress, awaiting review and complete.
2. Complete rows show score, percentage and pass/fail.
3. Filtering by group, exam and state combine.
4. It requires `Assessment.Results.View`.

**Tests** — *integration*: every state appears, including never-started; permission;
tenant isolation. *e2e*: the roster with the four states.

#### RES-02 · Read one candidate's paper
**MUST · NOT BUILT**

As a **reviewer**, I want to see exactly what one candidate was asked and
answered, so that I can defend the result if it is challenged.

**Acceptance**
1. The sheet shows every question on that candidate's form in the order served,
   with their answer, the mark and any comment.
2. It reflects the paper as served, even after the bank has been edited since.
3. It shows which named form or which seed produced the paper.
4. It requires `Assessment.Attempts.View`.

**Tests** — *integration*: editing a question after the attempt does not change the
sheet. *e2e*: sit, edit the question, reopen the sheet, confirm it is unchanged.

#### RES-03 · Read a result as a profile
**MUST · PARTIAL**

As a **training coordinator**, I want a result broken down by competency, so that
I know what to teach rather than only who passed.

**Acceptance**
1. The result shows a percentage per competency. *(Server built for the taker's own
   result — `BuildTopicBreakdownAsync`.)*
2. A question with no competency is counted in the total and excluded from the
   breakdown, and the exclusion is stated.
3. There is no staff-facing endpoint for this. *(Correct — none exists.)*

**Tests** — *integration*: the breakdown sums correctly and the uncategorised
remainder is stated. *e2e*: the breakdown on the result screen.

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

#### RES-05 · Get the results out
**MUST · NOT BUILT**

As a **training coordinator**, I want to export results as a spreadsheet, so that
I can put them where my centre already keeps records.

**Acceptance**
1. Export produces one row per attempt with candidate, exam, form, dates, score,
   percentage, pass/fail and per-competency columns.
2. Arabic text and Arabic-Indic digits survive the export and open correctly in a
   spreadsheet application without a manual encoding step.
3. It requires `Assessment.Results.Export`.
4. An export of an attempt still awaiting review is marked as such rather than
   showing a partial score.

**Tests** — *unit*: encoding and the pending-marking rule. *integration*:
permission; column set. *e2e*: export and reopen.

#### RES-06 · Compute the item statistics
**MUST · NOT BUILT**

As a **training coordinator**, I want to know which of my questions are working,
so that I can retire the ones that measure nothing.

**Acceptance**
1. A job updates `TimesAnswered`, `DifficultyIndex` and `DiscriminationIndex` from
   graded attempts. *(Today nothing writes any of these; they are columns only.)*
2. `TimesServed` is incremented at form assembly, not at grading, because exposure
   is who saw it rather than who answered it.
3. A question with too few responses reports "not enough data" rather than a
   meaningless index.
4. Statistics are per tenant and never aggregate across tenants.
5. Recomputation is idempotent — running the job twice does not double a count.

**Tests** — *unit*: both indices against a hand-worked example; the small-sample
rule. *integration*: idempotence, tenant separation, and that `TimesServed` moves
on assembly rather than on grading.

#### RES-07 · See which questions are not measuring anything
**SHOULD · NOT BUILT**

As a **teacher**, I want a list of my weakest questions in plain language, so that
I can fix them without knowing what a discrimination index is.

**Acceptance**
1. Questions with a discrimination index at or below zero are listed with the
   sentence that this question measures the opposite of what it claims.
2. Questions everyone gets right, everyone gets wrong, or that are over-exposed
   are listed with their own plain-language reason.
3. Each row opens the question's editor.
4. Nothing is auto-retired; every action is the author's.
5. The screen shows nothing rather than zeros when RES-06 has not run.

**Tests** — *integration*: each flag fires on its condition; the empty state when
no statistics exist. *e2e*: the list, the sentences in Arabic, and a row opening
the editor.

#### RES-08 · Watch a paper wear out
**SHOULD · NOT BUILT**

As a **training coordinator**, I want to know when a form or a question has been
seen by too many people, so that I write a replacement before it stops measuring.

**Acceptance**
1. Exposure is reported as a rate — times served over candidates — not only as a
   raw count.
2. The ceiling is a tenant setting rather than a constant. *(Today it is
   `OverExposedAfterServings = 500`, compiled in.)*
3. Crossing it appears as a publish-time warning naming the questions.

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

---

# Epic 12 — The tenant's own face

*`TenantBranding` is an entity, a table and a migration. There is no application
service, no DTO, no endpoint and no screen. `IsUsableColor` is written and called
from nowhere.*

#### BRD-01 · Put our name on it
**MUST · NOT BUILT**

As an **administrator**, I want to set our organisation's name and logo, so that
the people we invite see us rather than a platform they have never heard of.

**Acceptance**
1. Name, alternate-language name, logo, icon, brand colour, certificate footer and
   support email are saved as one record per tenant.
2. An organisation operating in one language is not required to invent a second
   name.
3. A tenant with no branding falls back to a neutral default and never shows
   another tenant's.

**Tests** — *integration*: one row per tenant; fallback; isolation. *e2e*: set a
name and logo and see them in the shell.

#### BRD-02 · Refuse a colour that will fail silently
**MUST · PARTIAL**

As an **administrator**, I want a bad colour rejected when I enter it, so that I
do not end up looking unbranded with no explanation.

**Acceptance**
1. Only `#rrggbb` is accepted. *(Domain rule written — `IsUsableColor` — and never
   called.)*
2. Rejection is at the point of entry, with a message, not a silent fallback.
3. The colour is chosen from a picker as well as typeable.
4. Derived hover, active and subtle variants keep their contrast ratios whatever
   colour is chosen, and a test asserts the derived text-on-brand contrast for a
   very light and a very dark brand colour.

**Tests** — *unit*: `IsUsableColor` across valid, short, long, non-hex and null;
the contrast derivation at both extremes. *integration*: the service rejects.
*e2e*: enter an invalid colour and read the message.

#### BRD-03 · Carry the branding to where it matters
**MUST · NOT BUILT**

As a **candidate**, I want the exam page and the invitation to carry the
organisation that invited me, so that it does not read as a phishing attempt.

**Acceptance**
1. The invitation email, the link preview, the exam page, the result page and the
   certificate all carry the tenant's name and logo.
2. The brand colour flows through the token layer to all of them.
3. The support address shown during an exam is the tenant's, not ours.
4. None of these surfaces requires a login to render the branding correctly.

**Tests** — *integration*: the rendered email body carries the tenant's name and
support address. *e2e*: open a link as an anonymous visitor and assert the logo,
the name and the colour.

#### BRD-04 · Speak the tenant's language everywhere
**SHOULD · NOT BUILT**

As a **candidate**, I want the exam to use my centre's vocabulary, so that a
student is not addressed as a candidate.

**Acceptance**
1. Taker-facing text uses `CategorySet`'s subject vocabulary.
2. Where the tenant has set only one language's labels, the other falls back to
   that one rather than to the platform default.

**Tests** — *integration*: the fallback. *e2e*: rename to "Student" and assert the
taker screens follow.

#### BRD-05 · Preview the branding before it is live
**COULD · NOT BUILT**

As an **administrator**, I want to see what a candidate will see, so that I can
check the logo and colour before anyone is invited.

**Acceptance**
1. A preview renders the link page and the invitation with the unsaved values.
2. The preview cannot send an email.

**Tests** — *e2e*: change a colour, preview, confirm nothing was saved or sent.

---

# Epic 13 — Access and administration

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

**Tests** — *integration*: each permission gates its own endpoint. *e2e*: log in
as a role holding only `Review.Grade` and confirm the exam screens are absent.

#### ADM-02 · Do not offer what cannot be opened
**MUST · PARTIAL**

As a **reviewer**, I want the navigation to show only what I can reach, so that I
am not sent to a dead end.

**Acceptance**
1. A navigation entry the user lacks permission for is not rendered. *(Guards
   built.)*
2. Every rendered entry resolves to a registered route. *(Today
   `angular/src/app/core/navigation.ts` links to `/questions`, `/groups`,
   `/assignments`, `/results`, `/catalog`, `/users` and `/settings`, none of which
   is registered; all seven fall through to the wildcard redirect.)*
3. A route under construction says so rather than showing an empty table.

**Tests** — *e2e*: enumerate every rendered navigation link and assert each
navigates somewhere that is not the wildcard redirect. This is the story's whole
point, so the assertion must be exhaustive rather than sampled.

#### ADM-03 · Keep tenants apart
**MUST · BUILT**

As an **administrator**, I want to be certain another organisation cannot see our
data, so that the product can be sold to two competitors at once.

**Acceptance**
1. Every entity under `Assessment` implements `IMultiTenant`, asserted by
   reflection so a new entity cannot forget.
2. A query as one tenant returns no row belonging to another, including through
   `ExamLink`.
3. Where the filter must be disabled for an anonymous taker, the attempt is loaded
   through the session's own claims and never by a caller-supplied id.

**Tests** — *integration*: `TenantIsolationTests`, extended for each new entity —
`ExamSection`, `ExamForm`, `ExamFormQuestion` are not yet covered.

#### ADM-04 · Keep the contexts apart
**SHOULD · BUILT**

As an **administrator**, I want the module boundaries enforced by the build, so
that the structure survives contact with a deadline.

**Acceptance**
1. A cross-context entity reference that points upward fails the build.
2. A contract referencing the domain fails the build.
3. The failure names the offending type and the rule.

**Tests** — *unit*: `ModuleBoundaryTests` and `ContractBoundaryTests`.

#### ADM-05 · Manage staff accounts
**SHOULD · PARTIAL**

As an **administrator**, I want to create staff users and set their roles, so that
a new coordinator can start work.

**Acceptance**
1. Users are created, edited, deactivated and given roles. *(Server built —
   `UserAppService`.)*
2. An administrator cannot remove their own last administrative role.
3. There is a screen. *(There is not; `/users` is a dead link.)*

**Tests** — *integration*: the self-lockout refusal. *e2e*: create a user, assign a
role, log in as them.

#### ADM-06 · Configure the tenant
**COULD · PARTIAL**

As an **administrator**, I want tenant-wide settings in one place, so that
thresholds are not compiled into the product.

**Acceptance**
1. The exposure ceiling (RES-08), the file size limit and the self-registration
   switch are settings.
2. Changing one takes effect without a restart.
3. Each has a documented default used when unset.

**Tests** — *integration*: a changed setting is read by the code that uses it.
*e2e*: change the exposure ceiling and see the publish warning change.

---

# Epic 14 — How the product behaves everywhere

#### PLT-01 · Never make anyone learn syntax
**MUST · PARTIAL** · ⚠ constraint

As a **teacher**, I want to operate the whole product without knowing any code, so
that I am not dependent on someone technical to set an exam.

**Acceptance**
1. A test enumerates `QuestionTypes` and asserts each resolves to a registered
   payload editor. *(Fails today for six types.)*
2. The raw-payload textarea does not render for any shipped type.
3. No field in the product accepts a regular expression, a JSON document, an HTML
   fragment or a template placeholder as author input.
4. Hotspot regions are drawn on the image; `X`, `Y`, `Width` and `Height` are never
   rendered as inputs.
5. A fill-in-the-blank blank is created by selecting a word and pressing a button;
   no placeholder syntax is typed, and the prompt a candidate sees contains no
   author markup.
6. An ordering question's correct order is set by dragging; `CorrectPosition` is
   never typed.
7. A matching question's pairs are entered as adjacent rows; no identifier is
   typed.
8. Catalogue codes are generated from the name and never required.

**Tests** — *unit*: the enumeration in (1). *e2e*: one spec per question type
authoring through controls only and asserting no raw field, no coordinate field
and no typed placeholder appears anywhere on the screen. This story's value is
entirely in the assertions being exhaustive rather than representative.

#### PLT-02 · Read correctly in Arabic
**MUST · PARTIAL**

As a **teacher**, I want the whole product to work in Arabic, so that it is not an
English product with Arabic pasted in.

**Acceptance**
1. Layout uses logical properties throughout; no screen scrolls the page
   horizontally at a phone viewport in Arabic.
2. Letter-spacing is not applied to Arabic text.
3. Switching language changes the text as well as the direction.
4. Every screen in the product is covered, not only the ones built first.

**Tests** — *e2e*: the existing RTL-at-phone-viewport pass, extended to each new
screen as it lands. Three real defects were found this way, including a table that
scrolled the whole page sideways, so the harness stays authoritative.

#### PLT-03 · Meet the accessibility standard the buyer names
**MUST · NOT BUILT**

As an **administrator**, I want to answer a public-sector accessibility question
truthfully, so that we are not disqualified from a bid.

**Acceptance**
1. Every screen passes an automated check with no critical or serious violations,
   in both languages.
2. Keyboard operation covers every interactive control.
3. The product is usable at 400% zoom without horizontal scrolling.
4. The compliance page names EN 301 549 / WCAG 2.1 AA, because that is the phrase
   on the buyer's checklist, while the build targets 2.2 AA.

**Tests** — *e2e*: axe assertions across every route in both languages; a
keyboard-only traversal.

#### PLT-04 · Tell people what went wrong
**MUST · BUILT**

As a **candidate**, I want to be told what happened in words, so that I know what
to do next.

**Acceptance**
1. Every business failure raises a named error code, not a status code.
2. Every code resolves to a localised message in Arabic and English.
3. No raw code is ever displayed to a user.
4. A new code without a localisation entry fails a test.
5. `IMS:ExamForm:NoQuestions` and `IMS:ExamForm:DuplicateQuestions` have entries in
   both languages. *(They do not. Of the 27 codes declared today, those two — both
   added with `ExamForm` — are missing from `en.json` and `ar.json`, so publishing
   an empty or duplicated form raises a failure that shows the reader a raw code.
   This is the exact defect criterion 4 exists to prevent, found by writing the
   check this story asks for.)*

**Tests** — *unit*: assert every constant in
`InternshipManagementSystemDomainErrorCodes` has an entry in both localisation
files. The test does not exist and currently fails on two codes. *e2e*: trigger
three failures and read three sentences.

#### PLT-05 · Never hand over the answer
**MUST · BUILT**

As a **training coordinator**, I want a structural guarantee that answer keys stay
on the server, so that the exam is worth setting.

**Acceptance**
1. No DTO reachable by a taker references the domain.
2. The projector copies an explicit field list; a new payload field is invisible
   to a taker unless it is added deliberately.
3. Recorded option order is stored for every type whose order carries the answer,
   regardless of the exam's shuffle setting.

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

**Tests** — *unit*: thirteen graders against a shared battery of hostile inputs.
*integration*: the attempt's end state after a grader failure.

#### PLT-07 · Cover every story from unit to end to end
**MUST · PARTIAL**

As an **administrator**, I want the test pyramid to actually exist, so that this
document's plan is real.

**Acceptance**
1. Every story in this document names its layers, and every named layer exists
   before the story is called done.
2. The following have no tests at all today and are not done until they do:
   `ExamAppService`, `AssignmentAppService`, `ExamTakingAppService`,
   `ReviewAppService`, `AttemptGradingService`, `ExamFormBuilder`,
   `ExamSessionTokenService`, `AttemptTimeoutWorker`.
3. Playwright specs stub HTTP today; at least one spec per epic runs against a real
   backend, because a stub cannot catch a contract drift.
4. The ABP template's sample tests are removed rather than counted.

**Tests** — this story is the test plan; its acceptance is measured by the coverage
of the others.

#### PLT-08 · Be honest about what a score means
**SHOULD · NOT BUILT**

As a **training coordinator**, I want the product to state the limits of its own
numbers, so that I do not claim more for a result than it can carry.

**Acceptance**
1. Where forms are not equated, the result and the export both state that scores
   are comparable within a form and not across forms.
2. An item statistic computed on too few responses is labelled as provisional
   rather than shown as a number.
3. An integrity signal is never presented as a conclusion.

**Tests** — *integration*: the statement appears on the export and the result.
*e2e*: the statement is visible, not hidden behind a tooltip.

---

# Summary

## By status

| Status | Stories |
|---|---|
| **BUILT** | 20 |
| **PARTIAL** | 43 |
| **NOT BUILT** | 57 |
| **Total** | **120** |

## By status and priority

| | MUST | SHOULD | COULD | Total |
|---|---|---|---|---|
| **BUILT** | 17 | 3 | 0 | **20** |
| **PARTIAL** | 30 | 11 | 2 | **43** |
| **NOT BUILT** | 25 | 21 | 11 | **57** |
| **Total** | **72** | **35** | **13** | **120** |

## By epic

| Epic | Stories | of which PARTIAL |
|---|---|---|
| 1 · The catalogue and the tenant's vocabulary | 6 | 0 |
| 2 · The question bank | 12 | 4 |
| 3 · Getting existing exams in | 5 | 0 |
| 4 · Exams, sections and publishing | 12 | 8 |
| 5 · Blueprints and per-candidate assembly | 7 | 2 |
| 6 · Named forms | 8 | 3 |
| 7 · People and cohorts | 6 | 0 |
| 8 · Assignment and links | 9 | 5 |
| 9 · Sitting the exam | 16 | 10 |
| 10 · Grading and the reviewer's queue | 9 | 3 |
| 11 · Results, item health and export | 11 | 1 |
| 12 · The tenant's own face | 5 | 1 |
| 13 · Access and administration | 6 | 3 |
| 14 · How the product behaves everywhere | 8 | 3 |

## What the shape of those tables says

**PARTIAL is the largest category after NOT BUILT, and it concentrates in three
epics.** Sitting the exam, assignment and links, and the reviewer's queue account
for eighteen of the forty-three, and in almost every case the missing layer is
Angular rather than C#: the application service exists, is permission-checked, is
careful, and has no screen. That is the cheapest work in this document per unit of
demonstrable product, and it is why
`docs/business/business-review.md` §8 puts it first.

**Epics 1, 3 and 7 have no PARTIAL stories at all** — the catalogue, import, and
people and cohorts are empty from the domain outward. They are small, and nothing
above them can be configured by a customer without them.

**Seventeen of the seventy-two MUST stories are BUILT.** The remaining fifty-five
are the first sellable release.

**Four MUST stories are defects rather than features** and all four are cheap:
BNK-07 (the shared bank is never drawn into a candidate's paper), ADM-02 (seven
navigation links go nowhere), BNK-01 / PLT-01 (six question types present a raw
JSON box, breaking the owner's authoring constraint), and PLT-04 (two of the
twenty-seven error codes have no localised message in either language, so the
person who hits them is shown `IMS:ExamForm:NoQuestions`).

**Eight application services have no tests.** PLT-07 names them. Every one is on
the critical path of the first release, and `ExamTakingAppService` — the largest
and the one a candidate's marks depend on — is among them.
