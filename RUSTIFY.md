# RUSTIFY.md — Getting Rust's memory architecture into C# HermitCrab

> **Thesis.** Rust's speed advantage over C# on this workload is ~80% *memory
> architecture* (stack/value-by-default, no GC, cache locality) and ~20% codegen.
> Architecture is portable: we can adopt Rust's data-oriented memory model **in C#**
> (pooling, struct-of-arrays, `Span<T>`, indices-not-pointers) and capture the large
> majority of the theoretical Rust ceiling — keeping **one engine, no native library,
> no per-RID packaging, and no dual-maintenance drift.** This document is the piece-by-piece
> plan to do that, with a measurement after every change.

## Why (the measured root cause)

Profiling the HermitCrab parser against the real **Sena** FLEx grammar showed it is
**allocation/GC-bound**, not compute-bound: ~371 MB and ~8,800 `Word.Clone` calls *per word*.
Two consequences follow directly:

1. Per-word latency is dominated by allocation + Gen0 collection, not by the FST math.
2. Parallel ("Parse All Words") throughput plateaus around ~2.8× regardless of core count,
   because every concurrent parse allocates from the **shared** managed heap — and Gen0
   allocation/collection is a process-wide synchronization point. The GC *is* the ceiling.

So the same fix — **stop churning the heap** — lowers single-word latency *and* removes the
parallel-scaling ceiling. The allocation win and the multithreading win are the same lever.

## Target & how close we expect to get

| Workload shape | Rust vs idiomatic C# | Rust vs data-oriented C# |
|---|---|---|
| Allocation/pointer-chasing heavy (**HermitCrab today**) | ~3–10× | **~1.2–2×** |

Because the bottleneck is allocation/GC (not raw compute), HermitCrab sits in the quadrant C#
can close *most*. Goal: land within ~1.2–2× of a hypothetical Rust port while staying pure C#.
If a hard floor remains after Phases 1–4, Phase 6 decides whether a Rust kernel spike is worth it.

## Non-goals & guardrails (read before touching `SIL.Machine`)

`SIL.Machine` is a **published, general-purpose library**. `Word`, `Shape`, `ShapeNode`,
`Annotation`, and `FeatureStruct` are used far beyond HermitCrab (Translation, Corpora,
SequenceAlignment, …). Therefore:

- **No public API/semantic change.** Pooling/arena behavior is opt-in and morpher-scoped
  (or internal). A consumer that does not opt in sees identical behavior.
- **Byte-identical output.** Every phase must keep the analysis set identical. Enforced by the
  existing concurrent-determinism test plus an analysis-signature snapshot (see harness).
- **Thread-safety is non-negotiable and must *improve*.** Pools/arenas are **per-thread or
  per-parse**, never a shared global pool. The frozen grammar stays immutable and shared (read).
- **One change at a time, measured, reversible.** Each piece is its own commit with before/after
  numbers recorded below. If a piece doesn't move the metric (or risks correctness), it is reverted.

## Measurement methodology

**Harness:** `RustifyBenchmark` (`[Explicit]`, not in CI) loads `samples/data/en-hc.xml` and the
in-repo WEB-PT corpus, de-duplicates tokens, and parses the set in a loop. Point `HC_GRAMMAR`
/ `HC_WORDS` at a FLEx-exported Sena grammar for production-scale numbers; the in-repo toy grammar
is enough for the **allocation** signal, which is grammar-independent and low-noise.

**Primary metrics (low-noise, deterministic):**
- **Bytes allocated / word** — `GC.GetTotalAllocatedBytes(precise:true)` delta ÷ words. *The* headline.
- **`Word.Clone` / word** — `MorpherStatistics` counter. Deterministic; the clone-churn proxy.
- **Gen0 / Gen1 / Gen2 collections** over the run.

**Secondary metrics (directional; noisier on the toy grammar):**
- Single-thread wall time / word.
- 16-thread throughput (words/sec) and **scaling factor** vs single-thread — the parallel-ceiling proxy.

**Protocol:** warm up (one full pass, discard), then N timed passes; report median. Same machine,
same build config (Release), same word set. Record results in the table below after every phase.

**Correctness gate (must pass after every piece):**
- `dotnet test SIL.Machine.Morphology.HermitCrab.Tests` (63 HC tests) — green.
- `AnalyzeWord_ConcurrentRepeatedParsing_IsDeterministic` — green.
- Analysis-signature snapshot for the WEB-PT set unchanged from baseline.

## Results (filled in as we go)

Harness: `MorpherBenchmark` (`[Explicit]`), `samples/data/en-hc.xml` + WEB-PT corpus, 439 distinct
forms, Release, 20 cores, Workstation GC. Run:
`dotnet test ...HermitCrab.Tests --filter FullyQualifiedName~MorpherBenchmark --logger "console;verbosity=detailed"`

| Phase | Change | KB/word | Word.Clone/word | Gen0 | ST serial (ms) | across-4 (ms) / scaling | Notes |
|------:|--------|--------:|----------------:|-----:|---------------:|------------------------:|-------|
| 0 | **baseline** | 106.5 | 2.3 | 3 | 91 | 27 / 3.4× | toy grammar: clones low; KB/word is the headline |
| 2 | **COW `FeatureStruct`** | **84.8** | 2.3 | 2 | 79 | 23 / 3.4× | **−20% bytes/word**, Gen0 3→2, ST −13%; 63 HC tests green |
| 1/4 | Pool `Shape.CopyTo` map | 84.8 | 2.3 | 2 | 79 | 23 / 3.4× | per-thread reuse of the per-clone node map; tiny on en-hc, −0.45% on Sena (see below) |

**Sena confirmation (real grammar, `SenaQuick` probe, 20 s budget, maxUnapp=5, shortest-first):**

| Build | words parsed in 20s | KB/word | Gen0 |
|------|--------------------:|--------:|-----:|
| no-COW (baseline) | 2,548 | 13,961 | 2,787 |
| **COW** | **2,789** | **11,997** | 2,621 |

→ **−14% bytes/word and +9.5% throughput on Sena** (more words parsed in the same budget),
consistent with en-hc's −20% and the original −11%.

### Parallel throughput — the 16-thread answer (`SenaParallel`, 800 Sena words, one shared serial-within-word morpher)

| threads | **Workstation GC** words/sec (scaling) | **Server GC** words/sec (scaling) |
|--------:|---------------------------------------:|----------------------------------:|
| 1 | 130 (1.0×) | 138 (1.0×) |
| 4 | 441 (3.4×) | 784 (5.7×) |
| 8 | 460 (3.6× — peak) | 1,120 (8.1×) |
| 16 | 407 (**3.1× — regresses**) | **1,424 (10.3×)** |

**After FST-traversal pooling (Server-GC-gated):** reusing a per-thread traversal method + its
instance free-list across `Transduce` calls (Phase 1/FST) cuts allocation **−16% (7,026→5,891 MB)**,
**halves Gen0 (88→42)**, and lifts **16-thread Server-GC scaling to 11.2×** (1,412 w/s). It is gated
to Server GC: under Workstation GC the larger *live* working set causes stop-the-world Gen2 pauses
that **regress** parallelism, so Workstation keeps the allocate-per-call path (3.16× unchanged).

| 16 threads | Workstation GC | Server GC (before) | **Server GC (pooled)** |
|---|---|---|---|
| scaling | 3.16× (capped) | 10.3× | **11.2×** |
| allocation (800w) | 7.2 GB | 7.0 GB | **5.9 GB (−16%)** |
| Gen0 | 582 | 88 | **42** |

This is the empirical proof of the whole thesis:

- **Under Workstation GC the parallel ceiling is ~3.5× and 16 threads is *slower* than 8** — the
  shared collector saturates (gen0 ≈ 580 regardless of thread count). Allocation *is* the ceiling.
- **Server GC scales to 10.3× at 16 threads** (gen0 ≈ 88), i.e. **~11× vs single-thread
  Workstation**. The out-of-process Server-GC worker (PR #438) delivers exactly this **without the
  host changing its GC mode**.
- COW's allocation cut compounds (less for the collector to chase); **Phase 1 pooling is what lifts
  the *Workstation* curve** (for in-process callers that can't switch GC mode).

> **Note on ordering:** Iterating on the fast in-repo toy grammar (`en-hc`, sub-second) per the
> "small samples first" rule; the real Sena grammar (`sena-hc.xml`, generated via FieldWorks
> `GenerateHCConfig`) is reserved for validating a proven change, because its pathological
> unapplication explosions make it far too slow for the inner A/B loop. COW (Phase 2) was taken
> first because it was already implemented + proven (−11% on Sena previously) and is a single-file,
> low-risk, thread-safe change — a fast confidence-builder before the larger pooling work (Phase 1).
| 1a | Pool `Word` | | | | | | |
| 1b | Pool `Shape`/`ShapeNode`/`Annotation` | | | | | | |
| 1c | Pool FST `Register`/traversal | | | | | | |
| 2 | `FeatureStruct` COW / pooling | | | | | | |
| 3 | SoA + indices + struct values | | | | | | |
| 4 | Kill hot-loop LINQ / `stackalloc` | | | | | | |
| 5 | NativeAOT worker (opt) | | | | | | |

### Measurement finding (important)

The real Sena grammar (33k-line `sena-hc.xml`) is **too slow for a per-piece A/B loop** in this
harness: the multi-pass `MorpherBenchmark` over even ~25 Sena words exceeds 150 s, because the
allocation/unapplication explosion we are fixing *is* the slowness. Consequences for the workflow:

- **Inner loop runs on `en-hc`** (sub-second) — it cleanly measures *per-parse fixed* allocation
  (cascade, FST traversal, segmentation, synthesis). This is where most pieces land.
- **`en-hc` under-measures clone-count wins** (toy grammar = 2.3 clones/word vs Sena's ~8,800), so
  pieces that target the *clone explosion* (Phase 1 pooling, Word-ctor allocation) need a dedicated
  **single-pass** Sena harness (≤20 cheap words, `MaxUnapplications`≤5) — a TODO before Phase 1.
- COW (Phase 2) was independently measured at **−11% on Sena** by the original author, consistent
  with the **−20% on en-hc** here, so its win is credible at scale.

## The plan, piece by piece (ordered by ROI × safety)

### Phase 0 — Baseline harness + correctness snapshot
- Add `RustifyBenchmark` reporting the standardized metric block above.
- Capture the baseline row and the analysis-signature snapshot.
- **Exit:** reproducible baseline numbers committed; correctness gate green.

### Phase 1 — Per-thread pooling of the hot objects *(biggest ROI; the clone killer)* — PARTIAL
The 8,800 clones/word are `Word`, its `Shape`/`ShapeNode`/`Annotation` graph, and the FST
traversal `Register`s. Replace per-parse heap churn with **per-thread reuse**.

> **Status:** the per-clone `Shape.CopyTo` node map is now pooled per-thread (1c-adjacent, done,
> small). Full pooling of `Word`/`ShapeNode`/`Annotation` (1a/1b) is **scoped but not landed** — it
> is a large, lifetime-sensitive refactor (these objects escape into the cascade's result sets and
> live to the end of the parse, so recycling needs per-parse ownership tracking). The `SenaQuick`
> harness now makes it measurable; see the decision gate for why it is the recommended next chunk.

- **1a. Pool `Word`.** Rent at parse start from a `ThreadLocal`/per-call pool; reset on return;
  **copy the small result (morpheme ids + ranges) out before recycling** (no use-after-recycle).
- **1b. Pool `Shape`/`ShapeNode`/`Annotation`.** The bulk of the graph; reset = mark-free.
- **1c. Pool FST `Register`/traversal instances** (`FiniteState/Register.cs`, `TraversalInstance.cs`).
- **Thread-safety:** per-thread only; one interlocked op per *word* at most, never per allocation.
- **Hypothesis:** bytes/word ↓ large; `Word.Clone`/word ↓ toward 0 churn; **16T scaling rises
  above ~2.8×** (shared-heap contention removed).
- **Risk:** stale pooled state → mitigated by disciplined reset + determinism/snapshot tests.

### Phase 2 — Reduce `FeatureStruct` allocation — DONE ✅
- Reintroduce **copy-on-write `FeatureStruct`** (previously measured ~−11% on Sena, pulled from
  PR #438 to keep it focused) and/or pool feature values. Re-evaluate now as its own change.
- **Hypothesis:** bytes/word ↓ on top of Phase 1.
- **Risk:** COW + concurrency was the original concern; per-parse arena from Phase 1 makes the
  safe version simpler (copy into the arena; no shared mutable inflation).

### Phase 3 — Cache locality (the Rust "data-oriented" core)
- **3a.** `ShapeNode` graph → **indices into a contiguous array** instead of object references (SoA).
- **3b.** `FeatureStruct` values / FST `Register`s → **`struct`** where viable.
- **3c.** Apply `StructureOfArraysGenerator`-style layout to the hottest node arrays.
- **Hypothesis:** ST ms/word ↓ at fixed allocation (fewer cache misses); compounds with Phase 1.
- **Risk:** larger refactor; gated behind the same byte-identical snapshot.

### Phase 4 — Remove hot-loop allocation
- Replace LINQ (`.Select`/`.Where`/`.ToArray`/`.ToList`) in the analysis/synthesis cascades and FST
  traversal with manual loops + pooled buffers; `stackalloc`/`Span<T>` for small transient buffers.
- **Hypothesis:** bytes/word ↓ (eliminates iterator + temp-array allocations).

### Phase 5 — Out-of-process Server-GC worker — REMOVED ✗
The `SIL.Machine.Morphology.HermitCrab.Server` project (worker host + `HermitCrabServerClient` +
protocol + tests) has been **deleted**. It was a workaround for the in-process Workstation-GC
ceiling (Server GC in a separate process → ~100 MB worker, .NET-10-runtime deployment, a richer
protocol + FW adapter to build). The RUSTIFY direction makes it unnecessary: **drive allocation low
enough in-process** (COW + bit-packed unify + future arena work) that the plain .NET process scales
without Server GC. Server GC remains available as a one-line `runtimeconfig` flag if ever wanted,
but the bespoke client/server architecture is gone.

### Phase 6 — Decision gate vs the Rust ceiling
- Compare final C# numbers to the ~1.2–2× Rust estimate. If a hard floor remains and the residual
  matters, scope a **Rust FST-kernel spike** (the ~13k-LOC engine boundary, one-call-per-word ABI)
  and benchmark it before committing. Otherwise: done — Rust's win was captured in C#.

## Per-piece workflow
1. Implement the single change.
2. Run `RustifyBenchmark`; record the row above.
3. Run the correctness gate (HC tests + determinism + signature snapshot).
4. Keep + commit with the numbers in the message, **or** revert if it didn't pay or risked correctness.
5. Next piece.

## Risks & rollback
- **Public-API breakage:** keep pooling internal/opt-in; run the full `SIL.Machine` test suite, not
  just HC, before merging structural changes.
- **Correctness drift from pooling:** the byte-identical snapshot + determinism test are the backstop.
- **Diminishing returns:** the table makes a dead phase obvious — revert and skip to the next lever.

## Phase 3 — Bit-packed feature vectors (the per-arc matching hot path)

**Finding.** The 80% "FST/cascade" allocation is dominated by `Input.Matches` → `FeatureStruct.IsUnifiable`, called thousands of times/word. Each call (a) **clones `VariableBindings`** (`definiteVarBindings = varBindings?.Clone()`) and (b) **walks the feature dictionary** with deref + virtual dispatch. Per-feature values are *already* `ulong` bitsets (`UlongSymbolicFeatureValueFlags`, `FeatureSymbol.Index`); what's missing is a **whole-struct flat vector** so unifiability is a few word-ops with no dictionary walk and no varBindings clone.

**Design (faithful: fast path + guaranteed fallback).**
- At `FeatureSystem.Freeze`, assign each `SymbolicFeature` (≤64 symbols) a dense `FlatIndex`.
- Lazily cache on each *frozen* `FeatureStruct` a `ulong[]` flat vector: `bits[FlatIndex] = allowed-symbol bits` for present features, `~0` (unconstrained) for absent. Mark the FS **Simple** iff every feature is a non-variable `SymbolicFeatureValue` with a ulong flag set (else **Complex**).
- **Fast unifiable** (no varBindings, no defaults, no negated FS): both Simple ⇒ `for each i: (a[i] & b[i]) != 0`. This is provably identical to `IsUnifiable(useDefaults:false)` for the simple/no-variable case (absent = `~0` = the "no constraint" branch), and needs **no varBindings clone** (Simple ⇒ no variables ⇒ nothing binds).
- `Input.Matches` uses the fast path when `unification && !useDefaults && _negatedFSs.Count==0` and both operands are Simple; **falls back** to the existing engine otherwise (variables, complex/string features, defaults, negation, subsumption) — so results are byte-identical.
- A static `FeatureStruct.FlatUnifyEnabled` toggle lets the benchmark A/B (parity + timing) on/off.

**Targets both costs:** removes the per-arc `VariableBindings.Clone()` allocation *and* the dictionary walk for the common phonological case. Measure single-thread ms/word + KB/word on **Sena and Indonesian**.

### Results (implemented + parity-validated + measured)

Correct: 63 HermitCrab + 806 SIL.Machine tests pass; an `Input.Matches` parity assertion (fast vs
slow) found **zero divergence** on the en, **Sena**, and **Indonesian** grammars. Key subtleties hit
along the way: (1) `FlatIndex` must be **globally unique** across feature systems (a struct mixes
`HCFeatureSystem.Type` with the grammar's phonological features); (2) assign it **lazily**, since
`XmlLanguageLoader` doesn't freeze the feature system; (3) the **segment** may carry non-symbolic
features (FLEx stamps a `StringFeatureValue` on every segment) — only the **arc input** must be fully
symbolic, so the fast path requires `input._flatComplete && segment._flatSafeSegment`.

| Grammar | KB/word before → after | Gen0 | fast-path coverage |
|---------|------------------------|------|--------------------|
| **Indonesian** | 12,463 → 11,268 (**−9.7%**) | 44 → 40 | **100%** |
| **Sena** | 9,053 → 9,018 (−0.4%, neutral) | 567 → 565 | 22% |

**Interpretation.** Coverage depends on whether a grammar's rule arcs use **variables (α-feature
agreement)**: Indonesian's nasalization rules are variable-free → 100% of arc matches take the
bitwise path → **−10% allocation**. Sena (Bantu) uses agreement variables heavily → 78% of arcs
fall back to the slow unifier → **neutral** (the cached `EnsureFlat` adds no measurable overhead, so
there is **no regression**). Wall-time tracks allocation but is noisy on a loaded box. **Next lever
for variable-heavy grammars:** extend the flat path to carry variable bindings as bitsets (so α-arcs
can bit-pack too) — that's what would move Sena.

## Phase 1b — Per-word FST-traversal arena: tried, REGRESSES parallel (key finding)

Implemented a per-thread arena that reuses traversal methods + their instance free-lists across a
word's thousands of `Transduce` calls, reset at each word boundary (`FstThreadPool.Reset()` from
`Morpher.ParseWord`). Non-generic thread-static (a `[ThreadStatic]` on the generic `Fst<,>` was also
tried and is its own contention trap).

**Result (Sena, A/B same load):** single-thread allocation **−13%** (good), but **16-thread scaling
collapses: 2.87× → 1.29×.** Confirmed across 4 pooling variants.

**Why — the counterintuitive lesson:** under **Workstation GC**, *pooling is anti-parallel*. A
pooled object lives across the word → survives a Gen0 collection → **promotes to Gen1/Gen2** →
**Gen2 is stop-the-world** → it serializes all 16 threads. Not pooling keeps every traversal object
short-lived (dies in Gen0, never promoted) → no Gen2 stalls → parallel scales. So for the *parallel*
goal you want allocations to **die young**, not be reused. Object-pooling reduces single-thread
allocation but is the wrong tool for "no Server GC at 16 threads."

**Consequence for the rearchitecture:** the right "arena" is **not object pooling** — it's
allocation reduction with **no GC retention**: `struct`/`Span`/`stackalloc` for the FST registers,
instances, and per-arc buffers so they never reach the GC heap (true value-type arena), plus the
bit-packed unify (Gen0 reduction, no retention). The pooling arena is kept behind
`Fst.TraversalPoolEnabled` (**off by default**) as an opt-in for single-threaded / Server-GC callers.

## Phase 3c — FstStatistics + cascade breakdown harness

**Status: complete.** `SenaQuick` emits a full per-category allocation breakdown covering every
probed window: initial shape segmentation, Word construction, Word clone, MarkMorph, VarBindings,
Registers, TraversalMethod, FST scaffold, and the analysis-cascade superset window. Run it on Sena
(or any grammar) to see where the allocation lives before committing to the flat-buffer rewrite.

### English toy grammar baseline (en-hc.xml + WEB-PT corpus, 439 distinct words)

```
SENAQUICK parsed=439 clones/word=2 KB/word=82.9 totalMB=35 gen0=2 ms=98
  BREAKDOWN (% of total alloc):
    Segment (initial Shape)    7.2%  (2,619KB, 439 words)
    Word.ctor(new)             9.6%  (3,509KB, 439 words)
    Word.Clone                21.3%  (7,764KB, 1,005 clones, 2 /word)
    MarkMorph                  0.0%  (16KB)
    VarBindings.Clone          1.0%  (372KB, 3,672 clones, 8 /word)
    Registers.Clone            0.1%  (26KB, 119 clones, 0 /word)
    TraversalMethod            0.7%  (262KB, 1,976 creates, 4 /word)
    Scaffold (initAnns/regs)  21.9%  (7,963KB, 1,976 calls)
    Other (LINQ, FstResult)   38.1%  (13,854KB)
  [analysis window superset:  64.4%  (23,446KB) — includes Clone/Scaffold/etc]
```

**Interpretation and overlap structure:**

The probes have overlapping windows; understanding the nesting is key to reading the numbers:

```
ParseWord total (100% = 35 MB / 439 words)
 ├── Segment (7.2%)                     ← Shape+ShapeNode creation per word
 ├── Word.ctor (9.6%)                   ← initial Word dicts/lists per word
 └── _analysisRule.Apply().ToList() (64.4%)     ← ANALYSIS WINDOW
      ├── per-Transduce (×1,976):
      │    ├── CreateTraversalMethod (0.7%)
      │    └── Scaffold inner loop (21.9%)        ← wraps Traverse()
      │         ├── Word.Clone (21.3%)             ← deep ShapeNode copy
      │         ├── VarBindings.Clone (1.0%)       ← bindings copy on accept
      │         └── Registers.Clone (0.1%)         ← register snapshot on accept
      └── rule-chain machinery (64.4% − 23.7% ≈ 40.7%)
           └── LINQ enumerators, FstResult objects, per-Advance List<int>, etc.
 └── outside analysis (synthesis etc.) ≈ 18.8%
```

- **Scaffold (21.9%) CONTAINS Word.Clone (21.3%) + VarBindings (1.0%) + Registers (0.1%).** Pure
  per-Transduce overhead (initAnns HashSet, Register[,], cmds List) ≈ 21.9% − 23.4% ≈ **−1.5% ≈ 0%**.
  The FST scaffolding itself is nearly free; the cost is entirely the clones inside it.
- **MarkMorph (0.0%)** — the morph annotation creation in `Word.MarkMorph` is negligible (~16KB total).
  This was previously lumped in "Other"; now confirmed non-issue.
- **Rule-chain machinery (~40.7% inside analysis, ~18.8% outside)** — `SelectMany`/`Where`/`Select`
  enumerator state machines (heap-allocated), `FstResult<Word,ShapeNode>` objects, `List<int>` per
  Advance call, `List<FstResult>` per Traverse call, synthesis cascade. NOT addressed by flat-buffer.

**Key findings:**

| Target | % of total | Lever | Status |
|--------|-----------|-------|--------|
| Rule-chain machinery (analysis) | ~40.7% | LINQ → loops; per-Traverse pool | Future Phase 4 |
| Word.Clone | 21.3% | Flat-buffer Shape (int indices) | Future Phase 3b |
| Synthesis + other | ~18.8% | Synthesis cascade reduction | Future |
| Segment + Word.ctor | 16.8% | Arena/pool for initial Word | Future Phase 1 |
| VarBindings, Registers, TraversalMethod | 1.8% | Minor; VarBindings grows on Sena | Future |
| MarkMorph, pure FST scaffold | ~0% | Negligible, skip | Done/skip |

**Flat-buffer ROI on English toy grammar: ~22%** (Word.Clone + pure FST scaffold). The remaining 78%
is cascade machinery, LINQ, synthesis, and initial object construction — not addressed by flat-buffer.

**Flat-buffer ROI on Sena is expected to be much larger:** Sena has ~276 clones/word vs 2 here, so
Word.Clone likely dominates. VarBindings.Clone also expected to be larger (78% α-variable arcs).

### To get the Sena breakdown (what actually decides the flat-buffer investment)

```
HC_GRAMMAR=...\sena-hc.xml HC_WORDS=...\sena-words.txt HC_MAX_UNAPP=5 HC_BUDGET_MS=30000
dotnet test ...HermitCrab.Tests -c Release --no-build --filter FullyQualifiedName~RustifyBenchmark.SenaQuick --logger "console;verbosity=detailed"
```

The Sena breakdown will reveal:
- Whether **Word.Clone** still dominates (it should with 276 clones/word)
- Whether **VarBindings.Clone** is large (expected: 78% α-arcs → many bindings copies per word)
- Whether **Rule-chain machinery** shrinks (Sena's dominant cost may shift to clone explosion)
- Whether **Segment/Word.ctor** are negligible relative to cascade (they should be at 276 clones/word)

Decision gates:
- If Word.Clone > 40% on Sena → flat-buffer int-offset Shape is the highest-ROI next step
- If VarBindings > 20% → extend bit-packed unify to variable arcs first
- If RegisterClone > 10% → flat Register<int> pays
- If Other > 40% → cascade LINQ replacement (Phase 4) competes with flat-buffer for ROI

## Phase 3b — struct/Span FST traversal: blocked on the data model (the real rustify)

Goal: make the FST traversal allocate nothing on the GC heap (registers, instances, per-arc buffers
as value types / `Span` / `stackalloc`), so 16-thread parsing has few enough **Gen0** collections to
scale past the ~3× ceiling **without Server GC** (short-lived, no-retention — the lesson from 1b).

**Blocker (verified):** the FST is generic over an *offset* type, and for HermitCrab that type is
**`ShapeNode` — a class**. So `Register<ShapeNode>` is a *managed* struct (it holds a `ShapeNode`
reference), and the traversal instances hold managed register arrays + `VariableBindings`. Managed
content **cannot be `stackalloc`'d** nor held in a stack `Span`, and pooling them re-creates the
Gen2-promotion regression from Phase 1b. (`Advance` being an iterator also forbids `stackalloc`
across its `yield`s.) So the struct/Span conversion is **not possible as a local change** to the
traversal code.

**What it actually requires — the foundational change:** re-represent the shape as a **flat array of
nodes addressed by `int` index**, and make FST offsets `int` indices rather than `ShapeNode`
references. Then `Register<int>` is **unmanaged** → register arrays/instances/per-arc buffers become
`Span<int>`/`stackalloc`/reusable value buffers with **zero GC-heap allocation** in the traversal →
Gen0 pressure drops → parallel scales without Server GC. This is the data-oriented core the whole
plan circles: it touches `Shape`/`ShapeNode`/`Annotation` and every rule/FST site that uses node
offsets, and must be re-validated against all grammars. It is a large, foundational rewrite — the
genuine "rustify" — not a session-sized increment.

## Phase 3b-impl — Flat int-index shape: staged implementation plan (chosen direction)

**Decision (this effort):** go flat. Re-examination corrected two pessimistic assumptions in the
blocker above:

- **`TOffset` has *no* generic constraints** anywhere in `SIL.Machine` — `int` is mechanically a
  legal offset type for `Fst`/`Register`/`Annotation`/`Matcher` today.
- **The `int`-offset engine already exists and is tested.** `AnnotatedStringData : IAnnotatedData<int>`
  drives `FstTests`/`MatcherTests`/`RuleTests`. So `Register<int>` (the unmanaged-traversal prize) is
  a *proven* path, not speculative.
- **`ShapeNode` is contained:** ~95 in-repo references (Annotations ×4, HermitCrab ×70, Tool ×1) and
  **zero** in Translation/Corpora/SequenceAlignment/Matching.

**The one real cost (accepted):** `ShapeNode` stops being a per-clone reference-identity heap object
and becomes a **handle (owner + `int` index)** whose data lives in the shape's flat arrays. Identity
becomes value-based `(owner, index)` instead of reference. This is a public/semantic change to a
published type — contained, but real — and is the thing that lets `Word.Clone` become an array copy
instead of N object allocations, and (Stage 2) lets the FST offset be `int`.

**API policy for this effort (decided 2026-06-29).** Change the `Shape`/`ShapeNode`/`Annotation`
public API *now* — freely, especially for the internal-to-`SIL.Machine` call sites — rather than
contorting the rewrite to preserve the old signatures. External/legacy consumers outside `machine`
(FieldWorks adapters, etc.) that depend on the old reference-identity `ShapeNode` surface get a
**compatibility adapter added later**, not a constraint on the rewrite. The hard gate is **byte-
identical behavior** (full `SIL.Machine` + HC suites + concurrent-determinism), not API stability.

### Stage 1 — Array-backed `Shape` + `ShapeNode` handle (data model) — LANDED ✅ (foundation)

**Status: done, full-suite green (803 SIL.Machine + 63 HC, incl. concurrent-determinism).** `Shape`
no longer inherits `OrderedBidirList<ShapeNode>`; it owns its nodes in flat backing arrays
(`_next`/`_prev` int links — an in-array doubly-linked list — plus a per-node frozen flag and the
canonical handle), addressed by a stable per-node `ShapeNode.Index`, and reimplements the
`IOrderedBidirList`/`IOrderedBidirListNode` surface over those arrays. `ShapeNode` is now a handle
(`Owner` + `Index`) whose links/frozen delegate to the owner arrays. Two deliberate scoping choices
keep this increment byte-identical and low-risk:
- **`Tag` stays on the node** (not in an array). It is the one piece of node state that must survive a
  node being *moved between shapes* (`AddAfter` sets the new tag *before* the node detaches from its
  old owner); keeping it on the node sidesteps that ordering hazard. Tag can relocate in Stage 2 when
  the dense index itself becomes the ordered FST offset.
- **The `ShapeNode` added to a shape is retained as the canonical one-per-slot handle**, so reference
  identity — every `==`, dictionary-key and `Range<ShapeNode>`-endpoint comparison across HC and
  `SIL.Machine` — is unchanged. No handle materialization in Stage 1.

**Measured (en-hc toy, `SenaQuick`, 439 words, Release): KB/word 80.5 → 80.5 (neutral), clones/word
2, gen0 2** — the per-node `Next`/`Prev`/`_list` reference fields removed roughly cancel the
per-shape backing arrays on a clone-light grammar.

**Measured on the REAL Sena grammar (`SenaQuick`, 400 words, `HC_MAX_UNAPP=5`, Release) — the
clone-heavy case where the payoff lives:**

| | pre-Stage-1 (`2fd1a2d3`) | **Stage 1 (HEAD)** | Δ |
|---|---|---|---|
| clones/word | 345 | **345** | **= (byte-identical *and* behavior-identical at scale)** |
| KB/word | 14,116 | 14,583 | **+3.3%** |
| gen0 | 442 | 457 | +3.4% |
| Scaffold | 42.2% | 42.7% | ~= |
| Word.Clone | 21.9% | 22.3% | ~= |

This is the honest "**slightly negative**" half of the Stage 1 prediction, now quantified where it
matters: on Sena's ~344 clones/word, each `Word.Clone` allocates a fresh `Shape` with **four backing
arrays** (`_nodes`/`_next`/`_prev`/`_frozen`) → **+3.3% allocation**, which the toy grammar's 2
clones/word entirely hid. The clone *count* is unchanged (345), confirming the flat data model is
behaviorally identical even on the pathological grammar. The cost is real but small and is **the
investment, not the return**: the 42.7% Scaffold and 22.3% Word.Clone buckets are the Stage 2/3
targets that the same flat foundation unlocks. (A cheap mitigation if wanted before then: pack
`_next`/`_prev`/`_frozen` into one struct array to drop 2 array headers per clone — but Stage 3's
`Array.Copy` clone + removed handle materialization is what actually turns this negative into the win.)

#### Original Stage 1 design notes (target end-state; some deferred past this first increment)
- `Shape` holds the node data in a flat backing (SoA/AoS): per node `FeatureStruct`, `Optional`,
  dirty flag, `Tag`, and `int` prev/next links (an in-array linked list so `AddAfter`/`Remove` stay
  O(1) and the existing tag-relabel order-maintenance is preserved).
- `ShapeNode` becomes a thin handle: `Shape Owner` + `int Index`; every property delegates to the
  owner's arrays; `Equals`/`GetHashCode` are `(owner, index)`; reference-identity dict/`==` uses
  migrate to that. Handles are **cached one-per-index on the owner** (lazily) so enumeration doesn't
  re-allocate them.
- `Shape.Clone` = `Array.Copy` the backing (+ COW `FeatureStruct`) + rebuild the flat annotation
  records; no per-node deep copy, no clone-mapping dictionary.
- Public method surface of `Shape`/`ShapeNode` preserved; gate **byte-identical** on the full
  `SIL.Machine` + HC suites + concurrent-determinism.
- **Honest expectation (corrected):** Stage 1 *in isolation is ≈0 or slightly negative* on
  allocation. A clone is a new owner, so the first traversal of every clone re-materializes its N
  handle objects, and the `Annotation` tree is still N objects per clone (morph annotations span
  ranges; they don't fall out of a node array for free) — after the cheap `Array.Copy` the clone
  re-allocates about what it does today, plus the backing array. "`Clone` = array copy" only becomes
  true once Stages 2–3 remove handle materialization. **Consequence:** Stages 1–3 are *not*
  individually-shippable measured increments (the project's normal land-a-green-increment rule); they
  are **one integrated spike on a dedicated branch, measured end-to-end, with a go/no-go before
  merge.**
- **Measurement prerequisite (blocking):** the entire payoff is the Sena clone explosion
  (~276 clones/word) — which is *estimated, never measured locally* (no `sena-hc.xml`; toy = 2
  clones/word). A foundational, API-breaking rewrite must not be written before a **clone-heavy
  benchmark** exists to show the number it moves. Get that first (generate `sena-hc.xml`, or
  synthesize a high-unapplication-ambiguity grammar that drives clones/word up locally).

### Stage 2 — `int` FST offset for HC + unmanaged traversal
- Bind the HC FST as `Fst<Word, int>` (offset = node index). `Register<int>` is unmanaged →
  traversal registers/instances/per-arc buffers become `Span<int>`/`stackalloc`/reusable value
  buffers → **zero GC-heap allocation in the hot loop** → Gen0 pressure drops → 16-thread parsing
  scales past the Workstation-GC ~3.5× ceiling **without Server GC**. This is the strategic payoff.

#### Stage 2 implementation blueprint (the resolved design — decided after Stage 1)

The non-obvious design questions, resolved by reading the FST traversal + range semantics:

- **What `int` is the offset? The node's dense frozen tag.** `Shape.Freeze()` already assigns content
  nodes `Tag = 0,1,2,…,N-1` (dense, in logical order); margins keep `int.MinValue`/`int.MaxValue`.
  **HC always freezes a word before the FST traverses it** (`Morpher.Parse`/`AnalysisStratumRule`/
  `AnalysisAffixTemplateRule` all `Freeze()` first; outputs are mutable clones built *separately*,
  then frozen before the next stage). So at traversal time the dense tag is a valid `0..N-1` position
  — exactly the contiguous offset `int`/`AnnotatedStringData` semantics already assume.
- **Range semantics: half-open `[t, t+1)`, reuse `IntegerRangeFactory` unchanged.** `ShapeRangeFactory`
  is `IncludeEndpoint=true` (inclusive `[t,t]`); `IntegerRangeFactory` is half-open. For a dense
  one-unit-per-node model these are **provably identical in ordering, `Overlaps`, and `Contains`**
  (verified: distinct nodes never overlap, spans contain their members) — so the int model yields a
  byte-identical traversal structure. `Range<int>` must NOT change `IntegerRangeFactory`'s semantics
  (AnnotatedStringData/FstTests depend on the half-open form), and it doesn't need to.
- **`Word`/`Shape` become `IAnnotatedData<int>`.** The `AnnotationList<int>` is a **freeze-time
  projection** of the existing `AnnotationList<ShapeNode>`: each annotation `[startNode, endNode]` →
  `[startNode.Tag, endNode.Tag + 1)`. It cannot be baked at annotation-creation time (tags are sparse
  until Freeze), so it is built in `Freeze()` alongside the dense-tag assignment. `Shape` also builds a
  `ShapeNode[] _byPos` (`NodeAt(int)`), the int→node map.
- **Rules resolve `int → ShapeNode`.** Every `Pattern<Word,ShapeNode>`/`Match<Word,ShapeNode>`/
  `PatternRule<Word,ShapeNode>`/`Matcher<Word,ShapeNode>` becomes `<Word,int>`. Rule RHS code that
  reads `match.Range`/`GroupCapture` (now `int` ranges over the frozen *input*) maps back to nodes via
  `match.Input.Shape.NodeAt(pos)`; shape *mutation* still happens on the mutable output clone through
  the usual clone-mapping. So the actual segment graph stays `ShapeNode`-handle-based; only the
  FST/pattern/match addressing layer goes `int`.
- **The payoff lands here:** with `TOffset = int`, `Register<int>` is unmanaged →
  `Register<int>[,]`/instances/per-arc buffers become `stackalloc`/`Span<int>`/reusable value buffers
  (the measured **42% Scaffold** bucket), and the per-`Transduce` managed `Register<ShapeNode>[,]`
  allocation disappears.

#### Stage 2 blueprint — CORRECTIONS after reading the rule-application flow (the subtleties)

Reading the actual apply path (`IterativePhonologicalPatternRule.Apply` + the rewrite `*SubruleSpec`s
+ the semantic-site catalog) overturned two assumptions above. The corrected design:

1. **Offset = the node's `Tag` (SPARSE), NOT a dense frozen position.** Phonological rewrite rules
   (`IterativePhonologicalPatternRule`/`SimultaneousPhonologicalPatternRule`) match the word, then
   **mutate `match.Input.Shape` in place — while it is UNFROZEN — and re-match repeatedly**
   (`shape.AddAfter(curNode, fs)` in the Epenthesis/Narrow subrule specs; `MoveNodesAfter` in
   Metathesis). So the shape the FST traverses is generally *not* frozen and its tags are *sparse*
   (the relabel-on-collision scheme), never `0..N-1`. ⇒ the offset must be the raw ordered `Tag`.
   **The `[Tag, Tag+1)` half-open mapping is still provably correct for sparse tags:** tags are
   strictly increasing ints, so the exclusive end `Tag+1` is always `≤` the next node's tag and
   `> Tag` — it covers exactly that one node and excludes the next, regardless of gap size. Ordering /
   `Overlaps` / `Contains` are preserved. Spans map `[startTag, endTag+1)`.
2. **`NodeAt` must work on UNFROZEN shapes ⇒ a `Tag→ShapeNode` map maintained incrementally**, not a
   freeze-only dense `_byPos` array. Maintain a `Dictionary<int,ShapeNode> _byTag` updated in
   `AddAfter`/`Remove`/`RelabelMinimumSparseEnclosingRange` (the relabel already iterates the affected
   nodes, so it is O(k) on the slots it touches — no extra asymptotic cost) and rebuilt densely at
   `Freeze`. **Cost note:** this adds bookkeeping to the hot mutate path for *all* `Shape` consumers,
   so it should likely be lazy/gated until the int-FST actually consumes it.
3. **Margin/overflow + live-projection sync — the real hazards to design for:**
   - `End.Tag == int.MaxValue` ⇒ `[Tag, Tag+1)` **overflows** to an invalid range. The shape puts the
     `Begin`/`End` anchor annotations *into* `_annotations` (unlike `AnnotatedStringData`), and the
     `AnnotationList<int>` skip-list orders them via `Range.CompareTo` on **add**, so the End anchor
     needs a non-overflowing int range (e.g. map End to an empty `[int.MaxValue, int.MaxValue]` and
     special-case, or exclude anchors from the projection and rely on the existing `_fst.Filter`).
     Inclusive `[t,t]` anchors are non-empty under `ShapeRangeFactory` but **empty** under half-open
     `int` — verify `Find`/`GetEnd` don't depend on anchor non-emptiness.
   - Because the traversed shape mutates between matches, the `AnnotationList<int>` projection cannot be
     a build-once-at-freeze artifact for rewrite rules; it must be **kept in sync with the live
     `AnnotationList<ShapeNode>`** (rebuild-on-dirty, or maintain in lockstep). Rebuild-per-match is
     O(n) and would make iterative rewrite O(n²) — acceptable-ish (the current re-match-from-start is
     already ~O(n²)) but should be measured.
4. **Offset navigation must route through the shape.** ~Several apply sites navigate the matched
   offset directly: `match.Range.End.Next`, `match.Range.Start.Prev` (RewriteRuleSpec 57/61/70/71),
   `targetMatch.Range.GetEnd(dir).GetNext(dir)` (IterativePhonologicalPatternRule 29/33),
   `rightGroup.Range.Start.Prev` (SynthesisMetathesisRuleSpec). With `int` offsets these become
   `shape.NodeAt(tag).Next?.Tag` etc. (returning `int?`, null at the margins). This is the bulk of the
   semantic rewrite — ~30 sites catalogued, each must preserve the exact null-at-boundary behavior.

**Net:** the flip is real but **larger and subtler than a mechanical generic swap** — the offset
semantics (sparse tags), the incremental `Tag→node` map, the margin/overflow + live-sync of the int
annotation projection, and the ~30 navigation rewrites are each a correctness-critical design point.
This is a multi-session spike; do each sub-piece behind the byte-identical gate.

**Execution note:** this is the "build red until complete, measure at end" integrated spike (chosen
2026-06-29). The `<Word,ShapeNode>`→`<Word,int>` generic flip across ~75 files cannot stay green
mid-flight; the substrate (dense-tag offsets, `NodeAt`, the `AnnotationList<int>` projection) is built
first, then the generic flip, then `Register<int>` stackalloc, then re-validate byte-identical + measure.

#### Stage 2 generic flip — LANDED, byte-identical green ✅ (2026-06-29)

The `<Word,ShapeNode>`→`<Word,int>` flip is complete: **63/63 HC + 808 SIL.Machine tests green**, full
Release solution builds clean. The substrate decision settled on **dense per-projection node positions**
(not the sparse raw `Tag`): the `Shape` lazily builds an `AnnotationList<int>` projection (Begin=0,
content 1..N, End=N+1) cached against the annotation-list `Version`, with `NodeAt`/`OffsetOf`/
`MatchStartOffset`/`ToIntRange` as the int↔node bridge. Dense offsets sidestep the sparse-tag overflow
and `Range<int>.Null = [-1,-1]` edge cases the blueprint flagged.

Bringing the flip from red (57/63) to green surfaced **two correctness bugs unique to the int model**,
both in analysis (right-to-left, optional-node-driven) rewrite rules, both now fixed + regression-tested:

1. **`Annotation.Optional` flips didn't invalidate the projection cache.** The projection copies
   `Optional` by value and caches against the list `Version`, but the `Optional` setter is non-structural
   and never bumped `Version`. Analysis sets `Optional=true` on existing nodes (epenthesis unapplication),
   so the matcher kept reading the stale `Optional=false` projection and never forked the optional-skip
   instances → under-generation. Fix: the setter bumps the root list's version (`AnnotationList.IncrementVersion`).
2. **`IntRange` was `[off(Begin), off(End)]` instead of the half-open `[off(Begin), off(End)+1)`.** The
   only framework consumer is `Matcher.GetStartAnnotation` via `Range.GetStart(dir)`; a RtL match starts
   at `GetStart(RtL)==End`. The End anchor's dense range is `[off(End), off(End)+1)`, whose RtL start
   coordinate is `off(End)+1` — without the `+1` a RtL match began at the *last content node*, skipping
   any edit adjacent to End (e.g. inserting a deleted segment after the final vowel). Fix: `+1`, matching
   the inclusive→half-open image the parity test already encodes.

**Why 59 of 63 passed with these bugs:** most analysis is RtL but the End anchor was rarely reachable as a
start position, and most rules don't flip `Optional` on existing nodes — so only the deletion/epenthesis/
reduplication analysis paths exercised them. Restoring both restores master behavior.

**Measured (en-hc toy, `SenaQuick`, 439 words, Release): KB/word 78.8 (Phase 4c) → 86.1 (+9%).** This is
the *investment, not the return*, exactly as Stage 1: the lazy `AnnotationList<int>` projection plus its
`_byOffset`/`_nodeOffset` maps are new per-shape allocation, and the `Register<int>` payoff that pays it
back is not in yet. The flip is correct (byte-identical) and builds the foundation; the toy grammar (2
clones/word) over-weights the projection cost vs the Scaffold/clone win that dominates at Sena's ~345
clones/word. **Still TODO for Stage 2: the strategic payoff — `Register<int>` → `stackalloc`/`Span`/
reusable value buffers for the 42% (Sena) Scaffold bucket, now unblocked because `Register<int>` is
unmanaged.** Note the working register array escapes into `FstResult` on accept (cloned), so the safe
target is the per-instance *working* buffer (no retention), not the accepted snapshot.

#### Stage 2 register-payoff investigation — register hypothesis REFUTED by measurement (2026-06-29)

The Stage-2 thesis was that with `Register<int>` unmanaged, the **42% Scaffold** bucket would fall to
`stackalloc`/`Span`/reuse. Measuring the *real* grammars (Sena **and** Indonesian, both now wired — see
below) refutes it:

- **`Registers.Clone` (the escaping accept snapshots) = 0.2%** on Sena (89,854 clones). The escape-into-
  `FstResult` contract the redesign would target is simply not where the bytes are.
- **Eliminating the per-push dedup-key `Tuple` heap object** (both nondeterministic traversal methods:
  `Tuple<State,int,Register[,][,Output[]]>` → an inline `readonly struct TraversalKey`) moved allocation
  **~0%** (Sena KB/word 14588→14579, Scaffold 2,248,027→2,243,668 KB; Indonesian flat). So the
  `traversed`-set key was not a meaningful allocator either. *(Kept anyway — zero-risk, byte-identical,
  removes a real per-push heap object + is CPU-positive, consistent with the Phase-4c micro-eliminations.)*
- **The Scaffold 38.5% is the clone explosion, not registers.** It *contains* `Word.Clone` (22.4%, via the
  per-initial-instance `inst.Output = Data.Clone()` in `InitializeStack`) plus the per-instance `Mappings`
  dictionary population (one entry per annotation, per cloned instance) and the per-instance `Output`
  graph. Pure non-clone scaffold (initAnns/initRegisters/collections) is the small remainder.

**Conclusion: the int flip's allocation payoff is *not* a register stackalloc — it is cutting `Word.Clone`
+ the per-instance `Mappings`/`Output` graph, i.e. Stage 3 (flat-shape clone).** The register work was the
right thing to *rule out* with data; the lever is the clone explosion the whole plan kept circling. Stage 3
(flat-shape `Array.Copy` clone, no per-node handle/annotation re-materialization) is the remaining prize.

### Stage 3 — Migrate rule sites off raw `ShapeNode` references to indices
- The ~70 HC rule references that pass `ShapeNode`/`Range<ShapeNode>` move to index-based access so
  no handle is materialized on the hot path; realizes the full `Word.Clone` allocation cut.

Each stage lands incrementally behind the byte-identical + determinism gates, one green commit at a
time, recorded here.

#### Levers 1 + 2 — lean Word + lean scaffold (2026-06-30): implemented green, modest, the big win stays blocked

After Stage 3, the per-word allocation (Sena ~13 MB/word) is dominated **not** by the word candidates but
by the **FST matcher running ~2,482 Transduce calls/word** (Scaffold 42% + Other 37%). Two byte-identical
green increments were landed against the named levers:

- **Lever 2 (lean Word):** `_mrulesUnapplied`/`_mrulesApplied`/`_disjunctiveAllomorphIndices` are lazily
  allocated (null = empty) — they stay empty through the phonological-analysis cascade but were cloned per
  candidate. Measured: Word.Clone 527,987 → 499,144 KB (−29 MB), Word.ctor 184,858 → 177,387 KB.
- **Lever 1 (lean scaffold):** `Fst.Transduce` allocated the initial `Register[regCount,2]` per start
  position; hoisted to one array + `Array.Clear` per start (Traverse only Array.Copy's it, never retains).
  Measured: Scaffold −~23 MB (helps the multi-start `AllMatches` analysis calls).

**First pass ≈ −1% allocation; both byte-identical.** See below for the deep rewrite that followed.

#### Lever 1 deep rewrite — the scaffold/instance value-ification (2026-06-30)

Profiling showed the 42% Scaffold is **instance churn**: ~2,927 traversal instances created per Sena word
(only ~20% reused — the pool is per-Transduce, thrown away each call; pooling across calls re-triggers the
Phase-1b Gen2 regression). So the fix is **leaner instances + less per-call garbage**, not pooling. Three
byte-identical green increments:

- **Visited HashSet → inline value bitset** (`VisitedStates`). States have a dense `Index`, so the
  per-instance epsilon-loop set is now states 0–63 in an inline `ulong` (zero heap; HC FSTs are tiny) +
  a lazy `ulong[]` overflow for 64+ states. Removes the ~1.17M `HashSet<State>`/word allocation.
  **Measured: Scaffold −100 MB (total 5,242 → 5,145 MB).**
- **De-iterator `Advance` + `Initialize`.** Both were `yield`/per-call-`List` (recursive), minting an
  iterator state machine / List on each of the millions of calls/word. Now both fill **one reusable
  per-method result buffer** (per-Transduce → no cross-word retention; not a thread-static because
  `CheckAccepting`'s `Acceptable` can re-enter `Transduce`; `Initialize` and `Advance` never overlap so
  they share it). **Measured: total 5,145 → 5,029 MB (−116 MB, ~−2.3%).**

**Cumulative lever-1 (incl. the `initRegisters` hoist): Sena total ~5,357 → ~5,029 MB (~−6%), all byte-
identical** (811 SIL.Machine + 63 HC incl. concurrent-determinism; analysis-signature diff vs the
pre-Stage-3 baseline IDENTICAL on Sena 400 + Indonesian 121 words).

**The register `stackalloc` premise does NOT hold for the nondeterministic matcher (the hot path).** The
`traversed` dedup retains a **per-config register snapshot** for the duration of each `Transduce`, so the
registers are not transient stack values — they're the evolving, snapshotted match state, one array per
config (≈ per instance), fundamentally needed for dedup/termination. They die in Gen0 with the Transduce,
but their **count can't be reduced** without reducing configs. So the achievable scaffold wins were the
iterator garbage + the Visited set (done); the genuine remaining step-change is **lever (b): reduce the
Transduce/clone count** (~2,482 / ~345 per word) algorithmically — the multiplier, not the per-call size.

#### Stage 3 clone-cost localization — the cost is inherent per-node materialization (2026-06-29)

Split `Shape.CopyTo` (the body of `Word.Clone`) with a temporary two-phase allocation probe on Sena:

| `Word.Clone` sub-phase | % of total alloc | bytes |
|---|---|---|
| CopyTo **node phase** (`node.Clone()` + per-node `dest.Add`) | **11.4%** | 666 MB |
| CopyTo **annotation phase** (`CopyAnnotations`) | 4.1% | 241 MB |
| remainder (Word's own dicts/lists/FS clones + Shape ctor arrays) | ~6.9% | — |

The node phase (11.4%) is the prize, and it is **inherent per-node object materialization**: each cloned
node allocates a `ShapeNode` handle + its `Annotation` + a COW `FeatureStruct` wrapper + the
`AnnotationList` skip-list `Add` structures. Two incremental attacks were tried and **both measured ~0 or
negative, then reverted** (the by-now-familiar lesson — hot-path micro-edits don't move an *object-count*
cost):
- Pre-sizing the four backing arrays once (vs `AddAfter`'s 4→8→16… doubling): node phase 666→688 MB
  (worse — `Count` over-sizes for partial-range `CopyTo`, and the doubling intermediates were never the
  cost). Reverted.
- The per-push dedup-key `Tuple`→struct change (above): ~0.

**Conclusion — the flat-clone payoff requires the deep redesign, not increments.** To cut the 11.4% node
phase you must stop materializing N `ShapeNode` + N `Annotation` + N skip-list entries per clone, which
means: (1) **lazy `ShapeNode` handles** (clone = `Array.Copy` the `_next`/`_prev`/`_frozen` backing +
defer handle creation to first access), and (2) a **bulk `AnnotationList` clone** (copy the skip-list
structure directly instead of N `Add`s), ideally with annotations addressed by node **index** rather than
`ShapeNode` reference so they don't force handle materialization. This is the Stage-1-deferred "Clone =
array copy" end-state and the genuine multi-session foundational rewrite the plan flagged for a go/no-go —
it touches `Shape`/`ShapeNode`/`AnnotationList` and re-validates byte-identical across all grammars.
**Go/no-go on that scope is the open decision; no incremental win is available short of it.**

**Decision (2026-06-29): go, plan-first.** Full design + sequencing in **`RUSTIFY-stage3-design.md`** —
the materialize-on-touch two-state shape (flat snapshot until a handle is touched), which resolves the
dense-index-vs-in-place-mutation tension (frozen-read pays nothing; unfrozen-mutate materializes as today),
the byte-identical risk register, the I→V sub-increment order (I/II gateable green; III–IV the red phase),
and rollback to `dbef327a`.

**Progress (2026-06-29) — Stage 3 LANDED, all byte-identical green; the flat-clone goal met:**
- **II-a** — grow the skip-list Begin/End margin towers on demand instead of the eager `[33]` pre-alloc.
- **II-b** — inline skip-list level 0 as fields, so ~50% of nodes (level-0) allocate no tower array.
- **III** — **copy-on-write `Shape`**: a clone of a frozen shape shares the source and copies nothing; it
  serves the FST matcher's int projection from the source and only materializes (`EnsureInflated`) when a
  `ShapeNode`/`Annotation` handle is handed out or it is mutated. A traverse-only clone costs a shell. The
  frozen source's projection is built eagerly at `Freeze()` so concurrent COW delegation is race-free.
- **Cumulative measured (SenaQuick, Release): Sena `Word.Clone` −778 MB (−59.6%), its share 22.4% → 9.9%;
  Indonesian `Word.Clone` −62%.** `Word.Clone` is no longer a top allocation bucket. Full `SIL.Machine`
  (808) + HC (63, incl. concurrent-determinism) green; full Release solution builds clean; `SenaParallel`
  scaling unregressed. Details in `RUSTIFY-stage3-design.md`.

### Measurement grammars: Sena + Indonesian (both wired)

Two real FLEx-exported grammars drive the high-signal allocation numbers (the in-repo `en-hc` toy is
2 clones/word and under-measures the clone explosion). Both `*-hc.xml` + `*-words.txt` live in
`samples/data/` (untracked — regenerate as below; the `.fwdata`/`.fwbackup` sources are large) and are
selected at run time via `HC_GRAMMAR`/`HC_WORDS` (absolute paths) on `RustifyBenchmark.SenaQuick`.

- **Sena** (Bantu, variable-heavy α-agreement — ~345 clones/word, the clone-explosion stress case):
  `generatehcconfig "<Sena 3>.fwdata" sena-hc.xml`; `sena-words.txt` = 7,121 distinct `<Run ws="seh">`
  running-text words.
- **Indonesian** (the classic HermitCrab nasalization demo, variable-*light* — ~150 clones/word, a
  useful mid-scale + fast-unify contrast): unzip the `.fwbackup` (it is a zip), then
  `"C:\Program Files\SIL\FieldWorks 9\GenerateHCConfig.exe" Indonesian-HermitCrab.fwdata indonesian-hc.xml`;
  `indonesian-words.txt` = the `<Run ws="id">` surface forms, space-split, rule-environment tokens
  (`/ _ [ ]`) and length<3 dropped → 121 distinct wordforms. `SenaQuick` on it: clones/word=150,
  KB/word≈8,690, Scaffold 24.9%, Word.Clone 22.1%, VarBindings 1.0% (vs Sena's 3.1% — the variable-light
  contrast).

`SenaQuick` on **Sena** (400 words, `HC_MAX_UNAPP=5`, Release):

```
SENAQUICK parsed=400 clones/word=345 KB/word=14116 totalMB=5513 gen0=442
  Segment            0.0%
  Word.ctor          3.3%
  Word.Clone        21.9%  (137,951 clones, 344/word)
  MarkMorph          0.5%
  VarBindings.Clone  3.2%  (1,781,634 clones)
  Registers.Clone    0.4%
  TraversalMethod    3.6%  (992,687 creates)
  Scaffold          42.2%  (992,687 Transduce calls)   <-- biggest bucket
  Other (LINQ/Fst)  24.8%
```

**This reshapes priorities (and confirms flat over COW):**
- `clones/word` = **345** — the clone explosion is real (matches the ~276 estimate), so the toy's 2
  clones/word genuinely under-measured; the benchmark now exists to validate the spike end-to-end.
- **`Scaffold` (42.2%) is the dominant bucket, ~2× `Word.Clone` (21.9%).** Scaffold is the per-
  `Transduce` `new Register<ShapeNode>[regCount,2]` (+ `initAnns`); with `TOffset = ShapeNode` it is a
  *managed* array on the GC heap. **Stage 2 (`Register<int>` → `stackalloc`/`Span`) attacks this 42%
  directly** — a bigger prize than the 22% `Word.Clone` the goal named, unlocked by the *same* flat
  foundation. COW could never touch it (it's traversal scaffold, not the clone).
- So the flat int-index foundation's headline payoff is **Stage 2 (register scaffold, 42%) + Stage 1/3
  (Word.Clone, 22%)** ≈ the analysis window's ~64% — and the parallel unlock on top.

## Phase 4a — Hot-loop allocation eliminations (safe, no lifetime extension)

**Status: complete, landed.** Four pure-elimination changes in the FST traversal + analysis
cascade — each removes an allocation outright (no object's lifetime is extended, so none can
trigger the Phase-1b parallel regression). All gated through the full correctness suite
(**803 SIL.Machine + 63 HermitCrab tests green**) and `SenaParallel` (no scaling regression).

1. **`ITraversalMethod.Traverse` returns `List<FstResult>` (was `IEnumerable`) → drop `.ToList()`
   in `Fst.Transduce`.** All four concrete `Traverse` implementations already `return curResults`
   (a `List`); the interface was needlessly widened to `IEnumerable`, forcing a redundant
   `.ToList()` copy at the call site. Widening the return type is source-compatible for every
   `IEnumerable` caller (Matcher, RootAllomorphTrie).
2. **Remove redundant `.Distinct(FreezableEqualityComparer<Word>.Default)` (×2) in
   `AnalysisStratumRule.ApplyMorphologicalRules`/`ApplyTemplates`.** Both `_mrulesRule`
   (`PermutationRuleCascade`/`CombinationRuleCascade`/`ParallelCombinationRuleCascade`) and
   `_templatesRule` (`RuleBatch`) are constructed with `FreezableEqualityComparer<Word>.Default`
   and return a `HashSet<Word>` already deduped by *that exact comparer* — so the `.Distinct`
   pass with the same comparer is provably a no-op `DistinctIterator` allocation. (Comparer match
   verified in the `AnalysisStratumRule` ctor; correctness confirmed by the 63 HC tests, which
   assert parse results, not just that parsing runs.)
3. **Skip the `DistinctIterator` for trivial result sets in `Fst.Transduce`:**
   `results = (allMatches && resultList.Count > 1) ? resultList.Distinct() : resultList;`
   At that line `resultList` is non-null with `Count >= 1`; for `Count == 1` (the common case)
   `Distinct` of a 1-element sequence is the sequence itself, so this is semantically identical
   and skips the lazy iterator + its internal set on every single-result Transduce.
4. **`TraversalMethodBase.Reset` — eliminate the per-Transduce `GetNodesDepthFirst` yield
   iterator.** `Reset` runs once per `Transduce` (thousands/word) and walked each top-level
   annotation with `GetNodesDepthFirst(dir)`, a heap-allocated yield state machine per top
   annotation. Replaced with the allocation-free `PreorderTraverse(action, dir)` callback form
   (identical depth-first preorder), with the insertion-sort body moved to an `InsertAnnotation`
   method and the delegate **cached as a `_insertAnnotation` field** (allocated once in the ctor,
   not per call). For the leaf-heavy HC annotation tree `PreorderTraverse` allocates no inner
   iterators.

**Measured (en-hc toy grammar, `SenaQuick`, 439 words):** `Other` (LINQ/iterators/FstResult)
**38.5% → 36.2%** (14,145KB → 12,884KB) — the eliminated `GetNodesDepthFirst` state machines —
and KB/word **83.6 → 81.1**, totalMB 35 → 34. `TraversalMethod` ticks up 262KB → 401KB (the once-
per-method cached delegate, allocated per-Transduce because pooling is off by default), but the
net is a ~1.1 MB/run reduction. `SenaParallel` scaling unchanged (no regression). On the toy
grammar these deltas are small/low-signal; the real magnitude needs the Sena grammar wired via
`HC_GRAMMAR`/`HC_WORDS`. (These 4a numbers were taken Debug, same-config before/after this
session, so the delta is valid — but they are not directly comparable to the Release rows in the
results table above.)

The `.Distinct` removal (#2) is safe on **all four** `_mrulesRule` types, including
`ParallelCombinationRuleCascade` (selected when `MaxDegreeOfParallelism != 1`): its `Apply`
returns `output.Distinct(Comparer)` with the same `FreezableEqualityComparer<Word>.Default`, so
the source is deduped by that exact comparer regardless of the within-word parallelism setting.

## Phase 4b — Scaffold-buffer ThreadStatic pooling: investigated, REJECTED (re-entrancy + thesis)

The `Fst.Transduce` per-call scaffold (`initAnns` `HashSet<int>`, `cmds` `List<TagMapCommand>`,
`initRegisters` `Register<TOffset>[,]`) shows as ~22% of toy-grammar allocation, so reusing those
buffers as thread-statics was the obvious next target. **Rejected without landing**, for three
independently sufficient reasons:

- **Re-entrancy (silent corruption).** `CheckAccepting` invokes `acceptInfo.Acceptable(_data,
  candidate)` — an arbitrary predicate that can re-enter another `Transduce` on the same thread.
  A shared thread-static `cmds`/`initAnns` would then be mutated mid-traversal by the inner call.
  Toy-grammar tests don't exercise this path, so the bug would be invisible to the harness.
- **Lifetime extension is the Phase-1b regression.** A process-lifetime thread-static is exactly
  the long-lived object that promotes to Gen2 and serializes parallel parsing under Workstation
  GC. `initRegisters` is the worst case — it holds `ShapeNode` references (cross-generational →
  card-scanned every Gen0) and is a generic-static (the contention trap noted on `FstThreadPool`).
  The big slice of Scaffold is `initRegisters`, which pooling therefore can't safely touch.
- **Unmeasurable here.** On en-hc the per-category deltas are noise, and `SenaParallel` at this
  scale (13–14 ms, dop=16 already jittering below dop=8) cannot discriminate a real regression —
  so the SenaParallel gate doesn't function without the real Sena grammar.

Consistent with Phase 1b's lesson: **for the parallel goal, allocations must die young, not be
reused.** Pooling reduces single-thread allocation but is the wrong tool here. A documented
negative result, like Phase 1b and 3b.

## Phase 4c — Five safe, no-retention eliminations in the FST traversal core

**Status: complete, landed (5 commits on `hc-rustify`).** Five pure-elimination / behavior-
preserving changes in `SIL.Machine` core (`Fst`, `TraversalMethodBase`, the Det/Nondet traversal
methods + instances) — each removes an allocation outright **or** halves redundant work, with **no
object lifetime extended** (so none can trigger the Phase-1b parallel regression). All gated through
the **full** suite — **803 SIL.Machine + 63 HermitCrab tests green** (incl. the concurrent-
determinism test) — and `SenaParallel` (no scaling regression). Done one-at-a-time, measured.

1. **`NondeterministicFstTraversalMethod.Traverse` (×2): `traversed.Contains(key)`+`Add(key)` →
   `if (traversed.Add(key)) Push`.** `HashSet.Add` already returns false when present, so this is
   one structural-key hash (state + annIndex + register array + outputs array) instead of two in the
   innermost loop. CPU-only (no allocation), byte-identical.
2. **`TraversalMethodBase.Advance`: drop the per-call `List<int> anns`.** The same-offset annotation
   window is a contiguous index range `[nextIndex, annsEnd)`; track the end bound and iterate the
   range directly. One List eliminated per arc match (hot).
3. **Det + Nondet `TraversalInstance.CopyTo`: collapse an identity-map LINQ block to
   `Mappings.AddRange(_mappings)`.** The original built `outputMappings` by zipping `this.Output`'s
   node sequence **with itself** (deterministic BFS paired element-for-element ⇒ identity map), then
   projected `_mappings` through it — provably equal to copying `_mappings` unchanged. Removes a
   Dictionary + two `SelectMany(GetNodesBreadthFirst)` (each a `Queue` + iterator) + `Zip` + `Select`
   per instance copy. `CopyTo` runs on every nondeterministic branch, so this is allocation-heavy at
   Sena's ~276 clones/word though invisible on the toy grammar (2 clones/word, few branches).
4. **Det + Nondet `InitializeStack`: paired-walk the clone mapping.** Both built `inst.Mappings`
   (source annotation → clone) by zipping two BFS node sequences. `Data` and `inst.Output`
   (`= Data.Clone()`) are isomorphic and the result dict is order-independent, so a new
   `DataStructuresExtensions.PairedPreorderTraverse` walks the two forests in lockstep and writes
   pairs straight into the dict via a **static (closure-free)** callback — no `Queue`/`SelectMany`/
   `Zip`/`KeyValuePair` per Transduce. `Debug.Assert`s guard the isomorphism invariant (root/leaf/
   child-count) so a future violation fails loudly instead of silently truncating like `Zip`.
5. **`Fst.Transduce`: precompute the initializer partition at `Freeze`.** `cmds` was rebuilt every
   call by filtering `_initializers` (`Dest!=0`), and is read-only downstream. Partition once in
   `Freeze()` into `_zeroDestInitializers`/`_nonZeroDestInitializers` and reuse the shared read-only
   list as `cmds` (fall back to the inline build when the FST isn't frozen). Eliminates a
   `List<TagMapCommand>` + filter loop per Transduce. Safe to share read-only across the parsing
   threads (the frozen grammar is immutable).

**Measured (en-hc toy grammar, `SenaQuick`, 439 words, Release):** KB/word **80.3 → 78.8**, totalMB
34 → 33; `Scaffold` 22.4% → **21.0%** (−636KB, from #5). `SenaParallel` scaling unchanged (5.2–5.4×
at 16T, MB flat) — no parallel regression. **These deltas are directional only:** the toy grammar
under-measures #3/#4 (clone- and branch-heavy, which dominate at Sena's ~276 clones/word) and #1
(CPU, not allocation), so **no-regression on the full suite is the headline bar**, exactly as Phase
4a. The real magnitude needs the Sena grammar wired via `HC_GRAMMAR`/`HC_WORDS` (only a FieldWorks
`.fwbackup` is present locally, not the generated `sena-hc.xml`).

All five are in `SIL.Machine` core (used far beyond HC — Matcher, Corpora, Translation), so each was
gated on the **full** `SIL.Machine` suite, not just HC, per the guardrail.

## Phase 6 — Decision gate (conclusion)

**What shipped (safe, measured, tested, committed on `hc-rustify`):**

| Lever | en-hc | Sena (`SenaQuick`) | Tests |
|------|------|--------------------|-------|
| Copy-on-write `FeatureStruct` | −20% bytes/word | −14% bytes/word, **+9.5% throughput** | 803 + 63 green |
| Pool `Shape.CopyTo` map | ~0 | −0.45% bytes/word, Gen0 2621→2561 | 803 + 63 green |

A single safe, one-file change (COW) recovered ~14–20% of allocation and ~10% throughput on the
real grammar — confirming the thesis that **the Rust advantage here is memory architecture, and it
is recoverable in C#.**

**Where the rest of the allocation lives (now measured precisely).** A per-thread allocation probe
around `Word.Clone` (`AllocationProbe` hook + `SenaQuick`) splits the ~11.8 MB/word as:

- **~20% inside `Word.Clone`** (the `Shape`/`ShapeNode`/`Annotation` deep copy, ~276 clones/word).
- **~80% in the FST traversal + cascade** — a *fresh* `…TraversalMethod` (+ its `_annotations`
  `List` and `_cachedInstances` `Queue`), `cmds`/`resultList`, plus per-arc `List`s,
  `VariableBindings.Clone()`, 2-D register-snapshot clones and `FstResult`s — allocated on **every
  rule application**, thousands of times per word.

**This redirects the plan:** the dominant lever is the **FST traversal**, not `Word` pooling. The
single biggest structural waste is that the per-traversal **instance cache (`_cachedInstances`) is
thrown away every `Fst.Transduce` call** — a new traversal method (and its pools) is built per rule
application, so the existing instance reuse never survives across the thousands of calls per word.
Making the traversal method + its instance/register buffers **per-thread and reused across
`Transduce` calls** is the high-ROI Phase-1/4 target (escaping/reentrant within a single traversal,
so it needs a per-traversal arena, but it is contained to `FiniteState/`). Cutting `Word.Clone`
(Shape sharing/pooling) is the secondary ~20% lever.

**The decision.** Continue in **C#, not Rust.**
- The architecture analysis put data-oriented C# within **~1.2–2×** of a Rust port for this
  GC-bound workload; the first safe lever already moved the needle in that direction.
- A Rust kernel is the ~13k-LOC engine boundary + N-platform native packaging + a **third** engine
  to keep correct (alongside `machine` C# and `machine.py`). Not justified while C# levers remain.
- **Next chunk (recommended):** Phase 1 per-thread pooling of `Word`/`ShapeNode`/`Annotation` and
  the FST per-arc buffers — now *measurable* via `SenaQuick`. Expected to also lift the 16-thread
  scaling past today's ~2.8× by removing shared-heap GC contention. Land it incrementally behind
  the byte-identical + determinism gates, each sub-step recorded in the results table.
- Phase 3 (struct-of-arrays / indices-not-pointers for the shape graph) is the follow-on once
  pooling caps allocation, to convert the saved GC time into cache-locality speedups.

**Phase 5 (NativeAOT worker) — decided, deferred.** Measured earlier: a self-contained net10
worker is ~100 MB (vs ~46 MB framework-dependent) and removes the end-user .NET-runtime
prerequisite; AOT recovers startup but **not** GC. It belongs with the FieldWorks out-of-process
integration, not this in-process allocation work, so it is tracked there rather than here.

**Bottom line:** the plan is proven and the loop is in place — one safe change banked a credible
−14% on Sena, the dominant remaining cost is localized to pooling, and the recommendation is to
keep capturing Rust's *architecture* in C# (pooling → SoA) rather than adopt Rust's *runtime*.

**The 16-thread result, plainly:** in-process under Workstation GC, "Parse All Words" tops out at
**~3.5× (8 threads) and regresses at 16** — a *fundamental* Workstation-GC ceiling (stop-the-world
Gen2 serializes all threads), which more allocation-reduction can't lift and pooling actually hurts.
Under **Server GC** (the out-of-process worker) the same morpher now scales to **11.2× at 16
threads** with the FST-traversal pooling — **allocation −16% on top of COW's −14%, and Gen0 cut from
~580 (Workstation) to 42**. So at 16 threads **GC no longer dominates**: the collector went from the
hard ceiling to a non-bottleneck. The lever combination is **Server GC + reduced allocation (COW +
Server-GC-gated FST pooling)**; Workstation in-process stays best at ~8 threads and is left untouched.

## Phase 4d — Cleanup: pooling/arena machinery removed (allocation made it unnecessary)

**Status: done.** With allocation driven down by COW (Phase 2), bit-packed unify (Phase 3) and the
Phase 4a/4c eliminations, the GC-*coping* machinery this document explored is no longer earning its
keep and has been removed (it was off by default and, for the parallel goal, actively harmful — the
Phase-1b lesson that allocations should **die young, not be pooled**). Removed:

- **`FstThreadPool` + `Fst.TraversalPoolEnabled` + the pooling branch in `Fst.Transduce`**, and
  `FstThreadPool.Reset()` in `Morpher.ParseWord`. `Transduce` now always builds a fresh, short-lived
  (Gen0) traversal method. *(References to these above are retained as the historical record of the
  Phase-1b/4b experiments; the code itself is gone.)*
- The out-of-process **Server-GC worker** (already removed earlier, Phase 5 ✗) and an unrelated
  **USFM/versification** change set that had ridden along on the branch.

The **`Shape.CopyTo` `[ThreadStatic]` clone-map pool was kept** after re-examination: it is a *safe*
pool, not the regressive kind. The Phase-1b regression came from objects retained **across** a word
(promoted to Gen2). `Shape.CopyTo`'s `CloneMapping` is cleared and fully consumed **within** each
call — its contents die immediately and only a small empty buffer persists — so it cannot promote
parse data to Gen2, and it still buys a small allocation win (~0.45% on Sena, +1.5% of `Word.Clone`
on the toy grammar). Its value-added companion (building the `src→dest` map inline during the clone
instead of a second `GetNodes().Zip().ToDictionary()` pass) is of course kept too.

**Kept:** the allocation instrumentation (`MorpherStatistics`/`FstStatistics` + probes) and the
`MorpherBenchmark`/`RustifyBenchmark` harnesses, for before/after measurement; the
`MaxDegreeOfParallelism` API (across-word parallelism, single-threaded within a word) and the
`Synthesize` refactor that replaced the `#if SINGLE_THREADED` compile flag; COW; bit-packed unify;
and the Phase 4a/4c eliminations. **Net effect:** the surviving wins are pure allocation *reduction*
(no retention, no GC tuning), which is the conclusion the whole exercise converged on.
