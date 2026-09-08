# HermitCrab optimization round, September 2026: final disposition

This is the final record for the count-driven round that followed
`hermitcrab-optimization-ledger.md`. Temporary counters were used to choose targets and were then removed;
the final branch carries only product changes, focused tests, the parity/timing harness, and these records.

## Measurement scope

The exploration used sequential parsing, one warm-up followed by repeated samples, and canonical analysis
signature sets as the parity gate. Deterministic work counts came from 45 Sena words (five known-heavy probes
plus the first 40 list entries) and 464 words from 33 conformance fixtures. Timing was never run concurrently.
The final branch-wide sample compares production commit `d7347226` (harness-only baseline tip `4f677735`)
with candidate code tip `7370e2dd`.

The probe counters are intentionally absent from the final diff. They put volatile gates in hot code across
two assemblies and would create permanent maintenance cost for a completed experiment. The reconstruction
design remains in `hermitcrab-probe-design.md`.

## Retained optimizations

Each retained mechanism has one record with its invariant, implementation, evidence, parity coverage,
portability, and limits:

| Optimization | Principal evidence | Record |
| --- | --- | --- |
| Deferred template clone | 1.054x on the 45-word Sena battery; exact parity | `optimizations/deferred-template-clone.md` |
| Feature-structure traversal-state reuse | equality/freezing allocation left the hot profile; not isolated | `optimizations/feature-struct-visited-state-reuse.md` |
| Frozen flat-tree equality | removes visited sets from the proved tree case; not isolated | `optimizations/feature-struct-frozen-tree-equality.md` |
| Frozen flat-tree clone | removes the copies map from the proved tree case; not isolated | `optimizations/feature-struct-frozen-tree-clone.md` |
| Single-pass `Shape.CopyTo` mapping | removes a second enumeration/dictionary build; not isolated | `optimizations/shape-copy-single-pass-map.md` |
| Copy-on-write shape sharing | clone 36% → 9% of profile; peak working set 11 GB → 6 GB | `optimizations/shape-sharing-on-clone.md` |
| Flat FST register arrays | `CreateInstanceMDArray` 11.5% → 0.2% of profile | `optimizations/flat-register-arrays.md` |
| Analysis edge-segment prefilter | 87% fewer FST traversals and 54% fewer arc checks on 45 Sena words | `optimizations/edge-segment-prefilter.md` |
| Copy-on-write syntactic feature structures | removes repeated clone/freeze/hash work measured at ~15% of a heavy Sena profile | `optimizations/syntactic-feature-structure-sharing.md` |

Only the deferred clone was timed alone. Profile movement and deterministic work removal justify the other
retained mechanisms; the five-language result below measures the branch as a package and must not be divided
among them.

## Rejected or removed experiments

| Experiment | Deciding evidence | Final state |
| --- | --- | --- |
| Two-pass nondeterministic traversal | 2.3x more traversal instances, only 1.6% fewer arc checks, 27% more clones | removed; `rejected-optimizations/two-pass-nondeterministic-traversal.md` |
| Final-template interleaving prune | zero prunes on Sena, Amharic, and Mbugwe because partial morphemes disable it | removed; `rejected-optimizations/final-template-prune.md` |
| `ExpandAlternatives` memo/hoist | word clones unchanged at 4,747,013; no retained difference from production ordering | removed; `rejected-optimizations/alternatives-memo-and-hoist.md` |
| Compounding split prefilter | only ~2% of clones and ~9% of FST traversals were in scope | not built; `rejected-optimizations/compounding-split-prefilter.md` |

All associated production switches, alternate traversal code, and research counters were deleted. The two
retained internal toggles exist solely for correctness/performance A/B of the edge prefilter and syntactic
feature-structure sharing.

## Five-language branch-wide sample

Each row used three fixed word-list indices and two reverse-ordered process rounds (baseline/candidate, then
candidate/baseline). Every process parsed sequentially, discarded one warm-up, and recorded three repetitions,
for six measured samples per arm. Times are the sum of each word's minimum sample across both rounds. Baseline
and candidate used identical grammar names and word keys; `scripts/hc-bench-compare.ps1` compared error
outcomes and rejected missing keys or signature differences. All ten arm comparisons passed parity, and the
saved outputs contained zero parse or load errors.

| Language | Word-list indices | Baseline ms | Candidate ms | Ratio | Interpretation |
| --- | --- | ---: | ---: | ---: | --- |
| Sena | 49, 52, 74 | 1,000.7 | 448.3 | 2.23x | directional: 0/3 probes clear both the 50 ms and spread rules |
| Amharic | 8, 9, 267 | 6,410.0 | 3,646.3 | **1.76x** | stable: 3/3 probes clear both rules |
| Indonesian | 0, 9, 44 | 99.0 | 56.0 | 1.77x | exploratory: 0/3 probes clear both rules |
| Mbugwe | 0, 65, 1272 | 8,979.4 | 4,901.6 | **1.83x** | stable: 3/3 probes clear both rules |
| Aweti | 2, 3, 27 | 134.2 | 54.4 | 2.47x | exploratory: 0/3 probes clear both rules |

The ratios are deliberately unpooled and are not extrapolated to whole corpora. An initially selected heavy
Sena trio did not complete the baseline within five minutes, and an Aweti sample containing index 0 crossed
two minutes; both whole testhosts were terminated and replaced symmetrically before any comparison. This is
sampling evidence, not a claim about those excluded words.

## Correctness gate

The minimized code tip was merged into the conformance integration branch and run through the complete
HermitCrab conformance-inclusive project:

- NUnit discovered and ran 604 non-explicit tests: 603 passed, zero failed.
- One symlink-security test was skipped because the Windows host lacks symlink privilege.
- Fixture manifests, DTD/schema checks, source/graph audits, 452 fixture rows, and 348 coverage items passed.
- The ordinary HermitCrab suite also passed with syntactic feature-structure sharing disabled.

## What remains

The edge prefilter leaves roughly 44% of remaining traversals successful, so another edge check has little
headroom. Compounding construction is too small on the measured heavy word. Alternatives and packed-readout
schemes remain bounded by the ordered pending trail. The next credible large change is a persistent/shared
trail representation that can avoid memo-replay and alternative-materialization clones without merging
linguistically distinct derivations; it is a data-structure redesign, not a safe incremental tweak.
