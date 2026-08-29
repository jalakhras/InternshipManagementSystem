# Design brief — Astrolabe

Paste this into a design tool (Blink, Stitch, v0, or similar) to generate screen
concepts. It is written to be pasted whole: the constraints matter as much as the
description, and a tool given only "design an exam platform" will return the
generic dashboard we are specifically trying not to build.

---

## The product in one paragraph

Astrolabe is an assessment platform. An organisation defines what it measures,
writes exams, and sends each person a private link; they sit the exam in a browser
under a timer and the platform marks it. The organisation is *any* organisation —
a recruitment firm testing developers, a language school placing students, a
trading academy checking whether its course worked. The platform never assumes the
subject.

The name is Arabic (أسطرلاب) and is the same word in English. An astrolabe is the
instrument that tells you where you actually stand. That is the product's claim,
and the visual language should feel like an instrument: precise, calm, unshowy.

---

## Who uses it, and what each needs from the screen

**The administrator** builds exams and question banks. Long sessions, dense forms,
thirteen question types each with a different shape. They need density and speed,
not whitespace.

**The reviewer** marks open answers. Reads a lot of prose, awards marks against a
rubric, sometimes hundreds in a sitting. They need the answer, the rubric and the
key on one screen without scrolling between them.

**The person sitting the exam** uses the product once, under a countdown, often on
a phone, and cannot come back. They need one question at a time, an unmistakable
timer, an obvious save state, and nothing else on the screen. This is the highest-
stakes surface: a defect here costs a real person their marks.

---

## Non-negotiable constraints

These are decisions already made and implemented. A design that ignores them
cannot be used.

**Arabic-first, right-to-left by default.** Not a translation layer — the default
reading direction is RTL and English is the second language. Every layout must
mirror. Directional icons flip; symmetrical ones must not.

**Bootstrap 5, no component library.** The implementation is plain Bootstrap with
a custom token layer. Designs should not depend on Material, Ant, Chakra or
shadcn components.

**Semantic colour is reserved.** Green means *passed*, amber means *awaiting
review*, red means *failed*. These three cannot be used decoratively anywhere,
and the brand accent must be clearly distinguishable from all three.

**The accent is a deep petrol blue** (`#0D5C70`, lighter `#4FC4D6` on dark
grounds). Explicitly not the generic SaaS blue `#0369A1`, and explicitly not a
purple-to-blue gradient.

**Typography is IBM Plex Sans Arabic**, with IBM Plex Mono for anything where
digits line up or tick — scores, timers, durations. Arabic needs more line-height
than a Latin-tuned scale gives it.

**Light and dark both ship.** Not an afterthought; both are designed.

**WCAG 2.2 AA.** 4.5:1 text contrast, visible focus rings everywhere, 44×44px
minimum touch targets, no meaning carried by colour alone.

---

## What to avoid, specifically

- The purple/blue gradient hero. It reads as machine-generated now.
- Emoji as icons. SVG only.
- A dashboard of stat tiles as the landing screen. A new customer's first view has
  no data, and four zeroes in cards tell them nothing. The landing screen should
  be the next action.
- Rounded cards with a coloured left rail on everything.
- Centring everything.
- Illustration-heavy empty states. An empty state should carry the action that
  fills it, not a drawing.

---

## Screens to design, in priority order

### 1. Sitting the exam — the highest-stakes screen

One question at a time. Needs, in rough order of importance:

- A countdown that cannot be misread, and is announced to screen readers. Warning
  states at five minutes and one minute.
- A save indicator that shows the last successful save. People under time pressure
  need to know their work is safe.
- A question map: which are answered, which are not, jump between them.
- Thirteen question types share this frame — multiple choice, multi-select,
  true/false, free text, a number with a unit, matching two columns, ordering
  items, filling blanks in a sentence, clicking a region of an image, code with a
  starter template, a file upload, an audio recording, and a 1-5 scale.
- Some questions share a stimulus: one reading passage, chart or audio clip with
  several questions about it. The stimulus must stay visible while answering.
- Submit with a confirmation that names how many questions are unanswered.
- Works on a phone. Most candidates will not be at a desk.

### 2. Building a question

Type is chosen first, and the form changes shape with it. Each type needs: the
prompt, its type-specific body, marks, an optional topic, an optional per-question
timer, an optional explanation shown afterwards in practice mode, and optional
media.

The hard part is that thirteen shapes must feel like one screen rather than
thirteen.

### 3. The exam list and the exam editor

List: title, category, level, status (draft / published / archived), question
count, pass mark. Publishing is a distinct action from saving.

Editor: settings, the question bank, and a "blueprint" — rules like *8 medium
questions from Listening* that generate a different but equally hard paper for
each candidate. Each rule shows how many bank questions actually match it, so
"draw 8 from a pool of 5" is visible before it shortens someone's exam.

### 4. The review queue and marking screen

Queue: oldest first, showing who, which exam, how many answers are pending, and a
count of integrity observations.

Marking: the answer, the rubric with per-criterion marks, the key, and the
behavioural context (was it pasted, how long did it take, how many corrections).
All on one screen — scrolling between the answer and the rubric is what makes
marking slow and inconsistent.

### 5. The result

For the candidate: their score, whether they passed, and a per-topic breakdown. A
single number tells nobody what to do; "SQL 85%, algorithms 40%" does.

For the organisation: the same, plus comparison against everyone else who sat it.

---

## Tone

Instruments, not dashboards. Quiet, dense where density helps, generous where
reading happens. The product's job is to be trusted with a judgement about a
person, so it should feel careful rather than clever.
