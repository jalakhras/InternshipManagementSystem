# 📌 Use Cases | حالات الاستخدام

The previous version of this document described an internship management system
with HR managers, supervisors, training plans and trainee self-registration. None
of that was ever built and none of it is what the product is. **Astrolabe is an
Arabic-first, domain-agnostic online assessment platform**: a training centre,
language school or academy writes its own questions, approves the paper, sends it
to a class, and reads what came back.

These are the journeys a real person completes, or fails to complete, in the
product as it stands today.

## How to read this | كيف تُقرأ هذه الوثيقة

Each case carries one status, claimed against the code rather than against intent.
Verified by opening the files: the Angular component, its route, the service
method, the controller route attribute, and the application service behind it.

**Pinned to `75b534d`** (this revision; the previous one was pinned to `0842cc9`).
Twenty-one commits landed between them and closed eight of the breaks recorded
here: the staff password that answered 200 and changed nothing, the unbranded
invitation, the missing resend, the missing expiry extension, the attempt monitor,
the five seeded roles, the permission that enforced nothing, the item-analysis
statistics that libelled correctly-keyed questions, and a fill-in-the-blank answer
that scored zero however right it was. Where that happened it is said, because
*how* those defects survived is more useful than the fact that they are gone. **A
capability that had no case at all is now Use Case 17**: importing a question bank
from a spreadsheet.

| Status | Means | بالعربية |
|---|---|---|
| **BUILT** | A person completes this today, end to end, from the browser | مكتملة |
| **PARTIAL** | Part of it works and the journey stops somewhere; the step it stops at is named | جزئية |
| **NOT BUILT** | The journey cannot be started | غير منفَّذة |

**A service is not a use case.** This project has four times shipped a finished,
tested application service with no controller and no screen, and each time it read
as complete in an inventory that counted services. A case is BUILT only when a
person can walk it in a browser. Where a screen exists but the machine behind it
does nothing, the case is PARTIAL and the dead step is named.

**The actors are the tenant's words.** A training coordinator, a teacher, a
reviewer, a candidate, an administrator. The product intends to let a tenant
rename these; today it can save its own words and no screen reads them.

**Every case is traceable.** Each carries a **Screen** — the route a person is
standing on — and a **Role**, one of the five seeded roles (`Admin`,
`Coordinator`, `Author`, `Marker`, `Observer`) whose permissions are set out in
`business/roles.md`. The candidate is deliberately not a role: they have no
account, and their link is their entire credential.

كلّ حالة هنا لها **شاشة** (المسار الذي يقف عليه الإنسان) و**دور** من الأدوار
الخمسة المبذورة. والممتحَن ليس دوراً ولا حساباً — رابطه هو بطاقته.

Story identifiers in brackets — `RES-01`, `PLT-09` — point at `user-stories.md`,
where the acceptance criteria, the test plan and the full traceability matrix
live.

---

## 🎯 Use Case 1: Set up the catalogue | إعداد الكتالوج
**Status: BUILT** · `CAT-02`, `CAT-03`, `CAT-04`

**Screen:** `/catalog`
**Role:** `Author` (`Catalog.Manage`) · `Coordinator` and `Observer` read it through
the pickers on other screens
**Actors:** Training coordinator, Administrator
**Preconditions:** A tenant exists, created by us out of band.
**Description:** The centre describes what it teaches — its domains, the levels
within each, and the competencies its questions measure — so that everything else
in the product has somewhere to be filed.

**Flow:**
1. Coordinator opens **Catalogue**.
2. Creates a category (a track, a language, a job role). The code is generated from
   the name and is never demanded.
3. Adds levels under it, or marks a level as applying to every category.
4. Builds the competency tree — Listening, Reading, Grammar — as topics under the
   category.
5. Everything else in the product now offers these: the exam editor, the question
   form, the class editor.

**Why this one comes first.** Until it shipped, every exam and every question had a
null category, which silently disabled four advertised capabilities at once — the
shared question bank, the competency breakdown on a result, blueprints keyed on
competency, and a class sitting at a level. One missing CRUD screen was turning
four features into no-ops.

**Known gaps:** A new tenant starts with an empty catalogue and must build a
taxonomy before writing its first question (`CAT-06`). The two-level limit on the
competency tree is documented and not enforced. Levels and topics show no usage
count, so deactivating one is blind (`CAT-05`).

---

## 🎯 Use Case 2: Write a question the centre owns | كتابة سؤال يملكه المركز
**Status: BUILT** · `BNK-01`, `BNK-02`, `BNK-05`, `BNK-06`

**Screen:** `/questions` · `/questions/new` · `/questions/:questionId`
**Role:** `Author` (`Questions.Create`, `.Edit`)
**Actors:** Teacher
**Preconditions:** A category exists (Use Case 1).
**Description:** A teacher writes a question that belongs to a domain and level
rather than to one exam, so that three papers for A1 cost three recipes instead of
three copies of forty questions.

**Flow:**
1. Teacher opens **Question bank → New question**.
2. Chooses a type from the thirteen the product ships. Every one has its own
   editor; no raw JSON box appears for any of them.
3. Writes the prompt with a formatting toolbar. No markup is typed, and the
   sanitised value is what is stored.
4. Files the question under a category, a level and a competency — leaving the exam
   blank, which is what makes it a bank question.
5. Optionally switches on weighted scoring, so that a defensible-but-not-best
   answer earns part of the marks.
6. Saves. Anything that cannot be graded is refused with a sentence, in Arabic or
   English, beside the control that caused it.

**The rule this obeys:** no input anywhere may require programming skill. Ordering
is set by arrow buttons, matching by adjacent rows, hotspot regions by drawing on
the image, blanks by selecting a word. The authoring half of that rule is kept.

**Known gaps:** No topic filter on the bank list, so a teacher cannot ask for "my
listening questions" (`BNK-09`). No duplicate action (`BNK-10`). A question has no
draft/approved/retired lifecycle — only an on/off flag (`BNK-11`).

---

## 🎯 Use Case 3: Attach a chart, a recording or a clip | إرفاق صورة أو تسجيل بالسؤال
**Status: BUILT — and it was broken until `3923129`, which is the interesting part** · `BNK-04`, `PLT-09`

**Screen:** the question editor's media field · `/exams/:examId/structure` for a
passage · the candidate's `/exam/:token/sitting`
**Role:** `Author` (`Questions.Edit` carries media upload and deletion) · the
candidate reads the file with a signed grant and no role at all
**Actors:** Teacher, Candidate
**Preconditions:** A question exists.
**Description:** A question about a candlestick chart should show the chart; a
listening question should play the clip.

**Flow:**
1. Teacher drags a file onto the media field, or clicks to pick one. No URL is
   typed.
2. The file uploads. Oversized files and disallowed types are refused by name and
   limit.
3. The preview renders in place.
4. The candidate sitting the exam sees the image or hears the clip — fetched with a
   signed grant that names one blob and expires with their attempt, so somebody
   with no account gets exactly their own paper's media and nothing else.

**Recorded because of how it broke, not because it is broken.** Until last week
every URL the product handed the browser to fetch for *itself* — an `img`, an
`audio`, a `video`, a download link — was built origin-relative. The app runs on
one origin and the API on another in both environment files, with no proxy, so the
browser asked the wrong server. Staff had a second, independent failure: a browser
will not attach a bearer token to an `img src` however much the page would like it
to, so an author's preview was an anonymous request against a permission check and
came back 404.

**Seven symptoms, one cause:** the author's preview, the candidate's question media
and passage, the exam entry page's logo, the staff shell's logo, the hotspot
editor's image, the reviewer's link to an uploaded answer, and the results export.

**Why it survived two reviews and 187 passing tests.** The browser test stubbed
this exact URL, so it asserted our own mock was reachable; the live backend test
fetched the blob with an API client carrying a token, which no `<img>` tag can do.
Both sides passed and neither crossed the seam. The fix serves the two callers
differently — the candidate's paper already carries its grant and needed only the
right origin; staff files are fetched with the token and handed to the page as
object URLs — and the live suite now covers the round trip, including that an
anonymous stranger holding a blob name gets 404 rather than the file.

---

## 🎯 Use Case 4: Build an exam and publish it | بناء اختبار ونشره
**Status: BUILT** · `EXM-01`, `EXM-03`, `EXM-04`

**Screen:** `/exams` · `/exams/new` · `/exams/:id`
**Role:** `Author` (`Exams.Create`, `.Edit`, `.Publish`) — publishing is
deliberately the author's, not the coordinator's: it is a statement that the paper
is finished, which is an authoring judgement
**Actors:** Training coordinator
**Preconditions:** A category and level exist; questions exist.
**Description:** An exam is created, given a time limit and a pass mark, and cannot
be published until everything wrong with it has been fixed.

**Flow:**
1. Coordinator creates an exam and sets its title, domain, level, time limit,
   passing percentage and mode — assessment or practice.
2. Opens the publish panel. Every blocker is listed at once — no questions, a form
   longer than the bank, an unfillable recipe — rather than one refused click at a
   time.
3. Warnings appear separately and never block: no competencies (the result will be
   a bare number), a practice exam without explanations, a bank too small to
   rotate, an over-exposed bank.
4. Fixes them, publishes. The exam becomes assignable.
5. Later, archives it. It stops being assignable; attempts already sat on it stay
   readable.

**Known gaps:** The exam list filters on title and status; the category and level
filters exist on the server and have no control. The availability-window fields
exist, are enforced at delivery, and have no input on the form, so a coordinator
cannot open a window (`EXM-12`).

---

## 🎯 Use Case 5: Lay an exam out in sections and passages | تقسيم الاختبار إلى أقسام وقطع
**Status: PARTIAL — authoring is complete; delivery ignores it entirely** · `EXM-06`, `EXM-07`, `EXM-08`, `TAK-09`, `RES-04`, `BNK-08`

**Screen:** `/exams/:examId/structure`
**Role:** `Author` (`Exams.Edit` — the structure service is guarded by exam
permissions, not question ones)
**Actors:** Teacher
**Preconditions:** An exam exists.
**Description:** A four-skills paper is divided into Listening, Reading, Grammar
and Writing; a reading passage or an audio clip carries six questions between them.

**Flow:**
1. Teacher opens **Exam → Structure**.
2. Creates sections, names them, orders them, sets a time limit and a minimum
   percentage on each, and marks one as a qualifying gate. **✅ All of this saves.**
3. Creates a passage — instructions plus a text, image, audio or video stimulus —
   and binds several questions to it from the question form. **✅ This works, and
   the candidate's screen renders the passage above its questions.**
4. The candidate should move through named sections with its own clock. **❌ There
   is no section anywhere in the delivery path.**
5. The result should report a score per section. **❌ Grading computes one flat
   total.**

**Where it breaks, exactly.** `AttemptQuestion` carries no section id; the form
builder records none; the taker has no notion of a section; and grading never calls
the domain rule that fails an attempt on a section minimum — which is written and
unit-tested and invoked by nothing.

**The part that is worse than missing.** A coordinator can set "Listening: 20
minutes" and a qualifying gate, see them saved, and every candidate will receive
the whole exam's clock and no gate. The software makes a promise in writing that
it does not keep. Until the delivery half lands, those two controls should be
hidden rather than shown.

**What it costs commercially.** This is the placement-test story. A result that
says 62% and nothing else cannot tell a coordinator which class to put a student
in. The competency breakdown (Use Case 11) now answers most of that question and
should be sold as the profile until sections land.

---

## 🎯 Use Case 6: Approve the exact paper before it goes out | اعتماد الورقة قبل إرسالها
**Status: BUILT** · `FRM-01`, `FRM-02`, `FRM-03`, `FRM-04`, `ASG-09`

**Screen:** `/exams/:examId/forms` (build and publish) · `/assignments/:examId`
(choose the paper, or rotate)
**Role:** `Author` builds and publishes the form (`Exams.Edit`, `.Publish`);
`Coordinator` chooses it at send time (`Assignments.Create`) without being able to
read the questions on it
**Actors:** Training coordinator, Reviewer
**Preconditions:** A published exam with questions.
**Description:** Rather than trusting a random draw, the centre builds "Form 1" as
a fixed list of questions, a human reads it, and that is the paper the class sits.

**Flow:**
1. Coordinator opens **Exam → Blueprint** and writes the recipe — "six grammar,
   four listening, two of them hard" — with each rule showing how many bank
   questions actually match it, and a rule the bank cannot fill marked on its row.
   That matters on screen rather than later: the builder contributes what it can
   and never fails, so an unfillable blueprint produces a short paper silently and
   nobody finds out until a candidate has sat it.
2. Opens **Exam → Papers** and creates a form with a name and a code.
3. Either hand-picks questions from the exam's drawable bank and orders them, or
   generates one from the blueprint — and the form records which it was. *(Until
   `0842cc9` the papers screen offered "fill from the blueprint" as the recommended
   route, there was nothing to fill from, and no way to say so.)*
4. A reviewer reads it and publishes it. The maximum score freezes; the question
   list becomes immutable; an empty or duplicated form is refused.
5. When sending the exam, the coordinator picks that published form.
6. Every candidate on that assignment sits exactly it, in its order, with its
   frozen marks.
7. Later, the form shows how many times it has been sat, and can be retired without
   breaking the results already sat on it.

**This is the product's best single answer to a sceptical coordinator**, and it was
worth nothing to a customer for two increments: the authoring API shipped first
and moved nobody, because delivery was the half that mattered. It became real when
the delivery branch and the picker landed together.

**One defect worth recording because of how it was avoided.** The named-form
delivery path originally built the candidate's paper by hand and omitted the
recorded option order — which handed the candidate the answer key to every matching
and ordering question, since with no shuffle recorded the right-hand column pairs
with the left in order. It now delegates to the same builder the random path uses,
so both share one implementation. That removes the class of defect rather than the
instance.

**Known gaps:** No rotation across forms for one class — three papers means three
assignments by hand (`FRM-05`). No guarantee a resit differs (`FRM-06`). No
printable paper or answer key (`FRM-08`).

---

## 🎯 Use Case 7: Bring in a class and put it at a level | إدخال الشعبة وربطها بمستوى
**Status: PARTIAL — import works; a person cannot be added or corrected by hand** · `IMP-05`, `PPL-01`, `PPL-02`, `PPL-05`, `PPL-07`

**Screen:** `/candidates` (paste import) · `/groups` (the class and its roll)
**Role:** `Coordinator` (`Candidates.*`, `Groups.*`)
**Actors:** Training coordinator
**Preconditions:** A category and level exist.
**Description:** Forty students are brought in from a spreadsheet and organised
into a class (شعبة) that sits at a level and runs between two dates.

**Flow:**
1. Coordinator opens **Candidates → Import** and pastes the roll, comma- or
   tab-separated.
2. Presses **Check**. Nothing is written. The preview reports counts, names every
   bad line with its number, and flags duplicates. **✅**
3. Presses **Confirm**. The valid rows import; re-running the same list changes
   nothing. **✅**
4. Opens **Classes**, creates a class, chooses a category and then a level within
   it, sets the term dates, and edits the whole roll in one save. **✅**
5. Corrects a misspelt name. **❌ There is no edit form.**
6. Adds one late student by hand. **❌ There is no create form.**

**Where it breaks, exactly.** `CandidateAppService.CreateAsync` and `UpdateAsync`
both exist, both have routes, and the Angular service has methods for both. Nothing
calls them: both primary buttons on the candidates screen open the import panel,
and the row renders only a delete action. The component even declares a
`canEdit` permission signal and never references it. Paste import is the only door
into the system, and there is no way back out of a typo except deleting the person
— which loses their attempts.

**The other constraint is commercial, not technical.** Every person needs a unique
email address. A vocational academy where siblings share a family address, or where
under-16s have none, cannot enter its roll at all — by import or by hand.

---

## 🎯 Use Case 8: Send an exam to a class | إرسال الاختبار إلى شعبة
**Status: BUILT** · `ASG-02`, `ASG-03`, `ASG-04`, `ASG-05`, `ASG-06`, `ASG-07`, `ASG-08`

**Screen:** `/assignments` (which exam) → `/assignments/:examId` (recipients, paper,
expiry, attempts, links) · `/results/running` while it is being sat
**Role:** `Coordinator` (`Assignments.Create`, `.Revoke`, `.SendEmail`,
`Attempts.View`, `.ForceSubmit`)
**Actors:** Training coordinator, Candidate
**Preconditions:** A published exam and a class with members.
**Description:** Forty links are created in one action, one per person, each
individually revocable — and nobody needs an account.

**Flow:**
1. From the exam list, the coordinator opens **Assign** for that exam.
2. Chooses the class, an expiry, how many attempts are allowed, and — when
   published papers exist — which one.
3. Sends. One link per member is created, its token hashed at rest, and the
   invitation emailed.
4. If one address bounces, the rest still send; the failure is reported with the
   address, and that person's link stays usable and copyable from the panel.
5. The link table shows, per person: the token prefix, expiry, attempts used
   against allowed, whether the email sent, whether it has been opened, and whether
   it is revoked.
6. A link sent to the wrong person is revoked and reports itself as revoked rather
   than as invalid.

7. Before sending, the screen shows **who will receive it** — names and addresses,
   not a count — and refuses an empty class rather than sending to nobody in
   silence.
8. A student who deleted the email gets a **reissued** link: a new address that
   kills the old one and buys no extra attempt. A student who was ill gets the
   expiry **extended**, forwards only — pulling it backwards would end a sitting
   under somebody in the middle of it, and closing early is what revoke is for.
9. While the exam is running, the coordinator watches it: **Results → In progress**
   (`/results/running`) lists the sittings under way and refreshes itself quietly.
   One that is stuck can be ended — everything answered so far counts in full, and
   the reason is recorded in the coordinator's own words on the attempt, because
   "the system did it" is not an answer anybody can defend weeks later. A sitting
   that should never have started can be discarded; a **graded** attempt cannot,
   because that is somebody's result and removing it is a disappearance rather than
   a correction.

**The invitation is now the centre's.** It carries the organisation's name in the
subject and in both language bodies, and a start button in the organisation's
colour. A tenant that has not named itself gets a sentence that reads correctly
with no name rather than a placeholder standing in for one. The colour is validated
before it reaches a `style` attribute, and names and titles are escaped before
being put into HTML — this is the one message that reaches a person with no account
and no prior relationship with us, and a single stray angle bracket was enough to
rebuild the message around itself. **The logo is deliberately not included**: it is
served behind a signed grant that a mail client does not carry, so it would arrive
as a broken image, which is worse for trust than no image (`BRD-03`).

**Known gaps.** Sending to one person still means creating a class of one
(`ASG-01`). And SMTP points at a local address with no credentials, so on a
deployment without a mail relay no invitation is delivered — the links are still
created and still copyable by hand.

---

## 🎯 Use Case 9: A candidate sits the exam | جلوس الممتحن للاختبار
**Status: BUILT** · `TAK-01` through `TAK-07`, `TAK-11`, `TAK-12`, `GRD-10`

**Screen:** `/exam/:token` → `/exam/:token/sitting` → `/exam/:token/result` —
outside the shell and **outside authentication**
**Role:** none, and that is the design. The candidate has no account, no password
and no code to type: the link is their entire credential, exchanged once for a
signed session token held in memory and sent as `X-Exam-Session`.
**Actors:** Candidate
**Preconditions:** A valid link.
**Description:** Someone with no account, on a phone, in Arabic, sits a timed exam
and cannot lose their work.

**Flow:**
1. Candidate opens their link. They see the exam's name, length, question count and
   attempts remaining, and the organisation's name. The clock has not started, and
   opening the link does not consume an attempt.
2. Presses **Start**. One question is fetched at a time — the whole paper is never
   in the browser.
3. Answers. Every answer autosaves after a short pause and again before navigating.
   A save that fails is re-queued and shown, not dropped.
4. The countdown comes from the server with every save, so a wrong device clock
   changes nothing. If the connection drops and they return, they resume the same
   paper, in the same order, against the same deadline.
5. At zero, the browser submits; if the browser has gone, a background worker does,
   and the two are recorded distinctly.
6. Submits after a confirmation naming how many questions are unanswered.
7. Sees either a score with a competency breakdown, or a clear statement that a
   person has to mark it and there is no score yet — never a provisional number.
8. In practice mode, sees the right answer and the explanation.

**What never crosses the wire:** no `isCorrect`, no weight, no reviewer guidance,
no explanation before submission in assessment mode. The projector copies an
explicit field list, so a new payload field is invisible to a candidate unless
somebody adds it deliberately — and the tests assert on the serialised response
rather than on the object, because the object is not what leaks.

**Three defects remain in this journey and one of them costs marks:**

- **A fill-in-the-blank answer is always scored zero** (`GRD-10`). The type is
  wired to a plain textarea that emits a bare string; the grader parses a map of
  blank id to answer, fails to read it, and returns *wrong* rather than routing it
  to a person. The only way to score on that type is to type JSON into an exam,
  which breaks the product's founding rule at the worst possible place.
- **Three types cannot be answered at all** — hotspot, file upload and spoken
  answer all fall back to a textarea (`TAK-08`).
- **Every integrity signal is mislabelled** (`TAK-13`). The browser posts one
  shape and the server expects another; nothing binds, so the type defaults to its
  first enum value and every window-blur is recorded as a paste. The candidate is
  also never told they are observed, and the tenant's switch to turn observation
  off is not consulted.

**Accessibility** is good by construction and unverified: real radios and
checkboxes, keyboard-operable ordering by arrow buttons rather than drag-and-drop,
a polite live region on the countdown. There is no automated check anywhere in the
repository (`PLT-03`), and no language switch on the taker (`TAK-14`).

---

## 🎯 Use Case 10: Mark what a person has to mark | تصحيح ما يحتاج إلى مصحِّح
**Status: BUILT, with two sharp gaps** · `GRD-01` to `GRD-06`

**Screen:** `/review` (the queue) → `/review/:attemptId` (award marks)
**Role:** `Marker` — the smallest role in the product, four permissions and one
sidebar item. A marker cannot list results, cannot see the roster, cannot see a
candidate record, and cannot see an exam or a question outside the attempt in front
of them.
**Actors:** Reviewer
**Preconditions:** A submitted attempt containing at least one written answer.
**Description:** Objective questions are marked the instant the exam is submitted;
what needs a human waits in a queue.

**Flow:**
1. Everything a machine can mark is marked at submission, in one pass. A grader
   that is missing or that throws sends the answer to a person rather than scoring
   it zero, and never rolls back the submission.
2. Reviewer opens **Review queue** — only attempts genuinely waiting on a person,
   oldest first.
3. Opens one. Sees each pending answer with its rubric criteria and maximum marks.
4. Scores each criterion and comments. The awarded total is re-checked against the
   question's marks server-side, not trusted from the browser.
5. Saves. The attempt's total recomputes and the row leaves the queue.
6. With the separate integrity permission, reads the observations beside the answer
   — with an explicit statement that these are observations, not conclusions, and
   no action that acts on them.

**Two gaps a working marker hits on day one:**

- **The model answer is not shown.** The server renders the correct answer and the
  explanation for every type, puts both on the DTO, and the Angular model types
  both. The marking screen binds neither, in 164 lines of template. The renderer,
  the transport and the client type are all finished; the whole cost of this is one
  template binding (`GRD-05`).
- **A mark cannot be changed.** Marking clears the pending flag, and both the queue
  and the answers endpoint filter on that flag — so reopening a marked attempt
  returns an empty list and a blank screen. The component contains a step commented
  "so a reopened attempt shows its marks" that can never run. A marker who mistypes
  a score has no route back (`GRD-07`).

**And the observations are narrower than the enum implies.** Six signal types are
defined and given plain-language sentences in the report — paste, window blur,
implausible speed, no corrections, developer tools, page reloaded — and the exam
screen reports **two** of them. The other four have a name, a translation and a
sentence, and are never produced. A marker reading "what was observed" is reading
paste and tab-switching, and nothing else.

**A third gap, unrelated to marking but paid for here.** A `scale` question is
recorded and scored **zero**, and is never routed to a marker: its grader returns a
settled zero rather than manual review. That is defensible for a confidence rating
that is meant to be read rather than counted, and it is not defensible on a paper
with a total — so it should be said before one is put on such a paper.

---

## 🎯 Use Case 11: Read the results and get them out | قراءة النتائج وتصديرها
**Status: BUILT — the export was fixed in `3923129`** · `RES-01`, `RES-02`, `RES-03`, `RES-05`, `RES-12`

**Screen:** `/results` (roster and summary) → `/results/:attemptId` (one answer
sheet with its competency breakdown)
**Role:** `Coordinator` and `Observer` (`Results.View`, `.Export`). Neither sees an
integrity flag count unless they also hold `Review.ViewIntegritySignals`, which
neither does — the roster zeroes it rather than omitting the column.
**Actors:** Training coordinator
**Preconditions:** Attempts have been sat.
**Description:** The coordinator who bought the product finds out what happened.

**Flow:**
1. Coordinator opens **Results**, filtered to an exam, a class, a paper or a state.
2. Reads the headline figures for the whole filtered set: how many sat, passed,
   failed, are awaiting marking, never started, and the mean and median percentage.
   **✅**
3. Reads the roster: each person, the paper they sat, how long they took, their
   score and percentage, pass or fail, and how many integrity flags. **✅**
4. Opens one row and reads that candidate's whole paper — every question in the
   order it was served, their answer, the mark, the reviewer's comment — reflecting
   the paper as served even if the bank has been edited since. **✅**
5. Reads the competency breakdown: listening 40%, reading 85%, rather than 62%.
6. Presses **Export** and gets a CSV.

**This closes the sharpest break in the product.** Until this month, a fully
automatic exam was graded, stored, and visible to nobody but the candidate — no
roster, no attempt list, no export anywhere, and three declared permissions with no
code behind them. Forty students sat the exam and the coordinator who paid for it
asked them what it said.

**The export is worth a note.** The file was always right — a UTF-8 byte-order mark
so Arabic opens correctly in a spreadsheet without a manual encoding step, proper
escaping, returned as a download rather than a JSON string for the front end to
reassemble. The **button** was a plain anchor pointing at an origin-relative path,
so the primary action on this screen took the coordinator to the dashboard and lost
their filters. Same cause as Use Case 3; it now fetches with the token and saves.
Fixed in the same pass: the integrity flag count — "this candidate pasted four
times", which is an accusation — was reaching anybody who could read a score,
through both this roster and the CSV.

**Known gaps:** No score per section, because sections never reach delivery
(`RES-04`). No per-competency columns in the export. No certificate, which is the
artefact a vocational student's employer actually asks for (`RES-10`). No export of
the whole bank, so "your questions are yours" is a claim we cannot demonstrate
(`RES-11`).

---

## 🎯 Use Case 12: Find the questions that have stopped measuring | كشف الأسئلة التي لم تعد تقيس
**Status: PARTIAL — the screen is good; a row does not open the question** · `RES-06`, `RES-07`

**Screen:** `/results/questions`
**Role:** `Observer` (`Results.ViewItemAnalysis`). **Not the author**, who is the
person this screen is for: `ViewItemAnalysis` nests under `Results.View` and the
two combine with AND, so granting it to an author would also grant them every
named candidate and what they scored. The author therefore gets neither. This is
recorded rather than worked around; see `business/roles.md`.
**Actors:** Teacher, Training coordinator
**Preconditions:** At least twenty people have answered a question.
**Description:** The centre is told, in plain language, which of its questions are
not working — without anyone having to know what a discrimination index is.

**Flow:**
1. Coordinator opens **Results → Questions** for an exam.
2. Reads the list, worst first. Questions where the strongest candidates did worse
   than the weakest are named first, because that nearly always means a wrong key.
3. Reads the other reasons: everyone got it right, everyone got it wrong, it barely
   separates anybody.
4. Questions answered by fewer than twenty people are flagged as nothing at all,
   rather than being given a meaningless number.
5. Opens the offending question to fix it. **❌ There is no link on a row.**

**"These six questions are not measuring anything" is the most credible sentence
this product can say to an assessment professional, and it can now say it.** What
it cannot do is take the teacher to the question, which is most of the value.

**The statistics were libelling correct questions, and that is fixed.** The
analysis compares the top and bottom quarter of candidates, and a group that never
answered a question was being scored as though everybody in it got it wrong. Named
papers are assigned per class, so the top quarter is routinely everybody who sat
Form A — and every Form B question then showed strongly negative discrimination and
was flagged "nearly always a wrong answer key", sorted to the top of the list. A
whole form's worth. It now reports discrimination as **unmeasurable** rather than
as zero, says so on the screen, and refuses to measure at all when the cohort's
totals sit too close together for the quartile split to be anything but row order.
*A statistic that is confidently wrong is worse than one that declines to answer,
because an author acts on it.*

**Two things behind the screen are unfinished.** Discrimination is computed at read
time, per exam, and thrown away — `Question.DiscriminationIndex` is a column
assigned nowhere, permanently null, and the item-health chip on the question list
therefore classifies from difficulty alone, which is precisely the one thing the
pair exists to do: tell "hard" apart from "the key is wrong". And the difficulty
index is a lifetime running mean that is never reset when a question or its key is
edited, so an author who fixes a wrong key keeps the wrong key's statistics — and
this screen then reports them as fact (`BNK-12`).

---

## 🎯 Use Case 13: Put the centre's own name on it | وضع اسم المركز وهويته
**Status: PARTIAL — nine settings are saved; four are read** · `BRD-01`, `BRD-02`, `BRD-03`, `CAT-01`, `ADM-06`

**Screen:** `/settings`
**Role:** `Admin` (`Administration.ManageSettings`). The **route itself carries no
guard**, deliberately, so anyone signed in can read the rules their exams run
under; only the write is guarded, and the sidebar link is hidden without the
permission.
**Actors:** Administrator
**Preconditions:** Signed in with the settings permission.
**Description:** The centre's students and staff should see the centre, not a
platform they have never heard of.

**Flow:**
1. Administrator opens **Settings**.
2. Sets the organisation name, a logo, a brand colour, a default language, a time
   zone, a default pass mark, and three switches. Everything saves. **✅**
3. The organisation's name replaces the product's in the staff shell and on the
   exam entry page a candidate sees. **✅**
4. The logo appears, in the shell and on the exam page. **✅ since `3923129`.**
5. The invitation email carries the centre's name and colour. **✅ since
   `4e59b1a`** — see Use Case 8.
6. The brand colour changes how the **product** looks. **❌ It reaches the
   invitation email and nothing else.**
7. The centre's own vocabulary — "Students" instead of "Candidates" — appears
   across the screens. **❌ It is saved and read by no screen.**

**Where it breaks, exactly.** The brand colour appears in the Angular source only
inside the settings feature — no CSS custom property is ever set from it, so an
administrator picks a colour, saves it, sees "saved", and the only thing that
changes is the button in an email. **Five of the nine settings are read by nothing
outside the settings screen**: self-registration, the default language, the time
zone, the default pass mark, and the switch that is supposed to turn integrity
observation off. The last of those is a consent problem rather than a configuration
one, and it has a twin: the same switch on an individual exam
(`Exam.CollectIntegritySignals`) is saved and never consulted either — signals are
recorded regardless of both. And the vocabulary editor writes a record that only
the catalogue screen reads back.

**One thing this screen fixed that is worth recording.** Two rival settings
services existed alongside it, and ABP generates a conventional controller for
every application service — so `PUT /api/app/system-general-settings`, which
carried no authorisation attribute at all, was an anonymous write that let anybody
rename the organisation without signing in. Both were deleted rather than guarded,
because a duplicate source of truth is how one of them ends up forgotten, and the
route smoke test now fails if either comes back.

**The read-only case is unreachable.** The settings route is deliberately left
ungated so that anyone signed in can read the rules their exams run under; the
sidebar link is hidden without the manage permission. The read-only mode cannot be
reached by exactly the people it was built for.

---

## 🎯 Use Case 14: Give staff accounts and decide what they may do | إدارة حسابات الموظفين وصلاحياتهم
**Status: BUILT — the password defect and the dead permission are both closed** · `ADM-01`, `ADM-02`, `ADM-05`

**Screen:** `/users`
**Role:** `Admin` alone. Staff accounts and tenant settings are the two things no
other role gets. `Users.ManageRoles` is a deliberate escalation guard: this product
has already had the failure where anybody who could correct a colleague's phone
number could tick `Admin` on their own record.
**Actors:** Administrator
**Preconditions:** Signed in as an administrator.
**Description:** A new coordinator is given an account with the roles their job
needs, and a marker is not given the answer keys to the whole bank.

**Flow:**
1. Administrator opens **Users**.
2. Creates an account, ticking the roles on the same form — an account is never
   briefly role-less. **✅**
3. Changes somebody's roles later, as a whole list rather than a diff — and that
   now requires its own permission, checked only when the list actually changes.
   **✅ since `3923129`; until then anyone who could edit a colleague's phone
   number could make themselves an administrator.**
4. Removes somebody who has left. **⚠ Only by hard delete; there is no
   deactivation.**
5. Resets a locked-out colleague's password. **✅ since `b07d970`.** Leaving the
   field blank keeps the current one; filling it replaces it, **and the old one
   stops working** — which was the broken half.
6. Corrects a colleague's phone number without touching their password or their
   roles. **✅** — the required-password rule moved to creation, where it belongs,
   and a country-code number is no longer refused by a ten-character column.

**What this cost, and why it is written down.** For a while the edit form showed a
password field, the client sent it, validation passed, the DTO carried it — and
nothing used it. The request answered **200**. An administrator would dictate a new
password to a colleague who could not sign in with it, and neither of them had
anything to make them suspicious. A refusal invites a retry; a lie gets built on.
It was proved fixed with `CheckPasswordAsync`, not with a status code, because the
status code was never the thing that was wrong — and `tools/probe-round-trip.js`
exists because of it: it sends a distinguishable value to every editable field and
reports every field that comes back unchanged.

**Still open:** there is no guard against an administrator removing their own last
administrative role.

**Five roles now exist where there were two.** `Admin`, `Coordinator`, `Author`,
`Marker`, `Observer`, seeded per tenant, and `angular/e2e/live/roles.spec.ts` is the
first test in this project that watches one of them be **refused**. Until then the
only role was `Admin`, which held every permission — so no permission here had ever
been exercised as a restriction, only ever as a grant that was always present. A
permission that is only ever granted is not a permission; it is a checkbox.

**Permissions are grouped by what a person does**, and the seeded administrator
role is granted by walking the permission tree rather than a hand-written list —
so adding a permission cannot silently lock the administrator out of a new screen.
That decision was made after a service authorised against a policy name nobody had
defined, which ASP.NET answers with a 500, so a permission mistake presented as a
broken screen.

**Every navigation link now resolves.** Seven went nowhere a month ago; the last
two — the user menu's profile link, pointing at a module deliberately not
registered, and the sidebar's Assignments entry, whose route required an exam id
and had no index page — were fixed while this document was being written. Two
subtler navigation defects went with them: in a zoneless application the sidebar
read permissions once at construction, so a user whose configuration had not landed
yet saw nothing but Dashboard permanently; and class and method authorisation
attributes combine with AND rather than override, so a "manage the classes" role
passed the route guard, watched the screen mount, and had every request refused
(`ADM-02`).

**The permission that enforced nothing is gone.** `Administration.Access` promised
"may reach the staff application" and guarded nothing at all — everybody who can
sign in is staff, and being signed in is what the shell already requires. It was
**removed rather than enforced**, because a permission that can be granted and
changes nothing is a promise the administration screen makes and the product does
not keep. It was found by a static check that now stands permanently: every defined
permission must be enforced somewhere, by an attribute or by an explicit check.

Two sibling checks stand beside it — every application service carries a
class-level `[Authorize]` (the candidate's own path being the single exception,
named individually with its reason), and every policy named in an attribute is
actually defined. These exist because the test host calls
`AddAlwaysAllowAuthorization`, so **no `[Authorize]` in this solution is executed by
any integration test**. The static checks close what can be closed without a
running request; they do not close the rest.

---

## 🎯 Use Case 15: Bring an existing exam in | استيراد اختبار قائم
**Status: NOT BUILT** · `IMP-01` to `IMP-04`

**Screen:** none exists
**Role:** would be `Author` (`Questions.Create`)
**Actors:** Training coordinator
**Preconditions:** The centre holds its exam as a Word file or a Google Forms
export.
**Description:** A centre with two hundred questions in a document should not have
to retype them to try this product.

**Intended flow:**
1. Coordinator pastes the exam as text, or uploads the `.docx`.
2. Numbered prompts and their option lines are recognised as draft questions; a
   correct-answer tick is proposed as the key, as a proposal and never saved
   silently.
3. The form's trailing chrome — "submit", "clear form", the required-field legend —
   is not imported as questions.
4. Every question whose text refers to a picture that was not in the file is
   listed, so the chart is attached rather than discovered missing by a candidate.
5. The coordinator names the domain and level once, and every imported question
   lands in the shared bank at that level.
6. Nothing is written until they confirm.

**Nothing of this exists.** No document parser, no `.docx` reader, no import screen
for a document, no route, no symbol.

**But the door it was guarding is now open by another route.** Use Case 17 —
importing a question bank from a spreadsheet — landed in `9da7c46` and takes a
centre from "our questions are in a file" to a populated bank without retyping.
That covers the common case: most centres' question banks are in a spreadsheet or
can be pasted into one. What remains unbuilt is the harder case — a Word document
or a Google Forms export, where the structure has to be *inferred* from numbered
prompts and option lines rather than read from named columns, and where a picture
referenced in the text was never in the file.

So this is no longer the single highest-leverage unbuilt thing. It is the second,
and the gap it still leaves is a centre whose two hundred questions live in a
document nobody will convert.

---

## 🎯 Use Case 16: Place a student by their profile | توزيع الطالب حسب مستواه
**Status: NOT BUILT** · `RES-04`, `EXM-06`, `TAK-09`, `BPR-04`

**Screen:** `/exams/:examId/structure` authors the sections; no screen reports
them
**Role:** `Author` writes the sections, `Coordinator` and `Observer` would read the
profile
**Actors:** Training coordinator
**Preconditions:** A sectioned placement exam.
**Description:** A new student sits one paper and the centre learns which class to
put them in — not merely whether they passed.

**Intended flow:**
1. Student sits a paper divided into Listening, Reading, Grammar and Writing, each
   drawn to its own recipe and timed separately.
2. The result reports a score per section against a maximum, not one number.
3. A section below its minimum is named as the reason, however good the total.
4. The coordinator reads the profile and assigns a class.

**What exists and what does not.** Sections can be created and configured; nothing
in delivery, grading or reporting knows they exist (Use Case 5). A blueprint rule
can be scoped to a section in the schema and in nothing else.

**The competency breakdown is the near substitute and it is real.** A result
already reports listening 40%, reading 85% by topic rather than by section, which
answers most of the placement question — provided the questions are filed under
competencies, which they now can be. Until sections ship, sell the competency
profile and do not promise a section-by-section report.

---

## 🎯 Use Case 17: Bring a question bank in from a spreadsheet | استيراد بنك الأسئلة من جدول
**Status: BUILT — new in `9da7c46`** · `IMP-06`

**Screen:** the import panel on `/questions` and on `/exams/:examId/questions`
**Role:** `Author` (`Questions.Create`)
**Actors:** Teacher, Training coordinator
**Preconditions:** A category exists, or an exam to import into.
**Description:** A centre whose questions are already in a spreadsheet gets them
into the bank without retyping any of them — and without writing a single line of
anything.

**Flow:**
1. Author opens **Import from a spreadsheet** and downloads the sample file. Its
   column headings are generated from **the same localisation keys the reader
   matches**, so the file we hand out is never a file we reject.
2. Fills it in, or renames the columns in their own file to match, and saves it as
   CSV. The screen says where that option is in Excel, and that Arabic needs UTF-8
   specifically.
3. Chooses the file. **Nothing is written.** The preview shows what will be created
   and what is wrong.
4. Reads the problems. Each carries the **row number as the spreadsheet shows it**
   and the column name, so the fix is one cell rather than nine.
5. Confirms. Good rows are added; rows already present are left alone; one bad row
   costs the good ones nothing.

**What makes this hold up.** The correct-answer cell accepts three shapes because
people write three: the option number (`٢`), several numbers (`١،٣`), or the answer
written out (`القاهرة`). True/false accepts صح/خطأ, نعم/لا, ١/٠ — and the two
options are generated in the language the answer was written in, so an Arabic bank
does not get `True`/`False`. Arabic text is normalised before any comparison:
alif forms, tāʾ marbūṭa, alif maqṣūra, hamza carriers, diacritics, tatwīl, and both
sets of digits. The file is read as Excel actually writes it — the byte-order mark,
comma or semicolon or tab depending on the machine's locale, all three line
endings, and standard quoting, so a question containing a comma survives and a
question spanning two lines does not shift every row number after it.

**Two refusals worth naming.** A file with no "question" column or no "type" column
is refused outright, because the type of a question is not something to guess.
And in English, "multiple choice" is refused as **ambiguous** and the author is
asked to say "single choice" or "multiple choice"— half of English speakers mean
one answer and half mean several, and guessing produces a bank that marks wrongly
and looks fine. In Arabic «اختيار من متعدد» reads as one answer and is accepted.

**And the rule it does not become a way around.** Every imported row goes through
the same validation a hand-written question does, so importing is not a route past
the checks that stop an ungradable question reaching a candidate.

**Known gaps.** Four types only (single choice, multiple choice, true/false, short
answer); no media column; two megabytes and two thousand rows per file.

---

## What a customer can and cannot be shown today | ما يمكن عرضه اليوم

Walked end to end, in order, as a demonstration:

| # | Step | | Case |
|---|---|---|---|
| 1 | Set up a catalogue | ✅ | 1 |
| 2 | Write questions the centre owns | ✅ | 2 |
| 3 | Import a question bank from a spreadsheet | ✅ | 17 |
| 4 | Attach a chart or a recording | ✅ | 3 |
| 5 | Build an exam, be stopped from publishing a broken one | ✅ | 4 |
| 6 | Write the blueprint the paper is drawn to | ✅ | 4 |
| 7 | Divide it into sections | ⚠ saves, does nothing | 5 |
| 8 | Build and approve a named paper | ✅ | 6 |
| 9 | Import a class roll | ✅ | 7 |
| 10 | Put the class at a level | ✅ | 7 |
| 11 | Send the exam, choosing the approved paper | ✅ | 8 |
| 12 | The invitation arrives carrying the centre's name and colour | ✅ needs an SMTP relay | 8 |
| 13 | Sit it — clock, autosave, resume, submit | ✅ | 9 |
| 14 | Watch the sittings in progress, end a stuck one, record why | ✅ | 8 |
| 15 | Mark the written answers | ✅ without the model answer | 10 |
| 16 | Read the roster, the profile and the answer sheet | ✅ | 11 |
| 17 | Export the results | ✅ | 11 |
| 18 | See which questions stopped measuring | ✅ no link to the question | 12 |
| 19 | Put the centre's name and logo on it | ✅ colour reaches the email only | 13 |
| 20 | Give five people five different jobs, and watch one be refused | ✅ | 14 |

**Nineteen of twenty steps work end to end**, and the twentieth — sections — is
the one marked as saving and doing nothing.

**The honest sentence for a meeting:**

> A centre can bring its question bank in from a spreadsheet or write it here,
> filed under its own domains and competencies; approve the exact paper before it
> goes out; send it to a class at a level in a message carrying the centre's own
> name; watch somebody sit it on a phone in Arabic with a clock it cannot cheat;
> watch the room while they sit it and end a sitting that hung, on the record; mark
> what needs a person; read the roster, the answer sheet and the competency
> profile; export it; be told which of its questions have stopped measuring; and
> give five members of staff five different jobs with five different sets of keys.

Every clause of that is demonstrable today.

---

## What this product does not do | ما لا يفعله هذا المنتج

Stated plainly, because a document that promises what is not there is worse than
one that is merely incomplete. The full list, with the code that was read to
confirm each line, is **`README.md` §3**. In short:

**Absent outright.** Candidate accounts (by design — the link is the credential).
Importing an exam from a Word document or a Google Forms export (Use Case 15).
Certificates. Any form of proctoring: no webcam, no screen share, no lockdown
browser. Code execution — a code answer is compared against an expected output as
text and nothing is ever run. On-premises installation. A global list of everything
that has been sent (links are read per exam). Printing a paper. Comparing one
candidate to another. Re-opening a mark that has been awarded, sharing the marking
queue out between markers, or measuring how consistently two markers agree.

**Saved, and read by nothing.** Sections and everything on them — their clock,
their floor, their qualifying flag (Use Case 5). The tenant's own vocabulary (Use
Case 13). Five tenant settings: self-registration, default language, time zone,
default pass mark, and the switch that is supposed to stop integrity observation.
The brand colour anywhere but the invitation email. The same integrity switch on an
individual exam.

**Half there.** Six integrity signal types are defined and two are ever produced. A
`scale` question is recorded and scores zero rather than going to a marker. A
blueprint rule that cannot find enough questions contributes what it can, so the
paper comes out shorter with no signal at delivery time. Item analysis cannot be
granted to the author it is for, because it cannot be separated from the roster.
Roles have no Arabic names.

**والوعد الذي لا يُقطَع هنا هو نصف قيمة هذه الوثيقة.** ما ليس في المنتج مذكورٌ
باسمه، لا مُخفَّفاً ولا مُؤجَّلاً إلى «قريباً».
