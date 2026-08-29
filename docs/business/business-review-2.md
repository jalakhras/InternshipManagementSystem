# Business review, second pass

`business-review.md` was written a few weeks and roughly twenty commits ago. It
made one finding worth more than the rest of it put together: three features that
the commit log described as working were not reachable, and the review found that
by opening the files.

This document assumes nothing in that one is still true, and re-derives the
business case by reading the code.

It stands beside the first review rather than replacing it, so the change in view
is visible. Where the two disagree, this one is right about the code and the first
one is right about the reasoning that got us here.

**The method, stated so it can be checked.** Every claim below that a thing is or
is not built was verified by opening the file and following the call. Where a
route is claimed to exist I looked for a controller with that route. Where a
column is claimed to be written I grepped for the assignment. Numbers that are
estimates are labelled as estimates.

**The state this is pinned to, and a warning about it.** The analysis was made
against `342efe5` ("Build the reviewer's queue and the screen where marks are
awarded") and the working tree beside it. **The tree moved twice while this was
being written.** The reviewer's queue was uncommitted when I started reading and
committed by the time I finished; and an in-flight change — `CandidateGroupForm`
deleted, `Assignment.ExamFormId` and `Attempt.ExamFormId` added,
`ExamTakingAppService` grown by a hundred lines — appeared in the tree partway
through. §2.3 and §8 are written against that change specifically, including a
defect it introduces.

That is itself worth recording as a business fact: **a review of this codebase has
a shelf life measured in hours, and the product has no customer.** The ratio of
those two numbers is the thing this document is really about.

---

## 0. The headline, before the argument

Three sentences, and the rest of the document supports them.

1. **The first-customer recommendation survives, but its lead changes.** Still a
   vocational training academy — and now sell the **end-of-level exam**, not the
   placement test, because placement needs a score profile and the profile is the
   one thing four separate half-built features all fail to produce.
2. **Nothing is sellable today, and the reason is not a missing feature — it is
   that the results never reach the person who paid.** A fully automatic exam is
   graded, stored, and then visible to nobody but the candidate. There is no
   results screen, no attempt list, no roster and no export anywhere in the
   product.
3. **The gap to a paid pilot is about four to six weeks of work on things that
   are almost all small**, and the largest single risk in it is not engineering —
   it is that we still have no named institution.

And one thing that is not a business finding but should not wait for one:
**the named-form delivery change currently in the working tree hands the candidate
the answer key to every matching and ordering question.** §2.3 has the detail. It
is a few lines to fix and it should be fixed before that change is committed.

---

## 1. What is actually there now

The last review's three headline defects are genuinely fixed. I checked all
three, and two more besides.

| Claim from last time | Status now | Evidence |
|---|---|---|
| The taker's application does not exist | **Fixed** | `angular/src/app/features/take/` — `take-entry`, `take-sitting` (393 lines, the largest component in the app), `take-result`. Server-authoritative countdown, debounced autosave, auto-submit at zero, integrity instrumentation |
| The shared bank never reaches a paper | **Fixed in code** | `Question.DrawableBy(...)` is now used in `ExamTakingAppService.OpenLinkAsync` and `StartAsync`, and in `ExamAppService.CheckPublishAsync`. Five tests in `SharedBankTests.cs`. *(But see §2.4 — it is unreachable through the product)* |
| `TimesServed` is a column nothing writes | **Fixed** | `ExamTakingAppService.RecordExposureAsync` increments it for every question that lands on a paper, in one batched update |
| Six question types fall back to a raw JSON box | **Fixed** | `angular/src/app/features/questions/payload/payload-editor.ts` registers 13 keys across 9 editor components — every shipped type. The raw textarea now renders only for a type the client has never heard of, which is the correct behaviour. A Playwright test loops all 13 and asserts zero raw-payload fields |
| Two error codes had no localised message | **Fixed** | `ErrorCodeCoverageTests` asserts every code resolves in both `ar.json` and `en.json`, and both files carry identical keys |

Genuinely new and genuinely good since then:

- **Sections and named forms have a real API.** `ExamStructureAppService` (452
  lines) and `ExamStructureController` — create/update/delete a section; create,
  generate-from-bank, hand-pick, publish, retire and delete a form. Ten service
  tests in `ExamStructureTests.cs` including "same seed produces the same paper"
  and "a published form is immutable".
- **Candidates and cohorts have a real API and a real screen**, and the paste
  import is better than it needed to be: comma or tab, the email column found
  rather than assumed, a dry run that writes nothing, per-line error reporting,
  idempotent re-import. Nine tests.
- **Weighted best-answer scoring**, with 14 grader tests and 8 authoring-validator
  tests covering the "tick everything and win" hole.
- **The reviewer's queue and the marking screen exist** — `review-queue` and
  `review-attempt`, 1,585 lines across six files, wired into `review.routes.ts`,
  committed in `342efe5`. `PlaceholderComponent` is now referenced by no route at
  all, which is the first time that has been true.
- **Difficulty index is now computed.** `AttemptGradingService.RecordOutcome`
  maintains a running mean per question.
- **The stimulus renderer is built**, contradicting the first review: the taker
  screen renders a group's instructions, media and passage text before the
  question. There is still no authoring screen for a group.

That is a real product's worth of parts. The problem is not the parts.

---

## 2. What the code says that the commit log does not

Seven findings. The first two decide the rest of the document.

### 2.1 The results never reach the customer

`ReviewAppService.GetQueueAsync` filters on:

```csharp
where attempt.IsSubmitted && attempt.NeedsManualReview
```

Our first customer's actual exam — the thirty-question, all-single-choice trading
paper in `الاختبار.docx` — is graded automatically the instant it is submitted.
`NeedsManualReview` is false. **It therefore never appears in the review queue,
and there is nowhere else it could appear.**

I searched the whole solution. `InternshipManagementSystemPermissions.Results.View`,
`.Export` and `.ViewItemAnalysis` are defined in the permission provider and
referenced by **no application service, no controller and no component**. There is
no `Results` folder in `Application`, `Application.Contracts`, `HttpApi` or
`angular/src/app/features`. The sidebar entry for `/results` points at a route
that is not registered.

The nearest thing to a result a member of staff can see is the number `3` in the
"attempts" column of the candidate list, which is not a link.

So the path ends like this: forty students sit the exam, each sees their own score
on their own phone, and the coordinator who bought the product asks them what it
said. **This is the sharpest break on the sellable path, and it is not close.**

### 2.2 Every image, every audio clip and every uploaded file is a 404

Five places in the codebase build a URL of the form `/api/assessment/media/{blob}`:

- `ExamTakingAppService.BuildMediaUrl` — what the candidate's browser requests
- `AssessmentMediaAppService.UploadAsync` — the URL returned after an upload
- `ReviewAppService` — `AnswerFileUrl`, how a marker opens an uploaded answer
- `angular/src/app/shared/ui/media-field.component.ts` — the author's preview
- `angular/src/app/features/questions/payload/hotspot-editor.component.ts` — the image regions are drawn on

**No controller anywhere serves that route.** `src/InternshipManagementSystem.HttpApi/Assessment/`
contains seven controllers and none of them is a media controller.
`AssessmentMediaAppService` is exposed only by ABP's conventional controllers,
which the host configures with the default prefix — so it lives at `/api/app/...`,
not `/api/assessment/media/...`. The upload itself is broken for the same reason:
`media-field.component.ts` POSTs to `/api/assessment/media`.

And even if the path were corrected, the read would still fail for a candidate:
the service carries a class-level `[Authorize]`, and candidates have no account by
deliberate design.

`take-sitting.component.html` renders `<img>`, `<audio>` and `<video>` against
those URLs. A candidate sitting a listening exam gets a dead player. A candidate
sitting the trading exam gets a broken-image icon next to "which of these
statements does not apply to the green candle".

The reason this survived is instructive and worth writing down: `question-form.spec.ts`
uploads a real PNG and asserts the preview renders — with `page.route('**/api/assessment/media', …)`
stubbing exactly that URL. **The test asserts that our own mock is reachable.**
Every Playwright test in the suite runs against stubs; there is no
against-the-real-API suite and no CI to run one.

The commercial cost is specific. §7.2 of the first review made *"your Google Form
lost the chart, and we keep it"* the opening argument to the first customer, in
the room, using their own file. That argument is currently a live demonstration of
our own product losing the chart.

### 2.3 Named forms could not be delivered to anybody — and the fix, landing now, leaks the answer key

**As committed at `342efe5`:** `ExamDeliveryMode` is declared in
`AssessmentEnums.cs`; `Exam.DeliveryMode` and `Exam.FixedFormId` are declared on
the entity; a grep across `src/`, `angular/src/` and `test/` returns three hits,
all of them the declaration itself. `StartAsync` calls
`_formBuilder.Build(exam, bank, …)` unconditionally. An author could build Form 1,
read it, approve it, publish it, attach it to a class — and every candidate
received a random per-candidate draw, silently, with no error. `ExamForm.TimesUsed`
was read three times and written never, so `DeleteFormAsync`'s guard against
deleting a used form could not fire: a paper forty people sat could be deleted
outright.

**In the working tree, as of this writing, that is being fixed, and well.**
`Assignment.ExamFormId` and `Attempt.ExamFormId` are added,
`BuildFromNamedFormAsync` builds the paper from `ExamFormQuestion` in
`DisplayOrder`, `TimesUsed` is incremented, a deleted question is skipped rather
than failing a candidate mid-sitting, and the link id is added to
`ExamSessionClaims` so the resit defect in §2.7 goes with it. This is the change
`classes-and-forms.md` specified and it is the right one.

**It also introduces an answer-key leak, and I would stop the commit for it.**

`BuildFromNamedFormAsync` constructs each `AttemptQuestion` without setting
`OptionOrder`. `TakerQuestionProjector` then calls
`OptionIdReader.ApplyOrder(items, savedOrder, …)`, and `ApplyOrder` returns the
list **unchanged** when the order is null. Follow that through for two of the
thirteen types:

- **Matching.** `Display["left"]` and `Display["right"]` are both projected from
  `spec.Pairs` in stored order. With no recorded shuffle, `right[i]` is the correct
  match for `left[i]`. **The key is in the JSON the candidate's browser receives.**
- **Ordering.** The items are emitted in `spec.Items` order, which is the authored
  — that is, correct — sequence.

`ExamFormBuilder` gets this right and says so in a comment: it shuffles option ids
whenever `exam.ShuffleOptions` is set *or* the type is matching or ordering,
"whatever the exam says", because for those two the recorded order is the only
thing that pulls the answer apart from the prompt. The new path does not go
through that builder and does not reproduce the rule. `exam.ShuffleOptions` and
`exam.ShuffleQuestions` are both ignored on a named form as well.

The projector's own test suite passes, because `TakerQuestionProjectorTests`
supplies an order explicitly. The defect is in the caller, and the caller is
`ExamTakingAppService` — the one service on the critical path with **no tests at
all** (§2.7). This is the same failure shape as the media route in §2.2: the
component is tested, the wiring is not, and the wiring is where the product
lives.

The fix is small — record an option order in `BuildFromNamedFormAsync` using the
same `ShuffleOptionIds` rule, seeded per attempt — but it must ship *with* the
change, not after it.

**What remains unbuilt even after this lands.** `Exam.DeliveryMode` and
`Exam.FixedFormId` are still read by nothing: a form is selected per assignment,
which is the right primary mechanism, but `FixedForm` and `RotateForms` as
exam-level defaults remain dead enum values. And there is still no screen anywhere
for building, reviewing or picking a form (§4, step 6), so the whole feature is
reachable only through Swagger.

### 2.4 The catalogue does not exist, and it silently disables four features

There is no `CatalogAppService`. No controller. No DTOs. No Angular feature
folder. No route. No seed data. `Category`, `Level`, `Topic` and `CategorySet` are
entities and tables and nothing else. The `Catalog.View` / `Catalog.Manage`
permissions exist with nothing behind them, and `/catalog` is a dead nav link and
a dead dashboard step.

`exam-form.component` reads `categoryId` and `levelId` when loading an exam and
has no form control for either. `question-form.component` patches `examId` from
the route and never touches `categoryId`, `levelId` or `topicId`.

Follow that through:

- Every exam has `CategoryId == null`. Every question has `ExamId` set and
  `CategoryId == null`. `Question.DrawableBy` therefore reduces to
  `question.ExamId == examId`. **The shared item bank is correct in code, covered
  by five tests, and unreachable through the product.** No customer can create a
  bank question.
- Every question has `TopicId == null`, so `BuildTopicBreakdownAsync` returns an
  empty list on every attempt. The per-competency breakdown on the result screen —
  the thing that makes a score actionable — is always empty.
- Blueprint rules key on topic and difficulty; there is no blueprint screen and no
  topics to key on. `ExamService.getBlueprint()` has zero callers.
- `CandidateGroup.LevelId` — the point of last week's "a cohort is a class at a
  level" — can never be set to anything.

One missing CRUD screen turns four advertised capabilities into no-ops. It is the
cheapest high-value work in the repository.

### 2.5 Sections are authoring-only

`ExamFormBuilder.Build` never reads `ExamSectionId`. `AttemptQuestion` has no
section column. `AttemptGradingService` has no notion of a section.
`ExamSection.IsFailedAt` and `IsQualifying` are exercised only by
`ExamFormTests.cs` — a pure domain unit test — and by nothing in the running
system.

The first review's §3 recommended building the taker and the result section-aware
from the first line of code, with one implicit section, precisely so the retrofit
would not be needed. That recommendation was not taken. The retrofit cost is now
real: paged navigation, a per-section clock, per-section scoring, per-section
reporting, and a section id on both `ExamFormQuestion` and `AttemptQuestion`
(`classes-and-forms.md` open question 3 notes that named forms and sections, the
two features of this quarter, do not currently compose at all).

### 2.6 Discrimination is still a column nothing writes

Difficulty is now computed. Discrimination is not: grep for `DiscriminationIndex`
returns the entity property, the EF precision config, the DTO, the DTO projection
— and no assignment anywhere. `QuestionDto.discriminationIndex` reaches the
Angular model and is rendered by nothing.

Two things follow. The item-health chip in `question-list.component.ts` classifies
questions as `unmeasured | healthy | tooEasy | tooHard` from difficulty alone,
which cannot distinguish "hard" from "the key is wrong" — the single most useful
thing the pair is for. And "these six questions are not measuring anything",
which `competitive-position.md` calls the most credible sentence we can say to an
assessment professional, is a sentence we cannot yet say.

The difficulty index that *is* computed has a latent correctness bug the research
document predicted (Q3): it is a lifetime running mean, never reset when the
question or its key is edited. The bug was invisible while the column was null.
It is now accumulating.

### 2.7 Smaller, but each of them is a sentence in a demo

- **Seven of eleven sidebar entries lead nowhere.** `/questions`, `/groups`,
  `/assignments`, `/results`, `/catalog`, `/users`, `/settings` are not registered
  routes; they fall through the `**` wildcard and silently bounce the user to the
  dashboard. The user-menu link to `/account/profile` does the same.
- **The assignment screen's send button can never be enabled.** It is gated on
  `!groupId()`, cohorts are the only permitted target, and `createGroup` /
  `updateGroup` / `deleteGroup` exist on the service with zero callers — there is
  no way to create a cohort in the UI. (Fixable by seeding one row, but it means
  the flow has never been walked.)
- **Candidates cannot be created or edited by hand.** `canEdit` is declared in
  `candidate-list.component.ts` and never referenced; `create`, `update` and `get`
  are unreachable. Import is the only way a person enters the system.
- **Email is mandatory and unique per candidate.** A vocational academy where
  students share a family address, or have none, cannot import its roll.
- **Three of thirteen question types are unanswerable.** `hotspot`, `file-upload`
  and `audio-response` have no answer component and fall back to a plain textarea.
  There is a 288-line hotspot region editor for authors and a text box for the
  person answering.
- **A second link to the same exam burns the wrong one.** `StartAsync` resolves
  the link by `CandidateId + ExamId + !IsRevoked` and `ExamSessionClaims` carries
  no link id, so a resit — the motivating scenario for named forms — picks
  whichever row the database returns first. *(Being fixed in the in-flight change:
  `ClaimLinkId` is added to the session token.)*
- **Tenant branding is a table.** No service, no screen, no consumer. The
  invitation email is hardcoded bilingual HTML with no tenant name or logo, which
  is the "reads as phishing" failure the competitive document warned about.
- **The exam-taking service has no tests.** 128 .NET test methods across 24 files,
  and `ExamTakingAppService` — start, resume, deadline, save, submit — is
  referenced by none of them. Neither is `AssignmentAppService`,
  `ExamSessionTokenService`, `ReviewAppService`, `AttemptGradingService` or
  `AssessmentMediaAppService`.
- **The permission model has zero verification.** `InternshipManagementSystemTestBaseModule`
  calls `AddAlwaysAllowAuthorization()`, so no `[Authorize]` attribute is ever
  exercised; the Playwright tests assert only that buttons are *hidden*. Multi-tenant
  isolation is the honourable exception and is properly covered.
- **There is no CI, no Dockerfile, no installer and no deployment manifest.** SMTP
  in `appsettings.json` points at `127.0.0.1:25` with no credentials. The repo's
  own `README.md` still calls the product an Internship Management System and
  claims "Dashboards for Review & Results ✅ Implemented" and "File Upload & Media
  Handling ✅ Working".

---

## 3. Is the first-customer recommendation still right?

**Yes — the vocational training academy — but lead with the end-of-level exam,
not the placement test.**

That is a change, and it is forced by what §2 found rather than by a change of
opinion about the buyer.

### Why the academy survives the retest

The academy's purchase is one exam, one paper, one score, one pass mark, one
level. Everything in that sentence except the level is built and tested:
`Attempt.ApplyScore(score, maxScore, passingPercentage)`, thirteen graders,
weighted best-answer scoring, a publish gate that reports every blocker in one
pass, a link per person that binds a response to a name without a login, and a
marking queue for the written half. The academy already owns its bank, which is
the content problem that kills trials; a recruiter expects us to supply questions
and a language centre expects CEFR mapping, and we have neither.

Against a Google Form we hold advantages a form structurally cannot copy. Against
TestGorilla or Evalufy we are a new product with no references. Win where the
incumbent is a form.

Nothing in the last three weeks weakened any of that.

### Why the lead changes from placement to the level exam

The first review said "sell the placement test before the level exam", because
placement is high volume, low stakes and has a small bank.

A placement test's output is *which class to put the student in*. That requires a
profile, and the profile is exactly what the product cannot produce today, in four
independent ways: sections never reach delivery (§2.5), topics can never be set
(§2.4), so `BuildTopicBreakdownAsync` always returns empty, discrimination is
never computed (§2.6), and there is no staff-facing results view of any kind
(§2.1). A placement test that returns 62% and nothing else is a test the
coordinator cannot act on, which is the exact criticism `competitive-position.md`
levels at single-number placement.

The end-of-level exam wants one number against a pass mark. That is the shape the
system genuinely produces. It is also higher stakes, which is where the link,
the revocation, the integrity signals and the approved paper are worth paying for
— on a low-stakes placement test none of that matters.

The first review's counter-argument was that the level exam is where the two
hundred questions live. That is true and it is the right thing to solve with the
importer and with paid onboarding, not by selling the weaker product first.

### What would change my mind

Three things, concretely:

1. **If the first two academy conversations both say the reason their exams are on
   paper is invigilation, not marking.** Then our whole differentiator is aimed at
   a problem they do not have, and the buyer becomes whoever assesses people who
   are not in the room — a recruiter, or a distance-learning provider.
2. **If a named language centre or certification body is willing to sign before
   sections ship.** A signed contract beats an argument about sequencing. The
   money is larger and the reference is better. The risk is that sections,
   per-section timing and per-section reporting are the largest remaining build in
   the product and we would be selling a promise on it.
3. **If the importer turns out to be hard.** The academy case rests on the claim
   that we can absorb their Word and Google Forms exams cheaply. If the real files
   are less regular than `الاختبار.docx` — scanned PDFs, tables, images inline —
   then the content barrier is unchanged and every buyer is equally cold, in which
   case pick the one with the biggest budget rather than the shortest path.

---

## 4. What is sellable today, with no further engineering

**Nothing.** Not a reduced version, not a services-led version. Here is the walk,
step by step, with each break named.

| # | Step the customer takes | What happens |
|---|---|---|
| 1 | Sign up | **Break.** There is no self-service tenant creation in the Angular app, no tenant management screen, and no users screen — `/users` and `/settings` are dead nav entries. A tenant is created by us, out of band. Acceptable for a pilot; not a product. |
| 2 | Define a domain and a level | **Hard break.** §2.4. No catalogue exists — no service, no API, no screen, no seed. The customer cannot create a category, a level or a topic, and there is no form control on any screen that would accept one. |
| 3 | Rename the vocabulary to theirs | **Break.** `CategorySet` — "what makes the product speak the tenant's language", one of the three claimed differentiators — is a table with no service. An academy is shown "Candidates" and "Groups". |
| 4 | Write questions | **Works, well.** Thirteen types, thirteen editors, a formatting editor for the prompt, a publish-time payload validator. This is the best part of the product. Two scratches: after creating a question the form does not navigate, so pressing Save twice creates two questions; and there is no way to write a bank question, only an exam-owned one (§2.4). |
| 5 | Attach a chart or a recording | **Break, and it is the one that embarrasses.** §2.2. The upload POSTs to a route that does not exist. If it did, the preview would 404. If that were fixed, the candidate's request would be 401. |
| 6 | Build a paper | **Half works.** Sections and named forms have a complete, tested API and **no screen at all** — no component, no service method, no route, no model in `assessment.models.ts`. There is also no blueprint screen. So in practice "the paper" is "every question in the exam, shuffled". |
| 7 | Approve the paper and send that one | **Break.** §2.3. Publishing a form is possible over the API and has no effect on what anybody sits. |
| 8 | Import a roll | **Works, and it is good.** Paste comma or tab, dry run, per-line errors, idempotent. **But** every person needs a unique email address, and there is no way to add or correct a person by hand afterwards. |
| 9 | Put them in a class | **Break in the UI.** The cohort API is complete; `createGroup` has no caller. With no cohort, the assignment screen's send button is permanently disabled. |
| 10 | Send links | **Works once a cohort exists** — a token per person, hashed at rest, bilingual invitation, per-recipient failure reporting, a copy-all panel. **Three breaks:** the plaintext link is returned exactly once at creation and can never be recovered (the list shows an 8-character prefix), there is no resend, and the email is unbranded and goes to `127.0.0.1:25` until SMTP is configured. |
| 11 | Someone sits it | **Works, and it is the strongest screen in the app.** Server-authoritative clock, one question at a time, debounced autosave, resume, auto-submit, integrity signals. Three of thirteen types are unanswerable (§2.7) and all media is broken (§2.2). |
| 12 | Mark the written answers | **Works** — in the uncommitted working tree. Queue, rubric, running total, integrity report. |
| 13 | Results come back | **Total break.** §2.1. For an auto-graded exam, no member of staff can see any result, anywhere, ever. No roster, no export, no attempt list. |

Two of thirteen steps work end to end without qualification.

The honest summary for a meeting: **we can demonstrate authoring and we can
demonstrate sitting an exam; we cannot demonstrate a customer receiving their
results, and we cannot show a question with a picture in it.**

---

## 5. The shortest path to one real institution running one real exam

Not a wish list. This is the ordered minimum, with the risk at each step. My
estimates are in developer-days and are labelled as estimates; the ordering is
the argument and I am more confident of it than of the numbers.

| # | Do this | Est. | Risk if skipped or wrong |
|---|---|---|---|
| 1 | **A media controller.** One controller at `/api/assessment/media`, `[AllowAnonymous]` on the read with the blob name as the only credential (they are unguessable GUIDs under a tenant prefix), `[Authorize]` on write and delete. Drop `.svg` from the allowlist — a stored SVG served same-origin is script. | 1 | Nothing with a picture or a sound works. Removes the opening sales argument. |
| 2 | **The catalogue.** Category, Level, Topic CRUD; generate the `Code` from the name and never require it; pickers on the exam and question forms. | 3–4 | Blocks the shared bank, all topic reporting, blueprints, and the class's level. Four features stay dark. |
| 3 | **Results.** One screen: attempts for an exam, with candidate, submitted-at, score, percentage, pass/fail, integrity flag count; one answer sheet; a CSV export. `Results.View` and `.Export` already exist as permissions. | 4–5 | The customer never receives what they bought. This is the deal. |
| 4 | **Finish the named-form delivery change already in the tree** — specifically, record an `OptionOrder` in `BuildFromNamedFormAsync` so a named form does not hand the candidate the key to every matching and ordering question (§2.3), and add a test that asserts it. The rest of this step is done. | 1 | Ships an answer-key leak into the first paying customer's exam. Everything else in the change is right, which is exactly what makes this dangerous. |
| 5 | **Sections and forms screens.** The API is done and tested; this is Angular only. A section list on the exam editor, a form builder that calls `GenerateFormAsync` and `PublishFormAsync`, a form picker on the assignment screen. | 4–5 | The approved-paper story — our single best answer to a sceptical coordinator — has no interface. |
| 6 | **Import.** A paste box for the Google Forms shape: numbered prompts, option lines, a ✅ key marker. Land everything in Draft; require the author to confirm each question; report every question whose text refers to an image that is not there. | 4 | The two-hundred-question barrier is untouched and the trial dies in week two. This is also the highest-leverage item in the list per day spent. |
| 7 | **Cohort management, a candidate add/edit form, and link recovery.** Small, and each one is a place the pilot currently stops dead. Store the link token encrypted rather than only hashed, or accept that a lost link means a new one. | 3 | The coordinator cannot fix a typo in a student's name or resend a link. |
| 8 | **Branding.** Name, logo, one colour, support address, on the shell, the exam page and the invitation email. | 2 | The invitation reads as phishing; the demo carries our name in front of their students. |
| 9 | **Fix the seven dead nav links** — hide what does not exist rather than route to it. | 0.5 | Every demo contains four clicks that go nowhere. |
| 10 | **Tests for `ExamTakingAppService` and `AssignmentAppService`, and one Playwright run against a real backend.** Turn off `AddAlwaysAllowAuthorization` for at least one permission test. | 3–4 | A defect here costs a real person their marks in front of the first customer. Right now the only end-to-end evidence we have is that our mocks agree with our code. |

**Total: roughly 26–30 developer-days, call it six weeks with a buffer.**

Steps 1, 3 and 6 are the ones that decide whether there is a pilot at all. Steps
2, 4 and 5 are what make it a product rather than a demo. Step 10 is what makes
it survivable.

**The risk that is not on the list.** Every item above is engineering, and
engineering is the part we are good at. The real risk is that after six weeks
we still will not have a named academy that has agreed to run a real intake on
this. That conversation costs nothing and should be started this week, in
parallel — and if that conversation cannot be started, the answer is more
informative than any of the days above.

---

## 6. Pricing, with real numbers

### What an attempt costs us to serve

Modelled on a 30-question single-choice exam plus five short written answers,
which is the shape of the first customer's real paper.

**Database.** One `Attempt` row, 30 `AttemptQuestion` rows (each carrying the
frozen score and the shuffled option order), up to 35 `Answer` rows (`Response` is
`nvarchar(max)`, unbounded, so an essay is whatever it is), and a handful of
`IntegritySignal` rows. Call it 66–70 rows. At a generous 350 bytes a row
including index pages, and 2 KB for each of five written answers:

> **≈ 35 KB per attempt.** *(Estimate — measured from the EF configuration and row
> shapes, not from a populated database.)*

1,000 attempts is 35 MB. 100,000 attempts is 3.5 GB. **Storage is not a cost
driver and never will be.** Whatever we charge, it is not for the rows.

**Email.** One invitation per attempt, plus perhaps one reminder. At transactional
email rates of roughly $0.10–0.12 per thousand messages, that is **$0.0002 per
attempt.** Also not a cost driver.

**Media.** This is the only real variable cost, and it is egress, not storage. An
audio clip is stored once and downloaded once per candidate. A listening paper
with six clips of four minutes at 96 kbps is about 18 MB per candidate. At cloud
egress rates of roughly $0.08–0.09/GB that is:

> **≈ $0.0015 per attempt for a text exam, ≈ $0.15 per attempt for a listening
> exam with six clips, and materially more if anyone uses video.**

A hundred-fold difference between the cheapest and the most expensive attempt is
the one genuine reason to meter something, and it argues for a media allowance
rather than an attempt price that has to cover the worst case.

**Compute and the fixed floor.** A small production deployment — one app service,
one managed SQL database, blob storage, an email sender — is on the order of
**$150–250 per month** before anyone sits anything. *(Estimate; tier-dependent.)*

That is the number that actually sets the price, because the marginal cost is
essentially zero:

| Monthly attempts | Fully-loaded cost per attempt |
|---|---|
| 200 | ~$1.00 |
| 1,000 | ~$0.20 |
| 5,000 | ~$0.04 |
| 20,000 | ~$0.01 |

**And the cost that dwarfs all of the above is human.** Onboarding a training
centre — typing or importing their first two levels, sitting with the coordinator,
answering the first month's questions — is realistically 3–5 days of somebody's
time. At any sane loaded rate that is **more than the first two years of
infrastructure for that tenant.** The first review proposed done-for-you
onboarding as a services line. It is not a services line. It *is* the cost
structure.

### What that means for the price

The floor is not compute. **The floor is the annual contract value at which one
customer is worth the onboarding and the support.** If onboarding is four days and
support is two hours a month, a tenant below roughly **$2,000–2,500 a year is
losing money for at least eighteen months.** *(Estimate, and the most important
number in this section — it should be checked against a real timesheet on the
first pilot.)*

So the shape is:

- **An annual tenant subscription with a named attempt allowance**, priced so the
  smallest sensible academy lands at or above that floor. Not per seat: candidates
  have no accounts by design, the people who generate cost are not users, and the
  people who are users number four.
- **A separate media allowance in gigabytes**, with overage. This is the only
  place metering reflects real cost, and it is the honest way to price a listening
  exam differently from a grammar exam without arguing about it.
- **Practice-mode attempts free or at a fraction.** `ExamMode.Practice` is already
  a first-class distinction. A centre that rations practice destroys the habit
  that makes the product stick.
- **Export always included, never an upsell.** "Your questions, your bank, your
  statistics, exported on request" is the whole tenant-owned-bank position.
  Charging for the exit contradicts it.
- **On-premise is a different price book entirely** — annual licence banded by
  institution size, plus installation, no metering. A ministry cannot accept
  usage-derived invoices for software on its own hardware. But see §7: we have no
  installer, so this is not currently sellable at any price.
- **Invoice in local currency.** This reads as an accounting detail and it is a
  purchase blocker in the region.

### Where we sit against the comparators

The two prior documents record the comparator landscape and I have not re-priced
it here; the shape is what matters and it has not moved.

- **Free** is occupied, twice: Google Forms at the bottom, and TAO Community
  Edition — open source, self-hostable, QTI-certified — at the top. We must not
  price near free, and we cannot beat free on price.
- **Enterprise** — Mettl, Questionmark, Surpass, ExamSoft — is quote-based with
  consulting attached, and a large share of the invoice is the consulting. We
  cannot price there: no accreditation, no reference logos, no standard-setting
  workflow, and as of §2.6 no computed discrimination.
- **Regional freemium** — Evalufy, Elevatus — is Arabic-capable, hiring-shaped,
  and aimed at KSA.

The defensible slot is **regional mid-market**: clearly above freemium, materially
below the enterprise platforms, sold on a bank the tenant owns outright and a
product their staff and students can read.

**The floor below which this is not a business** is therefore not a price point,
it is a customer count: at ~$2,500 a year and four days of onboarding each, this
does not become a business until it can acquire and onboard **twenty or so tenants
without twenty times the effort** — which means the importer (§5 step 6) and a
self-service catalogue (§5 step 2) are not features, they are the unit economics.

---

## 7. What we have built that nobody asked for

Honest list. One verdict each.

| Thing | Verdict | Why |
|---|---|---|
| **`CandidateGroupForm`** — a class's ordered list of the papers it will sit, shipped in `404b99d`: entity, table, two unique indexes, `SetGroupFormsAsync`, DTOs, controller action, three error codes in two languages, 265 lines of tests | **DELETE — already in progress** | See §8. Its own design document, written days later, specified its removal, and the working tree is executing that now. |
| **`hotspot`** — a 288-line region editor for authors, and a plain textarea for the person answering | **DELETE the type** | The most engineering per unit of customer value in the repository. No buyer in any of our three scenarios has asked to click on an image. Bring it back the day an anatomy or engineering customer asks. |
| **`code`** — `CodeOutputGrader` compares the candidate's typed text against `ExpectedOutput` | **DELETE, or rename it honestly** | Read the grader: nothing is executed, and the candidate's answer control is `TextAnswerComponent`, a plain textarea. The question is therefore *"type what you think this code prints"* — which is a legitimate exam question and is emphatically not what a buyer hears when we say "code questions". We have correctly declined to compete with HackerRank; shipping a type whose name promises a sandbox is the expensive way to be reminded of that in a meeting. |
| **`audio-response` and `file-upload`** | **PARK** | Both are unanswerable — no recorder, no file picker. `Answer.AnswerBlobName` and `ReviewAnswer.answerFileUrl` exist for answers that cannot be given. `file-upload` is worth finishing eventually (a vocational portfolio submission is real); `audio-response` needs a recorder and a speaking rubric and belongs to the language centre. |
| **`scale`** — a Likert scale, scored | **PARK** | Nobody in an assessment context has asked for it. It is a survey question in an exam product. |
| **Keystroke and backspace counters** on every answer | **KEEP** | They cost nothing, they are already stored, and they are the raw material for the collusion statistics that are our answer to "how do you stop cheating without a webcam". Do not build a screen for them yet. |
| **The module boundary and contract boundary architecture tests** | **KEEP** | Cheap, and the codebase reached this size without a cycle, which is most of why the parts are as good as they are. |
| **Astrolabe — the brand, the token layer, the owned application shell** | **KEEP, but stop** | It is genuinely good and it is the reason the product does not look like a template. It is also finished. Further design investment before there is a paying customer is appetite. |
| **The RTL-at-phone-viewport Playwright suite** | **KEEP** | It found three real layout defects and it protects the one differentiator no competitor can retrofit cheaply. |
| **13 question types** | **Reduce to 9** | Dropping `hotspot`, `code`, `audio-response` and `scale` removes four graders, four editors, four validator branches and four rows in every matrix test, and loses no customer we are pursuing. |
| **`docs/` at ~5,000 lines of strategy for a product with no customer** | **Note it** | Including this document. The analysis has been worth it — it found the media route and the results gap — but the ratio of strategy to shipped screens is now a signal in itself. |

---

## 8. What I would kill

**`CandidateGroupForm` and the entire "a class carries the papers it will sit"
feature, shipped last week in `404b99d`.**

**Update, and it is a good one: this is already happening.** The working tree as I
write shows `CandidateGroupForm.cs` deleted, `ClassCohortTests.cs` deleted, and
126 lines removed from `CandidateAppService`. I am leaving the argument below
standing rather than rewriting it in the past tense, because the reasoning is the
part worth keeping — and because the same reasoning applies to the next feature
built ahead of its mechanism.

The argument is not that it was badly built — it was careful, validated and tested.
The argument is that it was a control panel wired to a machine that was not running.

- Nothing consumes a form at delivery time (§2.3). A coordinator sets "this class
  sits Form 1, then Form 2 on the retake", and every student receives a random
  draw, silently, with no error. **A configuration surface for a mechanism that
  does not run is worse than no surface**, because the software then breaks a
  promise the customer made in writing.
- Nothing records which form was sat, so the retake guarantee it exists to deliver
  cannot be enforced even in principle.
- `Sequence` with unique indexes on `(GroupId, Sequence)` and `(GroupId, FormId)`
  forbids the morning/afternoon split — which is the *other* thing named forms
  were for.
- The same guarantee is delivered by two nullable columns and one branch, on
  `Assignment` and `Attempt`, which is where the sitting actually lives.

It is roughly half a day to remove: the entity, the `DbSet`, the EF block,
`SetGroupFormsAsync` and its interface member, three DTOs, the controller action,
three error codes and their six localisations, `ClassCohortTests` in part, and a
regenerated migration. Two of its three validations — a form must belong to the
exam, a form must be Published — should move to `AssignmentAppService.CreateAsync`
with their error messages intact, because that reasoning is exactly right and it
is what `Assignment.ExamFormId` will need.

Keep the rest of that commit: `CandidateGroup.LevelId`, `StartsOn`, `EndsOn` and
`IsActive` are right, and an intake calendar is exactly what the first customer
has.

**Since the removal is already in flight, the kill that still needs a decision is
`hotspot`.** A 288-line authoring editor for drawing regions on an image, whose
answer control for the person actually sitting the exam is a plain textarea, for a
question type nobody in any of the three buyer scenarios has requested — and whose
image would 404 anyway (§2.2). Delete the type, the editor, the grader, the
validator branch and the payload shape. Bring it back on the day an anatomy,
engineering or clinical customer asks for it by name.

The general rule both kills share, and the one worth keeping after the specifics
go stale: **build the mechanism before the control surface, and build the answer
before the authoring.** Every expensive thing in this document is a violation of
one or the other.

---

## 9. The competitive position, retested

The colleague's finding stands and has got worse, not better: TAO Community
Edition is free, open source, self-hostable and QTI-certified. It removed
installability as a moat.

**What §2 adds is that we do not currently have installability at all.** There is
no Dockerfile, no installer, no deployment manifest, no infrastructure-as-code and
no CI in the repository. TAO ships a product you can install. We ship a solution
file. Until that changes, "it can be deployed where the data must stay" should not
be said in a meeting — it is not a weakened claim, it is an unbuilt one.

The other three claimed differentiators, retested against the code:

- **Arabic-first.** *True, and the strongest thing we have.* Logical properties
  throughout, an Arabic-first type stack, a Playwright suite that runs the whole
  app in Arabic at a phone viewport, and localisation coverage enforced by a test
  that fails the build on a missing string. Contested by Evalufy and Elevatus at
  the marketing level; nobody has shown me a competitor whose *tests* run in
  Arabic.
- **The tenant owns the bank.** *True in the schema, false in the product.* §2.4:
  no customer can create a bank question, because there is no catalogue.
- **The tenant renames the vocabulary.** *False today.* `CategorySet` is a table
  with no service and no screen.
- **Psychometrics the tenant can see.** *Half true.* Difficulty is computed;
  discrimination is not; there is no item-health screen; and the one thing the
  pair is for — telling "hard" apart from "the key is wrong" — needs the half we
  do not have.

### The one sentence

> **It is the only assessment platform where an Arabic-speaking training centre
> can build an exam from questions it owns, approve the exact paper before it goes
> out, and have it marked — in a product its staff and its students can read.**

**Is it true? Not yet, and it is precisely four items away from being true.** The
"questions it owns" half needs the catalogue (§5 step 2). "Approve the exact
paper" needs the `DeliveryMode` branch and the forms screen (steps 4 and 5). "Have
it marked" needs the results screen (step 3) — the marking itself is built.

That is the right sentence to aim at, because every clause of it is something the
competitors structurally cannot say: TestGorilla's value is *their* library, TAO
asks a training centre to run a QTI-certified platform with no support, Google
Forms cannot bind a paper to a person, and the enterprise platforms will not
answer the phone for a forty-student intake.

It is also a sentence we must not say until it is true. The version that is true
today is much smaller, and saying the smaller one and then delivering the larger
one in six weeks is a far better position than the reverse.

---

## 10. Where this contradicts the first review

Recorded so the change of view is visible rather than quietly absorbed.

1. **"There is presently no end-to-end demonstration of this product."** No longer
   true for authoring and sitting; still true for the loop, because §2.1 breaks it
   at the last step. The first review found a missing screen; the missing thing now
   is a missing *service*.
2. **"Sell the placement test before the level exam."** Reversed. Placement needs a
   profile and four separate defects prevent one.
3. **"`QuestionGroup` has no delivery rendering."** Wrong — the taker renders a
   stimulus. The gap is the authoring screen, which is the cheaper half.
4. **"Named forms are the best cost-to-revenue ratio in the backlog."** Still true,
   and the cost was underestimated by one step: the authoring API shipped first and
   moved the customer nowhere, because delivery was the part that mattered.
   Delivery is landing now (§2.3). The screen — the part a coordinator touches —
   is still not started, so the feature will remain worth zero to a customer for
   one more increment.
5. **"Build the taker section-aware from the first line."** The advice was right
   and was not taken. The retrofit is now owed, and it now also owes a section id
   on `ExamFormQuestion`, which did not exist when the advice was given.
6. **"Item statistics are the most credible thing we can show an assessment
   professional."** Half of them are now computed, and the half that is missing is
   the half that carries the claim.
