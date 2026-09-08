Draft review comment for sillsdev/machine #491 (not posted). Numbers from head 96c97f76 with a throwaway counter build; see `docs/pr491-final-template-prune-upper-bound.md` on the perf branch for the full record.

---

I measured this on three FLEx exports (Sena, Mbugwe, Amharic) with sequential + memo and, for Mbugwe, in the parallel mode FLEx uses. The mechanism is exactly right and the win is large where it fires, but three things stop it from firing or make it misbehave when it does.

**1. The win, with the guard lifted (`AlwaysEnforceFinalTemplates = true`)**

| Grammar | Words | Wall ms off → on | Analysis affix-rule applications off → on | Analysis set |
| --- | ---: | ---: | ---: | --- |
| Mbugwe, memo on | 22 | 119,059 → 12,066 | 7,801,493 → 442,237 | identical on all 22 |
| Mbugwe, parallel, no memo | 6 | 25,694 → 3,189 | 1,153,594 → 171,594 | identical on all 6 |
| Sena, memo on | 42 | 66,742 → 2,033 | 2,411,519 → 228,865 | identical sets on all 42; 6 words return each analysis twice (see 2) |
| Amharic, memo on | 3 | 9,806 → 10,634 | 1,530 → 1,377 | identical |

One Sena word (word-list index 74) goes from 84 s to 0.3 s. So the "minutes or hours" diagnosis is confirmed and this PR is the fix for it. In these samples the partial rules never rescued a parse (no analysis-set change), which is corpus evidence rather than proof.

**2. Bug: duplicate parses when enforcement is active (default on any non-partial grammar)**

`AnalysisAffixTemplateRule.Apply` sets `FinalTemplateAfterNonTemplate` on the cloned `inWord`. When all slots are optional and none unapplies, `ApplySlots` adds that clone to the output. It differs from the template's input only by `FinalTemplateState`, which is now part of `Word.ValueEquals`, so the `!Equals(input, tempOutWord)` check in `AnalysisStratumRule.ApplyTemplates` no longer filters the "template applied nothing" result. With `MergeEquivalentAnalyses` it becomes an alternative for the same shape and synthesizes into a second copy of the same parse. Observed on 6 of 42 Sena words (word-list index 9: 12 → 24 identical analyses). Suggested fix: skip adding an all-skipped `inWord` whose only difference from the input is the state (or exclude the state from the equality used by that filter).

**3. Guard: partial lexical entries can never rescue a pruned branch, so they should not disable the prune**

`SynthesisAffixTemplatesRule.Apply` applies no template when `input.RootAllomorph.Morpheme.IsPartial`. Synthesis replays the analysis's rule list, so a candidate that unapplied a template slot rule with a partial root leaves that rule unapplied and dies with `PartialParse`. Only partial *rules* (and only in strata at or below the template's stratum) can make the poisoned order legal. `Morpher.IsPartial` currently counts lexical entries too: Sena has 25 partial entries and 6 partial rules; both Mbugwe and Amharic have exactly one partial rule (an unclassified affix) and no partial entries. A rules-only, per-stratum guard is strictly stronger and still sound. It does not rescue these three grammars (each has a partial rule in the template stratum), but it does for any export whose only partials are POS-less entries.

Worth telling FLEx users: on Mbugwe, classifying the single unclassified affix (mrule22, a subject-agreement prefix) would turn this prune on by default for a ~10x parse speedup.

**4. State transitions run while the prune is disabled**

`outWord.FinalTemplateState = NonTemplate` in `AnalysisAffixProcessRule` and `AnalysisCompoundingRule` is not gated by `!IsPartial || AlwaysEnforceFinalTemplates`, so `AnalysisStateKey` and `Word` equality split otherwise-equal states on every partial grammar, where no prune can ever fire. Measured memo-hit loss on Sena (856 → 768 positive hits) with default settings. Computing `enforce` once and leaving the state `None` when it is false removes the cost.

**5. Smaller points**

- The poison is stamped on the eager `input.Clone()` in the template rule; if the clone is later deferred (perf branch does this), the state has to travel separately.
- When every template in the stratum is final, a poisoned state can skip the whole battery before any slot rule runs; that removes the remaining `tmplEntry` work.
- Tests to add: partial grammar default behaviour, `AlwaysEnforceFinalTemplates` on a partial grammar, the all-optional-slots duplicate case, memo on/off parity with the prune active, stratum/clitic reset.
- Signature note for anyone re-measuring: `MorpherTests.WordAnalysisSignature` uses `Morpheme.Id`, which is empty for FLEx exports without `<MorphemeId>`, so it only compares morph counts on these grammars. Use gloss-based signatures.
