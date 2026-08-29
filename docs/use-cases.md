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

Story identifiers in brackets — `RES-01`, `PLT-09` — point at `user-stories.md`,
where the acceptance criteria and the test plan live.

---

## 🎯 Use Case 1: Set up the catalogue | إعداد الكتالوج
**Status: BUILT** · `CAT-02`, `CAT-03`, `CAT-04`

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
**Status: PARTIAL — the file uploads and nothing can display it** · `BNK-04`, `PLT-09`

**Actors:** Teacher, Candidate
**Preconditions:** A question exists.
**Description:** A question about a candlestick chart should show the chart; a
listening question should play the clip.

**Flow:**
1. Teacher drags a file onto the media field, or clicks to pick one. No URL is
   typed.
2. The file uploads. Oversized files and disallowed types are refused by name and
   limit. **✅ This works.**
3. The preview should render in place. **❌ It does not.**
4. The candidate sitting the exam should see the image or hear the clip. **❌ They
   do not.**

**Where it breaks, exactly.** Every URL the product hands the browser to fetch for
itself — an `img`, an `audio`, a `video`, a download link — is built
origin-relative, as `/api/assessment/media/…`. The app runs on one origin and the
API on another, in both the development and the "production" environment files,
with no proxy configured, so the browser asks the wrong server. And even
same-origin it would fail for staff, because a browser's media request carries no
authorisation header, so an author's preview is an anonymous request against a
permission check and comes back 404.

The candidate's path was designed correctly and is defeated by the same problem:
their media URL carries a signed grant naming one blob and expiring with the
attempt, which is exactly right for somebody with no account.

**Six symptoms, one cause:** the author's preview, the candidate's question media,
the exam entry page's logo, the staff shell's logo, the hotspot editor's image, and
the reviewer's link to an uploaded answer.

**Why it survived.** The browser test stubs this exact URL, so it asserts our own
mock is reachable; the live backend test fetches the blob with an API client
carrying a token, which no `<img>` tag can do. Both sides pass and neither crosses
the seam.

---

## 🎯 Use Case 4: Build an exam and publish it | بناء اختبار ونشره
**Status: BUILT** · `EXM-01`, `EXM-03`, `EXM-04`

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

**Actors:** Training coordinator, Reviewer
**Preconditions:** A published exam with questions.
**Description:** Rather than trusting a random draw, the centre builds "Form 1" as
a fixed list of questions, a human reads it, and that is the paper the class sits.

**Flow:**
1. Coordinator opens **Exam → Papers** and creates a form with a name and a code.
2. Either hand-picks questions from the exam's drawable bank and orders them, or
   generates one from the blueprint — and the form records which it was.
3. A reviewer reads it and publishes it. The maximum score freezes; the question
   list becomes immutable; an empty or duplicated form is refused.
4. When sending the exam, the coordinator picks that published form.
5. Every candidate on that assignment sits exactly it, in its order, with its
   frozen marks.
6. Later, the form shows how many times it has been sat, and can be retired without
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
**Status: BUILT, with the invitation unbranded** · `ASG-02`, `ASG-03`, `ASG-04`, `ASG-05`

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

**Known gaps, and each one is a place a pilot stops.** There is no resend: the
plaintext token is returned once at creation and only its hash is kept, so a
student who deletes the email needs a new link, not the same one (`ASG-06`). There
is no way to extend an expiry for someone who was ill (`ASG-07`). There is no way
to end an attempt that is stuck (`ASG-08`). Sending to one person means creating a
class of one (`ASG-01`).

**The invitation is not the centre's.** It is a hardcoded bilingual message
carrying the candidate's name, the exam title, the duration, the expiry and a long
token link — with no organisation name, no logo and no support address. An
unbranded message with a long token link, sent to a teenager, from nobody they
recognise, is a description of a phishing email (`BRD-03`). SMTP also points at a
local address with no credentials, so no invitation has ever actually been
delivered.

---

## 🎯 Use Case 9: A candidate sits the exam | جلوس الممتحن للاختبار
**Status: BUILT** · `TAK-01` through `TAK-07`, `TAK-11`, `TAK-12`

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

And the observations the reviewer reads are mislabelled, for the reason in Use
Case 9: they are all recorded as pastes.

---

## 🎯 Use Case 11: Read the results and get them out | قراءة النتائج وتصديرها
**Status: PARTIAL — every screen works; the export does not download** · `RES-01`, `RES-02`, `RES-03`, `RES-05`, `RES-12`

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
   **✅**
6. Presses **Export**. **❌ Nothing arrives.**

**This closes the sharpest break in the product.** Until this month, a fully
automatic exam was graded, stored, and visible to nobody but the candidate — no
roster, no attempt list, no export anywhere, and three declared permissions with no
code behind them. Forty students sat the exam and the coordinator who paid for it
asked them what it said.

**Where the export breaks, exactly.** The file itself is right: a UTF-8 byte-order
mark so Arabic opens correctly in a spreadsheet without a manual encoding step,
proper escaping, returned as a download rather than a JSON string. The button is
right and is hidden without the export permission. The **link** is a plain anchor
pointing at an origin-relative path, so the browser asks the app's own origin
instead of the API — and even same-origin it would carry no credential against a
permission-checked endpoint. Same cause as Use Case 3 (`PLT-09`).

**Known gaps:** No score per section, because sections never reach delivery
(`RES-04`). No per-competency columns in the export. No certificate, which is the
artefact a vocational student's employer actually asks for (`RES-10`). No export of
the whole bank, so "your questions are yours" is a claim we cannot demonstrate
(`RES-11`).

---

## 🎯 Use Case 12: Find the questions that have stopped measuring | كشف الأسئلة التي لم تعد تقيس
**Status: PARTIAL — the screen is good; a row does not open the question** · `RES-06`, `RES-07`

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
**Status: PARTIAL — nine settings are saved; two are read** · `BRD-01`, `BRD-02`, `BRD-03`, `CAT-01`, `ADM-06`

**Actors:** Administrator
**Preconditions:** Signed in with the settings permission.
**Description:** The centre's students and staff should see the centre, not a
platform they have never heard of.

**Flow:**
1. Administrator opens **Settings**.
2. Sets the organisation name, an alternate-language name, a logo, a brand colour, a
   support email, a certificate footer, a default language, a time zone and several
   assessment defaults. Everything saves. **✅**
3. The organisation's name replaces the product's in the staff shell and on the
   exam entry page a candidate sees. **✅**
4. The logo appears. **❌ A broken-image icon appears.**
5. The brand colour changes how the product looks. **❌ Nothing changes.**
6. The invitation email carries the centre's identity. **❌ It carries none.**
7. The centre's own vocabulary — "Students" instead of "Candidates" — appears
   across the screens. **❌ It is saved and read by no screen.**

**Where it breaks, exactly.** The logo URL is origin-relative and unauthenticated,
like every other file the browser fetches for itself (`PLT-09`). The brand colour
appears in the Angular source only inside the settings feature — no CSS custom
property is ever set from it. Seven of the nine saved settings are read by nothing
outside the settings screen, including the switch that is supposed to turn
integrity observation off, which is a consent problem rather than a configuration
one. And the vocabulary editor writes a record that only the catalogue screen reads
back.

**The read-only case is unreachable.** The settings route is deliberately left
ungated so that anyone signed in can read the rules their exams run under; the
sidebar link is hidden without the manage permission. The read-only mode cannot be
reached by exactly the people it was built for.

---

## 🎯 Use Case 14: Give staff accounts and decide what they may do | إدارة حسابات الموظفين وصلاحياتهم
**Status: PARTIAL — the password field reports success and changes nothing** · `ADM-01`, `ADM-02`, `ADM-05`

**Actors:** Administrator
**Preconditions:** Signed in as an administrator.
**Description:** A new coordinator is given an account with the roles their job
needs, and a marker is not given the answer keys to the whole bank.

**Flow:**
1. Administrator opens **Users**.
2. Creates an account, ticking the roles on the same form — an account is never
   briefly role-less. **✅**
3. Changes somebody's roles later, as a whole list rather than a diff. **✅**
4. Removes somebody who has left. **⚠ Only by hard delete; there is no
   deactivation.**
5. Resets a locked-out colleague's password. **❌ The request succeeds and the
   password is unchanged.**

**Where it breaks, exactly.** The edit form shows a password field and the client
sends it. The update method never touches the password — there is no reset call
anywhere in it. The administrator will tell their colleague a password that does
not work. There is also no guard against an administrator removing their own last
administrative role.

**Permissions are grouped by what a person does**, and the seeded administrator
role is granted by walking the permission tree rather than a hand-written list —
so adding a permission cannot silently lock the administrator out of a new screen.
That decision was made after a service authorised against a policy name nobody had
defined, which ASP.NET answers with a 500, so a permission mistake presented as a
broken screen.

**Six declared permissions enforce nothing** anywhere in the product. **Two
navigation links still go nowhere** — the user menu's profile link, and the
sidebar's Assignments entry, whose route requires an exam id and has no index page
— and both silently deposit the user on the dashboard, which is exactly the
behaviour the navigation is supposed to prevent (`ADM-02`).

---

## 🎯 Use Case 15: Bring an existing exam in | استيراد اختبار قائم
**Status: NOT BUILT** · `IMP-01` to `IMP-04`

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

**Nothing of this exists.** No parser, no import screen, no route, no symbol.

**This is the single highest-leverage unbuilt thing in the product.** The candidate
roll importer proves the team can build this well — dry run, per-line errors,
idempotent re-import. The destination now exists too: the catalogue, bank
ownership and topic filing all landed this month, so an importer would have
somewhere correct to write. Without it, a trial dies in its second week on data
entry, and onboarding cost — three to five days of somebody's time per tenant — is
what actually sets the price floor.

---

## 🎯 Use Case 16: Place a student by their profile | توزيع الطالب حسب مستواه
**Status: NOT BUILT** · `RES-04`, `EXM-06`, `TAK-09`, `BPR-04`

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

## What a customer can and cannot be shown today | ما يمكن عرضه اليوم

Walked end to end, in order, as a demonstration:

| # | Step | |
|---|---|---|
| 1 | Set up a catalogue | ✅ |
| 2 | Write questions the centre owns | ✅ |
| 3 | Attach a chart or a recording | ❌ nothing displays |
| 4 | Build an exam, be stopped from publishing a broken one | ✅ |
| 5 | Divide it into sections | ⚠ saves, does nothing |
| 6 | Build and approve a named paper | ✅ |
| 7 | Import a class roll | ✅ |
| 8 | Put the class at a level | ✅ |
| 9 | Send the exam, choosing the approved paper | ✅ unbranded email |
| 10 | Sit it — clock, autosave, resume, submit | ✅ |
| 11 | Mark the written answers | ✅ without the model answer |
| 12 | Read the roster, the profile and the answer sheet | ✅ |
| 13 | Export the results | ❌ nothing downloads |
| 14 | See which questions stopped measuring | ✅ |
| 15 | Put the centre's name and logo on it | ⚠ name yes, logo no |

**Eleven of fifteen steps work end to end.** A month ago the same walk had two.

**The honest sentence for a meeting:** *we can show a centre building its own exam
from its own questions, approving the exact paper, sending it to a class, watching
somebody sit it, marking it and reading the results — and we cannot yet show a
question with a picture in it, or hand them the spreadsheet at the end.*

Both of those are the same defect, and it is small.
