# الأداء والحِمل | Performance and load

<div dir="rtl">

ما يُقاس هنا ليس عدد الطلبات في الثانية. المقياس هو ما يشعر به ممتحَنٌ واحد بينما
يفعل مئة وتسعة وأربعون غيره الشيء نفسه: إنسان تحت ضغط الوقت، في محاولة واحدة لا
تُعاد، ينتظر سؤالاً يظهر. لذلك تُذكر النسب المئويّة لكلّ خطوة من رحلته، لا رقم
إجماليّ واحد — والرقم الإجماليّ يُخفي بالضبط ما نريد رؤيته.

</div>

Run it with:

```
node tools/seed-tenants.js          # once, to have something to sit
node tools/load-test.js --candidates 150
```

It drives the real HTTP surface as a candidate does — open the link, start, fetch
each question, save each answer, submit — with no token, because a candidate has
no account.

## Measured, 2026-08-30

All against SQL Server **LocalDB** on one developer machine, API and database on
the same host. Exam: `اختبار تحديد المستوى — الإنجليزية`, 8 questions in the bank.

| Concurrent candidates | open link (p50) | **start** (p50 / p95) | load question (p50) | save answer (p50) | submit (p50) | completed |
|---|---|---|---|---|---|---|
| 1 | 12ms | **28ms** / 28ms | 7ms | 11ms | 25ms | 1/1 |
| 25 | 60ms | **246ms** / 474ms | 11ms | 17ms | 150ms | 25/25 |
| 150 | 487ms | **1982ms** / 3214ms | 10ms | 17ms | 1619ms | 150/150 |

2,850 requests in 4.6s at 150 concurrent. **No journey failed at any level.**

## What the numbers say

**The steps a candidate repeats stay flat.** Loading a question and saving an
answer sit at 10–17ms whether one person is sitting the exam or a hundred and
fifty. These happen once per question — dozens of times per sitting — so they are
the ones that decide whether the exam feels responsive. They do not degrade.

**The two slow steps are the once-per-sitting ones**, and they degrade close to
linearly with concurrency: start costs 28ms alone and about 2s when a hundred and
fifty people press it in the same second. That is contention, not inherent cost —
the work itself is 28ms.

**It is not the question bank.** The obvious suspect is `StartAsync` reading
everything the exam may draw into memory for each candidate, but the bank here is
8 rows; at this size it is not the cost. It would become one, and the number
worth re-measuring against is a bank of several hundred.

**It is round trips and LocalDB.** `StartAsync` saves four separate times in one
request — insert the attempt, insert its questions, record exposure, then update
the attempt's maximum score — and each is its own trip to the database. Under a
thundering herd those serialise. LocalDB is also a developer database, not a
server: this ceiling is a property of the test rig at least as much as of the
product.

## What this does and does not establish

It establishes that 150 people can start the same exam in the same second and all
150 finish it, with the per-question interaction staying fast throughout. For a
cohort of that size — a language centre's intake day, an academy's monthly
assessment — that is the shape of the real event, since everybody is told to
start at nine o'clock.

It does not establish a production ceiling. Nothing here has been run against a
real SQL Server, on separate hosts, behind TLS, with more than one API replica,
or with a bank of realistic size. `docs/deployment.md` lists what a real
deployment still needs; until that exists these figures describe a laptop.

## If it needs to be faster

In the order worth trying, and none of it done yet:

1. **Collapse the four saves in `StartAsync` into one.** It is the clearest win
   and touches the most safety-critical path in the product — attempt creation is
   guarded by a unique index on `(ExamLinkId, unsubmitted)` so a double click
   cannot produce two sittings, and that guarantee must survive any batching.
   Worth doing deliberately, with the concurrency tests re-run, not casually.
2. **Cache the drawable bank per exam.** Same for every candidate and changing
   rarely during a sitting window — but it needs a decided answer to what happens
   when an author adds a question while a cohort is sitting. Papers already drawn
   should not change; later starts arguably should see it.
3. **Measure with a realistic bank** before either. A 500-question bank may move
   the bottleneck somewhere else entirely, and optimising the wrong step is worse
   than leaving it.
