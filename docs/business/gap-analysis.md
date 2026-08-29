# Gap analysis: what is missing to sell this to the first customer

**Written 2026-08-29, against `b7abe4e`.** Every claim was checked by opening the
file. Day estimates are estimates and are labelled as such; the *ordering* is the
argument, and I am more confident of it than of the numbers.

**The buyer this is ranked for:** a private vocational training academy or a
language centre, buying the **end-of-level exam** — one paper, one score, one pass
mark. Not the placement test; that needs a section-by-section profile and sections
do not reach delivery (gap 10).

---

## What changed, so the ranking is read against the right baseline

A month ago the walk from "sign up" to "read the results" broke at eleven of
thirteen steps. It now breaks at four of fifteen. The catalogue, the shared bank's
route in, named forms end to end, classes at a level, the review queue, the results
roster, item analysis, staff accounts and tenant settings all shipped and all work
from a browser.

**The remaining gaps are almost all small, and three of them are the same defect
wearing different clothes.** That is the good news and also the warning: the
project keeps shipping two correct halves with nothing testing the seam between
them.

---

## The ranking

### 1. There is no deployable product
**Blocks: any sale, at any price.** Est. 3–4 days.

No Dockerfile, no compose file, no CI configuration, no deployment manifest, no
installer. Both `environment.ts` **and** `environment.prod.ts` hardcode the app to
`http://localhost:4200` and the API to `https://localhost:44373`. SMTP points at
`127.0.0.1:25` with no credentials, so no invitation has ever been delivered.

Ranked first because it is the only gap that is not about the product. Everything
below can be demonstrated on a laptop; none of it can be *sold*, because there is
nothing to install and no address to send a customer to. It also gates gap 2:
serving the app and the API from one origin removes half of that defect for free.

`competitive-position.md` says "it can be deployed where the data must stay" is a
differentiator against TAO. It is not a weakened claim today; it is an unbuilt one,
and it should not be said in a meeting.

### 2. Nothing the server stores reaches the browser
**Blocks: the demo, the listening exam, the logo, and the file they paid for.**
Est. 1–2 days.

Seven places build a URL the browser fetches for *itself* — an `img`, an `audio`, a
`video`, a download link — and every one is origin-relative and carries no
credential. Symptoms: the author's media preview, the candidate's question image
and audio, the exam entry page's logo, the staff shell's logo, the hotspot image,
the reviewer's link to an uploaded answer, and the results CSV export.

Two independent causes, both cheap:
- **Origin.** The app and the API are on different origins with no proxy, so the
  browser asks the wrong server. Fixed by gap 1, or by prefixing the API base the
  way `RestService` already does for every XHR.
- **Credential.** A browser media request carries no `Authorization` header. The
  candidate path was solved correctly — a signed grant naming one blob, expiring
  with the attempt — and the staff paths carry nothing, so an author's preview is
  an anonymous request against a permission check and returns 404. The export is
  anonymous against `[Authorize]` and returns 401.

Ranked second because of what it costs commercially rather than what it costs to
fix. A language centre's entire product is listening; a vocational academy's exam
is full of diagrams. And the opening sales argument — *your Google Form lost the
chart, and we keep it* — is currently a live demonstration of our own product
losing the chart. The export is the thing the coordinator actually bought.

**It survived two reviews because both sides are tested and neither test crosses
the seam:** the browser suite stubs this exact URL, so it asserts our own mock is
reachable; the live-backend suite fetches the blob with an API client carrying a
token, which no `<img>` tag can do. The fix must land with a browser test that
renders a question and asserts the image request returned 200.

### 3. A correct answer is silently scored zero
**Blocks: nothing at demo time. Ends a pilot with a complaint.** Est. 1–2 days.

`fill-in-the-blank` is wired to a plain textarea that emits a bare string. Its
grader parses a map of blank id to answer, fails to read it, and returns **wrong** —
not "send to a person". The candidate can only score by typing JSON into an exam,
which breaks the product's founding rule at the worst possible point.

Three further types — hotspot, file upload, spoken answer — have no answer control
at all and fall back to the same textarea. `business-review-2.md` §7 recommends
deleting hotspot and parking the other two; that recommendation still stands and
would close most of this by removal rather than by building.

Ranked third, above the importer, because it is the only item on this list that
takes marks away from a real student in front of the first customer. Every other
gap disappoints; this one produces a wrong result the centre will defend to a
parent.

The fix is small. **The test is the point**: a matrix over `QuestionTypes`
asserting that each registered answer component's emitted shape parses in that
type's registered grader. Neither side alone catches this, which is why 154 green
tests did not.

### 4. There is no way to bring an existing exam in
**Blocks: the trial surviving its second week. Sets the price floor.** Est. 4 days.

No parser, no import screen, no route. A centre holding two hundred questions in a
Word file or a Google Forms export must retype them.

Ranked fourth rather than first because a pilot can be started without it — we
type the first two levels in ourselves, which we should do anyway. It is ranked
this high because **it is not a feature, it is the unit economics.** Onboarding is
realistically 3–5 days of somebody's time per tenant, which is more than the first
two years of infrastructure for that tenant. At roughly $2,000–2,500 a year per
academy *(estimate)*, this does not become a business until we can onboard twenty
tenants without twenty times the effort.

Two things now make this cheaper than it was: the candidate roll importer proves
the team builds this pattern well — dry run, per-line errors, idempotent re-import —
and the destination exists, since the catalogue, bank ownership and topic filing
all landed.

### 5. The invitation does not come from the customer
**Blocks: the first impression on forty students.** Est. 1 day.

The email is a hardcoded bilingual string carrying the candidate's name, the exam
title, the duration, the expiry and a long token link. No organisation name, no
logo, no support address — `TenantSettingsAppService` is not even injected. An
unbranded message with a long token link, sent to a teenager, from nobody they
recognise, is a description of a phishing email.

The settings that would fix it are already saved by a working screen. This is
plumbing two values into a template, plus gap 1's SMTP.

### 6. The coordinator cannot run week one
**Blocks: the pilot, four separate times.** Est. 3 days.

Each of these is small and each is a place a real coordinator stops dead:

- **A person cannot be added or corrected by hand.** The create and update services,
  routes and client methods all exist; nothing calls them. Import is the only door,
  and a typo can only be fixed by deleting the person, which loses their attempts.
- **A link cannot be resent or its expiry extended.** The plaintext token is
  returned once and only its hash is kept, so this is a decision — store it
  encrypted, or accept that a lost link is replaced rather than resent.
- **A staff password cannot be reset, and the form says it can.** The field is
  shown, the client sends it, the update method never touches it. The request
  returns 200 and the administrator tells their colleague a password that does not
  work.
- **Every person needs a unique email address.** An academy where siblings share a
  family address, or where under-16s have none, cannot enter its roll at all.

The last one is the largest and is a schema decision, not a screen.

### 7. Eleven controls that do nothing
**Blocks: nothing. Costs trust the first time one is noticed.** Est. 1 day to hide.

A section's time limit, a qualifying-section flag, a brand colour, and seven of the
nine tenant settings are all editable, all persist, and are read by nothing. A
coordinator can set "Listening: 20 minutes", see it saved, and every candidate gets
the whole exam's clock. An administrator can turn off integrity observation and
still be observed — which is a consent problem, not a configuration one.

Ranked here because it is nearly free and because the failure mode is specific: an
absent feature disappoints, a dead control lies. Hide each one until its mechanism
runs.

### 8. Nothing verifies that permissions work
**Blocks: nothing visible. It is the one that will cost a customer's trust.**
Est. 1–2 days.

`AddAlwaysAllowAuthorization()` is still called in the test base, so not one
`[Authorize]` attribute in the solution is executed by any test. Six declared
permissions turn out to enforce nothing anywhere — which is exactly what an
unexercised authorisation layer looks like from the inside.

Multi-tenant isolation is the honourable exception and is properly covered, which
is the right priority. But "a marker cannot see the answer keys to the whole bank"
is a sentence we say in meetings and have never run a test for.

### 9. Two bindings that finish two screens
**Blocks: a marker and a teacher, daily.** Est. half a day, together.

- The marking screen never renders the model answer. The server renders it, sends
  it, and the Angular model types it; the template binds neither it nor the
  explanation, in 164 lines. A marker works with the answer key in another tab.
- The item-analysis screen tells a teacher which six questions are broken and gives
  them no link to any of them.

Listed separately because they are the cheapest value in the repository and are the
kind of thing that never gets scheduled.

### 10. Sections end to end
**Blocks: the placement test only. Do not build it for this customer.**
Est. 5–8 days.

Sections can be created, named, ordered, timed and given a pass floor. Nothing in
delivery, grading or reporting knows they exist: `AttemptQuestion` carries no
section id, the form builder records none, and grading computes one flat total.
`ExamSection.IsFailedAt` is written, unit-tested and called by nothing.

Ranked last deliberately. **The end-of-level exam wants one number against a pass
mark, and that is exactly what the system produces well.** The competency breakdown
now genuinely returns data — listening 40%, reading 85% — and answers most of the
placement question by topic rather than by section. Sell that as the profile.

The first review's advice to build the taker section-aware from the first line was
right and was not taken; the retrofit is owed and now also owes a section id on
`ExamFormQuestion`. Take it when a customer asks for placement, and price it.

---

## The shortest path

| # | Gap | Est. days |
|---|---|---|
| 1 | A deployment that exists | 3–4 |
| 2 | Files reach the browser | 1–2 |
| 3 | Answers are graded on what was sent | 1–2 |
| 5 | The invitation is theirs | 1 |
| 9 | Two bindings | 0.5 |
| 7 | Hide the dead controls | 1 |
| 6 | Week-one operations | 3 |
| 8 | Test the permissions | 1–2 |
| 4 | The importer | 4 |

**Roughly 16–20 developer-days — three to four weeks with a buffer** *(estimate)*.
Gaps 1, 2 and 3 decide whether a pilot can start; 4 decides whether it survives; the
rest decide whether it is a product or a demo.

---

## The risk that is not on the list

Every item above is engineering, and engineering is the part this team is good at.
**The real risk is that in four weeks there is still no named academy that has
agreed to run a real intake on this.** That conversation costs nothing and should
have started already — and if it cannot be started, the answer is more informative
than any of the days above.

Two things to check in the first two conversations, because either would reorder
this document:

1. **If both academies say their exams are on paper because of invigilation, not
   marking**, the whole differentiator is aimed at a problem they do not have, and
   the buyer becomes whoever assesses people who are not in the room.
2. **If their real files are scanned PDFs rather than the regular Word export we
   modelled gap 4 on**, the content barrier is unchanged, every buyer is equally
   cold, and we should pick the one with the biggest budget rather than the
   shortest path.

---

## What is now true that was not

Worth stating, because the ranking above is a list of what is wrong and the balance
matters:

> A centre can build an exam from questions it owns, filed under its own domains
> and competencies; approve the exact paper before it goes out; send it to a class
> at a level; watch somebody sit it on a phone in Arabic with a server-authoritative
> clock; mark what needs a person; read the roster, the answer sheet and the
> competency profile; and be told which of its questions have stopped measuring.

Every clause of that is demonstrable today. The sentence we still cannot say is the
one about a picture in a question and a spreadsheet at the end — and those are gaps
2 and 2 again.
