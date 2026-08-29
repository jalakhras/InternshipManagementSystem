# Roles

Date: 2026-08-29
Scope: the `Assessment` permission group, turned into the five roles the three customer organisations actually run on.

Until this document there was one role, `Admin`, and it held every permission. That meant no permission in this product had ever been exercised as a *restriction* — only ever as a grant that was always present. A permission that is only ever granted is not a permission; it is a checkbox. The five roles below are the sets that make the tree mean something, and `angular/e2e/live/roles.spec.ts` is the first test in this project that watches one of them be refused.

The roles are seeded per tenant by `InternshipManagementSystemDataSeedContributor.SeedAssessmentRolesAsync`. The accounts that hold them in development are created by `tools/seed-role-users.js`.

---

## A rule that shapes every list below

ASP.NET and ABP **combine** a service's class-level `[Authorize]` with its method-level one using AND. `QuestionAppService` is guarded by `Questions` at the class and `Questions.Create` at the method, so creating a question requires *both*. A role holding only the leaf is a role that reads correctly in the permission screen and is refused on every request.

So every list below is stated as leaves, and the seeder expands each leaf to include every permission it hangs from — walking the definition tree rather than splitting on dots, because `Assessment.IdentityManagement.Users.View` has three dotted prefixes and only two of them are permissions. The counts in each heading are the expanded totals, which is what the database holds.

## A naming decision

ABP's `IdentityRole` has a name and no display name, so the role's name is simultaneously the database key, the string the permission store grants against, the value a script writes down, and the text the user screen renders. It cannot be both a stable key and an Arabic label. The names are therefore English identifiers — `Coordinator`, `Author`, `Marker`, `Observer` — and the Arabic names each role goes by belong in the localisation resource beside every other label, where they can be translated without a migration. This is the first thing in this product the permission model cannot express, and it is recorded again at the bottom.

---

## مدير النظام — `Admin` (65 permissions: everything)

The person who owns the organisation's account. Everything in the `Assessment` group, including the two things nobody else gets: the tenant's settings, and the staff accounts. They are the only role that can create another member of staff and decide what that person may do, which is deliberate — `Users.ManageRoles` is the escalation path this product has already had once, where anybody who could correct a colleague's phone number could tick `Admin` on their own record.

Not listed here, because it is not a list: `GrantAdminPanelAccessToAdminRoleAsync` reads the whole group from the permission definition manager, so a permission added tomorrow is granted without anybody editing a file. That behaviour is unchanged.

## منسّق — `Coordinator` (25 permissions)

```
Assessment.Exams                        (parent, required by ExamAppService)
Assessment.Exams.View

Assessment.Candidates                   (parent)
Assessment.Candidates.View
Assessment.Candidates.Create
Assessment.Candidates.Edit
Assessment.Candidates.Delete

Assessment.Groups                       (parent)
Assessment.Groups.View
Assessment.Groups.Create
Assessment.Groups.Edit
Assessment.Groups.Delete

Assessment.Assignments                  (parent, required by AssignmentAppService)
Assessment.Assignments.View
Assessment.Assignments.Create
Assessment.Assignments.Revoke
Assessment.Assignments.SendEmail

Assessment.Attempts                     (parent, required by AttemptAdminAppService)
Assessment.Attempts.View
Assessment.Attempts.ForceSubmit

Assessment.Results                      (parent)
Assessment.Results.View
Assessment.Results.Export

Assessment.Catalog                      (parent)
Assessment.Catalog.View
```

The person who runs sittings. They hold the roll of candidates and the classes those candidates sit in, they send the exam and can kill a link that leaked, they watch the attempt monitor while people are actually sitting, and they read the roster afterwards and take a copy of it. Everything they do is about *people and a date*; nothing they do is about *what the exam says*. In the sidebar they see Exams (read-only), Candidates, Groups, Assignments, the attempt monitor and Results, and nothing else.

**Where this overlaps the author, and the decision.** The coordinator holds `Exams.View`, which looks like authoring and is not. The assignment picker and both results screens begin by asking which exam, and a coordinator who cannot name an exam cannot send one. What `Exams.View` actually discloses is an exam's shape — title, timing, pass mark, counts — because the questions live behind `Questions`, which the coordinator does not hold at all. That last part is a deliberate refusal rather than an oversight: the answer key is stored in the question payload, and the person who mails forty links is the last person who should be able to read it. So the coordinator can send an exam they cannot read.

**Two things withheld, and why.** `Attempts.Delete` is not granted — ending a sitting that hung (`ForceSubmit`, which they do hold) and destroying the record that it happened are not the same act, and only the administrator does the second. `Results.ViewItemAnalysis` is not granted either: difficulty and discrimination are question-quality statistics, and the coordinator is not the person who acts on them.

## مُعِدّ الاختبارات — `Author` (14 permissions)

```
Assessment.Exams                        (parent)
Assessment.Exams.View
Assessment.Exams.Create
Assessment.Exams.Edit
Assessment.Exams.Delete
Assessment.Exams.Publish

Assessment.Questions                    (parent)
Assessment.Questions.View
Assessment.Questions.Create
Assessment.Questions.Edit
Assessment.Questions.Delete

Assessment.Catalog                      (parent)
Assessment.Catalog.View
Assessment.Catalog.Manage
```

The person who writes the assessment: exams, the question bank, named papers, the blueprint, and the tenant's own vocabulary of categories, levels and topics. They own the catalogue outright — `Catalog.Manage` as well as `Catalog.View` — because a question is tagged to a topic and an exam sits at a level, so an author without the vocabulary is an author writing untagged.

They see no candidate, send nothing, and read no result. That is the whole shape of the role. An author who can see who scored what on the question they wrote has a reason to change the question after the fact, and the tidy version of that story — quietly retiring an item that made the wrong people fail — is the one that damages an assessment product most. `Exams.Publish` is theirs rather than the coordinator's because publishing is a statement about whether the paper is finished, which is an authoring judgement, not a scheduling one.

`Questions.Edit` also carries media upload and deletion (`AssessmentMediaAppService`), which is correct: the listening clip and the diagram are part of the question.

**The one thing this role should have and cannot.** Item analysis — `Results.ViewItemAnalysis` — is question-quality data and is genuinely an author's tool; it is how you find out that option (c) is attracting the strong candidates. It is not granted, because it cannot be. See the last section.

## مصحّح — `Marker` (4 permissions)

```
Assessment.Review                       (parent, required by ReviewAppService)
Assessment.Review.ViewQueue
Assessment.Review.Grade
Assessment.Review.ViewIntegritySignals
```

The review queue and nothing else — the smallest role in the product, and the one whose boundary is sharpest. They open an attempt that is waiting on a human, read the free-text and uploaded answers, award marks and leave comments. They cannot list results, cannot see the roster, cannot see a candidate record, cannot see an exam or a question outside the attempt in front of them. In the sidebar they have one item.

### Can a marker see integrity signals? Yes.

This is the one genuine judgement call in this document, so here is the reasoning rather than the options.

`Review.ViewIntegritySignals` is held separately from `Review.Grade`, and the permission tree says why in its own comment: paste, focus-loss and timing observations are *behavioural data about a person*, not just their answers. That is a real distinction and a good reason for the permission to exist. It is not a reason to withhold it from the marker.

The marker is the only human in the system who reads a written answer and forms a judgement about whether it is the candidate's own work. A paste event in the middle of a 400-word essay is the single most relevant fact available to that judgement. `GetIntegrityReportAsync` is addressed by one attempt id — it is a report about the sitting they already have open, not a browsable record of anybody's behaviour, and the marker has no way to enumerate attempts outside their own queue precisely because they hold no `Results.View`. Withholding it does not protect the candidate; it produces a worse mark, or it sends the marker to ask a coordinator who has not read the answer and cannot interpret the flag.

The alternative — give the signals to the coordinator, who watches sittings happen — fails for exactly that reason, and so the coordinator does *not* hold it. Both halves of that decision are asserted in the spec.

The permission stays separate rather than being folded into `Grade`, and that separation now buys something concrete: an organisation running low-stakes practice, which has already turned `CollectIntegritySignals` off at the tenant level, can untick this from the Marker role without touching their ability to mark. That is what a separate permission is worth, and it only becomes worth anything once a role exists that could plausibly not have it.

## مشاهد النتائج — `Observer` (6 permissions)

```
Assessment.Exams                        (parent)
Assessment.Exams.View

Assessment.Results                      (parent)
Assessment.Results.View
Assessment.Results.Export
Assessment.Results.ViewItemAnalysis
```

A department head, an academic lead, a client who commissioned the screening. They read the roster, the summary, a single result in detail, and the item analysis, and they change nothing anywhere — every write in the product is refused for them.

**`Exams.View` is required, not convenience.** Item analysis is addressed as `/results/item-analysis/{examId}` and is unreachable without an exam id, and the only way to obtain one is the exam list. The roster's exam filter is the same story. An observer without `Exams.View` gets an item-analysis screen that cannot be opened, which is not a narrower role, just a broken one.

**`Results.Export` is granted, and "cannot change anything" survives it.** Exporting is reading; the CSV holds exactly the columns the screen already shows. Withholding it does not withhold the data, it withholds the convenient form of the data, and people respond to that by taking screenshots — which is the same disclosure with no audit trail and worse handling. If an organisation wants the roster to stay on the screen, they untick it, and the seeder will not put it back.

---

## What the permission tree could not express

Four things. Each is a decision that had to be made against the grain of the model rather than with it.

**1. Item analysis cannot be separated from the roster.** `ResultAppService` is guarded by `Results.View` at the class, and `ViewItemAnalysis` nests under `View` in the tree for that exact reason — the two combine with AND, so `ViewItemAnalysis` beside `View` would describe a role that cannot work. The consequence is that "may see how the questions performed" cannot be granted without also granting "may see every named candidate and what they scored." The author should have the first and must not have the second, so the author gets neither. Fixing this properly means moving item analysis onto its own service guarded by its own permission, at which point the author can have it. Recorded rather than worked around: granting the author `Results.View` to reach it would be a much larger disclosure than the feature is worth.

**2. A role cannot have an Arabic name.** Covered above. `IdentityRole` has no display name, so the name is a key and a label at once, and in an Arabic-first product it has to be the key. The Arabic names in this document are not yet rendered anywhere.

**3. `Assignments.SendEmail` cannot be separated from `Assignments.Create` at the endpoint.** It is enforced — an explicit `AuthorizationService.CheckAsync` inside `CreateAsync`, conditional on the `sendEmail` flag in the request body — which is the right design, since the decision depends on what the caller is actually asking for. But it means the split is invisible to anything that reasons about routes, and a role holding `Create` without `SendEmail` gets its refusal half way through a request rather than at the door. The coordinator holds both, so nothing in the seeded set exercises it.

**4. There is no list-of-assignments endpoint.** `Assignments.View` guards `GetLinksAsync`, which is per exam. The sidebar shows an Assignments item on `Assignments.View` and the screen behind it works by exam, so nothing is broken — but "may see what has been sent" is narrower in the product than the permission's name suggests, and a role granted `Assignments.View` alone, without `Exams.View`, could not name an exam to ask about.

## Two defects this work uncovered

Both were found by trying to seed a role per tenant and by asserting a refusal for the first time. Both are fixed; they are recorded here because they explain why the seeder and one controller changed.

**Roles and users were created in the host, never in the tenant.** `IdentityRole`'s tenant id defaults to null and `CreateRoleIfNotExistsAsync` never passed one, so a role created inside `ICurrentTenant.Change(tenant.Id)` was written as a *host* role. The lookup guarding it then could not see what it had just written, because the repository applies the multi-tenant filter and a host row is not in a tenant's results — so every pass for every tenant created one more duplicate in the host and none anywhere else. The database held 19 roles named `Supervisor`, 19 named `Trainee`, and 19 copies each of four seeded user accounts, all in the host, all holding `Admin`, all with a password written in the seeder. The tenant id is now passed, and the legacy internship accounts are scoped to the host — without that second change, the fix would have put a known-password administrator inside all three customer organisations.

**The results export answered 500 instead of 403.** `ResultController.ExportAsync` returns `IActionResult` rather than an object result, and ABP's exception filter converts `AbpAuthorizationException` into a 403 only for object results. Every other refusal in the product came back as 403; this one escaped the filter and surfaced as a server error, so a person without the permission was told the product was broken rather than that they were not allowed. The two permissions are now declared as attributes on the action, so ASP.NET refuses before it runs. This is the sort of defect only a test that expects a *specific* refusal code can see — one asserting merely "not 2xx" would have passed.
