# Where we win

A benchmark is only useful if it changes what we build. This one is written to
answer a single question: against eight established platforms, what can we
actually beat them at — and what should we stop pretending we will.

Sources are linked. Where a claim could not be verified beyond a marketing page,
it is marked as such rather than repeated as fact.

---

## First, a correction

The competitor list we started from was assembled against an earlier description
of this product: "six question types", "pre-employment assessment". Both are now
wrong. We ship thirteen types, and the product is domain-agnostic by explicit
decision — a language centre, a trading academy and a recruiter all use it for
their own field.

That matters because **five of the eight platforms on the list are technical
hiring tools**: HackerRank, Codility, Xobin, TestHike, SkillRank. They are the
right reference class for a coding-screen product and the wrong one for ours. A
language centre placing students into CEFR levels has nothing to learn from a
coding sandbox, and if we benchmark against those five we will build a worse
HackerRank instead of a product they cannot build at all.

The reference class that actually matches us is **item-banking and certification
platforms** — [Assessment Systems](https://assess.com/item-banking/),
Questionmark, ExamSoft, TAO — plus the two general skills platforms on the list,
Mercer Mettl and TestGorilla. Those are the ones this document takes seriously.

---

## What is genuinely worth taking

### 1. An exam is a composition, not a flat list — TestGorilla

The most useful single finding in the research. In TestGorilla an *assessment*
is not a list of questions; it bundles **up to 5 tests, up to 20 custom
questions, and up to 5 untimed qualifying questions**, where each test is a
reusable unit drawn from a question bank
([help centre](https://support.testgorilla.com/hc/en-us/articles/30469765739931-FAQ-Assessment-test-questions)).

We do not have that layer. Our `Exam` holds questions directly. Adding a
**section** between them is what makes one model serve all three of our
scenarios:

| Tenant | Exam | Sections |
|---|---|---|
| Recruiter | "Backend Developer" | Coding · Logic · English · role-specific questions |
| Language centre | "English A2" | Listening · Reading · Grammar · Writing |
| Trading academy | "Level 1" | Chart reading · Risk management |

The four language skills are the case that proves it: a placement test that
reports one number is useless, because a student strong in reading and weak in
listening needs a different class from the reverse. Sections are what make the
result actionable, and they are also where per-section timing belongs — a
listening section is timed differently from an essay.

**Qualifying questions** are the second idea here and cost almost nothing: an
untimed pass/fail gate before the exam proper. "Have you completed Level 1?"
"Do you hold a work permit?" A candidate who fails one is turned away in thirty
seconds instead of after an hour, and the reviewer's queue never sees them.

### 2. Competencies are organised by domain, role and level — Mercer Mettl

Mettl builds on competencies and sub-competencies indexed by **job function,
role, level and industry**, and will design a custom framework per client
([Mettl](https://mettl.com/competency-based-assessments/)).

This validates a structure we already had and exposes the gap in it. Our `Topic`
entity is already a hierarchy — a competency tree. But `Level` and `Topic` were
flat lists shared across the whole tenant, so a tenant assessing both software
roles and English levels was offered "QA Engineer" under "English Language".
Both are now scoped to a `Category`, which is what makes the tree mean something.

The part worth copying is the *custom framework per client*. Ours is
tenant-defined already, which is the same idea without the consulting engagement.

### 3. Items are reusable objects, and exposure kills them — Assessment Systems

Item banking treats a question as an object reused across many forms, with usage
tracked **across forms**, because over-exposure erodes validity
([Assessment Systems](https://assess.com/what-is-item-banking/)).

A correction to an earlier draft of this document: it stated a working rule of
"a bank roughly three times the form length", attributed to that page. **The page
does not say that.** The number came from a secondary source and was written here
as though it were the primary one. The publish-time warning built on it still
catches the real problem — a bank barely larger than the form draws nearly the
same paper every time — but the threshold is ours, not anyone's standard, and it
should eventually be replaced by an exposure rate per item, which is what the
literature actually measures.

This was our deepest structural gap and it is now closed: `Question.ExamId` is
nullable, and a bank question is owned by a domain and level instead. Three
forms for A1 are three blueprints over one bank, not three copies of forty
questions. Copies drift — a key corrected in one form stays wrong in the others,
and item statistics gathered against a copy describe a question nobody else
uses. Both rules are now publish-time warnings, alongside a new `TimesServed`
counter that measures exposure rather than answers.

### 4. The candidate has no account — SkillRank, TestHike

Both are described as letting a candidate enter through a secure link with no
account. We decided this independently and it is already how `ExamLink` works.
Worth noting as validation, not as something to copy.

*(SkillRank's site is entirely client-rendered and returned no readable content,
so nothing beyond that could be verified. It is a small, new product and not a
serious benchmark.)*

### 5. What we should decline

**A 350-test off-the-shelf library.** TestGorilla's library is its product. For
us it would be a liability: it commits us to authoring and maintaining content
across every field we claim to serve, in every language, forever. Our position
is the opposite one — see below.

**Competing on coding execution.** HackerRank and Codility have spent a decade
on sandboxed multi-language execution. Our `code` question type routes to a
pluggable grader; if a tenant needs real execution we integrate a runner. Trying
to beat them at it would consume the whole roadmap to serve one vertical.

---

## Where we actually win

None of the eight can follow us into these, for structural reasons rather than
because they have not got round to it.

### Arabic is a first-class language, not a translation

Every platform on the list is English-first. RTL, if present at all, is a
stylesheet flip applied late. We built the other way: logical properties
throughout, an Arabic-first type stack, letter-spacing suppressed for a
connected script, and RTL layout bugs caught by tests that run the whole suite
at a phone viewport in Arabic. Three real layout defects were found that way,
including one where a table scrolled the entire page sideways.

An organisation in the region does not want an English product with Arabic
strings pasted in. This is the difference between a tool their staff tolerate
and one they can put in front of a candidate.

### One product, any field

All eight are hiring products. Mettl and TestGorilla stretch furthest, and both
still assume the buyer is hiring. None serves a language centre, a trading
academy and a recruiter with the same deployment.

We do, and the mechanism is already in the schema: `CategorySet` lets a tenant
rename the vocabulary itself — what we call a candidate, a group, a category —
so the product speaks the tenant's language rather than making them speak
recruitment. A training centre sees students and cohorts; a recruiter sees
candidates and pipelines. Same tables.

### The bank belongs to the tenant

TestGorilla's value is *their* library. That is precisely wrong for an
institution whose exam questions are its intellectual property — a university, a
certification body, a training centre that spent years writing its bank. For
them a shared library is not a feature, it is a reason not to buy.

Our position: your questions, your bank, your statistics, your brand, exported
on request. Tenant branding — name, logo, one brand colour flowing through the
token layer to the exam page, the certificate and the invitation email — is
built for the same reason. A candidate invited to sit an exam is being asked to
trust the organisation that invited them. If the page carries our name instead
of theirs, the invitation reads as phishing.

### Psychometrics the tenant can see

We keep `DifficultyIndex` and `DiscriminationIndex` per item and update them
from attempt data. A discrimination index at or below zero means the question
measures the opposite of what it claims and should be pulled. Hiring platforms
generally hide this or do not compute it; certification platforms charge for it.

Surfacing it plainly — "these six questions are not measuring anything" — is
cheap for us and is the single most credible thing we can show an assessment
professional.

### It can be deployed where the data must stay

A modular monolith on .NET and SQL Server installs inside a ministry, a
university or a bank. Every platform on the list is SaaS-only. In this region
data residency is frequently not negotiable, and being installable is sometimes
the entire reason a deal closes.

---

## What this changes in the build

Ordered by what it unlocks, not by effort.

1. **`ExamSection`** — a named, separately scored, optionally timed part of an
   exam, each carrying its own blueprint. Blueprint rules move from exam to
   section. This is the composition layer, and the four-skills language case
   does not work without it.
2. **Qualifying questions** — an untimed gate before the exam. Cheap, and it
   protects the reviewer's queue.
3. **Per-section results** — the score breakdown becomes a profile rather than a
   number. Feeds the certificate and the recruiter's comparison view.
4. **Bank browser** — the bank now exists in the schema; it needs a screen that
   filters by domain, level, competency, difficulty and exposure, and shows the
   item statistics next to each question.
5. **Item health view** — questions flagged by discrimination index, exposure or
   an unfilled blueprint rule, in one list an author can act on.
6. **Branding screen** — name, logo, colour, certificate footer, support address.
7. **Candidate comparison** — several candidates against one exam, by section.
   This is the screen a recruiter actually lives in, and TestGorilla's ranking
   view is the reference.

---

## Sources

- [TestGorilla — FAQ: assessment & test questions](https://support.testgorilla.com/hc/en-us/articles/30469765739931-FAQ-Assessment-test-questions)
- [TestGorilla — guide to creating an assessment](https://support.testgorilla.com/hc/en-us/articles/9027624892315-Guide-to-creating-an-assessment)
- [Mercer Mettl — competency-based assessments](https://mettl.com/competency-based-assessments/)
- [Assessment Systems — what is item banking](https://assess.com/what-is-item-banking/)
- [Assessment Systems — item banking platform](https://assess.com/item-banking/)
- [HackerRank Screen](https://www.hackerrank.com/products/screen)
- [Codility — choosing a technical assessment platform](https://www.codility.com/how-to-choose-technical-assessment-platform/)
- [Testlify](https://testlify.com/)
