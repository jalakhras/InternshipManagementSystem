# Accessibility — file-by-file fix list

**Date:** 2026-08-31 · **Tree:** audited at `eeb4af4`, `b2c3f81` by the time this was written
(the worklist header still says `b75835d`). HEAD moved twice during the audit and the dirty set
turned over completely — all of it in `features/take/` and the delivery layer behind it. Treat
every line number in this document as a starting point and confirm by the surrounding text.
**Covers:** `worklist.md` §2.1–2.4.
**Method:** axe-core 4.13.0 fetched with `npm pack` into a scratch directory and injected with
`page.addScriptTag` — **nothing was installed into the project**; plus in-page geometry and
focus probes driven with the project's own Playwright build against the running app
(`localhost:4200` + `localhost:44373`), signed in as `admin`.

**Nothing in this document has been applied.** It is a list, not a patch.

> **`angular/src/app/features/take/` is off-limits** — another engineer is editing the exam
> paper. Everything in that folder below is reported with **intended markup, not a patch**, and
> its line numbers **will have moved** by the time you read this. Anchor on the surrounding text,
> not the number. (They already moved once during this audit: the submit dialog was at `:149`
> when the design review was written and is at `:187` now.)

---

## Ordering

Ordered by how many people it affects and how badly, which is **not** the order of how cheap
each is to fix:

| Rank | Item | Who | Cost to them |
|---|---|---|---|
| 1 | **2.2** dialogs | every keyboard and screen-reader user, on 11 dialogs — including the candidate's submit dialog, under a running clock | cannot confirm, cannot cancel, cannot get out |
| 2 | **2.4** tables at 390px | **everyone on a phone**, not only assistive-tech users | the action buttons do not exist as far as they can tell |
| 3 | **2.3** the exam timer | screen-reader candidates, for the whole sitting | the clock never says the time, and drowns everything else that would |
| 4 | **2.1** account menu button | screen-reader staff, 13 of 13 screens | announced as "button, collapsed"; this is how you sign out |

**2.1 is the cheapest thing on this list — one attribute and two localisation keys — and clears
the single most repeated critical in the product. Do it first regardless of this ordering.**

---

## Summary of totals, and what turned out not to be true

| Item | Reported | Measured / found | Verdict |
|---|---|---|---|
| 2.1 | critical on 13 of 13 screens | `button-name` (critical), 1 node, on **14 of 14** screens driven — target `.user__trigger` every time | **confirmed and slightly worse** |
| 2.2 | "35 dialogs, 14 use the directive" | **25 dialogs.** 14 use `astroModal`, **11 do not** | **the 11 is right; the 35 is wrong** |
| 2.3 | timer announces every second, label replaces the time, low-time is colour-only | all three confirmed by reading; the colour-only part confirmed in the stylesheet | **confirmed** |
| 2.4 | "the action column on **every** list table is off-screen", 10 screens | **12 tables.** 4 measured with the action column 0% visible; 1 measured only *partly* clipped; 1 measured **not affected at all**; 2 have **no action column**; 4 could not be staged | **overstated** |

**Things that turned out not to be true, or not to work:**

1. **"35 dialogs" is not reproducible.** There are **25**. The count comes out the same three ways:
   24 `.scrim`-based overlays in `app/features/**` (a 25th `.scrim` at `shell.component.html:145`
   is the nav drawer, not a dialog) plus one scrim-less `.sheet` at `exam-form.component.html:233`.
   `role="dialog"|"alertdialog"` appears 24 times and `aria-modal` 25 times, which reconciles
   exactly (`exam-forms.component.html:238` binds its role with `[attr.role]`, so a literal grep
   misses it). The **11 without the directive is correct** — that is the number that matters.
2. **The review says "three of them put `role="dialog"` on the scrim". It is four** — it missed
   `exam-form.component.html:233`, where the role sits on `.sheet` (a `position: fixed; inset: 0`
   overlay) and the actual box is `.sheet__panel` on the next line.
3. **The review's §8 CSS will not compile.** `.table thead { @extend .astro-visually-hidden; }`
   cannot work from a component stylesheet: `.astro-visually-hidden` is defined in
   `angular/src/styles/_base.scss:185`, reached only through the global `styles.scss`, and **no
   component stylesheet in `angular/src/app` contains a single `@use`, `@import` or `@extend`**.
   Sass will fail with "The target selector was not found". Even if it were `@use`d in, Angular's
   emulated encapsulation would rewrite the result with an `_ngcontent-*` attribute and scope it
   to one component. Write the rule out longhand.
4. **The review's §7 fix names a localisation key that does not exist.** `::Account` is in neither
   `en.json` nor `ar.json`. ABP's localizer returns `defaultValue || sourceKey`
   (`@abp/ng.core` fesm2022 bundle) — so `t('::Account')` renders the literal English string
   **"Account"** as the accessible name, in Arabic too, in an Arabic-first product. Same for
   `::Take:Hurry` in its §5 markup. Both keys must be added to both files.
5. **A sticky action column does not work here.** The obvious cheap alternative to collapsing the
   table — `position: sticky; inset-inline-end: 0` on the action cell — was measured in three
   variants (plain; with `border-collapse: separate`; with `contain: none` on `.astro-scroll-x`)
   on exams, candidates and users at 390px. In every case the cell's computed `position` became
   `sticky` and its visible fraction stayed at **0.00**. Do not pursue it without further
   investigation.
6. **`review-attempt.component.scss` and `review-queue.component.scss` each carry a complete
   copy-pasted dialog stylesheet** — `.scrim:185`, `.confirm:192`, `.panel:326`, and in
   `review-attempt` a whole `.table:75` / `.row-actions:227` block — and **neither template
   contains a dialog or a `<table>` at all** (`review-queue` does have a table; `review-attempt`
   does not). Dead CSS; not an accessibility defect, but it is why a `.scrim` grep over the
   stylesheets over-counts.
7. **One action `<th>` is bare.** The review states every action column carries a visually-hidden
   header. `attempt-monitor.component.html:72` is `<th scope="col"></th>` with nothing in it.
8. **axe is cleaner than the review found.** The review reported staff screens averaging two
   violated rules each. Today, under the same tag set, **every screen returns exactly one**
   (`button-name`), except `/results/running` which also returns `page-has-heading-one`
   (moderate). The contrast and muted-text items listed as closed on 2026-08-30 are genuinely gone.

---

# 2.2 — Dialogs that claim `aria-modal` and do not honour it

**Rank 1.** 11 of 25 dialogs. Measured on four of them; the mechanism is a single directive, so
the other seven are determined by one attribute and were read.

## First: what `modal.directive.ts` actually requires

`angular/src/app/shared/ui/modal.directive.ts` — selector `[astroModal]`, standalone. It supplies
four behaviours: focus moves to the first focusable child on open (`ngAfterViewInit`); Tab and
Shift+Tab wrap inside; Escape emits `dismiss`; focus returns to the opener on destroy
(`ngOnDestroy`), but only if that element is still connected.

Applying it at a site takes **exactly four things**, and the fourth is the one that is easy to miss:

1. **The bare attribute `astroModal` on the dialog *box*** — the element that draws the panel,
   never the scrim. `modal.directive.ts:29-30` says so in as many words: a trap around the
   backdrop would include the page the backdrop is covering.
2. **An output binding `(dismiss)="…"`** naming the component's own close method. The directive
   deliberately never guesses how to close; without this binding Escape does nothing at all.
   Bind it to **the same method Cancel binds to** — that is the whole point of the design.
3. **`ModalDirective` in the component's `imports: []` array.** Five of the eleven sites live in
   components that do not import it yet (listed per-site below). A standalone directive that is
   not imported silently does nothing — the attribute is inert, the template still compiles, and
   nothing warns you.
4. **Nothing else.** `tabindex="-1"` comes from the directive's `host`, so do **not** add it to
   the markup. The dialog must be inside an `@if` (all 11 are) so the lifecycle hooks fire on open
   and close.

**Where the role also has to move:** at four sites `role`/`aria-modal` sit on the full-viewport
overlay rather than on the box. Putting `astroModal` on the box while `role="dialog"` stays on the
overlay leaves the accessible dialog and the focus trap on two different elements. Move the
`role`, `aria-modal` and the label onto the box in the same edit.

## Measured, before any fix

Probe: open the dialog, read `document.activeElement`, press Escape, then press Tab up to 30 times
and watch whether focus ever leaves the box. Arabic, 1440×900.

| Dialog | `astroModal` | focus enters | Escape closes | Tab escapes |
|---|---|---|---|---|
| `exam-list.component.html:282` delete | no | **false** | **false** | **true** |
| `candidate-list.component.html:289` delete | no | **false** | **false** | **true** |
| `assignment.component.html:170` send panel | no | **false** | **false** | **true** |
| `exam-form.component.html:233` publish sheet | no | **false** | **false** | **true** |
| `candidate-list.component.html:309` edit | yes | true | true | — |
| `role-list.component.html:215` delete | yes | true | true | — |
| `user-list.component.html:170` delete | yes | true | true | — |
| `group-list.component.html:189` delete | yes | true | true | — |

Eight dialogs, one variable, no exceptions in either direction. In the four broken cases focus was
still sitting on the row button that opened the dialog, outside a container advertising
`aria-modal="true"`.

## The complete inventory — all 25 dialogs

### Needs the directive (11)

| # | File : line (box element) | Role | Where role currently sits | Add | Component imports `ModalDirective`? | Confirmed by |
|---|---|---|---|---|---|---|
| 1 | `angular/src/app/features/take/take-sitting.component.html:187` `.confirm` **(OFF-LIMITS)** | `alertdialog` | on the box — correct | `astroModal (dismiss)="confirmingSubmit.set(false)"` | **no** — `take-sitting.component.ts:55` is `imports: []` | reading (file in flux) |
| 2 | `angular/src/app/features/exams/exam-list.component.html:282` `.confirm` | `alertdialog` | on the box — correct | `astroModal (dismiss)="pendingDelete.set(null)"` | **no** — `exam-list.component.ts:26` | **measured** |
| 3 | `angular/src/app/features/candidates/candidate-list.component.html:289` `.confirm` | `alertdialog` | on the box — correct | `astroModal (dismiss)="pendingDelete.set(null)"` | yes | **measured** |
| 4 | `angular/src/app/features/candidates/candidate-list.component.html:188` `.panel` (import) | `dialog` | on the box — correct | `astroModal (dismiss)="closeImport()"` | yes | reading |
| 5 | `angular/src/app/features/questions/question-list.component.html:425` `.confirm` | `alertdialog` | on the box — correct | `astroModal (dismiss)="pendingDelete.set(null)"` | **no** — `question-list.component.ts:48` | reading |
| 6 | `angular/src/app/features/questions/question-list.component.html:257` `.qimport` | `dialog` | on the box — correct | `astroModal (dismiss)="closeImport()"` | **no** — same | reading |
| 7 | `angular/src/app/features/assignments/assignment.component.html:170` `.panel` (send) | `dialog` | on the box — correct | `astroModal (dismiss)="closeSend()"` | yes | **measured** |
| 8 | `angular/src/app/features/assignments/assignment.component.html:373` — role on **`.scrim`**, box is `.reissued` at `:374` | `dialog` | **on the overlay** | move role/`aria-modal`/label to `.reissued`, then `astroModal (dismiss)="closeReissued()"` | yes | reading |
| 9 | `angular/src/app/features/exams/exam-form.component.html:233` — role on **`.sheet`**, box is `.sheet__panel` at `:234` | `dialog` | **on the overlay** | move role/`aria-modal`/label to `.sheet__panel`, then `astroModal (dismiss)="showPublishPanel.set(false)"` | **no** — `exam-form.component.ts:33` | **measured** |
| 10 | `angular/src/app/features/results/attempt-monitor.component.html:109` — role on **`.scrim`**, box is `.dialog` at `:110` | `dialog` | **on the overlay** | move role/`aria-modal`/label to `.dialog`, then `astroModal (dismiss)="cancelEnd()"` | **no** — `attempt-monitor.component.ts:30` | reading |
| 11 | `angular/src/app/features/results/attempt-monitor.component.html:138` — role on **`.scrim`**, box is `.dialog` at `:139` | `dialog` | **on the overlay** | move role/`aria-modal`/label to `.dialog`, then `astroModal (dismiss)="cancelDiscard()"` | **no** — same | reading |

Five components need the import added: `exam-list`, `exam-form`, `question-list`,
`attempt-monitor`, and `take-sitting` (off-limits). The import line to copy is the one already in
`candidate-list.component.ts`.

### Already correct (14) — do not touch

| File : line | Role | Dismiss binding |
|---|---|---|
| `assignment.component.html:334` `.confirm` | `dialog` | `cancelExtend()` |
| `candidate-list.component.html:309` `.confirm` | `dialog` | `cancelEdit()` |
| `catalog.component.html:360` `.dialog` | `alertdialog` | — |
| `catalog.component.html:383` `.dialog--wide` | `dialog` | — |
| `exam-forms.component.html:235` `.confirm` | `[attr.role]` — `alertdialog` for delete, else `dialog` | `cancelPending()` |
| `exam-structure.component.html:333` `.confirm` | `alertdialog` | `cancelDelete()` |
| `group-list.component.html:189` `.dialog` | `alertdialog` | — |
| `group-list.component.html:212` `.dialog--wide` | `dialog` | — |
| `role-list.component.html:96` `.confirm` | `dialog` | `cancel()` |
| `role-list.component.html:148` `.perms` | `dialog` | `closePermissions()` |
| `role-list.component.html:215` `.confirm` | `alertdialog` | `cancelDelete()` |
| `tenant-list.component.html:71` `.confirm` | `dialog` | `cancel()` |
| `tenant-list.component.html:134` `.confirm` | `alertdialog` | `cancelDelete()` |
| `user-list.component.html:170` `.dialog` | `alertdialog` | — |

### Worked example — the two shapes

**Shape A, role already on the box** (`exam-list.component.html:282`). One line becomes three:

```html
<!-- before -->
<div class="confirm" role="alertdialog" aria-modal="true" aria-labelledby="confirmTitle">

<!-- after -->
<div
  class="confirm"
  astroModal
  (dismiss)="pendingDelete.set(null)"
  role="alertdialog"
  aria-modal="true"
  aria-labelledby="confirmTitle">
```

and in `exam-list.component.ts:26`:

```ts
imports: [FormsModule, RouterLink, DatePipe, PageHeaderComponent, StatusChipComponent, ModalDirective],
```

**Shape B, role on the overlay** (`attempt-monitor.component.html:109-110`). The attributes move
down one element:

```html
<!-- before -->
<div class="scrim" role="dialog" aria-modal="true" [attr.aria-label]="t('::Monitor:End:Title')">
  <div class="dialog">

<!-- after -->
<div class="scrim">
  <div
    class="dialog"
    astroModal
    (dismiss)="cancelEnd()"
    role="dialog"
    aria-modal="true"
    [attr.aria-label]="t('::Monitor:End:Title')">
```

### Two things worth fixing while you are in there, not required by 2.2

- **The submit dialog's whole reason for existing is not wired to it.**
  `take-sitting.component.html:187` has `aria-describedby: null` (measured). The unanswered-question
  count sits in a `<p>` inside the dialog with no `id`. Give that paragraph an `id` and point
  `aria-describedby` at it. **Off-limits file — hand this to whoever owns it.**
- **Nothing in the app is ever made `inert`.** A repo-wide grep for `inert` returns only a CSS
  class named `chip--inert`. The directive's Tab trap is what stands in for it, which is why the
  directive is not optional.

### How to verify 2.2

Per dialog, with the keyboard only:

1. Open it. Focus must land inside. `document.activeElement` must be a control in the box.
2. Press Escape. It must close, and focus must return to the control that opened it.
3. Re-open, hold Tab past the last control. Focus must wrap to the first, not step into the page.
4. Re-run axe — it will not catch any of this, which is exactly why this needs a keyboard.

A scripted check: open the dialog, then
`document.querySelector('[role=dialog],[role=alertdialog]').contains(document.activeElement)`
must be `true`; press Escape; the selector must return `null`.

---

# 2.4 — At 390px the action column is off-screen

**Rank 2.** This is the only item on the list that hits people who are not using assistive
technology at all. A coordinator on a phone cannot edit, publish, assign or delete anything
without first discovering an undiscoverable gesture.

## Measured, 390px, before any fix

Viewport 390px; the scroll container's own client width is 358px in every case. "Visible" is the
fraction of the first row's action cell that falls inside the container's visible box at scroll
position 0.

| Screen | File : `<table>` line | Container | Overflow **ar** | Overflow **en** | Action cell | Visible **ar** / **en** |
|---|---|---|---|---|---|---|
| Exams | `exam-list.component.html:78` (wrapper `:77`) | `.astro-scroll-x` | **531px** | **673px** | `.row-actions` 333px wide | **0% / 0%** |
| Users | `user-list.component.html:118` (wrapper `:117`) | `.astro-scroll-x` | **358px** | **366px** | `.actions` 125px | **0% / 0%** |
| Candidates | `candidate-list.component.html:84` (wrapper `:83`) | `.astro-scroll-x` | **257px** | **302px** | `.row-actions` 117px | **0% / 0%** |
| Groups | `group-list.component.html:122` (wrapper `:121`) | `.astro-scroll-x` | **237px** | **258px** | `.actions` 181px | **0% / 0%** |
| Roles | `role-list.component.html:22` (wrapper `:21`) | `.table-wrap` | 19px | 46px | `.role-actions` 202px | **91% / 79%** |
| Organisations | `tenant-list.component.html:22` (wrapper `:21`) | `.table-wrap` | **0px** | **0px** | `.tenant-actions` 120px | **100% / 100%** |
| Results | `result-list.component.html:130` (wrapper `:129`) | `.astro-scroll-x` | 361px | 426px | **no action column** | — |

Page horizontal scroll was **0 on every screen in both languages** — the `contain: paint` fix in
`_base.scss:207` is doing its job and is not the problem.

Not stageable in this dataset, so **read, not measured**: questions
(`question-list.component.html:139`, 6 columns, 2 actions), assignments
(`assignment.component.html:43`, 6 columns, 4 actions, plus a token column — almost certainly the
worst of the lot), review queue (`review-queue.component.html:51`, 7 columns, 1 action), attempt
monitor (`attempt-monitor.component.html:65`, 5 columns, 2 actions), item analysis
(`item-analysis.component.html:41`, 5 columns, **no action column**).

**So the claim "every list table" is wrong.** Twelve tables; two of them have no action column at
all; organisations fits comfortably at 390px and needs nothing; roles is clipped by 19–46px, not
"entirely beyond the edge". Four are confirmed unreachable, four more are unreachable by reading.

## The project's existing patterns — and the honest answer about them

I looked for a pattern to follow. **There is no table-to-cards pattern in this codebase.** A
repo-wide search for `data-label`, `content: attr(`, and `display: block` on table elements inside
a media query returns **zero hits**, and **no `<td>` anywhere carries a `data-label`**. What the
project does have:

- **Its breakpoint convention is `rem` for content and `px` for the shell.** Content-level
  breakpoints in use: `40rem` twice (`assignment.component.scss:495`,
  `take/answers/matching-answer.component.ts:58`), `60rem` twice
  (`catalog.component.scss:27`, `exam-forms.component.scss:149`). Shell breakpoints are
  `1024px`/`1023px`/`480px` in `shell.component.scss`. **`48rem` and `768px` appear nowhere.**
  The review's `48rem` is not the project's number; `40rem` is.
- **Its idiom for collapsing a multi-column row is "stack, don't label".** The closest analogue is
  `.recipient` — markup `assignment.component.html:194-211`, CSS
  `assignment.component.scss:470-497` — a three-column grid that becomes `grid-template-columns:
  1fr` at `40rem` with no generated labels, because a name reads as a name.
- **`.astro-scroll-x` (`_base.scss:199-210`) has no affordance of any kind** — no fade, mask,
  shadow or hint. That is a real and separate gap.
- **Roles and organisations bypass `.astro-scroll-x`** for a local `.table-wrap { overflow-x: auto }`
  (`role-list.component.scss:5`, `tenant-list.component.scss:5`) that lacks the `contain: paint`
  RTL fix. Normalising those two to `.astro-scroll-x` is a two-line change worth making anyway.
- **There are five different action-cell class names**, not one: `.row-actions` (exams, questions,
  candidates, assignments, review), `.actions` (groups, users, attempt monitor), `.role-actions`,
  `.tenant-actions`. **The review's rule names only `.row-actions` and so misses half the tables.**

**Verdict: "stack, don't label" is the project's idiom but it does not survive here.** Stacking
the exams row without labels produces `20 / 30 / 60` for Questions / Duration / Pass mark. The
labels are load-bearing, so `data-label` is unavoidable and the markup edit is per-template.

## The fix, measured

Two parts. Part 1 is CSS only and is where the win is; part 2 is the markup that makes part 1
legible.

**Part 1 — one global rule.** Put it in `angular/src/styles/_base.scss` immediately after the
`.astro-scroll-x` block closes at **line 210** (before the `img, video` rule at `:212`). That file
already owns every cross-cutting layout and accessibility primitive, it is unscoped so it reaches
every component template, and it is the only place a `.table` rule can live without being written
out twelve times.

```scss
// --- Tables on a phone ------------------------------------------------------
// Below this width a table cannot show its action column, and a horizontal
// scroll inside a box nobody can see is not a way to reach a Delete button.
// Each row becomes a card and each cell states its own column, because the
// numbers here — questions, duration, pass mark — mean nothing unlabelled.
@media (max-width: 40rem) {
  .astro-scroll-x, .table-wrap { overflow-x: visible; }

  .table { display: block; border: 0; }

  // Written out rather than @extend-ed: .astro-visually-hidden lives in the
  // global layer and component stylesheets import nothing, so @extend cannot
  // resolve it and Angular's emulated encapsulation would scope it if it could.
  .table thead {
    position: absolute;
    inline-size: 1px;
    block-size: 1px;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
  }

  .table tbody { display: block; }

  .table tbody tr {
    display: block;
    margin-block-end: var(--astro-space-3);
    padding: var(--astro-space-1) var(--astro-space-2);
    border: 1px solid var(--border-subtle);
    border-radius: var(--astro-radius-md);
    background: var(--surface-raised);
  }

  // The !important is not decoration. Component stylesheets emit
  // `.table td[_ngcontent-x]`, which outranks a bare global selector, and the
  // action cells specifically set inline-size: 1% and white-space: nowrap to
  // stay out of the table's column sizing — both of which overflow a card.
  .table tbody td {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    gap: var(--astro-space-2);
    inline-size: auto !important;
    white-space: normal !important;
    text-align: start !important;
    border: 0;
    padding: var(--astro-space-2) var(--astro-space-1) !important;
  }

  .table tbody td::before {
    content: attr(data-label);
    flex: 0 0 auto;
    color: var(--text-muted);
    font-size: var(--astro-text-2xs);
  }
}
```

Every token used above was checked and exists: `--astro-space-1/2/3` (`_tokens.scss:110-112`),
`--astro-radius-md` (`:121`), `--surface-raised` (`:147`), `--border-subtle` (`:158`),
`--text-muted` (`:155`), `--astro-text-2xs` (`:90`).

**Measured effect of exactly this rule, injected at runtime, 390px, Arabic:**

| Screen | Action cell visible | Container overflow | Page h-scroll | Page height |
|---|---|---|---|---|
| Exams | 0% → **100%** | 535px → **0** | 0 → 0 | 2934px → **7012px** |
| Candidates | 0% → **100%** | 257px → **0** | 0 → 0 | 1983px → **5570px** |
| Users | 0% → **100%** | 358px → **0** | 0 → 0 | 844px → **1642px** |

It works, and the cost is real: a twenty-row exams list goes from a 2,900px page to a 7,000px one.
That is the trade — a long page you can scroll with one thumb, against a short page whose buttons
do not exist. Take it, and consider dropping low-value columns below `40rem` (`Created`,
`Pass mark`) with `td:nth-child(n) { display: none }` if the length is judged too much.

**Part 2 — `data-label` on every `<td>` in the eleven tables that have one.** Without it,
`content: attr(data-label)` renders empty; the rule is safe to land first and the cards are simply
unlabelled until this follows. Roughly 60 cells. Bind it to the same key the `<th>` uses:

```html
<!-- exam-list.component.html — the <th> at :82 is ::Exam:Category -->
<td [attr.data-label]="t('::Exam:Category')">{{ exam.categoryName }}</td>
```

The action cell takes no label — the buttons name themselves:

```html
<td class="row-actions" [attr.data-label]="null">
```

**Part 3, small and separate — the scroll affordance above `40rem`.** Between `40rem` and the
desktop layout a table still scrolls inside a box with no sign that it does. In the same
`_base.scss` block:

```scss
@media (min-width: 40.0625rem) {
  .astro-scroll-x { scroll-snap-type: none; }
  .astro-scroll-x::after { /* or a mask-image fade on the container */ }
}
```

This one was **not measured** and is a judgement call, not a defect with a number attached.

**Part 4 — normalise the two outliers.** `role-list.component.html:21` and
`tenant-list.component.html:21` use `.table-wrap`; change both to `.astro-scroll-x` and delete
`.table-wrap` from `role-list.component.scss:5` and `tenant-list.component.scss:5`. They gain the
`contain: paint` RTL fix and fall under the rule above.

### How to verify 2.4

At 390px, in Arabic **and** English (the direction changes which edge the column hides behind, and
English is 20–27% wider — exams overflows by 531px in Arabic and 673px in English):

```js
// per table, at scroll position 0 — must be 1 after the fix, is 0 before it
const t = document.querySelector('table');
const w = t.closest('.astro-scroll-x, .table-wrap') || t.parentElement;
const c = [...t.querySelector('tbody tr').children].pop();
const wr = w.getBoundingClientRect(), cr = c.getBoundingClientRect();
(Math.max(0, Math.min(cr.right, wr.right) - Math.max(cr.left, wr.left)) / cr.width);
```

And the invariant that must not regress:
`document.documentElement.scrollWidth - document.documentElement.clientWidth === 0`
on every screen, in both languages. It is 0 today on all of them.

---

# 2.3 — The exam timer

**Rank 3.** **File: `angular/src/app/features/take/take-sitting.component.html:9-16` — OFF-LIMITS.**
Intended markup below, not a patch. Line numbers will have moved.

Confirmed **by reading**, not by a screen reader — I did not have one to drive.

## What is there now

```html
<span
  class="clock clock--{{ clockTone() }}"
  role="timer"
  aria-live="polite"
  [attr.aria-label]="t('::Take:TimeLeft')">
  <i class="bi bi-clock" aria-hidden="true"></i>
  <span class="astro-numeric">{{ clock() }}</span>
</span>
```

`clock()` recomputes every second (`take-sitting.component.ts:95-107`).
`clockTone()` (`:110-118`) returns `calm` above 5:00, `warn` at or below 5:00, `urgent` at or below 1:00.

Three defects, all confirmed:

1. **`aria-live="polite"` overrides an implicit `off`.** In ARIA 1.2, `role="timer"` is a subclass
   of `status` whose **implicit `aria-live` is `off`** — precisely so a ticking value does not
   flood the buffer. Setting it to `polite` produces **one announcement per second, indefinitely**.
   A polite queue that refills every second is a queue nothing else ever gets through: not the
   question text, not the save indicator at `:18`, not the error at `:31`.
2. **`aria-label` replaces the time instead of describing it.** `role="timer"` inherits **Name
   From: author** from `status` — the element's contents are not used to compute its name. So the
   accessible name of this element is exactly `"الوقت المتبقي"` / `"Time remaining"`. **The digits
   are not in it.** A user who navigates to the clock is told what it is and never what it says.
3. **Running low is signalled by colour alone.** `take-sitting.component.scss:57-70`: the three
   tones differ only in `background` and `color` (`--surface-sunken`, `--status-pending-bg`,
   `--status-fail-bg`). Same `bi-clock` glyph, same weight, same size, no text. WCAG 1.4.1
   Use of Colour.

## What it should be instead

```html
<!-- One element, three jobs kept apart: the clock says the time, the icon says
     the urgency, and a separate region says the two things worth interrupting for.

     role="timer" keeps its implicit aria-live="off": the spec gives it that
     default so a per-second value does not flood the buffer, and overriding it
     to "polite" made this clock the only thing a candidate could hear.

     timer takes its name from the author, so the name is built with
     aria-labelledby out of the label and the digits together — an aria-label
     here would replace the time rather than describe it. -->
<span class="clock clock--{{ clockTone() }}"
      role="timer"
      aria-labelledby="clockLabel clockValue">
  <i class="bi"
     [class.bi-clock]="clockTone() === 'calm'"
     [class.bi-alarm-fill]="clockTone() !== 'calm'"
     aria-hidden="true"></i>
  <span id="clockLabel" class="astro-visually-hidden">{{ t('::Take:TimeLeft') }}</span>
  <span id="clockValue" class="astro-numeric">{{ clock() }}</span>
  @if (clockTone() !== 'calm') {
    <span class="clock__word">{{ t('::Take:Hurry') }}</span>
  }
</span>

<!-- Two announcements in a whole sitting: one at 5:00, one at 1:00. Empty the
     rest of the time, so the region only ever changes twice. -->
<span class="astro-visually-hidden" role="status">{{ timeWarning() }}</span>
```

```scss
// The tone must survive greyscale and a colour-blind reader: the glyph changes
// shape and the weight changes with it, so colour is confirmation, not carrier.
.clock--warn,
.clock--urgent { font-weight: var(--astro-weight-bold); }

.clock__word { font-size: var(--astro-text-sm); }
```

`bi-alarm-fill` exists in the bundled `bootstrap-icons` — checked.

**Why `aria-labelledby` rather than the design review's version.** The review proposed dropping
`aria-label` entirely and relying on a visually-hidden label span sitting next to the digits. That
reads correctly when a user traverses the page linearly, but it leaves the element with **no
accessible name at all** — `role="timer"` takes its name from the author and from nowhere else, so
with no `aria-label` and no `aria-labelledby` the name is empty. `aria-labelledby="clockLabel
clockValue"` concatenates the two into `"الوقت المتبقي ١٢:٣٤"`, which is a name that contains the
number. That is the difference between hearing the time and hearing that there is a time.

**Why `role="status"` and not `role="alert"` for the warnings.** Once the per-second flood stops,
a polite region gets through in about a second. `role="alert"` is assertive and cuts a candidate
off mid-sentence while they are reading a question. If the team decides the last minute is worth
interrupting for, use a **second**, alert-role element for the 1:00 case only — do not switch
`aria-live` on a live element, which is unreliable across screen readers.

## What this needs that does not exist yet

- **`timeWarning()` does not exist** in `take-sitting.component.ts`. It has to be written: a
  computed returning the localised warning at exactly the 5:00 and 1:00 crossings and `''`
  otherwise. It must be edge-triggered, not threshold-triggered — a computed that returns the same
  string for 299 consecutive seconds is fine (the live region only announces on change), but one
  that flips back and forth is not.
- **`::Take:Hurry` does not exist** in `en.json` or `ar.json`. Nor do keys for the two warnings.
  Add, to both `src/InternshipManagementSystem.Domain.Shared/Localization/InternshipManagementSystem/en.json`
  and `ar.json`, in the existing alphabetical position near `Take:TimeLeft` (line 917):
  - `Take:Hurry` — a short word shown beside the clock, e.g. "Hurry" / «أسرِع»
  - `Take:TimeWarning:Five` — e.g. "Five minutes remaining" / «بقيت خمس دقائق»
  - `Take:TimeWarning:One` — e.g. "One minute remaining" / «بقيت دقيقة واحدة»
- `::Take:TimeLeft` already exists in both files at line 917 — keep using it.

### How to verify 2.3

- **The flood:** with a screen reader running, open the paper and read a question. Before: the
  clock interrupts every second and the question never finishes. After: the clock is silent until
  you navigate to it.
- **The name:** navigate to the clock. It must say the label **and the digits**. Scripted:
  the computed accessible name must match `/\d/`.
  In axe terms, `role="timer"` with `aria-live="polite"` is not a rule violation — **axe does not
  catch any part of this**, which is why it was found by reading and must be verified by ear.
- **The colour:** at 5:01 and at 4:59, screenshot and compare in greyscale. The glyph must differ,
  not only the fill.

---

# 2.1 — The account menu button has no accessible name

**Rank 4 by harm, first by cost.** One attribute and two localisation keys.

**File: `angular/src/app/layout/shell.component.html:85-92`.**

```html
<button
  type="button"
  class="user__trigger"
  (click)="toggleUserMenu()"
  [attr.aria-expanded]="userMenuOpen()"
  aria-haspopup="menu">
  <i class="bi bi-person-circle" aria-hidden="true"></i>
</button>
```

The only child is `aria-hidden`, so the accessible name is empty. A screen reader announces
**"button, collapsed"**. It is the control that signs you out.

## Measured

axe-core 4.13.0, tags `wcag2a, wcag2aa, wcag21a, wcag21aa, wcag22aa, best-practice`, Arabic,
1440×900, signed in as `admin`:

| Screen | Rule | Impact | Nodes | Target |
|---|---|---|---|---|
| dashboard, exams, questions, candidates, groups, assignments, results, `/results/running`, review, users, roles, catalog, settings — **13 screens** | `button-name` | **critical** | 1 | `.user__trigger` |
| `/results/running` also | `page-has-heading-one` | moderate | 1 | `html` |

**Every other rule was clean on every screen.** `button-name` is now the only violation in the
staff product apart from that one missing `<h1>`, and it is the same node on every screen.
An in-page probe confirmed `aria-label: null`, `title: null`, `textContent: ""` on all 14 loads.

It is the **only** icon-only button in the app without a label — the sidebar toggle
(`shell.component.html:14-21`, `[attr.aria-label]="t('::ToggleNavigation')"`) and the theme toggle
(`:71-82`, `[attr.aria-label]="t('::ToggleTheme')"`) both have one. A one-line omission, not a
pattern.

## The fix

**`shell.component.html`, line 90** — add one attribute after `aria-haspopup="menu"`:

```html
<button
  type="button"
  class="user__trigger"
  (click)="toggleUserMenu()"
  [attr.aria-expanded]="userMenuOpen()"
  aria-haspopup="menu"
  [attr.aria-label]="t('::Account')">
  <i class="bi bi-person-circle" aria-hidden="true"></i>
</button>
```

**And — this is the part the design review left out — add the key to both files**, or the fix
ships an English word as the Arabic accessible name. `src/InternshipManagementSystem.Domain.Shared/Localization/InternshipManagementSystem/en.json`
and `ar.json`, alphabetically before `"Logout"` (line 410):

```json
"Account": "Account"
```
```json
"Account": "الحساب"
```

ABP's localizer returns `defaultValue || sourceKey` on a miss, so without these two lines
`t('::Account')` renders the literal string `Account` in Arabic. Verified by reading
`@abp/ng.core`'s bundled localizer.

## Adjacent, in the same eight lines — worth a decision, not part of 2.1

`shell.component.html:95` declares `role="menu"` with `role="menuitem"` children (`:101`, `:105`)
and **there is no arrow-key handling anywhere in the component**. ARIA menu semantics promise a
keyboard model — Up/Down to move, Escape to close, Home/End — that this code does not implement,
so a screen-reader user is told to press arrow keys that do nothing. There is also no Escape
handler and no focus management on open. Dropping both roles is more honest than half-implementing
them; implementing them properly is the better answer if someone has the time.

Related, and confirmed: **`shell.component.html:143` carries a comment saying the sidebar scrim is
"inert to assistive tech, which reaches the same result with Escape or by moving focus". There is
no Escape handler for the sidebar either.** A grep for `Escape` across `angular/src/app` returns
`modal.directive.ts` and that comment, and nothing else. The comment describes an intention, not
the code.

### How to verify 2.1

```js
// on any staff screen — must be a non-empty string after the fix
document.querySelector('.user__trigger').getAttribute('aria-label')
```

Then re-run axe against the same 13 screens; `button-name` must return zero nodes on all of them.
And check it in **both** languages — the point of the localisation keys is that Arabic does not
get the word "Account".

---

## Appendix — how each claim in this document was established

**Measured in a browser:**

- 2.1 — axe-core 4.13.0 across 14 loads (13 distinct screens), Arabic, 1440×900; plus a DOM probe
  of `.user__trigger` on each.
- 2.2 — focus/Escape/Tab probes on 8 dialogs, Arabic, 1440×900. Four without the directive, four
  with; the split is clean.
- 2.4 — geometry probes on 7 tables at 390px in Arabic and English, before and after injecting the
  proposed CSS; plus three failed sticky-column variants.
- The `40rem` card rule was injected at runtime on exams, candidates and users and the
  before/after numbers above are from that run.

**Established by reading source:**

- The full 25-dialog inventory, and which seven of the eleven were not opened in a browser.
- Every `file:line` in this document (grep + read; the take-sitting numbers are already stale).
- Which five components lack the `ModalDirective` import.
- All of 2.3 — the ARIA semantics, the stylesheet's colour-only tones, and the missing
  `timeWarning()` and localisation keys. **No screen reader was run.**
- The `@extend` finding, the missing `::Account` and `::Take:Hurry` keys, and ABP's
  missing-key fallback.
- The five action-cell class names, the breakpoint census, and the absence of any existing
  table-to-cards pattern.

**Not established either way:**

- The four tables that could not be staged with rows at 390px (questions, assignments, review
  queue, attempt monitor). Their markup says they will behave like the four that were measured,
  but that is an inference.
- The scroll affordance in 2.4 Part 3.
- Whether the `40rem` page-height cost is acceptable. That is a product decision, and the numbers
  to decide it with are in the table above.
