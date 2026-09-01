# Astrolabe — design review of the surfaces built on 30–31 August

**Date:** 2026-08-31
**Scope:** only what was built or changed after the previous review (`design-review-2026-08.md`) — the question map on the exam paper, the audience picker in the send panel, the section picker and section column, the phone field and in-dialog error alert on the add-person dialog, the confirm-emptying dialog, and the three dialogs whose `role` moved off the overlay onto the box.
**Method:** every surface driven in a real browser against the running app (`localhost:4200` + `localhost:44373`), Arabic and English, 1440px and 390px, light and dark. Contrast measured with the compositing walk from `angular/e2e/support/contrast.ts`, geometry measured from real `getBoundingClientRect()` and glyph ranges, hit-testing done with `document.elementFromPoint` and with real clicks that were allowed to fail.
**Grounding:** `ui-ux-pro-max` (119 UX guidelines, stack `angular`). Each finding names the guideline.

**Two caveats, stated up front.**

1. The guideline database still has **no RTL or bidirectional-text entries** — I re-queried it. The RTL findings below are judged against this codebase's own written rules (`_tokens.scss`, `_base.scss:69–78`) and standard bidi practice, not against a database match. Same honesty the last review used.
2. `features/groups/**`, `features/results/**` and `features/exams/exam-forms.*` were being edited by other people throughout this review — the API restarted under me twice. I checked the working tree before writing each finding below: **none of the three broken dialogs is being fixed in the uncommitted work**, and `exam-forms.*` I have deliberately left alone. No line numbers are quoted for those files.

---

## The short version

The *thinking* in these surfaces is good, and in one specific way it is better than the code that came before it: **the primitive-versus-semantic lesson from the last review was actually learned.** I measured every new coloured state in dark mode and they land between 6.4:1 and 13.1:1. The question map's answered state survives greyscale. The section column shares its header's glyph edge in both directions.

The damage is concentrated in one refactor. Moving `role="dialog"` off the scrim and onto the box was the right call and the directive's own comment asks for it — but in three places the scrim was also doing the **centring**, via `display: grid; place-items: center`. The semantics moved and the CSS did not follow. The result is three dialogs that render in normal document flow *underneath* a fixed 45%-black overlay, with every button unclickable. I confirmed this by letting Playwright click them: `<div class="scrim"> intercepts pointer events`, three times.

So: **emptying a class, publishing an exam, and force-ending a stuck candidate's live attempt are all impossible with a mouse or a finger right now.** That is finding 1, and it dwarfs everything else here.

---

## The ten that matter most

---

### 1. Three dialogs render behind their own overlay. Every button in them is unclickable.

**Screens:** groups → roll → Save with nobody ticked; exam form → Publish; running sessions → End now, and → Discard.
**Elements:** `.confirm` (group-list), `.sheet__panel` (exam-form), `.dialog` (attempt-monitor, both).

Before the change, each of these boxes was a **child** of a fixed, full-viewport overlay that centred it:

```scss
.scrim { position: fixed; inset: 0; z-index: 40; display: grid; place-items: center; }
.dialog { background: var(--surface-overlay); border-radius: …; max-inline-size: 30rem; }
```

`.dialog` never had a `position` or a `z-index` of its own. It never needed one — `place-items: center` on the parent did the work. The refactor made it a **sibling**. It is now `position: static`, in normal document flow, at the bottom of the page, with `z-index: auto`, under a fixed overlay at `z-index: 40`.

Measured live, all three:

| Screen | Box computed | Where it lands | Real click |
|---|---|---|---|
| Empty the roll | `position: static`, `z-index: auto`, `background: rgba(0,0,0,0)`, `padding: 0` | `x 32, y 396, w 1120, h 138` — bare text across the full page width, under **two** stacked scrims | **intercepted by `div.scrim`** |
| Publish an exam | `position: static`, `z-index: auto` | `x 608, y 721` in a 900px viewport on a 2006px page — mostly below the fold | **intercepted by `div.sheet`** |
| End a live attempt | `position: static`, `z-index: auto` | `x 672, y 595, h 300` — hangs off the bottom of the viewport | **intercepted by `div.scrim`** |

The group one is the worst of the three, because `group-list.component.scss` **has no `.confirm` rule at all** — the class was borrowed from `candidate-list.component.scss`, and Angular's view encapsulation means it cannot reach across. So the box has no background, no padding, no shadow, no radius, no width; its `<h2>` falls back to the browser default and measures **28px** against `var(--astro-text-lg)` (18px) on every other dialog title in the product. The whole thing renders as loose 28px text and a maroon button floating over a darkened page, sliced in half by the roll panel that is still open above it.

**Cost.** A coordinator cannot empty a class. An author cannot publish an exam. An invigilator cannot force-end the attempt of a candidate whose browser has frozen — on a screen whose own lede says it exists for exactly that (*«مفيد حين يكون أحدهم في القاعة وقد توقّف متصفّحه عن الاستجابة»*). Escape still works, because `astroModal` is correctly applied; nothing else does.

**Fix.** Put the box back inside the overlay and leave the semantics where they now are. That satisfies the directive's rule — the rule was about which element carries `role` and `astroModal`, never about nesting:

```html
<div class="scrim">
  <div class="dialog" role="alertdialog" aria-modal="true"
       [attr.aria-label]="t('::Monitor:End:Title')"
       astroModal (dismiss)="cancelEnd()">
    …
  </div>
</div>
```

Keep the backdrop-dismiss by putting `(click)="cancelEnd()"` on the scrim and `(click)="$event.stopPropagation()"` on the box — not by making them siblings. Identical shape for `exam-form`'s `.sheet` / `.sheet__panel`.

`group-list`'s emptying dialog should simply match the two dialogs already in that file: wrap it in `<div class="scrim">` and rename `.confirm*` → `.dialog*`, which exist there and are already correct. Borrowing a class name from another component's stylesheet is the actual root cause and is worth saying out loud.

**Guideline:** Priority 2, Touch & Interaction (CRITICAL) — "Loading feedback / Don't: reliance on hover only"; Accessibility → *Focus Not Obscured* (Web) — "Keep the entire focused component unobscured by author-created content." Focus does move into all three boxes; it lands on a control the user cannot see and cannot point at.

---

### 2. The question map is on a different grid from the paper, and the "you are here" ring is clipped off the screen edge

**Screen:** the exam paper, `/exam/{token}`.
**Element:** `nav.qmap`, `.qmap__list`, `.qmap__item--current`.

`.paper` and `.actions` both carry `inline-size: min(48rem, 100%); margin-inline: auto; padding: … var(--astro-space-5)`. `.qmap` carries neither — only `padding-block: var(--astro-space-3)`.

Measured at 1440px, Arabic:

```
.paper    x=336  w=768      ← the question column
.actions  x=336  w=768      ← Previous / Next
.qmap     x=0    w=1440     ← full bleed
first .qmap__item  x=1396 … 1440   (right edge = viewport edge, 0px inset)
```

In English the same measurement mirrors: `first item x=0 … 44`.

Two consequences, both visible in the screenshots:

- **The map is 1,060px away from the question it belongs to.** It reads as page furniture rather than as part of the paper. Both other elements on that column are aligned; this one is not.
- **The current-question ring is sliced off.** `.qmap__item--current { outline: 2px solid; outline-offset: 2px }` extends 4px past the button box. The button's outer edge is at exactly the viewport edge, so the ring is cut. Measured `ringClippedEnd: true` at 1440px RTL and 390px RTL; `ringClippedStart: true` at 1440px LTR. This is the signal the component's own comment says is there so the state "reads without colour" — and on question 1 it is half-invisible in every viewport and both directions.

At 390px the map's buttons touch `x = 0` while the question text has 20px of padding, so it looks ragged; and a 44px target with its edge on `x = 0` sits inside the iOS/Android edge-swipe-back zone.

**Fix** — give it the same column as everything else on the screen:

```scss
.qmap {
  inline-size: min(48rem, 100%);
  margin-inline: auto;
  padding: var(--astro-space-3) var(--astro-space-5);
  border-block-start: 1px solid var(--border-subtle);
}
```

The 20px inline padding also gives the outline its 4px of room back.

**Guideline:** Priority 5, Layout & Responsive (High) — Layout → *Container Width*; Accessibility → *Focus Appearance* (Web) — "Use an indicator at least as large as a 2 CSS px perimeter"; an indicator clipped by the viewport does not meet it.

---

### 3. The person picker's three error states measure 2.67:1 in dark mode

**Screen:** send panel → "one person" → search.
**Element:** `.hint--warn` — used by *"nobody on the roll matches that"*, *"this class is empty"*, and *"the search failed"*.
**Rule:** `assignment.component.scss` — `.hint--warn { color: var(--astro-fail-600); }`

`--astro-fail-600` is `#b0342c`, a **primitive**. It has no dark override. `--status-fail-mark` does (`#e5786e` at `_tokens.scss:228`); `--status-fail-text` does.

Measured live with the compositing walk:

| Theme | Colour | Behind | Ratio | AA 4.5 |
|---|---|---|---|---|
| light | `rgb(176,52,44)` | `rgb(255,255,255)` | **6.22:1** | passes |
| dark | `rgb(176,52,44)` | `rgb(23,31,39)` | **2.67:1** | **fails** |

This is the same defect class as findings 1 and 2 of the previous review, in code written yesterday, in the only three sentences that tell a coordinator why the picker returned nothing. A coordinator in dark mode types a name, gets nothing, and the explanation is a dull maroon line they have to hunt for.

Everything else in this panel is correct — the audience picker's chosen state deliberately uses `--accent-subtle` / `--accent-subtle-text` with a comment saying *"the brand primitives do not, and reading white on white is how two of these went out."* One rule was missed.

**Fix:**

```scss
.hint--warn { color: var(--status-fail-text); }
```

`--status-fail-text` is defined for both themes. While you are there, `.astro-fail-600` appears nowhere else in this file's new code — this is the last one.

**Guideline:** Priority 1, Accessibility → *Color Contrast* (High) — "Minimum 4.5:1 ratio for normal text"; Priority 6 — "Semantic color tokens / Don't: raw hex in components." Also the codebase's own rule at `_tokens.scss:9-10`.

---

### 4. The "unfiled" filter the code comment promises does not exist

**Screen:** the question list of a sectioned exam.
**Element:** the section filter `<select>`, `question-list.component.html`.

Three lines above the control, the comment reads:

> *«"Unfiled" is offered beside them because it is the answer an author needs most: those are the questions a sectioned paper will never draw.»*

The control offers *All sections* and one option per section. **There is no "Unfiled" option.** I opened it on a real sectioned exam: two options, `["كل الأقسام", "استماع"]`. `sectionId` is a plain string signal passed straight through as `examSectionId`, so there is no sentinel value for "unfiled" either.

**Cost.** The new column does its job — it names a state that used to be a blank cell, and it names it well. But the state it names is the one that silently costs an author their work: on the 195-question paper that shipped this morning, ten questions were filed and a hundred and ninety were not. The author can now *see* the amber, one row at a time, and cannot list it. On a 200-row table that means paging until you spot a colour.

**Fix** — a sentinel the server already needs anyway:

```html
<option value="">{{ t('::Question:Filter:AllSections') }}</option>
<option [value]="UNFILED">{{ t('::Question:Section:None') }}</option>
@for (section of sections(); track section.id) { … }
```

with `readonly UNFILED = 'unfiled'` and the query sending `unfiledOnly: true` rather than an `examSectionId`. Place it directly under *All sections*, above the named sections — same reasoning the send panel already uses for putting "rotate" above the named forms.

**Guideline:** Search → *No Results* (Medium) — "Show 'No results' with suggestions / Don't: dead ends." A state you can see and cannot filter to is a dead end with extra steps.

---

### 5. A question map that cannot be used looks exactly like one that can

**Screen:** the exam paper on any exam with back-navigation off.
**Element:** `.qmap__item:disabled`.

```scss
&:disabled { cursor: default; }
```

That is the entire disabled treatment. Measured with `disabled` set on a real rendered map: `background`, `color`, `border-color`, `opacity` and `filter` are **identical** to the enabled state. `cursor` is the only difference, and a cursor does not exist on a phone and does not appear until you hover on a desktop.

So on a no-back paper the candidate sees a grid of numbered buttons — 44px, bordered, filled, in a product where every other bordered 44px box is pressable — that do nothing when pressed. The explanation exists and is well written:

```html
@if (!canJump()) { <p class="qmap__note">{{ t('::Take:Map:NoJumping') }}</p> }
```

but it is rendered **after** the whole list. On the 195-question paper that is ~1,470px of buttons before the sentence that explains them, at 13px. The candidate presses three or four before they find it, on a clock.

They are also `[disabled]`, not `aria-disabled`, so they are out of the tab order entirely: a keyboard candidate tabs from the answer straight to Previous/Next and never learns the map is there.

**Fix** — when it is a picture, make it look like a picture, and lead with the sentence:

```html
@if (!canJump()) { <p class="qmap__note">{{ t('::Take:Map:NoJumping') }}</p> }
<ol class="qmap__list" [class.qmap__list--static]="!canJump()"> … </ol>
```

```scss
.qmap__list--static .qmap__item {
  border-style: dashed;              // not a button shape
  background: var(--surface-sunken);
  box-shadow: none;
}
.qmap__item:disabled { cursor: default; }
```

and swap `[disabled]` for `[attr.aria-disabled]="!canJump()"` with the click already guarded in `jumpTo()`, so the map stays reachable and readable by keyboard and screen reader. `jumpTo` already checks `canJump()` — the guard is there, only the attribute is wrong.

**Guideline:** Priority 4, Style Selection (High) — "Consistency"; Interaction → *Disabled States*; Forms → *Input Affordance* (Medium) — "Do: use distinct input styling / Don't: inputs that look like plain text" — the same principle read the other way round.

---

### 6. `.astro-numeric` drags a form value to the opposite edge from its own label — in Arabic only

**Screen:** send panel → *محاولات لكل شخص* (attempts per person).
**Element:** `<input id="maxAttempts" class="form-control astro-numeric">`

This is the defect class the project has produced repeatedly, this time on a form field rather than a table cell. `.astro-numeric` sets `direction: ltr` and a monospace family; the input inherits `text-align: start`, and `start` in an LTR box is the **left**.

Measured, Arabic, 1440px:

```
label  "محاولات لكل شخص"   x 589 … 714   ← ends at the field's right (start) edge
input  value "1"          x 409 … 714   with direction:ltr, so the glyph sits at 415
sibling field "متاح حتى"  label and value both at the right edge
```

Every other label/value pair in that panel shares the start edge. This one does not. Measured in English the same pair reads `label x=726`, `input x=726` — **correct in LTR, wrong in RTL**, which is exactly how this bug hides.

The same shape is on the new phone field in the add-person dialog (`class="form-control astro-ltr"`, measured `direction: ltr`, value at `x=44`, label «الهاتف» at `x=309…349`). For a phone number the LTR *direction* is right — `+962 79 …` must not reorder — but the *alignment* need not follow it.

**Fix** — keep the direction, put the alignment back:

```scss
.form-control.astro-numeric,
.form-control.astro-ltr { text-align: start; }        // no
```

That is what is already happening. The correct form is to align to the field's own edge rather than the text run's:

```scss
[dir='rtl'] .form-control.astro-numeric,
[dir='rtl'] .form-control.astro-ltr { text-align: right; }
```

or, for `#maxAttempts` specifically, drop `.astro-numeric` altogether — a spinner input is already LTR-safe — and use `font-variant-numeric: tabular-nums`, which is exactly the remedy `take-sitting.component.scss` writes out at length for `.sitting__progress`.

**Guideline:** No database match — the guideline set has no bidi entries, as noted. Judged against `_base.scss:69-78`, which documents this precise trap, and against the previous review's finding 11.

---

### 7. The error a coordinator now sees inside the dialog is Bootstrap's, and they see it twice

**Screen:** add / edit a person, on any server refusal.
**Element:** `.alert-danger` inside `.confirm`, plus the page-level one behind the scrim.

The change itself is right, and the comment explaining it is right: the page alert sat behind the scrim, so *"somebody adding a person whose address is already on the roll pressed Save and watched nothing happen at all."* Two things came with it.

**(a) The page alert was not suppressed.** Measured with a real duplicate-email save:

```json
[ { "inDialog": false, "role": "alert", "rect": [16,261,358,90] },
  { "inDialog": true,  "role": "alert", "rect": [41,102,308,118] } ]
```

Two `role="alert"` live regions, identical text, fired together. A screen-reader user hears the sentence twice. A sighted user sees it twice — once inside the dialog and once bleeding through the scrim behind it, which is visible in the dark screenshot.

**(b) It is Bootstrap's raw `.alert-danger`.** Measured inside the dark dialog: `background rgb(248,215,218)`, `color rgb(88,21,28)`, `border-radius 6px`. Bootstrap's pink and maroon, not `--status-fail-*`; no dark flip; and 6px is not on the product's 4/8/12 radius scale. In dark mode it is a glaring pale-pink slab occupying the top third of a near-black dialog. The text inside it is legible (10.22:1) — this is a brand and consistency failure, not a contrast one.

The previous review's finding 9(a) and 9(b) called this out on the page background. It has now been promoted into the most-used dialog in the product.

**Fix:**

```scss
.alert-danger {
  --bs-alert-color: var(--status-fail-text);
  --bs-alert-bg: var(--status-fail-bg);
  --bs-alert-border-color: var(--status-fail-mark);
  --bs-alert-border-radius: var(--astro-radius-md);
}
```

and clear `actionError` when the dialog opens so only one copy is ever live.

**Guideline:** Priority 4, Style Selection (High) — "Consistency / Don't: mixing randomly"; Accessibility → *Error Messages* — one `role="alert"` per message.

---

### 8. The audience picker reads as two switches, not as one choice

**Screen:** send panel → *أرسله إلى*.
**Element:** `.audience` (`role="group"`) with two `.audience__option` buttons carrying `aria-pressed`.

The question the brief asks — *does it make "a class and a person at once" visibly impossible?* — has two answers, and they differ by width.

**At 1440px: adequately.** Two 307px buttons side by side; the chosen one takes `--accent` border, `--accent-subtle` background and medium weight. Measured 13.12:1 on / 10.25:1 off in dark, 44px tall. A user can see which one is live.

**At 390px: no.** `flex: 1 1 10rem` with `flex-wrap: wrap` in a 308px column cannot fit two 160px minimums plus the gap, so they **stack full-width, one above the other, each 308×44**. Two full-width bordered buttons stacked vertically is the visual grammar of two *actions* — "Send to a class" / "Send to one person" — not of a one-of-two selector. The only thing carrying exclusivity is the fill of one of them.

**Semantically, in both widths: no.** `aria-pressed` on two buttons in a bare `role="group"` announces two independent toggle buttons: *"class, toggle button, pressed"* and *"one person, toggle button, not pressed"*. Nothing says these are alternatives; nothing says "1 of 2". The correct pattern announces the set.

There is also a consistency cost. This is a **third** pattern for a mutually-exclusive choice in a product that already has two — the `.segmented` control lives in this very stylesheet, is used for status and difficulty filters on eight screens, and reads unmistakably as one-of-N because it shares a sunken track with 2px of padding and no gaps.

**Fix** — reuse the pattern the product already has, and make it a radio group:

```html
<div class="segmented" role="radiogroup" aria-labelledby="audienceLabel">
  <button type="button" class="segmented__option" role="radio"
          [attr.aria-checked]="audience() === 'group'"
          [class.segmented__option--on]="audience() === 'group'"
          (click)="setAudience('group')"> … </button>
  <!-- and the same for 'person' -->
</div>
```

with arrow-key handling, or drop to two real `<input type="radio">` in a `<fieldset>` and style them — which needs no JavaScript at all and is the more honest option here, since the set is two and will stay two.

**Guideline:** Accessibility → *Compact Control Semantics* (Web, **Critical**) — "Prefer a button and expose pressed or selected state that matches the visible label." The visible state here is a selection, and `aria-pressed` reports a toggle. Priority 4, Style Selection (High) — "Consistency."

---

### 9. The send panel has no type hierarchy: seven new hints render identically to the labels above them

**Screen:** the send panel.
**Elements:** `.form-label` and `.hint`, throughout.

Seven `.hint` paragraphs were added to this panel yesterday. **`assignment.component.scss` has no `.hint` rule**, and `_base.scss` has no `.form-label` rule, so both fall through to body defaults. Measured, both themes:

| | size | colour | weight |
|---|---|---|---|
| `.form-label` "أرسله إلى" | 16px | `--text-primary` | 400 |
| `.hint` "كل فرد في المجموعة يحصل على رابط…" | 16px | `--text-primary` | 400 |
| `.hint--warn` | 16px | `#b0342c` | 400 |

Label, value and helper text are the same size, the same weight and the same colour. The panel is a wall of undifferentiated 16px prose in which the thing you are being asked and the thing you are being told look identical.

The product does have a hierarchy — it just is not used here. The section picker two screens away, added in the same batch, measures label 14px / hint 12px `--text-muted`; `candidate-list`'s `.field__hint` is `--text-muted`. This panel is the outlier.

**Fix** — one rule, and the panel snaps into the product:

```scss
.panel .form-label {
  display: block;
  margin-block-end: var(--astro-space-1);
  font-size: var(--astro-text-sm);        /* 14px */
  font-weight: var(--astro-weight-medium);
  color: var(--text-primary);
}

.hint {
  margin-block: var(--astro-space-1) 0;
  font-size: var(--astro-text-xs);        /* 12px */
  line-height: var(--astro-leading-body);
  color: var(--text-muted);
}
```

**Same panel, same fix, second half.** The person search input reuses the class name `.search` from the page-level filter bar at the top of the same stylesheet, and inherits its `max-inline-size: 24rem`. Measured at 1440px: the search box is **384px inside a 622px column**, sitting 238px short of the left edge of its own results list, which is full width. The chosen-person state below it measures the full 622px, so the moment you pick someone the field jumps two hundred pixels wider. It is invisible at 390px, where 24rem never binds — a desktop-only defect, which is why it survived.

Rename it (`.person-search`) or scope the page rule to `.filters .search`.

**Guideline:** Priority 6, Typography → *Font Size Scale* (Medium) — "Use consistent modular scale / Don't: random font sizes"; Layout → *Container Width*.

---

### 10. At 195 questions the map is 1,473px of buttons with no cap and no scroll of its own; the person results clip mid-row

Two overflow problems from the same omission.

**(a) The map.** `.qmap__list` has `flex-wrap: wrap` and no `max-block-size`, no `overflow`. A 195-question paper is not hypothetical — one was built and shipped to a candidate at 01:14 this morning, and the backend cap that prevents it landed in the same commit; a 60- to 100-question placement paper needs no bug at all. Measured at 390px with 195 items in the real rendered list: **28 rows of 7, 1,473px tall, on an 1,850px page.**

The question stays above the fold (`.paper` comes first, measured `questionRect y=96`), so the map does **not** push the question down — that part is right. But the map stops being a map: the candidate can see about ten rows at once and has to scroll the whole page to read the rest, then scroll back to the question. And because it is not its own scroll container, jumping to question 140 leaves the current item somewhere off-screen with nothing to scroll it into view.

The previous review's sketch had `max-block-size: 4.5rem; overflow-y: auto` on it. That was dropped. Put a cap back and scroll the current item into view after a jump:

```scss
.qmap__list {
  max-block-size: 8.5rem;      /* three rows, then it scrolls in place */
  overflow-y: auto;
  overscroll-behavior: contain;
}
```

```ts
jumpTo(position: number): void {
  if (this.canJump() && position !== this.position()) {
    this.goTo(position);
    queueMicrotask(() =>
      this.host.nativeElement
        .querySelector('.qmap__item--current')
        ?.scrollIntoView({ block: 'nearest', behavior: 'auto' }));
  }
}
```

**(b) The person results.** `.found { max-block-size: 15rem; overflow-y: auto }` with no fade, shadow or count. Measured with eight matches: the container is 240px and the fourth row is **cut through the middle of a candidate's name**, at 1440px and at 390px alike — visible in both screenshots. A half-name is worse than a hidden one: it reads as a rendering fault, not as "there is more." The panel already knows the number; say it, and mark the edge:

```html
<p class="hint">{{ t('::Assignment:Person:Matches', personResults().length.toString()) }}</p>
```

```scss
.found { mask-image: linear-gradient(to bottom, #000 calc(100% - 1.5rem), transparent); }
```

**Guideline:** Layout → *Overflow Hidden* (Medium) — "Test all content fits within containers / Don't: blindly apply overflow-hidden"; Priority 3, Performance — reserve space rather than clip.

---

## Also worth fixing (11–17, briefly)

11. **The new section filter is 38px tall**, under the product's own `--astro-touch-min` (44px) that the question map correctly honours ten metres away. It matches the neighbouring type filter, which was already 38px — so this is a new control shipped to a known-wrong floor rather than to the token. `.type-filter { min-block-size: var(--astro-touch-min); }` fixes both. *(Touch → Touch Target Size, High.)*

12. **`"لديك 3 سؤالًا بلا إجابة"`** — measured live in the submit dialog. Arabic takes the plural of paucity for 3–10: *"3 أسئلة"*. `سؤالًا` is the accusative singular that goes with 11–99. The last review flagged the English `question(s)` hack; this is the Arabic half of the same string, and it is the sentence a candidate reads with the clock running. ICU plurals, six forms.

13. **The "go to the first unanswered" button is glued to the wrong sentence.** Measured in the submit dialog at 390px: the button's bottom edge is at `y=446` and `.confirm__body` starts at `y=446` — zero gap — while the warning it belongs to ends 12px above it. So it reads as attached to *"after submitting you cannot reopen the exam"* rather than to *"you have 3 unanswered."* Wrap it with the warning, or give it `margin-block: var(--astro-space-2) var(--astro-space-4)`. The control itself is exactly right, and gated on `canJump()` for exactly the right reason.

14. **`.unfiled` borrows a semantic token that already means something else.** `--status-pending-text` means "awaiting a marker" everywhere else in the product; it now also means "filed in no section." Contrast is fine (7.08:1 light, 8.91:1 dark, measured) and the words differ from a section name, so it does not depend on colour — but the same amber saying two things in one product is how a colour stops meaning anything. It is also **13px against 14px siblings** in the same row. Consider `--text-secondary` plus a small `bi-folder-x` icon, matching `astro-status-chip`'s own rule that a state ships a shape.

15. **Previous/Next chevrons still point the wrong way in English.** Measured in LTR: `bi-chevron-right.astro-flip` with `transform: none`, giving `> Back` and `< Next`. Unchanged since the previous review's finding 12, and now sitting directly above a brand-new map — worth doing in the same pass.

16. **Two untranslated resource keys are rendering to users.** `IMS:ExamLink:AttemptsExhausted` shown to a **candidate** on the exam entry screen when their attempts are used up, and `Pager:Range` on the running-sessions pager. Both observed live, both in Arabic.

17. **The add-person dialog still cannot put the person in a class**, while the paste-a-list dialog can. A coordinator adding one student must save, navigate to Groups, open the roll, search, tick, save again. `candidate.service.ts`, `CandidateDtos.cs` and `GroupRollTests.cs` are all modified in the working tree, so this looks like it is being built right now — flagging it only so it is not lost.

---

## What is already strong

Real care went into these, and three things in particular are better than the code they were added to.

**The dark-mode lesson was learned.** This is the headline of the good news. Every new coloured state I could reach, measured with the compositing walk in `contrast.ts`:

| Surface | Dark | Light |
|---|---|---|
| `.qmap__item--answered` | **8.76:1** | 7.9:1 |
| `.audience__option--on` | **13.12:1** | 12.6:1 |
| `.audience__option` (off) | **10.25:1** | 6.85:1 |
| `.unfiled` | **8.91:1** | 7.08:1 |
| `.confirm__warning` (submit) | — | 6.44:1 |

Not one of these reaches past a semantic token. `.qmap__item--answered` composites `rgba(31,157,85,0.16)` over the dark surface and pairs it with `--status-pass-text`, which is precisely the pairing the previous review asked for after measuring 1.01:1 and 1.06:1. `.audience__option--on` carries a comment saying so in as many words. One rule out of dozens (`.hint--warn`, finding 3) missed it.

**The answered state survives without colour, and I checked it in greyscale.** Rendered at `filter: grayscale(1)`, an answered map button is still unmistakable: a `bi-check` glyph in the corner, a darker border, and a darker fill — three channels, not one. The comment in the template says exactly why it is there. The per-item `aria-label` carries the state in words too (*«السؤال 1 — بلا إجابة»* / *«— مُجاب»*), the number span is `aria-hidden`, the current item carries `aria-current="step"`, the list is an `<ol>`, and the `<nav>` is labelled. That is the whole accessibility contract for a navigator, done in one pass.

**`--text-muted` was actually fixed.** The previous review's finding 4 asked for `#5a6876`. Measured live: table headers now read `rgb(90,104,118)` on `rgb(236,239,243)` = **4.95:1**, passing on the hardest of the three surfaces. That is 262 axe nodes cleared with a two-minute edit, and it means the new section column's header passes for free. The account-menu button now has a name too (finding 7, done), and the clock was rewritten at 01:38 (finding 5).

**No `.astro-numeric` on a cell, and I measured the glyphs.** The new section column shares its header's true glyph edge in both directions — Arabic `th` ends at 973 and every `td` ends at 973; English `th` starts at 497 and every `td` starts at 497. Where `.astro-numeric` *is* used it is correctly on a `<span>` inside the cell (`assignment.component.html`, the attempts column), never on the cell itself. The one place it went wrong (finding 6) is a form input, which is a new location for the bug rather than a return of the old one.

**44px targets on the thing that most needed them.** `.qmap__item` is `2.75rem` square with an 8px gap, and the comment explains why: *"a candidate under time pressure on a phone is the least forgiving target audience in the product."* That is the right instinct and it is the right number, and it is more than most of the staff-side controls in this product currently manage.

**Jumping works, and the restraint around it is the best judgement call in the batch.** I clicked map item 3 on a real sitting and landed on question 3. And `canJump()` refuses the map as a *control* when the paper forbids going back, with a written reason — jumping forward would strand every question it skipped — while still showing it as a *picture*. Somebody thought about the candidate who would be quietly cost marks by a feature that looked helpful. Likewise the submit dialog now ends the exact complaint the previous review made: it says two are blank *and offers the way there*.

**The dialog refactor got the hard half right.** `astroModal` is applied to every new dialog, on the box and never on the scrim, exactly as the directive's comment asks. I verified focus lands inside all three broken dialogs and that Escape closes them. The semantics are correct; only the CSS did not follow. That is a much better failure than the reverse.

**Nothing scrolls sideways.** Zero horizontal page scroll on every new surface at 390px, both languages. The send panel scrolls correctly at 390px (`max-block-size: 780px; overflow-y: auto`) with its actions reachable. The `.qmap` name was chosen specifically to dodge Bootstrap's `.nav`, `.progress` and `.map`, with the reasoning written down — the collision discipline this codebase learned the hard way is holding.

---

## What I could not evaluate, and why

- **The map at 195 real questions.** No seeded exam has more than three. I measured the layout by cloning the rendered `<li>` to 195 in the live DOM, which measures the CSS honestly — 28 rows, 1,473px, question still above the fold — but not the data path, the render cost of 195 Angular components, or what `jumpTo` does at that scale. **To check: build a paper with 60+ drawn questions and open it at 390px.**
- **The no-back-navigation map with a real exam.** No seeded exam sets `allowBackNavigation: false`, so I measured the disabled appearance by setting `disabled` on the rendered buttons. The `.qmap__note` sentence itself I have only read in source; its placement below the list is a source fact, not a measured one.
- **The group field in the add-person dialog.** It is not in the committed HTML and I did not find it in the working tree's Angular files, though the matching service, DTO and tests are all mid-edit. Reported as item 17 rather than as a defect.
- **The discard-attempt dialog specifically.** I probed the end-attempt dialog and found it unclickable; the discard one is structurally byte-identical in the same file and the same commit, so I am reporting it as broken by inspection rather than by click.
- **`features/exams/exam-forms.*`.** Under active edit for the whole session; deliberately untouched.
- **Anything after `e8a891a`.** A commit landed at 01:38 while I was measuring, and the API restarted twice under me. Everything above was re-checked against the working tree before it was written, but the three feature folders named in the brief will have moved.
- **Other tenants, other roles, real assistive technology.** Reviewed as `admin` on `trading-academy` and as a candidate. Findings 5 and 8 are the two that most deserve a session with a real screen reader before they are signed off.

---

## Suggested order

| # | Fix | Where | Effort |
|---|---|---|---|
| 1 | Put the three dialog boxes back inside their overlays, keeping `role` + `astroModal` on the box | attempt-monitor ×2, exam-form, group-list | 20 min |
| 2 | Rename group-list's `.confirm*` → the `.dialog*` classes that file already defines | group-list | 5 min |
| 3 | `.hint--warn` → `--status-fail-text` | assignment scss | 1 min |
| 4 | Give `.qmap` the paper's column and inline padding | take-sitting scss | 5 min |
| 5 | `.form-label` + `.hint` type scale in the send panel; rename its `.search` | assignment scss | 15 min |
| 6 | Map `.alert-danger` to the status tokens; clear the page alert when a dialog opens | `_base.scss`, candidate-list | 20 min |
| 7 | Add the "Unfiled" filter option | question-list | 30 min |
| 8 | Cap and scroll `.qmap__list`; scroll the current item into view after a jump | take-sitting | 45 min |
| 9 | Make the disabled map look static; `aria-disabled` instead of `disabled`; note first | take-sitting | 45 min |
| 10 | Rebuild the audience picker as a radio group on the existing `.segmented` | assignment | 1 hr |

Items 1–4 are under an hour, restore three actions that are currently impossible, and clear the only dark-mode contrast failure in the new work.
