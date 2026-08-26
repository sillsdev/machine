# Forest memoization: the most we think we can get

> **OUTCOME (2026-08-26): the ceiling was never approached, because R came in at 1.12 on Sena
> and 1.17 on Indonesian against a 1.3 gate.** Section 6's falsification table fired on its first
> row. The measured numbers and the analysis of why the worst case collapses while the realised
> state count does not are in `hermitcrab-forest-memo-plan.md` sections 3.1–3.5. The rest of this
> file is left exactly as written *before* the measurement, because its value now is as a record
> of what was predicted and how the prediction did.
>
> How the predictions scored, for calibration: Indonesian was predicted 1.0–1.3 and came in at
> 1.17 — right. Sena was predicted 2–4 and came in at 1.12 — wrong, and wrong in the direction
> that decides the project. Amharic was predicted "most likely to disappoint" and had by far the
> best static classification (31 of 36 rules provably shrinking) — wrong again, though its
> realised R is what actually matters.


Companion to `hermitcrab-packed-forest-research.md` (why) and
`hermitcrab-forest-memo-plan.md` (how). This file answers one question only: **if everything
in the plan works, what is the number?**

Written 2026-08-26 on `feature/forest-memo`. Every input is cited. Where a figure is derived
rather than measured, it says so — Stage 0 of the plan replaces the derived ones with measured
ones, and this file should be rewritten once it does.

---

## 1. The budget we are spending against

Sena `atawirambo`, the reference heavy word, on the sequential+memo path that
`feature/memoization` ships:

| | measured |
| --- | --- |
| fair sequential unmemoized baseline | 30.5 s |
| after mrule memo + template memo (current HEAD) | **6.1 s** |
| morphological-rule cascade, memoized | 1.4 s / 2,555 expansions vs a 2,546-state floor |
| affix-template battery, unmemoized | 93% of the 30.5 s, run 38,840x |
| distinct `AnalysisStateKey` values | ~2,581 |

**Derived, not measured:** the template battery costs about 28.4 s over 38,840 runs, so roughly
0.73 ms per run. Memoized, it runs once per distinct key — about 2,581 times — for roughly
**1.9 s**. Adding the cascade's 1.4 s, about **3.3 s of the current 6.1 s scales with the
distinct-key count.** The residual **~2.8 s** is lexical lookup, `ReplayOnto` materialization,
`ExpandAlternatives`, and forward synthesis.

That split is the whole ceiling argument, and it is the first thing Stage 0 must confirm
directly. If the real split is 1.5 s / 4.6 s, every number below halves.

---

## 2. Bound 1 — key narrowing

Dropping shrinking-rule counts from `AnalysisStateKey` merges states that differ only in *which
shrinking affixes have been stripped along the way*. Call the resulting state-count reduction
**R**.

Both key-proportional subsystems scale by 1/R, because the mrule memo and the template memo are
keyed by the same object and each runs once per distinct key. So:

| R (state-count reduction) | key-proportional cost | word total | speedup |
| --- | --- | --- | --- |
| 1.0 (no collapse — the change is inert) | 3.3 s | 6.1 s | 1.00x |
| 1.5 | 2.2 s | 5.0 s | 1.22x |
| 2 | 1.65 s | 4.45 s | **1.37x** |
| 4 | 0.83 s | 3.6 s | 1.68x |
| infinite (free) | 0 s | 2.8 s | **2.18x** |

**The asymptote is 2.2x on this word, and no amount of cleverness in the key gets past it.**
That is the honest ceiling for the proposal as stated.

### What sets R

R is driven by **affix homophony**. Two paths only collide after narrowing if they stripped
*different* affix sets and arrived at the same shape with the same feature structures — which
requires distinct affixes with the same surface form and compatible feature effects. Sena has
many; Indonesian has few. This is why the plan measures R before building anything.

Our prior on R, stated so it can be scored later: **Sena 2–4, Indonesian 1.0–1.3, Amharic
unknown.** If Sena comes back at 1.1 the whole line of work is inert and Stage 1 should not be
built.

### The sleeper effect

Narrowing the key also raises the **nogood** hit rate — `MemoEntry` with an empty `Results` list
short-circuits an entire subtree, and there are more ways to hit a narrower key. This is free
upside not modelled in the table above, and it is worth counting separately in Stage 0 because
nogood hits are the cheapest possible win.

---

## 3. Bound 2 — the forest proper

Back-edges plus deferred materialization attack the ~2.8 s residue, in two ways:

**Per-state lexical lookup instead of per-path.** Today `LexicalLookup` runs once per analysis
candidate. With a forest, the shape is a property of the state, so it can run once per state.
`atawirambo`: 41 candidates reach the lexicon check, 4 reach the lexicon, 2 parse. The T1 probe
bounds what this can prune on the words that actually hurt: **pooled 23.5% of steps are
lexically dead on failure words**, and `pidafikawo` is at exactly 0.0% — a root substring exists
at every node visited and the word still fails, on checks that only run *after* lexical lookup
succeeds.

**Less materialization.** `ReplayOnto` clones every stored result on every hit. Deferring
materialization to the paths that actually reach synthesis removes that. Calibration from the
allocation work: shape sharing at the two proven-safe sites bought **-4.5 to -7.6% bytes** with
no wall-clock change, and pooling small collections was a **net loss on every axis**. Allocation
reduction on this codebase has consistently converted to wall clock at well under 1:1.

**Realistic contribution: 10–20% of the residue, i.e. 0.3–0.6 s.** Combined with Bound 1 at
R=2, that is roughly **4.0 s, or 1.5x**. At R=4, roughly **3.1 s, or 2.0x**.

---

## 4. What this does *not* touch — and why the failure words are the real prize

`atawirambo` succeeds. The words that cost the most fail:

| word | steps | synthesis inputs | parses |
| --- | --- | --- | --- |
| `atawirambo` | 14.9 M | — | 2 |
| `cinacemerwa` | 37.5 M | **218,847** | **0** |

For `cinacemerwa` (26.9 s post-memo) the cost is not state expansion at all. It is 218,847
forward synthesis runs that all fail. Bounds 1 and 2 barely touch it: narrowing the key does not
reduce the number of *paths*, only the number of *states*, and the synthesis input set is
path-shaped.

**The only lever on that number is merging order variants at readout**, which requires proving
rule-pair commutativity — and which we have measured as **unsound to assume**: same pending-rule
multiset, different order, different synthesis output, on Sena and independently on Indonesian.
See the research doc, section 2.2.

If a static or verify-once commutativity analysis lands, the F1 probe's **28.72x** aggregate
synthesis-input dedup on Sena heavy words becomes reachable, and on a word like `cinacemerwa`
that is close to the whole runtime. **That, not the key narrowing, is where an order of
magnitude lives.** It is out of scope for this branch and is written up as the follow-on.

---

## 5. The honest headline

**What we expect to be able to claim at the end of this branch:**

- Sena heavy words: **1.3–2.0x**, contingent on R landing in the 2–4 range.
- Indonesian: **1.0–1.1x**. Its state pressure is low and its F1 dedup ratio was 1.41x. This
  change is not aimed at Indonesian and should not be sold as if it were.
- Amharic: unknown, must be measured, and it is the grammar most likely to surprise us — it has
  infixation, truncation-reinsertion, and a `ModifyFromInput`, so its shrinking-rule set may be
  small and R may be near 1.
- A **worst-case key-space reduction from 2^k to 2^k'**, where k' counts only the
  non-shrinking rules. For a template-only grammar k' is 0 and the key becomes
  (shape, stratum, feature structures) — which is the correspondent's claim, and it is correct
  for that restricted class.
- A **clean termination proof** for the memo, replacing "we count every rule because some rule
  might loop" with "we count exactly the rules that can loop."

**What we do not expect to be able to claim:**

- Polynomial end-to-end parsing. Analysis becomes polynomial in input length for the restricted
  class; the grammar constant is a feature-structure lattice, and synthesis remains
  path-enumerated. Maxwell & Kaplan 1993 is explicit that the exponential lives at the interface
  between the packable component and the constraint component, not inside either.
- Any material improvement on the pathological failure words. That needs commutativity.

**The single number to lead with, if one is wanted: about 2.2x is the asymptote on our reference
heavy word, and we expect to realise somewhere between a half and all of it.**

---

## 6. Falsification conditions

Written before the measurements, so they cannot be adjusted afterwards.

| If Stage 0 shows | then |
| --- | --- |
| R < 1.3 on Sena | stop. Do not build Stage 1. Report the negative result. |
| key-proportional share < 30% of wall time | the ceiling table is wrong; recompute before building |
| readout-time count filtering raises synthesis inputs by >10% | Stage 2 is a net loss; keep the counts in the key and take only the forest |
| Amharic R >> Sena R | the change is more general than we thought; raise ambition |
| any corpus shows an analysis-set difference | stop and fix; completeness is not negotiable |
