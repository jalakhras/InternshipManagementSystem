# External design tools — what we took, and why we kept our own system

Two tools were given the brief in `brief.md`: Blink and Google Stitch.

**Decision: keep our design system. Harvest five ideas from Blink. Stop there.**

---

## Why keep ours

The token layer, the RTL-first layout and the reserved semantic palette are
implemented, tested and correct. Twenty-seven browser tests hold them in place,
including two that caught real RTL defects a generator would not have found.

Blink's output reached the same *concepts* independently — which is useful
validation — but its *execution* carried four errors. Adopting it wholesale would
have imported them.

---

## What Blink got right, unprompted

Worth recording because it is evidence the direction is sound, not just our
preference. Asked for an exam screen and given our constraints, it reached for
"the care and restraint of Pearson VUE and Linear" and wrote:

> keeping the surrounding scaffold out of the way for v1 rather than turning this
> into a dashboard

That is the same conclusion the brief argues for, arrived at separately.

It also implemented, correctly and without being told the mechanics: a mono
countdown, a save indicator with a relative timestamp, a question map with an
explicit answered/unanswered legend, and a shared stimulus panel.

---

## The five ideas we are taking

### 1. Label which questions a stimulus covers

Blink rendered the passage panel with **"يظهر مع الأسئلة ١–٦"** — *shown with
questions 1–6*.

We had the shared-stimulus concept but not this label. It answers a question every
candidate has on a grouped exam — *how much of this passage do I need?* — before
they ask it. Cheap, and it removes a real moment of uncertainty under time
pressure.

### 2. Offer the stimulus in a second modality

It added **"يتوفر هذا النص أيضاً كتسجيل صوتي"** — *this text is also available as
an audio recording*.

An accessibility win we had not specified. The same stimulus in two forms serves
someone with low vision and someone who simply absorbs audio better, and for a
language exam it is sometimes the point.

### 3. Name the question type beside the question number

A small chip reading **"اختيار من متعدد"** next to *question 1 of 10*.

The candidate learns what kind of answer is expected before reading the question.
Cheap, and it prevents the small stall of reading a question twice to work out
whether it wants one answer or several.

### 4. Section label above the question

**"الجزء الأول · فهم المقروء"** — *part one · reading comprehension*.

Useful whenever an exam has parts. It also gives the topic breakdown a visible
presence during the exam rather than only in the result.

### 5. A legend on the question map

Explicit **"تمت الإجابة"** and **"لم تُجب"** labels rather than colour alone.

We already require this in principle — no meaning by colour alone — but seeing it
drawn is a reminder that the map is exactly where the rule is easiest to forget.

---

## The four errors, kept as a checklist

These are what we correct in our own build, and what any future generated design
must be checked against.

### 1. The layout was mirrored the wrong way

The question sat in a narrow column on the **left** and the map filled the right.
That is an LTR layout with Arabic poured into it. In RTL the primary content
starts at the right edge.

This is the single most common failure when a tool "supports RTL": it flips text
alignment and leaves the information hierarchy where it was.

### 2. Arabic was letter-spaced

`الــتـقـ__دم` and `الـجـزء الأول` rendered with tracking.

**Arabic is a connected script.** Letter-spacing either breaks the joins or makes
them look broken. Tracking is a Latin typographic convention and does not
transfer; emphasis in Arabic comes from weight and size.

Our `_base.scss` applies letter-spacing only to uppercase Latin labels. That
restriction is deliberate and must stay.

### 3. A navigation aid displaced the task

The question map took roughly 1100px while reading and answering — the entire
purpose of the screen — was squeezed into about 200px.

The map is an aid. It opens on demand and overlays; it does not push the task
aside.

### 4. The measure was far too narrow

A one-line question wrapped over five lines. Arabic needs 45–75 characters per
line and more line-height than a Latin-tuned scale gives it — our tokens set
1.75 for body text, and that number exists for this reason.

---

## Google Stitch

Never rendered. The page loads an Angular shell whose content sits in a
cross-origin companion iframe, and the inner application stayed blank through a
reload and a sign-in. Not pursued further: the marginal value over what Blink
already produced did not justify more time on the tooling itself.

---

## The standing conclusion

These tools are useful for **concept validation and idea harvesting**, and poor at
**RTL execution and script-specific typography**. Use them to check whether a
direction holds up when someone else reads the brief, then build it here — where
the RTL behaviour is tested rather than assumed.
