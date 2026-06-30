# RUSTIFY Stage 3 — flat-shape `Word.Clone` design + sequencing

> **Status:** design (2026-06-29). Precedes the multi-session "build red until complete" spike the
> user approved (plan-then-proceed). Companion to `RUSTIFY.md`; this file is the execution blueprint
> for the foundational clone rewrite. Rollback point if the spike is abandoned: commit `dbef327a`
> (clean/green: 4 tests fixed, Stage 2 closed, Stage 3 cost localized).

## 1. Goal & measured target

Cut the `Word.Clone` allocation — **22% of total** on Sena (137,951 clones over 400 words, ~9.7 KB/clone).
The two-phase probe localized it (Sena):

| `Word.Clone` sub-phase | % total | what allocates |
|---|---:|---|
| `CopyTo` **node phase** | **11.4%** | per node: `ShapeNode` handle + its `Annotation` + COW `FeatureStruct` + the `AnnotationList` skip-list tower (`_next[]`/`_prev[]`) for that annotation, via N× `dest.Add` |
| `CopyTo` **annotation phase** | 4.1% | per morph: a new spanning `Annotation` + its tower + recursion |
| Word/Shape ctor remainder | ~6.9% | Word's own dicts/lists/FS clones + Shape's 4 backing arrays |

Two incremental attacks already measured ~0/negative and were reverted (dedup-key `Tuple`→struct; array
pre-size). **The cost is inherent per-node object materialization** — only stopping the materialization
moves it. Target: make `Shape.Clone` an `Array.Copy` of the backing + a flat annotation-record copy, with
**no per-node `ShapeNode`/`Annotation`/skip-list-tower allocation** in the common (clone → freeze →
traverse-via-int-projection) path.

## 2. The entanglement (why this is one integrated spike, not increments)

Three reference-identity couplings force the change to land together:

1. **`ShapeNode` is reference-identity.** `==`, `GetHashCode`, dictionary keys, and `Range<ShapeNode>`
   endpoint identity across HC (189 refs / ~30 files) and `SIL.Machine` all rely on one canonical handle
   object per slot. Stage 1 deliberately kept that ("the `ShapeNode` added is retained as the canonical
   one-per-slot handle") — which is exactly why clone still materializes N handles.
2. **`Annotation`s reference nodes by `Range<ShapeNode>`.** Cloning the annotation tree therefore *forces*
   handle materialization (the range endpoints are handles). So lazy handles are impossible while
   annotations hold handle refs.
3. **`AnnotationList` is a skip-list of `Annotation` nodes**, each carrying its own `_next[]`/`_prev[]`
   tower arrays (`BidirListNode`). Cloning via N× `Add` allocates N annotations + 2N tower arrays + an
   O(log n) walk + a random level per node.

⇒ A cheap clone needs all three undone at once: (a) lazy `ShapeNode` handles, (b) annotations addressed by
node **index** not handle, (c) a flat-array `AnnotationList` whose clone is an `Array.Copy`. Undoing any
one alone yields nothing (the other two still force the allocation) — hence the integrated spike.

## 3. The hard constraint: dense index vs in-place mutation (the advisor's split)

Stage 2 chose **dense** per-projection offsets (Begin=0, content 1..N, End=N+1), correct for the frozen
FST traversal. But phonological **rewrite rules mutate an UNFROZEN shape in place** (`AddAfter`/`Remove`/
`MoveNodesAfter`) and re-match — re-densifying offsets every mutation. That is precisely why the shipped
fixes in `IterativePhonologicalPatternRule`/`SynthesisMetathesisRuleSpec` "resolve EVERYTHING to ShapeNode
refs up front." So **index-addressed annotations are valid for a frozen/read-only shape but go stale under
in-place mutation.**

Resolution — **materialize-on-touch, two-state shape:**

- A **clone starts as a flat snapshot**: `Array.Copy` of `_next`/`_prev`/`_frozen`/tags + a flat copy of
  the annotation records (range-as-index, FS by COW ref, optional, parent/child). **No `ShapeNode` handle
  and no `Annotation` object is created.**
- As long as the shape is only **read** — the FST traverses it through the **int projection** (Stage 2,
  which already needs no `ShapeNode`), it is frozen, results are inspected via the int range — it stays
  flat. This is the hot analysis path → clone becomes ~pure `Array.Copy`. **The payoff.**
- The moment a `ShapeNode` handle or `Annotation` object is **requested** (rule RHS navigation, a
  `Range<ShapeNode>` API), it is **materialized lazily and cached one-per-slot** — restoring exact
  reference identity for that node. Mutation (`AddAfter`/`Remove`) forces materialization of the touched
  region first, so the existing handle-based mutation path (and the Stage-2 "resolve to ShapeNode before
  mutating" fixes) keep working unchanged.

So: **frozen-read sites pay nothing; unfrozen-mutate sites pay the old price (materialize) but are far
colder.** This is the advisor's frozen-read-safe / unfrozen-mutate-unsafe split made concrete.

## 4. Target end-state representation

- **`Shape`** keeps its flat node backing (Stage 1: `_nodes`/`_next`/`_prev`/`_frozen`, dense `Index`).
  `_nodes[i]` becomes **lazily populated** (null until the handle for slot `i` is first requested).
- **`ShapeNode`** = handle `(Owner, Index)`; `==`/`GetHashCode`/`ValueEquals` already delegate to
  `(owner, index)`/`Tag`. Materialization = `_nodes[i] ??= new ShapeNode(owner, i)` rebuilt from the
  backing (FS pulled from a flat `FeatureStruct[]` the shape owns, COW-shared from the source).
  ⇒ `FeatureStruct` moves off the per-node `Annotation` into a shape-owned `FeatureStruct[]` so the clone
  can `Array.Copy` (COW-share) it without touching handles. (Stage-1 note already anticipated this.)
- **`AnnotationList`** for a shape becomes **flat-array-backed** (records: int range start/end, FS ref,
  optional, parent/first-child/next-sibling indices or an order index), mirroring the Shape rework. Clone
  = `Array.Copy`. The `Annotation<ShapeNode>` object is a **lazily-materialized handle** over a record,
  same pattern as `ShapeNode`. The existing `AnnotationList<int>` projection (Stage 2) is the read view
  the FST consumes; it is derivable directly from the flat records with no handle materialization.
- **Identity:** `ShapeNode`/`Annotation` equality stays value-based `(owner, index)`; the
  one-per-slot caching preserves `ReferenceEquals` for any handle actually materialized, so dictionary-key
  and `==` semantics are byte-identical for code that does touch handles.

## 5. Byte-identical risks (the gate is correctness, enumerated)

1. **Reference identity for code that mixes touched + untouched handles.** If site A materializes node 3
   and site B compares it to a freshly materialized node 3, they must be the *same object* → the
   one-per-slot cache must be authoritative and populated atomically. Risk: a handle materialized, then the
   slot freed/reused by mutation, then re-materialized as a different object. Mitigate: materialization and
   slot-free both go through the cache; freed slot clears its cache entry.
2. **`Range<ShapeNode>` ordering / `Overlaps` / `Contains`.** Must stay identical to the inclusive
   `ShapeRangeFactory` semantics. The int projection parity test (`IntOffsetRangeMapping`) already guards
   the index↔handle range mapping; extend it to the materialized-annotation path.
3. **Frozen hashing.** `ShapeNode.GetFrozenHashCode == Tag`, `ValueEquals` (frozen ⇒ tag compare). Tags
   must survive the flat clone (`Array.Copy` the tag array). `Shape.GetFrozenHashCode` /
   `AnnotationList.GetFrozenHashCode` must be computable from flat records without materializing.
4. **Annotation tree (subsume parent/child).** The morph-over-segment subsume structure must be cloned
   exactly. Flat records must encode parent/child; the `ValueEquals` used by tests compares the tree.
5. **Mutation correctness.** `AddAfter`/`Remove`/`MoveNodesAfter`/`RelabelMinimumSparseEnclosingRange` on
   a partially-materialized shape. Rule: any mutation materializes the affected nodes + annotations first
   (fall back to today's path), so mutation logic is unchanged. The win is only that *un-mutated* clones
   never materialize.
6. **Concurrent determinism.** The one-per-slot cache must be safe under the per-parse threading model
   (shapes are per-parse, not shared across threads while mutable; the frozen shared grammar is read-only).
   Lazy materialization of a *frozen shared* shape (if any) needs a thread-safe cache or eager freeze-time
   materialization. **Verify no frozen shape is shared across parse threads and lazily materialized.**

## 5.5 Pre-III verification gate (front-loaded, read-only, decisive — advisor review)

The payoff is decided by the O(N)-per-clone **read** consumers in `SIL.Machine` core — which are *not* in
the 189 HC `ShapeNode` refs (those are mostly the colder mutate path). If any read consumer walks handles
instead of flat records, the win silently reverts to zero. So before writing III, prove each can run from
flat records with **zero handle materialization**:

1. **LINCHPIN — the int projection.** `EnsureIntProjection`/`ProjectAnnotation` today walk `_annotations`
   reading `src.Range`/`FeatureStruct`/`Optional` — i.e. they materialize handle annotations on *every
   traversed clone*. The entire payoff requires rebuilding the projection from flat records, handle-free.
   If that is not achievable, the design collapses — verify first (it is downstream of II).
2. **`UpdateOutput` touches O(few) nodes per clone, not O(N).** The dominant clone source is FST-internal
   (`InitializeStack`/`CopyInstance` outputs), and each is mutated by `UpdateOutput`. Materialize-on-touch
   wins only if that touches a handful of nodes per clone; if it touches all, it materializes everything
   and the clone is no cheaper. **This is the make-or-break premise for the dominant population.**
3. **Result consumers (far fewer — hit results, not all clones, so don't overweight):** `Shape.ValueEquals`
   → `_annotations.ValueEquals` (two-shape walk) + `GetFrozenHashCode`; `Word.Morphs`. Each must be
   computable from flat records.

**Measure II before III (the go/no-go input).** II (flat `AnnotationList`, no laziness) removes the
per-annotation skip-list tower arrays and is byte-identical-gateable. Its Sena `SenaQuick` delta reveals how
much of the node-phase 11.4% is *towers* vs *`ShapeNode`/`Annotation` objects*: if towers dominate, II banks
most of the win green and **III's laziness risk may be unneeded**; if objects dominate, III is required —
and only then under checks 1–3 above.

**MEASURED (2026-06-29, Sena, temporary tower-allocation probe in `BidirListNode.Init` gated to
`Annotation<ShapeNode>` during the parse):**

> **Annotation skip-list towers = 7.4% of total allocation (~432 MB; 6.31M tower arrays, 36.4M ref slots,
> ~46 arrays/clone).** That is a **third of all `Word.Clone` (22.4%)** and **two-thirds of the
> node-phase (11.4%) + anns-phase (4.1%)**.

⇒ **The spike is resequenced.** Increment **II is the headline win — ~7.4% of total, byte-identical,
gateable green, zero laziness risk** — and it is independently keepable. Increment **III's lazy-handle
materialization now buys only the residual ~8%** (the `ShapeNode` + `Annotation` *objects* themselves), so
**III is downgraded to optional / gated** on whether that residual justifies the multi-file reference-
identity risk + the read-consumer audit (checks 1–3). The high-ROI, low-risk move is: **do II, land it
green, measure; then decide III on the residual.** The towers were the cheap two-thirds hiding behind the
"inherent objects" framing — exactly the kind of thing the measure-first discipline surfaces.

## 6. Sub-increment sequencing (red between III–IV; gate where possible)

- **I. Move `FeatureStruct` to a shape-owned flat `FeatureStruct[]`** (handle reads delegate). Byte-
  identical, gateable green. Prereq for an `Array.Copy` clone of FS (COW-shared). **Caveat (advisor):**
  detached nodes (`new ShapeNode(fs)` before `Add`) and morph-annotation FS have no slot in the node
  array → keep a detached-FS fallback field; the array is authoritative only once the node is owned.
- **II. Flatten `AnnotationList` backing into flat arrays** (records + flat skip/order links on the list,
  like Shape Stage 1), keeping the `Annotation` handle API and reference identity. Byte-identical,
  gateable green. Removes the per-annotation tower allocation even before laziness.
  - **II-a (LANDED ✅, green) — grow the Begin/End margin towers on demand instead of eager `[33]`.**
    Every `BidirList` ctor `Init`'d both margins at the 33-level skip-list maximum (`new TNode[33]` ×2)
    even though most lists stay shallow; this was a large slice of the tower allocation. Now margins start
    at level 0 and `GrowMargins` resizes + links a level only when a node first reaches it (`Clear` resets
    to level 0; higher levels relink lazily on regrowth). **Measured: Sena `Word.Clone` −123 MB
    (1,306,476→1,182,940 KB, −9.5% of `Word.Clone`, stable across runs); Indonesian similar. Full suite +
    concurrent-determinism green.** Contained to `BidirList`/`BidirListNode`; does not touch `Annotation`/
    `ShapeNode` identity, so it is independently keepable regardless of II-b/III.
  - **II-b (LANDED ✅, green) — inline level 0 of every node's tower as fields.** Level 0 (the only level
    ~50% of nodes have) moves from `_next[0]`/`_prev[0]` into `_next0`/`_prev0` fields, so level-0 nodes
    allocate **no tower array at all** and every taller node's array is one slot shorter; levels 1.. live in
    `_nextHigh`/`_prevHigh` (null when `Levels<=1`). Touches the hottest skip-list accessors
    (`GetNext`/`SetNext`/`GetPrev`/`SetPrev`/`Next`/`Prev`/`Init`/`Clear`/`EnsureLevelCapacity`) — gated on
    the full suite + concurrent-determinism (green). **Measured: Sena `Word.Clone` −54 MB
    (1,182,940→1,128,660 KB) on top of II-a; Indonesian −9.6 MB.**
  - **Cumulative II-a + II-b (vs pre-Stage-3):** Sena `Word.Clone` **−177 MB (−13.6%)**, total allocation
    **−4.2%** (KB/word 14,556→13,942); Indonesian total **−4.1%** (8,688→8,335). Both byte-identical, pure
    allocation reduction, no retention. Independently keepable regardless of III.
  - **II-c (optional, next) — remove the residual high-level tower arrays** (list-owned flat link backing,
    or inline level 1 too). Diminishing returns: levels ≥1 are ~50% of nodes and shrinking; weigh vs III.
- **III. (LANDED ✅, green) — copy-on-write Shape** (the §6.5 mechanism, simpler than the per-node lazy-
  handle scheme and it stayed green throughout). A clone of a frozen shape stores `_cowSource` and copies
  nothing; `IntAnnotations`/`IntRange`/`Count`/`GetFrozenHashCode`/`Freeze` are served from the frozen
  source, while the flat-backing link accessors (`GetNextLink`/`GetPrevLink`), `First`/`Last`/enumeration,
  `NodeAt`/`OffsetOf`/`Annotations`/`GetNodes`/`CopyTo`/`ValueEquals` and every mutator call `EnsureInflated()`
  (the real `CopyTo`, then re-freeze if it had been frozen-by-sharing). A clone that is only traversed via
  the int projection never inflates → costs a shell, not N nodes + N annotations + towers. Thread-safety:
  the frozen source's projection is now built **eagerly at `Freeze()`** (single-threaded) so concurrent COW
  delegation hits a complete cache, never a racing lazy build. **Measured: Sena `Word.Clone` 1,128,660 →
  528,071 KB (−53% on top of II; 20.2%→9.9% of total); Indonesian 212,911 → 85,566 KB (−60%). Full suite +
  concurrent-determinism green; `SenaParallel` scaling unregressed; total allocation MB down.**
- **Cumulative Stage 3 (II-a + II-b + III) vs pre-Stage-3:** Sena `Word.Clone` **−778 MB (−59.6%)**, its
  share **22.4% → 9.9%**; Indonesian `Word.Clone` **−62%**. The flat-clone goal — `Word.Clone` is no longer
  a top allocation bucket — is met.
- **IV. Triage the 189 HC `ShapeNode` refs into frozen-read vs unfrozen-mutate** and point the hot
  frozen-read sites at int-index access (no materialization); leave mutate sites on handles. Highest-count
  files first: `HermitCrabExtensions`(24), `SynthesisAffixProcessAllomorphRuleSpec`(21), `Word`(18),
  `Morpher`(15), `RootAllomorphTrie`(13), `SynthesisMetathesisRuleSpec`(13), `CharacterDefinitionTable`(12).
- **V. Re-validate byte-identical** (full `SIL.Machine` + 63 HC + concurrent-determinism) and **measure on
  Sena + Indonesian** (`SenaQuick`). Go/no-go vs the clean checkpoint. Expected: node-phase 11.4% →
  near-0 for un-mutated clones; net `Word.Clone` cut toward the analysis-path floor + a 16-thread scaling
  lift (fewer Gen0 → less Workstation-GC contention).

## 6.5 III feasibility measurement + the chosen mechanism: copy-on-write Shape (2026-06-29)

Measured (temporary probe counting clones created vs structurally mutated):
- **Sena: 41% of shape clones are never structurally mutated** (59% mutated); **Indonesian: 16.5% never**
  (83.5% mutated). So the never-touched floor is a real, sizeable win, but the majority *are* mutated —
  a per-node lazy-handle scheme would still materialize most of them.

Two blockers rule out a naive "share the source nodes" COW: **(1)** `Freeze` rewrites each node's `Tag`,
and **(2)** feature rules edit annotation `FeatureStruct` in place — both would corrupt a shared frozen
source, and neither routes through `Shape`'s mutation methods.

**Chosen mechanism — whole-shape copy-on-write, gated on handle hand-out (not on flat-record laziness).**
A clone stores `_cowSource = source` and does **no** `CopyTo`. The key asymmetry that makes this safe and
cheap: the hot read path (the FST matcher) consumes the clone only through `IntAnnotations`/`IntRange`
(Stage 2), which can be **served from the frozen source's projection** (identical until mutation) with zero
handle access; while **every path that could mutate first hands out a `ShapeNode`/`Annotation` handle**
(`NodeAt`, enumeration, `Annotations`, `First`/`Last`, …) or calls a `Shape` mutator. So: gate `Inflate()`
(= the real `CopyTo`, then `_cowSource = null`) on every handle hand-out + every mutator + `Freeze`-with-
intent-to-write; serve `IntAnnotations`/`IntRange`/`Count`/`IsFrozen`/`GetFrozenHashCode` from the source
while COW; a `Freeze` of an un-inflated clone is a no-op (it already equals the frozen source). Never-
handle-touched clones (the matcher-only carriers) then cost a shell, not N objects — capturing the Sena 41%
without the Tag/FS-relocation rewrite. Correctness gate: the full suite must catch any un-gated hand-out
(stale/empty structure) — byte-identical or it fails loudly.

## 7. Rollback

Spike lives on `hc-rustify`. If V's go/no-go fails (allocation win < risk, or an un-fixable byte-identical
divergence), `git reset --hard dbef327a` (this design doc + the clean Stage-1/2 foundation survive in
history). Increments I and II are independently green and **keepable even if III–V are abandoned** (they
remove the per-annotation tower allocation with no laziness risk).
