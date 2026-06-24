# Allocation / GC strategies for the HermitCrab morpher

Research + analysis of how to get past the per-parse allocation / GC ceiling that
FieldWorks measured (parallel scaling caps at ~3×; summed CPU inflates 5× under 20-way
concurrency — a GC/memory-bandwidth signature). Goal: rank known strategies by promise
*for this specific code*.

## What actually allocates here (grounded in the code)

The rule engine is functional-in-spirit but **eager-copy in implementation**: every rule
application clones the whole `Word`, mutates the clone, and dedups results into sets. So
allocation ≈ (number of search nodes) × (cost per clone). Both factors are large.

1. **`Word.Clone()` → `Shape.Clone()` → `Shape.CopyTo` (the firehose).** Per clone:
   - a new `ShapeNode` for every node in the shape (O(N)),
   - a cloned `FeatureStruct` annotation per node (O(N) feature structs),
   - **a `Dictionary<ShapeNode,ShapeNode>` built with `.Zip().ToDictionary()`** every single
     clone (`Shape.cs:121-123`) — dict + 2 enumerators per clone.
   Clones happen on essentially every rule (un)application, inside the combinatorial search.
2. **Combinatorial search bookkeeping (tiny-object flood).** `CombinationRuleCascade` /
   `ParallelCombinationRuleCascade` allocate, *per search node*, `new HashSet<int>(applied){i}`
   to track applied rules, plus `Tuple<…>` (System.Tuple is a **reference type** → heap).
   A search exploring thousands of nodes = thousands of tiny short-lived allocations →
   Gen0 spikes → exactly the parallel-scaling killer FieldWorks saw.
3. **Dedup churn.** `HashSet<Word>` keyed by `FreezableEqualityComparer<Word>` re-hashes the
   *entire shape* (`Shape.GetFrozenHashCode` walks all nodes) on every insert, and
   `.Distinct(FreezableEqualityComparer<Word>.Default)` is sprinkled after cascades that
   already deduped.
4. **FeatureStruct unification** in FST traversal allocates result structs + variable-binding
   clones (in `SIL.Machine` core).

## Strategies considered

### A. Red-green / persistent immutable Shape with structural sharing
Roslyn's solution: make `Shape`/`ShapeNode` immutable "green" nodes so a clone **shares all
unchanged nodes and only allocates the path to the change**. Most rule applications touch a
small part of the shape, so clones drop from O(N) to O(changed).
- **Promise: highest ceiling** — attacks the dominant allocator (#1) at the root, and would
  also make equality/hashing shareable.
- **Risk: very high.** The engine assumes *mutable* shapes (clone-then-mutate in place;
  `_prulesRule.Apply(input)` mutates `input`). Converting `Shape`/`ShapeNode`/`AnnotationList`
  and every rule's mutation to persistent nodes is a deep, multi-month rewrite with high
  regression risk. Not a "small targeted improvement."
- **Verdict: best long-term direction, wrong for now.**

### B. Object pooling (ArrayPool / custom pool / free lists)
Reuse `Word`/`Shape`/buffer objects instead of allocating. Precedent exists: the FST
traversal already pools instances (`TraversalMethodBase._cachedInstances`).
- **Promise: high for *transient* objects** that don't escape.
- **Risk: high for Words** — they escape into result sets, get frozen, and are stored as
  `Alternatives`; pooling escaping/shared objects needs ownership tracking and is bug-prone.
  Best applied to short-lived buffers, not Words.
- **Verdict: medium; reserve for buffers, not the Word graph.**

### C. Value-type search bookkeeping: bitmask + `ValueTuple` (attack #2)
Replace the per-search-node `HashSet<int>` (applied-rules set) with a **`ulong` bitmask**
(strata rarely exceed 64 morphological rules; fall back to a small bitset struct otherwise),
and replace `System.Tuple<…>` (class) with `readonly struct` / `ValueTuple` (no heap).
- **Promise: high and targeted.** Eliminates thousands of tiny Gen0 allocations in the
  hottest combinatorial loop — precisely the allocation pattern that throttles parallel
  scaling.
- **Risk: low-moderate**, localized to the two cascade classes; behavior-preserving.
- **Verdict: TOP near-term pick.**

### D. Kill redundant clones + LINQ in the clone path (attack #1's constant + #3)
- Replace `Shape.CopyTo`'s `.Zip().ToDictionary()` with a manual loop that builds the node
  mapping inline while copying — removes a `Dictionary` + 2 enumerators **per clone**.
- Drop `.Distinct(FreezableEqualityComparer<Word>)` where the cascade already returns a
  deduped `HashSet`.
- **Promise: medium-high, and genuinely "small/targeted."** The per-clone `Dictionary`
  removal scales with clone count (which is huge).
- **Risk: low.**
- **Verdict: TOP near-term pick (do alongside C).**

### E. Sub-word caching / memoize the cascade on `(shape, appliedRules)` (attack clone COUNT)
Don't re-expand search states reached by multiple paths. `MergeEquivalentAnalyses` already
does shape-merge at *stratum boundaries*; this pushes it *into* the search.
- **Promise: high** — fewer search nodes ⇒ fewer clones ⇒ less GC *and* less compute; the
  only lever that also attacks the exponential.
- **Risk: moderate-high** (memo key vs full `Word` identity; correctness). Needs the
  collision-rate instrumentation first.
- **Verdict: best medium-term, not "small."**

### F. FeatureStruct interning / flyweight (attack #1's per-node FS clone + #3 hashing)
Intern frozen `FeatureStruct`s into a canonical table so clones share references, equality
becomes reference compare, and hashes are cached. Many feature structs across a grammar are
identical.
- **Promise: medium-high** (FSes are pervasive; helps both allocation and equality cost).
- **Risk: medium** — `FeatureStruct` is core and used everywhere; interning must respect
  freezing/mutation.
- **Verdict: medium-term.**

### G. Struct-ify `ShapeNode` / data-oriented (struct-of-arrays) Shape
Make nodes value types / store shapes as parallel arrays to cut per-node object overhead and
improve cache locality.
- **Promise: high ceiling.**
- **Risk: very high** — same blast radius as A (intrusive linked list with annotation
  back-references). Huge rewrite.
- **Verdict: not now.**

### H. GC configuration (Server GC, `GCSettings.LatencyMode`)
Not a data-structure change, but FieldWorks measured Server GC ≈ 2×. App-side lever; nothing
to change in the library, but it's complementary to every option above and the cheapest win.
- **Verdict: do first (app side); it's the host app's call.**

## Ranking (promise ÷ risk, for *this* code, now)

| Rank | Strategy | Horizon | Why |
|---|---|---|---|
| 1 | **D — kill `CopyTo` Dictionary + redundant `Distinct`** | now | per-clone allocation, trivial risk |
| 2 | **C — bitmask + `ValueTuple` in cascades** | now | kills the tiny-object flood that throttles scaling |
| 3 | **H — Server GC (app side)** | now | ~2×, free, FieldWorks-confirmed |
| 4 | **F — FeatureStruct interning** | mid | pervasive FS allocation + equality |
| 5 | **E — sub-word memoization** | mid | fewer clones + attacks exponential; needs collision data |
| 6 | **A / G — persistent/red-green Shape** | long | highest ceiling, but a rewrite |
| — | **B — pool Words** | — | unsafe (Words escape); pool buffers only |

## Recommended sequence

1. **Confirm the attribution**: one Gen0-allocation-by-type capture (dotMemory /
   `dotnet-counters` / `DOTNET_gcServer`) on a real grammar Parse-All, to verify `Shape.CopyTo`
   + cascade bookkeeping dominate (code strongly implies it; measure to be sure).
2. **Do C + D** (value-type cascade state, manual `CopyTo` mapping, drop redundant `Distinct`).
   Low risk, measure Gen0 drop with `MorpherStatistics` + the profiler. Expect both faster
   single parses *and* a higher parallel ceiling (less GC contention).
3. Re-measure parallel scaling. Then evaluate **F** (interning) and **E** (memoization, after
   collision instrumentation).
4. Treat **A** (persistent Shape) as the long-term architectural north star, scoped separately.

## Sources
- Reducing GC pressure in .NET (pooling, Span, structs): https://dev.to/adrianbailador/reducing-garbage-collector-gc-pressure-in-net-practical-patterns-and-tools-5al3
- Span<T>/stackalloc/ArrayPool to eliminate allocations: https://dev.to/danqzq/c-performance-optimization-using-span-and-stackalloc-to-eliminate-allocations-ikc
- Persistent data structures / structural sharing: https://en.wikipedia.org/wiki/Persistent_data_structure ; https://medium.com/@dtinth/immutable-js-persistent-data-structures-and-structural-sharing-6d163fbd73d2
- Roslyn red-green trees (parser-specific structural sharing): https://github.com/dotnet/roslyn/blob/main/docs/compilers/Design/Red-Green%20Trees.md ; https://ericlippert.com/2012/06/08/red-green-trees/
