# Permission System Audit

Date: 2026-08-29
Scope: `Assessment` permission group end to end — constants, definitions, server enforcement, Angular guards, seeding, anonymous endpoints.
Method: cross-check of the four surfaces against each other. No code was changed.

## Surfaces that are correct

Three of the checked surfaces are clean and need no work:

- **Constants ↔ definitions.** Every one of the 51 constants in `InternshipManagementSystemPermissions.cs` is reachable from `InternshipManagementSystemPermissionDefinitionProvider.Define`. There is no second `IdentityManagement.Users.Default`-style hole: the `[Authorize]` policy names used on the server all resolve to a defined permission. No 500-instead-of-403 remains.
- **UI policy strings.** All 50 dotted strings in `angular/src/app/core/permissions.ts` match a C# constant exactly, character for character. No typos, no orphans in either direction. No screen is permanently invisible or permanently visible because of a misspelling.
- **The taker's anonymous path.** `ExamSessionTokenService` and `ExamTakingAppService` are soundly built (see finding 11).

The defects are concentrated in server enforcement, in the gap between the permission tree and what is actually checked, and in three Angular components that read permissions non-reactively.

---

## 1. CRITICAL — anonymous write to tenant settings

**`src/InternshipManagementSystem.Application/SystemSettings/Application/AppServices/SystemGeneralSettingsAppService.cs:8`**

The class carries no `[Authorize]` of any kind, and neither does `UpdateAsync` (line 28).

Three facts combine into an unauthenticated write:

1. `InternshipManagementSystemHttpApiHostModule.cs:74` and `:141` both call `options.ConventionalControllers.Create(typeof(InternshipManagementSystemApplicationModule).Assembly)`, so every `ApplicationService` in that assembly gets a generated controller.
2. There is no `[RemoteService(IsEnabled = false)]` anywhere in the Application project — verified by grep, zero matches.
3. There is no global fallback authorization policy. `ConfigureCors` (line 165) calls `AddDefaultPolicy`, but that is CORS. `app.UseAuthorization()` at line 211 enforces only what attributes declare.

So `PUT /api/app/system-general-settings` is reachable **with no token at all**, and writes `SiteName`, `DefaultLanguage`, `MaxExamAttempts` and `LogoUrl` through `SetForCurrentTenantAsync` (lines 30-33).

The explicit `SystemGeneralSettingsController` (`src/InternshipManagementSystem.HttpApi/Settings/SystemGeneralSettingsController.cs:12`) does carry `[Authorize]` — but it guards `/api/system-settings/general`, a different route. The generated one is unguarded and undocumented.

**What a user would see:** nothing, which is the problem. An anonymous caller can rename the tenant, swap its logo to an arbitrary URL, flip the default language, and change `MaxExamAttempts` — the last of which is exam-integrity relevant. There is no UI trace and no authorization failure to log.

Note that `[Authorize]` on the controller would only ever have meant "any signed-in user", not "an administrator". The correct guard is `Administration.ManageSettings` on the app service, matching how `TenantSettingsAppService.UpdateAsync` (`Assessment/Settings/TenantSettingsAppService.cs:56`) already does it.

## 2. HIGH — privilege escalation: `Users.ManageRoles` is never enforced

**`src/InternshipManagementSystem.Application/IdentityManagement/UserAppService.cs:77` and `:100`**

`UpdateAsync` is guarded by `IdentityManagement.Users.Edit`. At line 100 it calls `SetRolesAsync(user, input.Roles)`, which (lines 116-134) makes the account's roles match the submitted list exactly — adding any role the caller names, including `Admin`.

`InternshipManagementSystemPermissions.IdentityManagement.Users.ManageRoles` is defined (`InternshipManagementSystemPermissions.cs:133`), published in the tree (`...PermissionDefinitionProvider.cs:78`), and mirrored in `angular/src/app/core/permissions.ts:89` — and is checked **nowhere in the codebase**. Grep for it outside the constants and definition files returns zero enforcement sites.

The same applies to `CreateAsync` (line 47, guarded by `Users.Create`, calls `SetRolesAsync` at line 70).

**What a user would see:** an operator granted only "Users: Edit" — the permission you would hand a receptionist to fix a misspelled surname — opens any user record, ticks `Admin`, and saves. They now hold every permission in the system. The `ManageRoles` checkbox sits in the permission management UI looking like it controls this, and controls nothing.

## 3. HIGH — the seeder silently reverts deliberate revocations, and only on a migrator run

**`src/InternshipManagementSystem.Domain/Data/InternshipManagementSystemDataSeedContributor.cs:118-136`**

The good news first, since the brief asked directly: **`GrantAdminPanelAccessToAdminRoleAsync` does grant the admin role every permission in our group, including newly added ones.** It reads `_permissionDefinitions.GetGroupsAsync()`, filters to `GroupName`, and calls `GetPermissionsWithChildren()` — so a permission added tomorrow is granted without touching this file. `InternshipManagementSystemDbMigrationService` seeds per tenant inside `_currentTenant.Change(tenant.Id)` (lines 60-78), so multi-tenant scope is correct too. That part of the design is right.

Two real problems remain:

**(a) It only runs in the DbMigrator.** `DbMigratorHostedService` is the sole caller. Grep for `IDataSeeder` / `SeedAsync` across `HttpApi.Host` returns nothing — the API host does not seed on startup. So on an existing deployment, adding a permission and shipping only the schema migration (or restarting the API without re-running the migrator) leaves every admin with a 403 on the new screen. That is exactly the failure the method's own comment describes as already having happened once.

**(b) `SetForRoleAsync(adminRole.Name, permission.Name, true)` on line 134 runs unconditionally on every startup.** The comment above it calls this "idempotent", and it is idempotent for granting — but it is not a no-op for a permission an operator *deliberately revoked*. If a customer removes, say, `Results.Export` from their Admin role for a compliance reason, the next migrator run silently grants it back. The comment two paragraphs up correctly identifies this hazard for ABP's own permissions and scopes away from them; it then does the same thing to our own group.

Also: the seeder only ever targets the role literally named `Admin`. Custom roles and tenants that renamed their administrator role get nothing, which is defensible — but it means "does the admin role get every permission" is only true for the seeded role.

## 4. MEDIUM — class-level and method-level `[Authorize]` combine, making the cohort feature unreachable

ASP.NET Core and ABP both **combine** class-level and method-level `[Authorize]` — the caller must satisfy *all* of them. Method-level does not override class-level (only `[AllowAnonymous]` does). This produces guards stronger than intended in two places.

**`src/InternshipManagementSystem.Application/Assessment/People/CandidateAppService.cs:22` + `:253`**

The class is guarded by `Candidates.Default`. The cohort methods below it carry `Groups.View` (253), `Groups.Create` (289), `Groups.Edit` (306), `Groups.Delete` (323), `Groups.Edit` (340). Every cohort call therefore requires `Candidates.Default` **and** the `Groups.*` permission.

Meanwhile `angular/src/app/app.routes.ts:56-59` guards `/groups` on `Assessment.Groups.View` alone, and `navigation.ts:47` shows the nav item on the same. Granting a child in ABP's permission UI auto-selects its parent, so `Groups.View` implies `Groups.Default` — but never `Candidates.Default`, a sibling subtree.

**What a user would see:** a role built as "cohort manager" (all of `Groups.*`, none of `Candidates.*`) sees the Groups item in the sidebar, clicks it, passes the route guard, the screen mounts — and every request returns 403. A blank table and an error toast, on a screen the navigation just told them they could use.

**`src/InternshipManagementSystem.Application/Assessment/Catalog/CatalogAppService.cs:26`** has the same shape: class-level `Catalog.View` plus ten `Catalog.Manage` methods, so `Manage` is inert without `View`. Lower impact — the route and nav also gate on `Catalog.View`, so the two surfaces at least agree — but a "Manage-only" grant is silently useless.

## 5. MEDIUM — editing a question group requires the Create permission as well

**`src/InternshipManagementSystem.Application/Assessment/Exams/QuestionAppService.cs:273-274`**

```csharp
[Authorize(InternshipManagementSystemPermissions.Questions.Create)]
[Authorize(InternshipManagementSystemPermissions.Questions.Edit)]
public async Task<QuestionGroupDto> UpdateGroupAsync(Guid id, CreateUpdateQuestionGroupDto input)
```

Two stacked attributes, both required (same combining rule as finding 4). This is the only method in the codebase with two permission attributes, and the method only mutates — it creates nothing. Almost certainly a copy-paste from `CreateGroupAsync`; `Questions.Edit` alone is what the operation means.

**What a user would see:** a proofreader granted `Questions.Edit` but not `Questions.Create` — a deliberate and reasonable split — can edit individual questions but gets a 403 when fixing a typo in the reading passage above them.

## 6. MEDIUM — the entire sidebar is computed once and cached forever

**`angular/src/app/layout/shell.component.ts:100-107` and `:138-140`**

```ts
readonly sections = computed<readonly VisibleSection[]>(() =>
  NAVIGATION.map(...).filter(...)
);
// ...
private isVisible(item: NavItem): boolean {
  return !item.permission || this.permission.getGrantedPolicy(item.permission);
}
```

This *looks* reactive and is not. `computed()` re-runs only when a signal it read changes. `getGrantedPolicy()` is a synchronous non-signal call and `NAVIGATION` is a static `const`, so this computed has **zero signal dependencies**. It evaluates once on first template read and caches that value for the lifetime of the app. The app is zoneless (`app.config.ts:34`, `provideZonelessChangeDetection()`), so nothing else will mark the view dirty either.

The project's own `angular/src/app/core/permission.signal.ts:9-21` documents this exact race and notes it is asymmetric — Arabic loads its locale data with an extra dynamic import, which is enough to move component construction ahead of the configuration.

**What a user would see:** if ABP's config lands after the shell's first render, the sidebar collapses to the single Dashboard entry (`navigation.ts:33`, the only item with no `permission`) and stays that way until a hard reload. A fully-privileged admin sees a one-item menu, intermittently, and more often in Arabic.

## 7. MEDIUM — `exam-form.component.ts` reads permissions in field initialisers

**`angular/src/app/features/exams/exam-form.component.ts:95-96`**

```ts
readonly canEdit = this.permission.getGrantedPolicy(P.Exams.Edit);
readonly canPublish = this.permission.getGrantedPolicy(P.Exams.Publish);
```

These are plain booleans captured at construction, and the template consumes them as plain booleans — `exam-form.component.html:14` (`@if (canEdit)`) and `:22` (`@if (canPublish && ...)`), with no call parentheses.

This is the single file that was not migrated to `permissionSignal()`. Its sibling `exam-list.component.ts:46-53` uses the helper correctly, as do eleven other components (questions, candidates, groups, assignments, results, catalog, users, settings, exam-structure, exam-forms). This one is the exact offender the helper's doc comment was written about.

**What a user would see:** an author with full exam permissions opens an exam and finds no Save button and no Publish button. Reloading may fix it; it depends on whether config beat construction that time.

## 8. LOW/MEDIUM — dashboard permission checks have no reactive trigger

**`angular/src/app/features/dashboard/dashboard.component.ts:128-130`**

```ts
can(policy: string): boolean {
  return this.permission.getGrantedPolicy(policy);
}
```

Called from the inline template as `@if (!step.permission || can(step.permission))`. Not a cached field, so under Zone.js this would self-correct on any change-detection pass — but the app is zoneless, and nothing schedules a re-render when the config arrives.

**What a user would see:** the four "get started" tiles (permissions at lines 103, 110, 117, 124) can stay hidden on the landing screen, which is the first thing a new operator sees.

## 9. LOW/MEDIUM — integrity signal counts bypass `ViewIntegritySignals`

`InternshipManagementSystemPermissions.cs:90-94` holds `Review.ViewIntegritySignals` separately and explains why: "these are behavioural data about a person, not just their answers." `ReviewAppService.GetIntegrityReportAsync` (`Assessment/Review/ReviewAppService.cs:213`) honours that.

`ResultAppService` does not. It is guarded by `Results.View` at class level (`Assessment/Results/ResultAppService.cs:32`), and its rows carry `IntegrityFlagCount`, exported as the `IntegrityFlags` CSV column under `Results.Export` (`:200`, header at `:216`, value at `:241`).

A count is not the detail — no timestamps, no signal types — so this is a partial leak, not a full one. But whether a candidate was flagged at all is the fact the separate permission exists to gate, and it is currently readable by anyone who can view results.

## 10. LOW — `/settings` nav and route disagree

`angular/src/app/core/navigation.ts:63` hides the Settings item unless the user holds `Administration.ManageSettings`. `app.routes.ts:86-92` deliberately has no permission guard, with a comment stating that everybody signed in may read the settings and the screen is read-only without `ManageSettings`. `TenantSettingsAppService` agrees: class-level `[Authorize]` only (`:20`), with `ManageSettings` on `UpdateAsync` alone (`:56`).

The route, the service and the comment form a coherent design. The nav item contradicts all three: the read-only settings view is unreachable by navigation for precisely the users it was built for. They can still reach it by typing the URL.

## 11. Anonymous endpoints — one line each

Five `[AllowAnonymous]` sites, plus two services that are anonymous by omission.

| Site | Verdict |
|---|---|
| `ExamTakingController.cs:22` (class) and `ExamTakingAppService.cs:32` (class) | **Safe.** Every one of the eight methods calls `RequireSession` (`ExamTakingAppService.cs:475-477`), which validates an HMAC-SHA256 JWT scoped to one attempt and throws `AbpAuthorizationException` on failure; `LoadOwnAttemptAsync` (`:692-698`) re-checks that the session names the attempt being touched, so a valid session for attempt A cannot read attempt B. |
| `ExamTakingAppService.OpenLinkAsync` (`:94`, reached anonymously with no session) | **Safe.** The link token is 256 bits of `RandomNumberGenerator` entropy (`ExamSessionTokenService.cs:247`), stored only as a SHA-256 hash (`:243`), and looked up by hash; the multi-tenant filter is disabled deliberately (`:99`) because the link is what establishes tenant context, and every read afterwards is keyed off ids the link itself carries. |
| `AssessmentMediaController.cs:71` → `AssessmentMediaAppService.GetAsync` (`:132`) | **Safe, and correctly reasoned.** It cannot be a single attribute because two unlike callers are entitled: staff are checked against `Questions.Default` via `AuthorizationService.IsGrantedAsync` (`:143-144`), candidates present a signed grant naming exactly one blob and compared rather than read back (`ExamSessionTokenService.GrantsMedia`, `:210-235`), so a grant for the listening clip cannot be replayed against someone's uploaded answer. Traversal is rejected on the read path (`:137`), an unentitled caller gets `null` → 404 rather than 403 so blob existence is not disclosed, `X-Content-Type-Options: nosniff` is set, and SVG is served as `application/octet-stream` (`AssessmentMediaController.cs:112`) so a stored SVG cannot execute script. The grant-in-URL design is necessary — an `<img>` or `<audio>` request carries no custom header — and is mitigated by scoping to one blob with expiry tied to the attempt. |
| `AssessmentMediaController.UploadAsync` (`:52`) and `DeleteAsync` (`:90`) — no attribute on the controller | **Safe.** Both delegate to app service methods carrying `[Authorize(Questions.Edit)]` (`AssessmentMediaAppService.cs:74`, `:154`), and ABP's authorization interceptor runs on the app service regardless of the controller. |
| `SelfRegistrationSettingController.cs:19` | **Safe, but by accident.** It returns one boolean the pre-auth login page legitimately needs. However `SelfRegistrationSettingAppService.cs:7` has no `[Authorize]` either, so the *generated* route `GET /api/app/self-registration-setting/...` is anonymous too — the same root cause as finding 1, here landing on something harmless. |
| `SystemGeneralSettingsAppService` — no attribute | **Not safe.** See finding 1. |

## 12. LOW — dead permissions: five grantable checkboxes that enforce nothing

Enforced nowhere in `src/` (grep across all `[Authorize]` sites and manual `IsGrantedAsync` calls):

| Permission | Status |
|---|---|
| `Attempts.View` | No endpoint exists. |
| `Attempts.ForceSubmit` | No endpoint exists. Timeout submission is done by `AttemptTimeoutWorker`, a background worker with no user-facing trigger. |
| `Attempts.Delete` | No endpoint exists. |
| `Assignments.SendEmail` | Never checked. Email sending is driven by the `SendEmail` boolean on the request DTO (`AssignmentDtos.cs:49` → `AssignmentAppService.cs:153`), inside `CreateAsync`, which is guarded only by `Assignments.Create`. Anyone who can create an assignment can send mail. |
| `Administration.Access` | Never checked anywhere, server or client. |
| `Users.ManageRoles` | Never checked — see finding 2, where this is a live escalation rather than merely dead. |

All six are also present in `angular/src/app/core/permissions.ts` (lines 51, 56-58, 89, 95) and unreferenced by any component.

**What a user would see:** an administrator grants "Attempts: Force Submit" to a proctor so they can close out a stuck sitting, and there is no such button anywhere. The permission tree is a promise the product does not keep.

## 13. LOW — housekeeping

- **`InternshipManagementSystemHttpApiHostModule.cs:74` duplicates `:141`.** `ConfigureConventionalControllers()` is called at line 68, and then the identical `ConventionalControllers.Create(...)` is repeated inline at line 74. Harmless but confusing, and it doubles the registration for the same assembly.
- **`src/InternshipManagementSystem.Domain/Identity/RoleDataSeederContributor.cs` is dead code.** It implements `ITransientDependency` but *not* `IDataSeedContributor`, and nothing references it — grep returns only its own declaration. ABP never invokes it. Its `SeedAsync` is the only place that writes the `App.General.*` settings (lines 33-36), so those are never seeded, which is why `SystemGeneralSettingsAppService.GetAsync` reads nulls and falls back to `int.Parse(null ?? "1")` for `MaxExamAttempts`. Not a permission defect, but it is adjacent to finding 1 and will confuse whoever fixes it.
- **Route policies are string literals.** `app.routes.ts:38, 44, 51, 58, 64, 71, 77, 83, 96` hard-code `'Assessment.Exams.View'` and friends rather than referencing the `P.*` constants that `permissions.ts:1-9` exists to provide. They are all correct today (verified character-for-character), but they bypass the compile-time protection the constants file was written for. The next one added is the one that gets misspelled.

---

## Suggested order of work

1. Finding 1 — add `[Authorize(Administration.ManageSettings)]` to `SystemGeneralSettingsAppService`. Unauthenticated write, fix today.
2. Finding 2 — enforce `Users.ManageRoles` around `SetRolesAsync`, or drop the permission and document that `Users.Edit` includes role assignment. Escalation path.
3. Finding 3(b) — make the admin grant additive-only, or explicitly record that re-granting is intended.
4. Findings 4 and 5 — the two cumulative-`[Authorize]` bugs. Both are small edits with clear user-visible symptoms.
5. Findings 6, 7, 8 — migrate the three remaining call sites to `permissionSignal()`. Finding 6 is the widest blast radius of the three.
6. Findings 9-13 — tidy-up, and a decision on whether the six dead permissions should be implemented or removed from the tree.
