# Reachability audit — Astrolabe

**Date:** 2026-08-29
**Method:** static reading only. No build, no run, no browser. Angular sources under `angular/src/`, backend under `src/`.
**Scope:** every route, every link, every client API call, every application service, every screen, every localisation key.

---

## Summary of what is actually broken

| # | Defect | Blast radius |
|---|--------|--------------|
| 1 | Every media URL in the product is built relative and resolves to the SPA origin, not the API origin | Every question image, listening clip, hotspot picture, stimulus, and the tenant logo — staff **and** candidates mid-exam |
| 2 | Sidebar **Assignments** and the dashboard **Assign** card lead to a route that does not exist | Two of the most prominent links in the app; both silently land on the dashboard |
| 3 | **Export CSV** on the results screen is a relative `<a href>` to the API — wrong origin, and no bearer token | Results export is unreachable |
| 4 | **My profile** in the user menu points at `/account/profile`, which is not in the route table | Lands on the dashboard |
| 5 | Review queue page title uses `::Nav:Review`, which is in neither `en.json` nor `ar.json` | Page heading renders the raw key |
| 6 | Blueprint rules can be read by the client but never written; no screen authors them | "Generate from blueprint" starts from nothing |

Sections 1 (route targets) and 4 (services without controllers) are clean. The recurring
"finished service, missing route" defect has **not** recurred in that exact form — this time it has
mutated into *finished route, unreachable URL*.

---

## 1. Every route

**Clean.** Every `loadComponent` / `loadChildren` target resolves to a file that exists, and every
component with `templateUrl` has its `.html` and `.scss` beside it.

Enumerated: `angular/src/app/app.routes.ts` plus eleven lazy children —
`take.routes.ts`, `exam.routes.ts`, `question.routes.ts`, `candidate.routes.ts`, `group.routes.ts`,
`assignment.routes.ts`, `result.routes.ts`, `catalog.routes.ts`, `user.routes.ts`,
`settings.routes.ts`, `review.routes.ts`. 25 leaf routes, 25 components present.

Route ordering is also correct everywhere it matters — `exams/new` precedes `exams/:id`,
`results/questions` precedes `results/:attemptId`, `questions/new` precedes `questions/:questionId`.

Two observations that are *not* missing files:

- **`assignment.routes.ts:7-12` declares only `':examId'` and no `''`.** This is the cause of
  finding 2.2 below. The file itself is fine; the route table has a hole at `/assignments`.
- **`angular/src/app/features/placeholder/placeholder.component.ts`** is referenced by no route and
  no component. Dead code left from phase 3b scaffolding. Not user-visible; safe to delete.

---

## 2. Every link

### 2.1 Sidebar "Assignments" goes nowhere — `angular/src/app/core/navigation.ts:48`

```
{ labelKey: '::Nav:Assignments', route: '/assignments', icon: 'bi-send', permission: P.Assignments.View }
```

`ASSIGNMENT_ROUTES` (`angular/src/app/features/assignments/assignment.routes.ts:7-12`) contains one
route, `':examId'`. There is no `path: ''`. Navigating to `/assignments` matches the parent
`assignments` segment, finds no child that consumes an empty remainder, fails the whole branch, and
falls through to `{ path: '**', redirectTo: '' }` at `app.routes.ts:102`.

**What the user sees:** they click "Assignments" in the People section of the sidebar. The drawer
closes. The page is the dashboard. No error, no message, no URL that stays put. The link appears to
do nothing.

The only working entry to assignments is the row action on the exam list
(`exam-list.component.html:189`, `[routerLink]="['/assignments', exam.id]"`), which is correct
because it supplies the `examId`.

### 2.2 Dashboard "Assign" starter card goes nowhere — `angular/src/app/features/dashboard/dashboard.component.ts:120`

```
{ route: '/assignments', icon: 'bi-send', titleKey: '::Dashboard:Step:Assign', ... }
```

Same defect, second surface. This is the fourth and final card of the getting-started list — the one
that completes the "define, write, add people, send it out" story the dashboard is built around. A
new tenant following the dashboard's own instructions reaches step four and is bounced back to
step one.

### 2.3 "My profile" goes nowhere — `angular/src/app/layout/shell.component.html:96`

```
<a class="user__item" routerLink="/account/profile" role="menuitem" (click)="toggleUserMenu()">
```

There is no `account` path anywhere in `APP_ROUTES`, and `app.config.ts:20-26` states plainly that
ABP's account module is deliberately not registered ("ABP's component library and its identity /
account / tenant screens are deliberately absent"). So no route provider supplies `/account/**`
either.

**What the user sees:** the avatar menu opens, they click "My profile", the menu closes, and they are
on the dashboard.

### 2.4 Everything else — clean

All other 24 `routerLink` bindings and all 6 `router.navigate` calls resolve to declared routes:

- `exam-form.component.ts:198` → `/exams/:id` ✓
- `take-entry.component.ts:97` → `/exam/:token/sitting` ✓
- `take-sitting.component.ts:121, 164, 332` → `/exam/:token` and `/exam/:token/result` ✓
- `question-list.component.ts:103-113` `newLink()` / `editLink()` correctly emit either
  `['/questions', …]` or `['/exams', examId, 'questions', …]` depending on whether the screen is
  serving the bank or an exam — both trees exist ✓
- All `/exams/:id/{questions,structure,forms}`, `/results/:attemptId`, `/results/questions`,
  `/review/:attemptId` links ✓

Every `(click)`, `(submit)`, `(change)` and `(blur)` handler bound in a template resolves to a member
that exists on its component class — checked exhaustively across all 21 template/class pairs. No
handler is a no-op. No `href="#"` anywhere.

---

## 3. Every API call

### 3.1 All media is fetched from the wrong origin — product-wide

This is the worst finding in the audit and it has both a client and a server half.

The SPA runs at `http://localhost:4200` (`angular/src/environments/environment.ts:3` and
`environment.prod.ts:3`). The API runs at `https://localhost:44373`
(`environment.ts:20`, `appsettings.json:3`). `angular.json` declares **no** `proxyConfig`, so nothing
rewrites `/api/**` at the dev server. `App:ClientUrl` and `App:SelfUrl`
(`src/InternshipManagementSystem.HttpApi.Host/appsettings.json:3-4`) confirm two distinct origins by
design.

ABP's `RestService` prepends `environment.apis.default.url` to a relative `url:`, so every
`this.rest.request({ url: '/api/…' })` call is fine. **Anything that puts a relative `/api/…` string
into the DOM is not**, because the browser resolves it against the page origin.

Five places do exactly that:

| Location | Builds | Rendered as |
|---|---|---|
| `angular/src/app/shared/ui/media-field.component.ts:163` | `/api/assessment/media/${blobName}` | `<img>` / `<audio>` / `<video>` preview in every media picker |
| `angular/src/app/features/questions/payload/hotspot-editor.component.ts:188` | `/api/assessment/media/${imageBlobName}` | the hotspot background image an author clicks regions onto |
| `angular/src/app/layout/shell.component.ts:69` | `/api/assessment/media/${blob}` | the tenant logo in the top bar, `shell.component.html:28` |
| `src/…/Application/Assessment/Delivery/ExamTakingAppService.cs:675-677` (`BuildMediaUrl`) | `/api/assessment/media/{blobName}?grant=…` | every stimulus and question image/audio/video in the live exam — `take-sitting.component.html:53-79`, `choice-answer.component.ts:39` |
| `src/…/Application/Assessment/Media/AssessmentMediaAppService.cs:112` | `Url = "/api/assessment/media/{blobName}"` | returned from upload; same shape, same problem |

**What the user sees:** a broken-image icon, or an audio player that will not play, everywhere media
appears. A candidate sitting a listening exam gets a dead player and cannot answer the question.
`take-entry.component.html:31` shows the same broken logo on the exam welcome screen.

There is a **second, independent** failure on the staff branch even if the origins were unified.
`AssessmentMediaAppService.GetAsync` (`:142-149`) authorises a staff caller via
`AuthorizationService.IsGrantedAsync(Questions.Default)`. The host authenticates with a bearer JWT
(`ConfigureAuthentication`, `InternshipManagementSystemHttpApiHostModule.cs:78-85`), and a browser
`<img src>` cannot carry an `Authorization` header. So the staff request arrives anonymous,
`entitled` is false, `GetAsync` returns `null`, and the controller answers `404`
(`AssessmentMediaController.cs:76-79`). Staff media previews would be broken even same-origin.

The candidate branch is designed correctly for this — the signed `grant` travels in the query string
precisely because "no script gets to add a header to that request"
(`AssessmentMediaController.cs:30-38`). The staff branch has no equivalent and needs one.

Note the irony recorded in the controller's own doc comment (`AssessmentMediaController.cs:13-24`):
the media route was already the subject of one of the five documented recurrences, and the browser
tests stub this exact URL — so they still cannot see this.

### 3.2 "Export CSV" is a relative link with no credential — `angular/src/app/features/results/result-list.component.html:15`

```html
<a class="btn btn-primary" [href]="exportUrl()">
```

`ResultService.exportUrl()` (`angular/src/app/core/api/result.service.ts:68`) returns
`` `${this.base}/export?${query}` `` — that is `/api/assessment/results/export?…`, relative.

The server route exists and is correct: `ResultController.cs:47` `[HttpGet("export")]` with
`[FromQuery] ResultListRequestDto`, backed by `IResultAppService.ExportCsvAsync`. The parameter shape
matches — client sends a query string, server declares `[FromQuery]`.

The defect is entirely in how the URL is used. Two failures stacked:

1. It resolves to `http://localhost:4200/api/assessment/results/export?…`. The Angular dev server
   has no such file and no proxy, so it serves `index.html`; the router sees an unmatched path and
   `**` sends the user to the dashboard.
2. Even pointed at the right origin, a plain `<a href>` navigation carries no bearer token, so the
   API answers `401`.

**What the user sees:** they set their filters, click the primary blue "Export" button, and land on
the dashboard with their filters lost and no file downloaded.

The comment above the link (`result-list.component.html:12-14`) explains the design intent — "the
browser already knows how to save a file" — which is sound, but it needs an absolute URL and a
credential the browser will send.

### 3.3 Everything else — clean, including parameter shapes

All 68 client calls across `core/api/*.service.ts`, `features/take/take.service.ts` and
`shared/ui/media-field.component.ts` were matched one-for-one against `[Http*]` attributes in
`src/InternshipManagementSystem.HttpApi/**`. Every one has a server route, with the correct verb and
the correct path-vs-query split:

- `assignment.service.ts:90` `GET links/{examId}` + `params` ↔ `AssignmentController.cs:40`
  `[HttpGet("links/{examId}")]` + `[FromQuery] PagedAndSortedResultRequestDto` — path segment for the
  id, query string for paging. Agrees.
- `result.service.ts:54` `GET item-analysis/{examId}` ↔ `ResultController.cs:56`. Agrees.
- `question.service.ts:94` `GET groups/{examId}` ↔ `QuestionController.cs:68`. Agrees.
- `structure.service.ts:57` `GET forms/by-exam/{examId}` ↔ `ExamStructureController.cs:50`. Agrees.
- `catalog.service.ts:33` `GET categories` + `params: { includeInactive }` ↔ `CatalogController.cs:29`
  `[FromQuery] bool includeInactive = false`. Agrees.
- `take.service.ts:83` `GET question/${displayPosition - 1}` ↔ `ExamTakingController.cs:49`
  `[HttpGet("question/{position:int}")]`. Agrees — and the off-by-one is deliberate and correct
  (display position is 1-based, the wire is 0-based).
- All 11 `catalog`, 11 `candidate`, 9 `exam`, 10 `question`, 12 `exam-structure`, 4 `review`,
  4 `result`, 2 `settings`, 5 `user`, 8 `take` calls verified individually.

No literal/parameter route shadowing exists on the server: ASP.NET Core prefers literal segments, so
`GET candidates/groups` never resolves to `candidates/{id}`, and `questions/types` never resolves to
`questions/{id}`.

### 3.4 Server endpoints with no client caller

Not user-facing defects, but capability that no click can reach:

| Endpoint | Note |
|---|---|
| `ExamController.cs:67` `PUT {examId}/blueprint` | **No client method at all.** See section 5.1 — this is a real functional dead end. |
| `ExamController.cs:64` `GET {examId}/blueprint` | Client method exists (`exam.service.ts:84 getBlueprint`) but **no component calls it**. Dead client code. |
| `QuestionController.cs:64` `POST validate-payload` | The Angular payload editors validate locally instead (`choice-editor.component.ts:147-199` etc.). Two validators, one server one client, that can drift. |
| `SelfRegistrationSettingController.cs` `/api/settings/self-registration` | Nothing in `angular/src` references it. |
| `SystemGeneralSettingsController.cs` `/api/system-settings/general` | Nothing in `angular/src` references it. Distinct from the tenant settings screen, which uses `/api/assessment/settings`. |

`POST take/signal` (`ExamTakingController.cs:57`) **is** called — `take.service.ts:104`, driven by
`take-sitting.component.ts:350-375`. Not a gap.

---

## 4. Every application service without a controller

**Clean.** All 14 concrete `*AppService` classes have an explicit controller with an explicit
`[Route]`. None relies on ABP's convention to be reachable.

| Application service | Explicit controller | Route |
|---|---|---|
| `Assessment/Catalog/CatalogAppService.cs` | `Assessment/CatalogController.cs` | `api/assessment/catalog` |
| `Assessment/Delivery/AssignmentAppService.cs` | `Assessment/AssignmentController.cs` | `api/assessment/assignments` |
| `Assessment/Delivery/ExamTakingAppService.cs` | `Assessment/ExamTakingController.cs` | `api/assessment/take` |
| `Assessment/Exams/ExamAppService.cs` | `Assessment/ExamController.cs` | `api/assessment/exams` |
| `Assessment/Exams/ExamStructureAppService.cs` | `Assessment/ExamStructureController.cs` | `api/assessment/exam-structure` |
| `Assessment/Exams/QuestionAppService.cs` | `Assessment/QuestionController.cs` | `api/assessment/questions` |
| `Assessment/Media/AssessmentMediaAppService.cs` | `Assessment/AssessmentMediaController.cs` | `api/assessment/media` |
| `Assessment/People/CandidateAppService.cs` | `Assessment/CandidateController.cs` | `api/assessment/candidates` |
| `Assessment/Results/ResultAppService.cs` | `Assessment/ResultController.cs` | `api/assessment/results` |
| `Assessment/Review/ReviewAppService.cs` | `Assessment/ReviewController.cs` | `api/assessment/review` |
| `Assessment/Settings/TenantSettingsAppService.cs` | `Assessment/TenantSettingsController.cs` | `api/assessment/settings` |
| `IdentityManagement/UserAppService.cs` | `IdentityManagement/UserController.cs` | `api/app/users` |
| `SystemSettings/…/SelfRegistrationSettingAppService.cs` | `Settings/SelfRegistrationSettingController.cs` | `api/settings/self-registration` |
| `SystemSettings/…/SystemGeneralSettingsAppService.cs` | `Settings/SystemGeneralSettingsController.cs` | `api/system-settings/general` |

`InternshipManagementSystemAppService.cs` is the `abstract` base class, not a service. Correctly has
no controller.

Method-level coverage was checked too: every method on every `I*AppService` contract has a
corresponding controller action. Nothing is stranded behind an unrouted method.

Two structural notes:

- **Everything is also exposed conventionally.** `ConfigureConventionalControllers()` registers the
  whole `Application` assembly (`InternshipManagementSystemHttpApiHostModule.cs:137-143`), so each
  service additionally answers at ABP's generated `/api/app/…` routes. The explicit routes are the
  contract the client uses; the conventional ones are a second, unversioned surface nobody tests. It
  is also what made `AssessmentMediaAppService`'s missing `[Authorize]` exploitable, per that class's
  own remarks (`:33-38`).
- **`ConventionalControllers.Create` is called twice for the same assembly** — once inline at
  `InternshipManagementSystemHttpApiHostModule.cs:74` and once via
  `ConfigureConventionalControllers()` at `:68`/`:141`. Harmless-looking duplication that can surface
  as duplicate action descriptors or Swagger operation-id conflicts. Low priority, but one of the two
  should go.

---

## 5. Empty or placeholder screens

### 5.1 The dashboard is honest, but a quarter of it is broken

**The dashboard does not show invented numbers.** `angular/src/app/features/dashboard/dashboard.component.ts`
is deliberately an empty state, and the reasoning is written down at `:7-13`: "a dashboard of zeroes
tells them nothing. What they need is the next action." It renders a title, a lede, and four
permission-filtered starter cards. There are no charts, no KPI tiles, no fabricated counts. This is
the right call and it is implemented as described.

The defect is that **the fourth card is dead** — see finding 2.2. Three of four work.

### 5.2 The blueprint cannot be authored anywhere

`ExamFormsComponent` offers "Generate" as the first and recommended path to a paper
(`exam-forms.component.html:122`, and the class comment at `:32-34` says so: "Generating from the
blueprint is offered first, because starting from a filled paper and removing two is work somebody
will do"). It calls `structure.generateForm(form.id)` (`exam-forms.component.ts:244`), which the
server fills from the exam's blueprint rules.

But **no screen in the application writes blueprint rules.** `PUT /api/assessment/exams/{examId}/blueprint`
exists on the server (`ExamController.cs:67`) and has no client method. `getBlueprint`
(`exam.service.ts:84`) exists and is called by nothing. Searching `angular/src` for "blueprint"
returns only comments and the one dead service method.

**What the user sees:** they click "Generate", following the screen's own advice, and get either an
empty paper or an `IMS:Exam:BlueprintUnsatisfiable` blocker (`ExamAppService.cs:219`) that they have
no way to act on, because the thing it is complaining about cannot be edited in the product.

### 5.3 Orphaned placeholder component

`angular/src/app/features/placeholder/placeholder.component.ts` — the phase-3b stand-in described in
its own doc comment. No route references it, no component imports it. It renders `::ScreenNotBuiltYet`
if it ever did. Currently unreachable dead code.

### 5.4 Everything else — clean

Every routed feature component is wired to at least two real services (a data service plus
`TranslateService`) and renders real state. Checked all 21:
`catalog` (500 lines, 2 services), `exam-structure` (321, 5), `assignment` (295, 5),
`exam-forms` (325, 4), `question-list` (300, 4), `question-form` (320, 3), `group-list` (317, 3),
`take-sitting` (393, 2), `exam-form` (262, 4), `candidate-list` (242, 2), `review-attempt` (237, 2),
`exam-list` (233, 2), `user-list` (213, 2), `result-list` (182, 4), `settings` (121, 2),
`take-entry` (111, 2), `item-analysis` (96, 3), `review-queue` (93, 2), `result-detail` (87, 2),
`take-result` (54, 2), `dashboard` (131, 2).

No component renders a heading and nothing else. `item-analysis` in particular is real — it loads the
exam list, offers a picker, and fetches per-exam rows (`item-analysis.component.ts:49-84`) — so the
`/results/questions` link that carries no exam id is correct, not a gap.

### 5.5 Minor: the settings screen contradicts its own comment

`app.routes.ts:86-92` deliberately leaves `/settings` unguarded, with a comment: "everybody signed in
may read the settings, and the screen is read-only without ManageSettings. Knowing the rules the
exams run under is not a privilege."

But `navigation.ts:63` gates the sidebar item on `P.Administration.ManageSettings`. So the read-only
view the route was written to permit has no link anywhere in the UI. Either the route guard or the
nav permission is wrong; they cannot both be right.

---

## 6. Localisation keys

**One missing key, missing from both languages.**

598 distinct `::` literals were extracted from every `.ts` and `.html` under `angular/src` and checked
against both `en.json` and `ar.json`.

### 6.1 `::Nav:Review` — missing from `en.json` AND `ar.json`

`angular/src/app/features/review/review-queue.component.html:2`

```html
<astro-page-header
  [title]="t('::Nav:Review')"
  [description]="t('::Review:Queue:Lede')" />
```

The key the navigation actually defines is `::Nav:ReviewQueue` (`navigation.ts:54`), which does exist
in both files. `::Nav:Review` does not exist in either.

**What the user sees:** the review queue page — a daily-driver screen for markers — has the literal
text `::Nav:Review` as its `<h1>`, in English and in Arabic alike. The description underneath renders
correctly, which makes the broken heading look more like a bug than a missing translation.

### 6.2 Everything else — clean

- 750 keys in `en.json`, 750 in `ar.json`. **Perfect parity** — zero keys present in one file and
  absent from the other.
- All dynamically composed keys resolve. `t('::Theme:' + dir.theme())` at `shell.component.html:76`
  covers `system` / `light` / `dark` (`direction.service.ts:7`); all three exist
  (`en.json:731-733`).
- All 83 `IMS:*` codes referenced anywhere in the C# or Angular sources — publish-check blockers and
  warnings (`ExamAppService.cs:201-269`), CSV import problem reasons (`CandidateAppService.cs:186,
  211`), and the 22 client-side payload validation warnings across the seven payload editors — exist
  in **both** `en.json` and `ar.json`. These are the codes rendered through `t('::' + code)` at
  `exam-form.component.html:269, 281`, `candidate-list.component.html:222` and the payload editors,
  so a gap here would print a raw error code at exactly the moment an author most needs a sentence.
  There are none.
- 154 keys in `en.json` are never referenced from the Angular sources. Most are server-side
  (`Permission:*`, ABP defaults, exception messages resolved by the backend), so this is expected, not
  a finding.

---

## Suggested order of repair

1. **Media URLs** (3.1) — one shared helper that prefixes `environment.apis.default.url`, plus a
   staff-side grant equivalent to the candidate's, or a same-origin reverse proxy. Largest blast
   radius, and it breaks a live exam in progress.
2. **`/assignments` index route** (2.1, 2.2) — add `{ path: '', … }` to `ASSIGNMENT_ROUTES` with an
   exam picker, or point both links at `/exams` with an assign affordance. Two of the app's most
   prominent links.
3. **Export CSV** (3.2) — absolute URL plus a credential the browser will send.
4. **`/account/profile`** (2.3) — build the screen or remove the menu item.
5. **`::Nav:Review`** (6.1) — one-line fix, use `::Nav:ReviewQueue` or add the key to both files.
6. **Blueprint authoring** (5.2) — the server half is finished and waiting.
7. Housekeeping: settings guard/nav mismatch (5.5), duplicate `ConventionalControllers.Create` (4),
   orphaned `PlaceholderComponent` (5.3), dead `getBlueprint` client method (3.4).

## A note on the recurrence

The documented pattern is "a finished application service with no HTTP controller, or a sidebar link
with no route." Section 4 is clean for the first time — every service has an explicit controller, and
the team clearly went looking. The second half still recurred (2.1), and the failure mode has
otherwise shifted one layer outward: routes and controllers now exist, but the **URLs that reach
them** are built wrong (3.1, 3.2). A test that asserts "the controller answers" will not catch any of
the section 3 findings; only a test that loads a real page against a real origin will.

The media controller's own doc comment already names this trap: *"the browser tests stub this exact
URL, so they proved the page renders what the server would send rather than that the server sends
it."* The same stub is why the relative-URL defect survived the fix that comment describes.
