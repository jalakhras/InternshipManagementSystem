# UI audit — August 2026

Astrolabe (`localhost:4200`), audited against its own stated position: restraint in the
register of Pearson VUE and Linear, Arabic as a first-class language rather than a
translation layer.

---

## What was checked

Every screen below was opened in **Arabic and English**, in **light and dark**, at
**1280px (desktop)** and **412px (Pixel 7 / mobile)** — 8 permutations per screen,
driven through Playwright against the live app and the live API, with screenshots read
back as pixels and computed styles read out of the running DOM.

| Screen | Covered |
|---|---|
| `/` dashboard | full matrix |
| `/exams` list | full matrix, plus the delete confirmation, the filtered empty state, and the loading state (API delayed to 8s) |
| `/exams/new` | full matrix |
| `/exams/:id` | full matrix, including the header's Draft chip / Cancel / Save / Publish cluster |
| `/exams/:id/questions/new` | type picker (13 cards), then the single-choice, multiple-answers, true-or-false, written-answer and numeric editors; "Score by degree of correctness" toggled on; an empty submit to see the error treatment |

Measurements taken from the running page rather than the source: computed colours,
contrast ratios, control heights, touch-target boxes, `transform` on RTL icons,
`document.scrollWidth` at 412px, and the resolved value of every `--astro-*` custom
property.

---

## Findings, ranked by harm to a real user

### P1 — a control is unreadable, unfindable, or lies about what it is

---

#### 1. The delete confirmation dialog has no surface. The table shows through it.

**Screen** `/exams`, delete row action · **both languages** · **both themes** · **both viewports**

`.confirm` in `exam-list.component.scss` sets `background: var(--astro-surface-1)` and
`border: 1px solid var(--astro-line)`. Neither property exists — the token names are
`--surface-*` and `--border-*`. Measured on the live page: `background-color:
rgba(0, 0, 0, 0)`, `border-top-width: 0px`, `border-top-style: none`. Only the
`box-shadow` survives, because it uses a real token.

The result is that "Attempts already sat keep their results — the exam is hidden, not
erased." is painted directly on top of the exam row: the Draft chip, the `60`, the `60%`
and the em-dashes all run through the sentence. In dark mode the column headers
(`CATEGORY STATUS QUESTIONS MINUTES PASS MARK`) cross the dialog title. This is the
confirmation step for the only destructive action in the product, and it is the least
legible thing on the screen.

**Fix** — `exam-list.component.scss`, `.confirm`:
```scss
background: var(--surface-overlay);
border: 1px solid var(--border-subtle);
&__body { color: var(--text-secondary); }   // was --astro-ink-2
&__note { color: var(--text-muted); }       // was --astro-ink-3
```

---

#### 2. The loading spinner paints nothing.

**Screen** `/exams` while the list request is in flight · **both languages** · **both themes**

`.spinner` sets `border: 2px solid var(--astro-line)`. Because `--astro-line` is
undefined, the whole `border` shorthand is invalid at computed-value time and falls back
to its initial value. Measured on the live element with the API held at 8 seconds:
`border-top: 0px none`, `border-right: 0px none`, `background-color: rgba(0,0,0,0)`,
box `34 × 34`.

So the loading state is the word *جارٍ التحميل* with a 34px hole above it. Nothing moves,
nothing indicates the app is alive. On a slow connection the list looks broken.

**Fix** — set the ring in two declarations so a bad token cannot kill the style:
```scss
border-width: 2px;
border-style: solid;
border-color: var(--border-subtle);
border-block-start-color: var(--accent);
```

---

#### 3. Eight design tokens referenced across the app do not exist.

**Screen** exam list, choice editor, rich-text editor · **all four combinations**

Read out of `document.documentElement`, all eight resolve to the empty string:
`--astro-ink-1`, `--astro-ink-2`, `--astro-ink-3`, `--astro-line`, `--astro-surface-1`,
`--astro-surface-2`, `--astro-surface-3`, `--astro-fail-fg`. The real names are
`--text-*`, `--border-*`, `--surface-*`, `--status-fail-*`.

Findings 1, 2, 7, 9, 12 and 13 are all downstream of this single mistake. Beyond those,
the same root cause quietly kills:

- `.state--error i { color: var(--astro-fail-fg) }` — the error icon is not red.
- `.row-action:hover { border-color: var(--astro-brand-600) }` — the hover border is real
  but `border-style` is `none`, so it never paints.

**Fix** — a global rename, then a lint rule. The cheapest guard is a build-time check that
every `var(--astro-*)` in `src/app` has a definition in `src/styles/_tokens.scss`; the
grep that found these is one line.

---

#### 4. The primary action is Bootstrap's default blue, and Publish is Bootstrap's green.

**Screen** every screen with an action · **both languages** · **both themes** · **both viewports**

`.btn-primary` measures `background-color: rgb(13, 110, 253)` — `#0d6efd`, Bootstrap's
stock blue — in **light and dark alike**. The design system's accent is `#0d5c70` in light
and `#75d4f5` in dark, and `_tokens.scss` says in as many words that the brand is
"deliberately not #0369A1 — that blue is the default of every B2B SaaS". The most
prominent control on every screen is the exact thing the palette was written to avoid, and
it does not respond to the theme at all.

Worse, on `/exams/:id` the header carries `[Draft chip] [Cancel] [Save #0d6efd]
[Publish #198754]`. The green Publish is visually louder than the blue Save, so the
occasional action out-competes the primary one. And `_tokens.scss` reserves green for
outcomes — *"in an assessment product green means passed, so the brand cannot also be
green or the interface starts lying."* A green Publish button in an assessment product
reads as a verdict.

The same button also carries `border-radius: 6px` (the scale is 4 / 8 / 12) and
`font-size: 16px` (every neighbouring control is 14px).

**Fix** — override the Bootstrap variables once, globally, rather than per component:
```scss
.btn-primary {
  --bs-btn-bg: var(--accent); --bs-btn-border-color: var(--accent);
  --bs-btn-hover-bg: var(--accent-hover); --bs-btn-color: var(--text-on-accent);
  --bs-btn-border-radius: var(--astro-radius-md);
  font-size: var(--astro-text-sm);
}
```
and demote Publish to the same neutral outline treatment as Cancel — its weight should
come from position and label, not from hue. If it must be distinguished, use
`--accent-subtle` / `--accent-subtle-text`, never `--status-pass-*`.

---

#### 5. Every secondary button fails contrast, in both themes.

**Screen** `/exams/:id` (Cancel), question builder (Add option, Add criterion), error state (Retry) · **both languages** · **both themes**

`.btn-outline-secondary` renders `color: rgb(108, 117, 125)` — Bootstrap's `#6c757d` — at
`font-size: 16px`, regular weight. Measured against the actual page ground:

| Theme | Foreground on ground | Ratio | AA (4.5:1) |
|---|---|---|---|
| light | `#6c757d` on `#f5f7f9` | **4.37:1** | fail |
| dark | `#6c757d` on `#0d1319` | **3.98:1** | fail |

At 412px in dark mode the Cancel button beside Save reads as disabled. It is not.

**Fix** — `--bs-btn-color: var(--text-secondary); --bs-btn-border-color: var(--border-strong);`
(`#4e5c6b` on `#f5f7f9` is 7.0:1; `#c3ccd6` on `#0d1319` is 12.6:1).

---

#### 6. The paper-plane icon never mirrors in Arabic.

**Screen** sidebar *الإسنادات*, dashboard *أرسل الاختبار*, `/exams` publish row action · **Arabic** · **both themes** · **both viewports**

Measured with `dir="rtl"` active: `transform: none` on both the nav icon and the row
action icon. `bi-send` points up-and-to-the-right, which in Arabic points *backwards* —
towards where the sentence started. `_base.scss` already ships `.astro-flip` for exactly
this, and `bi-box-arrow-right` (Logout) and the two pager chevrons correctly use it.
`bi-send` was missed in all three places.

**Fix** — add `astro-flip` to the three `bi-send` usages. For the nav item this means the
`NavItem` icon field needs a companion flag, or `[dir="rtl"] .sidebar__link .bi-send`
gets the transform directly.

---

### P2 — the screen is harder to use than it needs to be

---

#### 7. Row actions are two different sizes and have no visible target.

**Screen** `/exams` table · **both languages** · **both themes** · **both viewports**

Measured in one row: the three `<a>` actions (add question, edit) are **32 × 32**; the
`<button>` actions (publish, archive, delete) are **44 × 44**, because the `button` rule in
`_base.scss` reaches the buttons and not the anchors. So four icons sit in one cluster at
two sizes.

On top of that, `.row-action` sets `border: 1px solid var(--astro-line)` and
`background: var(--astro-surface-1)` — both dead — so the intended bordered icon buttons
render as bare glyphs with `background-color: rgba(0,0,0,0)` and
`border-top-style: none`. There is no visible hit area at all.

**Fix** — in `.row-action`: `background: var(--surface-raised); border: 1px solid
var(--border-subtle); inline-size: var(--astro-touch-min); block-size: var(--astro-touch-min);`
and drop `min-inline-size: auto` so the anchors inherit the same box.

---

#### 8. A 16px stagger between side-by-side fields, on both forms.

**Screen** `/exams/new`, `/exams/:id` (Minutes / Pass mark), question builder (Marks / Difficulty / Time limit) · **both languages** · **both themes** · desktop

Measured on `/exams/new` (en, dark): column 1 label top `586`, column 2 label top `602`.
On the question builder: `829`, `845`, `845`. The first column sits exactly one
`--astro-space-4` higher than its neighbours, so the labels and the input tops do not line
up across a row that is supposed to read as one band.

Cause: `.field + .field { margin-block-start: var(--astro-space-4) }` in both
`exam-form.component.scss` and `question-form.component.scss`. The rule is meant for
stacked fields, but the adjacent-sibling selector also matches fields sitting side by side
inside `.row-2` / `.row-3`, so every column after the first is pushed down.

The numeric editor's own row (`numeric-editor.component.ts`) has no such rule and its
three columns measure identical tops — that is the correct rendering, and it is what the
other two should match.

**Fix** — scope the stack rule, or cancel it in the rows:
```scss
.row-2 > .field + .field,
.row-3 > .field + .field { margin-block-start: 0; }
```

---

#### 9. The weighted-scoring row reads at full strength, so the metadata competes with the author's own text.

**Screen** question builder → single choice → "Score by degree of correctness" on · **both languages** · **both themes**

With the toggle on, each option becomes: radio · option text · weight input · *of 1* ·
band name (*غير محتسبة* / "not counted") · remove. The band and the "of N" are meant to be
quiet: `.option__band` and `.option__of` both set `color: var(--astro-ink-3)`. That token
does not exist, so both measure `rgb(23, 31, 39)` — identical to `--text-primary` and to
the option text itself. The scaffolding is as loud as the content.

Two further problems with the same row:

- The weight column has **no header and no label**. With four options an author sees four
  bare number boxes and must infer the scale from the band text beside them.
- `.option__weight` is 38px tall inside a row of 44px controls (see finding 14).

**Fix** — `color: var(--text-muted)` on both, and give the weight column a `<legend>` or a
single line of hint text above the option list once the mode is on ("Weight, −1 to 1").

---

#### 10. Raw localisation placeholders are visible in the numeric editor.

**Screen** question builder → numeric answer · **both languages** · **both themes** · **both viewports**

The preview strip renders, verbatim:

- Arabic: `يقبل {0} — {1} 0 — 0`
- English: `Accepts {0} — {1} 0 — 0`

The resource string is `"Accepts {0} — {1}"`, but the template calls
`t('::Question:Accepts')` with no arguments and then appends the bounds in a separate
`<span>`. So the placeholders print, then the real numbers print again after them.

**Fix** — pass the bounds into the translation: `t('::Question:Accepts', lowerBound(),
upperBound())`, and drop the trailing span (keep `.astro-numeric` on the whole strip so
the digits stay tabular).

*Adjacent:* `numeric-editor.component.ts` calls `t('::IMS:Question:NegativeTolerance')`
with a doubled prefix. It happens to resolve here, but every other call in the file uses
the bare `::` form; the odd one out is worth normalising.

---

#### 11. Two different checkboxes for the same job.

**Screen** `/exams/new` toggles vs. question builder "Score by degree of correctness" · **both languages** · **both themes** · **both viewports**

- Exam form (`.toggle input`): **18 × 18**, `accent-color: rgb(13, 92, 112)` — the brand
  petrol, inside a bordered card with a title and a sentence of explanation.
- Choice editor (`.partial input`): **13 × 13**, `appearance: auto` — the raw browser
  checkbox, no accent, no card.

Both are a boolean with a title and an explanatory sentence. The exam form's treatment is
right; the choice editor's is the default nobody styled. At 412px the 13px box is the
smallest interactive target in the product.

**Fix** — give `.partial input` `inline-size: 1.1rem; block-size: 1.1rem; accent-color:
var(--accent);` to match `.toggle input`, and wrap the row in the same bordered `.toggle`
card so the two boolean patterns are one pattern.

---

#### 12. The rich-text toolbar has no frame, and at 412px it looks broken.

**Screen** question builder, "Question text" and "Explanation" · **both languages** · **both themes**

`.toolbar` in `rich-text.component.ts` sets `border: 1px solid var(--astro-line)` and
`background: var(--astro-surface-2)`, both dead. Measured: `border-top-width: 0px`,
`border-top-style: none`, `background-color: rgba(0, 0, 0, 0)`. The three
`border-*-radius: 0` declarations that were meant to fuse the toolbar to the top of the
editor now join nothing — the buttons float in the gap above a bordered box.

At 412px the eight buttons wrap to two rows (6 + 2), and with no enclosing frame the
second row reads as two stray icons rather than a continuation of the bar.

**Fix** — `border: 1px solid var(--border-subtle); border-block-end: 0; background:
var(--surface-sunken);` so the toolbar and the editing surface read as one control.

---

#### 13. The empty state has no hierarchy.

**Screen** `/exams` with a filter that matches nothing · **both languages** · **both themes** · **both viewports**

Measured on the live empty state: the 28px magnifier icon, the semibold title (*لا نتائج*)
and the body sentence all compute to `rgb(23, 31, 39)` — full-strength `--text-primary` —
and the title and the body are both `16px`. The only thing separating them is font weight.
`.state { color: var(--astro-ink-3) }` and `.state__title { color: var(--astro-ink-1) }`
were meant to create three levels; both tokens are dead, so all three levels collapsed
into one.

**Fix** — `.state { color: var(--text-secondary) }`, `.state__title { color:
var(--text-primary); font-size: var(--astro-text-lg) }`, `.state i { color:
var(--text-muted) }`. The icon should be the quietest thing in the block, not the loudest.

---

#### 14. Seven different control heights across two screens.

**Screen** exam list and question builder · **both languages** · **both themes** · desktop

All measured in the running page:

| Control | Height |
|---|---|
| `.form-control` (text, number, search) | 44px |
| `.btn-primary` as a `<button>` (Save) | 44px |
| `.segmented__option` (question builder) | 40px |
| `.option__text`, `.option__weight` | 38px |
| `.btn-primary` as an `<a>` (New exam), `.btn-outline-secondary` (Cancel) | 38px |
| `.segmented__option` (exam list) | 36px |
| `.row-action`, `.option__remove`, `.toolbar__button` | 32px |

Two of these hurt in particular. First, **New exam is 38px and Save is 44px** — the same
`.btn-primary` class, different heights, because one is an anchor and one is a button; on
`/exams/:id` the 38px Cancel sits directly beside the 44px Save in the same header, and
the step is obvious. Second, **the option text input is 38px while every other text input
on the same page is 44px** (`.option__text` sets `min-block-size: 2.25rem`, overriding the
44px `.form-control` rule).

**Fix** — set the `.btn` height from `--astro-touch-min` globally so anchors and buttons
agree; remove `.option__text`'s `min-block-size` override; move the two segmented controls
onto one shared value (40px reads best against 44px inputs, and is the one already used in
the question builder).

---

#### 15. On a phone, the row actions are off-screen behind an unhinted horizontal scroll.

**Screen** `/exams` at 412px · **both languages** · **both themes**

The eight-column table scrolls inside `.astro-scroll-x` — correctly, and the page itself
never scrolls sideways (`document.scrollWidth === 412` on every screen and every
combination tested; see *what is already good*). But at 412px the visible columns stop at
Pass mark. Created and the whole action cluster are outside the viewport, measured at
`left: -136` and `left: -268`. There is no scroll shadow, no fade, no edge cue. On a phone
the only route to Edit or Delete is to guess that the table scrolls.

**Fix** — the honest fix is a card layout below 768px (title, chip, two facts, an actions
row) rather than a scrolled table. The cheap fix is a fade mask on `.astro-scroll-x` and
pinning the action column with `position: sticky; inset-inline-end: 0`.

---

#### 16. On a phone the primary action scrolls away and never comes back.

**Screen** `/exams/new` and the question builder at 412px · **both languages** · **both themes**

The exam form is ~2040 CSS px tall and its only Save sits in the header, gone after the
first scroll. The question builder is ~1734px tall and its only Save sits at the very
bottom, in the corner, 66px wide. Neither has a sticky footer.

**Fix** — one sticky action bar below 1024px, shared by both forms:
`position: sticky; inset-block-end: 0; background: var(--surface-raised); border-block-start: 1px solid var(--border-subtle);`

---

#### 17. Two form-action patterns, and the question builder has no way out.

**Screen** `/exams/new` vs. question builder · **all four combinations**

The exam form puts `[Cancel] [Save]` in the page header, at the top. The question builder
puts a lone `Save` at the bottom of the page and offers no Cancel at all — the only escape
is the "Change type" text link at the very top, which measures **25px tall**
(`.link { min-block-size: auto }`) and does not read as a cancel.

The exam form's placement is the right one for this product: the actions stay with the
title, and on desktop they are visible without scrolling.

**Fix** — move the question builder's Save into a `page-header` actions slot alongside a
Cancel that returns to the exam, matching the exam form exactly.

---

### P3 — cosmetic, worth fixing when the file is open anyway

---

#### 18. The type picker borrows outcome colours for a non-outcome.

**Screen** question builder type picker · **both languages** · **both themes** · **both viewports**

`.type__grading` uses `--status-pass-text` for "Marked automatically" and
`--status-pending-text` for "Marked by a person". `_tokens.scss` reserves those hues:
*"Reserved for outcomes. Nothing decorative may borrow these."* Nine green labels and four
amber ones stripe the picker, and in an assessment product a green label beside a question
type suggests a verdict rather than a fact about who does the marking.

**Fix** — `--text-secondary` for both, and carry the distinction on the icon tile
(filled vs. outlined) or with the word alone.

---

#### 19. Hints break at 63% of their field's width.

**Screen** both forms · **all four combinations** · desktop

`.hint { max-inline-size: 60ch }` computes to **432px** under fields measuring **683px**,
so most hints drop one orphaned word onto a second line ("…never appears during the /
exam.") with a 250px gap beside it. It reads as a wrapping bug rather than a chosen
measure.

**Fix** — drop `max-inline-size` and let the hint take the field's width; the fields are
already capped at 46–48rem, which is a comfortable measure on its own.

---

#### 20. Type picker cards are 60% empty.

**Screen** type picker · **both languages** · **both themes** · most visible at 412px

`.type__body` has no `flex: 1`, so the icon and text hug the start edge and the rest of
each card is dead space. On desktop it is a nit; at 412px it is thirteen mostly-empty
boxes down the page.

**Fix** — `.type__body { flex: 1 }`, and below 768px drop the grid to two columns so the
cards are sized to their content.

---

#### 21. User-supplied text is not bidi-isolated.

**Screen** `/exams` title cell and `/exams/:id` title field, English UI · both themes

`.title` measures `unicode-bidi: normal`, so an Arabic exam title inherits the page's base
direction. Today's data (`اختبار 101`) survives, but a title ending in a number,
punctuation or a Latin fragment will not, and on `/exams/:id` the Arabic title sits
flush-left in the English UI with the caret at the wrong end.

**Fix** — `dir="auto"` on the title anchor and the title input, or
`unicode-bidi: plaintext` on `.title` and `.form-control`. This applies to every field that
holds text a user typed — exam title, option text, question text.

---

#### 22. Two visual languages for errors.

**Screen** question builder · **both languages** · **both themes**

The form-level error is a raw Bootstrap `.alert-danger`: `background rgb(248, 215, 218)`,
`color rgb(88, 21, 28)`, `border-radius 6px`. The inline warning in the same card
(`.warning`, "No correct option is marked…") uses the tokens properly:
`--status-pending-bg` / `--status-pending-text`, `--astro-radius-md`. Same screen, two
palettes and two radii. In dark mode the Bootstrap alert stays a light pink panel.

**Fix** — restyle `.alert-danger` from `--status-fail-bg` / `--status-fail-text` /
`--astro-radius-md`, and give it the same leading icon the `.warning` band has.

---

#### 23. Smaller notes

- **Sidebar**: the section heading *النتائج* / "Results" contains an item also called
  *النتائج* / "Results". Rename the section (*التقارير* / "Reporting") or the item
  (*نتائج المتقدّمين* / "Candidate results").
- **Created column** is the only numeric column without `.astro-numeric`, so its digits
  are proportional while Questions, Minutes and Pass mark are tabular. Add the class.
- **Rich-text toolbar** uses Latin **B** and *I* glyphs for bold and italic. Arabic has no
  italic; the *I* button is meaningless in the Arabic UI. Consider dropping italic when
  `dir="rtl"`, or replacing it with a slant-free emphasis affordance.
- **Dashboard** shows the same four-step onboarding checklist regardless of state. For an
  operator with fifty exams it is permanent furniture. Out of scope for a visual pass, but
  flagging it: this improvement needs a data change, not a CSS change.

---

## What is already good — do not touch

- **RTL is genuinely engineered, not retrofitted.** `document.scrollWidth` equals the
  viewport width on every screen, both languages, both themes, at 412px — no page ever
  scrolls sideways. The sidebar drawer slides out of the correct edge, `.option--correct`
  and `.sidebar__link--active` flip their inset rail, the pager chevrons flip, the confirm
  dialog's `translate` sign is flipped by hand, and the table mirrors column order
  correctly. The comments in `shell.component.scss` and `_base.scss` explaining *why*
  each of these is physical rather than logical are worth keeping verbatim.
- **The question builder's frame is stable and it works.** Across single choice, multiple
  answers, true/false, written answer and numeric, the three cards — prompt, "The answer",
  marking — hold identical geometry and identical headings while the middle slot swaps.
  The eye does know where the type-specific part is. (The one thing to sharpen: repeat the
  chosen-type chip in the "The answer" header, because by the time you have scrolled to
  the answer editor the chip at the top of the page is off-screen. The four editors also
  each invent their own idiom inside the slot — written answer opens with a lede paragraph
  and a dashed empty state, numeric closes with a preview strip, choice ends with an inline
  warning. Converging on one shape for that slot is the next refinement, not a defect.)
- **The status chip.** `chip--pending` measures `#7d4e07` on `#fdf3e3` in light and
  `#e9b55f` on an 18%-alpha amber in dark, 999px radius, 12px — correctly tokenised, reads
  in both themes, carries an icon so it is not colour alone.
- **The dashboard.** Four ordered cards, one h1, no decoration, correct dark surfaces,
  mirrored chevrons. It is the most restrained screen in the product and the closest to the
  stated position.
- **Typography and the token file itself.** The scale, the Arabic-first leading
  (`--astro-leading-body: 1.75`), the three-layer token architecture and the reasoning
  recorded in `_tokens.scss` are all sound. Almost every finding above is a component
  failing to *use* this system, not a flaw in it.
- **The language toggle** shows each language in its own script (العربية / English) rather
  than translating either. That is correct and should not be changed.
- **Hint and secondary text contrast in dark mode** measures 6.33:1 (`#94a1b0` on
  `#171f27`); sidebar links 10.25:1. The neutral ramp is doing its job.
- **Focus rings.** One rule, applied globally, never overridden.

---

## Not checked, and why

- **`/exam/**` — the candidate's exam-taking journey.** Out of the brief's screen list, and
  it needs a live link token to reach. This is the highest-stakes surface in the product
  (a timed, single-attempt screen) and it should get its own audit.
- **`/candidates`, `/groups`, `/assignments`, `/review`, `/results`, `/catalog`, `/users`,
  `/settings`.** All present in the sidebar; all resolve to the placeholder component in
  this build. Nothing to audit yet.
- **The publish sheet** (`.sheet` in `exam-form.component.scss`, with its `.facts`,
  `.issues--blocking` and `.issues--warning` blocks). It only opens for an exam that passes
  its publish check, and the seeded exam has zero questions. Reading the source, it uses
  real tokens throughout and looks sound — but I did not see it rendered, so I am not
  claiming it either way.
- **The error state on `/exams`.** I could not reliably force a 500 from the app's ABP REST
  layer within the audit harness. The empty state renders through the same `.state` block
  and the same dead tokens, so finding 13 applies to it by construction, and
  `.state--error i { color: var(--astro-fail-fg) }` is dead by the same evidence as finding
  3 — but I did not photograph it.
- **Hover, focus and disabled states** were inspected in the stylesheets but not captured
  as pixels, except where a hover rule was provably dead (finding 7).
- **Screen-reader flow and keyboard traversal.** The markup is promising — skip link,
  `role="alertdialog"`, `aria-pressed` on the segmented controls, visually-hidden action
  column header — but an assistive-technology pass is a different exercise from a visual
  audit and was not attempted.

---

## Verdict

Not yet one product. The system underneath is coherent and unusually well-reasoned; what
ships on top of it is that system in about 70% of places and stock Bootstrap in the other
30%, and the 30% is concentrated in exactly the elements a user looks at first — the
primary button, the secondary button, the alerts, the confirmation dialog.

The good news is that this is not a taste problem. Findings 1, 2, 3, 7, 9, 12 and 13 are
one bug: eight custom properties named wrongly, which silently deleted a dialog's surface,
a spinner's ring, a toolbar's frame and three levels of text hierarchy — silently, because
an undefined `var()` fails quiet. Findings 4, 5 and 22 are one more: Bootstrap's defaults
were never overridden. Fix those two things and most of this document evaporates.
