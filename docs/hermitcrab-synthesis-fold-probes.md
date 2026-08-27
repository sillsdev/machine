# Synthesis-fold probes: plan

Branch: `feature/synthesis-fold-probes`, off `integrate-conformance-framework` (`c0ac5c9f`).

Deliberately **not** off master or `feature/memoization`. The conformance branch carries 33
committed grammars — 8 typologically distinct languages (fusional-realizational, metathesis,
polysynthetic-stratal, prefixal-discontinuous, suffixing-evidential, suffixing-extension-slot,
suffixing-vowel-harmony, templatic-root-modification) plus 25 edge cases — each with hand-derived
expected outputs and a `Fixture.DiscoverAll` enumerator. Every prior conclusion in this area was
drawn from three grammars, and the last one died because a result that looked general was
Sena-shaped. Breadth is the point of this base.

Predecessor: `feature/forest-memo`, where the proposal to narrow `AnalysisStateKey` was measured
and stopped. Its three docs (`hermitcrab-packed-forest-research.md`,
`hermitcrab-forest-memo-ceiling.md`, `hermitcrab-forest-memo-plan.md`) hold the evidence base and
are ported here unchanged. Read them first; nothing below re-derives them.

---

## 1. The reframing this plan tests

Every previous attempt to share synthesis work assumed sharing means **merging by key**, which
requires order-invariance — and HermitCrab's morphological rules provably lack it (2 violations
on Sena, 12 independently on Indonesian).

But forward synthesis is not order-sensitive search. It is a **deterministic fold driven by the
trail**. Verified in code:

- `Word.IsMorphologicalRuleApplicable` (`Word.cs:248`) admits **only**
  `_mruleApps[_mruleAppIndex]`; `MorphologicalRuleApplied` decrements the index. Synthesis walks
  the trail from its end backwards, branching only on allomorph choice.
- The **end** of `_mruleApps` is the last-unapplied rule — the deepest, the one applied **first**
  in synthesis. `ReplayOnto`'s `mruleTrailPrefixLength` splits the trail at exactly that
  boundary. So **the analysis memo has already computed which candidates share a synthesis-first
  segment**, and that segment is currently re-folded from scratch for every path, every root,
  every alternative.

Two ways to share a deterministic fold, neither of which assumes commutativity:

1. **By guaranteed-identical subsequence** — the shared suffix above, already identified for free.
2. **By computed value** — if the extension step is a function of the partial's full value plus
   the next trail rule, then two permuted segments that *in fact* produced the same value merge
   automatically, and the rare genuine non-commutative pairs produce different values and stay
   separate.

Order-invariance stops being an assumption and becomes something detected. That is the whole
idea, and P1c is the measurement that decides whether it is worth anything.

### Two traps already found in the code

- **`Word.ValueEquals` (`Word.cs:600`) is not a valid synthesis fingerprint.** It compares shape,
  realizational FS, non-heads, stratum, root allomorph, trail, index, and the final-rule flag —
  and **omits `_syntacticFS`, MPR features, and disjunctive allomorph indices**, all of which
  synthesis reads. A `SynthesisStateKey` needs its own key-completeness audit against every
  `Synthesis*.cs`, exactly as `AnalysisStateKey` has one.
- **Realizational rules are trail-exempt.** `SynthesisRealizationalAffixProcessRule` has no
  `IsMorphologicalRuleApplicable` gate (contrast `SynthesisAffixProcessRule.cs:43`); it gates on
  `RealizationalFeatureStruct.Subsumes` plus `IsBlocked`. They branch *inside* a shared segment,
  so any stored partial must be a set, like `MemoEntry.Results`, not a value.

### One clean negative, recorded so it is not re-attempted

Maxwell & Kaplan's biggest measured win came from modifying the grammar so the chart prunes what
the constraint solver otherwise would. **That cannot transfer here.** Everything synthesis
rejects on is root- or realization-dependent, and the root is unknown until `LexicalLookup`. On
top of that, a Sena analysis state costs ~0.73 ms (template battery) while a synthesis input
costs at most ~0.12 ms — **states are dearer than synthesis inputs**, and that conclusion gets
stronger, not weaker, if synthesis turns out to be less than all of the runtime. The chart's
analogue of category-splitting here is the interface, not the key.

---

## 2. Standing rules

1. **Search completeness is never reduced.** HermitCrab is the permanent fallback engine behind
   the FST work.
2. **Acceptance is analysis-set equality**, never byte or object equality. Canonical
   morpheme-signature sets, sorted.
3. **Breadth before depth.** Every number is reported across all 33 conformance fixtures *and*
   Sena/Indonesian/Amharic. A result that holds only on Sena is not a result — that is exactly
   how the key-narrowing work went wrong.
4. **Gates are written before the run**, and a missed gate is reported as a finding, not
   renegotiated.
5. **No pooled averages across fixtures of wildly different size.** Report per-fixture, plus a
   distribution. The T1 tandem probe had to retract a pooled average; the forest-memo Amharic
   number nearly repeated it.
6. **Real grammars (Sena/Indonesian/Amharic) are never committed.** Conformance fixtures are
   synthetic and committed; corpus grammars stay in `.git/info/exclude`.

---

## 3. P1 — the synthesis instrumentation triple

One harness, three numbers. All three instrument the same loop in `Morpher.Synthesize`, so they
are gathered in a single pass rather than three.

### P1a — the wall-time split

Per word: time in the morphological-rule cascade, the affix-template battery, `LexicalLookup`,
and forward synthesis (`_synthesisRule.Apply` + `IsWordValid` + `IsMatch`).

**This is a gap in the predecessor's own work.** The forest-memo Stage-0 plan listed this row as
required and never delivered it, and every ceiling in this plan divides by it. It is cheap and it
comes first.

No gate — it is a precondition for interpreting P1b and P1c.

### P1b — the die-point histogram

`cinacemerwa` sends 218,847 candidates into forward synthesis and returns 0 parses. We have never
asked *why* they die. For each rejected candidate, record which check killed it:

| die point | where |
| --- | --- |
| lexical lookup miss | `Morpher.LexicalLookup` yields nothing |
| synthesis-side application count | `SynthesisAffixProcessRule.cs:46` |
| morphological rule not applicable / pattern match failure | `SynthesisAffixProcessRule` |
| allomorph environment | allomorph `Environments` check |
| realizational subsumption / blocking | `SynthesisRealizationalAffixProcessRule` |
| feature unification | `RequiredSyntacticFeatureStruct.Unify` chain |
| MPR features | required/excluded MPR check |
| `IsWordValid` | co-occurrence, obligatory features |
| final surface mismatch | `Morpher.IsMatch` |

**Gate:** if a single die point accounts for **≥40%** of rejections on the Sena heavy words *and*
is decidable from information available before the synthesis cascade runs, build that prefilter.
Report the histogram for all 33 fixtures regardless — the shape of the distribution across
typologies is itself the finding.

### P1c — the fold-step fingerprint ratio (the headline)

Count total synthesis rule applications against distinct `(fingerprint, applied rule)` pairs,
where the fingerprint covers everything a synthesis step reads: shape+annotations, syntactic FS,
realizational FS, MPR set, root allomorph, disjunctive allomorph indices, application counts,
`IsPartial`, `IsLastAppliedRuleFinal`, stratum, and pending-trail position. **Do not use
`Word.ValueEquals`** — see the trap above.

Also assert a **determinism check**: equal fingerprint plus equal applied rule must never yield
different outcomes. A violation means the fingerprint is incomplete and is the single most
important thing this probe can find.

**Gate:** ratio **≥5x** on `cinacemerwa` and `kukucitirani` → build suffix-anchored synthesis
sharing. **<2x** → the idea is dead, fact 5's pessimism was right, and we say so.
Between 2x and 5x → report and decide with the P1a split in hand.

---

## 4. Later probes, in order, each gated on the last

- **P2 — incremental surface-length pruning.** Gate A resurrected at the layer where it is sound:
  it died because at its comparison point the candidate was still the bare root with the trail
  unapplied; *inside* the fold the partial is real and the pending trail is known.
  `RuleLengthClassifier` (built on the predecessor branch, currently orphaned) supplies the
  min-insertion side. The only proposal that can touch Amharic's ~160 ms-per-run problem.
  Gated on P1b: build only if the histogram says candidates die somewhere a length window can see.
- **P3 — corpus-scope memoization.** `AnalysisScope` dies with each word, but the key is
  word-independent by construction. Least clever, lowest risk, aimed at the number users feel —
  the Amharic corpus run took 4.3 hours. Independent of P1; can run any time.
- **P4 — nogood lattice subsumption.** Lowest priority: if shape+FS discriminate almost perfectly
  (which the forest-memo measurement says they do), the subsumption buckets have one member and
  this never fires. Probe is a counting exercise; gate ≥30% subsumable.

---

## 5. Execution

One probe at a time. Each probe is implemented by a subagent against a hardened brief, then its
claims are verified independently before anything is believed. New branch per probe where the
work is separable. A Fable review after the first probe lands, then continue down the list.

Results land in section 6 of this file as they arrive.

---

## 6. Results

### 6.1 P1 — conformance breadth (33 fixtures)

Full HermitCrab suite on this branch with the probe present: **582 passed, 1 skipped, 0 failed**,
including every conformance fixture gate. The instrumentation is behaviour-neutral: all edits are
insertions before existing `return`/`continue` statements, gated on a single
`volatile bool SynthesisProbe.Enabled` that is false in normal operation.

**Determinism violations across all 33 fixtures: 0.** Equal fingerprint plus equal applied rule
never produced a different outcome multiset, across 8 typologies and 25 edge cases. This is the
check that would have exposed an incomplete fingerprint, and it is clean — which is what licenses
reading the P1c ratios below as real sharing rather than as collisions.

**P1c fold-step sharing ratio, by fixture** (never pooled — sizes differ by three orders of
magnitude):

| fixture | applications | distinct | ratio |
| --- | --- | --- | --- |
| languages/suffixing-evidential-adjacency-chain | 640 | 79 | **8.10x** |
| edge-cases/strrep-identity | 67 | 17 | 3.94x |
| edge-cases/deep-optional-affix-nesting | 5,556 | 1,727 | **3.22x** |
| edge-cases/diacritic-segments | 48 | 16 | 3.00x |
| edge-cases/disjunctive-recheck | 12 | 4 | 3.00x |
| languages/suffixing-vowel-harmony | 45 | 16 | 2.81x |
| languages/suffixing-extension-slot-ordering | 88 | 41 | 2.15x |
| languages/templatic-root-modification | 27 | 14 | 1.93x |
| edge-cases/morphotactic-attribute-breadth | 131 | 83 | 1.58x |
| languages/fusional-realizational-morphology | 59 | 40 | 1.48x |
| languages/metathesis-phase-isolation | 10 | 9 | 1.11x |
| languages/polysynthetic-stratal-derivation-chain | 5 | 5 | 1.00x |
| edge-cases/mpr-overwrite-order-dependence | 19 | 19 | 1.00x |

**Fold sharing is real and strongly typology-dependent.** Suffixing/agglutinative chains share
heavily; metathesis and MPR-order-dependent grammars share nothing. That split is a sanity check
in itself: the fixture literally built to be order-dependent
(`mpr-overwrite-order-dependence`) reports exactly 1.00x, and the metathesis fixture 1.11x, while
a suffix chain reports 8.10x. The measurement discriminates in the direction the mechanism
predicts.

This is precisely the information the predecessor branch lacked. Key narrowing looked general and
was Sena-shaped; fold sharing is *not* general either, but here we know the shape of the
dependence before building anything.

**P1b die-point histogram** — consistent across unrelated typologies:
`RuleNotApplicableOrPatternMismatch` 72–73%, `LexicalLookupMiss` 22–25%, everything else in the
single digits.

Two cautions on reading it:
- These are rejection **events**, not distinct candidates — one candidate branches into many
  internal attempts, each able to die at a different check. Documented in `SynthesisProbe`. It is
  **not** the same denominator as the historical 218,847 figure, which counts candidates
  *entering* synthesis (one per `ExpandAlternatives` output). The two numbers must not be
  compared.
- `RuleNotApplicableOrPatternMismatch` clears the 40% gate on count, but each such rejection is an
  O(1) trail-position check (`IsMorphologicalRuleApplicable` is a list index plus a reference
  compare). A count histogram overstates its cost share. **Cost-weighting is required before this
  becomes a build decision** — see 6.3.

**P1a wall-time split** is unreliable on these fixtures: most words run in well under 2 ms, where
`Stopwatch` overhead and JIT warm-up swamp the signal, and the four buckets frequently sum to well
under half of wall time. The one large-enough fixture is informative:
`deep-optional-affix-nesting` at 2,393 ms with **battery = 67.4%**, forward synthesis 6.2% —
matching the historical Sena finding that the affix-template battery dominates. Treat the split as
meaningful only on the real corpora.

### 6.2 P1 — Sena, and a correction to how these numbers must be read

| word | wall | successful apps | new distinct |
| --- | --- | --- | --- |
| `atawirambo` | 19,987 ms | 268 | 32 |
| `kukucitirani` | 89,679 ms | 39,270 | 112 |
| `cinacemerwa` | 61,672 ms | 2,149 | 13 |
| **total** | 171,338 ms | **41,687** | **157** |

**P1c = 265.5x. Determinism violations: 0.** The gate was ≥5x.

And on Sena that does not matter, because of the split:

`lookup 5.68 ms (0.0%) · synthesis cascade 2,629 ms (1.5%) · synthesis battery 5,677 ms (3.3%) ·
forward synthesis 328 ms (0.2%)` — **four buckets totalling 5.0% of wall time.**

Two separate findings are tangled here and must be kept apart.

**(i) A measurement defect.** All four timers landed on the *synthesis* side —
`AddCascadeTicks`/`AddTemplateBatteryTicks` are called from `SynthesisStratumRule.cs:107`/`:136`,
and `AnalysisStratumRule`/`MemoizedCombinationRuleCascade` were never instrumented. That is an
ambiguity in the brief: both a "morphological-rule cascade" and an "affix-template battery" exist
on each side. Being fixed; the `unaccounted` column is the deliverable.

**(ii) A real result about Sena.** Even granting the defect, forward synthesis plus lexical lookup
plus the synthesis-side cascade and battery are **5% of Sena heavy-word time**. The 218,847
synthesis inputs on `cinacemerwa` are real but cost ~0.5 µs each. Counting candidates told us
where the *volume* was and never where the *time* was. This is exactly the row the predecessor
plan required and the predecessor branch skipped, and skipping it cost two rounds of analysis
built on a wrong denominator.

Corroborating: **11,445,538 rejection events, 100.0% `RuleNotApplicableOrPatternMismatch`**,
against 41,687 successful applications — 274 wasted rule attempts per real one, at ~29 ns each.
Real waste, and the "~40x free" trail-position observation from
`docs/hermitcrab-parse-algorithm-analysis.md` reconfirmed at scale — but at 29 ns it is not 95% of
anything.

#### The correction to the metric

Sena being analysis-bound is a fact about Sena, not a verdict on the technique. HermitCrab runs on
a very large number of languages; a technique inert on two grammars and worth 5x on a third is a
useful technique. **The quantity that decides value, per grammar, is:**

> **value = P1c sharing ratio × forward-synthesis share of wall time**

We have the first across all 33 fixtures (1.00x–8.10x). We have the second only for Sena, where it
is ~0.2%. **No conclusion about any other grammar is licensed until the second factor is measured
per grammar.** Neither factor alone decides anything: high sharing in a phase that costs nothing
is worthless, and an expensive synthesis phase with no sharing is unimprovable by this route.

#### Why Amharic is the priority

From the predecessor branch: `ሄዶ` has **212 analysis states and 186 synthesis inputs, and takes
30 seconds** — roughly **160 ms per synthesis run**, against Sena's ~0.5 µs per synthesis input.
Five orders of magnitude apart per unit of synthesis. If Amharic's wall time sits in forward
synthesis, it is the grammar where fold sharing pays and Sena is the outlier rather than the rule.
Templatic/Semitic morphology is not a niche. This is the measurement that matters most next.

### 6.3 P1a fixed — where the time actually goes

Six exclusive slices now sum to wall. **Amharic `unaccounted` = 0.1%**, so the split is
trustworthy.

Amharic, 28 words: `anTotal` **99.5%** · lookup 0.1% · synCascade 0.1% · synBattery 0.1% ·
synForward **0.1%** · unaccounted 0.1%.

| word | wall | anCascade | anBattery | anPhono | synForward |
| --- | --- | --- | --- | --- | --- |
| `ሄዳችሁ` | 38,298 ms | **34,635** | 3,505 | 8.5 | 41 |
| `ሄዶ` | 37,953 ms | **36,154** | 1,682 | 0.8 | **14** |
| `ሁለተኛ` | 16,486 ms | **14,122** | 2,249 | 44 | 18 |

**This refutes a claim made earlier in this document.** Section 6.2 argued `ሄዶ` was ~160 ms per
synthesis run and therefore the likely synthesis-bound grammar. Its forward synthesis is **14 ms**.
The 160 ms came from dividing 30 s by 186 synthesis inputs — arithmetic on an unmeasured
denominator, the same error that produced the 218,847 framing. Amharic's 36 seconds are in the
**analysis morphological-rule cascade**, for a word with 212 distinct states: ~170 ms per state, in
a cascade already memoized and already at its state floor. Not state count, not the template
battery (4%), not phonology (0.002%), not synthesis (0.04%). What is expensive is what happens
*inside* the cascade per node — pattern matching across the rule set.

### 6.4 The answer to "does any grammar benefit?" — CORRECTED

> **The ceiling table first published in this section was arithmetically wrong and has been
> removed.** It divided by the wrong share. Found by adversarial review, confirmed in code.

**The error.** `synForward` is explicitly *net* of the cascade and battery buckets —
`Morpher.cs:424` records `forwardTotal - cascadeDelta - batteryDelta`. But every application P1c
counts is recorded inside `SynthesisAffixProcessRule.Apply` /
`SynthesisRealizationalAffixProcessRule.Apply`, which run **inside** the `synCascade` and
`synBattery` brackets (template slot rules compile to those same classes via `RuleBatch`,
`SynthesisAffixTemplateRule.cs:20-24`). So the shareable work lives in
`synCascade + synBattery + synForward`, and the table divided by `synForward` alone — the one
bucket that excludes it.

**Corrected ceilings** (`share x (1 - 1/ratio)`, share = synCascade + synBattery + synForward):

| grammar | ratio | corrected share | max possible speedup |
| --- | --- | --- | --- |
| **Sena** | 265x | **5.0%** | **~4.98%** (was reported as 0.2% — understated 25x) |
| **Amharic** | 2.15x | **0.3%** | **~0.16%** |

**The fixture ceiling column is withdrawn entirely**, for two reasons: the harness only emitted
`forwardShare`, so correcting it needs a re-run; and section 6.1 already declared fixture timings
unreliable at sub-2 ms scale, which makes the former "best anywhere 22.5%" headline
self-contradictory by this document's own standard. Do not quote it.

**Determinism violations across every run: 0 — but that proves less than previously claimed here.**
`RecordApplications` returns early when `outputs.Count == 0`, so a `(fingerprint, rule)` pair that
succeeds in one occurrence and fails in another is never compared; any omitted state that only
flips match to no-match is invisible to the check. And outcomes are compared with the same
`FingerprintEquals` used to key them, which covers pending-trail *position* but not remaining-trail
*content*. Zero violations licenses **per-step decision determinism**. It does not license "the
sharing is sound": memoized output `Word`s embed trails, so a real build needs delta-storage or
`ReplayOnto`-style re-anchoring. This makes a build harder, not easier.

#### Reading this honestly

Two competing explanations for why fixtures show high synthesis share and real grammars show
~0.3-5%:

1. **Typology.** Some morphological types are genuinely synthesis-heavy.
2. **Grammar size.** Analysis cost scales far worse with rule count and lexicon size than
   synthesis does.

The doc previously asserted (2), on the grounds that Sena is agglutinative and "still 0.2%". **That
premise used the wrong number** — Sena is ~5%. The size argument is now *directionally plausible
but not established*: the large end of the trend is a single fixture whose size comes from an
analysis-side stressor by construction, so size and analysis-pathology are confounded, and most
fixture ratios rest on 12-131 applications.

There is a concrete mechanism by which a synthesis-bound grammar could exist, and it is in this
document's own trap #2: **realizational rules are trail-exempt**, so their branching is not bounded
by the analysis trail and synthesis work can scale with paradigm size independently of analysis.
The family that would show this — large position-class fusional systems with many realizational
rules per slot and heavy blocking — is represented by none of Sena, Amharic, Indonesian, or any
sub-2 ms fixture. Probe F1 below is designed to settle it.

#### Verdict on the fold-sharing build — scoped

**For parsing workloads: do not build it, provisionally.** Amharic's ~0.16% ceiling is robust
under every attack found. **Sena's ~4.98% is provisional on probe N1** — the 20.1% unaccounted
could move it.

**This verdict does not cover generation.** `Morpher.GenerateWords` (`Morpher.cs:245-254, 805`) is
pure synthesis with no analysis phase — share ~100%, so the P1c ratio applies at face value.
Nothing in this round measured it.

#### What replaces it

**The analysis phase is the target on every real grammar — but which part of it differs by
grammar.** Sena re-run with the fixed timers (143,303 ms over the three heavy words):

| bucket | pooled % of Sena wall |
| --- | --- |
| **anBattery** (analysis affix-template battery) | **51.4%** |
| anCascade (analysis mrule cascade) | 18.4% |
| anOther (anTotal residual) | 5.1% |
| anPhono | 0.0% |
| synCascade + synBattery + synForward + lookup | ~5.0% |
| unaccounted | 20.1% |

Against Amharic, where `anCascade` is ~95% of `anTotal` and `anBattery` only 4%. **So there is no
single hot spot across grammars — only a single hot *phase*.** Sena is template-battery-bound;
Amharic is cascade-bound; neither is synthesis-bound. An earlier version of this section claimed
the cascade was "the target" on the strength of Amharic alone. That was the Sena-shaped error in
reverse, caught within one run.

Two things worth carrying forward:

- **The template battery is still 51.4% of Sena after being memoized.** Phase 3b measured it at
  93% pre-memo and its memo bought a 5x. It remains the largest single bucket. The memo reduced
  how often the battery runs; it did not reduce what a run costs.
- **The common thread is per-node cost, not node count.** Amharic spends ~170 ms per analysis
  state in a cascade already at its state floor. Every optimization attempted in this area —
  memoization, key narrowing, lexical gating, tandem intersection — has reduced *how many* nodes
  are visited. **None has touched what a node costs.** That is the unexplored axis.

#### One honest gap

Sena's `unaccounted` is **20.1%** (24.0% on `cinacemerwa`, 7.6% on `atawirambo`) — Amharic's is
0.1%, so this is Sena-specific, not a broken bracket. The grounded hypothesis is
`Word.ExpandAlternatives()` (`Word.cs:470`), called per synthesis word in `SynthesizeSequential`
outside every timed region, doing `Clone`/`Unify`/`Subtract`/`Freeze` work per call. It scales
with the number of analysis/alternative pairs, which fits the per-word spread. **This is a
hypothesis, not a measurement** — one more bracket would settle it, and it should be settled
before anyone quotes the Sena split as complete.

### 6.5 N1 — the gap is `ExpandAlternatives`, confirmed; a second, larger build candidate

**Hypothesis confirmed.** With `synExpand` broken out as its own exclusive bracket around
`Word.ExpandAlternatives()` in `Morpher.SynthesizeSequential` (the only call site this harness
reaches -- the harness always calls `ParseWord` with `guessRoot: false`, so the `guessRoot`
`ExpandAlternatives` call at the time was never in scope), Sena's `unaccounted` collapses from
20.1% to **1.5% pooled**, and per word: `atawirambo` 7.6% → **0.77%**, `kukucitirani` → **1.19%**,
`cinacemerwa` 24.0% → **2.01%**. All comfortably under the 5% gate. `synExpand` itself absorbs
almost exactly what `unaccounted` lost: **20.3% of wall pooled** (7.9% / 17.5% / 26.4% per word).
Determinism violations: 0. Full suite reconfirmed green (582/1/0) with the new brackets in place.

| word | wall | synExpand | unaccounted (old → new) |
| --- | --- | --- | --- |
| `atawirambo` | 12,101 ms | 950 ms (7.9%) | 7.6% → **0.77%** |
| `kukucitirani` | 51,872 ms | 9,087 ms (17.5%) | (n/a) → **1.19%** |
| `cinacemerwa` | 48,740 ms | 12,877 ms (26.4%) | 24.0% → **2.01%** |
| **pooled** | 112,713 ms | 22,914 ms (20.3%) | 20.1% → **1.5%** |

**This does not flip the 6.4 fold-sharing verdict for parsing.** `ExpandAlternatives` runs before
`_synthesisRule.Apply`, i.e. outside the fold P1c measures -- the fold-step ratio recomputed on
this same run is **265.52x**, matching the earlier 265.5x, and `synCascade + synBattery +
synForward` is still ~5% of wall (6,017 ms / 112,713 ms = 5.34% pooled, ceiling `5.34% x (1 -
1/265.52)` = **5.32%**, consistent with 6.4's 4.98%). The do-not-build verdict for the fold-step
build stands, unchanged, for both grammars.

**But the dedupe census at fold entry finds a second, distinct, and larger opportunity.** For
every `Word` `ExpandAlternatives()` produces, right before it enters `_synthesisRule.Apply`:

| word | alternatives | distinct | distinct/total | dupe: same analysis word | dupe: different |
| --- | --- | --- | --- | --- | --- |
| `atawirambo` | 17,699 | 12 | 0.07% | 17,380 (**98.3%**) | 307 (1.7%) |
| `kukucitirani` | 158,480 | 18 | 0.01% | 158,462 (**100%**) | 0 (0%) |
| `cinacemerwa` | 218,847 | 31 | 0.01% | 159,747 (**73.0%**) | 59,069 (27.0%) |
| **pooled** | 395,026 | 61 | **0.02%** | 335,589 (**85.0%**) | 59,376 (15.0%) |

**Gate: ON.** `distinct/total` (0.02% pooled, worst case 0.07%) is far under the 0.2 threshold,
and the same-analysis-word share of duplicates is the majority on every word individually (73.0%
- 100%) as well as pooled (85.0%) -- the OFF condition (duplicates predominantly cross-word) never
fires anywhere in this data. Most of the redundancy `ExpandAlternatives` re-does (`Clone` +
`Unify` + `Subtract` + `Freeze`, per Word.cs:470) is re-deriving an alternative already produced
earlier in the *same* analysis word's expansion, which means it is interceptable with a
per-analysis-word fingerprint cache checked before that work runs, not just after.

Ceiling if the same-word share were fully intercepted (`synExpand` share x same-word fraction of
alternatives, as a first-order estimate assuming roughly uniform per-alternative cost): pooled
20.3% x 85.0% ≈ **17.3% of Sena wall time** -- an order of magnitude above the fold-step build's
~5% ceiling, and a real, newly-identified target. `cinacemerwa`'s 27% cross-word remainder is the
honest caveat: even a perfect pre-expansion dedupe leaves a residual only a post-expansion
fingerprint (the existing P1c one) could catch, and that residual is real (59,069 events on this
one word alone).

**This is a measurement, not a build.** N1's brief was to settle the hypothesis and census, not to
implement the cache; it is reported here as a scoped, gated, next candidate distinct from the
fold-step build 6.4 already closed.

### 6.6 Follow-on notes

- **Cost-weight P1b.** Count is not cost. Attribute wall time, not events, to each die point.
- **The trail-position finding needs its own look.** If `RuleNotApplicableOrPatternMismatch` is
  dominated by the synthesis cascade trying every rule at each node when the trail dictates exactly
  one pending rule, that is the "~40x free" observation already recorded in
  `docs/hermitcrab-parse-algorithm-analysis.md` (complexity-cap branch), independently
  reconfirmed here across two typologies. Indexing synthesis rules by trail position is a much
  smaller change than anything else in this plan. Cheap to measure, cheap to build.


---

## 7. N1 correction — the census ratio repeats a retracted measurement

The N1 hypothesis result stands and is valuable. The N1 *census* conclusion does not, and the
"build is ON, ceiling 17.3%" line in section 6.5 must not be cited.

### What stands

`unaccounted` 20.1% -> **1.5%** pooled once `synExpand` is broken out, which absorbed 20.3%.
**`Word.ExpandAlternatives` is 20.3% of Sena wall time**, invisible to every prior round of
instrumentation. Census denominators cross-check exactly against an independent earlier run
(`cinacemerwa` 218,847, `atawirambo` 17,699, `kukucitirani` 158,480). Determinism violations: 0.

### What does not

The census reports 395,026 alternatives collapsing to **61 distinct** (0.02%), giving a claimed
~17.3% ceiling. That ratio is measured with the P1c fingerprint, which carries
`PendingTrailPosition` — an integer index — and **no remaining-trail content**
(`FingerprintHash`/`FingerprintEquals`, verified). For a fold *step* that is defensible because
the continuation is re-anchored. For **fold-entry dedupe**, which is what the census measures,
skipping an alternative because another shares its fingerprint discards its distinct
continuation: lost parses. Completeness is the one thing that is never negotiable here.

**This exact measurement has been made before**, at the same `ExpandAlternatives` call sites, with
the same `cinacemerwa` denominator of 218,847, on branch `parse-forest-tandem`:

| probe | key | ratio |
| --- | --- | --- |
| F1 v1 | naive | **9,774x** — proven unsound, 4/30 groups produced different outputs |
| F1 v2 | + pending-rule multiset | 28.72x — 2 residual violations, genuine non-commutativity |
| F2 as shipped | fully order-sound | **15–40% call reduction** |

The 6,476x is the 9,774x again. The adversarial review predicted this failure mode by name one
step earlier — trail position without trail content — and the probe used that fingerprint anyway.

### Corrected ceiling

**20.3% x (15–40% sound dedupe) ≈ 3–8% of Sena wall time.**

Still the largest single opportunity this effort has surfaced, and on an axis nobody had
instrumented. But a fifth of the claimed figure, and it needs a trail-complete key before any
build. The same-analysis-word share (85% pooled) is the encouraging part: alternatives from one
analysis word share a trail, so for *those* the existing fingerprint may already be adequate —
that, not the headline ratio, is the thing worth re-measuring with a sound key.


---

## 8. The corrected per-fixture table — the size argument is refuted

Re-run with the correct denominator (`synTotalShare = (synCascade + synBattery + synForward) / wall`)
and a reliability flag (`wallMs >= 50`). **This overturns section 6.4's size-vs-typology argument.**

Reliable rows only (`wallMs >= 50`), sorted by fold-step ceiling:

| fixture | ratio | synTotalShare | **foldStepCeiling** | wallMs | words |
| --- | --- | --- | --- | --- | --- |
| edge-cases/deep-optional-affix-nesting | 3.22x | **73.1%** | **50.4%** | 2,839 | 3 |
| languages/suffixing-evidential-adjacency-chain | 8.10x | **59.3%** | **52.0%** | 107 | 28 |
| languages/suffixing-extension-slot-ordering | 2.15x | 24.7% | 13.2% | 69 | 53 |
| languages/fusional-realizational-morphology | 1.48x | 14.3% | 4.6% | 93 | 63 |

**Two reliable fixtures show a ~50% wall-clock ceiling — a 2x speedup.**

### What was wrong before

Section 6.4 argued that fixtures only look synthesis-heavy because they are *small*, citing
`deep-optional-affix-nesting` as the largest fixture with the lowest synthesis share (6.1%) among
the high-ratio group. **That 6.1% was `forwardShare` — the wrong denominator.** Its correct
`synTotalShare` is **73.1%**, the highest of any reliable row, and it is by far the largest and
most trustworthy timing sample in the set (2.8 s, versus everything else under 110 ms).

So the trend the size argument rested on does not exist. The largest, most reliable fixture is
the *most* synthesis-bound, not the least. The argument was an artifact of the same wrong-share
error corrected in section 6.4, and it should not have been used to discount the fixture evidence.

### Bonus finding in the same table

`deep-optional-affix-nesting`: `altTotal` 926 -> `altDistinct` 3 (**0.32%**), with
`dupeSameWordPct` = **100.0%**. Every duplicate traces to the same analysis word, therefore shares
a trail — which is exactly the case where the existing fingerprint is adequate, because what it
omits (remaining-trail content) is identical across the group. The `ExpandAlternatives` dedupe is
fully interceptable pre-expansion on this grammar.

### Categories that clear the 2x bar

| group | fixtures at >=2x | verdict |
| --- | --- | --- |
| **A. Affix template slots** (obligatory/disjunctive/ordering) | 5 of 6 — 8.10x, 3.22x, 3.00x, 2.81x, 2.15x | **Clears, with both reliable rows** |
| **B. Disjunctive allomorphs / free fluctuation** | 2 of 2 — 3.00x, 2.00x | Clears; small samples, coherent mechanism |
| **C. NaturalClass precision** | 1 of 1 — 3.94x (81.1% share) | Clears; single fixture, 11.9 ms |
| **D. Stem names** | 1 of 1 — 2.00x (61.0% share) | Clears; single fixture, 2.1 ms |
| E. Subrule gating | 2.00x from **2 applications** | Discarded — one observation |
| F–K (compounding, MPR, metathesis/truncation, rewrite, loader, feature breadth) | none | Fail; retained as evidence for future grammars |

Groups B, C and D rest on sub-12 ms fixtures and cannot carry a timing claim on their own. Their
value is that the mechanism *engages* for those constructs — the wall-clock case is carried by
group A, where both reliable rows sit.


---

## 9. Reproducibility: two independent runs

The fixture pass was run twice, by two operators, on the same code. This is the check that decides
which rows can carry a claim.

| fixture | run A ceiling | run B ceiling | reliable |
| --- | --- | --- | --- |
| edge-cases/deep-optional-affix-nesting | 50.41% | **50.94%** | yes (2.4–2.8 s) |
| languages/suffixing-evidential-adjacency-chain | 51.95% | **50.23%** | yes (96–107 ms) |
| languages/suffixing-extension-slot-ordering | 13.17% | 10.16% | yes (62–69 ms) |
| edge-cases/strrep-identity — `synTotalShare` | 81.1% | **65.8%** | no (9.7–11.9 ms) |
| edge-cases/strrep-identity — `synExpandShare` | 1.3% | **14.5%** | no |

**The two reliable ≥2x rows reproduce to within ~1.7 points, both at ~50%.** That is the
load-bearing result and it is stable across runs.

**The unreliable rows do not reproduce.** `strrep-identity`'s `synExpandShare` moved 1.3% -> 14.5%,
an 11x swing on a 10 ms fixture. This is direct evidence for the `wallMs >= 50` flag rather than an
assumption behind it: sub-50 ms fixtures cannot carry a timing claim, and groups C and D (both
single sub-12 ms fixtures) must be read as "the mechanism engages for this construct", never as a
speedup estimate.

Deterministic counters — `applications`, `distinct`, `ratio`, `altTotal`, `altDistinct` — were
byte-identical across both runs, as they must be. Determinism violations: 0 in both.

### Commit hygiene note

Commit `1a7d484c` bundles the harness reporting change with the section 8 analysis, because the
agent that wrote the reporting change stopped with it uncommitted and the work was swept in when
the analysis was committed. Two concerns in one commit; flagged here rather than rewritten, since
the PR body is the review artifact.
---

## 10. The build was made, and it does not pay — because the redundancy is not real

`feature/synthesis-fold-sharing` (`1048c742`) implements the fold-step memo with a sound key:
`SynthesisStateKey` carries the **ordered remaining trail**, not just its index, plus shape,
syntactic FS, realizational FS, MPR set, root allomorph, disjunctive allomorph indices, applied
counts, `IsPartial`, `IsLastAppliedRuleFinal` and stratum — each field justified against its reader.

**Parity: 0 divergences across all 33 conformance fixtures.** The implementation is correct.

**Measured (warm-up discarded, min of 5 interleaved samples per arm):**

| fixture | predicted ceiling | realised | memo hits | off-arm spread |
| --- | --- | --- | --- | --- |
| deep-optional-affix-nesting | 50.4% (~2x) | **0.96x** (4.5% slower) | **0** | 38.6% |
| suffixing-evidential-adjacency-chain | 52.0% (~2x) | 1.06x (5.4%) | 2,682 | 21.9% |

**`hits = 0` on the fixture with the largest reliable sample and the highest predicted ceiling.**
With a trail-complete key the memo never fires there at all; the 0.96x is pure key-construction
overhead. On the evidential chain it does fire 2,682 times and returns 5.4% against 21.9% noise —
nothing.

### The structural finding

This is the same result arriving a third time, by a third independent route:

| measurement | key | apparent | sound |
| --- | --- | --- | --- |
| F1 synthesis-input dedupe | order-insensitive | 9,774x | 15–40% |
| N1 fold-entry census | trail position only | 6,476x | not established |
| **P1c fold-step sharing** | **trail position only** | **3.22x / 8.10x** | **hits=0 / 1.06x** |

**The redundancy in HermitCrab's synthesis is apparent, not real. The trail is what makes each
step distinct, and every measurement that shows large shareable work is measuring a key that omits
it.** Three different boundaries, three spectacular ratios, three collapses under a complete key.

That closes a family, not just a candidate: packed parse forests, fold-step sharing, and
synthesis-input dedupe all depend on distinct derivations converging on a genuinely identical
state, and in this engine they do not converge. It is the same fact that makes the rules
non-order-invariant, seen from the other side.

### Recommendation

**Do not merge the fold-step memo as a performance feature.** It is correct, it is off by default,
and it buys nothing — on the best case it is 4% slower. Keep the branch as the evidence that
closes the family.

The measurement infrastructure is the durable asset and should merge on its own.
