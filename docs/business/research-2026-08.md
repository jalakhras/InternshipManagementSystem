# Research, August 2026 — beyond the hiring reference class

`competitive-position.md` benchmarked us against eight platforms, five of which
turned out to be the wrong reference class, and correctly re-pointed us at item
banking and certification. This document goes the rest of the way: it looks at
what certification bodies, language testers, regulators and the psychometric
literature actually demand, and asks which of it we should build.

Rule for reading this: every claim carries a source. Where the source is a
vendor selling the thing it describes, it is marked **[vendor]**. Where I could
only reach a marketing page, or could not reach the primary document, it is
marked **[unverified]**. Where I read the primary document, it is unmarked.

---

## 1. What changed since the last benchmark

Six things have moved, and three of them change what we should build.

**The self-hosting advantage is no longer unique.** `competitive-position.md`
says "Every platform on the list is SaaS-only" and treats installability as a
structural win. That was true of the list. It is not true of the reference
class the same document told us to take seriously. **TAO Community Edition** is
open source, free to download and self-install, and was relaunched on 5 January
2026 with a modular stack (TAO Advance, TAO Grader, TAO Insights, TAO Portal).
TAO is also QTI-certified in four categories and took QTI v2.2 Application
certification on 30 January 2026. A ministry that wants an on-premise,
standards-compliant assessment platform has a free option. Our on-premise story
is still a real advantage against Questionmark, Mettl and TestGorilla; it is not
an advantage against TAO, and if a procurement officer has heard of TAO we need
a better answer than "we install locally".
([TAO CE launch](https://www.taotesting.com/blog/oat-launches-tao-community-edition/),
[1EdTech certification record](https://site.imsglobal.org/certifications/open-assessment-technologies-sa/tao))

**The Arabic-first claim is now contested.** The last benchmark's list was
English-first by construction; regional vendors were not on it. **Evalufy**
markets itself as Arabic-first — Arabic and English interfaces, 800+ predefined
tests including Arabic, Hijri/Gregorian dates, Saudi data-residency options, a
freemium tier — and **Elevatus** is described as having a mature Arabic
interface across ATS, assessment and video interviewing. **[vendor,
unverified]** — Evalufy's Arabic-first claim comes from Evalufy's own
comparison blog, which is marketing, and I did not verify the RTL quality of
either product. But we can no longer say "nobody else is Arabic-first" in a
meeting in Riyadh without being contradicted. What we can still say, and what
neither of them is, is Arabic-first **item banking with psychometrics**. Both
are hiring/ATS products.
([Evalufy vs TestGorilla](https://www.evalufy.com/blog/candidate-assessment-selection/evalufy-vs-testgorilla-ksa-uae-arabic-ui-pricing-support/),
[MENA platform roundup](https://www.evalufy.com/blog/video-interviewing-assessments/top-bilingual-assessment-platforms-arabic-english/))

**Invasive proctoring got legally worse, not better.** The 2022 US federal
ruling in *Ogletree v. Cleveland State* held that pre-exam room scans are an
unreasonable search under the Fourth Amendment — the first ruling of its kind.
In Europe, a Thuringian higher regional court decision of 17 November 2025 is
reported to have found that collecting biometric data for facial recognition to
identify examinees violates Article 9 GDPR **[unverified — I could not reach
the primary decision; the report is from a data-protection consultancy]**. Our
decision to avoid invasive proctoring has aged well and should now be marketed
as a position, not apologised for as a gap.
([Ogletree opinion](https://caselaw.findlaw.com/court/us-dis-crt-n-d-ohi-eas-div/2109381.html),
[EFF analysis](https://www.eff.org/deeplinks/2022/08/federal-judge-invasive-online-proctoring-room-scans-are-also-unconstitutional),
[Thuringian decision report](https://2b-advice.com/en/2025/12/18/proctoring-in-the-application-process-data-protection-admissibility-under-the-gdpr/))

**The EU AI Act now names us — and then gave everyone more time.** Annex III
category 3 classifies AI systems used in education and vocational training that
determine access, evaluate learning outcomes, assign levels, or monitor and
detect prohibited behaviour during tests as **high-risk**. The obligations
(risk management, bias testing, documented human oversight, transparency) were
due 2 August 2026. The Digital Omnibus, proposed by the Commission on 19
November 2025 and politically agreed in 2026, defers the bulk of the Annex III
obligations to **2 December 2027**. Practical consequence for us: any AI grading
or AI cheating-detection shipped into the EU must have documented human
oversight and must not produce a final grade without human review — and we have
until December 2027, not August 2026, to have that written down.
([Digital Omnibus deal, White & Case](https://www.whitecase.com/insight-alert/eu-agrees-digital-omnibus-deal-simplify-ai-rules),
[DLA Piper on the deferral](https://knowledge.dlapiper.com/dlapiperknowledge/globalemploymentlatestdevelopments/2026/The-Digital-AI-Omnibus-Proposed-deferral-of-high-risk-AI-obligations-under-the-AI-Act),
[Annex III education scope](https://www.praxikon.com/en/posts/high-risk-ai-education))

**Saudi data rules now bite in a way that favours us.** SDAIA's Regulation on
Personal Data Transfer Outside the Kingdom (updated September 2024) plus the
February 2025 Risk Assessment Guidelines require documented risk assessment for
continuous or large-scale transfers, and penalties reach SAR 5 million per
breach. **[unverified in the primary Arabic text — read via law-firm
summaries]**, but the direction is unambiguous. Egypt's Executive Regulations
to Law 151/2020 were issued in November 2025, bringing that regime fully into
operation. Our installable deployment is directly responsive to this — which is
exactly why point one matters.
([ITIF on KSA transfers](https://itif.org/publications/2025/06/09/saudi-arabia-cross-border-data-transfer-regulation/),
[Clyde & Co on the 2025 guidelines](https://www.clydeco.com/en/insights/2025/03/update-on-saudi-arabia-risk-assessment-guidelines),
[Baker McKenzie on Egypt](https://www.bakermckenzie.com/en/insight/publications/2026/01/egypt-important-data-protection-update))

**The "AI detectors are biased against non-native speakers" consensus is
cracking.** More on this in §3 — it is the finding I most expected to confirm
and least did.

---

## 2. Ranked recommendations

Ranked by what most strengthens *this* product given what it already has:
13 types behind a JSON payload, a domain+level-owned item bank, blueprint-driven
form generation with per-candidate shuffling, hybrid grading with a human review
queue, signed-link entry, per-item difficulty and discrimination, publish-time
warnings.

---

### R1. Turn blueprint generation into LOFT (linear-on-the-fly testing)

**What it is.** We already generate a form from a blueprint and shuffle per
candidate. LOFT is one step further: every candidate gets a *different subset*
of items from a pool, assembled to the same blueprint **and** to the same
statistical target, so forms are psychometrically parallel rather than merely
differently ordered.

Assessment Systems' description is exact and worth quoting: "every examinee gets
a randomly unique set of items from a pool, but built to equivalent
specifications to ensure fairness. For example, we might ensure that everyone
gets 100 items, 20 from each of five domains, with an average P item difficulty
of 0.72." Content equivalence comes from the blueprint; statistical equivalence
from targeting equal mean p-value (and, where point-biserials exist, equal SD
and reliability). The stated benefits: test security through reduced exposure,
no pre-assembly of parallel forms, comparable difficulty across candidates,
simpler than CAT to implement and defend, efficient pool use — and, critically
for us, candidates can still skip, return and change answers, which pure
adaptive testing usually forbids. **[vendor]** — ASC sells FastTest, which does
LOFT, and the same page's line "Very few testing platforms can implement a
quality LOFT assessment" is both a fair observation and a sales pitch.
([ASC on LOFT](https://assess.com/linear-on-the-fly-testing/))

**What it unlocks commercially.** This is the honest answer to "how do you stop
cheating without a webcam". A recruiter running the same role exam 400 times, a
language centre placing three intakes a year, a training academy whose students
share screenshots in a WhatsApp group — all of them currently depend on us
rotating forms by hand. LOFT makes exposure a *property of the algorithm* rather
than a warning an author has to act on. It is also the feature that makes
"psychometrically parallel forms" a defensible phrase in front of a
certification buyer, and it turns our existing `TimesServed` counter from a
diagnostic into an input.

**Cost.** Low to medium — the reason it ranks first. The generator, blueprint,
bank, difficulty index and exposure counter all exist. What is missing is a
constrained selection step: given a blueprint cell (domain × level × competency
× count), choose items that hit a target mean difficulty within tolerance while
preferring low-exposure items and respecting a per-item exposure ceiling. That
is a scoring-and-sort over a candidate set, not a new subsystem. Add
`TargetMeanDifficulty` and `DifficultyTolerance` to the blueprint rule,
`MaxExposureRate` to the pool, and a per-attempt record of which items were
served (needed anyway for R5).

**Fit.** So clean it is arguably where the current architecture was already
heading. The only friction: publish-time warnings change character. Today a thin
bank is advisory; under LOFT it is a hard assembly failure, and the author must
be told *which blueprint cell starved*.

---

### R2. Make the cut score a first-class object, with a modified-Angoff workflow

**What it is.** Today a pass mark is presumably a number an author types. That
is exactly what standard setting exists to replace. ASC is blunt: "If you have a
criterion-referenced interpretation, it is not legally defensible to just
conveniently pick a round number like 70%; you need a formal process." The
modified-Angoff method is the most common: assemble 6–20 subject-matter experts
(minimum 6, 8–10 preferred), define the Minimally Competent Candidate, have each
expert estimate the percentage of MCCs who would answer each item correctly,
check inter-rater agreement, discuss, re-rate, and take the mean of final
ratings as the expected MCC score. It works before a single candidate has sat
the exam, works under classical test theory and IRT, and works for polytomous
items. Its known failure mode is experts overestimating entry-level candidates,
which the Beuk method corrects by showing the panel the implied pass rate.
**[vendor]** — ASC sells Angoff consulting; the method itself is standard
literature, not their invention.
([ASC on modified-Angoff](https://assess.com/modified-angoff-method/),
[Questionmark on Angoff](https://www.questionmark.com/resources/blog/what-is-the-angoff-method/),
[ICE standard-setting overview](https://www.credentialingexcellence.org/Portals/0/Images/store/ICE%20Report_Standard%20Setting%20Overview%20for%20Credentialing%20Programs.pdf))

ISO/IEC 17024 — the accreditation standard for bodies that certify people —
requires, among other things, that "tests are designed by job analysis, have
cutscores set by a formal standard-setting study, and analyze psychometrics to
ensure quality", plus impartiality, defined competencies, a documented
certification decision process, appeals, record keeping and continual
improvement. ([ASC on ISO 17024](https://assess.com/ansi-iso-17024/)
**[vendor]**; [ISO catalogue entry](https://www.iso.org/standard/17024))

**What it unlocks commercially.** Three things at once, which is unusual.
(a) Certification bodies: a cut score with a documented panel study behind it is
the difference between "an exam" and "an accreditable exam". (b) Language
centres: CEFR placement is not one cut score, it is a *set* of boundaries
(A1/A2, A2/B1, …) over one form — multi-cut standard setting is the same object
with more thresholds. The Council of Europe's own Manual for relating
examinations to the CEFR prescribes familiarisation → specification →
standardisation → standard setting → validation, so a platform that supports the
standard-setting step is a platform a centre can honestly claim CEFR alignment
with. (c) Training academies: "you passed Level 1" becomes a defended claim.
([CoE Manual](https://rm.coe.int/1680667a2d))

**Cost.** Medium-low, and mostly UI. The model is a study (form + panel), a
rater, a rating per rater per item, a round number, and a computed result. The
aggregation is arithmetic. The valuable parts are the unglamorous ones:
inter-rater agreement display, outlier flagging, round-2 re-rating with round-1
visible, and a **printable study report** — because the report is the artefact
an auditor asks for, not the number.

**Fit.** Fits: items already live in the bank with a level, and a study is a
projection over a blueprint's items. One decision to make now — the cut score
belongs to the **form/blueprint**, not the exam, because under LOFT different
candidates see different items and the pass mark must be expressed on a common
scale (see R6).

---

### R3. Report a profile and a level, not a percentage

**What it is.** `competitive-position.md` already argues for per-section
results. Push it two steps further, because the reference implementations are
more interesting than "a bar chart per section".

The Duolingo English Test reports four skill subscores (Reading, Writing,
Listening, Speaking) **plus four integrated composites derived from them** —
Literacy = Reading + Writing, Comprehension = Reading + Listening, Conversation
= Speaking + Listening, Production = Speaking + Writing — all on a 10–160 scale
in 5-point increments. The composites cost nothing to compute and are the scores
an admissions officer actually uses. Pearson's Versant does the same shape
differently: an overall score from a weighted combination of four diagnostic
subscores, reported against the Global Scale of English (10–90), so the number
means something outside the test. Cambridge Linguaskill reports against CEFR,
adaptively, across all four skills.
([DET scoring](https://blog.englishtest.duolingo.com/how-is-the-duolingo-english-test-scored/),
[Versant score report](https://www.pearson.com/content/dam/one-dot-com/one-dot-com/english/versant-test/sample-score-reports-2022/sample-score-report-versant-english-test.pdf),
[Versant/GSE alignment](https://www.pearson.com/content/dam/one-dot-com/one-dot-com/english/versant-test/GSE_Versant_English_Placement_est.pdf),
[Linguaskill](https://pages.cambridgeenglish.org/linguaskill-2026) **[vendor]**)

Concretely: (1) sections carry weights, not just scores; (2) a tenant can define
**composite scores** as named weighted combinations of sections; (3) a tenant
can define a **reporting scale** — a named framework with ordered bands and cut
scores (CEFR A1…C2; "Level 1/2/3"; pass/fail) — and the result page leads with
the band, with the raw percentage available but secondary.

**What it unlocks commercially.** The language centre is unsellable without it,
which `competitive-position.md` already says. What is new is that the *same*
mechanism serves the certification buyer (scaled score plus pass/fail band) and
the academy (level attained), so one feature pays three times. It is also what
makes the certificate a document worth printing.

**Cost.** Low, once `ExamSection` exists. A weighted sum, a band lookup, a
report template. The reporting-scale definition is a small tenant-scoped table
that reuses the `Level`/`CategorySet` vocabulary pattern already in the schema.

**Fit.** Fits, and it is the natural consumer of R2's cut scores. Build R2 and
R3 together or the band boundaries are arbitrary again.

---

### R4. Item lifecycle: states, versions, and statistics bound to the version

**What it is.** Certification bodies do not buy an item bank; they buy a
*controlled* item bank. Surpass advertises "item version comparison and
auditing", SME management workflows, and unlimited psychometric and content
tagging as core features; ISO 17024 requires document control and record keeping
as part of the management system. **[vendor]** for Surpass's feature list — it
is their marketing site and I did not see it demonstrated.
([Surpass](https://surpass.com/), [ASC on ISO 17024](https://assess.com/ansi-iso-17024/))

Three parts, and the third is the important one:

1. **States.** Draft → In review → Approved → Live → Flagged → Retired. Only
   Approved/Live items are eligible for blueprint selection.
2. **A review record.** Who reviewed, when, what they said, what changed.
3. **Versioned statistics.** When an item's stem, key, options or scoring change
   materially, its accumulated difficulty and discrimination indices describe a
   question that no longer exists. Today, if a tenant corrects a wrong answer key
   on an item served 300 times, `DifficultyIndex` silently becomes a lie — and
   because the bank is shared across forms, it is a lie in every form. This is
   not a feature request; it is a correctness bug waiting to be found by the
   first customer who cares.

**What it unlocks commercially.** It is the precondition for every certification
conversation, and it converts our psychometrics from a report into evidence. It
also protects the thing `competitive-position.md` identifies as our most
credible demo — "these six questions are not measuring anything" — from being
wrong.

**Cost.** Medium. States and a review log are cheap. Versioning with statistics
rebinding needs care: an `ItemVersion` carrying its own statistics, a rule for
what counts as a material change (key, scoring, stem semantics — not a typo
fix), and a UI that says "this change will reset 300 responses' worth of
statistics; continue?".

**Fit.** Fits the bank as designed. It forces an answer to a question the
shared-bank model must answer anyway: if a form is live and an item is edited,
does that form see the new version mid-administration? It must not — a form
binds to item *versions*.

---

### R5. Statistical integrity instead of surveillance

**What it is.** The credible, legally durable alternative to proctoring, and the
one that suits a product that has deliberately declined cameras. Three families,
all computable from data we either have or can cheaply capture:

- **Answer-similarity analysis.** For any pair of candidates, estimate the
  probability of observing that much response agreement — including *wrong*
  answers in common — under an assumption of independent work. The literature is
  mature and still active: 2025 work extends it to weighted similarity and to
  using **response times** as part of the similarity signal, and to group-level
  detection rather than only pairwise.
  ([Gorney & Wollack 2025, response times in ASA](https://journals.sagepub.com/doi/abs/10.3102/10769986241248770),
  [Trout & Gorney 2025, weighted ASA](https://journals.sagepub.com/doi/10.1177/01466216251322353),
  [Eckerly 2021, group level](https://journals.sagepub.com/doi/abs/10.1177/01466216211013109))
- **Response-time anomalies.** Response Time Effort correlates with self-reported
  effort and with person fit; implausibly fast correct answers on hard items are
  the classic pre-knowledge signature.
  ([Wise & Kong on RTE](https://files.eric.ed.gov/fulltext/ED490203.pdf))
- **Person fit.** Does this candidate's pattern of right and wrong make sense
  given the item difficulties? The caveat is stated plainly in the literature and
  we must repeat it in the UI: person fit identifies *misfit*, not *cause*;
  attributing it to cheating requires other evidence. A healthy dataset flags
  well under 5% of cases.
  ([Aberrant behaviour in unproctored CAT](https://dergipark.org.tr/en/pub/ijate/article/1598330))

**What it unlocks commercially.** A differentiated security story no hiring
platform tells, and one that survives a GDPR review, a Saudi PDPL review and an
EU AI Act conversation, because it processes exam responses rather than
biometrics. It is also honest: "we detect collusion after the fact and can show
you the evidence" beats "we watch your candidates' bedrooms".

**Cost.** Medium, with a prerequisite. We must store **per-item responses and
per-item elapsed time for every attempt**, not just the final score. If we
already do, this is a background job and a report. If we do not, that capture is
the real cost — and it should be built regardless, because R1's exposure
accounting and R4's statistics both want it.

**Fit.** Fits, with one hard rule: these are **flags for a human**, never
automatic invalidation. Cheating detection in education is Annex III high-risk
under the EU AI Act, and a documented human-oversight arrangement — who reviews,
their competence, how they override — is part of what compliance means. Build
the reviewer screen at the same time as the statistic, not after.

---

### R6. Anchor items and drift monitoring (a deliberately partial equating)

**What it is.** The moment a language centre has three forms for A1 and uses
them across intakes, scores from different forms are not interchangeable unless
they are equated. Full equating — nonequivalent groups with anchor test (NEAT)
designs, common-item linking — is standard practice and genuinely hard: anchor
items must retain their statistical properties across forms, and small-sample
equating is a research field of its own (circle-arc and mean equating perform
best at small N).
([Anchor design and parameter stability](https://www.frontiersin.org/articles/10.3389/fams.2018.00050/full),
[ETS on matched equating samples](https://onlinelibrary.wiley.com/doi/full/10.1002/ets2.12313))

I do **not** recommend building an equating engine. I recommend the 20% that
delivers 80% of the value:

1. Let a blueprint mark a subset of items as **anchor items** that appear in
   every form of that level.
2. Track each anchor item's p-value per administration and **alert on drift** —
   an anchor item whose difficulty moves sharply between administrations is
   either compromised or context-sensitive, and either way the author must know.
3. Report honestly: where forms are not equated, the score report should say
   scores are comparable within a form, not across forms. Certification buyers
   respect a platform that says this; they do not respect one that pretends.

**What it unlocks.** Anchor-item drift is *the* cheapest live signal that items
have leaked — cheaper and far more legally comfortable than any proctoring
product. It also positions us for real equating later without repainting the
schema.

**Cost.** Low. An `IsAnchor` flag, per-administration statistics, a chart.

**Fit.** Fits, and interacts with R1: anchor items are by definition exempt from
LOFT's exposure ceiling, which the selection algorithm has to know.

---

### R7. Per-candidate accommodations, and WCAG 2.2 AA done properly

**What it is.** Two things joined deliberately, because doing only the second
produces an inaccessible exam that passes an audit.

WCAG 2.2 AA is the standard. The exam-specific trap is success criterion 2.2.1
(Timing Adjustable): users must be able to turn off, adjust or extend time
limits. A timed exam cannot honour that for everyone without ceasing to be a
timed exam — the criterion has exceptions, but the *practical* answer in
assessment is a **documented accommodation mechanism**: per-candidate extra time
(a multiplier), scheduled breaks, larger text, shuffle disabled, and item types
that degrade gracefully to a screen reader. Kryterion's accessible-delivery
description is a reasonable checklist for the delivery side: screen-reader
compatibility, 400% zoom without horizontal scrolling, full keyboard navigation,
configurable extended time and breaks. **[vendor]**
([WCAG 2.2](https://www.w3.org/TR/WCAG22/),
[Kryterion on accessible delivery](https://www.kryterion.com/blog/accessible-certification-exam-delivery-wcag-2-1-vpat-compliance/))

**Real unlock or checkbox? In our market, real.** Saudi Arabia's Digital
Government Authority publishes web accessibility guidelines based on WCAG for
government digital platforms, and the Saudi Web Accessibility policy is cited as
mandating WCAG 2.1 AA; Dubai's Digital Accessibility Policy (2020) enforces WCAG
2.1 AA for government entities. Public-sector procurement in exactly the
geography we target asks for this in writing. In the EU, the European
Accessibility Act became enforceable in June 2025 and the operative harmonised
standard is EN 301 549, which incorporates **WCAG 2.1 AA** — 2.2 is not yet
harmonised. So: build to 2.2 AA, but write "EN 301 549 / WCAG 2.1 AA" on the
compliance page, because that is the phrase on the buyer's checklist.
([Saudi DGA](https://dga.gov.sa/en/Web_Accessibility_of_Government_Websites),
[Saudi Web Accessibility policy](https://www.swa.gov.sa/en/digital-accessibility-policy),
[UAE design system accessibility](https://designsystem.gov.ae/guidelines/accessibility),
[Deque on MENA accessibility law](https://www.deque.com/mena-digital-accessibility-laws/) **[vendor]**,
[EAA guidance](https://www.levelaccess.com/compliance-overview/european-accessibility-act-eaa/) **[vendor]**)

**Cost.** Medium. The accommodations model is small — a per-candidate or
per-link accommodation profile the delivery engine reads. The WCAG work is a
sweep across 13 question types plus an audit; the RTL work already done means
the hard structural part (logical properties throughout) is behind us, and our
existing RTL-at-phone-viewport test suite is the natural harness to extend with
axe-core assertions.

**Fit.** Fits. `ExamLink` is the obvious carrier of an accommodation profile.

---

### R8. QTI import; export second; certification never (for now)

**What it is.** QTI 3.0 is the interchange format for items and tests — a data
model for `assessmentItem` and `assessmentTest`, with response processing,
multi-part test structures, timing and access controls, native CAT support,
integrated APIP accessibility features, and Portable Custom Interactions for
technology-enhanced items. Certification exists at Entry and Core profiles.
([QTI 3.0 overview](https://www.imsglobal.org/spec/qti/v3p0/oview),
[conformance](https://www.imsglobal.org/spec/qti/v3p0/conf),
[1EdTech's suggested RFP language](https://www.1edtech.org/standards/qti/rfp-procurement-agreements))

My recommendation is deliberately asymmetric:

- **Import is a migration weapon.** A university or awarding body with 4,000
  items in Surpass or TAO cannot switch to us if switching means retyping. A QTI
  2.x/3.0 importer that handles the item types we already support — and reports
  honestly on the ones it cannot — is the highest-leverage integration available
  to us, because it attacks the incumbent's lock-in directly. Our JSON payload
  model suits it: a QTI interaction maps to a payload shape plus a grader.
- **Export is a trust signal** ("your questions, exported on request" is already
  our stated position) but a smaller one, and cheaper once the import mapping
  exists.
- **Certification** is a badge that costs real engineering and that nobody in
  the current pipeline has asked for. Revisit when a ministry RFP names it —
  and note that 1EdTech actively publishes procurement language for
  institutions, so the phrase does appear in RFPs.

**LTI 1.3 / LTI Advantage** — LMS launch, deep linking, grade passback — is
genuinely useful for the training-academy shape and genuinely dead weight if no
tenant runs Moodle or Canvas. Build it against a named opportunity, not
speculatively.

**Cost.** Import: medium, bounded per item type. LTI 1.3: medium, mostly the
security model (JWT/JWKS, platform registration).

**Fit.** Fits well — the JSON payload abstraction is precisely the layer a QTI
mapper targets.

---

### R9. AI-assisted item authoring, gated by R4's review workflow

**What it is.** LLM drafting of items, entering the bank as **Draft**, never as
Live. Surpass already ships this as "Surpass Copilot" **[vendor]**.

The evidence is mixed and worth reading before promising anything. Positive: a
2025 study in the *International Journal of Selection and Assessment* comparing
AI-generated and human-authored items in employee selection found a significant
preference for LLM-generated items in some knowledge domains; LLM-generated
personality items matched expert items on internal consistency and convergent
validity against HEXACO scales. Negative, and consistent across the literature:
LLM-generated items can lack content validity, show linguistic or contextual
inaccuracies, and remain dependent on domain-specific prompting and expert
supervision.
([Kowal 2025](https://onlinelibrary.wiley.com/doi/10.1111/ijsa.70021),
[AIG for personality SJTs](https://arxiv.org/abs/2412.12144),
[Frontiers on instrumental quality of LLM-generated items](https://www.frontiersin.org/journals/education/articles/10.3389/feduc.2026.1837523/full))

**What it unlocks.** It answers the objection our own positioning creates. We
decline to ship a content library — correctly, see §3 — which means a new tenant
faces an empty bank, and "empty bank" is the commonest reason a trial dies. AI
drafting *from the tenant's own material* (their syllabus, manual, curriculum)
fills the bank with their content, which is the opposite of a shared library and
reinforces rather than contradicts our position.

**Cost.** Low to medium: a prompt, structured output into the existing JSON
payload, straight into Draft state. The gate is R4; without R4 this feature is
irresponsible.

**Fit.** Fits, and it is a reason to sequence R4 earlier than its own merits
alone would justify.

---

### R10. LLM as a *first rater* in the existing review queue — not as the marker

**What it is.** We already have a human review queue with rubrics. The upgrade is
to have a model pre-score against the rubric and present a suggestion plus
rationale to the human, tracking agreement over time.

The evidence, stated soberly. A 2025 synthesis of 65 studies (January 2022 –
August 2025) on LLM–human agreement in essay scoring found agreement "generally
moderate to good", with QWK / Pearson / Spearman "mostly ranging between 0.30
and 0.80" and substantial variability across studies. That range includes values
unusable for a high-stakes decision. Classroom deployments are mixed; one
191-student pilot found no significant correlation between AI and human scores.
([Research synthesis](https://www.researchgate.net/publication/397039628_Agreement_Between_Large_Language_Models_and_Human_Raters_in_Essay_Scoring_A_Research_Synthesis))

**The Arabic finding is the one that matters to us, and it is a caution.** On the
AR-AES dataset, a fine-tuned AraBERT model reached QWK 0.88 with 79.49% exact
match against human raters, while the strongest general LLM tested (ACEGPT)
reached QWK 0.67 — the *smaller, Arabic-specific* model beat the frontier
models. So the naive plan ("call a frontier model with the rubric, in Arabic")
is the weakest available option for our primary language.
([How well can LLMs grade essays in Arabic?](https://arxiv.org/abs/2501.16516),
[Arabic automated scoring literature review](https://arxiv.org/html/2606.09830))

**What it unlocks.** Throughput on the review queue — the operational bottleneck
for any tenant with writing sections — plus something worth more than speed:
**rater agreement statistics**. Double marking is the classical quality control
for subjective scoring, and Ofqual's own review found the move from single to
double marking produced "a very considerable reduction in inconsistency". If the
model is rater two, we can show a tenant how consistent their human markers are
— a report no hiring platform offers.
([Ofqual review of double marking](https://assets.publishing.service.gov.uk/media/5a82b3efed915d74e623734f/2014-02-14-review-of-double-marking-research.pdf),
[Ofqual on estimating inter-rater reliability](https://assets.publishing.service.gov.uk/media/5a7d9c18ed915d497af70733/2013-01-17-ca-estimation-of-inter-rater-reliability-report.pdf))

**Cost.** Low for the suggestion; medium for the agreement analytics.

**Fit.** Fits the existing queue exactly. Two hard rules: the score is never
final without human confirmation (EU AI Act Annex III human oversight, and plain
good sense), and we publish the agreement figure to the tenant rather than
hiding it.

---

## 3. What to decline

**Invasive proctoring — webcam monitoring, room scans, biometric identity
verification, gaze tracking.** Already our position; the evidence now supports it
as strategy rather than as a gap. *Ogletree* held room scans unreasonable under
the Fourth Amendment; a Thuringian court is reported to have found biometric
facial recognition of examinees contrary to Article 9 GDPR **[unverified]**;
consent from a job applicant or a student is structurally weak because of the
power imbalance; and under the EU AI Act proctoring and cheating detection are
explicitly Annex III high-risk. Building this would cost a year, put us in the
legal blast radius, and pit us against Proctorio and Honorlock. Decline — and
say why in the sales material, because it is a differentiator, not an omission.

**Automated AI-writing detection used to penalise a candidate.** This is the one
where I changed my mind mid-research, so the reasoning matters.

The received wisdom — seven detectors, a 61.3% average false-positive rate on 91
TOEFL essays by non-native writers, over 91% of those essays flagged by at least
one detector — is real, and is why institutions from Vanderbilt to UCLA disabled
Turnitin's detector; OpenAI withdrew its own classifier in July 2023 for low
accuracy.
([Liang et al. 2023](https://arxiv.org/abs/2304.02819),
[Vanderbilt](https://www.vanderbilt.edu/brightspace/2023/08/16/guidance-on-ai-detection-and-why-were-disabling-turnitins-ai-detector/))

But it is being revisited, and we should stop quoting the 2023 figure as if it
were current. A 2026 paper re-examining the claim in Czech reports that "the
perplexity of texts from non-native speakers of Czech is not lower than that of
native speakers", tests detectors from three separate families, finds "no
systematic bias against non-native speakers", and shows contemporary detectors
do not rely on perplexity at all. Separately, Pangram reports independent
evaluations — University of Chicago and Maryland researchers measuring a
1-in-10,000 false-positive rate, a 99.3% tie for first on the COLING 2025
benchmark, and a June 2026 Vrije Universiteit Brussel paper calling it the only
satisfactory tool of four tested. **[vendor]** — those figures are aggregated on
Pangram's own blog and I did not read the underlying papers.
([Revisiting the bias, 2026](https://arxiv.org/abs/2602.05769),
[Pangram third-party evals](https://www.pangram.com/blog/third-party-pangram-evals))

**We should still decline to ship it**, for three reasons that survive the
update. (1) There is no evidence for Arabic. The revisiting paper is Czech; the
original is English. Running an English-trained detector against Arabic
free-text answers from Arabic-L1 candidates is an experiment we would be
conducting on our customers' candidates. (2) An automated cheating flag is
high-risk under the EU AI Act, with the documentation burden that implies.
(3) The failure mode is asymmetric and unrecoverable: a wrongly accused
candidate loses a job or a place. R5's collusion statistics are strictly better
— they concern *this* exam's own data, they produce evidence a human can
inspect, and they require no claim about how the text was produced. Revisit if
somebody publishes a false-positive rate for Arabic academic writing.

**Full IRT-calibrated computerised adaptive testing.** CAT needs a calibrated
bank and large per-item samples, and it over-exposes high-information items —
the standard complaint in the literature is that CAT "pursues items with maximum
information", concentrating exposure exactly where it hurts. Our tenants have
small banks and small cohorts. LOFT (R1) gives unique forms without calibration;
multistage testing is the sensible later step if volume ever justifies it,
because MST matches CAT's classification accuracy with more efficient bank usage
and better exposure control. Decline CAT; keep MST on the someday list.
([ASC on MST](https://assess.com/multistage-testing/) **[vendor]**,
[MST vs CAT vs paper](https://files.eric.ed.gov/fulltext/EJ1111587.pdf))

**Automated speaking assessment.** Cambridge, Pearson and Duolingo have spent
years and large proprietary corpora on this; Duolingo's speaking scores come
from models trained on thousands of expert-rated samples with dedicated ASR and
speech processing. For Arabic, with its diglossia and dialect spread, this is a
research programme, not a feature. If a language-centre tenant needs speaking,
support **recorded response as a question type routed to the human review
queue** — which our architecture gives us for the cost of one grader class — and
let a human score it against a rubric.

**QTI certification, equating engines, and ISO 17024 consulting.** All three are
things to be *compatible with*, not things to *be*. ASC's own page notes that
for small certification bodies with no competitors, accreditation "is often not
worth the great expense" — equally true of us chasing the badge on their behalf.

**Reaffirmed from `competitive-position.md`:** the off-the-shelf test library and
competing on sandboxed code execution. Nothing found changes either. R9 is the
constructive answer to the empty-bank problem the first of those creates.

---

## 4. Where this contradicts what we already recorded

Four places, in descending order of how much they should change behaviour.

**1. "Every platform on the list is SaaS-only" — the list was incomplete.** TAO
Community Edition is free, open source, self-installable, QTI-certified, and was
relaunched in January 2026. Self-hosting is still a strong commercial position
against the SaaS vendors and still closes deals under Saudi PDPL, but it is not
a moat. Our answer against TAO must be Arabic-first delivery, the
domain-agnostic tenant vocabulary, and psychometrics surfaced for
non-psychometricians — not the deployment model.

**2. The "bank of roughly three times the form length" rule is not supported by
the source we cited for it.** I read
[assess.com/what-is-item-banking](https://assess.com/what-is-item-banking/) in
full. It says item performance must be tracked across forms, that over-exposure
reduces validity, and that items should be flagged for retirement or revision
after exposure to many examinees. It states no 3× multiple anywhere. The figure
may be sound folklore, but we are attributing it to a page that does not say it.
Two fixes: stop attributing it, and replace the fixed multiple with the metric
that actually governs — **exposure rate per item** (times served ÷ candidates),
with the ceiling a tenant setting. Under R1 the warning also changes character,
from advisory to an assembly failure naming the starved blueprint cell.

**3. "Certification platforms charge for [item statistics]" overstates our
edge.** Surpass advertises automated item- and test-level psychometric analysis
as a standard platform capability, and Questionmark's platform page markets
"identify poor performing questions" as core reporting. **[vendor]** on both.
The differentiator is not *having* discrimination indices — it is presenting
them to somebody who has never met a psychometrician, in Arabic, as the sentence
"these six questions are not measuring anything". Keep the feature; sharpen the
claim.

**4. "Every platform on the list is English-first" is true of the list and no
longer true of the market.** See §1. The defensible version is narrower and, I
believe, still correct: nobody is doing Arabic-first *item banking with
blueprints and psychometrics*. Say that instead.

---

## 5. Open questions for the product owner

1. **Which buyer do we sell to first?** R1+R5 (security without surveillance)
   wins recruiters and academies. R2+R3 (standard setting and framework
   reporting) wins language centres and certification bodies. Both are
   affordable; the ordering is a go-to-market decision, not an engineering one,
   and I cannot make it from the outside.
2. **Do we currently persist per-item responses and per-item elapsed time for
   every attempt?** R1, R4 and R5 all depend on it. If not, this is the first
   thing to build, before anything else in §2.
3. **What happens today when an author edits a live item that already has
   statistics?** If the answer is "the statistics carry over", we have a live
   correctness bug and R4 becomes urgent rather than merely valuable.
4. **Is there a named tenant who would pay for a documented Angoff study?** R2 is
   cheap either way, but the *report generator* — the auditable artefact — is
   only worth building against a real buyer.
5. **Do we intend to sell into the EU at all?** If yes, the EAA (June 2025) and
   the AI Act Annex III deadline (2 December 2027) become schedule items and R7
   moves up. If we are MENA-only, Saudi DGA/SWA and UAE accessibility rules still
   apply and R7 stays where it is, but the AI Act does not.
6. **What is our answer to TAO in a ministry procurement?** Worth rehearsing
   before a bid, not during one.
7. **Are we willing to publish a limitations page?** "Scores are comparable
   within a form, not across forms, unless equated." Certification buyers treat
   that sentence as a credential. It is also a commitment we would have to keep.

---

## Sources

**Psychometrics and certification**

- [Assessment Systems — Linear on the Fly Testing (LOFT)](https://assess.com/linear-on-the-fly-testing/) **[vendor]**
- [Assessment Systems — Modified-Angoff method](https://assess.com/modified-angoff-method/) **[vendor]**
- [Assessment Systems — ANSI ISO/IEC 17024 accreditation](https://assess.com/ansi-iso-17024/) **[vendor]**
- [Assessment Systems — What is item banking](https://assess.com/what-is-item-banking/) **[vendor]**
- [Assessment Systems — Multistage testing](https://assess.com/multistage-testing/) **[vendor]**
- [ISO/IEC 17024 catalogue entry](https://www.iso.org/standard/17024)
- [ICE — Standard setting overview for credentialing programs (PDF)](https://www.credentialingexcellence.org/Portals/0/Images/store/ICE%20Report_Standard%20Setting%20Overview%20for%20Credentialing%20Programs.pdf)
- [Questionmark — What is the Angoff method?](https://www.questionmark.com/resources/blog/what-is-the-angoff-method/) **[vendor]**
- [Questionmark — Standard setting: Bookmark method](https://www.questionmark.com/standard-setting-bookmark-method-overview/) **[vendor]**
- [Questionmark — Assessment platform](https://www.questionmark.com/platform/) **[vendor]**
- [Surpass Assessment](https://surpass.com/) **[vendor]**
- [Cirrus Assessment](https://www.cirrusassessment.com/) **[vendor]**
- [MST vs CAT vs paper-and-pencil comparison (ERIC)](https://files.eric.ed.gov/fulltext/EJ1111587.pdf)
- [Anchor design and parameter stability (Frontiers)](https://www.frontiersin.org/articles/10.3389/fams.2018.00050/full)
- [ETS — statistically matching equating samples](https://onlinelibrary.wiley.com/doi/full/10.1002/ets2.12313)

**Language testing**

- [Duolingo English Test — How is the DET scored?](https://blog.englishtest.duolingo.com/how-is-the-duolingo-english-test-scored/) **[vendor]**
- [Duolingo — Administration and scoring whitepaper (PDF)](https://duolingo-papers.s3.amazonaws.com/reports/Duolingo_whitepaper_test_scoring_2024_v1.pdf) **[vendor]**
- [Pearson Versant — sample score report (PDF)](https://www.pearson.com/content/dam/one-dot-com/one-dot-com/english/versant-test/sample-score-reports-2022/sample-score-report-versant-english-test.pdf) **[vendor]**
- [Pearson — Versant / GSE alignment (PDF)](https://www.pearson.com/content/dam/one-dot-com/one-dot-com/english/versant-test/GSE_Versant_English_Placement_est.pdf) **[vendor]**
- [Cambridge Linguaskill](https://pages.cambridgeenglish.org/linguaskill-2026) **[vendor]**
- [Council of Europe — Manual for relating examinations to the CEFR (PDF)](https://rm.coe.int/1680667a2d)
- [BanditCAT and AutoIRT — Duolingo research (arXiv)](https://arxiv.org/pdf/2410.21033)

**Integrity, proctoring and law**

- [Ogletree v. Cleveland State University — opinion (FindLaw)](https://caselaw.findlaw.com/court/us-dis-crt-n-d-ohi-eas-div/2109381.html)
- [EFF — Federal judge: online proctoring room scans are unconstitutional](https://www.eff.org/deeplinks/2022/08/federal-judge-invasive-online-proctoring-room-scans-are-also-unconstitutional)
- [Proctoring under the GDPR, incl. the Thuringian decision of 17 Nov 2025](https://2b-advice.com/en/2025/12/18/proctoring-in-the-application-process-data-protection-admissibility-under-the-gdpr/) **[unverified]**
- [EU AI Act Annex III — education, assessment and proctoring scope](https://www.praxikon.com/en/posts/high-risk-ai-education)
- [White & Case — EU agrees Digital Omnibus deal](https://www.whitecase.com/insight-alert/eu-agrees-digital-omnibus-deal-simplify-ai-rules)
- [DLA Piper — deferral of high-risk AI obligations to 2 Dec 2027](https://knowledge.dlapiper.com/dlapiperknowledge/globalemploymentlatestdevelopments/2026/The-Digital-AI-Omnibus-Proposed-deferral-of-high-risk-AI-obligations-under-the-AI-Act)
- [Gorney & Wollack — using response times in answer similarity analysis](https://journals.sagepub.com/doi/abs/10.3102/10769986241248770)
- [Trout & Gorney — weighted answer similarity analysis (2025)](https://journals.sagepub.com/doi/10.1177/01466216251322353)
- [Eckerly — answer similarity analysis at the group level](https://journals.sagepub.com/doi/abs/10.1177/01466216211013109)
- [Wise & Kong — Response Time Effort (PDF)](https://files.eric.ed.gov/fulltext/ED490203.pdf)
- [Detecting aberrant testing behaviour in unproctored CAT](https://dergipark.org.tr/en/pub/ijate/article/1598330)
- [Cizek & Wollack — Handbook of quantitative methods for detecting cheating on tests](https://www.routledge.com/Handbook-of-Quantitative-Methods-for-Detecting-Cheating-on-Tests/Cizek-Wollack/p/book/9781138821811)

**AI in assessment**

- [Liang et al. — GPT detectors are biased against non-native English writers](https://arxiv.org/abs/2304.02819)
- [Al Ali et al. 2026 — Revisiting the bias against non-native speakers in GPT detectors](https://arxiv.org/abs/2602.05769)
- [Vanderbilt — why we disabled Turnitin's AI detector](https://www.vanderbilt.edu/brightspace/2023/08/16/guidance-on-ai-detection-and-why-were-disabling-turnitins-ai-detector/)
- [Pangram — third-party evaluations](https://www.pangram.com/blog/third-party-pangram-evals) **[vendor]**
- [Agreement between LLMs and human raters in essay scoring — research synthesis](https://www.researchgate.net/publication/397039628_Agreement_Between_Large_Language_Models_and_Human_Raters_in_Essay_Scoring_A_Research_Synthesis)
- [Ghazawi & Simpson — How well can LLMs grade essays in Arabic?](https://arxiv.org/abs/2501.16516)
- [Automated scoring of Arabic text using LLMs — literature review](https://arxiv.org/html/2606.09830)
- [Kowal 2025 — AI-generated vs human-authored assessment items (IJSA)](https://onlinelibrary.wiley.com/doi/10.1111/ijsa.70021)
- [Automatic item generation for personality SJTs with LLMs](https://arxiv.org/abs/2412.12144)
- [Evaluating the instrumental quality of LLM-generated assessment items (Frontiers)](https://www.frontiersin.org/journals/education/articles/10.3389/feduc.2026.1837523/full)
- [Ofqual — Review of double marking research (PDF)](https://assets.publishing.service.gov.uk/media/5a82b3efed915d74e623734f/2014-02-14-review-of-double-marking-research.pdf)
- [Ofqual — Estimation of inter-rater reliability (PDF)](https://assets.publishing.service.gov.uk/media/5a7d9c18ed915d497af70733/2013-01-17-ca-estimation-of-inter-rater-reliability-report.pdf)

**Standards, accessibility and interoperability**

- [1EdTech — QTI 3.0 overview](https://www.imsglobal.org/spec/qti/v3p0/oview)
- [1EdTech — QTI v3 conformance and certification](https://www.imsglobal.org/spec/qti/v3p0/conf)
- [1EdTech — suggested QTI requirements for RFP and procurement](https://www.1edtech.org/standards/qti/rfp-procurement-agreements)
- [1EdTech — TAO certification record](https://site.imsglobal.org/certifications/open-assessment-technologies-sa/tao)
- [TAO — Community Edition launch](https://www.taotesting.com/blog/oat-launches-tao-community-edition/) **[vendor]**
- [TAO — products and tiers](https://www.taotesting.com/products/) **[vendor]**
- [W3C — WCAG 2.2](https://www.w3.org/TR/WCAG22/)
- [Kryterion — accessible certification exam delivery](https://www.kryterion.com/blog/accessible-certification-exam-delivery-wcag-2-1-vpat-compliance/) **[vendor]**
- [Level Access — European Accessibility Act overview](https://www.levelaccess.com/compliance-overview/european-accessibility-act-eaa/) **[vendor]**

**MENA market and regulation**

- [ETEC — National Center for Assessment (Qiyas)](https://etec.gov.sa/en/centers/qiyas)
- [ETEC — accrediting body profile](https://accreditation.org/accreditation-processes/accrediting-bodies/education-and-training-evaluation-commission)
- [Saudi Digital Government Authority — web accessibility of government websites](https://dga.gov.sa/en/Web_Accessibility_of_Government_Websites)
- [Saudi Web Accessibility — accessibility policy for digital platforms and services](https://www.swa.gov.sa/en/digital-accessibility-policy)
- [UAE Design System — accessibility guideline](https://designsystem.gov.ae/guidelines/accessibility)
- [Deque — digital accessibility laws across MENA](https://www.deque.com/mena-digital-accessibility-laws/) **[vendor]**
- [ITIF — Saudi Arabia's cross-border data transfer regulation](https://itif.org/publications/2025/06/09/saudi-arabia-cross-border-data-transfer-regulation/)
- [Clyde & Co — SDAIA risk assessment guidelines, February 2025](https://www.clydeco.com/en/insights/2025/03/update-on-saudi-arabia-risk-assessment-guidelines)
- [King & Spalding — international personal data transfers under KSA PDPL](https://www.kslaw.com/news-and-insights/international-personal-data-transfers-under-saudi-arabias-data-protection-law)
- [Baker McKenzie — Egypt data protection update](https://www.bakermckenzie.com/en/insight/publications/2026/01/egypt-important-data-protection-update)
- [Kennedys — Egypt's PDPL compliance countdown](https://www.kennedyslaw.com/en/thought-leadership/article/2026/egypt-s-personal-data-protection-law-the-compliance-countdown-has-begun/)
- [Evalufy vs TestGorilla — Arabic UI, pricing and support for KSA/UAE](https://www.evalufy.com/blog/candidate-assessment-selection/evalufy-vs-testgorilla-ksa-uae-arabic-ui-pricing-support/) **[vendor]**
- [Top bilingual assessment platforms for MENA](https://www.evalufy.com/blog/video-interviewing-assessments/top-bilingual-assessment-platforms-arabic-english/) **[vendor]**
