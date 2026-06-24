# HermitCrab Parser — Throughput Optimization Plan

Branch: `hc-optimizations`

## Status (2026-06-24)

- **Phase 0 — DONE.** `MorpherBenchmark.cs` (`[Explicit]`, not in CI). Loads
  `samples/data/en-hc.xml` + WEB 1–3 John, reports work-reduction + timings.
  Measured: **2,961 tokens → 439 distinct (85.2% cache-hit)**; dedup cuts parses
  6.7×; all three modes produce identical results.
- **Phase 1 — DONE.** `Morpher.AnalyzeWords(...)` batch API (dedup + parallel, with
  `maxDegreeOfParallelism`). Tests: batch == per-word with duplicates/unknowns; result
  **stable across 200 repeated parallel runs** (race check green). Full suite: 63/63.
- **Phase 2 — DEFERRED** (runtime parallelism knob). Not needed yet: capping the outer
  degree handles oversubscription, and .NET's shared ThreadPool degrades gracefully
  under nesting. Do only if real-grammar measurement shows within-word parallelism
  hurting corpus throughput.

## Real-grammar profiling (Sena) + allocation work — 2026-06-24

Generated the **real Sena grammar** via FieldWorks' own `GenerateHCConfig.exe`
(`sena-hc.xml`, 1.35 MB) and extracted **6,456 wordforms** from the project. Profiled the
machine library against it (benchmark now reports `GC.GetTotalAllocatedBytes` + Gen0/1/2).

**Killer measurement (real grammar, single-threaded, short-word sample):**
- **8,793 `Word.Clone` per word** (toy grammar: 2.3 — toy was wildly misleading)
- **371 MB allocated per word**; 669 Gen0 + 56 Gen1 + 1 Gen2 collections for 23 words
- The combinatorial unapplication search creates ~8.8k intermediate Words/word, each
  deep-cloning its shape (per-node `FeatureStruct.Clone`). This is the GC firehose that
  caps FieldWorks parallel scaling at ~3× (collector can't keep up; Gen2 firing).

**Implemented allocation win — `Shape.CopyTo` inline mapping** (removed per-clone
`.Zip().ToDictionary()` + double re-enumeration; `Shape.cs`): **−2.3% allocation/word**
(379,936 → 371,207 KB), Gen0 685 → 669. Safe (62 HC + 790 core tests pass). Confirms the
bulk is in per-node `FeatureStruct.Clone` + FST traversal, not the clone plumbing.

**Note:** `MaxUnapplications` does NOT bound the search — it truncates output *after* the
cascade fully runs, so pathological words still blow up (8+ GB). Bounding the search itself
is a separate concern.

## Machine-level work — IMPLEMENTED (2026-06-24)

**1. Single-threaded runtime option (for FieldWorks to parallelize across words).**
`new Morpher(traceManager, lang, maxDegreeOfParallelism: 1)` now makes the morpher fully
sequential within a word — analysis cascade (`CombinationRuleCascade`), affix-template
unapplication (`ApplySlots`), and synthesis all run serially. The compile-time
`SINGLE_THREADED` flag (never defined in any build) was replaced by this runtime knob:
`Morpher.MaxDegreeOfParallelism` (default `Environment.ProcessorCount`; `1` = single
threaded). Touched `Morpher.cs`, `AnalysisStratumRule.cs`, `AnalysisAffixTemplateRule.cs`.
Test `AnalyzeWord_SingleThreaded_MatchesParallel` asserts identical results. FieldWorks
should construct its bulk-parse morpher with `maxDegreeOfParallelism: 1`.

**2. Allocation/phase instrumentation (to trace small speedups).**
`MorpherStatistics` (opt-in, `Enabled=false` by default → zero overhead) counts
`Word.Clone` calls and splits analysis vs. synthesis wall time. Surfaced in the benchmark.

**Measured on en-hc.xml + WEB 1–3 John (439 distinct forms):**
| mode | corpus time |
|---|---|
| default (within-word parallel), serial corpus | 203 ms |
| single-threaded, serial corpus | **88 ms** |
| single-threaded, parallel across words (FieldWorks target) | **20 ms** |

→ within-word parallelism is *net overhead* for typical words; single-threaded + across-word
parallel is ~10× the current default. Instrumentation: **analysis = ~91% of per-word time**
(59.9 ms vs 5.8 ms synthesis); `Word.Clone` ≈ 2.3/word. **Next target: analysis-phase
allocations.** (Toy grammar — ms is noisy; clone count + phase split are the real signal.)

## FieldWorks experiment results (2026-06-24) — confirms the next target

FieldWorks implemented the across-word parallel plan with `maxDegreeOfParallelism: 1`
morphers. Findings:
- **Time is ~entirely in the morpher.** Filing ≈ 1%; the LCM read lock is essentially
  free. The serial write-back was correctly left serial; batching UOWs would buy nothing.
- **Scaling ceiling ≈ 3×**, and it's *inside the machine library*. Under 20-way
  concurrency, summed morpher CPU inflated ~5× (1,161 s → 6,150 s) — the signature of
  **GC / memory-bandwidth pressure from per-parse allocation**. Giving each thread its own
  Morpher made it *worse* → this is NOT lock contention on the shared grammar (that would
  be relieved by per-thread instances); the shared frozen grammar is the right design, and
  the bottleneck is transient per-parse allocation + working-set/bandwidth.
- **Server GC ~doubled the benefit** (1.56× → 3×). FieldWorks.exe uses workstation GC;
  enabling `<gcServer enabled="true"/>` is the biggest app-side lever (app-wide → their call).
- FieldWorks capped concurrency at `Min(ProcessorCount-1, 4)` (~2.4×, ~85% of the gain) for
  UI responsiveness; overridable via `ParserMaxConcurrency`.

**Implication for machine:** reducing per-parse allocation is no longer just a constant
factor — it is what raises the parallel-scaling ceiling above 3× AND lessens the need for
Server GC. This is exactly optimization #2, now the priority. The instrumentation shows
analysis = ~91% of per-word time, so the allocation lives in the **analysis phase**. Next
step (per review): *attribute* allocation within analysis (profiler Gen0-by-type, or a few
more counters — cascade `HashSet<Word>` inserts, `.Distinct` calls, FeatureStruct
unifications) BEFORE trimming, so we target the real allocator rather than the one counter
we happened to add (`Word.Clone` ≈ 2.3/word is low and may not be the cost).

## Goal

Make parsing a **corpus** (e.g. a book of the Bible) substantially faster, without
changing parse results. This plan targets **throughput**, not single-word latency.
The exponential cost of the unapplication search (the "30-minute word") is *not*
addressed here — that needs an algorithmic change and is out of scope.

## Why throughput, not big-O

`Morpher.ParseWord` is dominated, per word, by the morphological-rule unapplication
search (`CombinationRuleCascade` / `PermutationRuleCascade`), which is exponential in
the number of applicable rules. Threads buy at most a constant (≤ core count) factor,
so they cannot tame the worst case. But for a *corpus* the win is real and large:

1. **Most tokens repeat.** A Bible book has thousands of tokens but far fewer distinct
   surface forms. Parsing is pure per surface form, so identical forms should be parsed
   once and cached.
2. **Distinct forms are independent.** They can be parsed concurrently against one
   shared, immutable `Morpher`.

## Thread-safety basis (why concurrent parsing is sound)

After construction the `Morpher` is effectively immutable at parse time:

- Compiled rules and every `Matcher`'s FST are **frozen** (`Matcher.Compile` →
  `_fsa.Freeze()`); the `RootAllomorphTrie` FST likewise.
- `Fst.Transduce` allocates a **fresh traversal method and all working buffers per
  call** (`Fst.cs:320–391`); the shared FST is read-only during traversal.
- `TraceManager` is stateless; it writes trace nodes into per-parse `Word` objects.
- **Existing proof:** the default build already runs `Parallel.ForEach` over
  `_synthesisRule.Apply(...)` against these same shared FSTs *inside one parse*
  (`Morpher.Synthesize`, `ParallelCombinationRuleCascade`). Concurrent `ParseWord`
  just lifts that established pattern up one level.
- **Verified (code-read):** `TraversalMethodBase` is instantiated **per `Transduce`
  call** with all working buffers (`_annotations`, `_cachedInstances`, registers)
  per-call (`Fst.cs:320–391`); it only *reads* the shared FST graph. The one lazy-init
  race that mattered — `FeatureStruct._hashCode` — is closed because `Freeze()`
  **eagerly computes and stores the hash** (`FeatureStruct.cs:1197–1202`), so
  `GetFrozenHashCode()` on a shared frozen struct is a pure read. Analysis-phase
  matching uses the `Nondeterministic` traversal path (the path *not* previously
  exercised concurrently), so this is confirmed by a **repeated-run stability test**
  (below), not by inspection alone.

**No locks** are needed on the parse path. The only synchronization we introduce is a
`ConcurrentDictionary` cache. Constraints: don't mutate `Morpher` config
(`MaxStemCount`, `MaxUnapplications`, `LexEntrySelector`, `RuleSelector`) while parses
are in flight; don't enable tracing for batch runs.

## Nested-parallelism hazard

The default build is **already parallel within one word**. Adding `Parallel.ForEach`
across words on top of that creates nested parallelism → thread-pool oversubscription,
context-switch thrash, and multiplied peak memory. Phases are ordered to measure and
then resolve this fork.

---

## Phase 0 — Benchmark harness (do first; nothing is provable without it)

A repeatable measurement so every later change is justified by numbers.

- New console/bench entry that:
  - loads a grammar (`XmlLanguageLoader.Load`), compiles a `Morpher`;
  - reads a token list from a corpus (reuse the USFM corpus reader for the Bible text);
  - parses, reporting: total tokens, distinct tokens, wall-clock, tokens/sec, and a
    checksum of results (sorted analyses) so correctness can be compared across runs.
- Modes: `sequential`, `cached-sequential`, `cached-parallel` (with degree N).
- **Report deterministic work-reduction, not just wall-clock:** total tokens, distinct
  tokens, cache-hit rate, and **parse-count** (number of actual `ParseWord` calls).
  Dedup's win is provable as parse-count reduction *regardless of grammar speed* — this
  is the grammar-independent proof the mechanism works even when the toy grammar makes
  wall-clock pure noise.
- **Acceptance:** identical result checksum across all modes, **stable across many
  repeated parallel runs** (races are nondeterministic; one matching run is not proof);
  timings + work-reduction metrics printed.

## Phase 1 — Result caching + batch API (headline win, additive, low risk)

Add a batch method that dedups identical surface forms and parses distinct forms
concurrently against the shared `Morpher`. Additive — does not touch the existing
single-word parse path.

Proposed surface (final shape TBD during impl):

```csharp
// On Morpher (or as an extension), returns analyses keyed by input form.
IReadOnlyDictionary<string, IReadOnlyList<WordAnalysis>> AnalyzeWords(
    IReadOnlyCollection<string> words,
    int? maxDegreeOfParallelism = null);
```

- Backed by `ConcurrentDictionary<string, IReadOnlyList<WordAnalysis>>`.
- Parallelize across **distinct** forms with `Parallel.ForEach` +
  `MaxDegreeOfParallelism`.
- Per-word `InvalidShapeException` handled per existing `AnalyzeWord` (empty result),
  not fatal to the batch.
- **Acceptance (TDD):** results identical to looping `AnalyzeWord` per token; cache
  causes each distinct form to parse exactly once; benchmark shows speedup on the
  corpus.

## Phase 2 — Runtime parallelism control (deferred; do only if Phase 1 data shows oversubscription hurting)

Convert the compile-time `SINGLE_THREADED` flag into a runtime
`Morpher.MaxDegreeOfParallelism` (or `bool ParallelizeWithinWord`) so one binary can
run **across-word parallel + sequential-per-word** (best for corpus throughput)
without recompiling. This is the invasive part — it touches the core parse path
(`Synthesize`, `ParallelCombinationRuleCascade`, `AnalysisAffixTemplateRule`) which
currently branches on `#if SINGLE_THREADED` and even changes data types
(`ConcurrentQueue<Word>` vs `IEnumerable<Word>`). Gate this on measured need.

---

## Test data — what we have and its limits

**In-repo (sufficient for correctness + relative speedup, not for absolute realism):**

- Grammars: `samples/data/en-hc.xml`, `samples/data/sp-hc.xml`. These are **toy sample
  grammars** (~9 KB, 2 strata). They parse each word in microseconds, so threading
  overhead may swamp the signal and they will **not** reproduce the exponential
  pathology.
- Corpus: `samples/data/WEB-PT/` (World English Bible, USFM) contains **1–3 John only**
  (~3,600 tokens total). Real English Bible text, readable with the existing USFM
  corpus reader, but small.

**What's missing for realistic numbers:**

- A **realistic grammar** — a FieldWorks/FLEx-exported HermitCrab grammar with many
  strata, rules, and lexical entries. The toy grammars won't show meaningful per-word
  cost. **Action needed: the user should point us at a real exported grammar** (this is
  how HermitCrab is used in production via FLEx/Serval).
- A **larger corpus** — a full NT or OT book in the grammar's language. If a real
  grammar is for another language, supply matching Scripture text (USFM/Paratext).

**Recommendation:** build the harness against `en-hc.xml` + WEB 1–3 John to validate
correctness and the *mechanism* of speedup, then re-run against a real grammar +
full book once provided to get production-meaningful numbers.

## What could break (watch-list)

- **Oversubscription**: nested parallelism slower than expected — resolved by Phase 2 /
  degree caps.
- **Memory**: many concurrent parses multiply peak memory; one pathological word pins a
  thread and a large `HashSet<Word>`. Set `MaxUnapplications` for batch runs.
- **Exception semantics**: nested `Parallel.ForEach` wraps in `AggregateException`;
  batch layer must decide per-word failure vs. fail-batch.
- **Tracing**: must stay off for batch (interleaved shared trace trees are corrupt).
- **Result ordering**: already set/bag-based and unordered; tests must compare as sets.

## FieldWorks / liblcm integration findings (refines the above for FLEx)

Reading `FieldWorks/Src/LexText/ParserCore/*` and `liblcm`:

- **FLEx already dedups.** `WfiWordformRepository` keys wordforms by surface form
  (`m_wordformFromForm`, `GetMatchingWordform(ws, form)`); each form is one
  `IWfiWordform`. "Parse All Words" iterates *distinct* forms already → **the
  `AnalyzeWords` dedup win is already captured upstream in FLEx.** (Still useful for
  Serval/other batch callers that parse raw token streams.)
- **The parser is single-threaded at the FLEx level.** `ParserScheduler` runs one
  `ConsumerThread`; "Parse All Words" enqueues every wordform and that one thread
  processes them sequentially (`ParserWorker.ParseAndUpdateWordform`). The machine
  library's *within-word* parallelism runs on that single thread.
- **The expensive parse is LCM-lock-free.** `HCParser.ParseWord` calls
  `m_morpher.ParseWord(...)` (eager) *outside* the `WorkerThreadReadHelper`, against the
  compiled-grammar snapshot built in `LoadParser` — not the live cache. Only the short
  `GetMorphs` mapping holds an LCM read lock.
- **LCM permits concurrent reads.** `UnitOfWorkService` uses `ReaderWriterLockSlim`;
  `BeginReadTask`=`EnterReadLock`. Many parse threads can read concurrently; writes are
  exclusive. Risk is a write-convoy (filing waits for readers), **not deadlock** (no
  nested lock held).
- **Filing is already serial + decoupled.** `ParseFiler.ProcessParse` enqueues to the
  UI-thread `IdleQueue` (thread-safe; applied in a `NonUndoableUnitOfWork` write lock).

### What governs the win: the parse:file ratio (Amdahl)

"Parse All Words" time = parallelizable parse + **serial filing**. If filing is a large
fraction of per-word cost, parallelizing parse alone caps low (e.g. ~2.5–3×). Measure
the split via `result.ParseTime` (filing = remainder). The ratio chooses the lever:
parse-dominant → parallelize parse across wordforms; file-dominant → **batch many
results into one UOW** to amortize filing/PropChanged overhead.

### What needs updating in FieldWorks (analysis only — not done here)

1. **Parallelize the bulk-parse path** across queued wordforms (e.g. `Parallel.ForEach`
   inside a batch work item, or N consumer threads), each calling `m_parser.ParseWord`
   then `ProcessParse`. Keep `TryAWord` and filing as-is.
2. **REQUIRED before parallelizing:** `HCParser.ParseToXml` (trace path) **mutates**
   `m_morpher.LexEntrySelector`/`RuleSelector`. Safe today only because the single
   `ConsumerThread` serializes trace vs. bulk. Under parallelism a `TryAWord` trace can
   mutate shared morpher state mid-parse. Fix: give trace its own `Morpher`, or thread
   the selectors through as call parameters instead of mutable fields.
3. Possibly **batch filing** into fewer UOWs if measurement shows filing dominates.

### Reusable contribution from this branch for FLEx

Not `AnalyzeWords` (off FLEx's path), but the **thread-safety verification + 200×
stability test** — that is the precondition that licenses concurrent `ParseWord` on a
shared `Morpher`. Caveat: the stability test ran on the toy grammar with the *default*
config, not FLEx's (`guessRoots=true`, `MergeEquivalentAnalyses=true`); the reasoning
still holds (LexicalGuess builds local objects; merge uses per-`Apply` state) but the
empirical evidence is from a different config.

## Open questions for the user

- **What is the parse:file ratio** on a real project (from `result.ParseTime` vs. total)?
  It sets the ceiling and picks the lever (parallel-parse vs. batch-file).
- **Can you provide a real FLEx-exported grammar + a full book** in that language? The
  in-repo toy grammar proves the mechanism but not production-meaningful speed.

## Verification

- All existing HermitCrab tests pass unchanged.
- New tests: batch == sequential (as sets); cache hit-count correctness.
- Benchmark: identical result checksum across modes; report speedup.
