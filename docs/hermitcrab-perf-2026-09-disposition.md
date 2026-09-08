# HermitCrab optimization round, September 2026: final disposition of the branch

**Date:** 2026-09-03. **Branch:** `perf/hc-optimization-pr` @ `179708d2`, renamed `perf/hc-optimization-archive`.
**Decision:** do not merge the branch. Isolate one mechanism onto master as its own small PR; archive the rest with
its records. Re-focus on sillsdev/machine PR #491 and on grammar hygiene, which are where the large returns are.

## Why the package is not merged

- 896 source lines across 28 files; about a third inside `SIL.Machine.FeatureModel.FeatureStruct`, the rest in FST
  traversal internals, `Word`, `Shape`, and the analysis rules. Not reviewable as one change.
- Only one mechanism (deferred template clone, 1.054x) was ever timed alone. The package figure (1.76x Amharic,
  1.83x Mbugwe) cannot be attributed to its parts (`hermitcrab-perf-2026-09-counts.md`).
- The parity gate that blessed the package compared morph counts, not analyses: `MorpherTests.WordAnalysisSignature`
  joins `Morpheme.Id`, which is empty for every FLEx export used (no `<MorphemeId>` element). See
  `pr491-final-template-prune-upper-bound.md`, "Signature caveat".
- Meanwhile PR #491's mechanism, measured with the guard lifted, is 8–33x on Mbugwe and Sena with identical analysis
  sets (same document). The branch is worth keeping only where #491 cannot fire (Amharic-shaped grammars), and only
  in its most reviewable piece.

## Disposition per mechanism

| Mechanism | Lines / files | Evidence on the branch | Disposition |
| --- | --- | --- | --- |
| Analysis edge-segment prefilter | ~120 in 2 rule files + 9 in `Morpher` + tests | 87% fewer FST traversals on 45 Sena words (deterministic counts); one invariant; internal toggle | **Isolated onto master** as `perf/hc-edge-prefilter` (`be7f241d`), see below |
| Copy-on-write `Shape` sharing | `Word`, `Shape`, every clone-then-mutate site | clone 36% → 9% of profile; peak working set 11 GB → 6 GB | Archived. Aliasing rules are subtle; re-open only if memory becomes the constraint. Commit `dfd1be94`, `e18ca86d` |
| Copy-on-write syntactic `FeatureStruct` sharing | `Word`, `Morpher`, rules | ~15% of a heavy Sena profile; never isolated | Archived. `4a6a47ed`, `7f9fdef3`, `da35ad4e`, `f08fefa7` |
| `FeatureStruct` equality/clone/freeze fast paths, visited-set pooling | 312 lines in core FeatureModel | "not isolated" | Archived. `325f8419`, `424a3c23`, `b23a7c99`, `00ece2ba`, `2f4dabe0` |
| Flat FST register arrays | FST internals | `CreateInstanceMDArray` 11.5% → 0.2% of profile | Archived. `b1692a7a` |
| Deferred template clone | `AnalysisAffixTemplateRule` | 1.054x alone | Archived. `57703d6b`, `24d75c82` |
| `ExpandAlternatives` memo/hoist | `Morpher` | no retained difference | Already removed on the branch. `d8886b0c`, `f4195be1` |
| Two-pass nondeterministic traversal | FST | negative | Already removed. `c3366fcc` |
| Final-template prune (own version) | analysis rules | zero prunes under the guard | Already removed; superseded by #491 work. `7e6ec59d` |
| PerfBench / compare script / ParseCounters | tests, scripts | harness | Archived. Its signature is degenerate on FLEx exports; any revival must switch to a gloss/allomorph/feature-structure signature first |
| Research records (`docs/optimizations/*`, `docs/rejected-optimizations/*`, ledger, counts, probe design, this file, the #491 re-review) | docs | — | Kept on the archive branch as the record of what was tried and why |

## The isolated prefilter

Branch `perf/hc-edge-prefilter`, worktree `.worktrees/prefilter`, one commit `be7f241d` on `origin/master`
(`d7347226`). Content: `AnalysisAffixProcessAllomorphRuleSpec` captures the left/right top-level `Constraint`
feature structures at compile time; `AnalysisAffixProcessRule.Apply` rejects an allomorph before its matcher runs
when the shape's first/last Segment node provably cannot unify with them, using the matcher's own `IsUnifiable`
call and `UseDefaults`; optional edge nodes and non-Constraint edges always pass; tracing emits the same
`MorphologicalRuleNotUnapplied` the matcher would have. `Morpher.EdgePrefilterEnabled` (internal, default true) is
the same-binary A/B switch. The research counters were not carried over. `AnalysisEdgePrefilterTests` (final branch
version, no counter dependency) came with it. HermitCrab suite: 104/104.

### Standalone measurement (same binary, toggle off = master behavior, toggle on; sequential, memo on)

Signature: gloss + allomorph index + syntactic feature structure, compared as multisets. Counters: throwaway
increments at the two nondeterministic `Traverse` entries and at `CheckInputMatch` (not committed).

| Grammar | Words | Wall ms off → on | FST traversals off → on | Arc checks off → on | Parity |
| --- | ---: | ---: | ---: | ---: | --- |
| Sena | 42 (49,52,74,0–39; one invalid line skipped) | 71,296 → 69,900 (−2%) | 8,760,382 → 1,270,122 (−85%) | 12,618,566 → 6,004,919 (−52%) | identical 42/42 |
| Mbugwe | 22 (0,65,1272,1–19) | 110,609 → 96,293 (−13%) | 28,054,872 → 11,419,808 (−59%) | 36,811,510 → 25,960,854 (−29%) | identical 22/22 |
| Amharic | 10 (8,9,267,5,10,12,15,20,25,30) | 111,617 → 103,403 (−7%) | 145,050 → 129,802 (−10%) | 16,705,704 → 15,777,072 (−6%) | identical 10/10 |

**Reading.** The count reduction reproduces (Sena −85% traversals, matching the branch's −87%), and parity holds
under a real signature. But the traversals removed are the cheap ones: they would have died at their first arc
check, so wall time moves only 2–13%. The branch's package figure (1.76x Amharic, 1.83x Mbugwe) therefore came
mostly from the allocation, clone, and GC work, not from this mechanism. Standalone, the prefilter is correct,
small, and reviewable, but it is **not a large return**. Recommendation: keep `perf/hc-edge-prefilter` ready as an
optional small PR (7–13% on Bantu grammars, where it stacks with #491's prune on the surviving branches), and do not
spend review budget on it ahead of #491.

Raw logs: session scratchpad `pf-sena.log`, `pf-mbugwe.log`, `pf-amharic.log`.

## Branch and worktree inventory for the round (2026-08-20 to 2026-09-03)

Every branch below is local unless marked. Worktrees for archived branches were removed on 2026-09-03; the
branches remain.

| Branch | Tip | What it is | State |
| --- | --- | --- | --- |
| `perf/hc-optimization-archive` (was `perf/hc-optimization-pr`) | `b58b1642`+ | the whole round, code + records | archived, unmerged; worktree `.worktrees/ledger` |
| `perf/hc-defer-template-clone` | `4a6a47ed` | working branch of the round | fully contained in the archive; worktree removed |
| `perf/hc-engine-alloc` | `f22f88a8` | allocation/two-pass side branch | fully contained in the archive; worktree removed |
| `perf/hc-conformance-check` | `0799a977` | archive merged into the conformance integration branch for the 603-test gate | verification only; worktree `.worktrees/hc-conf` can go |
| `perf/hc-edge-prefilter` | `be7f241d` | the one isolated mechanism on `origin/master` | optional small PR; worktree `.worktrees/prefilter` holds uncommitted throwaway counters + `PrefilterAB` harness |
| `pr-491` | `96c97f76` | jtmaxwell3's PR head | worktree `.worktrees/pr491` holds uncommitted throwaway counters + `FinalTemplateUpperBound` harness |
| `docs/hc-optimization-ledger` (remote, PR #490) | `cc806613` | the 22-row ledger | open docs PR |
| `feature/forest-memo`, `feature/synthesis-fold-probes`, `feature/synthesis-fold-sharing`, `feature/per-node-cost` | — | evidence branches of the ledger round | see `hermitcrab-optimization-ledger.md` |
| `investigate-memoization-rule-order` | `4f5aeeb4` | despite the name, conformance-suite commits | belongs to the conformance line, not this round |

Records in this directory: `hermitcrab-optimization-ledger.md` (22 attempts), `hermitcrab-perf-2026-09-counts.md`
(count round), `hermitcrab-probe-design.md`, `optimizations/*` and `rejected-optimizations/*` (one record per
mechanism), `pr491-final-template-prune-upper-bound.md`, `pr491-review-draft.md`, `pangloss-final-template-prune-handoff.md`,
`pangloss-hc-rust-speedups-handoff-2026-09-02.md` (archived, section 4 superseded), `superpowers/plans/2026-09-02-hermitcrab-optimization-round.md`
(the execution plan), and this file.

Privacy: word forms and glosses from the private grammars appear only as word-list indices and rule ids in these
records. The raw A/B logs (which contain word forms) live only in the session scratchpad and are not to be
committed. The main checkout's `.git/info/exclude` now lists the local grammar backups, `Mbugwe.xml`, the Aweti
word list, and the rulestats HTML report so they cannot be staged by accident.

## What to do next, in order

1. Fix and merge PR #491 (duplicate parses, memo splitting while disabled, entry-counting guard). Draft comment in the
   re-review record.
2. Classify the unclassified affixes in the FLEx projects (Mbugwe 1, Amharic 1, Sena 6); that turns #491's default
   prune on. Add the finding to the grammar advisor.
3. Open the prefilter PR from `perf/hc-edge-prefilter` if the standalone numbers above hold.
4. Port the prune to PanGloss per the handoff (`HANDOFF-pangloss-final-template-prune.md`, session scratchpad).
