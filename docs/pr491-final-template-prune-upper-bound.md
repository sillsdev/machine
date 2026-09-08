# PR #491 (final-template prune) re-review: the guard, not the mechanism, is why it measures as inert

**Date:** 2026-09-03. **PR:** sillsdev/machine #491 `filter-final-templates-in-analysis`, head `96c97f76`
(author jtmaxwell3). **Scratch worktree:** `.worktrees/pr491` (PR head plus uncommitted throwaway counters and
the `FinalTemplateUpperBound` NUnit harness; nothing committed).

## Summary

1. The earlier review was right that #491's *default* does nothing on Sena, Amharic, Mbugwe, Indonesian and Aweti:
   every FLEx export contains at least one morpheme marked `partial`, and #491 disables the prune grammar-wide
   when any morpheme is partial.
2. The earlier review was wrong to conclude "no opportunity". With the guard lifted
   (`AlwaysEnforceFinalTemplates = true`) the prune removes the dominant cost on Bantu-shaped grammars:

   | Grammar | Words | Mode | Wall ms off → on | Affix-rule applications off → on | Template entries off → on | Analysis-set parity |
   | --- | ---: | --- | ---: | ---: | ---: | --- |
   | Mbugwe | 22 (idx 0,65,1272 + 1–19) | sequential, memo on | 119,059 → 12,066 (9.9x) | 7,801,493 → 442,237 (17.6x) | 190,372 → 28,908 | 22/22 identical |
   | Mbugwe | 6 (0,65,1272,4,11,14) | parallel, memo off (FLEx mode) | 25,694 → 3,189 (8.1x) | 1,153,594 → 171,594 (6.7x) | 11,271 → 11,220 | 6/6 identical |
   | Sena | 42 (49,52,74 + 0–39, one invalid) | sequential, memo on | 66,742 → 2,033 (33x) | 2,411,519 → 228,865 (10.5x) | 139,283 → 19,401 | 36/42 identical; 6 words return the same analyses with doubled multiplicity (bug, below) |
   | Sena word-list index 74 | 1 | sequential, memo on | 84,388 → 283 (298x) | 1,532,928 → 40,375 | 84,769 → 3,730 | identical |
   | Amharic | 3 (8,9,267) | sequential, memo on | 9,806 → 10,634 | 1,530 → 1,377 | 146 → 146 | identical |

   Amharic barely moves because its per-word cost is not in the interleaving (only ~700 affix-rule applications
   per 4-second word); Mbugwe and Sena are dominated by it. Compare the retained perf branch's whole package:
   1.76x Amharic, 1.83x Mbugwe. The prune is an order of magnitude larger where it applies and composes with
   the branch (it removes search branches; the branch makes surviving branches cheaper).

   Parity was re-checked with a gloss + allomorph-index + syntactic-feature-structure signature (see the
   signature caveat below): Mbugwe 22/22 and Amharic 7/7 identical multisets; Sena 42/42 identical *sets*, with
   all 54 differences being `off=1 on=2` copies of the same full signature (the duplicate-parse bug below).

3. In these samples the partial rules never rescued a parse: enforcement changed no analysis *set*. That is
   corpus evidence, not a proof, so the default cannot simply be flipped. But it says the prize is real.

## Why the memo does not already absorb this

The order-independent memo (PR #490) collapses re-arrivals at the same multiset state. The waste #491 targets is
different: after every cascade state that ends in a non-template rule (`{R1}`, `{R2}`, `{R1,R2}`, ...), the
template battery runs on that *distinct* state, and each battery entry costs a full slot-rule FST match sweep.
Those batteries' outputs are illegal (cannot survive synthesis) unless a partial rule is applied inside. The memo
shares the continuation *after* the battery, not the battery itself. Counters confirm it: with enforcement on,
template entries in poisoned contexts drop from ~95% of all entries to zero, and memo hits collapse (Mbugwe
9,079 → 36 positive hits) because the redundant arrivals were the pruned ones.

## The guard is provably over-broad (partial lexical entries never matter)

#491 sets `Morpher.IsPartial` if *any* morpheme is partial, including lexical entries (Sena: 25 of its 31). But
partial roots cannot rescue a poisoned branch:

- `SynthesisAffixTemplatesRule.Apply` applies no template when `input.RootAllomorph.Morpheme.IsPartial`; the word
  passes through with `IsLastAppliedRuleFinal = true`.
- Synthesis replays the analysis's rule list (`Word.IsMorphologicalRuleApplicable`). A candidate that unapplied a
  template slot rule but has a partial root leaves that rule unapplied, so `HasRemainingRulesFromStratum` fails
  it with `PartialParse`.
- Slot rules are never also stratum-level mrules in these exports (overlap 0/113 Sena, 0/203 Mbugwe, 0/52
  Amharic), so nothing else can apply them.

Therefore only partial **rules** can rescue, and only rules in strata at or below the template's stratum
(deeper strata apply earlier in synthesis). A sound, strictly stronger guard is
`enforce = !stratumHasPartialRuleAtOrBelow(k) || AlwaysEnforceFinalTemplates`, computed once per stratum.
On the three grammars it still disables the prune (each has partial rules in the template stratum), but it turns
the prune on for any grammar whose only partials are POS-less lexical entries, which FLEx produces routinely.

## What blocks the three grammars: a handful of unclassified affixes

| Grammar | Partial rules (all non-template, template stratum) | Partial entries | Templates final |
| --- | --- | ---: | --- |
| Sena | 6 (mrule15, 16, 19, 20, 21, 23; none in a template slot) | 25 | 24/24 |
| Mbugwe | 1 (mrule22, a subject-agreement prefix with a required POS) | 0 | 23/24 |
| Amharic | 1 (mrule11, a definiteness marker with a required POS) | 0 | 15/15 |
| Indonesian | 4 | 0 | no templates (no opportunity) |

FLEx marks an affix `partial` when its MSA is unclassified. Classifying Mbugwe's single unclassified subject
prefix (or Amharic's definite marker) makes the whole grammar non-partial, at which point #491's *default* fires
with the measured effect. This is the strongest grammar-advisor recommendation available: one unclassified affix
costs Mbugwe about 10x parse time under #491.

No sound *engine-side* condition finer than "no partial rule reachable inside" was found. Deferring templates
until a partial rule appears loses parses when the template affixes and the rescue chain are on the same side
(outer affixes must unapply first). Necessary-condition checks on the shape are unsound because analysis
unapplication generalizes segments (ModifyFromInput inverses), so a partial rule that cannot match now may match
later. An exact, sound, but small filter remains: at lexical lookup, drop a candidate that is still poisoned and
unapplied no partial rule inside (the synthesis replay would reject it at the first non-template rule after
the template anyway; `synthRejectAfterFinal` counted 16 of 36 synthesis affix applications on Amharic word 8).

## Defects found in #491 (head 96c97f76)

1. **Duplicate parses when the prune is active.** `AnalysisAffixTemplateRule.Apply` marks the poison on the
   cloned `inWord`; when every slot is optional and none applies, `ApplySlots` emits that clone. It differs from
   the input only by `FinalTemplateState`, which is now part of `Word.ValueEquals`, so
   `AnalysisStratumRule.ApplyTemplates`'s `!Equals(input, tempOutWord)` filter no longer removes the
   "template applied nothing" output. With `MergeEquivalentAnalyses` it becomes an alternative of the input's
   shape and synthesizes into a second copy of the same parse. Observed on 6 of 42 Sena words (e.g. word-list index 9:
   12 → 24 identical analyses). This is default behavior on any non-partial grammar. Fix: compare with the
   state masked, or do not emit an all-skipped poisoned clone.
2. **Memo splitting while disabled.** `NonTemplate`/`None` transitions in `AnalysisAffixProcessRule` and
   `AnalysisCompoundingRule` are not gated by the enforcement flag, so `AnalysisStateKey` and `Word` equality
   split otherwise-equal states even when no prune can fire (Sena memo hits 856 → 768 in the earlier review).
   Compute `enforce` once and leave the state `None` when it is false.
3. **Guard counts lexical entries** (section above).
4. **Result-changing option lacks evidence.** `AlwaysEnforceFinalTemplates` is the only way to reach the win on
   FLEx exports today; the PR carries no real-grammar measurement of either the win or the parse-set delta.

## Signature caveat for all earlier "exact parity" claims on these grammars

`MorpherTests.WordAnalysisSignature` (used by `PerfBench`, `MemoCorpusVerification`, and the earlier five-grammar
comparison) joins `Morpheme.Id`, which comes from `<MorphemeId>`. None of the three FLEx exports contain that
element, so on Sena, Mbugwe and Amharic the signature degenerates to a morph count per analysis. Parity claims
made with it on these grammars verified counts, not analyses. The harness in `.worktrees/pr491` uses a
gloss/allomorph/feature-structure signature; the earlier perf-branch parity gate should be re-run with one.

## Recommendation

- **Do not port #491 as-is into the perf branch or PanGloss.** Default-inert on every available grammar, and it
  duplicates parses where it does fire.
- **Feed the four defects and the rules-only, per-stratum guard back to #491** (draft comment prepared).
- **Add "unclassified affixes disable final-template pruning" to the grammar advisor**, with the per-grammar list
  above. This is the lever that turns the measured 8–33x into a default-mode win.
- **Port the mechanism to PanGloss behind the same guard, with the battery hoisted** (skip the whole template
  battery on a poisoned state when every template in the stratum is final), once #491's semantics are fixed.
- Re-run the perf branch's parity gate with a non-degenerate signature.
