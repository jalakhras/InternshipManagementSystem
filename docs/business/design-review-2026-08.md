# Astrolabe — design review

**Date:** 2026-08-30
**Scope:** the whole product, Arabic and English, 1440px and 390px, light and dark.
**Method:** every staff screen and the full candidate journey driven in a real browser against the running app (`localhost:4200` + `localhost:44373`), plus a full read of the stylesheet and component layer.
**Grounding:** `ui-ux-pro-max` (79 styles, 192 palettes, 119 UX guidelines, stack: `angular`). Each finding names the guideline it comes from.

---

## How this was checked, and what that means for the findings

Three passes, deliberately overlapping, because each catches what the others cannot.

1. **Live browser audit.** 28 staff screens × {Arabic, English} × {1440px, 390px}, plus a dark pass, plus three complete candidate sittings (entry → paper → submit → result). An in-page probe measured computed styles, real glyph geometry, contrast ratios, target sizes and accessible names on every rendered element.
2. **axe-core 4.13.0**, run against 15 screens under `wcag2a, wcag2aa, wcag21a, wcag21aa, wcag22aa, best-practice`. **It is not installed in the project** — it was fetched with `npm pack` into a scratch directory and injected with `page.addScriptTag`. Nothing in `package.json` changed.
3. **Source audit** of the token layer, base layer, and all 14 feature folders.

**Everything ranked below in the top ten was reproduced live and measured.** Where a finding comes from reading code and I could not stage the data to see it, it says so explicitly and tells you how to check it.

**One caveat on the guideline database.** `ui-ux-pro-max` has no RTL or bidirectional-text entries — I queried `--domain ux` and `--stack angular` for direction, mirroring and logical properties and got general layout rules back, twice. So the RTL findings below are judged against **this codebase's own written rules** (`_tokens.scss:4-10`, `_base.scss:36-85`) and standard bidi practice, not against a database match. I am flagging that rather than dressing general advice up as a citation.

---

## The short version

The RTL engineering here is the best I have seen in an Angular codebase, and I want to lead with that because it changes what this review is about. **The defect you warned me about — `.astro-numeric` / `.astro-ltr` dragging a table cell to the opposite edge from its `<th>` — is fixed.** I measured the actual glyph boxes of every column of every table on every list screen: 30 columns, all of them sharing an edge with their header. Not one drifted. There are also **zero physical CSS properties** (`margin-left`, `text-align: right`, `float`, …) in roughly 5,000 lines of component CSS, and **no horizontal page scroll on any of 28 screens at 390px**.

So the problems are not where the team has been looking. They are in three places:

- **Dark mode**, where ~145 references reach past the semantic token layer into raw primitives. Two of those make text on the candidate's screen literally unreadable.
- **The candidate's exam paper**, which is clean and calm but is missing a question navigator entirely.
- **Bootstrap**, which still owns four things the token layer never took back.

---

## The ten that matter most

Ranked by what they cost a real user, worst first.

---

### 1. In dark mode, the answer a candidate selects becomes invisible

**Screen:** the exam paper, `/exam/{token}` — every single-choice and multi-choice question, the most-used question type in the product.
**Element:** `.choice--picked` → `.choice__text`
**File:** `angular/src/app/features/take/answers/choice-answer.component.ts:77`

```scss
&--picked { border-color: var(--astro-brand-600); background: var(--astro-brand-50); }
```

`--astro-brand-50` is `#ecfaff` — a **primitive**, not a semantic token, so it does not change in dark mode. `.choice__text` sets no colour, so it inherits `--text-primary`, which *is* semantic and flips to `#f5f7f9`.

**Measured live: 1.01:1.** Not "low contrast" — the same colour.

The candidate can see *that* they picked something (the border survives) and cannot read *what*. To re-read their own answer they must deselect it. Under a countdown, on the screen where a mistake costs marks.

**Fix** — pair a semantic background with a semantic foreground, never a primitive with an inherited one:

```scss
&--picked {
  border-color: var(--accent);
  background: var(--accent-subtle);
  color: var(--accent-subtle-text);
}
```

**Guideline:** Accessibility → *Color Contrast* (High): "Minimum 4.5:1 ratio for normal text." Also the codebase's own rule, `_tokens.scss:9-10`: *"A component that reaches past semantic into primitive is a bug: the theme stops being switchable at that point."*

---

### 2. In dark mode, the candidate cannot read whether they passed

**Screen:** the result page, `/exam/{token}/result`
**Elements:** `.score__verdict` ("لم تنجح" / "Not passed"), `.score__marks`
**File:** `angular/src/app/features/take/take-result.component.scss:43,45,47`

Same root cause. `.score { background: var(--astro-fail-50) }` — `#fdecea`, a primitive — while the verdict and marks inherit dark-mode text colours.

**Measured live: 1.06:1.** The screenshot shows a glaring near-white block on a near-black page with the single most important word in the entire product invisible inside it. Only the `0%` figure is legible, because it alone has an explicit colour.

**Fix:**

```scss
.score          { background: var(--status-fail-bg); color: var(--status-fail-text); }
.score--passed  { background: var(--status-pass-bg); color: var(--status-pass-text); }
.pending        { background: var(--status-pending-bg); color: var(--status-pending-text); }
```

The semantic tokens already exist with correct dark overrides at `_tokens.scss:214-224`. This is a find-and-replace, not a redesign.

**Scale of it:** ~145 raw-primitive references across the app; the ~24 that use a `-50` (near-white) background are the ones that visibly break. Beyond these two, the countdown pill at low time (`take-sitting.component.scss:68-69`) and six link-state chips (`assignment.component.scss:424-428`) glow white in dark mode.

**Guideline:** Accessibility → *Color Contrast* (High); Priority 6, Typography & Color — "Semantic color tokens / Don't: raw hex in components."

---

### 3. There is no question navigator

**Screen:** the exam paper.
**Element:** the footer — `Previous` and `Next`, and nothing else.

I sat three exams. Confirmed at 1440px and 390px, Arabic and English: a candidate can move one question forward and one back. That is the entire navigation model.

The cost compounds:

- To revisit Q3 from Q20 with back-navigation on: **17 taps**, each a network round-trip and a full-pane spinner, on a running clock.
- **No way to see which questions are blank** until the submit dialog announces a bare count.
- The submit dialog then says *"You have 2 question(s) with no answer"* — and gives you **no way to reach them**. I screenshotted this. The one moment the product knows something actionable, it offers no action.
- No flag-for-review.

The server already ships the data. `take.models.ts:134-135` defines `answered: boolean[]` with the comment *"One entry per position, so the map can show what is left without fetching the paper."* **The map was designed and never built.** `goTo(position)` is public at `take-sitting.component.ts:220` and nothing calls it with an arbitrary position.

**Fix** — a palette between the bar and the paper, driven by `state().answered`:

```scss
.palette__list {
  display: flex; flex-wrap: wrap; gap: var(--astro-space-2);   /* 8px — meets touch spacing */
  padding: var(--astro-space-3) var(--astro-space-5);
  max-block-size: 4.5rem; overflow-y: auto;                     /* two rows at 390px, then scrolls */
  list-style: none; margin: 0;
}
.palette__cell {
  inline-size: var(--astro-touch-min); block-size: var(--astro-touch-min);  /* 44×44 */
  border: 1px solid var(--border-strong); border-radius: var(--astro-radius-sm);
  background: var(--surface-raised); color: var(--text-secondary);
}
.palette__cell--done {                       /* fill + tick + weight, never hue alone */
  background: var(--accent-subtle); color: var(--accent-subtle-text);
  border-color: var(--accent); font-weight: var(--astro-weight-semibold);
}
.palette__cell--here { outline: 2px solid var(--focus-ring); outline-offset: 1px; }
```

RTL flow comes free — `flex-wrap` starts the row at the right edge and wraps down. At 390px: six 44px cells per row.

Make the unanswered count in the submit dialog a list of links into that palette, and the dialog becomes useful instead of merely alarming.

**Guideline:** Priority 9, Navigation Patterns (High) — "Predictable back, deep linking / Don't: overloaded nav, broken back behavior."

---

### 4. `--text-muted` fails AA on all three light surfaces — 262 contrast violations

**Screens:** all 12 staff screens.
**Token:** `--text-muted: var(--astro-ink-500)` = `#6b7988` — `_tokens.scss:150`

Measured:

| On | Ratio | Needs | |
|---|---|---|---|
| `--surface-raised` `#ffffff` | **4.45:1** | 4.5 | fails |
| `--surface-page` `#f5f7f9` | **4.15:1** | 4.5 | fails |
| `--surface-sunken` `#eceff3` | **3.86:1** | 4.5 | fails clearly |

That third row is **every table header in the product** — `thead th` is `--text-muted` on `--surface-sunken`, on 12 tables.

axe-core reports **262 nodes across 12 screens**, and every sample it returned is this one token: `.sidebar__heading`, `th`, `.candidate__email`, `.stat__label`, `.field__hint`, `.category__meta`.

**One token fixes all 262.** Candidates:

| Value | on raised | on sunken | on page |
|---|---|---|---|
| `#6b7988` *(current)* | 4.45 | 3.86 | 4.15 |
| `#5f6d7c` | 5.30 | 4.59 | 4.93 |
| **`#5a6876`** *(recommended)* | **5.71** | **4.95** | **5.32** |
| `--astro-ink-600` `#4e5c6b` | 6.85 | 5.94 | 6.38 |

`#5a6876` clears AA on all three with headroom and still reads as visibly quieter than `--text-secondary`. Add it as `--astro-ink-550` so the primitive layer is not skipped.

**Dark mode is fine** — `#94a1b0` measures 6.33–7.10 on all three dark surfaces. This is a light-mode-only defect.

Two related items: `.sidebar__heading` (`shell.component.scss`) and `.level` (`exam-list.component.scss:119`) are **11px**, below the 12px floor the token file sets at `_tokens.scss:89`.

**Guideline:** Accessibility → *Color Contrast* (High); Typography → *Contrast Readability* (High) — "Don't: gray text on gray background."

---

### 5. The countdown floods screen readers, and never announces the time

**Screen:** the exam paper.
**File:** `angular/src/app/features/take/take-sitting.component.html:9-16`

```html
<span class="clock clock--{{ clockTone() }}" role="timer" aria-live="polite"
      [attr.aria-label]="t('::Take:TimeLeft')">
```

Three faults in one element, verified live:

1. **`role="timer"` carries an implicit `aria-live="off"`** precisely so a ticking value does not flood the buffer. Overriding it to `polite` produces **one announcement every second, indefinitely**. Nothing else on the page — not the question, not a save failure — can get through a polite queue that refills every second.
2. **`role="timer"` takes its name from the author**, so `aria-label` *replaces* the contents. A screen-reader user who navigates to the clock hears "الوقت المتبقي" and **not the time**. The one thing it exists to say is the one thing it does not say.
3. **Low time is signalled by colour alone.** `take-sitting.component.scss:67-69` changes only `background` and `color` across calm → warn → urgent. The icon stays `bi-clock`, the size and weight stay identical, no word changes. And the two alarm states are close in luminance (`#fdf3e3` vs `#fdecea`).

**Fix** — silence the tick, announce at thresholds, and give low time a second channel:

```html
<span class="clock clock--{{ clockTone() }}" role="timer" aria-live="off">
  <i class="bi" [class.bi-clock]="clockTone()==='calm'"
                [class.bi-alarm-fill]="clockTone()!=='calm'" aria-hidden="true"></i>
  <span class="astro-visually-hidden">{{ t('::Take:TimeLeft') }}</span>
  <span class="astro-numeric">{{ clock() }}</span>
  @if (clockTone() !== 'calm') { <span class="clock__word">{{ t('::Take:Hurry') }}</span> }
</span>

<!-- one announcement at 5:00, one at 1:00, nothing in between -->
<span class="astro-visually-hidden" role="alert">{{ timeWarning() }}</span>
```

```scss
.clock--warn, .clock--urgent { font-weight: var(--astro-weight-bold); }
```

The glyph changes shape (clock → alarm), not just hue.

**Guideline:** Accessibility → *Color Only* (High) — "Use icons/text in addition to color"; Accessibility → *Error Messages* (High) — "Use aria-live or role=alert."

---

### 6. The submit dialog claims to be modal and is not

**Screen:** the exam paper, submit confirmation. Same pattern on 10 other dialogs.
**File:** `take-sitting.component.html:146-172`

Declares `role="alertdialog" aria-modal="true" aria-labelledby="submitTitle"`. I opened it and probed it:

- `focusInside: false` — **focus never moves into the dialog.** It stays on the Finish button outside. `aria-modal="true"` tells assistive tech the rest of the page is inert while the user is standing outside it with nothing to read.
- **Escape does not close it** (`escapeClosed: false`).
- Nothing is `inert`, so Tab walks straight out into the answer inputs behind the scrim.
- `aria-describedby: null` — the unanswered-question count, the whole reason the dialog exists, is not wired to the dialog's description.

The project **already has the correct implementation.** `shared/ui/modal.directive.ts` does capture, trap, Escape and focus restore, and documents why. It is applied to 14 of 35 dialogs. These 11 have the ARIA and not the behaviour:

`assignment.component.html:170`, `:373` · `candidate-list.component.html:188`, `:289` · `exam-list.component.html:282` · `exam-form.component.html:233` · `question-list.component.html:257`, `:425` · `attempt-monitor.component.html:109`, `:138` · **`take-sitting.component.html:149`**

**Fix:** add `astroModal (dismiss)="cancelSubmit()"` to each dialog box — never to the scrim. (Three of them currently put `role="dialog"` on the scrim instead of the box, which `modal.directive.ts:29-30` explicitly warns against.)

**Guideline:** Accessibility → *Keyboard Navigation* (High) — "test every action without a pointer / Don't: keyboard traps or illogical tab order"; Interaction → *Focus States* (High) — "including modal controls."

---

### 7. The account menu button has no name — on every staff screen

**Screen:** all 13.
**File:** `angular/src/app/layout/shell.component.html:85-92`

```html
<button type="button" class="user__trigger" (click)="toggleUserMenu()"
        [attr.aria-expanded]="userMenuOpen()" aria-haspopup="menu">
  <i class="bi bi-person-circle" aria-hidden="true"></i>
</button>
```

The only child is `aria-hidden`. Accessible name: empty. A screen reader announces *"button, collapsed."* This is how you sign out.

axe flags it as **critical, on 13 of 13 screens** — the single most repeated violation in the product after contrast.

It is also the **only** icon-only button in the app missing a label: the sidebar toggle (`:14-21`) and the theme toggle (`:71-82`) both have `[attr.aria-label]`. A one-line omission, not a pattern.

```html
<button … [attr.aria-label]="t('::Account')">
```

*(Adjacent: `shell.component.html:95` uses `role="menu"` / `role="menuitem"` with no arrow-key handling. ARIA menu semantics promise keyboard navigation this code does not implement — dropping both roles is more honest than half-implementing them.)*

**Guideline:** Accessibility → *ARIA Labels* (High) — "Add aria-label for icon-only buttons / Don't: icon buttons without labels." Also Priority 1's named anti-pattern: "Icon-only buttons without labels."

---

### 8. On a phone, the action buttons on every list screen are off-screen

**Screens:** exams, questions, candidates, groups, assignments, results, review, users, roles, organisations — at 390px.

The responsive strategy for tables is a horizontal scroll inside `.astro-scroll-x`. It works — the *page* never scrolls sideways, which is the harder half and is done right. But at 390px the exams table shows Title / Category / Status / Questions and clips the rest. **The seven-button action column is entirely beyond the right edge**, reachable only by discovering that the table scrolls independently. There is no fade, shadow, or any other affordance that content continues.

A coordinator on a phone cannot edit, publish, assign or delete anything without first discovering a hidden gesture.

Two further costs at that width: the title column wraps to four lines, so twenty rows become a ~2,900px page; and the seven action icons are visually undifferentiated — same size, same weight, same grey — so Delete looks exactly like Duplicate. (They have `title`/`aria-label`, so they are named; the problem is visual, not semantic.)

**Fix** — collapse to cards below the breakpoint rather than scrolling:

```scss
@media (max-width: 48rem) {
  .table thead { @extend .astro-visually-hidden; }
  .table, .table tbody, .table tr, .table td { display: block; inline-size: 100%; }
  .table tr {
    margin-block-end: var(--astro-space-3);
    border: 1px solid var(--border-subtle);
    border-radius: var(--astro-radius-md);
    background: var(--surface-raised);
  }
  .table td { border: 0; padding: var(--astro-space-2) var(--astro-space-3); }
  .table td::before { content: attr(data-label); color: var(--text-muted); margin-inline-end: var(--astro-space-2); }
  .row-actions { display: flex; flex-wrap: wrap; gap: var(--astro-space-2); }
}
```

And give the destructive action a distinct treatment: `.row-action--danger { color: var(--status-fail-mark); }`.

**Guideline:** Priority 5, Layout & Responsive (High) — "Mobile-first breakpoints, no horizontal scroll"; Touch → *Touch Spacing* (Medium) — "Minimum 8px gap between touch targets."

---

### 9. Bootstrap still owns four things the token layer meant to take back

`_base.scss:218-275` does this correctly for `--bs-primary`, `.btn-primary`, `.btn-outline-secondary`, `.btn-danger` and `.btn-outline-danger`, and documents exactly why (*"the primary action on every screen was #0d6efd — the exact blue the token file says it deliberately avoided"*). Four cases were missed. All four measured live.

**(a) `.alert-danger` was never mapped.** Measured: `background: rgb(248,215,218)`, `color: rgb(88,21,28)` — Bootstrap's raw pink and maroon, not `--status-fail-*`. It is a different red from the `--astro-fail-*` family used everywhere else, and it **does not dark-flip**: a pale pink banner with near-black text on a `#0d1319` page. This is the top-of-page error banner on ~20 screens.

```scss
.alert-danger {
  --bs-alert-color: var(--status-fail-text);
  --bs-alert-bg: var(--status-fail-bg);
  --bs-alert-border-color: var(--status-fail-mark);
}
```

**(b) `--bs-border-radius` was never mapped.** Measured: inputs and buttons are **6px** (`0.375rem`); cards are 12px, icon buttons 4px. 6px is not on the 4/8/12 scale at all, so the entire form layer sits off the product's own geometry.

```scss
:root { --bs-border-radius: var(--astro-radius-md); }   /* 8px */
```

**(c) Table row hover is invisible on all 12 tables.** The `.table` rule sets `--bs-table-bg: var(--surface-raised)`, so Bootstrap's `.table > :not(caption) > * > * { background-color: var(--bs-table-bg) }` paints every `<td>` opaque **over** the `<tr>` hover. Measured on hover: `tr` background `rgb(236,239,243)`, `td` background still `rgb(255,255,255)`. The primary affordance telling a coordinator which row they are about to act on does not render.

```scss
tbody tr:hover > * { background: var(--surface-sunken); }
```

**(d) Every table has square corners.** `border-radius: 12px` is declared on `.table` with `overflow: hidden` — and measured `border-collapse: collapse`, which makes `border-radius` a no-op on a table. Confirmed visually: square-cornered tables beside 12px-rounded cards on every list screen. Move the radius and the `overflow` to the `.table-wrap` div (the pattern already exists at `role-list.component.html:21`).

**And one collision on the candidate's own screens.** `.card` in `take-entry.component.scss:12` and `take-result.component.scss:9` collides with Bootstrap's `.card`. Measured computed: `display: flex; flex-direction: column`. Angular's encapsulation wins on properties the local rule declares; `display` is not one of them. Because margin collapsing is disabled inside a flex container, every vertical gap on the entry and result screens is the *sum* of adjacent margins rather than the max — larger than authored throughout. Rename to `.entry-card` / `.result-card`, following the `.section` → `.paper-section` precedent the team already set at `take-sitting.component.scss:107-113`.

**Guideline:** Priority 4, Style Selection (High) — "Match product type, Consistency / Don't: mixing randomly"; Priority 6 — "Semantic color tokens."

---

### 10. The sign-in page is a different product

**Screen:** `/Account/Login` — served from the API origin, and the first thing every user of this product ever sees.

It is ABP's stock LeptonX page, untouched:

- The primary button is **Bootstrap `#0d6efd`** — the precise blue `_tokens.scss:16-18` says the brand deliberately avoids. You sign in through default blue and land in a carefully-built petrol-blue product.
- It loads **Font Awesome** (`fa fa-eye-slash`); the entire rest of the app uses Bootstrap Icons.
- The password-reveal button has **no accessible name** (axe: critical).
- No `<h1>`, no `<main>` landmark, an **empty `<h2>`**, 7 regions outside any landmark, and a heading-order jump. axe reports **6 rule violations here versus 2 on every app screen.**
- No logo — the wordmark is plain text. The organisation branding that `take-entry` goes to real trouble to paint (`take-entry.component.ts:66-74`) is absent from the screen that most needs it.
- At 390px "نسيت كلمة السر؟" wraps mid-phrase onto two lines.

This is a small amount of work for the highest-visibility surface in the product: override the LeptonX login stylesheet to map `--bs-primary` and the fonts to the Astrolabe tokens, add the organisation logo and a real `<h1>`, and label the reveal button.

**Guideline:** Priority 4, Style Selection (High) — "Consistency"; Accessibility → *ARIA Labels* (High).

---

## Also worth fixing (11–20, briefly)

11. **The marks line on the result page is word-order-scrambled in Arabic.** `take-result.component.html:38` puts `.astro-numeric` on a *translated sentence*. That class sets `direction: ltr` and a mono family with **no Arabic glyphs**. `"0 درجة من 3"` renders as `"من 3 درجة 0"` — I have the screenshot. This is exactly the trap `_base.scss:69-78` documents; the team avoided it in eight of nine places. Fix: drop the class, use `font-variant-numeric: tabular-nums` instead.

12. **Previous/Next chevrons point the wrong way in English.** `take-sitting.component.html:129,140` hand-mirror the glyphs for RTL *and* add `.astro-flip`, so the two corrections cancel. Measured in LTR: `transform: none`, giving `> Back` and `< Next`. Correct in Arabic, backwards in English. Author them LTR (`bi-chevron-left` on Previous) and let `.astro-flip` do the work.

13. **The save indicator says "Saved" after a save has failed.** `take-sitting.component.html:18-25` has only `saving` and `saved` states; on failure `savedAt` is untouched, so the green tick from the *previous* success returns. There is no failed state. Add one, with a shape change (check → triangle), not only a colour.

14. **The save-failure banner scrolls out of view and never clears.** `.notice` has no `position: sticky`, sits below the sticky bar so it shifts the question down when it appears, is styled amber (routine) rather than red, and is only cleared on question change.

15. **Touch targets below the product's own 44px floor.** Measured: language toggle **32px** (`shell.component.scss:128`), status filters **36px** on 8 screens (`.segmented__option`), permission rows 36px, sidebar links 40px, account menu items 40px. Separately, `<a class="btn btn-primary">` measures **38px** — `_base.scss:141-149` applies the floor to `button` and `[role=button]` but not to anchors styled as buttons. Add `a.btn` to that selector.

16. **Two error states do nothing and one screen renders blank.** `role-list.component.html:18` and `tenant-list.component.html:18` bind `(retry)="load()"` — `DataStateComponent` has no `retry` output, it projects `[slot=retry]`. Both screens' error states have **no retry button**, and nothing fails at build time. And `result-detail.component.html:1-7` has no `@else`: not loading, no error, null detail → **completely blank page**.

17. **A raw server exception is shown to the user.** Navigating to a bad exam id renders *"لا يوجد Exam بالمعرف = 00000000-…"* — an untranslated entity name and a GUID, inside Bootstrap's unmapped pink alert. Separately, every unmatched URL silently redirects to the dashboard (`app.routes.ts` `**`), so a stale link gives no "not found" feedback at all.

18. **`"You have 2 question(s) with no answer"`** — a parenthetical-plural hack in a product whose primary language has six plural forms. Use ICU plurals.

19. **Empty and loading states use four different treatments.** 11 screens hand-roll a private `.state` block instead of using `shared/ui/data-state`, whose header comment says it exists because *"twelve screens had grown a private copy of the block below, four of them had drifted."* They drifted again — the copies use `--astro-fail-600` for the error icon (2.7:1 in dark) where the shared one correctly uses `--status-fail-mark`. `exam-structure` uses a bare `<p>` with no icon at all — visible in my screenshot as a lone grey sentence where every other screen shows a composed block. *(Feedback → Empty States, Medium; Loading Indicators, High.)*

20. **Button hierarchy inverts in a few places.** `attempt-monitor.component.html:126` styles *force-end this candidate's live attempt* as `btn-primary` while the sibling dialog correctly uses `btn-danger`. `assignment.component.html:219` makes **Close** primary beside a secondary **Copy All**, in a panel that says the links are shown once only — the primary action destroys the only copy. And `catalog.component.html` can show three simultaneous `btn-primary` buttons all labelled "Save", targeting three different records, because the inline drafts do not close each other.

---

## What is already strong

This is not a courtesy section. Several of these are better than what most assessment products ship, and they change how the findings above should be read — these are gaps in a good system, not symptoms of a bad one.

**The RTL work is exemplary, and I checked it hard.**
- **Zero physical CSS properties** in ~5,000 lines of component CSS. Every offset, margin, border and size is logical. The only `left`/`right` matches in the tree are English words inside two prose comments.
- The five places `transform: translate()` genuinely cannot be expressed logically each carry an explicit `[dir='rtl']` sign flip *and* a comment naming the bug that motivated it — `candidate-list.component.scss:197-199`: *"the mistake that put a drawer 216px into a 412px phone."*
- **The `.astro-numeric` / `.astro-ltr` cell defect is gone.** I measured true glyph geometry on 30 table columns across exams, results, candidates and groups: every column shares an edge with its header. Not one instance of the class applied to a `<td>`, `<th>`, `<dd>` or `<dt>` anywhere in the app.
- **No horizontal page scroll on any of 28 screens at 390px**, in either language. `_base.scss:200-210` shows someone actually debugged Blink's RTL overflow accounting and wrote down what they learned (`contain: paint`).
- **Icon mirroring is correct.** I enumerated every element with a computed `scaleX(-1)`: chevrons, arrows, and the paper plane. No clock, no checkmark, no logo, nothing containing Latin text was flipped. The `[dir='rtl'] .bi-send` rule at `_base.scss:279-284` is applied by selector rather than by remembering a class at each call site — with a comment noting it had been missed in three of them.

**The token architecture is textbook.** Three enforced layers. Semantic colour reserved so green always means *passed* and the brand therefore cannot be green (`_tokens.scss:16-18`). Arabic-first type with justified 1.75 leading. The dark palette is a re-derivation, not an inversion, and correctly handles all three theme states — bare `:root`, media query guarded with `:not([data-theme="light"])`, then the attribute block. Most codebases get at least one of those directions wrong. **Where the tokens are actually used, dark mode measures 6.33–11.11:1 across the board.**

**Accessibility fundamentals are in place.** Form labelling is **100% clean** — all ~90 inputs have an accessible name, no placeholder-as-label anywhere. Every `<th>` in all 12 tables has `scope="col"`, and action columns carry a visually-hidden header rather than an empty one. Every `<img>` has an `alt`, with decorative ones correctly empty. One `:focus-visible` rule applied everywhere, and exactly **one** `outline: none` in the whole app — on `<main tabindex="-1">`, the textbook correct exception, with the reason written down. `prefers-reduced-motion` is honoured wholesale, and the spinner *slows* rather than freezing, because a stopped spinner reads as a hang.

**The candidate journey is nearly clean under axe.** Against 12 staff screens averaging 2 violated rules each and the login page at 6, the exam entry page returns 2 (landmark/region only) and the paper returns **1** (no `<h1>`). No contrast failures, no unnamed buttons, no unlabelled inputs. The paper itself is calm and uncluttered — one question, a timer, a save indicator, two buttons — which is the right instinct for a screen used once under pressure.

**`astro-status-chip` never relies on colour alone** — every tone ships an icon, with the reasoning at `status-chip.component.ts:8-10`: *"in a product where the difference between two of them is 'passed' and 'failed', that is not a detail."* The same thinking appears in the answer components: a square and a circle for record/stop, a shape change between radio and checkbox so a candidate knows how many they may pick without reading an instruction.

**Interaction choices were made for accessibility over flash, and the reasoning is recorded.** Arrow buttons instead of drag-and-drop for ordering. A `<select>` per row instead of drawn lines for matching. Discrete buttons instead of a slider for scale. Each is the harder implementation and the right one.

**The codebase documents its own past defects where the next person will read them.** `take-sitting.component.scss:33-47` on the `.progress` collision — *"the candidate's position was a grey pill sixteen pixels tall with the sentence sliced off inside it. Nothing in this file was wrong; the name was, and the collision is silent."* Then a preventive rename of `.section` → `.paper-section` with the reasoning attached. `modal.directive.ts` is exactly right where it is applied. The problem in every one of those cases is adoption, not design — which is a far better problem to have.

---

## What I could not evaluate, and why

- **`.row` colliding with Bootstrap's grid on the exam-structure screen.** The strongest unverified claim in this review. `exam-structure.component.scss:52` defines `.row` and applies it to `<li class="row">` (`:57`); Bootstrap's `.row` adds negative inline margins and `.row > *` sets `width: 100%; flex-shrink: 0`, which on a `flex-wrap: wrap` parent would stack every child onto its own line and hang the row 12px outside its container. I could not render it: the section-creation endpoint returned **405**, so the list stayed empty and the screen showed only its empty state. **To check: add two sections to any exam and open `/exams/{id}/structure`.** Same pattern, milder, at `numeric-editor.component.ts:72`.
- **Hotspot, audio, upload, matching, ordering, blanks and scale question types.** The seeded exams contain only single-choice questions, so these were reviewed in source only. Two look serious enough to name: `hotspot-answer.component.ts:64` binds a left-origin coordinate to `inset-inline-start`, which resolves to `right` in RTL — the crosshair would land mirrored, making the question unanswerable in Arabic; and `audio-answer.component.ts:164` releases the stream on destroy without calling `recorder.stop()`, so a recording in progress at time-expiry is never uploaded. Both need a seeded question to confirm.
- **Time expiry and auto-submit.** `take-sitting.component.ts:414-426` takes the `automatic` path without `flush()`, unlike the manual path — the last ~800ms of debounced typing looks discardable, and expiry is silent (no message, straight to the result page). Confirming this needs a short-duration exam; I did not want to alter exam configuration during a review.
- **Roles other than `admin`, and the other two tenants.** I reviewed as `admin` on the default tenant. `coordinator`, `author`, `marker` and `observer` see permission-filtered navigation and action sets I did not exercise, and `language-centre` / `recruitment` may carry different branding and default languages.
- **Marking and item analysis with real data.** The review queue was empty (*"every submitted attempt has been marked"*) and item analysis needs a statistically meaningful number of attempts. Both were reviewed only in their empty states — which is where finding 19 came from.
- **Branding upload and the host `organisations` screen with real tenants.**
- **Real assistive technology.** The screen-reader findings above are derived from the accessibility tree, axe, and ARIA semantics — not from a session with NVDA, JAWS or VoiceOver. Findings 5 and 6 in particular deserve one before they are signed off.

---

## Suggested order

| # | Fix | Where | Effort |
|---|---|---|---|
| 1 | Swap primitives for semantic tokens on the two invisible-text cases | `choice-answer.component.ts:77`, `take-result.component.scss:43,45` | 15 min |
| 2 | `--text-muted` → `#5a6876` | `_tokens.scss:150` | 2 min |
| 3 | `aria-label` on the account button | `shell.component.html:85` | 1 min |
| 4 | Map `.alert-danger` and `--bs-border-radius`; fix `tr:hover > *`; move table radius to `.table-wrap` | `_base.scss` + 13 `.table` blocks | 30 min |
| 5 | Rewrite the clock markup (silences the tick, names the time, adds a non-colour channel) | `take-sitting.component.html:9-16` | 45 min |
| 6 | `astroModal` on the 11 untrapped dialogs, starting with submit | table in finding 6 | 1 hr |
| 7 | Remaining ~143 primitive→semantic swaps | finding 2 | 1–2 hr |
| 8 | Card layout for tables below 48rem | shared `.table` block | 2 hr |
| 9 | The question navigator | `take-sitting.component.*` | half a day |
| 10 | Brand the login page | LeptonX override | half a day |

Items 1–4 are under an hour and clear 275 of the 289 axe violations plus both unreadable-text defects. Item 9 is the largest build and the largest win for the person this product exists to serve.
