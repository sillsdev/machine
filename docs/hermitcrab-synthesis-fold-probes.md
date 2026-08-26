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

_P1 pending._
