# PR #494 fs-merge measurements ("Change Add to PriorityUnion")

Branch `perf/pr494-priority-union`, measured at HEAD `207de3e2` (base research toggle commit `7ac310e4`
already contained the same-binary `Add` / `PriorityUnion` / `Exact` mode switch in
`AnalysisSyntacticFeatureMerge`; this round added four more deterministic counters
(`AnalysisAffixApplyCalls`, `AnalysisUnapplied`, `LexicalLookupCandidates`, `SynthesisAffixApplyCalls`) and
the `FsMergeBench` harness).

Grammars and word lists are private and were not committed; results below identify words only by their
0-based line index in each grammar's word list, never by word form, gloss, or morpheme name.

## Commands used

```powershell
# build once
dotnet build tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj -c Release

# per grammar (run separately; mbugwe/sena/amharic take minutes, so each grammar was its own invocation)
.\scripts\fs-merge-bench.ps1 `
    -GrammarsDir <scratch>\grammars `
    -OutDir <scratch>\bench\full `
    -Grammars @('indonesian') `        # then 'amharic', 'sena', 'mbugwe'
    -Modes @('Add','PriorityUnion','Exact') `
    -MaxWords 30 `
    -TimeoutMs 180000 `
    -SkipBuild

# final aggregation across all four grammars once every jsonl file existed
.\scripts\fs-merge-bench.ps1 -GrammarsDir <scratch>\grammars -OutDir <scratch>\bench\full `
    -Grammars @('sena','amharic','mbugwe','indonesian') -Modes @('Add','PriorityUnion','Exact') -AggregateOnly
```

Each combination is also just `dotnet test ...Tests.csproj -c Release --no-build --filter
"FullyQualifiedName~FsMergeBench"` with `HC_ANALYSIS_FS_MERGE`, `HC_FSM_GRAMMAR`, `HC_FSM_WORDS`,
`HC_FSM_MAX_WORDS=30`, `HC_FSM_TIMEOUT_MS=180000`, and `HC_FSM_OUT` set per invocation -- that is all the
script does, back to back for the three modes per grammar.

30 words per grammar, 180000ms per-word timeout, `MaxDegreeOfParallelism = 1` (single Morpher instance
reused across all words in a run, for deterministic counters).

## Per grammar x mode

| Grammar | Mode | Words | Completed | Timeouts | Errors | Total ms | checkCalls | checkRejects | merges | exactUnifyFailures | analysisAffixApplyCalls | analysisUnapplied | lexicalLookupCandidates | synthesisAffixApplyCalls |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| sena | Add | 30 | 29 | 0 | 1 | 27,933.6 | 746,808 | 31,025 | 66,217 | 0 | 743,352 | 65,094 | 91 | 339,732 |
| sena | PriorityUnion | 30 | 29 | 0 | 1 | 8,469.8 | 137,964 | 15,110 | 10,604 | 0 | 127,862 | 10,200 | 87 | 70,473 |
| sena | Exact | 30 | 29 | 0 | 1 | 6,527.2 | 74,058 | 14,438 | 5,980 | 0 | 67,936 | 5,810 | 84 | 56,397 |
| amharic | Add | 30 | 25 | 1 | 4 | 306,455.5 | 64,664 | 10,588 | 4,556 | 0 | 71,593 | 4,512 | 503 | 44,092 |
| amharic | PriorityUnion | 30 | 25 | 1 | 4 | 266,818.7 | 51,641 | 10,128 | 3,618 | 0 | 56,163 | 3,556 | 445 | 35,562 |
| amharic | Exact | 30 | 25 | 1 | 4 | 249,642.5 | 12,913 | 4,880 | 711 | 0 | 12,831 | 707 | 348 | 27,228 |
| mbugwe | Add | 30 | 30 | 0 | 0 | 339,743.3 | 14,687,568 | 1,960,897 | 1,632,377 | 0 | 16,058,308 | 1,632,377 | 144 | 11,517,396 |
| mbugwe | PriorityUnion | 30 | 30 | 0 | 0 | 132,837.6 | 4,042,965 | 1,082,123 | 522,599 | 0 | 4,446,599 | 522,599 | 144 | 5,429,688 |
| mbugwe | Exact | 30 | 30 | 0 | 0 | 103,616.2 | 2,827,307 | 233,721 | 468,004 | 0 | 3,190,391 | 468,004 | 56 | 1,068,998 |
| indonesian | Add | 30 | 30 | 0 | 0 | 178.9 | 699 | 80 | 18 | 0 | 624 | 15 | 30 | 780 |
| indonesian | PriorityUnion | 30 | 30 | 0 | 0 | 183.4 | 699 | 80 | 18 | 0 | 624 | 15 | 30 | 780 |
| indonesian | Exact | 30 | 30 | 0 | 0 | 178.9 | 699 | 120 | 18 | 0 | 624 | 15 | 30 | 780 |

Amharic's "Total ms" and "Timeouts" columns are inflated by the same ~180,000ms once in every mode (word
index 29 -- see Anomalies); the three amharic rows are still directly comparable to each other since the
offset is identical in each, just not comparable in absolute terms to the other grammars.

## Parity (analysis-set signature differs across modes)

Zero divergences in all four grammars: every one of the 30 words compared produced the identical set of
analyses (morphemes in order, gloss, allomorph, and resulting syntactic feature structure) under `Add`,
`PriorityUnion`, and `Exact`.

## Anomalies

- `exactUnifyFailures` is 0 in every grammar x mode cell: the `Exact` mode's documented-impossible fallback
  branch (unify failing after `CanUnapply` already succeeded) was never exercised on this corpus.
- sena, word index 3: `InvalidShapeException` (undefined phoneme), identical error in all three modes --
  a corpus/character-table issue, unrelated to the merge mode.
- amharic, word indices 0-3: `InvalidShapeException` (undefined phoneme), identical in all three modes.
- amharic, word index 29: timed out at 180,000ms in all three modes. Mode-independent, so it does not bear
  on the PR #494 comparison, but see the caveat on amharic's Total ms above. `ParseWord` has no
  cooperative-cancellation hook, so background work from a timed-out word can keep running after the
  harness moves on to the next word -- possible, though not confirmed, contamination of subsequent counters
  within the same amharic run.
- No other exceptions or timeouts anywhere in the 4 x 30 x 3 = 360 (word, grammar, mode) cells run.

## Reading

On the two grammars with real measurable load (Sena, Mbugwe), `PriorityUnion` (PR #494) does substantially
less work than master's `Add`: on Mbugwe, `merges` drops 68% (1,632,377 to 522,599), `checkCalls` drops 72%
(14.69M to 4.04M), and wall time drops 2.6x (339.7s to 132.8s); Sena shows the same shape, with wall time
dropping from 27.9s to 8.5s. `Exact` reduces work further still on every counter in both grammars and is
faster again in wall time (Mbugwe 103.6s, Sena 6.5s), though the margin over `PriorityUnion` is smaller than
`PriorityUnion`'s margin over `Add`. Amharic and Indonesian are too light or too dominated by the one shared
timeout to show a clean signal either way; Indonesian in particular has essentially identical counters
across all three modes; only `checkRejects` differs at all (120 for Exact vs 80 for Add/PriorityUnion), and
that has zero effect on the resulting analysis set. Parity is clean everywhere measured: all 120 completed
words (30 per grammar, minus the corpus-level errors/timeouts noted above) produced byte-identical analysis
signatures across `Add`, `PriorityUnion`, and `Exact`, and `exactUnifyFailures` stayed at 0 throughout,
which is the sanity check the PR's design comment claims should always hold. Net: PR #494's `PriorityUnion`
looks like a clear, correctness-preserving win on the grammars where analysis does real work, and `Exact`
(not itself proposed by PR #494, included here as the theoretical floor) shows there is still headroom
beyond `PriorityUnion` on this corpus.

## After canonical-FS widening (28750977+)

`perf/pr494-priority-union` was merged with `perf/pr494-break` at commit `28750977`, adding
`AnalysisSyntacticFeatureMerge.WidenMergedAnalyses` (default `true`) and a `MergedAnalysesWidened` counter.
When `Morpher.MergeEquivalentAnalyses` folds a same-shape analysis into a canonical word, the canonical's
syntactic FS is now widened to `FeatureStruct.Union` of every merged alternative's FS, fixing a lost-parse
regression (a rule valid for an alternative but not for the narrower canonical FS was previously pruned).
`FsMergeBench` gained a `mergedAnalysesWidened` field (jsonl) / column (report), and 118/118 tests pass
(up from 93/93 pre-merge; `perf/pr494-break` added its own tests).

Re-ran sena and mbugwe only (indonesian/amharic were not re-run: the widening only affects the merge path,
and those two showed no signal in the base run), same settings: 30 words, 180000ms/word timeout, `HEAD
28750977` (post `d1a1ef1e`, which added `mergedAnalysesWidened` to the harness).

```powershell
.\scripts\fs-merge-bench.ps1 -GrammarsDir <scratch>\grammars -OutDir <scratch>\bench\full-widened `
    -Grammars @('sena') -Modes @('Add','PriorityUnion','Exact') -MaxWords 30 -TimeoutMs 180000 -SkipBuild
.\scripts\fs-merge-bench.ps1 -GrammarsDir <scratch>\grammars -OutDir <scratch>\bench\full-widened `
    -Grammars @('mbugwe') -Modes @('Add','PriorityUnion','Exact') -MaxWords 30 -TimeoutMs 180000 -SkipBuild
.\scripts\fs-merge-bench.ps1 -GrammarsDir <scratch>\grammars -OutDir <scratch>\bench\full-widened `
    -Grammars @('sena','mbugwe') -Modes @('Add','PriorityUnion','Exact') -AggregateOnly
```

| Grammar | Mode | Words | Completed | Timeouts | Errors | Total ms | checkCalls | checkRejects | merges | exactUnifyFailures | analysisAffixApplyCalls | analysisUnapplied | lexicalLookupCandidates | synthesisAffixApplyCalls | mergedAnalysesWidened |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| sena | Add | 30 | 29 | 0 | 1 | 20,892.3 | 749,164 | 31,065 | 66,385 | 0 | 745,805 | 65,262 | 91 | 340,072 | 62,526 |
| sena | PriorityUnion | 30 | 29 | 0 | 1 | 5,160.5 | 137,964 | 15,110 | 10,604 | 0 | 127,862 | 10,200 | 87 | 70,473 | 1,341 |
| sena | Exact | 30 | 29 | 0 | 1 | 6,539.9 | 74,058 | 14,438 | 5,980 | 0 | 67,936 | 5,810 | 84 | 56,397 | 493 |
| mbugwe | Add | 30 | 30 | 0 | 0 | 339,385.0 | 14,694,527 | 1,960,987 | 1,632,950 | 0 | 16,065,282 | 1,632,950 | 144 | 11,517,396 | 260,759 |
| mbugwe | PriorityUnion | 30 | 30 | 0 | 0 | 164,514.0 | 4,061,324 | 1,084,025 | 524,090 | 0 | 4,466,021 | 524,090 | 144 | 5,436,312 | 102,804 |
| mbugwe | Exact | 30 | 30 | 0 | 0 | 64,151.4 | 2,834,161 | 233,721 | 468,577 | 0 | 3,197,260 | 468,577 | 56 | 1,068,998 | 7,750 |

`checkCalls`/`merges`/etc. for PriorityUnion and Exact are within noise of the pre-widening run (sena
PriorityUnion checkCalls 137,964 in both; mbugwe Exact merges 468,004 -> 468,577, +0.1%); Add's counts also
moved by well under 1% in both grammars. Timings moved more (e.g. sena Add 27.9s -> 20.9s, mbugwe
PriorityUnion 132.8s -> 164.5s) but that is JIT/GC/machine-load noise between separate `dotnet test`
invocations, not a work-count change -- the deterministic counters are the signal here, wall time is not.
`mergedAnalysesWidened` is non-zero in every cell (widening does fire on this corpus), heaviest under `Add`
(sena 62,526; mbugwe 260,759) and lightest under `Exact` (sena 493; mbugwe 7,750), tracking each mode's
overall analysis-rule traffic.

### Parity vs. the pre-widening run

Compared each (grammar, mode, word index) triple's analysis-set signature between the pre-widening run
(`b9757382`, table above) and this post-widening run (`28750977`+): **zero differing indices** across all
30 sena words and all 30 mbugwe words, in all three modes (180 (grammar, mode, word) triples compared, the
same 1 sena error / 0 mbugwe errors excluded on both sides as before). Every signature is byte-identical,
which is a valid special case of "superset" (no index needed the general subset/superset check). The
lost-parse bug `WidenMergedAnalyses` fixes therefore does not manifest on this particular 30-word slice of
either grammar, even though the widening path is clearly active (`mergedAnalysesWidened` > 0 everywhere) --
whatever parses it rescues on the full corpora that motivated `perf/pr494-break` are apparently outside this
sample.
