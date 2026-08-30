# Business review, third pass — every tab, every link, every story

`business-review.md` found three features the commit log called finished and
nobody could reach. `business-review-2.md` found that the results never arrived
at the person who paid for them. Both were right, and both are now largely
history: twenty-four commits have landed since `use-cases.md` and
`user-stories.md` were pinned to `0842cc9`, and between them they closed the
fill-in-the-blank scoring defect, the dead password field, the unbranded
invitation body, the missing expiry extension, the missing link reissue, form
rotation on a retake, five real roles, and a question-bank importer that nothing
in the docs mentions.

So this pass is not a re-run of those two. It is the review the product owner
asked for — **every tab, every component, every link, every use case and every
user story** — re-derived at `75b534d` against a running server, and it is
written to be actionable without rediscovery.

**The method, stated so it can be checked.**

1. Every route in `angular/src/app/**/*.routes.ts` and every entry in
   `core/navigation.ts` enumerated and resolved against each other.
2. Every application-service method, its `[Authorize]`, and its controller route
   enumerated from `src/`.
3. The verification tools run against the live API on `https://localhost:44373`:
   `node tools/smoke-routes.js`, `node tools/probe-round-trip.js`,
   `python tools/check-localization.py`.
4. **A live permission matrix.** I signed in as `admin`, `coordinator`, `author`,
   `marker` and `observer` in `trading-academy` and called the endpoint behind
   every screen, then behind every in-screen action. Twenty-seven endpoints × five
   accounts. Where this document says a role can or cannot do something, it is
   because the running server said so, not because a file said so.
5. Live data read back where a claim was about state rather than code — which is
   how the first finding below was found.

Nothing was written to the database. No source file was changed.

**The vocabulary is the one the earlier reviews established, and it is worth
restating because the whole shape of this document turns on it.**

| Status | Means |
|---|---|
| **DONE** | A person completes this today, end to end, in the running product |
| **PARTIAL** | Part of it works and the journey stops somewhere, **or** a finished control exists that no mechanism reads — a dead control that makes a promise in writing |
| **ABSENT** | The journey cannot be started |

A dead control is worse than an absent feature. An absent feature disappoints; a
dead control lies, and the person it lies to acts on it. Nine of the thirty-three
PARTIAL rows below are that shape, and the top of the ranked findings is now held
by a screen that does not merely stay silent — it states something false about a
named person.

---

## 0. The headline, before the argument

**Sixty-nine of a hundred and twenty-five stories are complete, against sixty-two
a month ago, and eleven of sixteen use cases are walkable end to end.** The
product is further along than either previous review found it, and the walk a
salesperson can do in front of a training academy is now sixteen steps long.

Three sentences carry the rest:

1. **The product's remaining defects have moved from "the screen does nothing" to
   "the screen says something untrue."** A candidates roster reports "لم يُدعَ /
   Not invited" beside six people who sat and finished an exam. A marker is told
   a candidate pasted four times when the candidate alt-tabbed four times. An item
   with a wrong answer key is labelled "Measuring". These are not gaps; they are
   the product asserting facts it has not got.
2. **Sections remain the one feature-shaped hole, and it is the placement-test
   story.** Everything about sections is authorable and none of it is delivered,
   graded or reported. Sell the competency profile; do not promise a
   section-by-section result.
3. **The API has two doors and one of them is undocumented.** A complete second
   surface at `/api/app/*` answers alongside the hand-written `/api/assessment/*`
   routes. It honours permissions — I checked — but nothing tests it, no document
   names it, and it is what makes `GET /swagger/v1/swagger.json` return 500, so
   the API documentation two of our own documents send people to is down.

And one sentence that is not a finding but frames all of them: **the two
verification tools that exist are both excellent and both far too narrow.**
`smoke-routes.js` proves fifteen routes answer; the client calls about ninety.
`probe-round-trip.js` proves three entities keep what you send them; the product
has roughly twenty editable things, and the settings screen — where six of nine
controls are inert — is not one of the three. Widening those two tools is the
cheapest work on this page and it would have caught four of the top ten findings.

---

## 1. What moved since the documents were pinned

`use-cases.md` and `user-stories.md` are both pinned to `0842cc9` and both warn
that a status document here has a shelf life measured in hours. They were right.
Twenty-four commits later, here is what those documents now get wrong. This
matters more than the individual rows, because anyone acting on those files today
will act on six stale statuses.

| Doc says | Actually, at `75b534d` |
|---|---|
| `GRD-10` — a fill-in-the-blank answer is always scored zero | **Fixed** (`4a43679`). There is one input per blank, and a grader that cannot read an answer routes it to a person rather than returning wrong |
| `ADM-05` — the password field reports success and changes nothing | **Fixed** (`b07d970`). `probe-round-trip.js` confirms the round trip on a live server |
| `ASG-07` — there is no way to extend an expiry | **Built** (`7dd405e`). Forward-only, past dates refused by name, the URL unchanged, no attempt refunded |
| `ASG-06` — there is no resend | **Answered differently** (`a2fbf91`). `ReissueLinkAsync` mints a new address and kills the old one, granting no extra attempt. The plaintext token is still never recoverable, which is correct |
| `FRM-06` — no guarantee a resit differs | **Built** (`4a43679`). `RotateForms` picks `published[attemptsAlreadySat % published.Count]` |
| `IMP-01`/`IMP-04` — no importer, no parser, no symbol | **Substantially built** (`9da7c46`). A spreadsheet importer with a dry run, per-cell errors, Arabic/English header matching, three answer-key notations, and category/level mapping on the way in |
| Use Case 13 — "the read-only settings case is unreachable" | **Wrong now.** The user menu carries a Settings link with no permission check at all, and `GET /api/assessment/settings` returns 200 for every seeded role including the marker. The read-only case is reachable by exactly the people it was built for |
| `BRD-03` — the invitation carries no identity | **Half-fixed** (`4e59b1a`). The subject and body carry the centre's name and its colour, escaped, with the colour validated before it reaches a `style` attribute. The **From** line does not — see finding 11 |

Six of the eight are improvements the documents do not record. That is the
healthier direction for a status file to be wrong in, but it is still wrong, and
`user-stories.md`'s summary tables are the artefact most likely to be quoted in a
meeting.

---

## 2. The scoreboard

### Use cases — 16

| # | Use case | `use-cases.md` at `0842cc9` | This review at `75b534d` |
|---|---|---|---|
| 1 | Set up the catalogue | BUILT | **DONE** |
| 2 | Write a question the centre owns | BUILT | **DONE** |
| 3 | Attach a chart, a recording or a clip | BUILT | **DONE** |
| 4 | Build an exam and publish it | BUILT | **DONE** |
| 5 | Lay an exam out in sections and passages | PARTIAL | **PARTIAL** — unchanged, and the largest one |
| 6 | Approve the exact paper before it goes out | BUILT | **DONE** |
| 7 | Bring in a class and put it at a level | PARTIAL | **PARTIAL** — unchanged; still no way to add or correct a person by hand |
| 8 | Send an exam to a class | BUILT, invitation unbranded | **DONE** — reissue and extend now close the two named gaps |
| 9 | A candidate sits the exam | BUILT, one defect costing marks | **DONE** — that defect is fixed; two remain |
| 10 | Mark what a person has to mark | BUILT, two sharp gaps | **DONE** — both gaps remain exactly as described |
| 11 | Read the results and get them out | BUILT | **DONE** |
| 12 | Find the questions that have stopped measuring | PARTIAL | **PARTIAL** — a row still does not open the question |
| 13 | Put the centre's own name on it | PARTIAL | **PARTIAL** — and the count is worse than recorded: six of nine settings are inert, not seven of nine reading two |
| 14 | Give staff accounts and decide what they may do | PARTIAL | **DONE** — the password works and five roles are real and enforced |
| 15 | Bring an existing exam in | NOT BUILT | **PARTIAL** — a spreadsheet importer shipped; the Word file and the Forms export did not |
| 16 | Place a student by their profile | NOT BUILT | **ABSENT** |

**11 DONE · 4 PARTIAL · 1 ABSENT.**

### User stories — 125

| Epic | Stories | DONE | PARTIAL | ABSENT |
|---|---|---|---|---|
| 1 · The catalogue and the tenant's vocabulary | 6 | 2 | 3 | 1 |
| 2 · The question bank | 12 | 8 | 1 | 3 |
| 3 · Getting existing exams in | 5 | 2 | 1 | 2 |
| 4 · Exams, sections and publishing | 12 | 7 | 5 | 0 |
| 5 · Blueprints and per-candidate assembly | 7 | 3 | 1 | 3 |
| 6 · Named forms | 8 | 6 | 0 | 2 |
| 7 · People and cohorts | 7 | 4 | 3 | 0 |
| 8 · Assignment and links | 9 | 7 | 2 | 0 |
| 9 · Sitting the exam | 16 | 9 | 3 | 4 |
| 10 · Grading and the reviewer's queue | 10 | 5 | 2 | 3 |
| 11 · Results, item health and export | 12 | 5 | 3 | 4 |
| 12 · The tenant's own face | 5 | 1 | 3 | 1 |
| 13 · Access and administration | 6 | 5 | 1 | 0 |
| 14 · How the product behaves everywhere | 10 | 5 | 5 | 0 |
| **Total** | **125** | **69** | **33** | **23** |

Against `user-stories.md`'s 62 / 33 / 30: **seven stories moved to DONE and seven
moved out of ABSENT**, and PARTIAL is unchanged in size while turning over
internally. PARTIAL not shrinking is the shape to watch. It is not a queue that
is being worked down; it is a bucket that refills, because each increment ships
a control before the mechanism that reads it.

Per-story verdicts that differ from `user-stories.md` are listed in §9. Every
other row is confirmed unchanged, which is itself a finding: nothing regressed.

---

## 3. The map — screen ↔ route ↔ service ↔ permission

Thirty-one routes. Twelve sidebar entries plus a user menu. Thirteen application
services, ninety-four service methods, fifty declared permissions.

### Staff routes

| Route | Screen | Route guard demands | Service behind it | Service class guard |
|---|---|---|---|---|
| `/` | Dashboard | `authGuard` only | — | — |
| `/exams` | Exam list | `Assessment.Exams.View` | `ExamAppService` | `Assessment.Exams` |
| `/exams/new`, `/exams/:id` | Exam form | `Assessment.Exams.View` | `ExamAppService` | `Assessment.Exams` |
| `/exams/:examId/questions[/new|/:qid]` | Question list / form, exam-scoped | `Assessment.Exams.View` | `QuestionAppService` | `Assessment.Questions` |
| `/exams/:examId/blueprint` | Blueprint editor | `Assessment.Exams.View` | `ExamAppService` | `Assessment.Exams` |
| `/exams/:examId/structure` | Sections and passages | `Assessment.Exams.View` | `ExamStructureAppService` | `Assessment.Exams` |
| `/exams/:examId/forms` | Named papers | `Assessment.Exams.View` | `ExamStructureAppService` | `Assessment.Exams` |
| `/questions[/new|/:qid]` | Bank list / form | `Assessment.Questions.View` | `QuestionAppService` | `Assessment.Questions` |
| `/candidates` | People | `Assessment.Candidates.View` | `CandidateAppService` | bare `[Authorize]` |
| `/groups` | Classes | `Assessment.Groups.View` | `CandidateAppService` | bare `[Authorize]` |
| `/assignments` | Exam picker | `Assessment.Assignments.View` | `ExamAppService` | `Assessment.Exams` |
| `/assignments/:examId` | Send and link table | `Assessment.Assignments.View` | `AssignmentAppService` | `Assessment.Assignments` |
| `/results` | Roster and summary | `Assessment.Results.View` | `ResultAppService` | `Assessment.Results.View` |
| `/results/questions` | Item analysis | `Assessment.Results.View` | `ResultAppService.GetItemAnalysisAsync` | `Results.ViewItemAnalysis` |
| `/results/running` | Attempt monitor | `Assessment.Results.View` | `AttemptAdminAppService` | `Assessment.Attempts` |
| `/results/:attemptId` | One candidate's paper | `Assessment.Results.View` | `ResultAppService` | `Assessment.Results.View` |
| `/review` | Marking queue | `Assessment.Review.ViewQueue` | `ReviewAppService` | `Assessment.Review` |
| `/review/:attemptId` | Marking screen | `Assessment.Review.ViewQueue` | `ReviewAppService` | `Assessment.Review` |
| `/catalog` | Catalogue and vocabulary | `Assessment.Catalog.View` | `CatalogAppService` | `Assessment.Catalog.View` |
| `/users` | Staff accounts | `...IdentityManagement.Users.View` | `UserAppService` | `...Users` |
| `/settings` | Tenant settings | **`authGuard` only — no permission** | `TenantSettingsAppService` | bare `[Authorize]`; write needs `Administration.ManageSettings` |
| `**` | *redirect to `/`* | — | — | — |

### Anonymous routes

`/exam/:token`, `/exam/:token/sitting`, `/exam/:token/result` — no guard, and
correctly so: the token is the credential. `ExamTakingAppService` is
`[AllowAnonymous]` at class level and validates a signed session token from the
`X-Exam-Session` header on every call.

### The live permission matrix

Every cell is the HTTP status the running server returned. This is the evidence
behind every permission claim in this document.

| Endpoint behind the screen | admin | coordinator | author | marker | observer |
|---|---|---|---|---|---|
| `GET /api/assessment/exams` | 200 | 200 | 200 | **403** | 200 |
| `GET /api/assessment/questions` | 200 | **403** | 200 | **403** | **403** |
| `GET /api/assessment/questions/types` | 200 | **403** | 200 | **403** | **403** |
| `GET /api/assessment/candidates` | 200 | 200 | **403** | **403** | **403** |
| `GET /api/assessment/candidates/groups` | 200 | 200 | **403** | **403** | **403** |
| `GET /api/assessment/catalog/categories` | 200 | 200 | 200 | **403** | **403** |
| `GET /api/assessment/results` | 200 | 200 | **403** | **403** | 200 |
| `GET /api/assessment/results/summary` | 200 | 200 | **403** | **403** | 200 |
| `GET /api/assessment/results/item-analysis/{id}` | 200 | **403** | **403** | **403** | 200 |
| `GET /api/assessment/results/export` | 200 | 200 | **403** | **403** | 200 |
| `GET /api/assessment/attempts/running` | 200 | 200 | **403** | **403** | **403** |
| `GET /api/assessment/review/queue` | 200 | **403** | **403** | 200 | **403** |
| `GET /api/assessment/review/attempts/{id}` | 200 | **403** | **403** | 200 | **403** |
| `GET .../review/attempts/{id}/integrity` | 200 | **403** | **403** | 200 | **403** |
| `GET /api/assessment/assignments/links/{examId}` | 200 | 200 | **403** | **403** | **403** |
| `GET /api/assessment/exam-structure/sections/{id}` | 200 | 200 | 200 | **403** | 200 |
| `GET /api/assessment/questions/groups/{examId}` | 200 | **403** | 200 | **403** | **403** |
| `GET /api/assessment/settings` | 200 | 200 | 200 | 200 | 200 |
| `GET /api/app/users` | 200 | **403** | **403** | **403** | **403** |

**This matrix matches `docs/business/roles.md` in every cell.** That document is
accurate, and it is the first thing in this repository that describes a
restriction and turns out to be describing one. The one row worth reading twice
is `.../integrity`: 200 for the marker, 403 for the coordinator, which is the
deliberate judgement `roles.md` argues for, actually enforced.

One anomaly, and it is a defect: `GET /api/assessment/questions/import/template`
returns **500** for the coordinator, the marker and the observer, and 200 for the
admin and the author. Everything else refuses with 403. This is the same class of
defect `roles.md` records for the results export — a controller action returning
`IActionResult` escapes ABP's exception filter, so an authorisation failure
surfaces as a server error. The fix there was to declare the permission as an
attribute on the action; `QuestionController.GetImportTemplateAsync` has not had
it. Nobody without `Questions.Create` sees the button, so no user hits this today
— but it is the same trap, re-set, three commits after it was disarmed.

---

## 4. Findings, ranked by what they cost a real user

### 1 — The candidates screen states a falsehood about a named person

**`Candidate.Status` is read, filtered on, projected and rendered — and never
written.** `CandidateAppService.Apply` sets three fields:

```csharp
private static void Apply(Candidate candidate, CreateUpdateCandidateDto input)
{
    candidate.PhoneNumber = input.PhoneNumber;
    candidate.CategoryId  = input.CategoryId;
    candidate.Reference   = input.Reference;
}
```

`CreateUpdateCandidateDto` has no `Status` property at all. `Candidate.Status`
defaults to `Pending` and the only other two occurrences of it in the whole
Application and Domain layers are a `Where` clause and a DTO projection.

I read the live data. In `trading-academy` there are eight candidates and six
attempts; six of the eight people have sat an exam and submitted it. **All eight
read status 0.** The Arabic label for status 0 is **«لم يُدعَ»** — *not invited*.

So the coordinator's roster tells them that the student whose paper they marked
last week has not been invited. That is not an empty state or a missing feature;
it is a screen answering a question with the opposite of the truth, in a column
headed «الحالة», beside a person's name. And the status filter above it can only
ever return everything or nothing, which is worse than no filter, because a
coordinator who filters to «أنهى» sees an empty table and concludes nobody
finished.

Cost: the highest on this page, because it is the screen a coordinator opens to
answer "who still needs chasing", and it answers "all of them."

*Where to act:* either write the status from the delivery path (`Invited` on link
creation, `InProgress` on start, `Completed` on submit) or delete the column and
the filter. Do not leave a third state where the column is right for new rows and
wrong for old ones.

### 2 — Six of the nine settings are inert, and two of them are consent switches

`use-cases.md` records "nine settings are saved; two are read." The true count is
worse, and the two that matter most are not the ones the doc names.

| Control on `/settings` | Read anywhere outside the settings service? |
|---|---|
| Organisation name | **Yes** — exam entry page, invitation subject and body |
| Logo | **Yes** — exam entry page, staff shell |
| Brand colour | **Yes, but only in the invitation email.** Nothing in the SPA reads it; `setProperty` appears nowhere in `angular/src`, and `--astro-brand-*` are fixed values in `_tokens.scss` |
| Default language | **No** |
| Time zone | **No** |
| Default pass mark | **No** — `exam-form.component.ts` hardcodes `passingPercentage: 60` |
| Show the result to the candidate | **No** |
| Record integrity signals | **No** |
| Enable self-registration | **No** — and `/api/app/self-registration-setting` is 404 |

Each of these carries a hint that is a written promise. Quoting them is the
fastest way to see the cost:

- *"Every exam clock and scheduled window is read in this zone. Getting it wrong
  opens exams at the wrong hour."* — nothing reads it; every clock is
  `Clock.Now`.
- *"Applied to a new exam unless its author changes it."* — a centre that sets 70
  gets 60 on its next exam.
- *"As soon as marking finishes. Turn it off where a result has to be released by
  a person — a certificate that arrives before the coordinator has seen it is
  hard to take back."* — `ExamTakingAppService.GetResultAsync` has no gate. The
  administrator turns it off and every candidate still sees their score the
  instant grading finishes.
- *"Pasting, leaving the tab, and how long each answer took. … in some places not
  something to record without telling people."* — the switch does nothing, and
  nobody is told. `RecordSignalAsync` inserts unconditionally.

The last two are governance controls, not preferences. An administrator who
turned observation off in order to run a low-stakes practice programme, or to
satisfy a local rule, has been told it is off and it is on. **And the per-exam
switch is dead too:** `Exam.CollectIntegritySignals` is on the exam form, saves,
round-trips, and is consulted nowhere in the delivery path — it appears only in
`ExamAppService`'s `Apply` and two projections. Both switches, at both levels,
are inert.

Cost: an administrator makes a decision, is told it took effect, and it did not.
Two of the six are the kind of decision that gets written into a contract.

### 3 — Every integrity observation is recorded as a paste

Unchanged since `TAK-13` was first written, and it now costs more than it did,
because the `Marker` role exists and `roles.md` has argued — well, and at
length — that the marker should hold `Review.ViewIntegritySignals` *because a
paste event in the middle of a 400-word essay is the single most relevant fact
available to that judgement*. That argument is sound. The fact is not true.

The browser posts:

```ts
this.http.post(`${this.base}/signal`, { kind, detail }, ...)   // kind: 'window-blur' | 'paste'
```

The server binds:

```csharp
public class ReportIntegritySignalDto
{
    public IntegritySignalType Type { get; set; }   // Paste = 0, WindowBlur = 1, …
    public Guid? QuestionId { get; set; }
    public int? Magnitude { get; set; }
}
```

`kind` binds to nothing. `Type` takes its default, which is `Paste = 0`. Every
window-blur, on every attempt, in every tenant, is stored as a paste and counted
into `Attempt.IntegrityFlagCount`.

So the sentence the marker reads is: *this candidate pasted four times*. The true
sentence is: *this candidate's phone rang four times*. One of those is an
accusation and the other is nothing at all, and the product cannot tell them
apart while presenting itself as being able to.

The candidate is also never told they are observed, on any screen.

*Why it survives:* there is no test anywhere that pairs the client's payload with
the server's DTO. The client test stubs the endpoint; the server test constructs
the DTO directly. Both sides pass. This is the sixth instance of the seam defect
`user-stories.md` catalogues, and the two named as still open — `GRD-10` and
`TAK-13` — are now one.

### 4 — Sections: authored, saved, and invisible to everything downstream

Unchanged, and still the largest feature-shaped hole. `ExamSectionId` appears in
`ExamStructureAppService`, `QuestionAppService`, `ExamBlueprintRule`, `Question`
and `QuestionGroup` — and **nowhere in `ExamTakingAppService`, nowhere in
`AttemptGradingService`, nowhere in `ResultAppService`, and not at all on
`AttemptQuestion`.** A teacher sets "Listening: 20 minutes, minimum 60%,
qualifying", sees it saved, and every candidate receives one clock, no gate, and
one flat total.

Two controls on that screen are therefore dead in the strongest sense — the
section's own time limit and its qualifying flag — and the domain rule that fails
an attempt on a section minimum is written, unit-tested, and invoked by nothing.

Commercially this is the placement test. A result that says 62% cannot tell a
coordinator which class to put a student in. **The competency breakdown already
answers most of that question and is real** — a result reports listening 40%,
reading 85% by topic — so the honest sales position is unchanged from
`use-cases.md`: sell the competency profile, and do not promise a
section-by-section report until delivery lands.

*Interim recommendation, unchanged and now overdue:* hide the section time limit
and the qualifying flag. A control that saves and does nothing is a defect the
author will not discover until a candidate has sat the paper.

### 5 — The marking screen still hides the model answer, and a mark cannot be reopened

Both exactly as `use-cases.md` describes them, verified again.

`ReviewAnswerDto` carries `CorrectAnswer` and `Explanation`. The Angular model in
`review.service.ts` types both. The 164-line marking template binds **neither** —
`grep` for either identifier in `review-attempt.component.html` returns nothing.
The renderer, the transport and the client type are finished; the whole remaining
cost is one template binding, and until it is paid every marker marks blind
against a rubric with no key.

And `ReviewAppService` filters both the queue and the answers endpoint on
`NeedsManualReview`:

```csharp
.Where(a => a.AttemptId == attemptId && a.NeedsManualReview)
```

Marking clears that flag, so reopening a marked attempt returns an empty list and
a blank screen. A marker who mistypes a score has no route back, and the
component still contains a step commented "so a reopened attempt shows its marks"
that can never run.

Cost: these are the first and second things a working marker hits on day one.

### 6 — A person cannot be added or corrected by hand

`CandidateAppService.CreateAsync` and `UpdateAsync` both exist, both carry
permissions, both have controller routes. `CandidateService.create()`,
`.update()` and `.get()` all exist in Angular. **All three are called from
nowhere** — verified by resolving the injected variable name rather than by
substring, so this is not a false positive.

Both primary buttons on `/candidates` call `openImport()`. The row renders one
action: delete. The component declares `readonly canEdit = permissionSignal(P.Candidates.Edit)`
and never references it — `canEdit` appears zero times in the template. And
`canCreate`, which does appear, gates the **import** button, so the permission
named "may create a candidate" grants access to a paste box.

Paste import is the only door in, and the only way out of a typo is deleting the
person, which `DeleteAsync` refuses once they have attempts. So a misspelt name
on someone who has sat an exam is permanent.

There is no `/candidates/:id` route either, so `PPL-04` — one person's history —
has no screen to live on.

Cost: three endpoints and one permission exist to serve a journey with no
controls. This is the cheapest large win on the page: two forms.

### 7 — Swagger is down, and a delivered requirement with it

`docs/requirements.md` lists, under non-functional requirements:

> ✅ Swagger Integration — تكامل كامل مع Swagger لتوثيق الـ API

`GET /swagger/v1/swagger.json` returns **500**:

```
Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorException:
Conflicting method/path combination "GET api/app/result" for actions -
ResultAppService.GetListAsync, ResultAppService.GetAsync.
```

`/swagger/index.html` loads, so the page appears to work and then shows nothing.
The cause is finding 8. Every integrator, every future front end, and every
person trying to learn this API is currently reading source code instead.

### 8 — A second, undocumented API surface

`InternshipManagementSystemHttpApiHostModule` calls
`options.ConventionalControllers.Create(...ApplicationModule).Assembly)` — twice,
once inline and once through `ConfigureConventionalControllers()` — and no
application service carries `[RemoteService(IsEnabled = false)]`. ABP therefore
generates a controller for every application service alongside the hand-written
`/api/assessment/*` ones.

I confirmed this against the running server rather than inferring it:

```
200 GET /api/app/exam                 → the exam list
200 GET /api/app/catalog/categories   → the whole catalogue
200 GET /api/app/tenant-settings      → every tenant setting
200 GET /api/app/question/types       → the type registry
200 GET /api/app/review/queue         → the marking queue
```

**It honours permissions.** `/api/app/exam` is 403 for the marker,
`/api/app/catalog/categories` is 403 for the marker and the observer,
`/api/app/review/queue` is 403 for everyone but the marker and the admin. I went
looking for an open door and there is not one — the services' own `[Authorize]`
attributes travel with them. That is the good news and it should be said plainly.

What is wrong with it is everything else:

- It breaks Swagger outright (finding 7).
- No document mentions it. `smoke-routes.js` does not test it. No e2e touches it.
- Where a hand-written controller adds a guard the service lacks, the second door
  does not have it. `ResultController.ExportAsync` was given two explicit
  `[Authorize]` attributes specifically to turn a 500 into a 403 — a fix
  `roles.md` records. `/api/app/result/export-csv` happens to 404, so that
  particular case is closed by luck rather than by design.
- `ExamTakingAppService` is `[AllowAnonymous]`, so its conventional twin accepts
  the session token as an ordinary parameter rather than through the
  `X-Exam-Session` header. Authorisation still holds — `RequireSession` validates
  the signed token either way — but a credential that can travel in a query
  string ends up in access logs, browser history and `Referer` headers, and the
  header discipline the hand-written controller enforces is not the boundary it
  looks like.

*Where to act:* one `[RemoteService(IsEnabled = false)]` on each application
service, or one line removing the conventional registration. Swagger comes back
with it.

### 9 — Item health cannot say the thing it exists to say

`Question.DiscriminationIndex` is a column, is projected onto the DTO, and is
**assigned nowhere** — permanently null. `AttemptGradingService` updates
`DifficultyIndex` and `TimesAnswered` and nothing else.

So the item-health chip on the question list classifies from difficulty alone:

```ts
if (question.timesAnswered < 20 || question.difficultyIndex == null) return 'unmeasured';
if (question.difficultyIndex >= 0.95) return 'tooEasy';
return question.difficultyIndex <= 0.15 ? 'tooHard' : 'healthy';
```

A question with a wrong answer key is hard, so it is labelled **«يقيس» /
"Measuring"** or "Too hard" — never "the key is wrong", which is precisely the
one distinction the difficulty/discrimination pair exists to draw.

And `DifficultyIndex` is a lifetime running mean that is never reset when a
question or its key is edited, so an author who fixes a wrong key inherits the
wrong key's statistics, and the chip reports them as fact.

The `/results/questions` screen does compute discrimination properly at read
time — and `4a574ab` made it honest by reporting *unmeasurable* rather than zero
when a quartile never saw the question, which was a real fix. But that screen
throws the number away, and **a row still does not link to the question**, which
is most of the value: "these six questions are not measuring anything" is the
most credible sentence this product can say to an assessment professional, and it
cannot take the teacher to any of the six.

### 10 — Three question types cannot be answered

`ANSWER_INPUTS` in `angular/src/app/features/take/answers/answer-input.ts` maps
ten of the thirteen shipped types. `hotspot`, `file-upload` and `audio-response`
fall through to `FALLBACK_ANSWER_INPUT` — a plain textarea.

The fallback is the right design decision and the comment defending it is
correct: an unknown type must not strand a candidate on a question they can read
and cannot answer, with a clock running. But an author has a finished hotspot
editor with region drawing, and a candidate is given a text box. The authoring
half of "no input anywhere may require programming skill" is kept; the answering
half is three-thirteenths short.

`fill-in-the-blank` was the fourth and the worst, and it is fixed.

### 11 — The invitation still arrives from a stranger

`4e59b1a` put the centre's name into the subject and the body, escaped the
values, validated the colour before it reaches a `style` attribute, and
deliberately left the logo out because a bearer-protected image renders broken in
a mail client. All of that is careful and right.

What it did not change is the **From** line. `AssignmentAppService` calls
`SendAsync(candidate.Email, subject, body, isBodyHtml: true)` with no sender, so
ABP uses the host-wide settings in `appsettings.json`:

```json
"Abp.Mailing.DefaultFromAddress":     "no-reply@localhost",
"Abp.Mailing.DefaultFromDisplayName": "Assessment Platform",
"Abp.Mailing.Smtp.Host":              "127.0.0.1"
```

The student's inbox shows **Assessment Platform** in the sender column. That is
the field a person actually looks at before deciding whether a message with a
long token link is a phishing attempt, and it is the one field the tenant cannot
reach — it is a host setting, not a per-tenant one, so all three seeded
organisations would send as the same stranger.

And the transport still points at `127.0.0.1:25` with a null sender in DEBUG
builds, so no invitation has yet been delivered anywhere.

### 12 — Two of the five roles land on an empty page

The dashboard is the index route, is shown to everyone, and renders four
"getting started" cards gated on `Catalog.Manage`, `Exams.Create`,
`Candidates.Create` and `Assignments.Create`.

- Admin: 4 cards.
- Author: 2. Coordinator: 2.
- **Marker: 0. Observer: 0.**

There is no empty state. A marker signing in for the first time sees «أهلاً بك»,
then *"أربع خطوات تفصلك عن أول اختبار في يد أول متقدّم"* — four steps between
here and your first exam in someone's hands — under a heading reading «خطوات
البدء», with nothing beneath it. The sidebar has one item.

Cost: it is the first screen of the product for two of the five roles the
business has just defined, and it promises four steps and shows none.

### 13 — Dead controls, catalogued

Everything a user can operate today that no mechanism reads. This is the list
worth keeping, because it is the failure shape this codebase repeats.

| Control | Screen | What reads it |
|---|---|---|
| Section time limit | `/exams/:id/structure` | nothing |
| Section qualifying flag | `/exams/:id/structure` | nothing |
| Section minimum percentage | `/exams/:id/structure` | nothing in grading |
| Exam · "record integrity signals" | `/exams/:id` | nothing |
| Exam · "one question at a time" | `/exams/:id` | nothing — the taker always serves one at a time regardless |
| Settings · brand colour | `/settings` | the invitation email only; nothing in the app |
| Settings · default language | `/settings` | nothing |
| Settings · time zone | `/settings` | nothing |
| Settings · default pass mark | `/settings` | nothing |
| Settings · show result to candidate | `/settings` | nothing |
| Settings · record integrity signals | `/settings` | nothing |
| Settings · enable self-registration | `/settings` | nothing |
| Candidates · status filter | `/candidates` | a column that is always `Pending` |
| Catalogue · vocabulary editor | `/catalog` | the catalogue screen and nothing else |

Fourteen. `user-stories.md` counted ten and it was counting the ones it knew
about.

The mirror-image defect also exists and is rarer: **`Exam.AllowBackNavigation` is
honoured by the taker** — `canGoBack` reads it and `previous()` obeys it — **and
has no control on the exam form.** It defaults to true and cannot be turned off.
Same for the availability window: `Exam.IsOpenAt` is enforced in two places in
the delivery path, `ExamAppService.UpdateAsync` validates the dates, and no input
on the form sets them.

### 14 — Smaller, verified, and worth naming

- **`ExamStructureAppService.UpdateSectionAsync` ignores `input.ExamId`.** The
  field is `[Required]`, so every caller must send it, and `Apply` never touches
  `section.ExamId`. Sending a different exam id returns 200 and moves nothing.
  Cross-tenant is safe — the repository is tenant-filtered — but this is a write
  that reports success and discards part of its input, which is exactly what
  `probe-round-trip.js` was built to find and does not cover.
- **`UserAppService` writes `user.Surname = string.Empty` unconditionally** in
  both create and update. `CreateUpdateUserDto` has no surname field, so a
  surname set through ABP's own identity module is wiped on the next save from
  the staff screen.
- **A phone number cannot be cleared.** Both create and update apply
  `PhoneNumber` only when non-blank.
- **There is no 404 route.** `{ path: '**', redirectTo: '' }` sends every unknown
  URL to the dashboard. A stale deep link is indistinguishable from a successful
  navigation home — and bare `/exam`, which is what a taker gets from a truncated
  link, lands them on the staff login instead of on a page that explains
  anything.
- **`/results/running` has no way out.** The attempt monitor contains no
  `routerLink` and no `navigate` call at all. A coordinator watching a sitting
  cannot click through to that candidate's result or to the marking queue.
- **A nav/route permission mismatch, currently latent.** The sidebar shows the
  attempt monitor on `Assessment.Attempts.View`; the route guard demands
  `Assessment.Results.View`; the service demands `Assessment.Attempts`. No seeded
  role is hurt — the coordinator holds all three and the observer sees no link —
  but an observer typing the URL passes the route guard and is refused by the
  server, which is the "offer nothing that cannot be opened" rule failing in the
  other direction.
- **Six declared permissions are never checked, and the inconsistency behind that
  is invisible.** `Assessment.Candidates`, `Assessment.Groups`,
  `Assessment.Results`, `Assessment.Catalog`, `Assessment.IdentityManagement` and
  `Assessment.Administration` appear in no attribute and no explicit check.
  `AuthorizationCoverageTests.IsGroupingPermission` exempts every permission that
  has children, on the documented and defensible grounds that ABP's permission
  screen grants a parent alongside a child. That exemption is right — but it
  hides a real split: six services guard at the group
  (`ExamAppService`, `ExamStructureAppService`, `QuestionAppService`,
  `AssignmentAppService`, `AttemptAdminAppService`, `ReviewAppService`, and
  `UserAppService` at `...Users`) and the other six do not —
  `CandidateAppService` carries a bare `[Authorize]`, `ResultAppService` and
  `CatalogAppService` carry a leaf, `TenantSettingsAppService` carries a bare
  `[Authorize]`. `roles.md` opens with "a rule that shapes every list below":
  class and method attributes combine with AND, so a role holding only a leaf is
  refused on every request. That rule is true of half the services and not the
  other half, and the seeder's ancestor expansion is doing no work for the second
  half. Nothing is broken today; the model simply means two different things in
  two places, which is how a future role gets granted a set that reads correctly
  and behaves differently.
- **A whole table nothing uses.** `TenantBranding` — an entity with
  `LogoBlobName`, `SupportEmail`, `CertificateFooter`, a colour validator and a
  migration — is registered as a `DbSet`, configured in the EF model, and read
  or written by no application code. `use-cases.md` §13 describes an
  administrator setting "an alternate-language name, a support email, a
  certificate footer"; those three fields exist on this dead entity and on no
  screen.
- **A dead component.** `features/placeholder/placeholder.component.ts` is
  referenced by nothing.
- **The marking screen does not gate its Award button on `Review.Grade`.** The
  integrity panel handles its own refusal gracefully — `getIntegrity()`'s error
  branch is deliberately silent, so a marker without
  `Review.ViewIntegritySignals` simply sees no panel, which is exactly the
  configuration `roles.md` argues the separate permission exists to allow. The
  Award button has no such treatment: a role holding `ViewQueue` without `Grade`
  would open an attempt, score every criterion, press Award and be refused. No
  seeded role splits the two, so this is latent — but `roles.md` explicitly
  invites an organisation to split them.
- **`requirements.md` promises "File Upload & Cleanup — تنظيف تلقائي للملفات
  القديمة".** There is exactly one background worker in the solution
  (`AttemptTimeoutWorker`) and it auto-submits attempts. Nothing ever deletes a
  file. Question deletion is soft — correctly, so historic papers stay
  reproducible — so every uploaded image and clip is kept for ever by design and
  the requirement describing otherwise has never been true.

---

## 5. Dead ends — the direct answers

**Nav links to routes that do not exist:** none. All twelve sidebar entries and
both user-menu entries resolve to a registered route. The seven dead links the
earlier reviews found are all gone and none has come back.

**Buttons bound to nothing:** none. Every `(click)`, `(submit)` and `(change)`
handler in every feature template — including the inline-template components —
was resolved against a member of its component. All of them bind.

**Screens that read a permission never granted:** none. Every permission a screen
reads is granted to at least one seeded role, verified live.

**Permissions declared and enforcing nothing:** six, listed above.
`Assessment.Administration.Access` — the one `use-cases.md` says should be
removed — **has been removed**, with a comment where it stood.

**Client methods with no caller:** three, all on `CandidateService` — `create`,
`update`, `get`.

**Endpoints with no client:** three, the counterparts of the above —
`POST /api/assessment/candidates`, `PUT /api/assessment/candidates/{id}`,
`GET /api/assessment/candidates/{id}`. Plus the entire `/api/app/*` conventional
surface, which no client calls and every caller can reach.

**Service methods with no controller route:** none. All ninety-four methods on
all thirteen interfaces have an explicit route.

**Endpoints that return 200 while doing nothing:**
`ExamStructureAppService.UpdateSectionAsync` when `ExamId` differs;
`TenantSettingsAppService.UpdateAsync` for six of its nine fields;
`ExamTakingAppService.ReportSignalAsync` for every non-paste signal, which
returns 200 and stores a falsehood.

**Localisation keys asked for and not defined:** zero, of 624. See §6.

---

## 6. What I expected to be broken and is not

A review that only lists problems is not trustworthy. Each of these is something
I went looking for on the strength of a previous document or a reasonable guess,
and did not find.

- **Localisation is complete.** 624 keys asked for by the client, zero missing
  from `ar.json`, zero from `en.json`. The tool that proves it understands the
  three ways a key gets written — literal, runtime-composed, and stored in a
  table away from its use — which is why its answer is trustworthy rather than
  noisy. Fifty-nine keys are defined and unrequested and almost all of them are
  server-rendered on purpose (question type names, link states, the import
  template's own headers). Four are worth a glance as possible orphans:
  `Exam:Take`, `Monitor:Answered`, `Results:Detail:Awarded`, `Results:Summary:Range`.
- **`smoke-routes.js` passes**, including the two negative assertions that matter:
  `/api/app/system-general-settings` and `/api/app/self-registration-setting` are
  both still 404, so neither of the deleted anonymous-write services has come
  back.
- **`probe-round-trip.js` reports zero fields worth a look.** The staff password
  round-trips on a live server. The defect that produced that tool is genuinely
  closed, proven by `CheckPasswordAsync` rather than by a status code.
- **The five roles are real restrictions.** Twenty-seven endpoints across five
  accounts, and every cell matches `roles.md`. This product now has permissions
  rather than checkboxes, and the marker's access to integrity signals — the one
  genuine judgement call in that document — is enforced exactly as argued.
- **The conventional API surface honours permissions.** I expected an open door
  and there is not one.
- **The item-analysis button is correctly gated.** I expected the coordinator to
  see a button leading to a 403; `result-list.component.html` gates it on
  `Results.ViewItemAnalysis`, which the coordinator does not hold, so they never
  see it. The route guard behind it is looser than the button, but no user meets
  that.
- **The settings read-only case is reachable.** `use-cases.md` records it as
  unreachable by exactly the people it was built for. The user-menu Settings link
  carries no permission check and the read endpoint returns 200 for every role
  including the marker, so a marker can read the rules their marking runs under.
  That is what was wanted.
- **The sidebar reads permissions reactively.** `permissionSignal` wraps
  `getGrantedPolicy$`, the observable form, so the zoneless "Dashboard-only
  sidebar for ever" defect cannot recur. Sections with no visible items are
  dropped rather than left as empty headings.
- **Route ordering is correct everywhere.** `/exams/new` before `/exams/:id`,
  `/results/questions` and `/results/running` before `/results/:attemptId`,
  `summary`, `export`, `types`, `import/template`, `groups`, `running` all
  declared before their sibling parameter routes.
- **`/health` answers.**
- **Nothing regressed.** Of the 125 stories, none moved backwards.

---

## 7. What the docs promise that the product does not do

**A note that changes how this section reads.** While this review was being
written, somebody else rewrote `docs/requirements.md` (64 lines → 505),
`docs/use-cases.md`, `docs/README.md` and `docs/DeveloperGuide.md` in the working
tree. I did not touch them and have not reverted anything. The three
`requirements.md` claims this section originally listed — Swagger, automatic file
cleanup, and candidate registration — were true of the committed file at
`75b534d` and **have all been cut from the rewrite**, which is the right
disposition for each of them. What follows is stated against the tree as it
stands at the moment of writing.

Documented claims still standing that the product does not keep:

1. **`DeveloperGuide.md` sends a developer to `https://localhost:44373/swagger`
   and `deployment.md` lists Swagger as a deployment endpoint.** The page loads
   and the document behind it is 500. Two documents point at a broken door.
2. **`use-cases.md` §13 — "The read-only case is unreachable."** Still present at
   line 658 of the rewritten file, and no longer true; see §6.

And on-screen, the seven settings hints quoted in finding 2 are each a written
promise the software does not keep. Those are the ones that reach a customer, and
none of them is affected by any document being rewritten.

**Credit where the rewrite earns it, because a review that ignores concurrent
work is a review that inflates itself.** The new `requirements.md` independently
records, and records well, several findings on this page: the dead per-exam
integrity switch (`FR-11.5`), five inert tenant settings (`FR-14.4`), the brand
colour reaching neither shell nor exam page (`FR-14.5`), no reopening a mark
(`FR-10.10`), sections absent from delivery (`FR-9.13`), no single-person history
(`FR-7.8`), and no topic filter on the bank (`FR-3.8`). It carries ten PARTIAL
and eleven UNMET markers and does not soften them.

What it does **not** contain, and what this review therefore still contributes
alone:

- The candidate status column reporting «لم يُدعَ» about people who finished
  (finding 1).
- Every integrity observation being stored as a paste (finding 3) — `FR-11.4`
  records that the screen reports only two of six signal types, which is a
  different and milder statement than "the type never binds and the default is
  always chosen".
- Swagger being down, and the second `/api/app/*` surface that causes it
  (findings 7 and 8).
- The `import/template` 500-instead-of-403.
- `UpdateSectionAsync` discarding `ExamId`; `Surname` being wiped; a phone number
  that cannot be cleared.
- The empty dashboard for the marker and the observer.
- The live permission matrix in §3, which is the evidence any of these documents
  would need to claim a role is enforced.

---

## 8. What the product does that the docs never mention

This is the other half of the review, and it is a longer list than expected.

- **The question-bank importer (`9da7c46`).** A whole feature — a 1,188-line
  parser, a dry run, per-cell errors carrying the row number the spreadsheet
  shows, Arabic and English header matching with normalisation of alef forms, taa
  marbuta, tatweel and both digit sets, three accepted notations for the answer
  key, a generated template, and a deliberate refusal to guess what English
  "multiple choice" means. It appears in no use case and no story. `IMP-01`
  through `IMP-04` are still written as NOT BUILT with "no parser, no import
  screen, no route, no symbol."
- **Link reissue (`a2fbf91`) and expiry extension (`7dd405e`).** `ASG-06` and
  `ASG-07` are both written as NOT BUILT.
- **Form rotation on a retake (`4a43679`).** `Assignment.RotateForms` and
  `RotatedFormIdAsync` close `FRM-06`, which is written as NOT BUILT. Worth being
  precise about what it is not: rotation indexes on *that candidate's* attempt
  count, so a whole class still sits Form A on its first sitting. `FRM-05` —
  spread a cohort across forms — is genuinely absent, and the control's label,
  *"Next paper each time (rotate)"*, does not claim otherwise. That is an honest
  label and it deserves saying.
- **The named-recipient list before sending, and copy-all.** The send panel now
  shows every person and address before the button, and says so when a class is
  empty rather than sending to nobody in silence.
- **Deployability (`41c97d3`).** Container images, CI, a runtime `config.json`
  read at boot, eight hardcoded literals turned into settings, an OpenIddict
  signing certificate, a data-protection key path and a `/health` endpoint.
  `docs/deployment.md` documents it; no story covers it.
- **The tooling.** `seed-tenants.js` (three organisations with real Arabic data),
  `load-test.js` (which found that 39 of 40 candidates could not start),
  `purge-test-data.sql`, `check-localization.py`, `probe-round-trip.js`,
  `seed-role-users.js`, and the live e2e suites for tenancy, roles and
  screenshots. This is a substantial verification capability that the business
  documents do not account for.
- **The staff shell has a language switch**, driven by the languages the server
  actually offers rather than a hardcoded list. The exam taker does not — which
  is `TAK-14`, and worth noting that the half that exists is the half fewer
  people needed.
- **Item analysis reports "unmeasurable"** rather than a false zero when a
  quartile never saw a question, and refuses to measure at all when the cohort's
  totals are too close for a quartile split to mean anything. That is a real
  piece of statistical honesty and it is recorded nowhere.
- **The `/api/app/*` surface**, and **the `TenantBranding` table**. Both exist,
  neither is written down, and neither is used by the client.

---

## 9. Story verdicts that differ from `user-stories.md`

Everything not listed here is confirmed at the status that document gives it.

**Moved to DONE (7):** `GRD-10`, `ADM-05`, `ASG-06`, `ASG-07`, `FRM-06`,
`IMP-04`, `ADM-02`.

**Moved out of ABSENT into PARTIAL (1):** `IMP-01` — a real, syntax-free import
route exists and lands in the catalogue; the Word file and the Forms export it
names do not.

**Moved from BUILT to PARTIAL (2), both because a promise was found to be
unkept:**

- `ASG-03` · *Deliver the invitation* — the message is the centre's; the sender
  is not, and cannot be made to be.
- `BRD-03` · *Carry the branding to where it matters* — same cause.

**Moved from PARTIAL to ABSENT (1):**

- `PPL-04` · *See one person's history* — recorded as PARTIAL on the grounds that
  the data is reachable by a search. There is no `/candidates/:id` route, no
  component and no inbound link, and the results roster filters by class and
  exam, not by person. Nothing of this journey can be started.

**Confirmed PARTIAL, but the description in the doc understates it (3):**

- `ADM-06` — six settings inert, not seven reading two, and two of them are
  consent switches.
- `BNK-09` — the doc says "the two filters a bank needs are absent". Category,
  level, type and difficulty filters all shipped; the topic filter did not, so
  "my listening questions" is still unaskable, but this is much closer than
  recorded.
- `PLT-10` — the tool exists and *is* run; it covers fifteen of roughly ninety
  routes the client calls, and the other route inventory, Swagger, is 500.

**New, not in `user-stories.md` at all (3), all found this pass:**

- **`PPL-08` · A person's status must mean something.** Finding 1.
- **`PLT-11` · One API surface, not two.** Findings 7 and 8.
- **`TAK-17` · A candidate is told they are observed.** Currently nobody is told,
  on any screen, in either language, and the switch that would turn observation
  off is inert.

---

## 10. What to do next, in the order that pays

Ranked by cost to a real user, not by effort — but effort is noted, because three
of the top six are hours rather than days.

| # | Work | Rough size | Closes |
|---|---|---|---|
| 1 | Write `Candidate.Status` from the delivery path, or delete the column and its filter | small | Finding 1 |
| 2 | Bind `kind` to `Type` on the integrity signal, and add one test that pairs the two payloads | small | Finding 3, `TAK-13` |
| 3 | Render `correctAnswer` and `explanation` on the marking screen | one binding | Finding 5, `GRD-05` |
| 4 | Hide the section clock and the qualifying flag until delivery reads them | small | Finding 4's dead half |
| 5 | Consult `CollectIntegritySignals` and `ShowResultToCandidate` in delivery; disable or remove the other four inert settings | small | Finding 2 |
| 6 | `[RemoteService(IsEnabled = false)]` across the application services | one line each | Findings 7 and 8, and Swagger returns |
| 7 | A create form and an edit form for a candidate | medium | Finding 6, `PPL-01`, `PPL-05`, Use Case 7 |
| 8 | A row link from item analysis to the question | small | `RES-07`, Use Case 12 |
| 9 | Persist `DiscriminationIndex`; reset item statistics when a key changes | medium | Finding 9, `RES-06`, `BNK-12` |
| 10 | Per-tenant sender name and address on the invitation | small–medium | Finding 11, `ASG-03`, `BRD-03` |
| 11 | Widen `smoke-routes.js` to every client route and `probe-round-trip.js` to exams, questions, sections and settings | medium | `PLT-10`, and it would have caught 1, 2 and 14 |
| 12 | Answer inputs for hotspot, file upload and spoken answer | medium | `TAK-08`, `PLT-01` |
| 13 | Sections through delivery, grading and reporting | large | Use Cases 5 and 16, six stories |

Items 1 through 6 are, together, a few days, and they remove every finding on
this page that consists of the software telling somebody something untrue. That
is the right thing to buy first, because a dead control disappoints once and a
false statement gets acted on.

---

## 11. What I could not determine, and why

Recorded so nobody re-derives it and concludes it was checked.

1. **Whether an invitation is actually deliverable.** SMTP points at
   `127.0.0.1:25` and DEBUG builds install a null sender, so no message has ever
   left this deployment. `InvitationEmail` is a pure function with ten tests over
   its output, so *what would be sent* is verified; *that it arrives* is not
   testable without a relay. The From-line finding is from configuration and the
   absent `from` argument, not from a received message.
2. **Accessibility.** `PLT-03` claims WCAG 2.1 AA. There is no axe, no
   `@axe-core/playwright`, no pa11y and no accessibility assertion anywhere in
   the repository. The implementation reads as careful — real radios and
   checkboxes, arrow-button ordering rather than drag-and-drop, a polite live
   region on the countdown — and I could not verify a single claim of it. This is
   unchanged and it is a procurement question in the public sector.
3. **Whether the section domain rule is correct.** The rule that fails an attempt
   on a section minimum is written and unit-tested and called by nothing, so I
   could confirm it exists and not that it would behave correctly once wired.
4. **The exact behaviour of `/api/app/*` for writes.** I confirmed the read
   surface answers and honours permissions. I did not attempt a write through it,
   because a successful write would have modified the shared database that other
   engineers are using. The reasoning that writes are equally guarded is sound —
   ABP carries the service's own attributes onto the conventional controller, and
   the read side proves that mechanism is active — but it is reasoning, not a
   measurement.
5. **Whether `BPR-05` still fails silently.** The blueprint editor now shows a
   per-rule match count and marks an unfillable rule on its row, which is most of
   the story. Whether the publish-check blocker names the specific starving rule I
   did not confirm; it is recorded as PARTIAL on the conservative reading.
6. **Load and concurrency at the fixed state.** `load-test.js` exists and found
   real defects, and I did not run it, because forty simultaneous sittings against
   a database three other people are using is not a read-only act.
7. **`CAT-04`'s two unenforced rules.** The two-level limit on the competency tree
   is documented as unenforced. I did not re-verify it and carried the doc's
   status forward.

---

*Pinned to `75b534d`, `feat/platform-foundation`, with the API and the SPA both
running and other engineers using them.*

*No source file moved while this was being written — which is a first for a
review in this repository — but four documents did:
`docs/requirements.md`, `docs/use-cases.md`, `docs/README.md` and
`docs/DeveloperGuide.md` were all rewritten in the working tree during the last
hour of it, by somebody else. Every code claim above was made against `75b534d`
and re-verified against the running server, so none of them is affected. The
document claims in §7 were re-checked against the rewritten files and adjusted;
§1 and §9 are still stated against the committed `use-cases.md` and
`user-stories.md` at `0842cc9`, because those are the versions their status
tables are pinned to and the versions people quote. The shelf life of a status
document in this repository is still measured in hours, and this is now the third
review in a row to end by saying so.*
