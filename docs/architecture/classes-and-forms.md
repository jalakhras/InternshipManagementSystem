# Classes and forms

## The problem, in the owner's terms

Add a **شعبة** — a class or section of students — for each group of students,
sitting **under a job role or a training level**, and **linked to exam forms**.

Three claims are packed into that sentence, and they are not equally right.

1. *A group of students is a thing the product should name.* It already is:
   `CandidateGroup`, with a name, a description and an optional `CategoryId`.
2. *It sits under a role or a level.* It does not today. A cohort knows its
   domain and nothing about how advanced it is, so "Evening A1" and "Evening B2"
   are two rows the catalogue cannot tell apart.
3. *It is linked to exam forms.* It is not, and this document argues it should
   not be — not because the need is imagined, but because the link belongs one
   layer down, and putting it here builds a control for a mechanism that is not
   running.

---

## The decision

**Keep the level and the dates on the class. Drop `CandidateGroupForm`. Put the
form on the sitting and on the attempt, and make delivery honour it.**

Concretely: `CandidateGroup` keeps `LevelId`, `StartsOn`, `EndsOn` and
`IsActive` — that half of the proposal is right and should ship. Everything
`CandidateGroupForm` touches is removed. In its place, two nullable columns and a
branch:

| Change | Where | Why |
|---|---|---|
| `ExamFormId` (nullable) | `Assignment` | Which paper *this sitting* uses. Null means "let the exam's `DeliveryMode` decide." |
| `ExamFormId` (nullable) | `Attempt` | Which paper was actually served. Null means it was drawn per candidate. |
| honour `DeliveryMode` | `ExamTakingAppService.StartAsync` | The whole named-form feature is currently unreachable. |

That is two columns and one branch, against one entity, one table, two unique
indexes, a migration, a DTO, an app service and a screen — and the smaller
change is also the one that actually delivers the guarantee the larger one was
written for.

---

## Why: four facts about the code that settle it

These are verified in the repository at `404b99d`, not inferred. None of them was
changed by the commit that shipped the class — it touches the People context
only, and the delivery path is exactly as it was.

**1. Nothing consumes a form at delivery time.** `ExamDeliveryMode` is declared
in `AssessmentEnums.cs` and read by nothing outside a DTO projection.
`Exam.FixedFormId` is written by nothing and read by nothing.
`ExamTakingAppService.StartAsync` calls `_formBuilder.Build(exam, bank, …)`
unconditionally: every attempt in the system is a per-candidate draw, whatever
the exam says. `ExamForm` today is an authoring artefact with no delivery
consequence.

Linking a class to forms is therefore the third floor of a building with no
second floor. A coordinator would set "this class sits Form 1 first, Form 2 on
the retake", and every student would receive a random draw, silently. A
configuration surface for a mechanism that does not run is worse than no
surface, because it is a promise the software then breaks without an error.

**2. Nothing records which form was sat.** `Attempt` carries `ExamId`,
`CandidateId` and `ExamLinkId`. There is no `ExamFormId` anywhere in
`Assessment/Delivery/`. `AttemptQuestion` carries the frozen paper but not its
provenance.

This is the load-bearing gap. Without it: `ExamForm.TimesUsed` cannot be
incremented honestly, a result cannot print "Form B", an equating study can
never be run against real data, and — critically — **the retake rule cannot be
enforced**, because "give them a form they have not sat" requires knowing what
they sat.

**3. `TimesUsed` is read three times and written nowhere.** Grep confirms it:
two DTO projections and one guard. The guard is
`ExamStructureAppService.DeleteFormAsync`, which refuses to delete a form with
`TimesUsed > 0` — a guard that can never fire, so today a published form that
forty people sat can be deleted outright. `business-review.md` §2 offers
"this paper has been used three times, write a fourth" as our cheapest honest
security story. That sentence is currently false.

**4. A second link to the same exam is already a live defect.**
`StartAsync` resolves the link with

```csharp
.FirstOrDefaultAsync(l => l.CandidateId == claims.CandidateId
                       && l.ExamId == claims.ExamId && !l.IsRevoked)
```

`ExamSessionClaims` is `(AttemptId, CandidateId, ExamId, TenantId)` — no link
id — so the token minted from the *correct* link in `OpenLinkAsync` cannot tell
`StartAsync` which one it was. With one link per candidate per exam this is
harmless. A resit is a second link to the same exam. **The proposal's own
motivating scenario walks straight into it**: the student opens their Form 2
link, `StartAsync` picks whichever row the database returns first, and burns
`AttemptsUsed` on the wrong one.

This must be fixed whatever else is decided. It is not caused by the proposal;
the proposal is what makes it reachable.

---

## 1. Is the class the right unit, or is it two units?

**One unit, because the second unit already exists and it is not a group — it is
the `Assignment`.**

The distinction the question raises is real in the world. A class is who is
taught together: a roster, a room, a teacher, a term. An exam cohort is who sits
a paper together: a date, a form, an invigilator. In a school these diverge
constantly, in both directions:

- **Wider than a class.** A certification body examines 300 people on one
  morning, drawn from twelve different classes, all on one form.
- **Narrower than a class.** One class of thirty splits across a morning and an
  afternoon sitting, on two forms, because the room holds fifteen.
- **Neither.** Eight students from four classes sit a make-up paper in December.

The codebase already models this correctly and it is easy to miss:
`CandidateGroup` is the durable roster, and `Assignment` — one exam, one target,
one expiry, one attempt allowance, fanned out to one `ExamLink` per person — is
the sitting. `Assignment.CandidateGroupId` is already the link between them.
Two levels, and the second is per-event by construction.

So collapsing costs nothing, provided the *form* goes on the sitting. It costs a
great deal if the form goes on the class, because all three cases above become
inexpressible:

- The cross-class sitting needs the same form configured on twelve classes, and
  the exposure count fragments across twelve rows.
- The within-class split is forbidden outright by the migration as written. The
  unique index `(CandidateGroupId, Sequence)` means a class cannot have two
  papers at the same position — so Form 1 in the morning and Form 2 in the
  afternoon, which is the example `ExamForm`'s own XML doc comment gives as the
  reason forms exist, cannot be configured for one class.
- The make-up cohort is not a class and has no home.

### The incumbents agree, and they are worth reading

Both mature systems a coordinator has already used keep the roster and the paper
apart, and neither lets a class carry a version.

Canvas's `AssignmentOverride` is the cleanest evidence available. Its complete
field list — `student_ids`, `group_id`, `course_section_id`, `title`, `due_at`,
`all_day`, `all_day_date`, `unlock_at`, `lock_at` — is *who* × *when* and nothing
else. **There is no field that changes content, questions or version.**
([Canvas Assignments API](https://canvas.instructure.com/doc/api/assignments.html) — verified.)
Moodle's quiz override is the same shape: password, open, close, time limit,
attempts allowed, reason. No content field
([Moodle — Quiz overrides](https://docs.moodle.org/en/Quiz_overrides) — verified).

The consequence, which follows from those schemas rather than from any vendor's
opinion, is that to give two sections different questions in either product you
create two assessments and target one at each section. The version binds to the
*assessment side*; the section stays a roster. Our `Assignment` is exactly that
targeting object, and it already carries the two axes Canvas has (`CandidateId`
or `CandidateGroupId`; `ExpiresAt`). Adding `ExamFormId` to it gives us the one
thing both incumbents make you clone a whole quiz to get — which is a real
advantage, and it is available only if the form is on the assignment.

### On the word itself

Worth checking, because the owner chose it deliberately. In Saudi and Jordanian
administrative usage **شعبة is a course or class section** — a scheduled division
of students with a head count — not a department and not a track. King Saud
University's registration deanship describes managing *"الطاقة الاستيعابية للشعب
الدراسية"* and *"دمج أو الغاء الشعب الدراسية"* — a thing with a capacity that can
be merged or cancelled against a course
([KSU Deanship of Admissions and Registration](https://dar.ksu.edu.sa/ar/registration-dept) — verified).
Jordanian licensing regulation is more explicit still: *"لا يجوز ان يتجاوز عدد
الطلبة في الشعبة الواحدة ثلاثين طالبا"*, and it names the compound **شعبة صفية**
and charges a fee to add one
([Jordanian regulation on licensing educational institutions](https://jordanianlaw.com/الأنظمة/نظام-تأسيس-وترخيص-المؤسسات-التعليمية/) — verified).

Two things follow. First, the owner means a *taught* section with a roll and a
cap, which is nearer Canvas's Section than Moodle's Group — so the collapse this
document recommends is a deliberate simplification and should be described as one
in the room, not as a translation. Second, the sense is regional: in Maghreb
usage شعبة commonly means a stream or field of study
([Almaany](https://www.almaany.com/ar/dict/ar-ar/شعبة/) — verified; Algerian and
Moroccan usage — asserted, secondary). One more reason the noun belongs in
`CategorySet`, per tenant, rather than in the schema.

**Does the recruiter's case break?** No — and the honest version of that answer
has two halves.

The entity does not break. An "October intake" of applicants carries a name, a
role, a window and a paper, and needs none of the teaching apparatus. Nothing in
`CandidateGroup` presumes teaching; nothing added here does either. A recruiter's
intake and a language centre's class are the same seven columns.

The *word* breaks, badly. A recruiter told their applicants are in a "شعبة"
concludes the product is for schools, and that is a lost meeting. The platform
already has the mechanism to prevent it — `CategorySet.GroupSingularName` and
`GroupPluralName`, per tenant, exactly so a recruiter sees "Batch" and an
academy sees "شعبة". But note honestly: **`CategorySet` has no application
service, no API and no UI.** It is an entity, an EF configuration and four doc
comments. So the mitigation is currently theoretical.

The instruction that follows is therefore firm: **do not introduce "class" or
"شعبة" as a new noun in the schema, the DTOs, the permissions or the API.** The
noun is `Group`, and the tenant renames it. If the owner wants the word in front
of a customer, the story to build is the catalogue vocabulary screen (CAT-01),
not a second entity.

One genuine cost of collapsing, stated so it is not discovered later: a person
belongs to several groups (`CandidateGroupMember` allows it, deliberately), so
"which class is this student in?" has no single answer. For a result sheet that
wants one heading, this will need a rule — most likely "the active group at the
level of the exam sat" — and it is cheaper to decide that when a results screen
exists than to prevent it now with a constraint the recruiter would resent.

---

## 2. Level or exam?

**Level. Keep `LevelId` nullable, and add nothing else.**

The argument for pointing at exams is not silly: what a coordinator wants on the
class screen is "which papers does this class sit", and an exam is nearer that
answer than a level is. It fails on three counts.

**A class outlives any one exam.** "Evening A1, autumn" sits a placement test in
week one, a mid-term in week six and a final in week twelve — three exams, and
the coordinator adds the third in November. A single `ExamId` is wrong on day
one. A collection of exam ids is a join table that duplicates, less capably,
what `Assignment` already stores: `Assignment` records not just *which* exam but
*when it was sent*, *how long the links live*, and *how many attempts* — and it
records it per event rather than as a standing intention.

**Level is already the coordinate system.** `Level` is scoped to `Category`
(precisely so a tenant assessing both software roles and English levels is not
offered "QA Engineer" under "English"). `Exam` carries `CategoryId` +
`LevelId`. `Question.DrawableBy(examId, categoryId, levelId)` — the predicate
that makes the shared bank work — is keyed on the same pair. A class carrying
`(CategoryId, LevelId)` lands in the same coordinate space as the exams and the
bank, which means "which exams can this class sit?" is a `Where` clause, not a
join. That is the whole practical value the exam link was reaching for, obtained
for a column already added.

**Level carries the ladder the academy buyer actually has.** `Level.DisplayOrder`
is documented as "the ranking a level name implies". A sequential-levels academy
— `business-review.md` §2's first customer — is a ladder, and "which class comes
after this one" is a question the level answers and an exam does not.

Two qualifications, both of which mean *keep the nullability*:

- **A recruiter has no level.** `CategoryId` alone ("Backend Developer") is the
  whole coordinate. `LevelId` must never be required in the UI, and the class
  form must not show it as an empty required field to a tenant with no levels.
- **A placement test inverts the level, and this is verified rather than
  assumed.** Seton Hall's language placement policy states it plainly:
  *"Students continuing a previously-studied language must take a placement test
  **before they can register for a language class**"*, and *"You should register
  for the level you placed into"*
  ([Seton Hall — about the language placement test](https://www.shu.edu/global-learning-center/about-the-language-placement-test.html) — verified).
  Melbourne's arts faculty adds the correction path: a mis-placed student is
  asked *"to withdraw from their current subject and re-enrol in an alternative
  level"* — the fix is moving the student between classes, never re-versioning
  the test (asserted; the page returned 403 and this is from search summary).
  ELS emails the placement test six weeks before the start date, and a student's
  level *"can only be determined after they have taken"* it (asserted,
  secondary).

  So the placement class does not have a level; the level is the test's *output*,
  and the class is the test's output too. The real language-centre sequence is:
  create "October intake" with no level → assign the placement test → read the
  per-section profile → create "Evening A1" and "Evening B1" and move people into
  them. The class screen must therefore allow a class with no level and allow the
  level to be set later. A `[Required]` attribute on
  `CreateUpdateCandidateGroupDto.LevelId` would break the language centre's
  primary workflow. It is absent today; keep it absent, and write the test that
  keeps it absent.

  It also means the exam cohort must be able to exist without a class at all —
  which it can, because `Assignment.CandidateId` targets one person. Had the form
  gone on the class, the single commonest language-centre assessment in the world
  would have been the one case the feature could not serve.

---

## 3. The form link

**`Sequence` is the wrong shape, and the join is the wrong table.**

### What `Sequence` claims

That a class decides in advance which paper is first and which is the retake,
and that this is what makes the retake guarantee real. Two objections, one about
the world and one about the code.

### The world: nobody pre-assigns "the retake paper", and forms are spiralled per person

I looked for a body that does what `Sequence` describes and did not find one.
What the evidence shows instead, in three parts.

**Retake policy is a waiting rule and a fee, not a nominated second paper.**
Every official page I could read is governed by *when you may book again*, and
every one of them is silent on which paper you then get.

- IELTS: *"you can apply to resit your test as soon as you feel ready"* — no
  waiting period, no cap, and no statement about the version
  ([ielts.org — resitting the test](https://www.ielts.org/en-us/for-test-takers/resitting-the-test) — verified).
  Its One Skill Retake must be taken within 60 days, once per test, and is
  described as *"the same format and timing"* as the original skill
  ([ielts.org — One Skill Retake](https://ielts.org/take-a-test/booking-your-test/one-skill-retake) — verified).
- TOEFL iBT: *"as often as you like, but not more than once in a 3-day period"*
  ([ETS FAQ](https://www.ets.org/toefl/test-takers/ibt/faq/taking-the-test.html) — verified).
- PTE Academic: *"You can only book your new test once you have received the
  scores from your last test"*, capped at twelve in twelve months
  ([Pearson retake policy](https://www.pearsonpte.com/policy-center/retake-policy/) — verified).
  The page says nothing about versions or pools; I looked for it specifically.
- Cambridge English devolves it: no global retake policy, with local centres
  setting their own (asserted — the official regulations returned 403; the claim
  is from a *Language Testing* review,
  [Pearson 2023](https://journals.sagepub.com/doi/full/10.1177/02655322231186706)).
  That last one is worth dwelling on: the body closest to our buyer treats resit
  policy as an *administrative* decision made by the centre, not a test-design
  artefact — which is an argument for a coordinator choosing a form on a sitting,
  and against the platform encoding an order.

**The modern high-security answer is a fresh draw, not a second named paper.**
Duolingo is explicit: items are *"selected in real time from our bank of tens of
thousands"*, so *"no two tests are identical, meaning it's impossible to get
answers to the test in advance"*
([Duolingo blog](https://blog.duolingo.com/is-the-duolingo-english-test-hard/) — verified).
PSI describes LOFT the same way — *"the generation of unique and equivalent exam
forms for each test taker"* — and frames publishing *"as few as one, two, or
three versions"* as precisely the weakness LOFT exists to solve
([PSI — LOFT](https://www.psiexams.com/knowledge-hub/increase-security-efficiency-linear-on-the-fly-testing-loft/) — verified).
Note where that leaves us: `DeliveryMode.DrawPerCandidate`, our default, is the
*more* modern position, and named forms are the concession we make to a
reviewer who must read the paper. Both are right; neither is a cohort ordering.

**Where forms genuinely are pre-built and assigned, the unit is the individual,
and assigning by cohort actively breaks it.** Professional Testing's account of
multi-form development is the clearest primary source I found: forms are
assembled from the bank to a blueprint, *"a test form is statistically equated to
another test form to make the resulting test scores directly comparable"*, and
the assignment mechanism is a **spiralled** random-groups design —
*"Form A may be given to the first examinee, Form B to the second examinee, Form
A to the third examinee, and so on"*
([Professional Testing — developing multiple forms](https://www.proftesting.com/test_topics/steps_7.php) — verified).

That sentence is the one that decides this. Spiralling alternates forms *within*
one administration so that the group taking each form is randomly equivalent,
which is what makes equating possible at all. **Giving one form to a whole class
is the exact opposite of spiralling**: it makes form and cohort perfectly
confounded, so any later attempt to compare Form 1 against Form 2 measures the
difference between two classes rather than between two papers. `Sequence` does
not merely fail to help with comparability — it destroys the design that would
have delivered it, and it does so silently, years before anybody tries the
analysis.

`ExamDeliveryMode.RotateForms` as documented — spread candidates across forms in
turn — is spiralling, correctly named, already in the enum, and unimplemented.
That is the feature. The class ordering is its opposite.

Two honest limits on all of the above. No official page in either direction says
how IELTS, TOEFL or PTE choose a retake form; silence is not evidence of a pool.
And `assess.com`, which `research-2026-08.md` leans on, returned 403 throughout,
so the equating claims here rest on proftesting.com, which I did read, rather
than on the source that document cites.

**The mechanism that actually delivers the guarantee** is a selection rule at
delivery — *do not serve this candidate a form they have already sat* — and that
rule is stronger than a configured sequence in three ways:

1. It holds for a candidate who joined the class after the order was set.
2. It holds for a candidate who is not in a class at all — the recruiter's
   single-candidate assignment, `Assignment.CandidateId`, which
   `CandidateGroupForm` cannot reach.
3. It cannot be forgotten. A configured order is one unset field away from
   silently degrading to a redraw.

### The code: the backlog already specifies this, better

`docs/user-stories.md` Epic 6 already contains the story, already accepted:

> **FRM-06 · Guarantee a retake differs** — "A second attempt by the same
> candidate on the same exam is assigned a different published form when one
> exists. When none exists, the coordinator is warned at assignment time rather
> than the candidate discovering it."

and

> **FRM-05 · Spread a cohort across forms** — "each new attempt takes the
> published form with the lowest `TimesUsed`; ties break deterministically…
> A retake by the same candidate takes a form they have not sat, when one
> exists."

Both are candidate-scoped and resolved at attempt start. `CandidateGroupForm` is
a second, weaker implementation of a story the project had already written down
and solved correctly. It is class-scoped where the requirement is
candidate-scoped, configured where the requirement is computed, and it cannot
satisfy FRM-05's exposure-levelling at all because a class's fixed order defeats
the point of levelling `TimesUsed` across the tenant.

### The tell

`CandidateGroupForm` already carries `SittingOn`. A row with a date is not a
position in an order — it is an event. And an event that has a date wants an
expiry, an attempt allowance, a recipient list and a link fan-out, all of which
`Assignment` already has. The entity is drifting toward `Assignment` while it is
still being written. That is the signal to stop and use `Assignment`.

### What practice does want, and where each of those things goes

| What a centre actually does | Where it belongs |
|---|---|
| One paper for the whole class, this term | `Assignment.ExamFormId` on the group assignment |
| Morning group Form 1, afternoon Form 2 | Two assignments, two subsets, two forms |
| Resit in three weeks on a different paper | A second assignment; form chosen by the rule, or named explicitly |
| Make-up sitting for eight people in December | An assignment to eight candidates |
| Spread a big cohort across three papers | `DeliveryMode.RotateForms`, no `ExamFormId`, resolved per attempt |
| "Which paper did this student sit?" | `Attempt.ExamFormId` |
| "How worn is Form 2?" | `ExamForm.TimesUsed`, incremented at start |

Every row is served by two columns and a branch. None is served by `Sequence`.

One tension in that table is worth naming rather than hiding. Row one —
`Assignment.ExamFormId` on a group assignment — *is* one form per cohort, which
is the thing spiralling exists to avoid. It stays, because it is what a
coordinator legitimately wants on the day: an approved paper, read in advance,
sat by everyone in the room. The difference from `Sequence` is that it is a
choice about *one sitting*, revisable next term, rather than a standing property
of the class; and that the alternative sits beside it in the same field, one
`null` away. The right shape is: `RotateForms` is the default we recommend and
the one that keeps scores comparable, and naming a form on the assignment is the
override a certification-minded coordinator reaches for knowingly. If we ever
publish the limitations page `research-2026-08.md` §5.7 asks for, this is one of
the sentences on it.

---

## The entity changes, field by field

### `CandidateGroup` — keep, with reasons

| Field | Verdict | Reasoning |
|---|---|---|
| `LevelId` (nullable Guid) | **Keep** | Puts the class in the same `(Category, Level)` coordinate space as `Exam` and `Question.DrawableBy`, so "the exams this class can sit" is a filter rather than a join. Nullable because a recruiter has no level and a placement cohort does not have one *yet*. |
| `StartsOn` / `EndsOn` (nullable DateTime) | **Keep** | Distinguishes this term's class from last term's, which is otherwise one row that keeps being edited and one results history that keeps being overwritten. Both nullable because a self-paced academy has no term. |
| `IsActive` (bool) | **Keep** | The assign screen's group picker must not grow forever. Cheaper and clearer than inferring "finished" from `EndsOn < today`, which would hide a class whose end date was typed wrong. |
| `Forms` collection | **Remove** | See above. |

One constraint worth adding and one worth resisting.

- **Add:** `EndsOn >= StartsOn` when both are set, refused in the app service
  with a named error code, not swallowed. A class that ends before it begins
  breaks every ordering the results screens will want.
- **Resist:** making `StartsOn`/`EndsOn` govern anything. They are the class's
  life, not an exam window. `Exam.ScheduledStartTime`/`ScheduledEndTime` and
  `Assignment.ExpiresAt` already control when a paper can be sat, and both are
  enforced (`Exam.IsOpenAt`, `ExamLink.GetBlockReason`). If the class dates
  silently also gate attempts we will have three time windows with two owners,
  and the bug where a student cannot sit their exam because the term end date
  was typed as the last day of teaching.

### `CandidateGroupForm` — delete

**A correction, and it costs more than it did an hour ago.** This section was
drafted against the working tree, where the entity existed and nothing in the
application layer referenced it. Commit `404b99d` — *"Make a cohort a class: at a
level, in a term, with the papers it will sit"* — shipped the whole server side
while this document was being written: `SetGroupFormsAsync` on
`CandidateAppService`, `CandidateGroupFormDto`, three error codes with Arabic and
English strings, a controller action, and `ClassCohortTests.cs` at 265 lines.
There is no Angular screen yet.

So the removal is now: the entity, the `DbSet`, the
`builder.Entity<CandidateGroupForm>` block, `SetGroupFormsAsync` and its
interface member, `CandidateGroupFormDto` and `CandidateGroupDto.Forms`, the
`SetGroupFormsDto`, the controller action, the three `GroupForm*` error codes and
their six localisation entries, the tests that cover them, and the table from
`20260829165045_Add_Class_Cohorts_And_Their_Forms` — which also carries
`LevelId`, `StartsOn`, `EndsOn` and `IsActive`, so it should be regenerated
rather than hand-edited.

That is perhaps half a day, and it is still the right call, for the reason the
commit message itself anticipates: *"anything they find that contradicts this
will be a follow-up rather than a revert."* This is that follow-up. Three
observations to make the decision easier rather than more painful:

- **Two of the three validations survive the move.** `SetGroupFormsAsync`
  refuses a form belonging to another exam and refuses a form that is not
  `Published` (`GroupFormNotPublished` — *"scheduling a draft is scheduling a
  paper nobody approved"*). That reasoning is exactly right and it is the
  validation `Assignment.ExamFormId` needs. Move the check and the error codes to
  `AssignmentAppService.CreateAsync`; rename the codes from `GroupForm*` to
  `Assignment*`, keep the sentences.
- **The third does not, and should not.** The unique-index pair on
  `(CandidateGroupId, Sequence)` and `(CandidateGroupId, ExamFormId)` is the part
  that forbids the morning/afternoon split. It goes with the table.
- **The tests mostly survive too.** A test asserting "a retake gets a different
  paper" is still the test we want; it moves from asserting a configured order to
  asserting FRM-05's selection rule, which is a stronger assertion of the same
  guarantee.

The one thing genuinely lost is `SittingOn` as a stored intention — "this class
sits Form 2 on 14 December" before any assignment exists. If a coordinator turns
out to want to plan a term's sittings in advance rather than issue them, that is
a scheduling feature with its own shape, and it should be built as *a scheduled
assignment* (an `Assignment` with a future `SendOn`), not as an ordering on the
class. Worth watching for; not worth pre-building.

### `Assignment.ExamFormId` (nullable Guid) — add

*Which paper this sitting uses.* Null means "resolve from the exam's
`DeliveryMode`", which preserves every existing assignment's behaviour exactly.
Set means this sitting is on that named form, whatever the exam's default.

Nullable rather than required because the three delivery modes are genuinely
different intentions and the assignment should be able to express "I don't
care, follow the exam" as well as "this paper, this morning".

Validated at creation, not at start: the form must belong to the assignment's
exam and must be `Published`. Refusing a retired form here is what
`user-stories.md` FRM-03.3 asks for — "the assign action explains itself" —
and it is the difference between a coordinator learning at 8am and a candidate
learning at 9am.

### `Attempt.ExamFormId` (nullable Guid) — add

*Which paper was actually served.* This is the field the whole feature rests on,
and it is the one the proposal omits.

Null means the paper was drawn per candidate and exists only as
`AttemptQuestion` rows — which stays correct, and stays the default. Set means
the paper is a named form, and then: the result can print it, `TimesUsed` has a
denominator, FRM-06's "a form they have not sat" is a query, and an equating
study three years from now can read what happened.

Copied, not referenced through the link — for the same reason
`AttemptQuestion.Score` and `ExamFormQuestion.Score` are copied. What a candidate
sat must keep meaning what it meant on the day.

### `ExamTakingAppService.StartAsync` — branch on `DeliveryMode`

- `FixedForm` → the assignment's `ExamFormId`, else `Exam.FixedFormId`. Build
  `AttemptQuestion` rows from `ExamFormQuestion` in `DisplayOrder`; do not
  consult the blueprint. `Attempt.MaxScore` is the form's frozen `MaxScore`.
  Option shuffling still applies if the exam asks; question order does not,
  because the form's order *is* the form. (FRM-04.)
- `RotateForms` → published forms of this exam, excluding any this candidate has
  already sat (`Attempt.ExamFormId`), lowest `TimesUsed` first, ties broken on
  `Code` so the choice is deterministic and reproducible. (FRM-05, FRM-06.)
- `DrawPerCandidate` → today's path, unchanged.
- Increment `ExamForm.TimesUsed` on a real start, in the same place
  `link.AttemptsUsed++` happens — never on `OpenLinkAsync`, which is only a
  validity check.

`AttemptQuestion` needs no change: it carries `QuestionId`, `Position`,
`OptionOrder` and `Score` and is already agnostic about where the paper came
from. `ExamFormBuilder` needs no change either; the fixed-form path does not go
through it.

### The link-resolution defect — fix it in the same change

Add the `ExamLinkId` to `ExamSessionClaims` and mint it in `OpenLinkAsync`, so
`StartAsync` uses the link the candidate actually opened. Two links to one exam
is not an edge case once resits exist; it is the normal case.

---

## What it unlocks, by buyer

### The recruiter

A class is "October applicants — Backend Developer": `CategoryId` set,
`LevelId` null, `StartsOn`/`EndsOn` bounding the intake.

Unlocked: one assignment to the whole intake on one approved paper; a roster and
a results view scoped to that intake rather than to all candidates ever; and the
comparison screen `competitive-position.md` calls the one a recruiter lives in,
which needs a *set* of comparable people and gets it from the group. The intake
window is what makes "this quarter's applicants" a query.

Not unlocked, and not wanted: levels, terms, teaching. Say plainly in the room
that the level is optional and the word is theirs.

### The language centre

A class is "Evening A1, autumn": `CategoryId` = English, `LevelId` = A1, dated.

Unlocked: the placement workflow end to end — an undated, unlevelled "October
intake" class, one assignment of the placement test, and levelled classes
created from the result. This is the sequence the buyer actually runs, and the
class entity is what makes the second half of it possible. It is worth noting
that the *first* half still depends on section-aware results, which are
authoring-only today (`ExamFormQuestion` and `AttemptQuestion` both carry no
`ExamSectionId`), so the profile that decides the placement does not exist yet.
The class does not change that; it stops being the blocker it would otherwise
become.

Also unlocked: a resit on a genuinely different paper, which for a centre
charging for resits is a fairness question their students will raise.

### The training academy

A class is "October intake — Level 1": the ladder is `Level.DisplayOrder`.

Unlocked: the record the academy actually keeps, which is *pass/fail per
student, per level, per intake*. Without a dated class that record has no third
axis and last October's cohort overwrites this one. With it, "who passed Level 1
in the October intake" is a query, and the second conversation — Level 2's exam
— has somewhere to put its results.

Also unlocked: `MinimumPercentage` on a safety module becomes a defensible
statement about a named cohort rather than about a person.

---

## What we deliberately exclude

The adjacent things that look obvious. Each is named with the reason it is or is
not our problem, because the trap is that they all feel like one more column.

**Timetables — not ours, and the most expensive mistake available.** A class has
a life (`StartsOn`/`EndsOn`) and a sitting has a date (`Assignment.ExpiresAt`).
A recurring weekly schedule with rooms and periods is a different product, it
has a daily write cadence this application has no rhythm for, and the coordinator
already owns one. Building it means competing with school-management systems on
their ground, with none of their features, to serve a need our buyer has already
met.

**Attendance — not ours, and the trap is that half of it already exists.**
"Did this student sit the exam?" is answered today by whether an `Attempt` row
exists, and that is the only attendance question an assessment platform should
answer. Lesson attendance is a daily record against a timetable we do not have.
The reason it is a trap: a coordinator asked to demo will ask for it, because it
is the thing they do most often, and it is the thing we would do worst.

**Teachers — not now, and this is the closest call.** An instructor on a class
is one nullable `OwnerUserId`, and it buys "my classes" filtering and a reviewer
scoped to their own students. Three reasons to wait. First, the reviewer's queue
is only now being built, and scoping a screen that has never been used by a real
marker is guessing at the shape of a problem nobody has hit. Second, a teacher
entity is never one column: it
becomes a login, a permission scope, a dashboard, and then the question of
whether a teacher may see another teacher's items — which our tenant-owned-bank
position answers "yes" and every teacher answers "no". Third, `Description` holds
the instructor's name today at zero cost. Revisit when the review screen exists
and a real tenant asks.

**Enrolment and capacity — not ours, and it is the disguised one.** Enrolment is
the class as a *registration*: a per-member status, a join date, a drop date, a
transfer, a capacity. `CandidateGroupMember` is a plain membership with none of
that, and that is correct. The moment membership acquires a status and dates,
every later question becomes temporal — "was she enrolled on the day she sat?" —
and we have signed up to maintain a registration system's invariants inside an
assessment product. If a centre needs to know who dropped, they know; we need to
know who sat, and the `Attempt` tells us.

Capacity deserves its own sentence because the owner's word implies it: a KSU
شعبة has a *الطاقة الاستيعابية* and a Jordanian one is capped at thirty by
regulation. A `MaxMembers` column is one line and it is still wrong here.
Capacity exists to ration a *room and a teacher*, neither of which we have; the
only thing capping a group would do in this product is refuse to add the
thirty-first student to a list of names, which is a rule the tenant's real
registration system already enforces and would only ever fire here as a
frustration. If it is ever asked for, it is a warning on the group screen, never
a refusal in the app service.

**Fees — no, and not even a little.** A resit fee is the commonest real gate on a
retake, which makes it look like it belongs beside the retake rule. It does not.
Payments bring reconciliation, refunds, tax, currency and an audit trail, and the
centre's existing system already holds them. Our equivalent is
`Assignment.MaxAttempts`, which is already there: the centre takes the money and
then issues the assignment. Model the permission, never the payment.

**One more, unasked and worth naming: certificates.** `TenantBranding.
CertificateFooter` exists and no certificate does. A class is where somebody will
want to press "print thirty certificates", and that is a real feature with a real
buyer — but it depends on section-aware results and on `Attempt.ExamFormId`
existing first, and it is a document-generation project, not a column. Keep it
out of this change.

---

## What this changes in the business review

**The first customer does not move.** It is still the training academy, and the
class strengthens rather than complicates that recommendation: an intake calendar
is exactly what `StartsOn`/`EndsOn` describe, and pass/fail per level per intake
is exactly the record the academy keeps.

**Three corrections to `business-review.md`, which is now stale in the buyer's
favour.** §1's headline claim — "there is presently no end-to-end demonstration
of this product, not a partial one, none" — is no longer true. `take/`,
`candidates/` and `assignments/` all have real components, and `review/` — the
last `PlaceholderComponent` — is being built as this is written. §3's screen table lists screens 7,
8 and 10 as "nothing" and they are built. §6's "named forms: domain only" is
stale too — `ExamStructureAppService` has `CreateFormAsync`, `GenerateFormAsync`,
`SetFormQuestionsAsync`, `PublishFormAsync` and `RetireFormAsync`. What is *not*
built is the delivery half, which is precisely what this document asks for.

**The smallest sellable scope does not grow, but its ordering changes.** The
class is not a new story; it upgrades two existing ones — PPL-02 ("group people
into a class or a batch") gains the level and the dates, and ASG-01/ASG-02 gain a
form picker. Neither is new work of consequence.

What *does* move, and this is the honest cost of the owner's proposal: **FRM-04
and FRM-05 become prerequisites rather than follow-ons.** `business-review.md`
§8 lists the `DeliveryMode` branch as step 4, after the taker chain and the
catalogue. The moment a class is presented as "linked to exam forms", that branch
is no longer step 4 — it is the thing that makes the class screen tell the truth.
So:

| Story | Was | Now |
|---|---|---|
| FRM-04 · Sit a fixed form | SHOULD, step 4 | **MUST, and first** — without it every form link is decorative |
| FRM-05 · Spread a cohort across forms | SHOULD, not built | **MUST** — it is where the rotation and retake rules live |
| FRM-06 · Guarantee a retake differs | SHOULD | **satisfied by FRM-05**, not by a new entity |
| FRM-07 · Know how worn a paper is | SHOULD | unblocked by the `TimesUsed` write, which is one line |
| PPL-02 · Group people into a class | MUST | unchanged in scope; gains `LevelId`, dates, `IsActive` |
| ASG-01 / ASG-02 · Assign | MUST | gains one optional form field on the DTO and the screen |
| CAT-01 · Name the vocabulary | MUST | **moves up.** Shipping a class screen labelled "Group" to an academy, or "شعبة" to a recruiter, is the avoidable half of a lost meeting, and `CategorySet` has no app service today |

Net: the proposal adds roughly a day of entity work and pulls perhaps a week of
delivery work forward. That is a good trade and it should be made deliberately,
not discovered when the first coordinator asks why everyone got a different
paper.

---

## Open questions

1. **When a candidate belongs to two classes, which one owns the result?** Not
   urgent — it becomes urgent the day a results screen wants one heading per
   student. The likely rule is "the active group whose level matches the exam's",
   but it should be decided with a real roster in front of us, not now.

2. **What happens when `RotateForms` runs out of forms a candidate has not sat?**
   FRM-06.2 says warn at assignment time. But the third attempt on a two-form
   exam is a real case, and there are only two honest answers: refuse the attempt,
   or repeat a form and record that it was repeated. My inclination is to repeat
   and record — refusing punishes the candidate for the centre's shortage — but
   it is the coordinator's policy, not ours, and it may want to be a setting.

3. **Should a named form be section-aware?** `ExamFormQuestion` carries no
   `ExamSectionId`, and neither does `AttemptQuestion`. So a fixed form of a
   four-skill placement test cannot preserve its section boundaries, which means
   named forms and sections — the two features this quarter — do not compose.
   This is the largest gap this document found outside its own subject and it
   should be answered before either ships in a version a language centre sees.

4. **Do we keep `Exam.FixedFormId` once `Assignment.ExamFormId` exists?** Two
   places to name the same thing, with the assignment winning. It is defensible
   as an exam-level default, and it is also exactly the kind of duplicate that
   goes stale when a form is retired. I lean toward keeping it, because a
   certification body genuinely wants "this exam is always Form 3" without
   restating it on every assignment — but it needs the retirement guard in
   FRM-03.3 to be real, or it fails silently.

5. **Is `TimesUsed` per form the exposure metric we want, or is it per item?**
   `research-2026-08.md` §4.2 argues the real metric is exposure rate per item,
   times served ÷ candidates. Per form is cheaper, comprehensible without a
   psychometrician, and available the moment `Attempt.ExamFormId` exists. Both,
   eventually. Per form, first.

6. **Is the owner asking for a taught section or an assessment cohort?** The
   terminology evidence says شعبة is a taught section with a cap and a schedule.
   This document gives him a roster with a level and a term, and deliberately
   withholds the timetable, the instructor and the capacity. That is the right
   product decision and it may not be what he pictured. Worth one direct question
   before the screen is built, because the answer changes nothing in the schema
   and everything in how the screen is introduced.

---

## Sources

Marked **verified** where the primary page was read and quoted, **asserted**
where the claim rests on a secondary source or on inference from a verified one.

**How systems model classes and assessments**

- [Canvas Assignments API — `AssignmentOverride` field list](https://canvas.instructure.com/doc/api/assignments.html) — *verified.* Who × when; no content field.
- [Canvas Quiz Assignment Overrides](https://canvas.instructure.com/doc/api/quiz_assignment_overrides.html) — *verified.* Same shape.
- [Moodle — Quiz overrides](https://docs.moodle.org/en/Quiz_overrides) — *verified.* Password, dates, time limit, attempts; no version field.
- [Moodle — Groupings](https://docs.moodle.org/en/Groupings), [Cohorts](https://docs.moodle.org/en/Cohorts) — *verified.* Three separate constructs; a cohort is a bulk-enrolment device carrying no content.
- That both products achieve per-section content by duplicating the assessment — *asserted*, but it follows directly from the verified schemas.

**Forms, equating and how one is assigned**

- [Professional Testing — developing multiple forms](https://www.proftesting.com/test_topics/steps_7.php) — *verified.* Parallel forms, equating, and the spiralled random-groups design quoted above.
- [PSI — Linear-on-the-fly testing](https://www.psiexams.com/knowledge-hub/increase-security-efficiency-linear-on-the-fly-testing-loft/) — *verified.*
- [CompTIA — exam development and ISO 17024 accreditation](https://www.comptia.org/en-us/resources/test-policies/exam-development/) — *verified* for the accreditation and cut-score statements; CompTIA publishes no form counts or assignment rules.
- assess.com (Assessment Systems) — **could not be read**; every page returned 403. `research-2026-08.md` cites it heavily. Nothing here rests on it.
- AERA/APA/NCME *Standards for Educational and Psychological Testing* — **not accessed.** No claim here is attributed to it.

**Retake and resit policy**

- [IELTS — resitting the test](https://www.ielts.org/en-us/for-test-takers/resitting-the-test) — *verified.*
- [IELTS — One Skill Retake](https://ielts.org/take-a-test/booking-your-test/one-skill-retake) — *verified.*
- [ETS — TOEFL iBT test-day FAQ](https://www.ets.org/toefl/test-takers/ibt/faq/taking-the-test.html) — *verified.* Once per 3-day period.
- [Pearson PTE — retake policy](https://www.pearsonpte.com/policy-center/retake-policy/) — *verified.* Effective 8 August 2024.
- [Duolingo — is the Duolingo English Test hard?](https://blog.duolingo.com/is-the-duolingo-english-test-hard/) — *verified* for the item-bank and no-two-tests-identical claims. Retake counts per 30 days are *asserted* and were not found on an official support page.
- [Pearson, W.S. (2023), *Language Testing*](https://journals.sagepub.com/doi/full/10.1177/02655322231186706) — *asserted, secondary.* Source for Cambridge devolving resit policy to centres, and for a PTE five-day figure that the current official page contradicts. Prefer the official page.
- Cambridge English official regulations — **could not be read** (403).

**Placement testing**

- [Seton Hall — about the language placement test](https://www.shu.edu/global-learning-center/about-the-language-placement-test.html) — *verified.* Placement precedes registration; the result is final and cannot be retaken.
- University of Melbourne, Faculty of Arts language placement — *asserted* (403 on fetch; from search summary). Mis-placement corrected by moving the student between classes.
- ELS Language Centers policies — *asserted*, secondary.
- Berlitz and Wall Street English — **no published policy page found.**

**Terminology**

- [King Saud University — Deanship of Admissions and Registration](https://dar.ksu.edu.sa/ar/registration-dept) — *verified.* الشعب الدراسية with capacity, merged and cancelled against courses.
- [Jordanian regulation on licensing private and foreign educational institutions](https://jordanianlaw.com/الأنظمة/نظام-تأسيس-وترخيص-المؤسسات-التعليمية/) — *verified.* Thirty-student cap; شعبة صفية; a fee to add one.
- [Almaany — شعبة](https://www.almaany.com/ar/dict/ar-ar/شعبة/) — *verified* for the general "branch, division" sense.
- Maghreb usage as a study stream — *asserted*, secondary. Egyptian and non-Saudi Gulf usage — **not verified**; the reading above is extrapolated from Jordan and Saudi Arabia.
