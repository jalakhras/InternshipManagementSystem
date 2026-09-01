# Test audit — Astrolabe

**Date:** 2026-08-31
**Question asked:** not "do these tests pass" — they do — but **"can they fail?"**
**Method:** every test file read. Backend findings marked PROVEN were established by mutation: the
solution was copied to a scratchpad, production behaviour was deleted, and the suite was re-run.
Nothing in the repository was modified. Browser findings marked PROVEN were established by reading
the stub, the Angular source and the ABP library source together.

**Repository state at audit:** `HEAD = e8a891a`, working tree dirty (7 modified files under `src/`
and `test/`, an in-progress `SetGroupMembers` → `ChangeGroupMembers` refactor). The task named
`1902a71`; the tree has moved since.

**Not audited:** `angular/e2e/pagination.spec.ts` (8 tests) and `angular/src/app/shared/ui/pager.component.ts`
were created *during* this audit and are excluded from every count and finding below.

---

## Counts

| Suite | Declarations | Executions |
|---|---|---|
| `test/…Application.Tests` | 106 `[Fact]`/`[Theory]` | **193** |
| `test/…EntityFrameworkCore.Tests` | 172 | **165** |
| `test/…Domain.Tests` | 11 | **10** |
| **Backend total** | | **368** |
| `angular/e2e/**` stubbed | 154 `test()` | ~295 (×2 projects, minus 4 skips) |
| `angular/e2e/live/**` | 34 `test()` | 34 |
| **Browser total** | **188** | ~329 |
| Angular unit tests (`src/**/*.spec.ts`) | **0** | 0 |

**556 test declarations audited.** `npm test` (`ng test`) runs nothing: there is not one unit test
for any Angular service, signal, or computed value in the product. Every claim about client-side
logic rests on Playwright driving a stub.

---

## Summary — ranked by false assurance bought

A green test guarding nothing is worse than no test, because it stops anyone looking again. Ranked
by how much a reader would wrongly conclude from the tick.

| # | Test | Verdict | What can be deleted and leave it green |
|---|---|---|---|
| 1 | `AuthorizationCoverageTests.Every_defined_permission_is_enforced_somewhere` | **PROVEN by mutation** | `[Authorize(Attempts.View)]` **and** `[Authorize(Attempts.Delete)]` — the two defects its own docstring names |
| 2 | `staff-journey.spec.ts` → *an exam link still opens in a browser that holds a stale staff session* | **PROVEN by reading ABP source** | the entire deep-link fix; the setup is a no-op |
| 3 | `CandidateStatusTests.An_organisation_that_releases_results_itself_shows_the_candidate_none` | **PROVEN by mutation** | the early return that withholds the score — it leaks in full and the test passes |
| 4 | `ExamStructureTests.The_same_seed_produces_the_same_paper` | **PROVEN by mutation** | the seed. `var seed = 0;` passes |
| 5 | `ModuleBoundaryTests.No_context_depends_on_one_it_should_not_know_about` | **PROVEN by mutation** | nothing needed — it is structurally always empty |
| 6 | `ContractBoundaryTests.No_contract_exposes_a_domain_entity` | **PROVEN by construction** | nothing — the violation it forbids is a compile error |
| 7 | `NamedFormDeliveryTests.A_rotating_sitting_gives_a_retake_a_different_paper` | **PROVEN by reading** | the whole rotation feature |
| 8 | `NamedFormDeliveryTests.A_matching_question_…_does_not_arrive_in_its_authored_order` | **PROVEN by reading** | the shuffle — the answer key ships to the candidate and it passes |
| 9 | `CatalogTests.An_unused_domain_takes_its_levels_with_it` | **PROVEN by mutation** | the level deletion — levels are orphaned, test is green |
| 10 | `ExamStructureTests.An_exam_can_be_split_into_the_four_skills` | **PROVEN by mutation** | `.OrderBy(s => s.DisplayOrder)` |
| 11 | `staff-journey.spec.ts` → *there is a way to put a person into a group* | **PROVEN by reading** | every control on the candidates screen |
| 12 | `TenantIsolationTests` — 6 of 7 | **PROVEN by reading** | `TenantId = tenantId` from 8 of 9 entity constructors |
| 13 | `QuestionAuthoringTests` `DiscriminationIndex.ShouldBeNull()` | **PROVEN by reading** | the half of the reset that clears it |
| 14 | `live/roles.spec.ts:411` `not.toBe(403)` | **PROVEN by reading** | passes on 404, 500 and 401 — including the undefined-policy 500 the file exists to catch |
| 15 | `ShouldAllBe` without a count guard (3 sites) | **PROVEN by reading** | serve zero questions |
| 16 | `live/screenshot.spec.ts` | **Not a test** | 1 of the 34 "live tests" asserts nothing behavioural |

---

## 1. Proven by mutation

Four production behaviours were deleted in a scratchpad copy of the solution. All four target tests
stayed green. Byte-identical source was confirmed between the repository and the copy for every file
touched.

```
Baseline (scratchpad copy, unmutated)
  Application.Tests            193 passed
  Domain.Tests                  10 passed
  EntityFrameworkCore.Tests    163 passed
```

### 1.1 `Every_defined_permission_is_enforced_somewhere` cannot see a lost permission

`test/InternshipManagementSystem.Application.Tests/Permissions/AuthorizationCoverageTests.cs:123-164`

The docstring is explicit about what it exists to catch:

> *"Four of them were found this month — Attempts.View, .ForceSubmit, .Delete and Users.ManageRoles."*

**Mutation:** delete `[Authorize(InternshipManagementSystemPermissions.Attempts.View)]`
(`AttemptAdminAppService.cs:52`) and `[Authorize(…Attempts.Delete)]` (`:130`).
**Result: 193 passed, 0 failed.** Two of the four named regressions are undetectable today.

The cause is the escape hatch at line 208:

```csharp
private static bool EnforcedInCode(string permission)
{
    var leaf = permission.Split('.').Last();
    return ApplicationSources.Value.Any(source => source.Contains($".{leaf});") || source.Contains($".{leaf})"));
}
```

Matching is by **leaf name across the whole Application source tree**. `.View)` occurs 20 times and
`.Delete)` 8 times, so any one `.View` guard keeps all nine `*.View` permissions alive. Of the 36
non-grouping permissions the provider defines, **25 (69%) cannot be reported unenforced**.

Two further defects in the same method:

- Line 212's first disjunct `source.Contains($".{leaf});")` is a strict substring of the second. It
  can never be the deciding test.
- Line 141 reads `InternshipManagementSystemPermissions.cs` into a local `source` that is **never
  used**. The comment above it describes a check that no longer exists.
- `Assessment.Assignments.SendEmail` is kept alive by `if (input.SendEmail)` — a DTO property read,
  not an authorisation check. It is permanently uncatchable.

**Calibration (control):** the sibling test *is* real. Removing the class-level
`[Authorize(Attempts.Default)]` produced `Every_application_service_is_guarded [FAIL]`, 192/193.
So: one half of this file works and one half does not. `Every_policy_named_in_an_attribute_is_defined`
also works — 42 real policies checked against the real provider.

### 1.2 The withheld-score test passes with the score fully leaked

`test/…EntityFrameworkCore.Tests/Assessment/CandidateStatusTests.cs:198-200`

```csharp
result.ScoreWithheld.ShouldBeTrue();
result.Score.ShouldBe(0m);
result.TopicBreakdown.ShouldBeEmpty();
```

`ExamTakingAppService.BuildResultAsync` assigns `result.Score` at line 911 and
`result.TopicBreakdown` at 922 — both *after* the `return result;` at line 907. Lines 199-200
therefore assert DTO field initialisers, not behaviour. Doubly so: the fixture's candidate submits
without answering anything, and the fixture's question carries no `TopicId`, so the honest values are
also `0m` and empty.

**Mutation:** delete the `return result;` from the withheld branch, so the score, percentage, pass
verdict and topic breakdown are all populated and returned to a candidate the organisation chose to
withhold them from. **Result: test passed.**

Only `ScoreWithheld.ShouldBeTrue()` is load-bearing. And `ShowResultToCandidate` appears in exactly
one test in the whole repository — **the permitted case, where a candidate should see a real score,
is untested everywhere.**

### 1.3 The seed test cannot tell determinism from "not randomised at all"

`test/…EntityFrameworkCore.Tests/Assessment/ExamStructureTests.cs:198`

Both generations pass `Seed = 7` and their question lists are compared.

**Mutation:** `ExamStructureAppService.cs:280`, `var seed = input.Seed ?? Random.Shared.Next();` →
`var seed = 0;`. **Result: test passed.** The seed input is ignored entirely; every generation on
the system returns the same paper; the test named for seeding is green.

No test anywhere asserts that **different** seeds produce different papers. The seed is also never
persisted on `ExamForm`, so "regenerate the same paper later" is unverifiable.

### 1.4 Deleting a domain does not have to take its levels

`test/…EntityFrameworkCore.Tests/Assessment/CatalogTests.cs:145`

```csharp
(await _catalog.GetCategoriesAsync()).ShouldNotContain(c => c.Id == category.Id);
```

The test creates a category **and a level under it**, deletes the category, and then looks only at
the category.

**Mutation:** delete `await _levels.DeleteManyAsync(levels, autoSave: true);`
(`CatalogAppService.cs:205`). **Result: test passed** — with the level orphaned against a
`CategoryId` that no longer resolves. The test is named `An_unused_domain_takes_its_levels_with_it`.

### 1.5 Two ordering tests whose fixtures make ordered and unordered identical

**`ExamStructureTests.An_exam_can_be_split_into_the_four_skills:62`.** The comment says *"Returned in
the order they are sat, not in the order they were typed"* — but the fixture creates the sections
with `DisplayOrder` 0,1,2,3 **in that same insertion order**, and SQLite returns a table scan in
rowid order.

**Mutation:** delete `.OrderBy(s => s.DisplayOrder)` at `ExamStructureAppService.cs:64`.
**Result: test passed.**

`CatalogTests.A_domain_created_here_is_offered_with_its_levels:58` has the identical shape:
insertion order, `DisplayOrder`, alphabetical-by-code and alphabetical-by-name all coincide on
`cat-a1`/`cat-a2`. Fix for both is one line: create them in reverse.

### 1.6 `ModuleBoundaryTests` has never had anything to find

`test/InternshipManagementSystem.Domain.Tests/Architecture/ModuleBoundaryTests.cs:58-89`

`ReferencedAssessmentTypes` (lines 137-160) inspects property types, constructor parameters and base
type, then keeps only those whose namespace starts with `InternshipManagementSystem.Assessment.`.
**Every cross-context relationship in this domain is a `Guid` foreign key**, whose namespace is
`System`. There are no EF navigation properties across contexts anywhere. The `target == owner`
short-circuit at line 75 is therefore taken on 100% of iterations, and `AllowedDependencies`
(lines 33-54) is consulted 22 times without ever rejecting anything.

**Mutation:** add `public Guid ExamId { get; set; }` to `Assessment/Catalog/Topic.cs` — a textbook
violation, since `AllowedDependencies["Catalog"] = []`. **Result: 10 passed, 0 failed.**

Scope hole on top of that: only `typeof(Exam).Assembly` (Domain) is scanned. `Grading` and
`Delivery` — where cross-context coupling would realistically appear — live in the Application
assembly and are never looked at.

---

## 2. Proven by construction

### 2.1 `ContractBoundaryTests.No_contract_exposes_a_domain_entity` duplicates the compiler

`test/…Application.Tests/Architecture/ContractBoundaryTests.cs:25-60`

The entity set is drawn from `typeof(Exam).Assembly` = `InternshipManagementSystem.Domain`. But:

```xml
<!-- src/InternshipManagementSystem.Application.Contracts/…csproj -->
<ItemGroup>
  <ProjectReference Include="..\InternshipManagementSystem.Domain.Shared\…csproj" />
</ItemGroup>
```

Contracts has **no reference to Domain at all**. A DTO property typed as a domain entity is a
compile error. 78 DTOs and 616 properties are scanned against 21 entities that cannot appear. To make
this test capable of failing you would first have to add the `ProjectReference` — the architectural
violation the test does not check.

*Its sibling `No_taker_facing_contract_carries_an_answer_key` is real* — but its type list is
hand-maintained (lines 71-78), and `PracticeReviewItemDto` (`TakerDtos.cs:346`) sits outside it
carrying `CorrectAnswer` and `Explanation`, reachable from the anonymous candidate service. That is
intentional for practice mode; the point is that the docstring's claim — *"the mistake cannot be made
one careless property at a time"* — is false.

### 2.2 A literal tautology

`test/…EntityFrameworkCore.Tests/Assessment/NamedFormDeliveryTests.cs:242-243`

```csharp
var forms = new[] { first.Id, second.Id };
forms.ShouldContain(f => f == first.Id);
```

A locally constructed two-element array is asserted to contain its own first element. This would hold
if the entire application were deleted. It reads as "rotation used both named forms"; **the test
never asserts which form was served.** The question-id comparison above it (239-240) cannot
distinguish rotation between named forms from two disjoint random draws.

### 2.3 `ShouldNotBeNull` on the field whose *value* is the defect

Same file, line 204:

```csharp
served.OptionOrder.ShouldNotBeNull();
```

The docstring above it says the defect is the right-hand column arriving **in authored order** —
*"left[i] then pairs with right[i] in the JSON the candidate is handed. That is the answer key."*
`ShouldNotBeNull()` accepts `""`, `"[]"`, and the authored order written out verbatim. Set
`OptionOrder = "r1,r2,r3,r4"` on the named-form path and the answer key ships to every candidate with
the test green.

### 2.4 Asserting a field is null that the test never made non-null

`QuestionAuthoringTests.Correcting_a_wrong_key_forgets_what_the_wrong_key_taught_us:353-355`.
The fixture sets `TimesAnswered = 100` and `DifficultyIndex = 0.04m`. `DiscriminationIndex` is never
assigned and is null before the update, so line 355 cannot fail — and a partial reset that clears two
of three statistics is exactly what it is there to catch.

### 2.5 `TenantIsolationTests` — six of seven assert absence with no presence

Every fixture inserts directly through the repository with the tenant id handed to the constructor
(`:54`, `:104`, `:141`, `:163`, …), and every one of those constructors is a bare
`TenantId = tenantId;`. Only `A_tenant_cannot_see_another_tenants_exams` reads back as the owning
tenant (`:62-67`).

Delete `TenantId = tenantId;` from the constructors of `Question`, `Candidate`, `Attempt`,
`ExamLink`, `Category`, `Topic`, `CandidateGroup` and `Answer`. Those rows become host rows; ABP's
filter still excludes them from TenantB's read; **six of the seven tests stay green.** Everything but
`Exam` is protected against under-filtering only, never against a lost tenant stamp.

And no application service is on the path, so nothing here proves that `ExamAppService`,
`CandidateAppService` or `AssignmentAppService` stamp `CurrentTenant.Id` when a real request writes.

*(`Every_assessment_entity_declares_itself_multi_tenant` at `:204-230` is the strongest test in the
file and should be kept.)*

---

## 3. The browser suite

### 3.1 The stale-session regression test never makes the session stale — PROVEN

`angular/e2e/live/staff-journey.spec.ts:262-267`

```js
await theirs.evaluate(() => {
  localStorage.setItem('expires_at', String(Date.now() - 60_000));
  localStorage.setItem('refresh_token', 'no-longer-valid');
});
```

In `@abp/ng.oauth@10.6`, `oAuthStorageFactory()` returns `MemoryTokenStorageService` for any app that
did not start with SSR — which this one is (`app.config.ts:52`, plain `ng serve`). That class's
`keysShouldStoreInMemory` is:

```
access_token, id_token, expires_at, id_token_claims_obj, id_token_expires_at,
id_token_stored_at, access_token_stored_at, abpOAuthClientId, granted_scopes
```

`getItem('expires_at')` returns `this.cache.get('expires_at')` and **never reads localStorage**. The
write is a no-op. The in-memory cache still holds the real, valid, one-hour expiry, so
`refreshToken()` is never called, the code flow is never started, and the deep-link-discarding
scenario is never reproduced. (`refresh_token` is not in the memory list and *is* written — but it is
only consulted once the access token has expired, which it has not.)

The test then asserts that a **valid** staff session can open `/exam/{token}` — a page that is
anonymous anyway. It would pass with the fix removed.

The repository already knows this. `angular/e2e/support/abp-stub.ts:100`:

> *"ABP 10 keeps OAuth tokens in memory (MemoryTokenStorageService), so a session cannot be seeded
> from outside the page — there is no storage to write to."*

Two files, one codebase, opposite beliefs. This is the single most expensive finding in the browser
suite: it is the regression test for a defect the product owner reported, it is the one the team
would point at, and it proves nothing.

*Suggested fix:* drive the expiry from inside the app (advance the clock, or intercept
`**/connect/token` with a 400 on the refresh grant and `**/connect/authorize` to observe the
`redirect_uri`), and assert on the redirect target rather than on the Start button.

### 3.2 "There is a way to put a person into a group" matches the sidebar — PROVEN

`angular/e2e/live/staff-journey.spec.ts:82-87`

```js
const routes = page.locator(
  'button:has-text("Group"), button:has-text("مجموعة"), ' +
  'select[name*="roup"], select[name*="ategory"], a:has-text("Groups"), a:has-text("المجموعات")');
await expect(routes.first()).toBeVisible({ timeout: 20_000 });
```

`app/core/navigation.ts:47` declares `{ labelKey: '::Nav:Groups', route: '/groups' }`, and the
localisation is `Nav:Groups → "Groups"` (en) / `"المجموعات"` (ar). The shell renders that as an
`<a>` on every screen including `/candidates`. **Both language alternatives are satisfied by the
sidebar link.** The test's own comment says its question is *"whether a coordinator has ANY route to
it"* — and it answers that question with the navigation menu, not with any control on the screen
under test. It cannot fail while the shell renders.

### 3.3 `not.toBe(403)` — the shape the file was written to avoid

`angular/e2e/live/roles.spec.ts:407-411`

```js
const report = await as.get('marker')!.get(`/api/assessment/review/attempts/${attemptId}/integrity`);
expect(report.status(), 'the marker was refused the integrity report').not.toBe(403);
```

Passes on 200, 404, 401 **and 500**. The file's own `refused()` helper (lines 90-102) explains why
that matters:

> *"a 500 is what ASP.NET answers when an `[Authorize]` names a policy nobody defined, which is a
> different defect wearing the same clothes and has shipped here before."*

The file's `allowed()` helper (`status < 400`) exists and was deliberately not used here. The stated
reason — an attempt with no signals legitimately 404s — is soluble: record a signal first, as
`a marker can open an answer somebody uploaded` already does for media.

*The rest of `live/roles.spec.ts` is the strongest file in the repository.* `refused()` asserts
exactly `403`, positive and negative cases are paired, and the media test explicitly builds a real
uploaded blob because *"the endpoint answers 404 for a blob it will not serve **and** for one that is
not there."* That lesson was learned once and then not applied at line 411.

### 3.4 The Arabic half of one absence assertion can never match

`angular/e2e/live/staff-journey.spec.ts:148`

```js
await expect(send.getByText(/nobody in it yet|لا أحد/)).toHaveCount(0);
```

`Assignment:GroupEmpty` in `ar.json` is `"لا يوجد أحد في هذه الشعبة بعد…"`. The alternative `لا أحد`
is not a substring of `لا يوجد أحد`. Under an Arabic-defaulted organisation this assertion is
vacuous. (It is an absence assertion, so the English half is only load-bearing in English.)

### 3.5 One of the 34 "live tests" is a screenshot run

`angular/e2e/live/screenshot.spec.ts` writes PNGs. Its only assertions are that an exam exists, a
Start button renders, and a timer appears. The file says so honestly — *"Not an assertion suite"* —
but it is counted in the headline 34.

### 3.6 The submit dialog's unanswered count is not distinguished from the total

`angular/e2e/take.spec.ts:101-120`. The fixture is `totalQuestions: 2` with **nothing answered**, so
`unanswered == total == 2`, and `expect(dialog).toContainText('2 question')` passes whether the
dialog counts what is unanswered or simply prints the paper length. The test is named
*asks before submitting and counts what is unanswered*. Answer one question and the assertion
discriminates.

### 3.7 137 of 154 stubbed tests run twice and can only differ by accident

`playwright.config.ts` runs every non-live spec under both `desktop` (Chrome) and `mobile`
(Pixel 7). Only 17 tests are viewport-aware: 11 `scrollWidth > clientWidth` overflow checks, 3
`isMobile` guards, 1 explicit `setViewportSize`, 2 phone-drawer tests. The other ~137 assert
identical things at both sizes.

The headline **295 is really ~154 distinct behaviours**. And the desktop half of each of the 11
overflow checks is near-vacuous on its own terms: a seven-column table does not overflow 1280px, so
those 11 buy assurance only in the mobile project.

### 3.8 What the browser suite gets right

Worth naming so the ranking is calibrated. The stubs are unusually honest:

- `abp-stub.ts:95` registers a catch-all 404 **first**, so an unstubbed call fails loudly rather than
  hanging.
- `abp-stub.ts:38-55` and `take-stub.ts:382` read the server's real `ar.json`/`en.json` from disk
  rather than restating them — a missing translation fails a test.
- `take-stub.ts:212` answers with the position it was asked for, refusing to echo the client's number,
  and the comment records the off-by-one that survived when it did.
- `take-stub.ts:154` models `saved` and `isExpired` as non-opposites because the screen once assumed
  they were.
- `take-stub.ts:267` attaches section instructions to the first question only, so a client that shows
  them everywhere fails.
- `take.spec.ts:441-481` pairs the "make the URL absolute" test with "leave a `data:` URI alone" —
  both halves of a rule.
- `contrast.ts` composites through translucent layers, because `toBeVisible()` returns true for white
  on white.

Every one of those is the discipline the findings above are missing. The suite knows how; it applies
it unevenly.

---

## 4. Vacuous on empty, and other narrower faults

`ShouldAllBe` and `ShouldNotContain(predicate)` in Shouldly 4.3.0 **pass on an empty collection.**
Three sites have no count guard:

| Site | Assertion | Mutation that stays green |
|---|---|---|
| `SectionDeliveryTests.cs:338` | `served.ShouldAllBe(q => q.ExamSectionId == listeningId)` | make `DeleteSectionAsync` cascade to `AttemptQuestion` — the exact catastrophe the test guards |
| `SectionDeliveryTests.cs:194` | `paper.ShouldAllBe(q => q.Section!.Name == "Listening")` | serve zero questions when any section is empty |
| `ExamFormBuilderSectionTests.cs:253` | `paper.ShouldAllBe(q => q.ExamSectionId == Listening)` | `ExamFormBuilder.Build` returns an empty list |

The same file gets it right elsewhere — `SectionDeliveryTests.cs:298-299` asserts
`paper.Count.ShouldBe(4)` first. The pattern is known and applied inconsistently.

Others:

- **`AttemptAdminTests.cs:199`** — `A_running_attempt_can_be_discarded_with_everything_it_recorded`
  checks only that the slot rows are gone. Delete
  `await _attempts.DeleteAsync(attempt, autoSave: true);` and it still passes: the sitting survives.
  `IntegritySignal` rows are never deleted by the service at all and are never asserted.
- **`ResultsTests.cs:201-203`** — item analysis uses exactly 20 sittings, the floor. Change
  `if (answered < 20)` to `if (answered < 1)` and it passes. No test uses 19. `TooHard`,
  `NegativeDiscrimination` and `WeakDiscrimination` are asserted nowhere.
- **`QuestionAuthoringTests.cs:148`** — the only `Should.ThrowAsync<BusinessException>` in the
  backend without a `.Code` check. The other 22 all check it.
- **`IntegritySignalTests`** — `report.Signals.ShouldBeEmpty()` appears at lines 103, 132, 162, 222
  and 283, plus a `ShouldNotContain` on an empty list at 320. One mutation
  (`GetIntegrityReportAsync` returns `Signals = []`) turns six tests green. Two of them are named for
  behaviour they do not assert: `One_blocked_paste_is_one_record…` proves *zero* records, and
  `One_event_is_not_described_three_times` asserts `ShouldBeEmpty()`. Line 85's
  `o.Contains("paste")` is case-sensitive and passes on `"Pasted text detected"`.
- **`RoleTenancyTests.cs:84-85`** — the fixture is
  `new IdentityRole(Guid.NewGuid(), "Shared", tenant)` and the assertion is
  `mine.Single().TenantId.ShouldBe(TenantA)`: the test asserts the value it handed in. The docstring
  names the seeder defect it guards; the seeder
  (`InternshipManagementSystemDataSeedContributor.CreateRoleIfNotExistsAsync:116`) is never invoked
  by either test in the file. Revert that line to the two-argument constructor and both stay green.
- **`ExamFormTests.cs:56-71`** — `form.Publish(5m); form.MaxScore.ShouldBe(5m);` against a production
  body of `MaxScore = maxScore;`. `Publish(999m)` would pass identically; that the frozen total
  matches the slots added (3m + 2m) is never asserted. Line 70's `IsUsable` is
  `Status == Published`, a restatement of line 69.
- **`StaffAccountTests.cs:116`** — the comment says the cap is the point, but the DTO allows 32 and
  the EF column is `nvarchar(16)`. The test uses 13 characters and clears both — and **SQLite ignores
  `nvarchar(n)` entirely**, so the column limit has zero coverage. A 17-32 character phone number
  passes every test here and truncates or throws on SQL Server.
- **`ScheduledWindowTests`** — the file states its own assumption (*"This machine sits at UTC+03:00"*)
  and it holds today (Jordan Standard Time). On a UTC CI runner
  `An_unusable_zone_falls_back_rather_than_closing_the_exam` passes whether the fallback uses local
  or UTC. Nothing proves the window is still **enforced** after an invalid-zone fallback: skip the
  window check in the catch block and every exam with a mistyped timezone becomes permanently open,
  with the suite green.
- **Duplicated coverage:** `CandidateStatusTests.Withdrawn_matches_nobody_rather_than_everybody` is
  fully subsumed by the `OnlyMatchesAsync` helper already called from four other tests;
  `MultiSelectGraderTests` `Selecting_every_option_scores_zero` and
  `One_wrong_option_voids_an_otherwise_complete_answer` are the same call with the same expectation;
  `NamedFormDeliveryTests:239-240` — the `Intersect(…).ShouldBeEmpty()` strictly subsumes the
  `ShouldNotBe` above it; `WeightedChoiceGraderTests:188-189` — `ShouldNotContain("weighted")` is
  subsumed by `ShouldNotContain("weight")`; `SharedBankTests:123-124` — `CanPublish` is derived from
  `Blockers`.
- **`EntityFrameworkCore/Samples/SampleRepositoryTests.cs:41`** — ABP template boilerplate testing
  ABP's own repository. The file's own comment says not to.

---

## 5. Structural facts that cap what any backend test can prove

These are not defects in individual tests. They are the ceiling.

1. **`AddAlwaysAllowAuthorization()`** — `InternshipManagementSystemTestBaseModule.cs:44`. **No
   `[Authorize]` anywhere in the solution is ever executed by any of the 368 backend tests.** The
   only authorisation assertions that run anything are the two
   `Should.ThrowAsync<AbpAuthorizationException>` in `AnswerUploadTests.cs:114,130`, and neither
   checks a code or message. Every permission guarantee in the product rests on the 17 opt-in
   `live/roles.spec.ts` tests, which need three seeded tenants and five seeded accounts to run at all.
2. **`FakeCurrentPrincipalAccessor`** — a hardcoded user with **no roles and no tenant claim**. Every
   backend test runs as the same host-tenant admin.
3. **In-memory SQLite, tables built from the model.**
   `EntityFrameworkCoreTestModule.cs:77` calls `IRelationalDatabaseCreator.CreateTables()`, which
   builds from the current model and **bypasses migrations entirely**. There are **57 migrations in
   `src/…EntityFrameworkCore/Migrations/`** and **no test in the repository runs any of them** — no
   `Migrate()`, no `GetPendingMigrations()`, no `HasPendingModelChanges()`. A migration that does not
   match the model, or that fails on SQL Server, ships undetected.
4. **SQLite is not SQL Server.** Collation, `nvarchar(n)` caps, `LIKE` semantics, decimal precision,
   `rowversion`, `DATETIMEOFFSET` — none are exercised. This matters most for an **Arabic-first**
   product: SQLite's default BINARY collation is not `Arabic_CI_AI`, so every test that asserts a
   search finds or does not find an Arabic name proves nothing about production.
5. **`AddAlwaysDisableUnitOfWorkTransaction()`** — `:43`. No rollback semantics are exercised. A
   partial write on failure is invisible.
6. **Zero Angular unit tests.** `ng test` runs nothing.

---

## 6. The five most important untested behaviours

Chosen by what a user loses if they break, not by line count.

**1. Media read authorisation — `AssessmentMediaAppService.GetAsync`.**
`AssessmentMediaController.cs:86` is `[AllowAnonymous]` by design; the entire decision — staff
checked against the question permission, candidate checked against a signed grant, markers narrowed
to `answers/` — lives inside the service and behind no `[Authorize]` attribute, so
`AuthorizationCoverageTests` cannot see it and no backend test calls it. Every question image,
listening clip, hotspot picture and uploaded answer in the product goes through this one method. Its
only coverage is one opt-in live browser test requiring a three-tenant seeded database. A regression
here either blanks every image mid-exam or hands the answer-key media to anyone with a URL.

**2. Migrations.** 57 of them, zero executed by any test. The suite builds its schema from the model,
so the model and the migrations can diverge silently, and the divergence surfaces as a host that will
not start — or worse, one that starts against a schema missing a column. The two most recent
migrations (`Candidate_Status_Is_Derived`, `Section_On_The_Delivered_Paper`) are dated yesterday.

**3. A candidate seeing a score they are allowed to see.** `ShowResultToCandidate` appears in exactly
one test, and (§1.2) that test's discriminating assertions are dead. Nothing proves a real,
non-zero score with a topic breakdown reaches a candidate at an organisation that releases results.
`ExamTakingAppService.BuildResultAsync` can be mutated to return `ScoreWithheld = true`
unconditionally and the entire backend suite stays green — every candidate would see nothing after
sitting an exam, and no test would say so.

**4. Application services stamping the tenant on write.** `TenantIsolationTests` inserts entities
directly through repositories with the tenant handed in. The path a real request takes —
`ExamAppService.CreateAsync` and its siblings reading `CurrentTenant.Id` — is never exercised. A
service that forgets it writes a host row that no tenant can ever see again, and the multi-tenancy
suite would not notice.

**5. Arabic text against the real database provider.** The product's whole premise is Arabic-first.
`ArabicAnswerTests` is excellent — but it tests an in-process grader, not a query. Every test that
searches, sorts or matches Arabic through EF runs on SQLite BINARY collation. Candidate search,
question search and exam-title search have no coverage that reflects how SQL Server will actually
behave.

Close runners-up: `Assessment.Attempts.*` and `Assessment.Users.ManageRoles` enforcement (§1.1); the
affirmative case for role Edit/Delete in `roles.spec.ts` (`Edit: Marker` absence is asserted, but no
test proves a non-static role *does* show the buttons — `canEdit()` could return false always); item
analysis flags other than `TooEasy`.

---

## 7. The pattern, and what would prevent the next one

Every finding above is one shape:

> **An assertion whose passing state is also the state of a broken system.**

It arrives three ways.

**(a) An absence asserted with no presence beside it.** `ShouldBeEmpty`, `ShouldNotBeNull`,
`toHaveCount(0)`, `not.toBe(403)`, `ShouldNotContain`. Each is satisfied by "the feature was never
built", "the request never happened", "the selector was renamed", "the endpoint 500s".

**(b) A fixture in which the discriminating variable is degenerate.** Withheld score `== 0` because
nothing was answered. Unanswered `== total` because nothing was answered. Insertion order `==`
display order. The same seed used twice. Twenty sittings exactly at the floor of twenty. A phone
number below every cap. In each case the test and its negation produce the same observation.

**(c) A detector whose search space is empty or whose key collides.** `ModuleBoundaryTests` looks for
typed references in a model wired with `Guid`s. `ContractBoundaryTests` looks for a type the compiler
already forbids. `EnforcedInCode` matches on a leaf name that 9 permissions share.

Fixing the sixteen tests above is worth a day. Preventing the seventeenth needs three cheap
mechanisms:

1. **Make the mutation part of the test.** This repository is unusually good at explaining *why* a
   test exists — the comments are the best documentation in the project. The missing half-sentence is
   *"and it fails if you delete X."* Where the author cannot name an X, the test is decoration. Two
   files already do this exactly right and should be the template: `ErrorCodeCoverageTests` guards
   its own search space with `codes.Count.ShouldBeGreaterThan(40)` (104 codes actually match), and
   `live/roles.spec.ts` builds a real blob because a fake path 404s either way.

2. **Run a mutation gate in CI.** Stryker.NET over `InternshipManagementSystem.Application` and
   `.Domain` would have caught findings 1, 3, 4, 5, 9 and 10 automatically. The four mutations in §1
   took 9 seconds to evaluate once built. Start with a survived-mutant *report* rather than a
   threshold, and read it weekly.

3. **Three lint-able rules.**
   - A count assertion must precede every `ShouldAllBe` / `ShouldNotContain(predicate)` /
     `toHaveCount(0)` — or the absence must be paired with the presence **in the same test**.
   - Ban `ShouldNotBeNull()` and `not.toBe(x)` where the expected value is knowable. Assert the value.
   - Every reflection or convention test must assert the **size** of the set it discovered before
     asserting anything about its contents.

And one structural change worth more than all three: **remove `AddAlwaysAllowAuthorization` for at
least one integration test class**, seeded with a real principal and real granted permissions. Today
the entire authorisation layer of a multi-tenant assessment product — where the answer key, the
candidate roster and the marks all live behind permissions — is verified by 17 opt-in browser tests
and one static check that is 69% blind.

---

*No source or test file in the repository was modified. Mutation experiments ran against a copy in a
scratchpad; the four production files touched were verified byte-identical to the repository before
and after, and the copy was restored.*
