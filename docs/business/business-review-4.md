# Business review, fourth pass — every screen, every control, and whether it leads anywhere

`business-review-3.md` was written at `75b534d` and ranked fourteen findings by
what they cost a real user. Thirty commits have landed since. Eleven of the
fourteen have been worked on, and the commit messages are unusually honest about
which half of each was finished — one of them records a fix that was attempted
and reverted, and why.

So this pass is not a scorecard on that document. It is the review the product
owner asked for, re-derived at `d2e1a3a`: **every component, use case, user
story, feature, link, screen and button, and whether it behaves as expected.**

The vocabulary is the one the earlier reviews established and it is worth
restating, because the whole document turns on it.

| Status | Means |
|---|---|
| **DONE** | A person completes this today, end to end, in the running product |
| **PARTIAL** | Part of it works and the journey stops somewhere, **or** a finished control exists that no mechanism reads |
| **ABSENT** | The journey cannot be started |

One warning about that vocabulary, earned this pass and not the last: **DONE
measures reachability, not correctness.** Three of the ten DONE use cases below
now carry defects that cost a user more than several of the PARTIAL ones. The
worst finding on this page sits inside a journey I have marked DONE. Read §5
before reading §3.

---

## 1. One thing that changes how every live measurement here reads

I was asked not to stop or restart the API host, and I did not. It follows that
this section has to come first.

**The running API is fourteen commits behind `HEAD`.** Process 37204 started at
2026-08-30 13:08:59 from
`src/InternshipManagementSystem.HttpApi.Host/bin/Debug/net10.0/`, whose
assemblies were built at 13:07:52. The last commit before that build is
`6efa48f` (12:52). Everything from `6ac6ad5` (13:14) onward — thirteen commits,
including the three new question types, the copy/paste block, the three new
integrity signals, the roles screen, the organisations screen, candidate
create/edit and the re-mark queue — is **not** in the process answering on
`https://localhost:44373`.

Two things obscure this and both misled me before I checked the process:

- ABP replaces embedded resources with physical files in DEBUG builds, so
  `ar.json` and `en.json` are read from disk at request time. The running server
  serves **current** localisation — `Nav:Roles`, `Candidate:Edit`,
  `Section:NotEnforced:Title` are all there — while its compiled behaviour is
  three hours old.
- The SPA is `ng serve` in watch mode, so `http://localhost:4200` serves the
  **working tree**, which is not `HEAD` either (see below).

Two measurements caught it. `GET /api/assessment/review/queue?Finished=true`
returns 200 with an empty list on a tenant holding six qualifying attempts,
because `ReviewQueueRequestDto.Finished` does not exist in the running binary —
`MaxResultCount=2000` correctly returns 400, so the model binder is working and
the property is simply absent. And `POST /api/assessment/media/answer`, the
candidate upload door added in `59d436e`, is not in the running server's
`swagger.json`.

**Consequently:** every live number in §9 is evidence about `6efa48f`. Every
claim about the last fourteen commits in §4, §5 and §6 is from reading code at
`HEAD`, and is labelled as such. Where the two disagree, the code is the
authority and the divergence is stated.

**The working tree is also not `HEAD`.** Four files are modified and
uncommitted: `angular/src/app/core/direction.service.ts`,
`angular/src/app/layout/shell.component.ts`, `angular/e2e/shell.spec.ts` and
`angular/e2e/exam-form.spec.ts`. They add
`DirectionService.useOrganisationDefault`, which is the reader that makes the
default-language setting live. That reader **does not exist at committed
`HEAD`**, so `Assessment.DefaultLanguage` is judged inert below. The fourth file
changed while this review was being written, by somebody else, and two further
commits landed before it was finished — one of them this very fix. Both are
recorded in §13 rather than folded silently into the text above them. This is the
second review in a row to end that way.

---

## 2. The headline

**Sixty-nine of a hundred and twenty-seven stories are complete — exactly where
the count stood at `75b534d` — and ten of seventeen use cases are walkable end
to end, against eleven.** Thirty commits, and the DONE column has not moved by
one. Underneath it, nine stories went up and nine went down, and the composition
of PARTIAL turned over almost entirely.

Three sentences carry the rest.

1. **The defect shape has changed again, and not for the better.** The third
   pass found "the screen says something untrue" replacing "the screen does
   nothing". This pass finds a third shape: **the newest work is correct in
   every layer and does not connect at the last joint.** Sections are delivered,
   ordered, reported per section on two screens, covered by twenty-nine new
   tests — and no control anywhere in this product can put a question into a
   section. A candidate can now record a spoken answer; the marker cannot open
   it. A marker can now reach a marked sitting; the screen it opens is blank.
   Each of these is a finished mechanism reachable by nobody, which is the
   PARTIAL of the second review wearing the clothes of the third.

2. **One defect is worse than anything the third pass found, and it was
   introduced by the work that fixed the third pass's third finding.** Blocking
   paste (`336976f`) added a browser-side paste report without removing the
   save-time one. `wasPasted` is reset only when the candidate leaves the
   question, and autosave fires every 800 ms. So a single blocked `Ctrl+V` on a
   long answer is recorded once by the browser and then **again on every
   subsequent save of that question**, each with a magnitude equal to the length
   of what the candidate typed themselves. The marker reads *"14 paste event(s),
   totalling 3,900 characters."* None of it happened.

3. **Nine of the fourteen dead controls are now read or removed, and three of
   the survivors are honestly labelled.** `08cacb0` chose to say on screen that
   the section clock, the minimum percentage and the qualifying flag are stored
   and enforced by nothing — a banner, a chip on every row, a warning under
   every field. That is the right disposition and it should be said plainly: it
   is the first time in this repository that a half-built rule has been declared
   to the person relying on it rather than left for them to discover after a
   cohort has sat the paper.

---

## 3. The scoreboard

### Use cases — 17

| # | Use case | `use-cases.md` at `75b534d` | This review at `d2e1a3a` |
|---|---|---|---|
| 1 | Set up the catalogue | BUILT | **DONE** |
| 2 | Write a question the centre owns | BUILT | **DONE** |
| 3 | Attach a chart, a recording or a clip | BUILT | **PARTIAL** — the hotspot image is the eighth surface to carry the origin-relative URL bug |
| 4 | Build an exam and publish it | BUILT | **DONE** |
| 5 | Lay an exam out in sections and passages | PARTIAL | **PARTIAL** — the stopping step moved *earlier*: nothing can put a question in a section |
| 6 | Approve the exact paper before it goes out | BUILT | **DONE** |
| 7 | Bring in a class and put it at a level | PARTIAL | **DONE** — and it destroys two fields on every correction |
| 8 | Send an exam to a class | BUILT | **DONE** |
| 9 | A candidate sits the exam | BUILT | **PARTIAL** — hotspot unanswerable; resume loses an uploaded answer |
| 10 | Mark what a person has to mark | BUILT, two sharp gaps | **PARTIAL** — one gap closed; the other now opens a blank screen |
| 11 | Read the results and get them out | BUILT | **DONE** |
| 12 | Find the questions that have stopped measuring | PARTIAL | **DONE** — the row opens the question |
| 13 | Put the centre's own name on it | PARTIAL | **PARTIAL** — seven of eight settings read; the vocabulary is still rendered nowhere |
| 14 | Give staff accounts and decide what they may do | BUILT | **DONE** — and two whole screens wider than the doc knows |
| 15 | Bring an existing exam in (Word, Forms) | NOT BUILT | **ABSENT** |
| 16 | Place a student by their profile | NOT BUILT | **ABSENT** — the code is there and no user can reach it |
| 17 | Import a question bank from a spreadsheet | BUILT | **DONE** |

**10 DONE · 5 PARTIAL · 2 ABSENT.**

Up: 7 (`44fccba`), 12 (`20e4af8`). Down: 3 (`9820158`), 9, 10 (`1c2a5fd`).
Unchanged in status, transformed underneath: 5, 13, 16.

### User stories — 127

| Epic | Stories | DONE | PARTIAL | ABSENT |
|---|---|---|---|---|
| 1 · The catalogue and the tenant's vocabulary | 6 | 2 | 3 | 1 |
| 2 · The question bank | 12 | 8 | 2 | 2 |
| 3 · Getting existing exams in | 6 | 2 | 1 | 3 |
| 4 · Exams, sections and publishing | 12 | 5 | 2 | 5 |
| 5 · Blueprints and per-candidate assembly | 7 | 3 | 1 | 3 |
| 6 · Named forms | 8 | 5 | 1 | 2 |
| 7 · People and cohorts | 7 | 4 | 3 | 0 |
| 8 · Assignment and links | 9 | 7 | 2 | 0 |
| 9 · Sitting the exam | 16 | 9 | 4 | 3 |
| 10 · Grading and the reviewer's queue | 10 | 6 | 2 | 2 |
| 11 · Results, item health and export | 12 | 6 | 2 | 4 |
| 12 · The tenant's own face | 5 | 1 | 3 | 1 |
| 13 · Access and administration | 6 | 5 | 1 | 0 |
| 14 · How the product behaves everywhere | 11 | 6 | 5 | 0 |
| **Total** | **127** | **69** | **32** | **26** |

Against `user-stories.md`'s 69 / 31 / 27: **the DONE column is identical and
nothing about it is the same.** Nine stories moved up, nine moved down.

**Up (9):** `PPL-01` and `PPL-05` — a person can be created and corrected by
hand (`44fccba`). `GRD-05` — the model answer is on the marking screen
(`1a6ce10`). `RES-07` — the item-analysis row opens the question (`20e4af8`).
`PLT-01` — thirteen of thirteen types have a purpose-built answer input, so the
authoring constraint is kept on both halves (`87331aa`, `9820158`). `BNK-12` —
item statistics are forgotten when a key changes (`20e4af8`). `TAK-08` and
`GRD-07` — from ABSENT to PARTIAL, both because the mechanism shipped and stops
one joint short. `IMP-04` and `FRM-06` — from ABSENT to PARTIAL, both work that
existed at the pin and was never recorded.

**Down (9):** `EXM-07`, `EXM-08`, `EXM-09` — PARTIAL to ABSENT. The three
section fields are unchanged, but `08cacb0` retracted the written promise on
screen, so the "dead control that lies" ground for PARTIAL is gone and what
remains is an unbuilt feature honestly labelled. `EXM-12` — the scheduled window
is enforced, validated and now time-zone-correct, and no screen can set it.
`PLT-09` — *serve a stored file to the browser that renders it*, BUILT to
PARTIAL: the marker's uploaded-answer link is precisely the surface that story
exists for, and it 404s silently (finding 3). `ASG-03` — the invitation's sender.
`EXM-02`, `PPL-06`, `PPL-07` — three long-standing gaps the document records too
generously.

**Two stories keep their status and change their meaning entirely.** `RES-06`'s
sole residual is now the deliberately unpersisted discrimination index rather
than a running mean nobody resets. `GRD-06` — *the marking screen shows what
actually happened* — stays PARTIAL for the opposite reason it did before: the
payload binds correctly now, and the count the marker reads is inflated by
finding 1.

**Three capabilities are new and none of them is in `user-stories.md`:** the
roles screen, the organisations screen, and the candidate answer-upload door.
All three are DONE and are counted above.

Two judgement calls worth exposing, because both were argued the other way by a
reasonable reading. `TAK-09` (a candidate sees which part of the paper they are
in) and `RES-04` (a score per section) both have complete, tested, delivered
code — and both are recorded ABSENT, not PARTIAL, because no user can put a
question into a section (finding 2), so neither journey can be started at all.

---

## 4. How the fourteen findings of the third pass stand

| # | Finding | At `d2e1a3a` |
|---|---|---|
| 1 | The candidates screen states a falsehood about a named person | **FIXED** |
| 2 | Six of nine settings are inert, two of them consent switches | **FIXED but one** |
| 3 | Every integrity observation is recorded as a paste | **FIXED, and a worse one put in its place** |
| 4 | Sections: authored, saved, invisible downstream | **PARTLY FIXED, and unreachable** |
| 5 | The marking screen hides the model answer; a mark cannot be reopened | **HALF FIXED; the other half is a blank screen** |
| 6 | A person cannot be added or corrected by hand | **FIXED, with data loss** |
| 7 | Swagger is down | **FIXED** |
| 8 | A second, undocumented API surface | **PARTLY FIXED — deliberately, and the reasoning is sound** |
| 9 | Item health cannot say the thing it exists to say | **3 of 4 FIXED; the fourth refused, in writing** |
| 10 | Three question types cannot be answered | **FIXED — and one of the three cannot be seen** |
| 11 | The invitation still arrives from a stranger | **UNTOUCHED** |
| 12 | Two of the five roles land on an empty page | **FIXED** |
| 13 | Dead controls, catalogued (14) | **9 read or removed; 5 remain, 3 of them labelled** |
| 14 | Smaller, verified, and worth naming (12 items) | **ALL TWELVE UNTOUCHED** |

Four of these deserve more than a cell.

**Finding 1 is properly fixed, and I checked it against live data rather than
code.** `Candidate.Status` was not written — it was **deleted**. Migration
`20260830072406_Candidate_Status_Is_Derived` drops the column; the enum was
redefined from Pending/Passed/Failed to Pending/Invited/InProgress/Completed/
Withdrawn; `CandidateAppService.StatusOf` derives it from a live link, an
unsubmitted attempt and a submitted attempt, in that precedence. In
`trading-academy` today, the six people who sat and submitted read status 3
(Completed) and the two who did not read status 1 (Invited). The column that
said *«لم يُدعَ»* about people who had finished now says what happened. The
broken filter control was removed from the screen rather than left to return
everything or nothing.

**Finding 2's remaining one is `DefaultLanguage`, and only because its reader is
uncommitted at the pin.** It was committed as `c0d9fa2` twenty-five minutes after
this review's pin; see the closing note. Seven of the eight surviving settings
are read outside the
settings service: the organisation name and logo (the invitation and the
candidate's landing page), the brand colour (`brand.service.ts` paints
`--accent` and derives hover and active with `color-mix`), the time zone
(`TenantNowAsync`, so a scheduled window is compared as wall-clock in the
organisation's own zone), the default pass mark, "show the result to the
candidate" (`GetResultAsync` sets `ScoreWithheld` and gives the candidate a
sentence of their own rather than borrowing the marking one), and "record
integrity signals" (`CollectsSignalsAsync`, ANDed with the exam's own switch, so
both dead switches at both levels are now live). The self-registration key was
deleted — and then `b9fef28` found that ABP's own `/Account/Register` page was
live and answering 200, so **anyone who knew the URL could create an account
inside a customer's tenant**. That is now defaulted off at ABP's own setting, in
the host project, which is the only project that loads the account web module.
The earlier placement would have been a silent no-op.

The default pass mark is honoured **client-side only**:
`exam-form.component.ts` prefills a new exam from the tenant setting, while
`CreateUpdateExamDto.PassingPercentage` and `Exam.PassingPercentage` still carry
`= 60m` and nothing on the server consults the setting. An exam created by the
importer, the seeder, or any API client still gets 60.

**Finding 8 was not fixed and should not have been.** `1f9c85e` fixed Swagger by
disabling the conventional controller for `ResultAppService` alone — the one
genuine route collision, `GetListAsync` and `GetAsync` both landing on
`GET api/app/result` because the parameter is named `attemptId` rather than `id`
— and by removing a duplicate registration that gave every service two sets of
routes. It records, in a comment in the source, that removing the conventional
registration outright was tried and reverted: it turns every authorisation
refusal from a 403 into a 302 to a login page, which thirteen role tests caught,
and which looks like success to a client. That is a better answer than the one
review-3 recommended, and the reasoning belongs in the record. The
`/api/app/*` surface therefore remains: 70 paths beside 66 `/api/assessment/*`
ones in a `swagger.json` that now returns 200. It still honours permissions —
verified live across five accounts — and the anonymous `ExamTakingAppService`
twin still accepts the session token as an ordinary parameter rather than
through the `X-Exam-Session` header.

**Finding 9's fourth part was refused, in writing, and the refusal is correct.**
`20e4af8` records that `DiscriminationIndex` was left unwritten deliberately:
it is a statistic of a cohort, not a property of a question, it needs the top
and bottom quartiles together, it is therefore computed at read time on the
item-analysis screen — which now renders it, and refuses to report it when the
two groups are not far enough apart to mean anything — and computing it on every
submission would load the slowest step in the product. Leaving a column empty
and saying so is more honest than a number updated when convenient. The other
three parts are built: the difficulty index and the answer count are cleared
when the *rendered* answer key changes and not when the wording is corrected,
and the item-analysis row now links to the question.

---

## 5. Findings, ranked by what they cost a real user

### 1 — One blocked paste is recorded again on every autosave, as an accusation with a number on it

`336976f` blocked pasting in the exam and reported it from the browser, which is
right. It did not remove the older report on the server:

```csharp
// ExamTakingAppService.SaveAnswerAsync
if (input.WasPasted && (input.Response?.Length ?? 0) > 120)
{
    await RecordSignalAsync(attempt, IntegritySignalType.Paste, input.QuestionId, input.Response!.Length);
}
```

The browser's handler sets a flag and reports:

```ts
const onPaste = (event: ClipboardEvent) => {
  event.preventDefault();
  this.wasPasted = true;
  this.take.reportSignal(IntegritySignalType.Paste, this.question()?.id);
  ...
};
```

`this.wasPasted` is cleared only by `resetObservations()`, which runs when the
candidate **moves to another question**. Autosave is debounced at 800 ms and
sends `wasPasted` every time. So one blocked `Ctrl+V` on a question whose answer
grows past 120 characters produces one signal from the browser and then another
on every save until the candidate leaves that question — and because the paste
was *blocked*, the `Magnitude` recorded is the length of the candidate's own
typing.

`Attempt.IntegrityFlagCount` is incremented once per signal. The marker's screen
then renders, from `ReviewAppService`:

> `{count} paste event(s), totalling {sum} characters.`

For a 400-word essay written by hand after one instinctive paste of the
candidate's own draft, that sentence can read *"14 paste event(s), totalling
3,900 characters."* Every word of it is false, it is attached to a named person,
and it is the single most weighted fact on the screen where a human decides
whether they passed — because `roles.md` argues, at length and correctly, that
the marker should hold `Review.ViewIntegritySignals` precisely so they can see
it.

The same blocked paste also **suppresses** the two honest signals:
`NoteHowItWasWrittenAsync` returns early on `input.WasPasted`, by design, so
`ImplausibleSpeed` and `NoCorrections` are silenced for the rest of that
question while the false one multiplies.

This is the exact harm `4660f5c` was written to remove, at a larger magnitude,
introduced eight commits later.

*Where to act:* delete the save-time branch. The browser reports the event now,
and it reports the one thing the server cannot see — an attempt that was
refused.

### 2 — Nothing in this product can put a question into a section

`08cacb0` is the largest piece of work in the range: a migration adding
`AttemptQuestion.ExamSectionId`, a form builder that draws section by section in
display order, a taker that shows the candidate which part they are in and its
instructions at its first question, per-section scores on both result screens,
and twenty-nine new tests. It is careful work and it is unreachable.

`Question.ExamSectionId` exists on the entity, on `CreateUpdateQuestionDto`, and
is assigned by `QuestionAppService.Apply`. **The string `examSectionId` does not
appear anywhere in `angular/src`** — not in `assessment.models.ts`, not on the
question form, not in any service. Neither `CreateUpdateQuestionGroupDto` (the
passage) nor `CreateUpdateBlueprintRuleDto` carries the field at all, on either
side of the wire. So there is no control, no client model field and no API shape
by which a question, a passage or a blueprint rule can be filed under a section.

Every section pool is therefore empty, and `DrawBySection` skips empty sections:

```csharp
var pool = bank.Where(q => q.IsActive && q.ExamSectionId == section.Id).ToList();
if (pool.Count == 0) { continue; }
```

Every paper falls through to the unfiled branch. The candidate never sees a
heading. `bySection` on the result is always empty. The competency profile that
`use-cases.md` tells a salesperson to sell instead of a section report is
unchanged, and the section report is exactly as far away as it was.

And the one control that touches the field destroys it: because the Angular DTO
omits `examSectionId`, every question saved through the question form sends no
section, and `Apply` assigns `question.ExamSectionId = input.ExamSectionId`
unconditionally. **Any section membership set by any other route is silently
cleared by the next edit of that question.**

*Where to act:* a section select on the question form and the passage dialog,
and the field on both client DTOs. It is small, and without it the whole of
`08cacb0` is dark.

### 3 — A marker cannot open the answer they are asked to mark

`87331aa` and `59d436e` closed the last two unanswerable types. A candidate can
now upload a document or record a spoken answer; the blob is stored under their
attempt, the answer carries its name, and both types correctly force manual
review — so they land in the marker's queue by construction.

`review-attempt.component.html` renders the attachment as a paperclip link.
`fileUrl()` resolves it through `MediaService.objectUrl`, which fetches the blob
with the signed-in user's token. `AssessmentMediaAppService.GetAsync` gates the
staff branch on `Assessment.Questions.Default`. The seeded `Marker` role holds
`Review.ViewQueue`, `Review.Grade` and `Review.ViewIntegritySignals` — and
nothing else. The request is refused, and `objectUrl`'s error branch is
deliberately silent, on the good grounds that a missing thumbnail should not
throw a banner over a form somebody is filling in.

The result: the marker sees a paperclip and a filename, clicks it, and **nothing
happens at all.** No error, no explanation. The only role that can both reach
the marking screen and open the file is the tenant administrator, which is not
the role the product asks to do the marking.

There is also **no `<audio>` element anywhere on the marking screen.** A spoken
answer is a filename. A marker grading fifty of them downloads fifty files, if
they are the administrator.

*Where to act:* not by granting the marker `Questions.Default` — that opens the
whole bank. An answer blob belongs to an attempt; the read should be authorised
by `Review.ViewQueue` plus ownership of that attempt, the way the candidate's
own read is authorised by their session grant. And an `<audio controls>` when
the stored type is audio.

### 4 — The candidate cannot see the picture they are told to point at

`9820158` built a hotspot answer input with a keyboard path, percentage
coordinates independent of display size, and a marker that reads without relying
on colour. It binds the image like this:

```html
<img class="hotspot__image" [src]="url" alt="" draggable="false" />
```

`url` is `question.display['imageUrl']`, which the server builds as
`/api/assessment/media/{blobName}?grant=…` — server-relative. Every other
candidate-facing media binding in the application passes through
`MediaService.absolute()`; this component does not inject `MediaService` at all.
With the SPA on one origin and the API on another — the development
configuration, and the one every demo runs on — the browser asks the SPA for the
image and gets a 404.

So a candidate reaches a question that says *"point to the support level"*, sees
a broken image, and has a clock running. This is the eighth surface to carry the
defect `use-cases.md` §3 documents under "Seven symptoms, one cause", and it
survived for the reason that section itself diagnoses: the browser test feeds
the hotspot a `data:` URI, so it asserts that its own mock is reachable.

### 5 — Correcting a person's name erases their phone number and their category

`44fccba` closed review-3's finding 6: there is now a create dialog and an edit
dialog, `canEdit` is used, and both call the endpoints that had been sitting
there with no caller.

The dialog carries three fields — name, email, reference — and `saveDraft()`
sends exactly those three. The server applies unconditionally:

```csharp
private static void Apply(Candidate candidate, CreateUpdateCandidateDto input)
{
    candidate.PhoneNumber = input.PhoneNumber;   // null from the edit dialog
    candidate.CategoryId  = input.CategoryId;    // null from the edit dialog
    candidate.Reference   = input.Reference;
}
```

The paste importer does populate the phone number. The category is what files a
person under a domain and decides which level papers are offered for them. So
the routine act the commit was written to enable — fixing a misspelt name —
silently destroys two fields, one of them structural, and the loss is visible
only afterwards, in the row.

*Where to act:* put phone and category on the dialog, or make `Apply` patch
rather than replace. The first is better: an edit dialog that shows three of
five fields is a trap whichever way the server behaves.

### 6 — The "already marked" tab opens a blank screen

`1c2a5fd` is a good commit with a good argument: marking is human judgement,
people revise judgements, and somebody who typed 7 meaning 17 had no route back
because the attempt left the queue the moment its last answer was scored. It
gave the queue two tabs — waiting, oldest first; marked, newest first — and
`GetQueueAsync` now branches on `input.Finished`.

`GetAnswersAsync` was not touched:

```csharp
var pending = await (await _answers.GetQueryableAsync())
    .Where(a => a.AttemptId == attemptId && a.NeedsManualReview)
    .ToListAsync();

if (pending.Count == 0) { return []; }
```

`GradeAnswerAsync` clears `NeedsManualReview`. So every row in the new tab
navigates to `/review/:attemptId` and receives an empty list. The screen renders
the observations block and nothing else — not even the "all marked" line, since
`isDone()` requires at least one answer. The correction the commit exists to
allow works only within the page session that made the mark, through a local
patch of the in-memory list; a reload loses it.

`RemarkTests.cs` passes because it captures the answer id before the first grade
and re-grades with it, never re-entering through `GetAnswersAsync` — which is
the path the screen takes.

Two smaller things compound it. The tab's predicate is "submitted and nothing
pending", so it lists **every auto-graded attempt no human ever touched**; on
`trading-academy` that is all six. And its tooltip reads *"To revisit a mark, or
put one right"*, which is a written promise on a control that cannot keep it.

### 7 — The marker writes feedback for the candidate that the candidate never sees

Two sentences on the marking screen:

> `Review:Attempt:Lede` — "Award marks and write what the person should know.
> **Your comment reaches them.**"
>
> `Review:Comment:Hint` — "This is shown to them with their result, so it is
> feedback rather than a note to yourself."

`Answer.ReviewComment` is written by `GradeAnswerAsync`. It is read back in
exactly two places: the marker's own screen, and `result-detail.component.html`
— the **staff** result screen. `AttemptResultDto` and `PracticeReviewItemDto`,
the two shapes a candidate ever receives, carry no comment field, and
`take-result.component.html` renders none.

So a marker who spends time writing a paragraph explaining why an essay lost
four marks is writing it for a coordinator, having been told twice on the same
screen that they are writing it for the student.

### 8 — The scheduled window is enforced, validated, time-zone-correct, and cannot be set

`d5cf42a` is careful work: a coordinator typing 09:00 means nine in the morning
where they are, so `IsOpenAt` now takes a wall-clock instant converted into the
organisation's zone, an unrecognised zone falls back to the server clock with a
log line rather than blocking an exam, and the author records that their first
test was worthless because Riyadh and the build machine share an offset.

`ExamAppService.UpdateAsync` writes `IsScheduled`, `ScheduledStartTime` and
`ScheduledEndTime` and refuses a start without an end or an end before a start.
`exam-form.component.ts` holds all three in its form model.
`exam-form.component.html` contains **no date input of any kind** — twelve
controls, none of them a window. `requirements.md` FR-5.6 records the optional
window as **مُنفَّذ / met**.

Meanwhile the setting whose hint says *"Every exam clock and scheduled window is
read in this zone. Getting it wrong opens exams at the wrong hour"* now protects
a feature nobody can turn on.

### 9 — The marker's observations are in English, in an Arabic-first product

`ReviewAppService.BuildIntegrityReport` composes the six sentences by string
interpolation in C#:

```csharp
IntegritySignalType.WindowBlur =>
    $"Left the exam window {count} time(s), for {group.Sum(s => s.Magnitude ?? 0)} seconds in total.",
```

There are 694 localisation keys in this product and zero missing from either
file. These six are not among them. The tenant default is Arabic, the seeded
markers' interface is Arabic, and the one screen in the product where a person
makes a judgement about another person speaks English — with `(s)` pluralisation
that is poor even in English.

### 10 — Smaller, verified, and still worth naming

- **The invitation still arrives from "Assessment Platform".**
  `AssignmentAppService` still calls `SendAsync(candidate.Email, subject, body,
  isBodyHtml: true)` with no sender; `appsettings.json` still holds
  `no-reply@localhost` and `127.0.0.1:25` host-wide. `AccountSettingOverrides.cs`
  is about self-registration, not mail. Finding 11 is untouched.
- **`GET /api/assessment/questions/import/template` still returns 500 instead of
  403** for the coordinator, the marker and the observer. Confirmed live this
  pass, in the same three cells as last pass. The controller still returns
  `IActionResult`, so the authorisation failure escapes ABP's exception filter.
- **A candidate who reconnects loses sight of their uploaded answer.**
  `SavedFileName` is projected by the server and declared in `take.models.ts`
  and read by no component, so `mountAnswer()` never restores it. Somebody who
  uploaded a scan and then lost their connection sees an empty picker under a
  running clock, and will upload again or assume it was lost.
- **The brand colour does not survive a refresh during the exam.**
  `brand.apply` is called from the staff shell and from `take-entry`. It is not
  called from `take-sitting` or `take-result`, so a candidate who reloads mid-exam,
  or lands directly on the sitting URL, finishes the paper in the platform's blue.
- **`angular/src/app/features/tenants/tenant-list.component.ts` contains a raw
  NUL byte** at offset 2742 — a literal `\0` written into the source rather than
  escaped, as the sentinel in the delete-confirmation comparison. It compiles and
  behaves correctly. Git treats the file as binary: it appeared in the diffstat as
  `Bin 0 -> 5725 bytes`, it cannot be diffed, and it cannot be blamed. The most
  destructive action in the product — deleting an organisation and every exam,
  question, person and result in it — has its only guard in a file no reviewer can
  read a change to.
- **All twelve items of finding 14 are untouched**, and none of their files has a
  commit in the range: the section update that discards `ExamId`, the wiped
  surname, the phone number that cannot be cleared, the missing 404 route, the
  attempt monitor with no way out (it contains zero `routerLink`, `href` or
  `navigate` in 154 lines of template and 206 of component), the nav/route/service
  permission mismatch on that monitor, the six group permissions nothing checks,
  the unused `TenantBranding` table, the unreferenced `placeholder.component.ts`,
  the Award button not gated on `Review.Grade`, and the promised file cleanup that
  has never existed. The inconsistent half of the guarding split has grown by one:
  `AssessmentMediaAppService` carries a bare `[Authorize]`.

---

## 6. Dead controls, re-derived at `HEAD`

Nine of the fourteen are read or gone. Five remain, and five more are new.

| Control | Screen | What reads it at `HEAD` |
|---|---|---|
| Section time limit | `/exams/:id/structure` | nothing — **labelled "not enforced"** |
| Section minimum percentage | `/exams/:id/structure` | nothing — **labelled** |
| Section qualifying flag | `/exams/:id/structure` | nothing — **labelled** |
| Exam · "one question at a time" | `/exams/:id` | nothing — now *sent* to the taker and read by no component |
| Catalogue · vocabulary editor | `/catalog` | its own dialog and nothing else |
| **Per-question time limit** *(new)* | `/questions/:id` | nothing — authored, saved, projected to the taker, read by no component |
| **`CandidateGroup.IsActive`** *(new)* | — | a column on no DTO, set by no control, read by no query |
| **`SavedFileName`** *(new)* | `/exam/:token/sitting` | sent by the server, declared client-side, never mounted |
| **`SettingManagement.TimeZone`** *(new)* | `/roles` | a grantable permission governing a screen this product does not ship |
| **Reviewer comment** *(new)* | `/review/:attemptId` | staff screens only, under two sentences saying it reaches the candidate |
| `Question.DiscriminationIndex` | `/questions` | nothing — **refused in writing**, and the refusal is right |
| `TenantBranding` | — | nothing, still |
| `POST questions/validate-payload` | — | a working route with no client |

Read or removed since the last pass: brand colour, default language *(uncommitted
at the pin; committed as `c0d9fa2` — see §13)*, time zone, default pass mark, show-result-to-candidate,
record-integrity-signals at both the tenant and the exam level, self-registration
*(deleted, and the real thing behind it closed)*, and the candidate status filter
*(the column is now true and the broken filter was taken off the screen)*.

**Mechanisms with no control** — the mirror shape, and it has grown:

| Mechanism | Enforced in | Control |
|---|---|---|
| `Question.ExamSectionId` / `QuestionGroup.ExamSectionId` | the form builder, the taker, both result screens | **none, and every question edit clears it** |
| `ExamBlueprintRule.ExamSectionId` | `DrawBySection` | none — absent from the DTO |
| `Exam.IsScheduled` / `ScheduledStartTime` / `ScheduledEndTime` | `IsOpenAt`, in the tenant's zone | none |
| `Exam.AllowBackNavigation` | `take-sitting.component.ts` | none |
| `Candidate.Status` filter | `CandidateAppService` | none, since the broken one was removed |
| `Exam.DeliveryMode` / `FixedFormId` | — | none, and absent from the DTO |

---

## 7. Promises in UI text that the code does not keep

Each of these is a sentence a customer reads on a screen.

| Where | What it says | What happens |
|---|---|---|
| `/review/:id` header | "Your comment reaches them." | It reaches staff only |
| `/review/:id` comment field | "This is shown to them with their result" | It is not |
| `/review` marked tab | "To revisit a mark, or put one right" | The screen it opens is blank |
| `/settings` time zone | "Every exam clock and scheduled window is read in this zone" | True of the window, which no screen can set; the attempt clock is server time |
| `/settings` default language | "What people here get before they choose one" | Inert at the pin; closed by `c0d9fa2` (§13) |
| `/settings` pass mark | "Applied to a new exam unless its author changes it" | True through the exam form only; the server still defaults 60 |
| `/exams/:id` one-at-a-time | "The whole paper never reaches the browser" | True, and the switch changes nothing either way |
| `/exams/:id/structure` sections lede | "A section can have its own clock and its own floor" | Contradicted by the notice directly beneath it |
| `/questions/:id` question timer | "Leave empty to use the exam's own limit" | A value set here is enforced by nothing |

The section screen deserves credit inside its own row: `08cacb0` added a banner,
a per-row chip and a per-field warning saying in plain words that three of these
controls are stored and read by nothing. The lede above them was not updated to
match, which is the only thing wrong with an otherwise exemplary disposition.

---

## 8. What is new since the third pass, and how much of it a user can reach

| What shipped | Verdict |
|---|---|
| Sections delivered to the paper (`08cacb0`) | Built through every layer; **reachable by nobody** — finding 2 |
| Hotspot answer input (`9820158`) | Built, keyboard-accessible; **the image does not load** — finding 4 |
| File upload and spoken answer (`87331aa`, `59d436e`) | Candidate side complete; **the marker cannot open either** — finding 3 |
| Copy/paste blocking (`336976f`) | Works, explains itself, honest that it is a deterrent; **double-records** — finding 1 |
| Three new integrity signals (`493e0e9`) | Real, thresholded above human performance, tested not to fire on ordinary work, and a fourth deliberately refused |
| Roles screen (`3f865f3`) | **DONE.** 62 permissions, parent/child cascade, tri-state groups, whole set sent on save, static roles renamed and deleted by nobody |
| Organisations screen (`0d9f643`) | **DONE.** Host-only, verified live; refuses to create a tenant without a first administrator; deletion requires typing the name back |
| Candidate create and edit (`44fccba`) | **DONE**, and it wipes two fields — finding 5 |
| Re-marking (`1c2a5fd`) | Queue and server done; **the screen is blank** — finding 6 |
| Login page repaired, self-registration closed (`b9fef28`) | **DONE**, and it closed a real hole: `/Account/Register` was answering 200 inside customer tenants |
| Cross-tenant role repair (`6efa48f`) | **DONE**, with a dry-run-by-default SQL tool and a test that checks the user can *see* the role, not merely that the join row exists |
| Scheduled window in the tenant's clock (`d5cf42a`) | Correct, and gated behind a control that does not exist — finding 8 |
| Default pass mark from the tenant (`0d7ee65`) | Client-side only |
| Scale question authorable at last (`7d3f685`) | **DONE.** The type was previously impossible to author correctly: any mark on it was subtracted from everyone |
| Model answer on the marking screen (`1a6ce10`) | **DONE**, below the candidate's answer rather than above it, deliberately |
| Item statistics forgotten on a key change (`20e4af8`) | **DONE**, compared on the rendered key so a typo fix keeps history |
| Item-analysis row opens the question (`20e4af8`) | **DONE**, hidden from anyone without `Questions.Edit` |
| Brand colour applied (`6ac6ad5`) | **DONE** in the shell and at exam entry; absent from the sitting and result screens |
| `/swagger` returns 200 (`1f9c85e`) | **DONE** |
| Role-aware dashboard (`8d53cf1`) | **DONE.** Marker and observer each get a card, and the "four steps" lede is now shown only to somebody who builds exams |
| `tools/check-reachability.py` (`d2e1a3a`) | **DONE**, and validated against three seeded defects before being trusted |

Two new Angular routes (`/roles`, `/organisations`), two new nav entries, one new
controller route (`POST /api/assessment/media/answer`), one new application
service method. The roles and organisations screens add no server code at all —
they call ABP's own identity and tenant-management endpoints, guarded by ABP's
own permissions rather than by names invented here.

---

## 9. What I expected to be broken and is not

A review that only lists problems is not trustworthy. Each of these is something
I went looking for and did not find.

- **The candidate status fix is real, and I checked the data, not the code.** Six
  Completed, two Invited, matching six attempts and two people who have not sat.
- **The roles screen cannot leak tenancy.** The permission tree a tenant
  administrator is offered contains 62 permissions in three groups — Identity
  management, Setting management, Assessment — and **nothing about tenants**. I
  went looking for the escalation and it is not there. `/api/multi-tenancy/tenants`
  answers 403 for a tenant admin and 200 for the host, live.
- **The live permission matrix is unchanged, cell for cell, and still matches
  `roles.md`.** Twenty-five endpoints against five tenant accounts and a host
  account. Every cell review-3 published came back identical, and the six new
  rows behave. The marker's access to integrity signals and the coordinator's
  refusal — the one genuine judgement call in that document — are still enforced
  exactly as argued.

  | Endpoint | admin | coord. | author | marker | observer | host |
  |---|---|---|---|---|---|---|
  | `GET /api/assessment/exams` | 200 | 200 | 200 | **403** | 200 | 200 |
  | `GET /api/assessment/questions` | 200 | **403** | 200 | **403** | **403** | 200 |
  | `GET /api/assessment/questions/import/template` | 200 | **500** | 200 | **500** | **500** | 200 |
  | `GET /api/assessment/candidates` | 200 | 200 | **403** | **403** | **403** | 200 |
  | `GET /api/assessment/catalog/categories` | 200 | 200 | 200 | **403** | **403** | 200 |
  | `GET /api/assessment/results` | 200 | 200 | **403** | **403** | 200 | 200 |
  | `GET /api/assessment/results/export` | 200 | 200 | **403** | **403** | 200 | 200 |
  | `GET /api/assessment/attempts/running` | 200 | 200 | **403** | **403** | **403** | 200 |
  | `GET /api/assessment/review/queue` | 200 | **403** | **403** | 200 | **403** | 200 |
  | `GET /api/assessment/settings` | 200 | 200 | 200 | 200 | 200 | 200 |
  | `GET /api/app/users` | 200 | **403** | **403** | **403** | **403** | 200 |
  | `GET /api/identity/roles` | 200 | **403** | **403** | **403** | **403** | 200 |
  | `GET /api/permission-management/permissions` | 200 | **403** | **403** | **403** | **403** | 200 |
  | `GET /api/multi-tenancy/tenants` | **403** | **403** | **403** | **403** | **403** | 200 |
  | `GET /api/app/exam` | 200 | 200 | 200 | **403** | 200 | 200 |
  | `GET /api/app/review/queue` | 200 | **403** | **403** | 200 | **403** | 200 |
  | `GET /api/app/result` | 404 | 404 | 404 | 404 | 404 | 404 |

  The last row is `1f9c85e`'s opt-out taking effect. The `import/template` row is
  the anomaly review-3 named and it is still set: an `IActionResult` action whose
  permission lives on the app service, so the refusal escapes ABP's exception
  filter and surfaces as a server error. Nobody without `Questions.Create` sees
  the button, so no user meets it today.

  This matrix was measured on the 13:07 build (§1). It is nonetheless evidence
  about `HEAD` for these rows: no commit after that build changes an
  `[Authorize]` attribute on any of these services, and the roles and
  organisations screens add no server code at all — they call ABP's own identity
  and tenant-management endpoints, which have been there throughout. The one
  authorisation that *did* change after the build is
  `AssessmentMediaAppService`, and it is finding 3.
- **`check-reachability.py` is a good tool and it runs clean.** 30 routes, 42
  lazy loads, 0 problems. Its commit records that three defects were seeded — a
  link to a route that does not exist, a button calling a function nobody wrote,
  a guard demanding an undefined permission — and that it caught all three by
  name and position before being trusted. It also states what it cannot see. I
  confirmed independently that every route in the application has an inbound link
  or nav entry; the only unreachable path is bare `/exam`, which is finding 14's
  missing 404 route.
- **Localisation is still complete.** 694 keys asked for, 0 missing from `ar.json`,
  0 from `en.json`. The tool's second number is noisier than its first: at least
  six of the 59 "defined and asked for nowhere" keys — the `Link:State:*` family —
  are requested through runtime composition at `assignment.component.ts:215`.
- **`smoke-routes.js` passes**, including both negative assertions.
  **`probe-round-trip.js` reports zero fields worth a look.**
- **The browser suite is green where it should be.** 280 of 282 tests pass. Both
  failures are the same test, and both are caused by the uncommitted
  default-language work: the stub returns `defaultLanguage: 'ar'` while the test
  asks for an English session, and the new reader switches the page to Arabic
  under it. Somebody else corrected the fixture while this was being written.
- **The settings read-only case is reachable, and the sidebar gate is not new.**
  I suspected a regression: the sidebar entry is gated on
  `Administration.ManageSettings`, which only the admin holds. It predates the
  baseline, the route carries no guard, the user-menu link is inside no `@if`,
  and `GET /api/assessment/settings` is 200 for all five roles. A marker can
  still read the rules their marking runs under.
- **Nothing regressed that was not introduced by the last four commits.** Of the
  127 stories, the only downward movements are the three section fields (a
  reclassification, not a regression), the scheduled window (a reclassification),
  and three long-standing gaps the documents recorded too generously.

---

## 10. What the documents now get wrong

`use-cases.md` and `user-stories.md` are both pinned to `75b534d`, which is where
`business-review-3.md` was written. `db3b94d` rewrote `README.md`,
`requirements.md`, `use-cases.md` and `DeveloperGuide.md` at 10:35 — **before
seven of the fixes above landed.** The rewrite is much better than what it
replaced and it is already stale in a specific, listable way.

Wrong because the code moved:

- `use-cases.md` §5: "There is no section anywhere in the delivery path",
  "`AttemptQuestion` carries no section id", "the taker has no notion of a
  section", "Grading computes one flat total" on reporting. The first three are
  flatly wrong; the reporting clause is wrong on two screens.
- §7: "a person cannot be added or corrected by hand", and "both primary buttons
  open the import panel". Wrong.
- §9: "Three types cannot be answered at all"; "Every integrity signal is
  mislabelled"; "the tenant's switch to turn observation off is not consulted".
  All wrong.
- §10: "The marking screen binds neither"; "six signal types are defined and the
  screen reports two"; "A marker who mistypes a score has no route back" — the
  last is now half-wrong in the worse direction: there is a route and it leads
  nowhere.
- §12: "a row does not open the question"; "the difficulty index is never reset".
  Wrong.
- §13: "nine settings are saved; four are read" — eight exist, seven are read;
  "the colour reaches the invitation email and nothing else". Wrong.
- §16: "nothing in delivery, grading or reporting knows they exist". Wrong in
  code and, for a different reason, still true in effect.
- `requirements.md` FR-5.6 marks the scheduled window **met**; no screen can set
  one. FR-11.4's "the screen reports two of six types" is now wrong.

Wrong at `75b534d` too — the documents were never right about these:

- `use-cases.md` §13: "The read-only settings case is unreachable." The
  user-menu link landed in `3923129`, an ancestor of the pin. `business-review-3`
  caught this; the rewrite kept the sentence.
- §6: "No guarantee a resit differs." `RotatedFormIdAsync` has guaranteed it
  since `4a43679`, also an ancestor of the pin.

And `gap-analysis.md` is pinned to `0842cc9`, three revisions back. Its first
two ranked gaps — no deployable product, and a correct answer scored zero — are
both closed. It is the business document most likely to be quoted and the least
likely to be right.

---

## 11. What I could not determine, and why

Recorded so nobody re-derives it and concludes it was checked.

1. **Anything the last fourteen commits do at runtime.** §1: the API host is a
   build from 13:07 and I was asked not to restart it. The client halves of that
   work I could exercise, because `ng serve` is live; the server halves I could
   only read. Specifically unexercised end to end: the candidate upload door, the
   three new integrity signals, the re-mark queue, and the marker's view of an
   uploaded answer. Every finding about them in §5 is from code, and each names
   the file and the line.
2. **The single-origin question behind finding 4.** The hotspot image is
   server-relative and every sibling binding is made absolute, so the defect is
   certain in the development configuration. Whether a production deployment
   serves the SPA and the API from one origin — `deployment.md` discusses it — I
   did not establish, and if it does, the symptom disappears without the cause
   being fixed.
3. **The backend test suite.** I did not run `dotnet test`. Building would write
   to output directories the running host holds open, and the instruction not to
   disturb it outranks the value of the result. The commit messages claim 292
   backend and 26 live tests green; I have not verified that number. I did run the
   browser suite, which writes only to the gitignored `angular/test-results/`.
4. **Accessibility.** Unchanged from the last pass: no axe, no
   `@axe-core/playwright`, no pa11y, no accessibility assertion anywhere. The new
   work reads as careful — the hotspot has a real keyboard path with arrow-key
   nudging, the recorder's states are shape-coded rather than colour-coded, the
   file input is visually hidden rather than `display:none` so it stays
   focusable — and I could verify none of it. `PLT-03` claims WCAG 2.1 AA and
   remains unverifiable.
5. **Whether an invitation is deliverable.** SMTP still points at `127.0.0.1:25`.
   `InvitationEmail` is a pure function with tests over its output, so *what*
   would be sent is verified; *that it arrives* is not testable here. The
   From-line finding is from configuration and an absent argument, not from a
   received message.
6. **Load at the fixed state.** `load-test.js` exists and `7caeb71` reports 150
   concurrent journeys completing. I did not run it: 150 sittings against a
   shared database is not a read-only act.
7. **The section domain rule.** `ExamSection.IsFailedAt` is written and unit
   tested and called by nothing outside tests. I can confirm it exists and not
   that it behaves correctly once wired — and now, additionally, that it could
   never fire, because no section can contain a question.
8. **Writes through `/api/app/*`.** I confirmed the read surface answers and
   honours permissions across five accounts. I did not attempt a write, for the
   same reason as last time.

---

## 12. What to do next, in the order that pays

Ranked by cost to a real user. Effort is noted because six of the top eight are
hours.

| # | Work | Size | Closes |
|---|---|---|---|
| 1 | Delete the save-time paste branch in `SaveAnswerAsync`; add a test that pairs one blocked paste with the count the marker reads | small | Finding 1 |
| 2 | A section select on the question form and the passage dialog, and `examSectionId` on both client DTOs | small | Finding 2, and all of `08cacb0` |
| 3 | Authorise an answer blob by `Review.ViewQueue` plus attempt ownership; render `<audio controls>` for a recorded answer | small–medium | Finding 3 |
| 4 | Inject `MediaService` into the hotspot component; stop the stub handing it a `data:` URI so the test can fail | one line, plus the fixture | Finding 4 |
| 5 | Put phone and category on the candidate dialog, or make `Apply` patch rather than replace | small | Finding 5 |
| 6 | Parameterise the `NeedsManualReview` filter in `GetAnswersAsync`; re-fetch in `RemarkTests` | small | Finding 6 |
| 7 | Carry `ReviewComment` onto the candidate's result, or change the two sentences that promise it | small | Finding 7 |
| 8 | Date inputs for the scheduled window on the exam form | small | Finding 8, `EXM-12`, `requirements.md` FR-5.6 |
| 9 | Localise the six integrity sentences | small | Finding 9 |
| 10 | Restore `SavedFileName` on resume; apply the brand colour on the sitting and result screens | small | Finding 10 |
| 11 | Per-tenant sender name and address on the invitation | small–medium | Finding 11, `ASG-03`, `BRD-03` |
| 12 | Widen `smoke-routes.js` beyond fifteen routes and `probe-round-trip.js` beyond three entities | medium | `PLT-10` — and it would have caught findings 5 and 6 |
| 13 | Section clock, minimum and qualifying gate through grading | large | `EXM-07`, `EXM-08`, `EXM-09`, Use Cases 5 and 16 |

Items 1 through 7 are, together, a few days, and they remove every finding on
this page in which the software either tells somebody something untrue or hands
them a control that leads nowhere. Item 1 first and alone if only one thing is
done: it is the only finding here that accuses a named person of something they
did not do, in a number, on the screen where somebody decides whether they
passed.

**A note on the tools, because it is the cheapest lesson on this page.** Four
verification tools exist and all four ran clean this pass. Not one of them could
see any of the top six findings. `check-reachability.py` says so itself — it
cannot tell whether a handler that exists does anything worth doing. What would
have caught findings 2, 3, 5 and 6 is the same instrument in each case: a probe
that sends a value through the running product and reads it back the way the next
screen would. `probe-round-trip.js` is exactly that instrument and it covers
three entities out of roughly twenty. Widening it is item 12 and it is arguably
item 1.

---

## 13. Two commits landed while this was being written

Recorded rather than quietly folded in, because a reader needs to know which
sentences above were true of what and when.

**`c0d9fa2`** (16:13) commits the default-language reader that §1 records as
sitting uncommitted in the working tree. `Assessment.DefaultLanguage` is
therefore no longer inert: **all eight tenant settings are now read**, and
finding 2 is closed outright rather than closed but one. Three rows change with
it — the `DefaultLanguage` line in §6, its line in §7, and the qualifier on
finding 2. The commit also records that the author's first attempt was wrong for
an interesting reason: it inferred "this person chose a language" from the
session having one, and ABP always supplies a culture, so the condition was true
on a first visit and said nothing. The corrected signal records the *act* of
choosing. That is the same class of mistake as the seam defects this codebase
catalogues, caught by a test rather than by a user.

**`e8ac835`** (16:21) is a defect this review did not find, and it is worth
saying so plainly rather than filing it as news. The two text graders compared
Arabic characters as written. A candidate who typed «المدرسه» rather than
«المدرسة» — which is what most people type — scored zero on a correct answer.
Worse, a numeric answer written in Arabic-Indic digits («١٢٣») was not merely
unread: it was declared **wrong**, rather than routed to a person, so a mark was
taken off somebody for owning an Arabic keyboard. No Unicode normalisation form
folds any of this; it has to be written out. The fix deliberately does not reuse
the importer's normaliser, because that one turns dots and hyphens into spaces —
correct for matching a column heading, and wrong here, where it would turn "3.5"
into something else and invent a mistake nobody made. Eight of its eleven tests
fail without it.

I did not find this because I traced controls to mechanisms and did not read the
graders' string comparison. It is the single most costly kind of defect this
product can have — an Arabic-first assessment platform marking correct Arabic
wrong — and it sat inside a use case this document marks **DONE**, which is the
sharpest possible illustration of the warning at the top of this page: DONE
measures reachability, not correctness.

---

*Pinned to `d2e1a3a`, `feat/platform-foundation`. The API host is a build from
13:07 (`6efa48f`) and was not restarted; the SPA is `ng serve` against a working
tree that carried four uncommitted files. Nothing was written to the database. No
source file was changed. `angular/e2e/exam-form.spec.ts` was modified, and then
two commits landed, while this document was being written — which is the fourth
review in a row to end by noting that a status document in this repository has a
shelf life measured in hours.*
