# Gap analysis: what is missing to sell this to the first customer

**Pinned to `0842cc9`, 2026-08-29.** Every claim was checked by opening the file.
Day estimates are estimates and are labelled as such; the *ordering* is the
argument, and I am more confident of it than of the numbers.

**The buyer this is ranked for:** a private vocational training academy or a
language centre, buying the **end-of-level exam** — one paper, one score, one pass
mark. Not the placement test; that needs a section-by-section profile, and sections
do not reach delivery (gap 9).

**A warning about this document's shelf life.** Four commits landed while it was
being written, and they removed what had been ranked second — the media and export
URLs — along with two authorisation holes and the last dead navigation links. The
ranking below is the state after that. Re-derive before acting on it.

A second wave — an attempt monitor, and form rotation on a retake — arrived in the
working tree while the ranking itself was being typed. Neither touches a gap below.
I checked the three most fragile claims against the tree as I closed: the invitation
is still unbranded, the blank-filling answer is still scored zero, and the test base
still allows every authorisation.

---

## Baseline

A month ago the walk from "sign up" to "read the results" broke at eleven of
thirteen steps. It now breaks at two of seventeen. **Fifty of seventy-five MUST
stories are BUILT, against seventeen at the last review.**

The engineering that remains is small. What is left is mostly not features.

---

## The ranking

### 1. There is no deployable product
**Blocks: any sale, at any price.** Est. 3–4 days.

No Dockerfile, no compose file, no CI configuration, no deployment manifest, no
installer. Both `environment.ts` **and** `environment.prod.ts` hardcode the app to
`http://localhost:4200` and the API to `https://localhost:44373`. SMTP points at
`127.0.0.1:25` with no credentials, so no invitation has ever been delivered.

First because it is the only gap that is not about the product. Everything below
can be demonstrated on a laptop; none of it can be *sold*, because there is nothing
to install and no address to send a customer to.

`competitive-position.md` offers "it can be deployed where the data must stay" as
the answer to TAO Community Edition, which is free, open source and self-hostable.
That is not a weakened claim today — it is an unbuilt one, and it should not be
said in a meeting. TAO ships a product you can install; we ship a solution file.

Note what this also unblocks: serving the app and the API from one origin removes a
whole class of defect that has already cost this project a week.

### 2. A correct answer is silently scored zero
**Blocks: nothing at demo time. Ends a pilot with a complaint.** Est. 1–2 days.

`fill-in-the-blank` is wired to a plain textarea that emits a bare string. Its
grader parses a map of blank id to answer, fails to read it, and returns **wrong** —
not "send this to a person". The candidate can only score by typing JSON into an
exam, which breaks the product's founding rule at the worst possible point.

Three further types — hotspot, file upload, spoken answer — have no answer control
at all and fall back to the same textarea. `business-review-2.md` §7 recommends
deleting hotspot and parking the other two; that still stands and would close most
of this by removal rather than by building.

Ranked here, above the importer, because **it is the only item on this list that
takes marks from a real student in front of the first customer.** Every other gap
disappoints; this one produces a wrong result the centre will have to defend to a
parent, and it is invisible until then.

The fix is small. **The test is the point:** a matrix over `QuestionTypes`
asserting that each registered answer component's emitted shape parses in that
type's registered grader. Neither side alone catches this, which is why 154 green
tests did not.

### 3. There is no way to bring an existing exam in
**Blocks: the trial surviving its second week. Sets the price floor.** Est. 4 days.

No parser, no import screen, no route. A centre holding two hundred questions in a
Word file or a Google Forms export must retype them.

Not first, because a pilot can start without it — we type the first two levels in
ourselves, which we should do anyway. This high, because **it is not a feature, it
is the unit economics.** Onboarding is realistically 3–5 days of somebody's time per
tenant, which is more than the first two years of infrastructure for that tenant.
At roughly $2,000–2,500 a year per academy *(estimate)*, this is not a business
until we can onboard twenty tenants without twenty times the effort.

Two things make it cheaper than it was: the candidate roll importer proves the team
builds this pattern well — dry run, per-line errors, idempotent re-import — and the
destination now exists, since the catalogue, bank ownership and topic filing all
landed.

### 4. The invitation does not come from the customer
**Blocks: the first impression on forty students.** Est. 1 day.

The email is a hardcoded bilingual string carrying the candidate's name, the exam
title, the duration, the expiry and a long token link. No organisation name, no
logo, no support address — `TenantSettingsAppService` is not even injected.

An unbranded message with a long token link, sent to a teenager, from nobody they
recognise, is a description of a phishing email. The settings that would fix it are
already saved by a working screen and already reach the exam page; this is plumbing
two values into a template, plus gap 1's SMTP.

### 5. The coordinator cannot run week one
**Blocks: the pilot, four separate times.** Est. 3 days.

Each is small; each is a place a real coordinator stops dead.

- **A person cannot be added or corrected by hand.** The create and update services,
  routes and client methods all exist; nothing calls them. Import is the only door,
  and a typo can only be fixed by deleting the person, which loses their attempts.
- **A staff password cannot be reset, and the form says it can.** The field is
  shown, the client sends it, the update method never touches it. The request
  returns 200 and the administrator tells their colleague a password that does not
  work.
- **A link cannot be resent, and an expiry cannot be extended.** The plaintext token
  is returned once and only its hash is kept, so resend is a decision — store it
  encrypted, or accept that a lost link is replaced rather than resent.
- **Every person needs a unique email address.** An academy where siblings share a
  family address, or where under-16s have none, cannot enter its roll at all — by
  import or by hand.

The last is the largest and is a schema decision, not a screen.

### 6. Ten controls that do nothing
**Blocks: nothing. Costs trust the first time one is noticed.** Est. 1 day to hide.

A section's time limit, a qualifying-section flag, a brand colour, and seven of the
nine tenant settings are all editable, all persist, and are read by nothing. A
coordinator can set "Listening: 20 minutes", see it saved, and every candidate gets
the whole exam's clock. An administrator can turn off integrity observation and
still be observed — which is a consent problem, not a configuration one.

Here because it is nearly free and because the failure mode is specific: an absent
feature disappoints, a dead control lies. Hide each until its mechanism runs.

### 7. Nothing verifies that permissions work
**Blocks: nothing visible. Two holes this month say what it costs.** Est. 1–2 days.

`AddAlwaysAllowAuthorization()` is still called in the test base, so **not one
`[Authorize]` attribute in the solution is executed by any test.**

This is no longer hypothetical. In the last week, reading — not testing — found a
settings service with no authorisation attribute at all whose ABP-generated
controller let anybody rename the organisation without signing in; a defined
permission that was never checked, so anyone who could edit a colleague's phone
number could make themselves an administrator; class and method attributes
combining with AND rather than override, so a working role could not be expressed;
integrity flag counts reaching anybody who could read a score; and a seeder that
restored deliberately revoked permissions on every deployment.

Multi-tenant isolation is properly covered and is the right first priority. "A
marker cannot see the answer keys to the whole bank" is a sentence we say in
meetings and have never run a test for.

### 8. Three bindings and one flag that finish three screens
**Blocks: a marker and a teacher, daily.** Est. 1 day, together.

- The marking screen never renders the model answer. The server renders it, sends
  it, and the Angular model types it; the template binds neither it nor the
  explanation, in 164 lines. A marker works with the answer key in another tab.
- The item-analysis screen names the six questions a teacher should fix and gives
  them no link to any of them.
- A mark cannot be changed. Marking clears the pending flag and both the queue and
  the answers endpoint filter on it, so reopening a marked attempt shows a blank
  screen. A marker who mistypes a score has no route back.

Listed together because they are the cheapest value in the repository and are
exactly the kind of thing that never gets scheduled.

### 9. Sections end to end
**Blocks: the placement test only. Do not build it for this customer.**
Est. 5–8 days.

Sections can be created, named, ordered, timed and given a pass floor. Nothing in
delivery, grading or reporting knows they exist: `AttemptQuestion` carries no
section id, the form builder records none, grading computes one flat total, and
`ExamSection.IsFailedAt` is written, unit-tested and called by nothing.

Last, deliberately. **The end-of-level exam wants one number against a pass mark,
and that is exactly what the system produces well.** The competency breakdown now
returns real data — listening 40%, reading 85% — and answers most of the placement
question by topic rather than by section. Sell that as the profile.

The first review's advice to build the taker section-aware from the first line was
right and was not taken; the retrofit is owed and now also owes a section id on
`ExamFormQuestion`. Take it when a customer asks for placement, and price it.

---

## The shortest path

| # | Gap | Est. days |
|---|---|---|
| 1 | A deployment that exists | 3–4 |
| 2 | Answers graded on what was actually sent | 1–2 |
| 4 | The invitation is theirs | 1 |
| 8 | Three bindings | 1 |
| 6 | Hide the dead controls | 1 |
| 5 | Week-one operations | 3 |
| 7 | Test the permissions | 1–2 |
| 3 | The importer | 4 |

**Roughly 15–18 developer-days — three weeks with a buffer** *(estimate)*. Gaps 1
and 2 decide whether a pilot can start; 3 decides whether it survives; the rest
decide whether it is a product or a demo.

---

## The pattern behind six of these

Worth naming once, because it has now cost more than any feature on the list.

| Defect | Both sides correct | Nothing crossed between them |
|---|---|---|
| Media route with no controller | service, five callers | no route test |
| BLOB container with no provider | container, writer | nothing activated it |
| `[Authorize]` naming an undefined policy | service, permission tree | nothing ran the policy |
| Origin-relative media and export URLs | grant minting, controller | stub answered our own URL |
| Question positions off by one | screen counts from 1, server from 0 | stub echoed any position |
| The blank-filling answer shape (gap 2) | answer control, grader | no test pairs the two |

Five of six were found by a person reading code. The sixth — every candidate being
served the second question first, live, on the one screen somebody uses once under
time pressure and cannot retry — was found by the live end-to-end suite on its
first run.

**That suite is the most valuable thing shipped this month**, and the lesson beside
it is sharper: the stubbed browser suite could not see the off-by-one because the
stub echoed back whatever position it was asked for. **A stub that answers anything
proves nothing.** Gap 7 is the same lesson applied to authorisation, which is the
last place it has not been.

---

## The risk that is not on the list

Every item above is engineering, and engineering is the part this team is
demonstrably good at — a month took this from two working steps to fifteen.

**The real risk is that in three weeks there is still no named academy that has
agreed to run a real intake on this.** That conversation costs nothing and should
already have started. If it cannot be started, the answer is more informative than
any of the days above.

Two things to check in the first two conversations, because either would reorder
this document:

1. **If both academies say their exams are on paper because of invigilation, not
   marking**, our whole differentiator is aimed at a problem they do not have, and
   the buyer becomes whoever assesses people who are not in the room.
2. **If their real files are scanned PDFs rather than the regular Word export gap 3
   is modelled on**, the content barrier is unchanged, every buyer is equally cold,
   and we should pick the one with the biggest budget rather than the shortest path.

---

## What is now true

The ranking above is a list of what is wrong, so the balance belongs beside it:

> A centre can build an exam from questions it owns, filed under its own domains
> and competencies; write the blueprint the paper is drawn to; approve the exact
> paper before it goes out; send it to a class at a level; watch somebody sit it on
> a phone in Arabic with a clock they cannot cheat; watch the sittings in progress;
> mark what needs a person; read the roster, the answer sheet and the competency
> profile; export it; and be told which of its questions have stopped measuring.

Every clause is demonstrable today. A month ago none of the second half was.
