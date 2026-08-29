# Module structure

## The decision

**A modular monolith, split by bounded context and enforced by a test — not by
project count.**

The system has six contexts. The tempting move is to give each one its own set of
projects the way ABP modules are usually packaged, which would produce roughly
twenty-four csproj files. That is the right shape for a framework you distribute
on NuGet and the wrong shape here: it buys independent versioning nobody needs,
and charges for it in build time, navigation friction and ceremony on every
change that crosses two contexts — which, early in a product's life, is most of
them.

What a split genuinely buys is **boundaries you cannot cross by accident**. That
is available without the project sprawl: one folder per context inside each
layer, and an architecture test that fails the build when one context reaches
into another's internals. The enforcement is the valuable half; the project count
was only ever a proxy for it.

Contexts can be lifted into their own assemblies later, one at a time, if a real
reason appears — a separate deploy, a separate team, a licensing boundary. Doing
it now would be paying up front for an option that may never be exercised.

---

## The seven contexts

| Context | Owns | Depends on |
|---|---|---|
| **Catalog** | The tenant's own vocabulary: categories, levels, topics, and what it calls its people | — |
| **Authoring** | Exams, question groups, questions, form blueprints | Catalog |
| **People** | Candidates and cohorts | Catalog |
| **Delivery** | Assignments, links, attempts, answers, integrity signals | Authoring, People |
| **Grading** | Graders per question type, scoring, manual review | Delivery, Authoring |
| **Analytics** | Results, topic breakdown, item analysis | Delivery, Authoring |
| **Tenancy** | How a tenant appears to its own people: name, logo, brand colour | — |

Dependencies point one way only. Catalog knows nothing about exams; Authoring
knows nothing about attempts. A cycle here would mean two contexts are really one
and should be merged rather than wired together.

Tenancy depends on nothing for the same reason Catalog does: it is read by the
shell, the exam page, the certificate and the invitation email, and if it knew
about any of them the arrow would run backwards. It is separate from Catalog
rather than folded into it because the two answer different questions — Catalog
is the vocabulary a tenant assesses with, Tenancy is the face it shows while
doing so, and a tenant changing its logo has no business touching the table that
holds its competency tree.

### Why these lines and not others

They follow the sentences the business actually says.

*"Define what you measure"* is Catalog. *"Write the exam"* is Authoring. *"Add the
people"* is People. *"Send it and let them sit it"* is Delivery. *"Mark it"* is
Grading. *"Tell me what it means"* is Analytics.

Splitting instead by entity — an Exams module, a Questions module — would put a
seam through the middle of a single job, and every change would touch both sides
of it.

---

## Backend

Folders mirror across layers, so a context is one path in each project:

```
src/
  Astrolabe.Domain/
    Assessment/
      Catalog/          Category · Level · Topic · CategorySet
      Exams/            Exam · QuestionGroup · Question · ExamBlueprintRule
      People/           Candidate · CandidateGroup · CandidateGroupMember
      Delivery/         Assignment · ExamLink · Attempt · AttemptQuestion · Answer · IntegritySignal
      Grading/          IQuestionGrader · IGraderResolver · GradeResult

  Astrolabe.Application/
    Assessment/
      Delivery/         ExamTakingAppService · ExamFormBuilder · TakerQuestionProjector · AttemptTimeoutWorker
      Grading/          Graders/ · AttemptGradingService · CorrectAnswerRenderer
      Review/           ReviewAppService
      Media/            AssessmentMediaAppService

  Astrolabe.Application.Contracts/
    Assessment/         one folder per context, DTOs and interfaces only

  Astrolabe.EntityFrameworkCore/
    Assessment/         one partial DbContext method per context
```

### The rules a test enforces

1. **No cross-context entity reference except downward.** Delivery may hold an
   `ExamId`; Authoring may not hold an `AttemptId`.
2. **Cross-context reads go through Application, never repository to repository.**
   A context's persistence is its own business.
3. **Every entity under `Assessment` implements `IMultiTenant`.** Already enforced
   by reflection in `TenantIsolationTests`, and that test exists because the
   original codebase had multi-tenancy switched on with not one entity
   implementing it — users were separated while their data was not.
4. **Contracts never reference Domain.** A DTO that leaks an entity drags the
   whole model onto the wire.

---

## Frontend

The same contexts, plus one hard split that is *not* a context: the taker's
application.

```
angular/src/app/
  core/                singletons — direction, permissions, API clients
  shared/ui/           presentational components with no data access
  layout/              the shell: top bar, sidebar, drawer
  features/
    dashboard/
    catalog/           Catalog
    exams/             Authoring
    candidates/        People
    assignments/       Delivery, staff side
    review/            Grading, staff side
    results/           Analytics
    take/              Delivery, taker side — see below
```

### Why `take/` is separated hardest

Every other feature is a lazy route inside the shell, behind a login. `take/` is a
different application wearing the same build:

- **No account.** The person has a link, not a session. Their entire credential is
  a token exchanged once for a claim on a single attempt.
- **No shell.** No sidebar, no navigation, nothing to click away to. Someone
  sitting a timed exam should see the exam.
- **A different failure cost.** Every other screen can be reloaded. This one is
  used once, under a countdown, and a defect costs a real person their marks.

It is kept structurally separate so nothing from the staff application can leak
into it — an import from `layout/` or from a staff feature is visible in review
rather than merely discouraged.

---

## What would justify splitting further

Concrete triggers, so the decision gets revisited on evidence rather than taste:

- **A separate deploy for the taker surface.** It is anonymous and
  internet-facing while the rest is authenticated; different rate limits and a
  different edge posture would be a real reason to lift `Delivery` out.
- **A second team.** Boundaries that cost nothing to one developer start costing
  something to two.
- **Selling a context on its own** — the grading engine, say — which needs a real
  package boundary rather than a folder.

None of these is true today.
