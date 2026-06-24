# Cutting the FeatureStruct clone firehose — 3 plans (scoped by subagents)

Problem: ~371 MB and ~8,793 `Word.Clone` per word on the real Sena grammar; each clone
deep-copies the `Shape` and every node's `FeatureStruct` (~tens of thousands of
`FeatureStruct.Clone()`/word). ~99.9% of clones are discarded by search pruning. This
allocation caps parallel scaling.

Three approaches were scoped/derisked against the code by independent subagents. They
converge: this is a **whole-Word/Shape speculative-clone** problem, best solved by
**copy-on-write**, not a unification-algorithm problem.

## The reframe (the "look at it differently" answer)

The computational-linguistics "copying problem" literature (Tomabechi quasi-destructive
unification; Wroblewski; Kogure lazy-incremental-copy; van Lohuizen thread-safe QD)
optimizes copying *inside graph unification*. **That is the wrong layer here.** Tracing the
code: the matching hot path (`FeatureStruct.IsUnifiable`) is already **read-only and
non-copying** — and that read-only-ness is exactly what makes the current parallel parsing
thread-safe over the one shared frozen grammar. The 371 MB is whole-`Word.Clone` →
`Shape.CopyTo` → per-node `FeatureStruct.Clone`. So:
- Quasi-destructive unification would optimize a cold path AND its node "temp marks +
  generation counters" would *add a data race* to the currently-safe shared-grammar reads.
  **Rejected.**
- The transferable principle — "copy only on a needed, successful mutation" — is real, but it
  belongs at the **Word/Shape/FeatureStruct** level, i.e. copy-on-write.

## Plan A — Copy-on-write `FeatureStruct` (RECOMMENDED)

`FeatureStruct.Clone()` of a **frozen** FS returns a thin shell that shares the frozen backing
dictionary; the first mutating call inflates it (deep-copies via the existing `CloneImpl`)
before mutating. Replace the single `CheckFrozen()` guard with `EnsureWritable()` at the
existing mutation entry points.

- **Why it works:** every mutation already funnels through one guard at the FeatureStruct
  public boundary (leaves/nested children have no guard of their own), so intercepting there
  is sufficient and safe. Inflation reuses the proven deep clone, so nesting/variables/
  reentrancy are correct *by construction*.
- **Blast radius:** `FeatureStruct.cs` only. No public API change. No HermitCrab changes.
- **Thread-safety:** safe — shells are per-clone/per-thread; shared frozen backing is only
  read; `Freeze()` already pre-computes the hash so reads never lazily write.
- **Gain:** HIGH — most of the ~88k cloned FSes/word are never mutated → collapse to O(1)
  shells (no dictionary, no entries, no leaf `HashSet`s), so the bulk of the 371 MB/word and
  the GC pressure that caps parallel scaling largely vanish. Mutated FSes pay the same cost as
  today (deferred to first write).
- **Risk: LOW. Feasibility: HIGH.**
- **Derisk:** invariant tests (mutating a clone never changes the frozen source; frozen stays
  immutable; hash/equality stable across share-then-inflate); the 36 tests that broke under
  naive sharing are the canary; ship with "always inflate immediately" first to prove
  semantic equivalence, then enable lazy sharing; re-run Sena rig + parallel scaling.

## Plan B — Copy-on-write `Shape`/`ShapeNode` (complements A)

Defer cloning a node's FS until a rewrite actually mutates that node; share unchanged nodes
across clones. Targets two speculative clones the profiling/subagents pinpointed:
`AnalysisStratumRule.cs:115` (clone-per-stratum then rewrite in place — wasted entirely if no
phonological rule fires) and the post-match morphological-rule fan-out (each clone mutates
only a few nodes).

- **Blast radius:** `Shape`, `ShapeNode`, `AnnotationList`, `CopyTo` — core, broadly used.
- **Gain:** HIGH on the speculative clones; also removes per-clone `ShapeNode`/`Dictionary`
  allocations that Plan A leaves untouched. **Stacks with A.**
- **Risk: MEDIUM** (the Shape mutation model is more spread out than FeatureStruct's single
  guard). **Feasibility: MEDIUM.**
- Best pursued *after* A proves out; A is the bigger, cheaper win.

## Plan C — Persistent `FeatureStruct` map (the "red-black tree" idea)

Replace the internal `Feature→FeatureValue` dictionary with a persistent immutable map
(structural sharing): `Clone()` shares the root (O(1)); a mutation creates O(log n) new path
nodes.

- **Critical catch found in scoping:** a spine-only persistent map is **insufficient** — the
  leaves (`SymbolicFeatureValue._flags`, `StringFeatureValue._values`) and nested FSes are
  mutated **in place** by `Union`/`Add`/`Subtract`. So **leaf copy-on-write is mandatory**,
  extending the blast radius to the three value classes + a build-phase mutable buffer.
- **Do NOT use `System.Collections.Immutable.ImmutableDictionary`** (~10× slower than
  `Dictionary`, allocates per `SetItem`; FSes are tiny so it would regress the build phase).
  Use a hand-rolled COW small-array / compact persistent tree with a builder.
- **Gain:** HIGH (similar to A on the clone-heavy phase), but its only edge over A is cheaper
  *mutation* (O(log n) vs A's full inflate) — and mutations are rare (few nodes/rule), so that
  edge is small for this workload.
- **Risk: MEDIUM. Feasibility: MEDIUM.** More files, leaf-COW mandatory, build-phase
  regression risk.
- **Verdict:** viable but **dominated by Plan A** for this workload — same gain on the common
  path, far less risk/blast-radius. Choose C only if cheap *mutation* later proves to matter.

## Recommendation

1. **Plan A (COW FeatureStruct)** — do this first. Highest gain-to-risk: single file, no API
   change, thread-safe, reuses the existing deep clone for correctness.
2. **Plan B (COW Shape)** — stack on top of A to remove the residual `ShapeNode`/`Dictionary`
   per-clone allocation and kill the wasted per-stratum clone.
3. **Plan C / quasi-destructive unification** — not recommended (C is dominated by A; QD
   optimizes the wrong layer and would break parallel thread-safety).

## Sources
- Tomabechi, Quasi-Destructive Graph Unification (1991/1992): https://aclanthology.org/1991.iwpt-1.19.pdf , https://aclanthology.org/C92-2068.pdf
- van Lohuizen, Memory-Efficient and Thread-Safe Quasi-Destructive Graph Unification (2001)
- Pereira, A Structure-Sharing Representation for Unification-Based Grammar (1985): https://aclanthology.org/P85-1017/
- Kogure, Strategic Lazy Incremental Copy Graph Unification: https://aclanthology.org/C90-2039.pdf
- .NET ImmutableDictionary perf: dotnet/runtime #47812, #26001
- Roslyn red-green trees: https://github.com/dotnet/roslyn/blob/main/docs/compilers/Design/Red-Green%20Trees.md
