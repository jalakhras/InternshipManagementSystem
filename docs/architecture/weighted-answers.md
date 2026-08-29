# Weighted answers

## The problem

Every choice question is binary today: an option is right or wrong, and the
grader rewards the exact correct set and nothing else.

The product owner described the gap as *"خطأ ام صحيح ام أصح"* — wrong, right, or
**more** right — and said the idea was not fully formed. It is a real gap for
the audiences this product serves. A trading academy asks what to do with a
losing position: "close it" and "move the stop to break-even" are both
defensible, one is better, and "add to the position" is the answer the question
exists to screen out. A language exam has a phrasing that is correct and one
that is idiomatic. Binary scoring cannot represent either.

This is the established **best-answer** pattern from professional certification.
It needs three things a flat `IsCorrect` flag cannot give: a best answer,
acceptable answers worth something less, and harmful answers worth less than
nothing.

## The decision

**A per-option `weight` in −1..1 inside the existing `ChoicePayload`, switched on
by a per-question `weighted` flag, computed by the two graders that already own
that payload.** No new grader, no new column, no migration, and no change to
what a candidate receives.

Four decisions make it concrete.

**The switch is explicit.** `ChoicePayload.Weighted` is a nullable bool; absent
means false. An author does not get weighted scoring by leaving a stray weight
behind — inferring the mode from "does any option carry a weight" is one careless
save away from silently changing how a live question is graded.

**Weighted replaces all-or-nothing for that question, entirely.**
`AllowPartialCredit` and the "any wrong selection voids it" rule apply only when
`Weighted` is false. The two models were never meant to combine; the gap is
answered by one new mode, not by teaching the old one to do fractions.

**Negative weights price harm, but a question floors at zero.** A harmful option
pulls a multi-select sum down and wastes a single-choice pick. It never scores
below what leaving the question blank would score, because a scoring system that
punishes attempting is a scoring system that teaches candidates not to attempt.

**`IsCorrect` marks the best answer**, enforced by the validator, so the
reviewer's key, the item statistics and the grader cannot drift apart. What that
means differs by type, and getting it wrong here was expensive: on a single
choice the best answer is one option worth the whole question, but on a
multi-select it is a *set* whose parts add up to the whole question. The first
version of this validator required every credited option to be worth 1.0 —
so two of them summed to twice the marks, the award was clamped back down to
full, and ticking every box scored full marks on any multi-select with two right
answers. A security review found it within the hour.

### Why not a new question type

A new type costs a grader, a payload shape, an entry in `QuestionTypes`, the
validator, the projector, and the correct-answer renderer — for a change that is
about how two graders compute a number, not about what the taker sees. The taker
sees identical options and the same interaction. Nothing about the type changed.

## The payload

```csharp
public sealed class OptionPayload
{
    public string Id { get; set; } = default!;
    public string Text { get; set; } = default!;
    public bool IsCorrect { get; set; }
    public string? BlobName { get; set; }

    /// Weighted mode only. Share of the question's marks this option is worth:
    /// 1.0 is the best answer, 0 is neutral, negative is actively harmful.
    public decimal? Weight { get; set; }
}

public sealed class ChoicePayload
{
    public List<OptionPayload> Options { get; set; } = new();
    public bool AllowPartialCredit { get; set; }

    /// Switches to per-option weighted scoring. Absent or false is every
    /// question written before this existed, graded exactly as it is today.
    public bool? Weighted { get; set; }
}
```

Both are nullable so an unweighted question's JSON is byte-for-byte what it is
today — the serialiser omits nulls. A non-nullable `bool Weighted = false` would
have written `"weighted":false` onto every payload from now on for no reason.

### Worked example — single choice

```json
{
  "weighted": true,
  "options": [
    { "id": "a", "text": "Close the full position",        "isCorrect": false, "weight": 0.6 },
    { "id": "b", "text": "Move the stop to break-even",    "isCorrect": true,  "weight": 1.0 },
    { "id": "c", "text": "Add to the losing position",     "isCorrect": false, "weight": -0.5 },
    { "id": "d", "text": "Do nothing",                     "isCorrect": false, "weight": 0 }
  ]
}
```

### Worked example — multi-select

```json
{
  "weighted": true,
  "options": [
    { "id": "a", "text": "Check the airway",   "isCorrect": true,  "weight": 0.4 },
    { "id": "b", "text": "Check breathing",    "isCorrect": true,  "weight": 0.4 },
    { "id": "c", "text": "Check circulation",  "isCorrect": true,  "weight": 0.2 },
    { "id": "d", "text": "Medicate first",     "isCorrect": false, "weight": -0.6 }
  ]
}
```

`a+b+c` sums to 1.0 and is the only combination that reaches full marks, because
the one option outside the best set is priced to cost more than any partial
credit it could sit beside.

## Grading, case by case

With `maxScore = 10`.

| Type | Mode | Selection | Awarded | IsCorrect |
|---|---|---|---|---|
| single | binary | correct id | 10 | yes |
| single | binary | wrong id | 0 | no |
| single | **weighted** | `b` (1.0) | 10 | yes |
| single | **weighted** | `a` (0.6) | 6 | no |
| single | **weighted** | `c` (−0.5) | 0 (floored) | no |
| single | either | none, or two | 0 | no |
| multi | binary | exact correct set | 10 | yes |
| multi | binary | any wrong pick | 0 | no |
| multi | **weighted** | `{a,b,c}` | 10 | yes |
| multi | **weighted** | `{a,b}` | 8 | no |
| multi | **weighted** | `{a,b,c,d}` | 4 | no |
| multi | **weighted** | `{d}` | 0 (floored) | no |
| multi | either | nothing selected | 0 | no |

Rounded to two places, away from zero — the convention the partial-credit path
already uses. `GradeResult` needs no change: `Partial(awarded, maxScore)` already
sets `IsCorrect = awarded >= maxScore`, which is exactly "reached full marks".

## What the validator refuses

| Code | Fires when | Why it blocks |
|---|---|---|
| `WeightMissing` | weighted and any option has no weight | The grader cannot price the question; treating the gap as zero would hide an authoring slip. |
| `WeightOutOfRange` | any weight outside −1..1 | One careless option could dominate or invert the question's marks. |
| `WeightConflictsWithCorrectFlag` | single: `IsCorrect` without weight 1.0, or the reverse. multi: `IsCorrect` without a positive weight, or the reverse | Keeps one canonical key. Credited means *part of* the best answer on a multi-select, not *the whole* of it. |
| `WeightsDoNotSumToOne` (multi only) | the credited weights do not add to 1.0 | Short of it and nobody can reach full marks; over it and a partial answer does. |
| `SelectingEverythingScoresFull` (multi only) | all weights sum to 1.0 or more | The condition that actually closes "tick every box". An earlier rule asked only whether *a* penalty existed, which let a −0.1 sit beside two options worth 1.0 each and called it safe. |

The existing `NoCorrectOption` check already refuses a question with no
`IsCorrect` option, which together with the invariant above guarantees a weighted
question has an option that can reach full marks.

## What a candidate receives

Nothing. `TakerQuestionProjector` copies exactly `Id`, `Text` and `MediaUrl` into
`TakerOptionDto`, which has no weight field — the new property is invisible to it
by construction, the same way `IsCorrect` already is. No change is required
there, and a test asserts `weight` never appears on the wire, beside the one that
already asserts it for `isCorrect`.

## What a reviewer and a learner see

The reviewer already receives the rendered key. Rendering gains four buckets:
**best answer**, **acceptable**, **not credited**, **penalised** — so a 6 out of
10 explains itself where the reviewer is already looking.

In practice mode a learner sees three of those four: best, acceptable, or not
credited. "Penalised" is a reviewer's concept; naming a harmful option to a
learner without the room to explain why would alarm without teaching. Telling
someone they chose an acceptable answer rather than the best one is the whole
pedagogical value of the feature, and it is designed rather than implied.

## Backward compatibility

Every existing row has neither field. Both deserialise to null, both graders
branch on `Weighted == true`, and null takes the path that runs today unchanged.
No backfill, no default weights, no re-save.

## Item statistics

`DifficultyIndex` keeps its definition: `IsCorrect` on a weighted question means
"this taker found the best answer", which is the same shape of fact it means on a
binary one. No special case.

`DiscriminationIndex` is less clean. A binary calculation still works but discards
what weighted scoring exists to capture — a weak taker landing on "acceptable"
and one landing on "penalised" both count as not correct. A ratio over
`awarded / max` would discriminate better. Left open: no item-statistics job
exists yet to weigh the cost against.

## Open questions

- Is ±1 a platform constant or a tenant policy? Some certification bodies cap a
  penalty at −0.25. Hard-coded for now; making it configurable is a genuine case
  for tenant settings.
- Should weighted multi-select also offer "any penalised pick voids the
  question", for tenants wanting a harsher rule than the additive floor?
- Ordering and matching could carry the same per-item weight later, by the same
  validator technique. Not designed here.
