> **Archived 2026-09-03.** Section 4 (PR #491) is superseded by `pangloss-final-template-prune-handoff.md`;
> the branch it pins was archived unmerged (`hermitcrab-perf-2026-09-disposition.md`), and its edge-prefilter item
> measured only 2–13% wall standalone. Kept as the record of the mapping from the Machine round to PanGloss.

# Temporary implementation handoff: Machine HC speedups applicable to PanGloss HC-Rust

**Temporary working document — 2026-09-02.** Delete or move this file after the PanGloss work is
planned. This document does not assert that PanGloss has been modified.

## Source pins and scope

- Machine optimization branch: `perf/hc-optimization-pr` at
  `179708d2f185637ecab7ac985e02d57bfa73b3f1`.
- PanGloss code inspected at `b22ad5c146dbed71f1de6139d3feabd4c1262837`.
- Related upstream work: [sillsdev/machine PR #491, “Filter final templates in analysis”](https://github.com/sillsdev/machine/pull/491).
- Machine evidence:
  - `docs/hermitcrab-perf-2026-09-counts.md`
  - `docs/optimizations/*.md`
  - `docs/rejected-optimizations/final-template-prune.md`

The goal is to transfer only mechanisms whose invariant survives the C# → Rust representation change.
Do not mechanically port CLR allocation work that PanGloss already eliminates by construction.

## Executive implementation decision

| Priority | Mechanism | PanGloss disposition | Why |
| --- | --- | --- | --- |
| 1 | Analysis affix edge-segment prefilter | **Implement and measure first** | Algorithmic, representation-independent, and the strongest deterministic result: 87% fewer FST traversals and 54% fewer arc checks on 45 Sena words. |
| 2a | Interned grammar-FS ID equality | **Implement as a tiny direct fix** | `constraints_equal` resolves two IDs from the same hash-consing interner and deep-compares their trees; equal `FsId`s are already the exact equality proof. |
| 2b | One-pass optional shape construction | **Implement after a call/count census** | `freeze_out` builds a shape, then deep-copies it through `ShapeBuilder::from_shape` solely to set optional segment flags. Emit those flags in the first pass instead. |
| 3 | Immutable `Shape` sharing | **Implement if a PanGloss clone census confirms the expected traffic** | `Word`, `WordKey`, and `AnalysisStateKey` currently clone owned `Shape` columns. PanGloss shapes are already immutable and builders already provide the mutation boundary, so sharing is natural and low-risk. |
| 4 | Immutable runtime syntactic-FS sharing | **Census independently; implement as a separate change if justified** | Runtime `FeatureStruct` is an immutable tree but is owned/cloned into words and memo keys. Sharing removes repeated tree clones; Machine measured the analogous clone/freeze/hash path at about 15% of a heavy profile. |
| 5 | PR #491 final-template interleaving prune | **Instrument first; implement only if it fires on a target grammar** | Same optimization as Machine's rejected experiment. Its grammar-wide partiality guard caused zero prunes on Sena, Amharic, and Mbugwe. It can be very large on a suitable non-partial/template-heavy grammar. |

Everything else from the Machine branch is already inherent in PanGloss, already implemented, or tied to a
C#-specific representation. See “Do not re-port these” below.

## Non-negotiable verification protocol

Use this protocol for each optimization independently; do not land a package whose individual effects cannot
be reconstructed.

1. Pin one baseline commit and one candidate commit. Build both in `--release` mode.
2. Run sequentially (`--threads 1`) for deterministic work counts. Do not benchmark two arms concurrently.
3. Use one warm-up followed by alternating baseline/candidate process rounds. Compare per-word minima; retain
   raw output.
4. Compare canonical analysis signature **sets** per word, not only analysis counts. Errors, caps, timeouts,
   missing word keys, and duplicate-signature changes are mismatches.
5. Run focused unit tests, the complete workspace test suite, the PanGloss conformance fixtures, and a
   C#-oracle corpus parity comparison.
6. Prefer mechanism counts over small wall-time deltas. Wall time is supporting evidence; a deterministic
   reduction in the intended work is the deciding evidence.
7. Run the correctness suite with the optimization disabled if an internal A/B switch is retained during
   development. Remove research-only switches and counters after the decision unless they are useful permanent
   diagnostics.
8. Check both memo modes where relevant (`--memo=on` and `--memo=off`) and both sequential and parallel batch
   execution. Within-word analysis is single-threaded today, but word batches are not.

Useful existing surfaces:

```powershell
cd rust
cargo test --workspace --release
cargo clippy --workspace --all-targets -- -D warnings
cargo build --release -p pg-cli

# Existing batch/parity surface; use the real grammar and word-list paths for the corpus.
target\release\pangloss.exe batch <grammar> <words.txt> <out.tsv> `
  --threads 1 --memo=on --stats --word-timeout-ms <bound>

# Existing FST diagnostic. Capture stderr separately; it is cumulative per worker thread.
$env:HC_FST_PROFILE='1'
target\release\pangloss.exe batch <grammar> <words.txt> <out.tsv> --threads 1
Remove-Item Env:HC_FST_PROFILE
```

The existing `pg_fst::profile::snapshot()` exposes traversal-run and nondeterministic traversal counts, but not
arc checks. Add temporary deterministic counters for prefilter candidates/rejects and arc checks if needed.
Keep timing outside the inner edge-check helper.

---

## 1. Analysis affix edge-segment prefilter

### Safety invariant

PanGloss builds each analysis affix LHS from the allomorph RHS and runs it with:

- left and right anchoring (`Transduce::anchored(true, true)`);
- segment-only analysis input (`segs_of(..., include_boundaries = false)`);
- lane unification (`pg_featstruct::flat_unifiable`);
- nondeterministic/all-matches traversal.

If the first top-level `AnalysisLhs.nodes` item is a bare `CompileNode::Constraint`, the first non-skipped input
segment must unify with it. If the last top-level item is a bare constraint, the last non-skipped segment must
unify with it. `CompileNode::Constraint` emits one mandatory input arc and has no epsilon alternative.

The proof does **not** allow looking through `Group`, `Alternation`, or `Quantifier`, even when a particular
instance seems mandatory. Start with the syntactically obvious bare-constraint case only.

An optional input edge disables the check on that side. The FST may skip it and match the constraint against a
later segment. If the input contains no segments and a corresponding mandatory edge constraint exists, reject:
the anchored FST cannot consume that constraint.

Use the complete compiled lane vector, including PanGloss's optional `StrRep` identity lane. Do not compare
only `char_def`, and do not use equality where the matcher uses unifiability.

### Exact landing points

- `rust/crates/pg-rules/src/morph.rs:1941` — extend `AnalysisLhs` with cached edge constraints.
- `rust/crates/pg-rules/src/morph.rs:2002` — after building `lhs.nodes`, derive the first/last bare constraints.
- `rust/crates/pg-rules/src/morph.rs:2214` — `build_ana_affix_lhs` already compiles once for the cached path.
- `rust/crates/pg-rules/src/cache.rs:147` — `AllomorphCache.ana_lhs` already retains `AnalysisLhs`; no separate
  lookup table is required.
- `rust/crates/pg-rules/src/morph.rs:2306` — run the check in `ana_allomorph_matches` before
  `Transduce::new(fst, segs.to_vec())`.

Because `ana_allomorph_matches` is shared by ordinary and realizational analysis, placing the check there will
cover both. That extension is logically safe under the same pattern construction, but Machine measured ordinary
analysis affix rules only; report the two categories separately if PanGloss statistics can distinguish them.
At the pinned commit, both direct callers—and their traced/cached entry paths—ultimately receive `segs` produced
by `segs_of(..., false)`, so boundaries are excluded. Keep the helper private to that contract; any future caller
using boundary-inclusive input must establish the same invariant explicitly.

### Suggested representation

```rust
#[derive(Clone, Debug, Default)]
struct EdgeConstraints {
    left: Option<Vec<u64>>,
    right: Option<Vec<u64>>,
}

pub(crate) struct AnalysisLhs {
    nodes: Vec<CompileNode>,
    captured: HashMap<String, usize>,
    modify: HashMap<String, (usize, SimpleContext)>,
    edges: EdgeConstraints,
}
```

Derive after the complete LHS has been constructed so `Modify` pinning and the identity lane are already in the
constraint:

```rust
fn edge_constraints(nodes: &[CompileNode]) -> EdgeConstraints {
    let left = match nodes.first() {
        Some(CompileNode::Constraint(lanes)) => Some(lanes.clone()),
        _ => None,
    };
    let right = match nodes.last() {
        Some(CompileNode::Constraint(lanes)) => Some(lanes.clone()),
        _ => None,
    };
    EdgeConstraints { left, right }
}
```

Runtime predicate:

```rust
fn edge_constraints_allow(edges: &EdgeConstraints, segs: &[Segment]) -> bool {
    if let Some(required) = &edges.left {
        let Some(actual) = segs.first() else { return false };
        if !actual.optional && !pg_featstruct::flat_unifiable(&actual.lanes, required) {
            return false;
        }
    }
    if let Some(required) = &edges.right {
        let Some(actual) = segs.last() else { return false };
        if !actual.optional && !pg_featstruct::flat_unifiable(&actual.lanes, required) {
            return false;
        }
    }
    true
}
```

Before copying this literally, confirm `flat_unifiable` remains symmetric. It currently performs the same
lane-intersection test in either argument order.

### Trace, stats, and accounting

- A prefilter rejection is the same externally visible failure as an FST pattern mismatch. Existing traced
  wrappers should emit `FailureReason::Pattern`; verify that the early return still passes through that wrapper.
- Count `edge_prefilter_candidates` once per allomorph with at least one safe edge constraint.
- Count `edge_prefilter_rejects` once per allomorph rejected before traversal.
- Evaluate both enabled sides in tests, but increment `edge_prefilter_rejects` only once when both sides mismatch.
- Preserve the existing rule/allomorph “attempt” count. The optimization removes traversal work, not attempts.
- Compare `pg_fst::profile` run calls with the prefilter off/on. Expected identity:
  `run_calls_off - run_calls_on == prefilter_rejects`, modulo other traversal users if the profile is aggregated.
- If adding arc instrumentation, count every examined FST input arc, not only successful unifications.

### Focused tests

Add tests beside the analysis-affix tests or in a dedicated `analysis_edge_prefilter` module:

1. Left constraint mismatch rejects without entering `Transduce`.
2. Right constraint mismatch rejects without entering `Transduce`.
3. Matching left/right edges produce exactly the same analyses as the disabled path.
4. A leading optional segment disables only the left check; likewise trailing optional/right.
5. Empty input plus a mandatory edge constraint rejects safely.
6. Top-level `Group`, `Quantifier`, and `Alternation` yield `None`, even if their first child is a constraint.
7. Analysis ignores boundary nodes exactly as `segs_of(..., false)` does.
8. Identity-lane disagreement rejects even when phonological lanes unify.
9. Wide character tables, where the identity lane is absent, retain the existing lane-unification semantics.
10. Cached and uncached analysis paths return identical signature sets.
11. Traced and untraced paths return identical analyses and the traced rejection reports `Pattern`.

### Acceptance gate

- Zero signature differences across all conformance fixtures and the reference corpora.
- Nonzero candidate and rejection counts on at least one target corpus.
- FST run count decreases by the number of prefilter rejections.
- No increase in rule attempts or analysis-cascade steps.
- Keep the production change even if wall time is noisy when deterministic traversal work drops materially.

Machine reference result, not a promised PanGloss result:

| 45 Sena words | off | on | change |
| --- | ---: | ---: | ---: |
| FST traversals | 12,376,416 | 1,617,662 | **−87%** |
| FST arc checks | 34,170,777 | 15,583,242 | **−54%** |
| traversal instances | 15,642,175 | 2,429,643 | **−84%** |
| pattern attempts | 11,979,916 | 11,979,916 | unchanged |

Afterward, Machine's remaining traversals were about 44% successful; do not assume a second-segment prefilter
has useful headroom without new counts.

---

## 1a. Two small direct Rust wins found while mapping the Machine work

These are not literal ports of the corresponding C# implementations, but they follow the same proven rules:
use an existing immutable identity instead of repeating structural work, and populate an output during the pass
that already has all source information.

### Compare interned grammar feature structures by `FsId`

At `rust/crates/pg-rules/src/morph.rs:1300-1305`, `constraints_equal` ends with:

```rust
g.fs_interner.get(a.required_syn_fs) == g.fs_interner.get(b.required_syn_fs)
```

Both IDs belong to the same grammar interner. `Interner::intern` guarantees structurally equal values receive
the same stable ID, so replace the deep comparison with:

```rust
a.required_syn_fs == b.required_syn_fs
```

Add a unit test with two allomorphs whose required FSs were independently authored but intern to the same ID,
plus a genuinely different pair. This is the exact Rust-native descendant of “frozen-tree equality,” though its
likely aggregate impact is small. Search for any other `interner.get(id1) == interner.get(id2)` sites; the audit
at the pinned commit found only this one.

### Build optional output segments in one pass

At `rust/crates/pg-rules/src/morph.rs:813-861`, `freeze_out` currently:

1. builds and freezes the complete shape;
2. collects optional segment positions;
3. if any exist, deep-copies every shape column with `ShapeBuilder::from_shape`;
4. deletes and reinserts each optional segment solely to attach `NodeFlags::OPTIONAL`;
5. freezes again.

Add builder entry points that accept flags during the original append, for both concrete and
`NO_CHAR_DEF + CdSet` segments. Prefer one general internal primitive with thin public helpers, for example:

```rust
pub fn push_segment_with_lanes_and_flags(
    &mut self,
    char_def: u32,
    lanes: &[u64],
    flags: NodeFlags,
)

pub fn push_segment_with_lanes_set_and_flags(
    &mut self,
    lanes: &[u64],
    cd_set: CdSet,
    flags: NodeFlags,
)
```

Then `freeze_out` chooses `NodeFlags::OPTIONAL` from `OutNode.optional` and performs one builder pass. Remove the
`optional_positions`, `ShapeBuilder::from_shape`, delete/reinsert loop, and second freeze.

Required tests:

1. Mixed mandatory/optional concrete segments preserve order, lanes, `char_def`, and flags.
2. Optional `NO_CHAR_DEF` segments preserve their exact `CdSet`.
3. Boundaries remain optional under the existing boundary helper.
4. Empty and no-optional outputs take one pass and remain byte/structurally equal to baseline.
5. Analysis signature parity covers deletion reinsertion and untruncation, the two paths most likely to mint
   optional segments.

Before assigning performance value, count `freeze_out` calls, outputs containing optional segments, total nodes
recopied in the second pass, and bytes/lanes recopied. This is a PanGloss-native analogue of Machine's
single-pass `Shape.CopyTo` work, not a port of its annotation-node map.

---

## 2. Make immutable `Shape::clone` cheap

### Current PanGloss condition

`pg_shape::Shape` is immutable after construction, but `#[derive(Clone)]` copies six owned columns:

- `kinds: Box<[NodeKind]>`
- `char_defs: Box<[u32]>`
- `flags: Box<[NodeFlags]>`
- `feat_lanes: Box<[u64]>`
- `cd_sets: Box<[CdSet]>`
- scalar `feat_width`

Those clones occur transitively in:

- `Word`'s derived `Clone`;
- `Word::dedup_key` (`word.rs:423-436`);
- `AnalysisStateKey` construction (`stratum.rs:554-563`, `pg-memo/src/lib.rs`);
- memo storage/replay and dedup maps;
- shape-equivalent merge bookkeeping (`stratum.rs:1043-1065`).

The existing mutation design is already copy-on-write in the useful sense: callers construct a
`ShapeBuilder::from_shape`, mutate private `Vec` columns, and freeze a new immutable shape. No caller mutates a
`Shape` in place. Therefore the safest design is to make `Shape` itself a cheap shared-value handle rather than
adding ownership flags to `Word`.

### Recommended representation

In `rust/crates/pg-shape/src/lib.rs`, preserve the public `Shape` API and move the columns into one shared body:

```rust
use std::sync::Arc;

#[derive(PartialEq, Eq, Hash, Debug, Default)]
struct ShapeData {
    kinds: Box<[NodeKind]>,
    char_defs: Box<[u32]>,
    flags: Box<[NodeFlags]>,
    feat_width: u32,
    feat_lanes: Box<[u64]>,
    cd_sets: Box<[CdSet]>,
}

#[derive(Clone, Debug, Default)]
pub struct Shape {
    inner: Arc<ShapeData>,
}
```

Use `Arc`, not `Rc`, at this layer so `Shape` remains `Send + Sync` when grammar/cache data crosses batch
workers. One `Arc<ShapeData>` is preferable to one `Arc` per column: `Shape::clone` should perform one refcount
increment.

Implement semantic traits explicitly:

```rust
impl PartialEq for Shape {
    fn eq(&self, other: &Self) -> bool {
        Arc::ptr_eq(&self.inner, &other.inner) || self.inner == other.inner
    }
}
impl Eq for Shape {}
impl Hash for Shape {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.inner.hash(state) // content hash, never pointer hash
    }
}
```

All accessors delegate to `inner`. `ShapeBuilder::into_shape` constructs exactly one `Arc<ShapeData>`.
`ShapeBuilder::from_shape` remains the only deep-copy-on-mutation boundary and copies from `src.inner` into
scratch `Vec`s as it does today. `ShapeInterner` may continue interning `Shape`; equality/hash remain structural.

Do **not** use pointer identity as the only equality or hash. Two independently built but structurally equal
shapes must still deduplicate and intern to the same identity.

### Required inventory before editing

Run and classify every direct construction and every `.shape.clone()`:

```powershell
rg -n "Shape \{|Shape::default|\.shape\.clone\(\)|shape: .*clone\(\)" rust/crates -g '*.rs'
```

The API-preserving inner-`Arc` design should require changes primarily inside `pg-shape`. Any caller changes are
a warning that representation details have leaked.

### Focused tests

1. `Shape::clone` shares its body (`Arc::ptr_eq` exposed through a test-only helper or test in the module).
2. Independently built equal shapes compare equal and hash equally despite different allocation identity.
3. `ShapeBuilder::from_shape` mutation leaves the source unchanged.
4. Interning two independently built equal shapes still returns one `ShapeId`.
5. Every column—including `CdSet`, optional flags, identity, and feature lanes—participates in equality/hash.
6. A compile-time assertion or test confirms `Shape: Send + Sync`.
7. `WordKey`, `AnalysisStateKey`, memo replay, alternative expansion, and shape-merge tests remain green.

### Measurements and acceptance

Add temporary clone counters if profiling cannot attribute the effect:

- logical `Shape::clone` calls;
- deep shape bodies allocated/frozen;
- total nodes and lane words copied by `ShapeBuilder::from_shape`;
- memo key constructions;
- peak working set on known-heavy words.

The logical clone count should remain stable while copied bytes and body allocations fall. Actual mutation
builders must remain unchanged. Machine's analogous change moved `Word` clone from 36% to 9% of its heavy-word
profile and peak working set from 11 GB to 6 GB; these are motivation, not expected Rust ratios.

---

## 3. Share immutable runtime `FeatureStruct` values

### Current PanGloss condition

PanGloss's syntactic/realizational `FeatureStruct` is already a frozen tree:

- sorted unique `(FeatId, FeatureValue)` entries;
- no authored reentrancy, aliases, cycles, or alpha variables;
- all operations (`unify`, `add`, `priority_union`, etc.) are nondestructive and return a new value;
- grammar-tier feature structures are interned as `FsId`, but runtime `Word.syn_fs`, `Word.real_fs`, `WordKey`,
  and `AnalysisStateKey` own and clone full values.

This means the C# frozen-tree clone/equality and visited-set changes should **not** be ported. Their useful Rust
descendant is simply cheap immutable sharing.

### Recommended representation

Keep the `FeatureStruct` public type and operations intact, but store nonempty canonical entries behind one
`Arc`. Preserve `FeatureStruct::EMPTY` as a real `const` by giving empty and nonempty values distinct canonical
representations:

```rust
use std::sync::Arc;

#[derive(Clone, PartialEq, Eq, Hash, Debug)]
enum Entries {
    Empty,
    NonEmpty(Arc<[(FeatId, FeatureValue)]>),
}

#[derive(Clone, PartialEq, Eq, Hash, Debug)]
pub struct FeatureStruct {
    entries: Entries,
}

impl FeatureStruct {
    pub const EMPTY: FeatureStruct = FeatureStruct { entries: Entries::Empty };
}
```

`FeatureStructBuilder::build` must return `FeatureStruct::EMPTY` when its vector is empty; otherwise convert the
vector into `Arc<[...]>`. This single-canonical-empty invariant is required if traits remain derived.
`entries()` returns `&[]` or the shared slice. `FeatureValue::Complex(FeatureStruct)` then becomes recursively
cheap to clone automatically.

As with Shape, use content equality/hash, optionally with `Arc::ptr_eq` as an equality fast path. Never use an
address-derived hash. **Do not derive `Serialize` on this representation:** that would leak the private enum and
change the JSON schema. Preserve today's logical `{ "entries": [...] }` wire format with a manual serializer:

```rust
impl serde::Serialize for FeatureStruct {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        use serde::ser::SerializeStruct;
        let mut state = serializer.serialize_struct("FeatureStruct", 1)?;
        state.serialize_field("entries", self.entries())?;
        state.end()
    }
}
```

Retain the existing manual `Deserialize` path through `WireFeatureStruct` and `FeatureStructBuilder`; it both
accepts the old schema and re-establishes sorted/unique/canonical-empty invariants. Audit every direct constructor
so `NonEmpty` can never contain an empty slice. With manual serde over the logical slice, the serde `rc` feature is
not required.

An alternative is `Arc<FeatureStruct>` only in `Word` and key fields. That touches `pg-rules`, `pg-memo`, and
many operation signatures, while leaving nested complex values expensive to clone. Prefer the deep-module
representation above unless a prototype shows a compatibility problem.

### Exact consumers that should become cheap without API changes

- `rust/crates/pg-rules/src/word.rs:169-181` — derived `Word::clone`.
- `rust/crates/pg-rules/src/word.rs:290-302` — `WordKey`.
- `rust/crates/pg-rules/src/word.rs:423-436` — dedup-key construction.
- `rust/crates/pg-rules/src/stratum.rs:554-563` — analysis-key construction.
- `rust/crates/pg-memo/src/lib.rs` — `AnalysisStateKey` owned fields.
- `rust/crates/pg-rules/src/morph.rs:1252-1270` — analysis FS gate; it already returns a clone only when the
  result is logically unchanged, which becomes cheap.
- Lexical/root seed assignments in `pg-parse/src/morpher.rs` and `pg-rules/src/morph.rs`.

### Focused tests

1. Nonempty `FeatureStruct::clone` shares storage; nested complex children remain shared.
2. Independently built equal structures compare/hash equally despite different allocation identity.
3. Builder output remains sorted and duplicate-feature replacement remains last-write-wins.
4. Empty builder output is exactly the canonical empty representation.
5. JSON serialization/deserialization is byte/schema compatible with the current logical `{ entries: ... }`
   representation.
6. Every operation returns the same value as before for empty, disjoint, overlapping, and nested structures.
7. `FeatureStruct: Send + Sync` remains true.
8. Memo keys distinguish every field they distinguish today; equal keys from different rule orders hash equally.

### Measurements and acceptance

Count logical FS clones separately from newly allocated FS bodies/entry arrays. The desired result is stable
logical behavior with far fewer allocations. Profile key construction and hashing separately: sharing removes
deep clone allocation, but content hashing may still walk the tree. If hashing remains hot after sharing, the
next experiment is a cached structural hash or per-parse runtime interning—not a reason to abandon sharing.

Do not fold runtime interning into the first change. `pg-shape::ShapeInterner` exists and grammar FS interning
exists, but `pg-memo` explicitly documents runtime owned values as a deferred design. Converting the whole
analysis state to `ShapeId`/`FsId` is a larger arena/lifetime change and should have its own evidence and parity
gate.

---

## 4. PR #491 final-template prune: conditional follow-up

### Relationship and evidence

PR #491 and Machine's removed experiment implement the same semantic search-space prune: during analysis, do
not cross a final template after a non-template rule when synthesis would reject that ordering. PR #491 is more
complete because it carries an explicit state through words, compounding, equality, and memoization.

The proof is valid only when partiality cannot legalize the ordering. PR #491 uses a conservative grammar-wide
test: any partial morpheme disables normal enforcement. Machine measured:

- Sena: 25 partial lexical entries and six partial rules → zero prunes.
- Amharic: at least one partial rule → zero prunes.
- Mbugwe: at least one partial rule → zero prunes.

Therefore do not add permanent state before establishing applicability in PanGloss.

### Phase A: opportunity census without changing semantics

At final-template entry in `AnalysisDriver::analyze_template` (`stratum.rs:875`), count a candidate when the
latest analysis trail item is a non-template rule. PanGloss already carries `Word.mrule_apps`, and
`AffixProcessRuleDef.is_template_rule` is populated during grammar load/compile. Compounding is non-template by
definition. Separately compute whether the grammar contains any partial lexical entry or partial morphological
rule, matching #491's conservative guard.

Compute that guard once when constructing the immutable grammar or analysis cache, exactly as:

```rust
let grammar_has_partial_morphemes = g.entries.iter().any(|e| e.partial)
    || g.mrules.iter().any(|r| {
        matches!(r, MorphRuleDef::AffixProcess(a) if a.partial)
    });
```

`Compounding` and `Realizational` have no `partial` field at the pinned commit and must not be guessed from
other properties. Pass the cached boolean into the driver; do not rescan the grammar at template entry.

Record at least:

- final-template-after-non-template candidates;
- candidates permitted by the no-partials guard;
- candidates suppressed only by grammar-wide partiality;
- grammar/stratum/template identity;
- descendant cascade steps and FST runs beneath candidate branches, if a research build can attribute them.

Run the census on all target corpora. Continue only if guarded candidates are nonzero and materially expensive.

### Phase B: state-machine implementation, only if Phase A justifies it

Add:

```rust
#[derive(Copy, Clone, Debug, Default, PartialEq, Eq, Hash)]
enum FinalTemplateState {
    #[default]
    None,
    NonTemplate,
    FinalTemplateAfterNonTemplate,
}
```

Carry it in `WordFlags` as a new `analysis_final_template_state` field. Do not replace or overload the existing
`is_last_applied_rule_final: Option<bool>`: that field already participates in PanGloss's synthesis-side template
gates and `WordKey`, whereas this new state records the analysis interleaving history needed by #491.
Required transitions, matching the current PR:

1. Entering a final analysis template with state `NonTemplate`, while the partiality guard permits enforcement:
   mark the template walk `FinalTemplateAfterNonTemplate`.
2. Before applying an analysis affix-process rule, reject input in
   `FinalTemplateAfterNonTemplate` with `FailureReason::NonPartialRuleProhibitedAfterFinalTemplate` (or the
   PanGloss equivalent). This must occur before the step/timing accounting if a rejected rule is defined as
   unattempted; be consistent with existing `apply_one_mrule` gates.
3. After successful analysis affix unapplication, set `NonTemplate` for a non-template rule and `None` for a
   template rule.
4. After successful analysis compounding unapplication, set `NonTemplate`.
5. Clear to `None` on stratum output so clitics/later-stratum behavior remains unchanged.

PanGloss's `MorphRuleDef::AffixProcess` already exposes `is_template_rule`; do not infer membership repeatedly
by scanning templates in the hot loop.

### Memo/dedup correctness

This state changes which successor rules are legal. It **must** be included in:

- `pg_memo::AnalysisStateKey` fields, constructor, `Eq`, and `Hash`;
- `AnalysisDriver::state_key`;
- `WordKey` and `Word::dedup_key`;
- clone/replay paths.

Failing to include it in `AnalysisStateKey` makes positive and nogood memo replays unsound. Including it can
split previously equal memo states and lower hit rate; measure that cost alongside prunes.

At the pinned commit, cascade/self-loop, slot/template-batch, guided-synthesis, and stratum-output dedup all flow
through `WordKey`. The morphological and template memo tables plus both in-progress guards flow through
`AnalysisStateKey`. Derived `Word::clone` preserves `WordFlags`, but `Word::replay_onto` deliberately reconstructs
history and must be audited explicitly; add a positive-memo replay test in which two otherwise-equal arrivals have
different final-template states and prove they are neither conflated nor replayed across the gate. The analysis
stratum's shape-only merge happens at exit; clear the state before that merge, as the current synthesis path clears
`is_last_applied_rule_final` before output dedup.

Avoid an eager clone for every template merely to carry this enum. Either:

- clone only in the target `final + NonTemplate + enforce` case (Shape/FS sharing makes that clone cheap), or
- pass the state through the template recursion and stamp produced words before slot rule application if that
  can be done without duplicating rule logic.

The first option is simpler and affects only branches the optimization targets.

### `AlwaysEnforceFinalTemplates` is a semantic feature

PR #491 also offers an override that enforces final templates even when partial morphemes exist. If PanGloss
wants this, expose and test it separately from the performance optimization. It can change accepted
analyses/generations for partial data, so performance comparisons with it enabled are not semantics-preserving
comparisons against the default engine.

### Required tests

1. Non-partial grammar: final template after non-template is rejected.
2. Non-final template remains allowed.
3. Partial lexical root/rule preserves the old path under default policy.
4. Optional-slot and empty-template behavior matches the C# oracle.
5. Compounding transitions to `NonTemplate` and participates in the prune.
6. Stratum exit clears the state and permits the intended later-stratum/clitic behavior.
7. Memo on/off yields identical signatures; keys differing only in final-template state are unequal.
8. Traced/untraced outputs agree and the failure reason is stable.
9. The override's result differences are explicitly asserted as policy behavior.
10. Mechanism counters prove nonzero prunes on the motivating real grammar.

### Better follow-on than the global guard

The global “any partial morpheme anywhere” guard is sound but overbroad. A worthwhile research direction is a
static per-stratum/reachability proof or a dynamic per-state proof that no relevant partial root/rule can occur
below the current analysis state. Do not simply use `Word.flags.is_partial` before lexical lookup: analysis does
not know the root yet, which is the reason #491 needs the conservative guard.

---

## Do not re-port these Machine changes

| Machine optimization | PanGloss status |
| --- | --- |
| Deferred per-template clone | **Already inherent.** `analyze_template` passes the borrowed input into `template_unapply_slots`; only the terminal output clones via `or_insert_with(|| in_word.clone())`. Preserve this when adding #491 state. |
| Frozen flat-tree equality | **Already inherent.** PanGloss FSs are canonical immutable trees with derived structural equality; there is no cyclic visited-pair walk to bypass. |
| Frozen flat-tree clone-map removal | **Already inherent.** PanGloss FSs cannot contain aliases/cycles requiring an old→new clone map. |
| Feature-structure visited-state pooling | **Not applicable.** There are no cycle/reentrancy sets in the Rust tree operations. |
| Flat FST register arrays | **Already implemented.** `FstResult.registers` is a flat `Vec<Register>`; live traversal instances use `Rc<Vec<Register>>` plus `Rc::make_mut`-style lazy copying. |
| `Shape.CopyTo` single-pass annotation map | **Literal mechanism not applicable.** PanGloss has no C# node-reference/annotation remapping dictionary. Its analogous avoid-a-second-pass opportunity is `freeze_out`'s optional-segment rebuild, specified above. |
| C# `Word.CloneForEngine` ownership flags | **Do not reproduce literally.** PanGloss values are immutable and rebuilt through builders; put sharing inside the value representation instead. |

Also do not revive the rejected Machine experiments without new PanGloss evidence:

- two-pass nondeterministic traversal (Machine used 2.3× more traversal instances for only 1.6% fewer arc
  checks and 27% more clones);
- `ExpandAlternatives` memo/hoist (no clone reduction);
- compounding split prefilter (only about 2% of clones and 9% of traversals were in scope on the measured word).

## Suggested commit sequence

Keep each step independently reviewable and benchmarkable:

1. `test(stats): add temporary HC optimization counters`
2. `perf(rules): prefilter analysis allomorphs by edge segment`
3. `test(rules): cover analysis edge prefilter invariants`
4. `perf(rules): compare interned grammar feature structs by id`
5. `perf(shape): emit optional output flags in one pass`
6. `perf(shape): share immutable shape storage`
7. `test(shape): pin structural equality and COW mutation`
8. `perf(featstruct): share immutable runtime trees`
9. `test(featstruct): pin sharing, serde, and operation parity`
10. `docs(perf): record per-change counts and corpus parity`
11. Remove temporary counters/toggles after decisions.
12. Only after a positive census: a separate PR/series for the final-template state machine.

Do not combine edge pruning, `Shape` sharing, or `FeatureStruct` sharing in one performance commit. Edge pruning
reduces the number of clones and keys reached, while each sharing change reduces a different unit cost; make each
an independent go/no-go decision and measurement.

## Definition of done

The transfer is complete when:

- each retained optimization has a written invariant and a focused regression test;
- complete Rust workspace tests and conformance gates pass in release mode;
- corpus signatures match the pinned baseline and C# oracle;
- deterministic counters prove the intended work disappeared;
- single-thread and multi-thread batch output are identical;
- memo on/off parity is clean;
- raw benchmark inputs, outputs, commit pins, command lines, and interpretation are recorded;
- no CLR-specific pools, mutable ownership flags, or research-only production switches were added without a
  Rust-side need.
