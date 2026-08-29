# Business review

`competitive-position.md` decided where we can win. `research-2026-08.md` corrected
four of its claims and ranked ten things worth building. Neither answers the
question that has to come next: **who writes the first cheque, and what is the
smallest thing they can be sold.**

This document does, and it is written against what the code actually contains
rather than what either of those documents assumed. Where a claim depends on
something being built, it names the file.

---

## 1. What is actually there — because it changes the recommendation

Five corrections to the shared picture, each verified by reading the code, each
of which moves the go-to-market answer.

**The taker's application does not exist.** The server side is complete —
`ExamTakingAppService` has `OpenLinkAsync`, `StartAsync`, `GetQuestionAsync`,
`SaveAnswerAsync`, `ReportSignalAsync`, `SubmitAsync` and `GetResultAsync`, with a
signed session token, a frozen per-attempt form and a timeout worker behind it.
The Angular route `angular/src/app/features/take/take.routes.ts` loads
`PlaceholderComponent`, which renders the words "screen not built yet". So does
`review/`, and so does `candidates/`. There is presently **no end-to-end
demonstration of this product** — not a partial one, none. That single fact
dominates everything below.

**Item statistics are columns, not numbers.** `Question.DifficultyIndex`,
`DiscriminationIndex`, `TimesAnswered` and `TimesServed` are declared, migrated,
mapped into `QuestionDto`, and **never written by anything**. The only
non-migration reads are the DTO projection in `QuestionAppService` and the
over-exposure warning in `ExamAppService` — and since nothing increments
`TimesServed`, that warning is unreachable code. "Psychometrics the tenant can
see" is currently a schema. It is the most credible thing we planned to show an
assessment professional, and today there is nothing to show.

**The shared bank is wired into authoring but not into delivery.**
`QuestionAppService.GetListAsync` correctly widens an exam's list to include bank
questions matching its category and level. `ExamTakingAppService.StartAsync` then
builds the form from `q.ExamId == exam.Id` only, and
`ExamAppService.CheckPublishAsync` counts the same narrow set.
`Question.IsDrawableBy(...)` — the domain method written for exactly this — is
called from nowhere. An author sees forty bank questions in the editor,
publishes, and every candidate sits a form drawn from the exam-owned subset. This
is a defect, not a missing feature, and it is small: one query change in two
places. Until it is fixed we cannot say "the bank is shared" in a meeting.

**Sections and named forms landed this week, in the domain only.** `ExamSection`
(with `TimeLimitInMinutes`, `MinimumPercentage`, `QuestionsPerForm` and
`IsQualifying`), `ExamForm` / `ExamFormQuestion`, `ExamDeliveryMode`, and an
optional `ExamSectionId` on `Question`, `QuestionGroup` and `ExamBlueprintRule`.
Entities, EF configuration, a migration and six domain tests. No application
service, no API, no UI. The model is right and nothing can use it yet.

**`QuestionGroup` has always supported a shared stimulus.** `StimulusText`,
`StimulusBlobName` and `StimulusMediaType` have been in the schema since the
platform migration, and `ExamFormBuilder.ApplyOrdering` already keeps a group's
questions together and in sequence. A reading passage with six questions, or an
audio clip with four, is a schema capability today with no authoring screen and no
delivery rendering. That is a much cheaper thing to finish than to start.

Two smaller ones worth knowing: six of the thirteen question types (`matching`,
`ordering`, `hotspot`, `fill-in-the-blank`, `code`, `scale`) have no payload
editor and fall back to raw JSON; and `angular/src/app/core/navigation.ts` links
to seven routes that are not registered, so the sidebar has dead entries.

What genuinely is finished and good: exam authoring with a publish gate that
reports every blocker and warning in one pass; thirteen graders behind a payload
abstraction, each tested against hostile input; weighted best-answer scoring with
a validator two security reviews have hardened; blueprint-driven, seed-reproducible
form assembly; a grading pipeline that routes a missing *or throwing* grader to a
human instead of silently scoring zero; hashed link tokens; and multi-tenant
isolation enforced by a reflection test. That is a real spine. It has no skin.

---

## 2. The first customer

**Sell first to the training academy — a private vocational or skills centre
running sequential levels — and lead with its placement test.**

Not the recruiter, and not the language centre. The reasoning, in the order it
matters.

### It needs the least of what is missing

A training academy's assessment is one exam, one paper, one score, one pass mark,
one level. That is precisely what `Attempt.ApplyScore(score, maxScore,
passingPercentage)` already computes, and precisely what
`BuildTopicBreakdownAsync` already decorates with a per-competency breakdown.

The language centre cannot be served without section-aware *delivery* —
`competitive-position.md` says so itself: a placement test reporting one number is
useless, because a student strong in reading and weak in listening needs a
different class from the reverse. The entity now exists; nothing renders it,
scores it or reports it. Selling to that buyer today is selling a promise on the
largest remaining piece of work.

The recruiter needs less than the language centre and more than the academy: a
comparison view they live in, a results screen, and a library. On the library we
have taken the opposite position by decision — correct strategy, hard first sale,
and now against regional Arabic-first incumbents (Evalufy, Elevatus) rather than
only against English-first ones.

### Its content problem is the smallest, and content is what kills trials

`research-2026-08.md` names the empty bank as the commonest reason a trial dies,
and it is right. The three buyers differ sharply in how badly this bites.

A recruiter has no questions and expects us to supply them — that is what
TestGorilla sells. A language centre has questions but wants them mapped to CEFR,
which is standard-setting work we have not built. **A training academy already
owns its bank**: a curriculum, a syllabus and end-of-level papers in Word files,
written by its own instructors, which it regards as its property. Our position —
your questions, your bank, your statistics, exported on request — is not a
compromise for this buyer. It is the reason they pick us over a platform whose
value is somebody else's library.

### It re-buys on a schedule, which is what our cost model needs

Recruiter volume is spiky and dies in a hiring freeze. Academy volume is an intake
calendar: the same levels, the same exams, three or four times a year, forever.
When the price is metered on attempts, predictable attempts are worth more than
large ones.

### Its sales cycle is a meeting, not a procurement

An owner-operated training centre decides in one or two conversations. No security
review, no DPA, no vendor portal. A recruiter of any size routes through HR
procurement. A university or ministry — where our installable deployment is
genuinely differentiating — is a nine-month bid against TAO Community Edition,
which is free. Keep the institutional deal in the pipeline; do not let it be first.

### It is the only one where the competitor is a Google Form

Against TestGorilla, Mettl, Evalufy and Elevatus we are a new product with no
references. Against a Google Form and a paper exam we hold a structural advantage
that took real engineering and cannot be copied by the incumbent, because the
incumbent is a form. Win where the competition is weakest, then use those logos to
enter the fights where it is not.

**What this recommendation depends on.** The academy is a small buyer. It will not
fund the company. It funds the *evidence* — a live tenant, real attempts, real
item statistics accumulating in columns that are presently empty — and that
evidence is the precondition for the language-centre and certification sales that
are worth more. Sequence, not ambition.

### Does the fixed-form model change this answer?

**No — it confirms it, and it changes the pitch.**

I expected `ExamForm` to pull towards the certification body and away from the
academy. Reading it, it does the opposite, for a reason that is about trust rather
than about features.

`DeliveryMode.DrawPerCandidate` asks a coordinator to hand the paper to an
algorithm. For someone who has set exams on paper for fifteen years, that is the
hardest thing in the product to accept — and the objection is not irrational,
because nobody reads the paper before a candidate sits it. `FixedForm` removes the
objection entirely: *you write Form 1, you read it, you approve it, then it goes
out.* Named forms are how a sceptical first customer is brought in the door, and
`RotateForms` is then the upgrade that sells itself once they trust the machine —
"the morning group takes Form 1, the afternoon Form 2, and what leaks at lunchtime
is worth nothing after it."

Two consequences worth acting on:

- **The demo changes.** My instinct before reading `ExamForm.cs` was to lead with
  "every candidate gets a different paper". Lead instead with "approve the paper,
  then rotate it", and keep per-candidate drawing for practice mode, which is
  exactly what the `DrawPerCandidate` doc comment says it is best at.
- **`TimesUsed` is exposure a coordinator can understand.** Per-item exposure needs
  psychometric literacy and, today, a job that does not exist. "This paper has been
  used three times, write a fourth" needs neither. It is the cheapest honest
  security story we have, and it works before the statistics job ships.

The one thing fixed forms genuinely change is the *second* customer. The
certification body and the small awarding body — who cannot ship a paper no human
has read — move ahead of the recruiter in the pipeline. Named forms plus a review
step plus a printable result is most of what they ask for at the entry level.

---

## 3. The smallest complete thing we could sell

Call it the **Level Exam Pack**: one training centre, its own bank, its own
cohorts, its own branding, a link per student, an approved paper, automatic marking
with a human queue for the written parts, and a pass/fail record per student per
level.

### The screens it needs

Twelve, of which two and a half exist.

| # | Screen | State today |
|---|---|---|
| 1 | Catalogue — categories, levels, topics, and the vocabulary form (`CategorySet`) | **Nothing.** No app service, no DTOs, no route |
| 2 | Exam list with status, form/bank counts, filters | **Built** (`exam-list.component`) |
| 3 | Exam editor with the publish panel | **Built** (`exam-form.component`) |
| 4 | Question list inside an exam, with bank filters | **Nothing.** The API filters correctly; nothing links to `:examId/questions/:questionId` |
| 5 | Question editor, with media attachment | **Media built** (`MediaFieldComponent`, e2e covered). Payload editors for **7 of 13 types**; the rest fall back to a raw JSON box, which §7.3 shows breaks the owner's authoring constraint |
| 5b | Stimulus groups — one passage or chart, several questions on it | **Schema built** (`QuestionGroup`, and `ExamFormBuilder` already keeps a group together); no authoring screen, no renderer |
| 5c | Import from a Google Forms export or pasted text | **Nothing**, and per §7.1 it is the cheapest large thing in this table |
| 6 | Named forms: build, review, publish, retire | **Domain only** (`ExamForm`, six domain tests) |
| 7 | Candidates and cohorts, with file import | **Nothing.** No app service either |
| 8 | Assign: exam, cohort, form, expiry, attempts, send | **Server built** (`AssignmentAppService`), no screen, no form selection |
| 9 | Links: list, state, copy, resend, revoke | **Server built** except resend, no screen |
| 10 | **The exam-taking screen** | **Server built in full**, no screen |
| 11 | Reviewer's queue and marking screen with rubric | **Server built** (`ReviewAppService`), no screen |
| 12 | Results: attempts per exam, one answer sheet, cohort roster, CSV | **Nothing.** `Results.Export` is a permission string with no code behind it |
| 13 | Branding: name, logo, colour, support address | **Entity and table only** |

### The backend work that must ship with it

- Draw the bank through `Question.IsDrawableBy` in `StartAsync` and
  `CheckPublishAsync`, and increment `TimesServed` when a question lands on a form.
  Without the first, "the bank is shared" is false; without the second, exposure
  has no denominator.
- Application and API for `ExamForm`: build from blueprint or by hand, publish
  (which freezes `MaxScore` and refuses duplicates — both already enforced in the
  entity), retire, and a delivery path that reads `ExamFormQuestion` when
  `DeliveryMode` is `FixedForm` or `RotateForms`. This is a smaller job than it
  sounds: `ExamFormBuilder` already produces exactly the list a form stores.

### What we would be promising that we cannot yet deliver

Say these out loud in the room. Each is a sentence a buyer hears as a commitment.

- **Item statistics.** Nothing computes difficulty or discrimination. Do not show a
  psychometrics screen with empty columns and call it a preview.
- **A certificate.** `TenantBranding.CertificateFooter` exists; no certificate does.
- **Per-skill profiles.** Sections are modelled — that is a real change from a week
  ago and it makes the roadmap statement honest — but nothing assembles, times,
  scores or reports them. "Modelled, not delivered" is the accurate phrase.
- **Comparability across forms.** Even with named forms, two forms are not equated.
  `research-2026-08.md` recommends saying so plainly. Put it on the pricing page,
  not in a footnote — certification buyers treat that sentence as a credential.
- **Accessibility conformance.** We have RTL screenshot tests at a phone viewport.
  We have no WCAG audit, no axe assertions, no VPAT. Do not answer a public-sector
  accessibility question with what we have.
- **Code execution.** The `code` type compares expected output as text. A buyer
  hears "code questions" and pictures a sandbox.
- **Anything called proctoring.** Integrity signals are recorded and shown to a
  human, deliberately and by argument. It is a better story than the alternative.
  It is not proctoring.

### Do sections change the smallest sellable scope?

**For the language centre, four skill scores are not a feature — they are the
purchase.** A placement test that returns 62% does not tell a coordinator which
class to put the student in, which is the only decision the test exists to
support. So yes: sections stop being optional for that buyer, and they were never
optional. That is an argument about *which customer is first*, and it is one of
the reasons the answer is not the language centre.

For the training academy the answer is different and more interesting. Two of the
four things `ExamSection` carries are academy features rather than language ones:

- `MinimumPercentage` — fail the whole exam however well the rest went. A
  vocational syllabus with a safety module needs exactly this, and "passed overall
  while failing safety" is not a pass any centre will defend.
- `IsQualifying` — an untimed gate that ends the attempt. "Have you completed
  Level 1?" is the first question a sequential-levels academy asks.

So my recommendation is a split one, and it is a design instruction rather than a
scope increase:

**Keep sections out of the first sellable slice, but build the taker and the
result with a section dimension from the first line of code.** Group the paper by
`ExamSectionId`, treat an exam with no sections as one implicit section, and carry
a per-section subtotal through `Attempt` scoring even when there is only ever one
of them. Retrofitting a section boundary into paged taker navigation, a per-section
countdown and a scored result is several times the cost of allowing for it now, and
the entity is already there to model against.

The first release then ships with one section per exam and nobody notices. The
second turns the feature on, and the language centre becomes sellable without a
rewrite of the two screens that are hardest to change.

---

## 4. Pricing

### Per seat is structurally wrong for this product

Our candidates have no accounts. `Candidate` is deliberately not an `IdentityUser`,
and that is one of the reasons the product is pleasant to use. It also destroys
per-seat pricing: the people who generate all the cost are not users, and the
people who are users number four.

Cost tracks **attempts** — grading compute at submit, one `Answer` row per
question with response text, timing and keystroke counters, one `AttemptQuestion`
row per served item — and **storage**, which is dominated by uploaded files and
audio responses rather than by rows.

### The shape

**A tenant subscription with a named attempt allowance, per-attempt overage above
it, and metered media storage above a floor.** Three deliberate details:

1. **Staff authors are counted in the tier; candidates are not.** The tier says how
   many people may write and mark. The allowance says how many sittings.
2. **Practice-mode attempts are priced at a fraction, or free.** They cost the same
   compute, and a centre that rations practice destroys the habit that makes the
   product stick. `ExamMode.Practice` is already a first-class distinction — use it
   commercially.
3. **A separate, non-metered price book for on-premise.** A ministry, university or
   bank cannot meter usage on its own hardware and will not accept usage-derived
   invoices. Annual licence, banded by institution size, plus installation. A
   different motion; do not force it into the SaaS table.

Bill in local currency and issue a local invoice. This sounds like an accounting
detail and it is a purchase blocker in the region.

### What the comparators charge

From what the two research documents record. This is not fresh pricing work and
should be refreshed before a price list is printed.

- **TestGorilla** sells assessments, not seats: up to 5 tests, 20 custom questions
  and 5 qualifying questions per assessment, and the library is the thing paid for.
- **Mercer Mettl, Questionmark, Surpass** are enterprise and quote-based, with
  consulting attached — competency framework design, Angoff studies. The consulting
  is a large share of the invoice.
- **TAO Community Edition is free**, open source, self-installable and
  QTI-certified. That is the floor for on-premise, and `research-2026-08.md` is
  right that it removes installability as a moat.
- **Evalufy** runs a freemium tier aimed at KSA, Arabic-first, with 800+ predefined
  tests.

### Where we sit

Between the freemium regional tools and the enterprise item-banking platforms, and
we should be explicit rather than drift.

We cannot price near Mettl or Questionmark: no accreditation, no reference logos,
no standard-setting workflow, and today no computed psychometrics. We must not
price near free: TAO already occupies free, and free-without-support is not what a
training centre wants — which is precisely why TAO is not winning these centres.

The defensible slot is **regional mid-market**: materially below the enterprise
platforms, clearly above freemium, sold on three things none of the comparators
combines — Arabic-first delivery a candidate can actually read, a bank the tenant
owns outright, and a vocabulary the tenant renames itself.

Two positions worth taking deliberately:

- **Export is included, never an upsell.** "Your questions, exported on request" is
  what makes the tenant-owned bank credible. Charging for the exit contradicts it.
- **The paid upgrades are artefacts, not features.** The item-health report, and
  later the standard-setting study report `research-2026-08.md` calls the artefact
  an auditor asks for. Those are worth money because somebody else demands them,
  and they cost us a template.

---

## 5. What would actually make a training centre switch

Honestly: **the pull is moderate, and the barrier is not features.**

Google Forms is free, universally known, and already does multiple choice, option
shuffling, automatic marking and a results spreadsheet. "An online exam that marks
itself" is not a reason to switch, and pitching it as one is how the meeting ends
politely.

Five things a form genuinely cannot do:

1. **You can approve the paper, and then rotate it.** With `ExamForm` this is now
   the strongest opener: write Form 1, read it, publish it, send it to the morning
   group, send Form 2 in the afternoon. A form gives everyone the same questions,
   forever, and inside one intake it is in the class WhatsApp group by Tuesday.
2. **A link per person, expiring, revocable, one attempt.** A form cannot bind a
   response to a person without a login or an honour-system name box. `ExamLink`
   with a hashed token does, and `AttemptsUsed` moves on a real start rather than
   on a validity check.
3. **Marking that is not the coordinator's Saturday.** Written, uploaded and spoken
   answers land in a queue with a rubric and a running total that recomputes on
   save. Google Forms cannot mark an essay at all; today that happens on paper, and
   that is where the coordinator's hours actually go.
4. **A passage or a recording with six questions on it.** `QuestionGroup` models
   this already. Forms can show a passage; it cannot keep six questions bound to
   it, keep them together when the paper is shuffled, or score "how well did they
   read this passage".
5. **Arabic that is not broken, and the centre's own name on the page.** A student
   receiving an English, left-aligned Google Forms link and a student receiving a
   page in Arabic carrying their centre's logo are being sold two different levels
   of institution. For a centre that sells to parents, this is not decoration.

### The honest part

The barrier is **typing in two hundred questions**. The centre's exams exist as
Word files. Every platform they might buy has this barrier, which is why they have
not bought one. Our features do not reduce it at all — *until we build an
importer*, and §7.1 shows the actual file the first customer holds is far easier
to import than I assumed when I wrote this paragraph. Read §7 before acting on the
next two.

The pull becomes strong only if we remove that barrier ourselves, and the way to
remove it is not a feature — it is **bulk import plus done-for-you onboarding**:
paste or upload a Word/Excel exam, map it, and for the first customers, type the
first two levels for them as part of the contract. That is a services line with a
cost, and it should be planned and priced rather than discovered.
`research-2026-08.md`'s R9 — drafting items from the tenant's own syllabus, into
Draft state, never Live — is the eventual mechanised version of the same move, and
it reinforces rather than contradicts the tenant-owned-bank position.

One targeting note: **sell the placement test before the level exam.** Placement is
high-volume, repeated every intake, low-stakes, and the thing a coordinator most
dislikes administering. Its bank is small and the centre can be onboarded in an
afternoon. The end-of-level exam is where the two hundred questions live, and it is
the second conversation.

---

## 6. The two unbuilt features that unlock the most revenue

### 1. The exam-taking screen, with the chain it depends on

Not a feature — the product. Every other capability in this codebase is
unreachable without it, and there is currently no way to show the system end to end
to anybody. Its server side is complete and unusually careful: a server-authoritative
deadline, a frozen per-attempt form, autosave on every answer, a projector proven
by test never to emit an answer key, and a timeout worker for the closed browser.
All of that is worth zero per year today.

It carries a dependency chain that must ship with it: candidates and cohorts,
because a link needs a person; assignment, because forty links by hand is the thing
we are replacing; and the reviewer's queue, because the written half of the exam is
where the coordinator's hours are.

### 2. Section-aware delivery and per-section results

The entity landed this week; the revenue is in the application and UI half, which
is most of the cost. It is the only remaining item that opens a *new shape of
customer* rather than completing the current one.

It converts the language centre from "cannot buy" to "can buy" — the larger and
more defensible market — and `research-2026-08.md` argues persuasively that
per-section scoring plus a reporting scale also carries the certification buyer
(scaled score with a band) and the academy (level attained). One feature, three
buyers. It is the layer the certificate and the recruiter's comparison view both
draw their content from, and everything R2, R3 and R6 propose sits on top of it.

### The one to build before either of the above's second half: named forms

`ExamForm` is the best cost-to-revenue ratio in the backlog and it did not exist
last week. `ExamFormBuilder` already produces exactly the list a form stores; the
entity already enforces the two rules that matter (no empty form, no duplicated
question) and already freezes `MaxScore`. What is missing is an app service, a
build-and-review screen, and a delivery branch on `DeliveryMode`.

For that price it does three things: it removes the first customer's biggest
objection, it opens the certification body as a second customer, and it gives us
`TimesUsed` — an exposure story that needs no psychometrics job and no
psychometrician to explain.

So: **revenue ranking** is taker chain, then sections. **Build order** is taker
chain, then named forms, then sections.

### Which are engineering wishes

Real work, real merit, no revenue this year.

- **The bank browser as its own screen.** The filtering already exists in
  `QuestionAppService.GetListAsync`. A dedicated browser matters at 500 items and
  no tenant has 40. Put the filters in the exam's question list and stop.
- **Qualifying questions as a separate feature.** They are now an `IsQualifying`
  flag on a section, which is the right design and costs almost nothing once
  sections ship. They protect a reviewer's queue in high-volume open recruiting — a
  scenario the first customer does not have. No longer a separate build; keep it
  sequenced with sections.
- **The item-health view.** Our best demo, our least urgent screen, because it has
  nothing to display until the statistics job exists. The revenue is in the job.
  The screen is the wrapper.
- **LOFT, modified-Angoff standard setting, anchor-item drift** (R1, R2, R6). All
  correctly identified as strategy, and all second-sale features. None closes a
  customer who cannot yet sit an exam. `research-2026-08.md`'s open question 4 — is
  there a named tenant who would pay for a documented Angoff study — is the right
  gate for R2, and the answer today is no.
- **QTI import.** Genuinely the highest-leverage integration available to us, aimed
  at a buyer with 4,000 items in Surpass. That is an institution, not a training
  centre. It belongs to the second customer — and §7.1 argues that a far cruder
  importer, aimed at the Google Forms export the first customer actually holds,
  beats it on cost per closed deal by roughly an order of magnitude. Build the
  crude one first.

---

## 7. The real exam, and the three things it changes

The product owner sat a trading course and sent the exam. It is worth more than
any competitor page in this document, because it is not a hypothesis about the
first customer — it is what the first customer is doing today.

`الاختبار.docx`, unpacked and counted:

- A **Google Forms export**, in Arabic throughout, with a "back / submit / clear
  form" footer still on the last page.
- **Thirty questions, all single choice**, two to five options each — 86 options
  in total — every one marked required.
- **The answer key is in the file.** Exactly one option per question carries a ✅.
  Thirty questions, thirty keys, no ambiguity anywhere.
- **`word/media/` does not exist.** The archive contains no images at all, and
  several questions need one: question 28 asks which statement does *not* apply
  to the green candle, and question 30 asks what an impulse wave is. The chart
  they refer to is gone.

Three conclusions, in order of how much money is attached to them.

### 7.1 Migration is a feature, and this shape is the cheapest one to import

**Yes — this is the cheapest real switching cost we can remove, and it is roughly
a tenth of the cost of QTI import.**

§5 says the barrier is typing in two hundred questions and that no feature of ours
reduces it. This file changes that answer, because it is far more machine-readable
than it has any right to be. Numbered prompts, unindented option lines, an
unmistakable key marker, one key per question. A parser for this shape is a
regular expression over paragraphs and a preview screen — days, not a quarter.

Weigh that against QTI, which `research-2026-08.md` rightly calls the highest-
leverage integration available. QTI attacks the lock-in of an institution with
4,000 items in Surpass or TAO. That institution is our *third* customer. This
importer attacks the lock-in of the customer §2 recommends selling to *first*,
and costs a fraction. **Do this one first. Keep QTI for the institutional sale.**

Three honest qualifications:

- **The most valuable input is "paste a block of text", not any file format.** A
  coordinator can paste from a Word file, a PDF, an email or a WhatsApp message. A
  `.docx` uploader serves one of those. Build the paste box, then add uploaders.
- **Imported questions must land in a state an author confirms, one by one.** An
  importer that silently guesses a key is *worse* than typing, because a wrong key
  is invisible until candidates complain and then it is invisible in every form
  the bank feeds. Import to draft; require a confirm.
- **It cannot recover the images, and it must say so.** They are not in the file.
  The import has to detect a question whose text refers to something not present
  and tell the author which ones need a chart attaching. Which leads directly to:

### 7.2 The lost images are a sales argument — for this customer specifically

**Use it, and do not build the deck around it.**

It is a demonstrable failure of the incumbent, shown in the customer's own file,
in the room, without a slide: *your question asks about a candle, and there is no
candle.* That is worth more than any feature claim, because the coordinator
already knows it — they have been working around it by sending charts separately
or by quietly dropping chart questions from the exam.

Be honest about its reach. It matters to exams whose questions **are** a picture or
a sound: trading charts, listening comprehension, anatomy, engineering diagrams,
clinical images. For a purely textual exam it is worth nothing. Our first customer
happens to be precisely the kind that suffers, which is a reason to lead with it in
*that* meeting rather than a reason to make it the company's positioning.

The sharper version of the argument is not per-question media at all — it is
`QuestionGroup`. Google Forms cannot bind four questions to one chart, or six to
one recording. It has no concept of a stimulus. We have had one in the schema since
the platform migration, and it needs an authoring screen and a delivery renderer
rather than a design. That is the demo that beats a form outright, and §1 already
notes it is cheaper to finish than to start.

Per-question media itself is **built**: `AssessmentMediaAppService` on the server,
`MediaFieldComponent` in the question builder, drag-or-click with a live preview
for image, audio and video, and end-to-end coverage in `question-form.spec.ts`.

### 7.3 The no-programming constraint has one live violation, today

The owner's constraint — *no input anywhere may require programming skill, to
write a question or to answer one* — is the right constraint, and the product
mostly honours it: the prompt is a formatting editor rather than an HTML box,
media is a drop target rather than a URL field, and nothing in any payload shape
is a regular expression. `FillInTheBlankPayload.AcceptedAnswers` is a list of
strings, which is exactly right.

It is violated in one place, and the violation ships today. `PAYLOAD_EDITORS` in
`angular/src/app/features/questions/payload/payload-editor.ts` registers editors
for seven types. The other six — `matching`, `ordering`, `hotspot`,
`fill-in-the-blank`, `code`, `scale` — fall through to a **raw JSON textarea** in
`question-form.component.html`. The comment there argues, correctly, that a type
with no editor should still save rather than be refused. That argument is right
for a *tenant-specific* type the build has never heard of. It is wrong for six
types we ship, document and grade.

Four more places where the obvious implementation would violate the constraint,
flagged now so they are designed rather than discovered:

- **Hotspot.** `HotspotRegion` is `X`, `Y`, `Width`, `Height` as decimals. Four
  number fields is coordinate arithmetic. It has to be a rectangle drawn on the
  image.
- **Fill in the blank.** Every implementation of this in the wild asks the author
  to type a placeholder — `{{1}}`, `___`, `[blank]`. That is a syntax to learn.
  It has to be: select a word in the prompt, press a button.
- **Ordering.** `OrderingItem.CorrectPosition` is an integer. Typing 1..5 into
  boxes is a spreadsheet. It has to be drag to reorder.
- **Catalogue codes.** `Category`, `Level` and `Topic` each carry a `Code`
  described as a "stable machine key". Asking a teacher to invent a machine key
  is a small violation that accumulates. Generate it from the name; let it be
  edited; never require it.

One legitimate exception, worth stating so it is not mistaken for a breach: the
`code` question type asks the author to write code. That is the *subject matter*,
not the *tool*. The constraint is that the product never makes an author learn
syntax in order to operate the product — not that a programming exam can avoid
programming.

The backlog states this as a testable rule rather than an aspiration: a test
enumerates `QuestionTypes` and asserts every shipped type resolves to a registered
editor, and that the raw-payload field renders for none of them.

### 7.4 On the sibling product

I could not read it. `github.com/jalakhras/Quizbee` returns 404 from both the web
interface and the API, and it does not appear among the sixteen public repositories
on that account, so it is private or has been renamed. `gh` is not installed in
this environment and WebFetch cannot authenticate. Nothing in this document
reflects it. If it is opened up, or a zip is dropped somewhere readable, it is
worth a second pass — an earlier attempt at the same idea usually holds the
pricing and audience thinking that a rewrite forgets.

---

## 8. What this implies for the next quarter

In order, and the order is the argument.

1. Fix the bank draw and start counting exposure. Two queries and a counter.
2. Build the taking screen, then candidates and cohorts, then assignment and links,
   then the reviewer's queue. The shortest path to one complete demonstration.
   Build all of it section-aware, with one implicit section, per §3.
3. Build the catalogue screens, or nobody but us can configure any of the above.
4. Application, API and screens for `ExamForm`, and the `DeliveryMode` branch in
   delivery. Cheap, and it is what the first customer will ask for in the meeting.
5. Build the branding screen. Small, and it is what stops the invitation email
   reading as phishing.
6. **Import.** A paste box and a `.docx` uploader for the Google Forms shape, into
   draft questions an author confirms one at a time, reporting every question
   whose media did not survive. Days of work against the largest barrier we have,
   per §7.1 — and it can be built in parallel with anything above it.
7. Authoring screens for `QuestionGroup` — one passage or recording, several
   questions bound to it — and its renderer in the taker. Schema already there.
8. Payload editors for the remaining six question types, closing the raw-JSON
   violation in §7.3.
9. Write the item-statistics job, so the columns stop being a promise.
10. Then turn sections on — assembly, timing, scoring, reporting — and sell the
    language centre.

Everything in `research-2026-08.md`'s ranked list stays ranked. It starts after
step ten, and QTI import in particular comes after step six rather than instead
of it.

---

## 9. Answers to `research-2026-08.md`'s open questions, where the code settles them

Three of the seven can now be closed from the repository rather than argued.

**Q2 — do we persist per-item responses and per-item elapsed time?** *Yes.*
`Answer` carries `Response`, `TimeSpentSeconds`, `WasPasted`, `KeystrokeCount`,
`BackspaceCount` and `AnsweredAt` per question per attempt, and
`AttemptQuestion` records which item was served in which position with which
option order. R1, R4 and R5 have the data they need. The gap is that nothing
aggregates it.

**Q3 — what happens when an author edits a live item that already has statistics?**
*The statistics carry over, and they are all null anyway.* Both halves matter: the
correctness bug R4 predicts is real and latent, and it is currently invisible
because no statistics exist to be corrupted. Fix the versioning rule before the
statistics job ships, not after — otherwise the first numbers we ever compute are
already wrong.

**Q4 — is there a named tenant who would pay for a documented Angoff study?** Not
in the pipeline this document recommends. Build R2's model if it is cheap; do not
build the report generator until somebody asks for it by name.
