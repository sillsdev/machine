# PR #494 "Change Add to PriorityUnion": review, break attempts, extension

**Date:** 2026-09-08. **PR:** sillsdev/machine #494 (jtmaxwell3, head `dc8efec5`, one line in
`AnalysisAffixProcessRule.Apply`). **Reviewer asks (ddaspit):** a unit test for the described scenario; apply
the same change to compounding rules.

**Branches produced here**

| Branch | Purpose |
| --- | --- |
| `perf/pr494-proposed` (pushed, `3be1eb96` on top of the PR commit) | What should land: PR #494 + compounding + a soundness fix + 3 tests. 97/97. |
| `perf/pr494-priority-union` (pushed) | Research: same-binary `Add` / `PriorityUnion` / `Exact` toggle (`HC_ANALYSIS_FS_MERGE`), deterministic counters, `FsMergeBench` harness, 21 adversarial tests, measurements. 118/118 under every mode. |

## 1. Is the change sound?

Yes. Synthesis (`SynthesisAffixProcessRule`) computes `PriorityUnion(Unify(rule.Required, stem), rule.Out)`.
Whatever the analysis side has accumulated, the stem must unify with `rule.Required`, so after un-applying a rule
the stem constraint is `Required` on those features. `Add` (per-feature value union) only ever widened that
constraint; `PriorityUnion` states it. Analysis over-generation is harmless (lexical lookup never filters on the
analysis FS; synthesis re-verifies every candidate), so the only hazard is under-generation, and PriorityUnion
never rejects a stem synthesis would accept **at the rule itself**. Real grammars put feature variables only in
phonological rules, so the variable-handling difference between `Add` and `PriorityUnion` is not exercised.

## 2. Measured effect (deterministic counts, sequential Morpher, 30 words per grammar, gloss+allomorph+FS parity)

| Grammar | Mode | Wall ms | checkCalls | merges | synthesisAffixApplyCalls | Parity |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Mbugwe | Add (master) | 339,743 | 14,687,568 | 1,632,377 | 11,517,396 | — |
| Mbugwe | PriorityUnion (#494) | 132,838 | 4,042,965 | 522,599 | 5,429,688 | identical 30/30 |
| Mbugwe | Exact (research) | 103,616 | 2,827,307 | 468,004 | 1,068,998 | identical 30/30 |
| Sena | Add | 27,934 | 746,808 | 66,217 | 339,732 | — |
| Sena | PriorityUnion | 8,470 | 137,964 | 10,604 | 70,473 | identical 29/29 |
| Sena | Exact | 6,527 | 74,058 | 5,980 | 56,397 | identical 29/29 |

Amharic and Indonesian carry no signal (Indonesian counters identical across modes; Amharic dominated by one
mode-independent 180 s timeout). Full tables, commands, and the post-fix re-run: `pr494-fs-merge-measurements.md`.
Wall time is reported only as a sanity check; the counts are the claim.

## 3. Break attempts

21 adversarial tests (`tests/.../MorphologicalRules/AnalysisSyntacticFeatureMergeTests.cs`), each run under all
three modes. Outcomes:

| Case | Add | PriorityUnion | Exact |
| --- | --- | --- | --- |
| Maxwell's chain: POS after two un-applications; third rule gate | {N,V}, attempted | N, rejected | N, rejected |
| Full valid chain found end to end | yes | yes | yes |
| Nested head features, disjunctive required POS, Clear branch, rule applied twice, compounding | parity | parity | parity |
| Guessed roots (`LexicalGuess`) | parity | parity | parity |
| **Same-shape analyses merged, narrower path canonical (two strata)** | **found** | **LOST** | **LOST** |
| Override loss: inner Out tense:pres overridden by outer Out tense:past, outermost requires past | lost | lost | **found** |
| Same, through a non-final template | lost | lost | lost |

**The one real regression.** `Word.ValueEquals` and the frozen hash exclude `SyntacticFeatureStruct`.
`Morpher.MergeEquivalentAnalyses` (default true) folds same-shape outputs of a stratum into one canonical word,
and the strata below are un-applied against the canonical word's FS only (`Word.ExpandAlternatives` replays the
alternatives' trails onto the canonical's descendants). Two upper-stratum paths that reach the same shape can
carry different stem constraints; if the narrower one is canonical, a lower-stratum rule valid for the other
path is filtered and its parse is lost. Master finds it only because `Add`'s union happened to keep the
canonical FS loose. Repro: `ShapeMerge_OneRulePathFirst_NoWidening_PriorityUnionLosesParse` (0 analyses under
PriorityUnion, 2 under Add).

**Fix (in `perf/pr494-proposed`):** when merging, generalize the canonical FS with `FeatureStruct.Union`
(features present in both keep the union of their values, features present in only one are dropped). This is
the least upper bound of the merged constraints, so it is sound for every alternative. Test fails without it and
passes with it (mutation-checked). On the 30-word corpora the widening fires (Mbugwe: 102,804 times under
PriorityUnion) without changing any analysis set and with <1% change in counts.

## 4. Extension: the exact inverse ("Exact" mode, research only)

Synthesis output is `PU(stem ⊓ required, out)`, so the exact analysis inverse is: un-apply only if the input
unifies with `PU(required, out)` (a strictly stronger, still necessary check), and set the stem constraint to
`(input with out's feature paths removed) ⊓ required`. Never `Clear()`. It cuts a further 30-45% of checks on
Mbugwe/Sena with identical analysis sets, and it fixes a pre-existing master bug: a rule whose Out overrides an
inner rule's Out (tense:past over tense:pres) loses the parse under `Add` and `PriorityUnion` because neither
removes the stale Out feature.

**Not shippable yet.** The template battery dedups outputs with the FS-blind Word equality, so the exact template
merge collapses two templates that share a slot rule but differ in required POS
(`AffixTemplateTests.SameRuleUsedInMultipleTemplates` fails). Prerequisite: FS-aware dedup, either Word equality
including the syntactic FS (would also remove the "which duplicate survives is observable" hazard noted in
`MemoizedCombinationRuleCascade`) or Union-widening at every collapse site. The template-level override loss
(`OverrideLoss_ThroughNonFinalTemplate_LostInAllModes_KnownLimitation`) is pinned as a known limitation.

## 5. Side findings

- `Morpher.LexicalGuess` overwrites the guessed entry's FS with the pattern entry's, so the analysis FS never
  reaches guessed roots; the earlier assignment is dead code when the pattern has an owning entry.
- `FeatureStruct.RemoveValue(IEnumerable<Feature>)` always throws (missing `return`). Unused.
- `AnalysisAffixTemplateRule` mutates the syntactic FS of already-frozen words (`Add(fs)`); safe only because
  Word's freeze excludes the FS.

## 6. Ready-to-paste PR comment (not posted)

> I put #494 through a measurement and adversarial round on four FLEx grammars (deterministic counts, sequential
> parser, analysis sets compared by gloss + allomorph + feature structure).
>
> **Effect:** on Mbugwe the analysis-side unifiability checks drop from 14.7M to 4.0M and synthesis rule
> attempts from 11.5M to 5.4M over 30 words (wall 340 s → 133 s); Sena 747k → 138k checks (28 s → 8.5 s).
> Analysis sets are identical on every word. Amharic and Indonesian are unaffected either way.
>
> **Soundness:** PriorityUnion is the right inverse of `Unify(required, stem)` in `SynthesisAffixProcessRule`,
> so it never rejects a stem synthesis would accept at the rule itself.
>
> **One regression, with a fix:** `Word.ValueEquals` ignores `SyntacticFeatureStruct`, and
> `MergeEquivalentAnalyses` un-applies lower strata against the canonical word only. Two same-shape paths with
> different stem constraints can therefore lose a valid parse when the narrower one is canonical; `Add`'s union
> hid this. Fix: generalize the canonical FS with `Union` when merging (`AnalysisStratumRule`). Test included,
> fails without the fix.
>
> `perf/pr494-proposed` has this PR's commit plus: PriorityUnion in `AnalysisCompoundingRule` (as requested),
> the Union generalization, and three tests including the unit test for the three-rule scenario. Happy to open it
> against this branch or for you to cherry-pick `3be1eb96`.
