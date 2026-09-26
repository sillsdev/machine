# PR #494 against the conformance suite: break attempts and draft review comments

**Date:** 2026-09-09. **Branch:** `perf/pr494-conformance` (branch since deleted; preserved as tag
`archive/pr494-conformance`) = `conformance/fieldworks-witnesses` (d3b7643d; branch since deleted,
preserved as tag `archive/conformance-tip-unsquashed`) + one squashed commit carrying PR #494 (dc8efec5)
plus the review-round follow-ups (compounding PriorityUnion, Union-generalized merge, tests). Earlier
measurement/adversarial round: `docs/pr494-review.md` and `docs/pr494-fs-merge-measurements.md` on
`perf/pr494-priority-union`.

## What was run

| Check | Result |
| --- | --- |
| Conformance self-check, all 36 fixtures incl. pathological | 36/36 PASS |
| Generated-word fuzz: every root x every 0..3-subset of affix rules (and up to 6 nonhead roots where the grammar compounds) through `Morpher.GenerateWords`, then parsed by master (d3b7643d) and by this branch, analysis signatures diffed | 1,300 words over 35 grammars, 0 differences attributable to the change |
| HermitCrab unit tests | see status below |

Fuzz harness: scratchpad `fuzzgen` (throwaway console project) + `rundiff.sh` driving `hc.dll batch` through
`conformance/adapters/hc-dotnet-wrapper.sh`. `prefixal-discontinuous-slot-dependency` generated no words at
depth 3 (its template needs more slots filled); its fixture words pass self-check.

## The one diff, and why it is not #494

`edge-cases/truncate-morphotactic`, generated words `a` and `ga` (not fixture words): the analysis
*multiplicities* vary run to run on **master alone** (four runs: `a` = `+|a;|a`, `|a;|a`, `++|a;|a`,
`++|a;|a`), and the same on the PR build. The analysis *sets* are stable; which duplicate survives is not.
Three rules that all apply "unordered" with truncation/insertion produce same-shape words differing only
in morpheme trail, and a parallel HashSet keeps whichever arrives first. Pre-existing oracle
nondeterminism, unrelated to the syntactic FS. Worth its own issue (the conformance signature would pin
it); not for this PR.

## Draft comments for PR #494 (NOT posted; await approval)

### Line comment: `AnalysisAffixProcessRule.cs` line 59 (the changed line)

> This is exactly the inverse of `SynthesisAffixProcessRule`: synthesis unifies the stem with `Required`
> before applying `Out`, so after un-applying, the stem's constraint on those features is `Required` and
> nothing else. `Add` kept the union of everything seen so far, which can only over-generate.
>
> Tried to break it: 21 adversarial unit tests (disjunctive POS, nested head features, rule applied
> twice, guessed roots, compounding, two strata), all 36 conformance fixtures, and 1,300 words generated
> from the conformance grammars, parsed on master and here. Identical analysis sets everywhere. The only
> hazard is the merge below (general comment).

### Line comment: `AnalysisAffixProcessRule.cs` line 61 (`Clear()` branch, context line)

> Not for this PR, but same neighbourhood: when `Required` is empty and `Out` is not, `Out`'s features stay
> on the stem. An inner rule whose `Out` sets `tense:pres` under an outer rule whose `Out` sets
> `tense:past` then fails the `IsUnifiable` gate at line 46, and the parse is lost on master and here
> alike. Removing `Out`'s feature paths from the stem here (the exact inverse of `PriorityUnion(_, Out)`)
> finds it and cuts another 30-45% of checks on Mbugwe/Sena. It needs the template battery to stop
> deduping words FS-blind first, so: follow-up.

### General comment

> Measured this on four FLEx grammars with deterministic counts (sequential parser, analysis sets compared by
> gloss + allomorph + feature structure), 30 words each:
>
> | Grammar | Unifiability checks | Synthesis rule attempts | Wall |
> | --- | ---: | ---: | ---: |
> | Mbugwe | 14.7M -> 4.0M | 11.5M -> 5.4M | 340 s -> 133 s |
> | Sena | 747k -> 138k | 340k -> 70k | 28 s -> 8.5 s |
>
> Amharic and Indonesian: no change either way. Analysis sets identical on every word. Also 36/36 conformance
> fixtures pass and 1,300 words generated from the conformance grammars parse identically to master.
>
> **One regression, with a fix.** `Word.ValueEquals` ignores `SyntacticFeatureStruct`, and
> `MergeEquivalentAnalyses` (`AnalysisStratumRule`) un-applies lower strata against the canonical word
> only, replaying the alternatives' trails afterwards. Two same-shape paths can now carry different stem
> constraints; if the narrower one is canonical, a parse master finds is lost. `Add`'s looseness hid it.
> Fix: when merging, generalize the canonical FS with `Union` (least upper bound: shared features keep the
> union of values, the rest drop). Fires ~100k times on Mbugwe with zero change to any analysis set and
> <1% change in counts. Same hazard applies to #493's `Alternatives.Add` path, so whichever lands first
> wants it.
>
> `perf/pr494-proposed` (3be1eb96, on top of this PR's commit) has: PriorityUnion in
> `AnalysisCompoundingRule` (as Damien asked), the Union generalization, and three tests including the
> three-rule chain from the description, which fails without the change. Happy to open it against this
> branch or for you to cherry-pick.

## Draft comment for PR #493 (cross-reference; NOT posted)

> Heads-up from #494: attaching an FS-different word as an `Alternative` (your suggested fix) puts it
> behind a canonical whose FS may be narrower than its own, and lower strata only see the canonical's FS.
> Under PriorityUnion that loses parses (repro in `perf/pr494-proposed`); the fix there is to `Union` the
> canonical's FS with the alternative's on every merge. If #493 lands first it wants the same line.

## Unit tests

HermitCrab test project on this branch: 582 passed, 1 skipped, 1 failed. The failure is
`ConformanceManifestTests.TheCheckedInManifestMatchesRegeneration`: the checked-in
`conformance/generated/hc-conformance-manifest.v1.json` carries a stale `wordsSha256` for `conformance/edge-cases/alpha-variable-name-collision/words.yaml`.
It fails identically on the base commit d3b7643d with no #494 code, so it is
conformance-branch housekeeping (regenerate with `hc-conformance --generate-manifest`), not a #494 effect.
