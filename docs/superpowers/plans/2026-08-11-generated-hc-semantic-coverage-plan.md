# Generated HermitCrab Semantic Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [x]`) syntax for tracking.

**Goal:** Build a Machine-owned, fail-closed system that mechanically enumerates the HermitCrab XML,
loader, model, and audited execution surfaces; generates every atomic, interaction, and ordering
obligation; and proves that each obligation is tested, explicitly retired, or red as a C# defect.

**Architecture:** A deterministic inventory library extracts structural facts from the DTD and C#
source. One versioned YAML semantic catalog classifies those generated facts and declares phase-aware
effects, compatibility, carriers, and evidence requirements. The generator emits canonical JSON
manifests, obligation ledgers, and a Markdown report; the conformance CLI verifies checked-in outputs
and fails on unmapped, multiply mapped, stale, unproved, crashed, or missing-C# states.

**Tech Stack:** .NET 10, C#, NUnit, YamlDotNet, Microsoft.CodeAnalysis.CSharp 4.8.0,
NJsonSchema 10.9.0, System.Text.Json, the existing HermitCrab conformance runner, and the existing
XML fixtures.

## Status update, 2026-08-14: the combination-ledger design (Tasks 3-5, 7-10) is superseded

`docs/coverage-strategy.md` is now the governing statement for how this suite's coverage claim is
built, and it replaces this plan's central mechanism where they disagree. Read it first.

**What is superseded, and why.** Tasks 3 and 4 below build a `CoverageObligation` ledger over
mechanically generated compatible pairs, repeated pairs, and connected triples of catalog features
(`ObligationGenerator`, `DerivedProfileGenerator`), with Tasks 5, 8, and 9 generating discriminating
counterfactuals and fixtures against that same ledger, and Task 7's reports/CLI publishing it. This
whole chain is not merely undone — it is the wrong shape, for a reason this plan's own "Status: what
was built instead" section below already gestures at (no `CanonicalIdentity`/`ObligationGenerator`
exists) but did not yet have the counter-example for. `docs/coverage-levels.md`'s "Level 3 is not a
level, and pairs are not enough" section supplies it: an MPR `overwrite` group evicted by
unordered-stratum rule order needs four co-occurring ingredients, all four individually `Evidenced`,
and a pairwise (or even triple-wise) enumerator reports every pair inside that conjunction as covered
and still misses the defect. Arity is not the axis, and enumerating to arity 4 is not an option either
(264 observable surfaces give 34,716 pairs, ~3.03 million triples, ~198 million 4-tuples).

In its place, `docs/coverage-strategy.md` defines two mechanical layers above the unit layer that
Tasks 0-2A here already ship: **integration/edge** — does data cross a declared handoff at all, from
the DTD's own `IDREF`/`IDREFS` attributes, landed as `conformance/interface-inventory.tsv` (60
interfaces, 42 exercised, 18 not) — and **integration/chain** — does a written payload survive to its
reader, at the (structurally few, roughly 15) junctions where a type is both written and read, still
being built as `conformance/interaction-chains.tsv`. Neither is a pair/triple/schedule ledger over the
whole catalog; both are denominators sized from the DTD/engine's own structure, which is exactly this
plan's own stated goal, achieved by a different and much smaller mechanism than Tasks 3-10 specify.

**Task 6 is unaffected** — typed C# oracle outcomes and engine-owned convergence budgets are an
orthogonal correctness concern (`budget_ms`/`expect_crash` currently granting an easy pass), not a
coverage-denominator question, and remain open future work independent of this supersession.

**Task bodies below are kept as the record of what was designed and why**, per this project's own
rule that reasoning about a rejected approach outlives the approach. Do not resume Tasks 3, 4, 5, 7,
8, 9, or 10 as specified; do not cite this plan's obligation/pair/triple counts (or its "governing
complete-coverage plan" self-references) as the current coverage claim. For current numbers, read
`docs/coverage-strategy.md`, `docs/pangloss-handoff.md`, `conformance/interface-inventory.tsv`, and
`conformance/semantic-coverage-counterfactuals.tsv`.

---

## Scope, seams, and completion proof

The public seams under test are:

1. `SemanticCoverageInventory.Generate`: deterministic structural inventory from source inputs;
2. `SemanticCoverageAudit.Run`: fail-closed validation of catalog, obligations, and evidence;
3. `hc-conformance --semantic-coverage ...`: the user-facing generation/check command; and
4. checked-in `conformance/generated/*`: reproducible manifests and reports consumed by PanGloss.

DTD/source readers, graph normalization, and transformations are internal implementation details.
Generated JSON is compared through the aggregate inventory/audit seams to independent literal
expectations for small synthetic inputs; tests do not recompute expected IDs with production code.
During development every C# defect remains a visible red row. Final completion requires zero
unaccounted surfaces, zero stale outputs, zero silently dropped obligations, and zero blocked C#
defects after the C# oracle has been corrected.

The work remains on `docs/hc-semantic-catalog` in the isolated Machine worktree until the other
Machine/conformance work is available. Rebase is the final integration step; do not edit the shared
PanGloss `machine` checkout or its gitlink.

## File structure

- `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/InventoryModels.cs`:
  immutable structural inventory records and stable IDs.
- `.../SemanticCoverage/SemanticCoverageInventory.cs`: aggregate public generation seam.
- `.../SemanticCoverage/DtdInventoryReader.cs`: DTD tokenizer/parser and surface expansion.
- `.../SemanticCoverage/CSharpInventoryReader.cs`: Roslyn source census for XML accesses, model
  kinds, `SemanticBranch.Hit` sites, and source hashes.
- `.../SemanticCoverage/SemanticCatalog.cs`: YAML catalog model and strict loader.
- `.../SemanticCoverage/SemanticCoverageAudit.cs`: exact-once mapping and fail-closed diagnostics.
- `.../SemanticCoverage/ObligationGenerator.cs`: atomic, within-rule, pair, triple, and schedule
  candidate generation.
- `.../SemanticCoverage/DerivedProfileGenerator.cs`: finite normal-form algebras for composite XML
  shapes.
- `.../SemanticCoverage/CanonicalIdentity.cs`: RFC 8785-compatible canonical projection, graph
  relabeling, SHA-256 IDs, and diagnostic slugs.
- `.../SemanticCoverage/FixtureEvidenceReader.cs`: grammar/words evidence and branch-attribution
  extraction.
- `.../SemanticCoverage/CounterfactualGenerator.cs`: declaration permutations and neutralized or
  reversed variants.
- `.../SemanticCoverage/SemanticCoverageReport.cs`: deterministic JSON and Markdown output.
- `.../Program.cs`: add the semantic-coverage CLI mode only.
- `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/*Tests.cs`: public-seam tests.
- `tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj`:
  reference the conformance executable for testing.
- `conformance/semantic-catalog.yaml`: the only hand-maintained semantic classification.
- `conformance/schema/semantic-catalog.schema.json`: versioned catalog/decision contract.
- `conformance/schema/semantic-inventory.schema.json`: generated surface-manifest contract.
- `conformance/schema/semantic-obligations.schema.json`: generated obligation-ledger contract.
- `conformance/schema/semantic-evidence.schema.json`: fixture evidence/outcome contract.
- `conformance/generated/xml-surface.json`: generated DTD/source census.
- `conformance/generated/obligations.json`: generated required/retired/blocked ledger.
- `conformance/generated/semantic-coverage.md`: generated human audit report.
- `docs/hermit-crab-xml-semantic-coverage-catalog.md`: generated narrative view after migration.
- `.github/workflows/ci.yml` and `local_check.sh`: generated-output staleness and completeness
  gates.

### Task 0: Concurrent Machine integration preflight

**Files:**
- No repository edits

- [x] **Step 1: Record the observable integration state**

The isolated branch currently starts from Machine `73599a8` plus catalog commit `9ee06b4`.
`origin/conformance-framework` and the shared detached Machine checkout are also `73599a8`; the
other agent's unpublished integration tip is not currently observable as a Machine worktree or
branch. Record fresh SHAs and statuses before each slice.

- [x] **Step 2: Partition overlap before implementation**

Tasks 1–4 primarily add `SemanticCoverage/*` and `SemanticCoverage/*Tests.cs`; only the
conformance/test project files are shared seams. Defer edits to `Program.cs`, `Runner.cs`,
`WordsYaml*`, protocol docs, and fixtures until the other foundation tip is published or explicitly
confirmed disjoint. If it appears, compare file sets and rebase the clean branch before entering an
overlapping task. Retain the final clean rebase in Task 10.

- [x] **Step 3: Recheck before every overlapping task**

Run `git worktree list`, `git branch --all --contains`, and status/diff checks in both repositories.
Never infer ownership from the shared submodule gitlink.

### Task 1: Deterministic DTD surface inventory

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/InventoryModels.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/DtdInventoryReader.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticCoverageInventory.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/DtdInventoryReaderTests.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj`

- [x] **Step 1: Add the conformance project reference and a failing public-seam test**

The test supplies a literal DTD containing one element, an enum with a default, an
optional child, and an implied IDREF. It asserts exact stable IDs:

```csharp
string dtd = """
    <!ELEMENT Root (Child?)>
    <!ATTLIST Root mode (one | two) "one" target IDREF #IMPLIED>
    <!ELEMENT Child (#PCDATA)>
    """;
SemanticInventory inventory = SemanticCoverageInventory.Generate(
    SemanticCoverageSourceSet.FromDtd("fixture.dtd", dtd)
);
Assert.That(
    inventory.Surfaces.Select(s => s.Id),
    Is.EqualTo(
        new[]
        {
            "dtd:attribute-default/Root/mode/default/one",
            "dtd:attribute-default/Root/target/implied",
            "dtd:attribute-type/Root/mode/enumeration",
            "dtd:attribute-type/Root/target/IDREF",
            "dtd:attribute/Root/mode",
            "dtd:attribute/Root/target",
            "dtd:content/Child/r.pcdata@one",
            "dtd:content/Root/r.sequence@one",
            "dtd:default/Root/mode/one",
            "dtd:element/Child",
            "dtd:element/Root",
            "dtd:enum/Root/mode/one",
            "dtd:enum/Root/mode/two",
            "dtd:placement/Root/r.0/Child/optional",
        }
    )
);
```

- [x] **Step 2: Run the focused test and verify RED**

Run:
`dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --filter FullyQualifiedName~DtdInventoryReaderTests`

Expected: compilation fails because `SemanticCoverageInventory` does not exist.

- [x] **Step 3: Implement a real DTD tokenizer/parser**

Parse comments, multiline declarations, nested sequence/choice groups, occurrence suffixes, PCDATA,
ID/IDREF/IDREFS, required/implied/fixed/default values, and enum members. Emit element, attribute
declaration/type/presence, enum/default, structural content-model group, special-content, and
parent/child placement surfaces. Structural paths make group ancestry and child position
recoverable; group and leaf cardinality are addressable IDs. Every authored string in an ID uses
one deterministic UTF-8 percent-encoding routine. Reject duplicate declarations, duplicate
generated IDs, and unsupported syntax with source-span diagnostics; never silently skip a
declaration.

```csharp
public sealed record InventorySurface(
    string Id,
    string Kind,
    string Name,
    string? Parent,
    string Source,
    string? Value = null
);

public sealed record SemanticInventory(
    string Profile,
    string SourceHash,
    IReadOnlyList<InventorySurface> Surfaces
);

public sealed record CSharpInventoryInput(string RelativePath, string SourceText);

public sealed record SemanticCoverageSourceSet(
    string DtdPath,
    string DtdText,
    IReadOnlyList<CSharpInventoryInput> CSharpSources
)
{
    public static SemanticCoverageSourceSet FromDtd(string path, string text) =>
        new(path, text, Array.Empty<CSharpInventoryInput>());
}

public static class SemanticCoverageInventory
{
    public static SemanticInventory Generate(SemanticCoverageSourceSet sources);
}
```

- [x] **Step 4: Add boundary tests**

Cover nested alternation, repeated placements of the same child, sequence/choice topology,
whitespace and valid XML comments, malformed-comment rejection, strict XML DTD S separators at every grammar boundary, malformed declaration keywords,
DOCTYPE rejection including external SYSTEM/PUBLIC references, `#FIXED`, multiline ATTLISTs with
multiple attributes, adjacent-only occurrence suffixes, malformed unterminated declarations, equivalent nested-shape mutations,
group-cardinality mutations, XML 1.0 mixed-content shape (PCDATA first, choice-only, outer `*`,
unique unquantified element names), IDREF-versus-IDREFS/CDATAs,
required/implied/ordinary-default/fixed presence modes, exact enum-member defaults, punctuation
and percent-encoding, NOTATION rejection, exact authoritative per-kind counts, duplicate IDs,
and invalid public inputs. Assert the complete tiny manifest as literals. Mutation tests prove
that every structural, cardinality, type, presence, and authored-value change changes the
generated denominator or exact identity; Task 3 later proves that an unmapped generated change
turns `SemanticCoverageAudit` red until classified.

- [x] **Step 5: Run the focused tests and commit**

Expected: all `DtdInventoryReaderTests` pass.

Commit: `feat(conformance): enumerate HermitCrab DTD surfaces`

### Task 2A: Roslyn loader, model, and source-census foundation

**Files:**
- Create: src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CSharpInventoryReader.cs
- Create: src/SIL.Machine.Morphology.HermitCrab/SemanticBranch.cs
- Create: tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/CSharpInventoryReaderTests.cs
- Modify: src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj
- Modify: SemanticCoverage/InventoryModels.cs and SemanticCoverageInventory.cs only to compose DTD+C# inventories.

- [x] **Step 1: Add Roslyn 4.8.0 and write a failing source-census test**

The public seam takes immutable CSharpInventoryInput values. Each input may carry an exact,
sorted audited-scope list; scope names are fully qualified types or members, never wildcard
patterns. The literal source proves extraction of Element, Elements, and Attribute calls,
containing method symbols, enum members, source-local transitive rule implementations,
and SemanticBranch.Hit("...") calls. Expected IDs include
loader:Loader.Load/Attribute/isActive#0 and
branch:phon/rewrite/analysis/forced-simultaneous.

- [x] **Step 2: Run the focused test and verify RED**

The focused test was first written against the absent reader/capture API and failed to compile.

- [x] **Step 3: Implement the Roslyn syntax census and public runtime capture seam**

The reader uses Roslyn syntax nodes rather than regex, normalizes source paths/text and hashes
the ordered source set with SHA-256, and fails closed on malformed syntax, duplicate source paths,
duplicate generated IDs, duplicate markers, dynamic marker IDs, and unknown/non-exact audited
scopes. It emits literal and dynamic XML access surfaces, enum members, concrete/transitive
recognized rule implementations, literal marker surfaces, and every conditional/switch/catch/loop
decision inside exact audited scopes. Decision IDs contain the containing symbol, kind/ordinal, and
normalized-node fingerprint; line spans remain evidence only. SemanticCoverageInventory.Generate
preserves the DTD-only manifest and deterministically composes DTD+C# surfaces.

SemanticBranch.BeginCapture uses AsyncLocal, restores nested captures, and has a no-listener
path that does not allocate a capture object.

- [x] **Step 4: Run focused tests and record the green gate**

dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj
--no-restore --filter FullyQualifiedName~CSharpInventoryReaderTests passed 5/5, and the existing
DtdInventoryReaderTests remain the authoritative Task 1 gate. The backend marker insertion,
source mutations, and audit-red assertions are intentionally not part of Task 2A.

- [x] **Step 5: Census every conditional-compilation configuration**

Not in the original plan. Step 3 claimed every conditional/switch/catch/loop decision inside an
audited scope, but the reader parsed one configuration with no preprocessor symbols defined, so
`#if SINGLE_THREADED` and `#if OUTPUT_ANALYSES` regions were disabled trivia and never entered the
syntax tree. Real code was invisible: `Morpher.Synthesize(string, IEnumerable<Word>)` and every
decision in it are declared inside `#if SINGLE_THREADED`.

The reader now parses all four configurations over the two symbols and unions the results. IDs are
assigned **after** the union, because xml and decision IDs both carry an ordinal positional within a
(parent, method-or-kind) group: a configuration-only candidate at an earlier span shifts the ordinals
of everything after it, so per-configuration ID assignment would emit one decision under two IDs.
Union keys use span offsets, which are configuration-independent since every configuration parses
identical source text. Fingerprints hash `ToString()`, not `ToFullString()`, because a disabled `#if`
block is leading trivia of the next statement and would otherwise fingerprint one node differently
per configuration.

Every surface carries the sorted configuration names that contain it, so a later audit can tell a
surface that exists everywhere from one that exists only under a symbol. An audited scope resolves
if it names a symbol present in **at least one** configuration. The configuration set is folded into
the manifest hash, so widening it invalidates a stale census.

Not yet done from this slice: control-flow categories beyond the existing kinds (short-circuit
operators, null-coalescing/conditional access, `goto`/`return` transfers) and compiler/reference
profile fingerprinting.

### Task 2B: Runtime semantic marker insertion and audit-red proof

**Status:** open; depends on the other agent’s backend fixes and the Task 3 audit seam.

- [ ] **Step 1: Insert reviewed SemanticBranch.Hit markers**

Add markers at the existing analysis/synthesis rewrite mode split, rewrite subrule priority split,
boundary/filter split, metathesis feature-swap/node-move split, linear optional-unapplication path,
template/morphology two-root paths, compounding MPR phase split, and controlled nonconvergence paths.
Markers name semantic paths, never line numbers.

- [ ] **Step 2: Add mutation tests after SemanticCoverageAudit exists**

A new unmarked conditional/catch/private rule/schedule marker or concrete rule implementation must
increase the source census and make the audit red without catalog edits. Removing a runtime marker
while retaining its classified conditional must also be red. This is deliberately deferred until
Task 3 owns exact-once audit diagnostics; Task 2A only proves that the generated surfaces exist.

- [ ] **Step 3: Run the backend-focused gate and commit**

Run the marker/runtime tests together with the source and DTD census tests after coordinating
with the backend owner. The commit should remain isolated from unrelated backend fixes.

## The two-verdict standard (supersedes the evidence grading below)

Every grammar-observable surface must end in exactly one of two states. There is no third.

1. **`evidenced`** — full evidence, mechanically checked, INCLUDING a counterfactual. A mutated grammar
   with the surface neutralized produces a DIFFERENT result from the same words, and the delta is
   recorded. Or the mutation fails to load, which is `required-to-load` and equally conclusive.
2. **`proof:<kind>`** — an explicit claim that evidence is IMPOSSIBLE, with recomputed proof.

Nothing else passes. The earlier vocabulary is retired: `structural` ("the fixture parsed something")
and `presence` ("declared") are not evidence and are not accepted states, and prose waivers are not
proof. `trace` alone is not enough either: it shows the owning RULE ran, not that this declaration
mattered, so every trace claim must be upgraded with a counterfactual or reclassified.

### The mechanism

For surface `S` covered by fixture `F`, generate `F'` with `S` neutralized, typed per surface kind:

| Surface kind | Mutation |
|---|---|
| `dtd:element/X` | delete every `X` element |
| enum value, non-default | rewrite to a declared sibling value |
| enum value, equal to the DTD default | delete the attribute; the validating parser supplies it |

Run `F` and `F'` over `F`'s word list through the same engine and diff the full signature sets:

- outputs differ -> `evidenced`, with the delta (which word, which signature appeared or vanished)
- `F'` fails to load or fails DTD validation -> `required-to-load`, with the loader's own error
- outputs identical -> **fails the gate**, printing the fixture and words, unless a proof applies

A mutation that changes nothing observable is a bug in the mutator, not evidence, so the mutator needs
its own tests.

### Measured result

The sweep runs and the standard holds. 193 grammar-observable surfaces, one child process per surface so
a non-terminating mutant is killed rather than abandoned:

| Verdict | Count |
|---|---|
| Evidenced, a parse result changed | 80 |
| RequiredToLoad, the mutant would not load | 72 |
| Timeout | 0 |
| Unobservable | 41 |

**This was an early sweep, since superseded by a larger fixture set and a finer verdict split; it is
kept as the historical measurement that motivated the standard, not the current count.** The
`RequiredToLoad` bucket above was later split into `RequiredByDtd`/`RequiredByLoader` because it
overstated the evidence it represented (see `docs/coverage-levels.md`), and the grammar-observable
denominator has since grown from 193 to 264. Current numbers: `docs/pangloss-handoff.md`'s "Current
numbers" section and `conformance/semantic-coverage-counterfactuals.tsv` (106 `Evidenced` + 7
`EvidencedJointly` = 113 of 264, plus 65 `RequiredByDtd`, 13 `RequiredByLoader`, 3 `Unobservable`, 70
still unresolved).

152 of 193 carry full mechanical evidence with a recorded delta. Of the 41 remaining, ONE is provable
(`VariableFeature/name` is a symmetric alphabet) and 40 are fixture work, in four groups:

- 16 `isActive="no"` decoys that nothing references. Making a decoy unreferenced so it cannot dangle when
  dropped is exactly what makes activating it change nothing; a real control needs the decoy to compete
  with something live.
- 9 boolean or enumerated values with no observable effect in their host grammar (`blockable`, `partial`,
  `final`, `cyclicity`, `phonologicalRuleOrder`, `outputType`). Each needs a grammar where the value's
  effect is visible, which for `blockable` means a competing more-specific form to be blocked by.
- 6 elements declared but inert, including the compounding head/foot requirements, which the compounding
  fixture SATISFIES rather than violates.
- 7 ordering and adjacency values needing a word where direction or adjacency changes the outcome.

Cost and determinism, both load-bearing for a gate:

- 5m32s wall clock, 5814 mutant word parses, 340 unmutated.
- Zero words over 2s, mutated or not. The 20s timeout is a safety net that never fires; it was kept wide
  deliberately so a merely slow mutant is never recorded as evidence.
- Deterministic. `Morpher.Synthesize` sizes its `Parallel.ForEach` off `Environment.ProcessorCount`, so a
  pathological mutant threw from one worker nondeterministically and a verdict could flip between runs;
  the child process now runs with `DOTNET_PROCESSOR_COUNT=1`. Two consecutive checks agree.
- The sweep is not in the per-push path. It has its own workflow, on demand plus weekly, because a
  minutes-long gate on every push is one that gets switched off.

### The only admissible proofs, each recomputed and never trusted from a file

- **`proof:dtd-default`** — write-vs-omit produce byte-identical output. Proves unobservability rather
  than arguing it from `ValidationType.DTD`.
- **`proof:label-symmetry`** — permuting the label (alpha for beta throughout) produces identical
  output. Applies to the Greek `VariableFeature` names. It does NOT apply to `multipleApplication`,
  whose values are not interchangeable, so those quotients are invalid and return to the worklist.
- **`proof:no-consumer`** — dead schema, behavioural rather than a text scan: deleting the element from
  a grammar that contains it produces byte-identical output AND the engine has no reference to it.
- **`proof:blocked-by-defect`** — a linked, reproducing `expect_crash` fixture. Admissible only while
  the defect is open, and it must name both the fixture and the upstream issue.

### Expected fallout, stated up front

Applying this standard makes the reported number DROP before it climbs. The 94 `structural` and 13
`presence` entries mostly will not survive as covered, and 15 mislabelled `multipleApplication`
quotients return to the worklist. That is the cost of the number being true.

## Status: what was built instead of Tasks 3 through 10

Tasks 0, 1 and 2A are done as specified. Tasks 2B and 4 through 10 are NOT, and their checkboxes
below are unchecked accordingly. What exists instead is a narrower, working coverage ratchet, recorded
here so the plan is not silent about four commits of substitute machinery:

- `SemanticCatalog.cs` / `SemanticCoverageAudit.cs` plus a GENERATED `conformance/semantic-catalog.yaml`
  (`CatalogBootstrap.cs`, `--write-semantic-catalog`). All 1059 DTD surfaces are mapped exactly once,
  so a new surface fails closed. Grammar-observable surfaces are grouped per DTD element and left
  `unclassified` rather than given fabricated phase effects. The catalog therefore satisfies exact-once
  mapping but does NOT yet carry the plan's `ProfileAlgebras` or `ObligationDecisions`, and no feature
  has been promoted to `semantic`.
- `GrammarCoverageGate.cs` / `GrammarFeatureUsage.cs` / `DeadSchemaDetector.cs` / `TraceEvidence.cs`
  plus `conformance/semantic-coverage-baseline.txt` and `semantic-coverage-presence-waivers.txt`: a
  two-way ratchet over the 264 grammar-observable surfaces, graded by evidence strength, with
  dead-schema and alphabet-quotient classifications recomputed rather than trusted.
- `conformance/edge-cases/loader-isactive-breadth/`: one new fixture, oracle-verified.
- CI now runs the conformance fixtures at all, which it previously did not.

What is deliberately absent, and what that costs:

- No `CanonicalIdentity`, `ObligationGenerator`, or `DerivedProfileGenerator`. There is no combination
  ledger, so no pair, triple, or schedule obligation exists and the denominator is surfaces, not
  obligations.
- No `CounterfactualGenerator` or `CarrierTransformations`. Nothing generates a grammar variant and
  re-runs it, which is why evidence grading tops out at "a rule that owns this construct fired" and
  cannot show that a construct is load-bearing. This is the single largest gap: the plan's evidence
  model is a baseline/counterfactual delta per obligation, and none is computed.
- No `CSharpOracleOutcome` or typed outcomes. `budget_ms` and `expect_crash` still grant a pass, which
  contradicts this document's own final checklist item 7.
- No `GrammarRecipe` / `WitnessSearch`, no `conformance/schema/*.json`, no `conformance/generated/*`,
  and no rebase onto the integration tip.
- The ordering carriers this plan was built around (`Stratum@phonologicalRules`,
  `@morphologicalRules`, `@activeTemplates`) are IDREFS attributes, so they fall outside the 264
  observable surfaces entirely. No fixture can currently be demanded for ordering.

### Task 3: Single curated catalog and exact-once audit — SUPERSEDED (see status update above)

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticCatalog.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticCoverageAudit.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/SemanticCoverageAuditTests.cs`
- Create: `conformance/semantic-catalog.yaml`
- Create: `conformance/schema/semantic-catalog.schema.json`
- Create: `conformance/schema/semantic-inventory.schema.json`
- Create: `conformance/schema/semantic-obligations.schema.json`
- Create: `conformance/schema/semantic-evidence.schema.json`

- [ ] **Step 1: Write failing exact-once mapping tests**

Tests must independently assert that an unmapped surface, duplicate exact mappings, an unknown feature
ID, any wildcard/pattern mapping syntax, an unclassified conditional inside an audited source scope,
a rule-interface implementation
outside every audited scope, a semantic feature without all three phase effects, and a retirement
without a reason/citation, an unused decision, and a generated obligation without a decision each
fail with deterministic diagnostic codes. A minimal fully mapped catalog must pass.

- [ ] **Step 2: Run the focused tests and verify RED**

- [ ] **Step 3: Implement strict YAML loading and auditing**

The profile is `sil.machine.hc-semantic-catalog/v1`. Deserialize YAML to a JSON-compatible tree and
validate it against the versioned JSON Schema before binding the strict C# model. Validate every
generated manifest against its output schema in tests and before writing. Each feature declares disposition,
`analysisCandidateEffect`, `synthesisConfirmationEffect`, `finalParseEffect`, read/write state
domains, legal carriers, profiles, and evidence policy. Every `surfaceMappings` row names one exact
generated surface ID; wildcard, glob, regex, and prefix selectors are invalid. Every generated surface
must have exactly one row, and every row must name a current surface. Unknown YAML keys are errors.

```csharp
public sealed record PhaseEffect(
    string Behavior,
    IReadOnlyList<string> Reads,
    IReadOnlyList<string> Writes
);

public sealed record SemanticFeature(
    string Id,
    string Disposition,
    PhaseEffect AnalysisCandidateEffect,
    PhaseEffect SynthesisConfirmationEffect,
    PhaseEffect FinalParseEffect,
    IReadOnlyList<string> Carriers
);

public sealed record SemanticCatalog(
    string Profile,
    IReadOnlyList<SemanticFeature> Features,
    IReadOnlyList<SurfaceMapping> SurfaceMappings,
    IReadOnlyList<string> AuditedSourceScopes,
    IReadOnlyList<ProfileAlgebraDefinition> ProfileAlgebras,
    IReadOnlyList<ObligationDecision> ObligationDecisions
);

public sealed record SurfaceMapping(string SurfaceId, string FeatureId);
public sealed record ProfileAlgebraDefinition(
    string Id,
    string Algebra,
    IReadOnlyList<string> TriggerSurfaceIds,
    IReadOnlyDictionary<string, string> Parameters
);
public enum ObligationStatus
{
    Required,
    RetiredInvalid,
    RetiredOrthogonal,
    RetiredDuplicate,
    BlockedCSharpDefect,
}
public sealed record CombinationEdge(int Left, string Relation, int Right, string Carrier);
public sealed record ObligationDecision(
    string CandidateKey,
    ObligationStatus Status,
    IReadOnlyList<CombinationEdge> Interactions,
    string? Reason,
    IReadOnlyList<string> Citations
);
public sealed record AuditDiagnostic(string Code, string SubjectId, string Message);
public sealed record AuditResult(bool IsComplete, IReadOnlyList<AuditDiagnostic> Diagnostics);

public static class SemanticCoverageAudit
{
    public static AuditResult Run(SemanticInventory inventory, SemanticCatalog catalog);
}
```

- [ ] **Step 4: Bootstrap and classify the complete current DTD/loader surface**

Generate a proposed patch containing exact-ID entries, then review and copy accepted entries into the
catalog as semantic, loader, metadata, ignored,
ignored-reference, partial-load, invalid, nonconvergent, or blocked-C# classifications. Preserve the
known ignored and asymmetric rows from the design document. Regeneration never updates the canonical
catalog, so a new surface remains red until its exact row is human-reviewed.

- [ ] **Step 5: Define decision ownership and precedence**

The generator first canonicalizes occurrences plus mechanically derived carrier/schedule topology
into a graph-free `candidateKey`, then reads decisions. Every non-duplicate candidate must have
exactly one candidate-key decision in `semantic-catalog.yaml`. The decision supplies interaction
relations but cannot rewrite the generated occurrence or schedule topology. After applying the
decision, the generator computes the authoritative graph-bearing obligation ID. There are no
last-match-wins rules and no decision wildcard. Mechanical canonicalization alone emits
`retired-duplicate`; a human decision cannot declare a duplicate. Required decisions name
interaction relations and carrier-specific schedule topology. Invalid/orthogonal/blocked decisions
require a reason and source/DTD/catalog citation. A decision for no generated candidate is stale and
red.

- [ ] **Step 6: Run focused and repository-catalog tests and commit**

Commit: `feat(conformance): fail closed on unmapped HC surfaces`

### Task 4: Canonical identities and obligation generation — SUPERSEDED (see status update above)

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CanonicalIdentity.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/ObligationGenerator.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/DerivedProfileGenerator.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/CanonicalIdentityTests.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ObligationGeneratorTests.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/DerivedProfileGeneratorTests.cs`

- [ ] **Step 1: Write canonicalization vectors before implementation**

Use literal expected SHA-256 values computed independently for unique labels, `AAB`, `AAA`,
jointly permuted schedule and interaction edges, symmetric edges, and mixed partial orders. Assert
that fixture IDs and authored occurrence indices do not affect the digest.

- [ ] **Step 2: Verify the vectors fail**

- [ ] **Step 3: Implement canonical graph normalization**

Project the semantic object, enumerate label-preserving node permutations, jointly remap and sort
edges, use transitive reduction for DAG schedules, serialize with RFC 8785-compatible property and
number rules, and hash UTF-8 bytes with SHA-256. Slugs are non-authoritative and gain twelve digest
digits on collision.

```csharp
public sealed record CanonicalIdentityValue(string Digest, string Slug, string CanonicalJson);

internal static class CanonicalIdentity
{
    public static string CreateCandidateKey(
        IReadOnlyList<CombinationOccurrence> occurrences,
        IReadOnlyList<CombinationEdge> schedule,
        string carrierTopology
    );
    public static CanonicalIdentityValue CreateObligationId(
        CombinationCandidate candidate,
        IReadOnlyList<CombinationEdge> interactions
    );
}
```

- [ ] **Step 4: Write failing obligation-ledger tests**

From a three-feature literal inventory/catalog, assert the complete atomic set, compatible ordered
pairs, unordered multisets with repetition, all co-schedulable triples before connectedness is
classified, mixed partial orders, and explicit
`retired-invalid`, `retired-orthogonal`, `retired-duplicate`, and
`blocked-csharp-defect` rows. Assert that no generated candidate disappears.

- [ ] **Step 5: Implement and test finite composite profile algebras**

Provide sealed algorithms for cardinality relation (empty/equal/narrow/widen), output-action topology
(insert/copy/modify/omit, relative prefix/infix/suffix/circumfix position, reorder, repeated copy),
environment topology (left/right/bilateral, required/excluded/conflicting), representation relation
(equal/prefix/normalization/cross-table), schedule topology, and occurrence multiplicity. Algebra
alphabets come from DTD choices/model types; algorithms enumerate finite normal forms for unbounded
sequences under an explicit quotient. Every repeatable/choice DTD placement and every audited
derived-shape conditional must be named by exactly one profile-algebra trigger or exact retirement;
unused algebra declarations are red.

Mutation tests add a new output action, cardinality case, environment carrier, table relation, and
derived conditional. Each must either produce a new profile normal form or turn the audit red; it
cannot vanish because the catalog omitted a friendly profile name.

```csharp
internal static class DerivedProfileGenerator
{
    public static IReadOnlyList<DerivedProfile> Generate(
        SemanticInventory inventory,
        SemanticCatalog catalog
    );
}

public sealed record DerivedProfile(
    string Id,
    string AlgebraId,
    IReadOnlyList<string> TriggerSurfaceIds,
    IReadOnlyDictionary<string, string> NormalForm
);
```

- [ ] **Step 6: Implement obligation generation and commit**

Derive the profile universe from DTD enum/default/placement surfaces, concrete model/rule types,
loader dispatch, and marked execution/schedule branches. Generate every atomic profile and every
co-schedulable pair/triple with repetition from mechanically derived carrier placement; curated
read/write domains may propose interaction edges but may not prune a candidate. A triple without a
declared connected graph still exists and requires an explicit orthogonal/disconnected retirement.
Atomic, within-rule, cross-rule, and schedule ledgers remain distinct but share canonical IDs.

```csharp
public sealed record CombinationOccurrence(string FeatureId, string ProfileId);
public sealed record CombinationCandidate(
    string CandidateKey,
    IReadOnlyList<CombinationOccurrence> Occurrences,
    IReadOnlyList<CombinationEdge> Schedule,
    string CarrierTopology
);
public sealed record CoverageObligation(
    string Id,
    string Kind,
    ObligationStatus Status,
    CombinationCandidate Candidate,
    IReadOnlyList<CombinationEdge> Interactions,
    string? Reason
);

internal static class ObligationGenerator
{
    public static IReadOnlyList<CoverageObligation> Generate(
        SemanticInventory inventory,
        SemanticCatalog catalog
    );
}
```

Commit: `feat(conformance): generate canonical HC coverage obligations`

- [ ] **Step 7: Produce the early denominator census**

Run the generator against the repository before fixture population. Check in a review artifact
listing raw counts and IDs by atomic profile, carrier, pair, repeated pair, triple, repeated triple,
and schedule topology. Mutation tests add one DTD carrier, enum profile, concrete rule type, and
schedule branch and prove each increases the denominator and turns the audit red. This census sizes
Task 9; it is not semantic completion.

Commit: `docs(conformance): record generated HC denominator census`

### Task 5: Fixture evidence and discriminating counterfactuals — SUPERSEDED (see status update above)

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/FixtureEvidenceReader.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CounterfactualGenerator.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CarrierTransformations.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/FixtureEvidenceTests.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/WordsYaml.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/WordsYamlLoader.cs`

- [ ] **Step 1: Define and test obligation evidence in `words.yaml`**

Add versioned `obligations:` references and counterfactual evidence without changing the structured
analysis identity. Tests reject unknown obligation IDs, a trace-only participation claim, a required
interaction lacking a counterfactual, a connected triple without a typed comparison targeting each
declared edge, a variant whose role does not match its transformation, and unordered evidence missing
a declaration permutation.

- [ ] **Step 2: Verify RED**

- [ ] **Step 3: Implement evidence loading and fixture XML indexing**

Presence of an XML construct is evidence context, never semantic credit. Positive evidence requires a
complete C# result; interaction credit requires the declared original/counterfactual result delta;
ordering credit requires reversal or all required declaration permutations.

```csharp
public sealed record ObligationEvidence(
    string ObligationId,
    string FixtureId,
    string Word,
    IReadOnlyList<EvidenceComparison> Comparisons
);

public enum GrammarVariantRole
{
    Baseline,
    NeutralizedOccurrence,
    ReversedSchedule,
    DeclarationPermutation,
    MixedPartialOrder,
}

public enum EvidencePredicate
{
    AnalysisSetDiffers,
    AnalysisSetEquals,
    ContainsOrderContribution,
    TypedOutcomeDiffers,
}

public sealed record EvidenceComparison(
    string BaselineVariantId,
    string ComparedVariantId,
    EvidencePredicate Predicate,
    IReadOnlyList<int> TargetOccurrenceIds,
    IReadOnlyList<string> TargetEdgeIds
);

public sealed record XmlDelta(
    string Path,
    string Operation,
    string? Before,
    string? After
);

public sealed record GrammarVariant(
    string Id,
    GrammarVariantRole Role,
    IReadOnlyList<int> TargetOccurrenceIds,
    IReadOnlyList<string> TargetEdgeIds,
    string GrammarXml,
    IReadOnlyList<XmlDelta> Delta
);

internal static class FixtureEvidenceReader
{
    public static IReadOnlyList<ObligationEvidence> Read(
        string fixturesRoot,
        IReadOnlyDictionary<string, CoverageObligation> obligations
    );
}
```

- [ ] **Step 4: Implement deterministic XML transformations**

Implement separate adapters for `Stratum@phonologicalRules`,
`Stratum@activeTemplates/morphologicalRules`, template-slot IDREF lists, inline phonological
subrules, morphological allomorph alternatives, declaration order, and occurrence neutralization.
Each transformation returns an exact XML delta manifest naming changed element/attribute paths.
Tests assert the intended execution carrier changed and no unrelated semantic field changed; merely
reordering declarations when an IDREF list controls execution is rejected. Cover alternatives,
`AAA`, `AAB`, and mixed partial orders. Validate every generated variant with the DTD and C#
loader before execution.

```csharp
internal static class CounterfactualGenerator
{
    public static IReadOnlyList<GrammarVariant> Generate(
        CoverageObligation obligation,
        XDocument grammar
    );
}

internal interface ICarrierTransformation
{
    string Carrier { get; }
    GrammarVariant Apply(CoverageObligation obligation, XDocument grammar);
}
```

- [ ] **Step 5: Run focused tests and commit**

Commit: `feat(conformance): require discriminating semantic evidence`

### Task 6: C# typed outcomes and logical-budget reporting

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CSharpOracleOutcome.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab/HermitCrabExecutionContext.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/Morpher.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/AnalysisStratumRule.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/SynthesisStratumRule.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/PhonologicalRules/AnalysisRewriteRule.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/PhonologicalRules/SynthesisRewriteRule.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SelfCheckEngine.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/Runner.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/WordsYaml.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/WordsYamlLoader.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/Fixture.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/FixtureMaterializer.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/FixtureManifest.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/MaterializedRunner.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/AdapterEngine.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SignatureTsv.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/Program.cs`
- Modify: `conformance/README.md`
- Modify: `conformance/PROTOCOL.md`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/CSharpOracleOutcomeTests.cs`

- [ ] **Step 1: Write failing typed-outcome tests**

Pin `complete`, `invalidInput`, `invalidGrammar`, `partialLoad`, `nonconvergent`, and
`harnessFailure`. Unknown exceptions and process exits remain failures. Valid zero-analysis is
distinct from invalid input. A loader error handler that continues is `partialLoad`, not complete.

```csharp
public enum CSharpOracleStatus
{
    Complete,
    InvalidInput,
    InvalidGrammar,
    PartialLoad,
    Nonconvergent,
    HarnessFailure,
}

public sealed record CSharpOracleOutcome(
    CSharpOracleStatus Status,
    IReadOnlyList<string> Analyses,
    IReadOnlyList<OracleDiagnostic> Diagnostics,
    IReadOnlySet<string> SemanticBranches
);

public sealed record OracleDiagnostic(string Code, string Message);
public sealed record OracleExecutionLimits(
    int MaximumNodes,
    int MaximumArcs,
    int MaximumApplications,
    int MaximumTraversals
);
```

- [ ] **Step 2: Verify RED**

- [ ] **Step 3: Implement the adapter without time semantics**

Translate only known C# outcomes. Engine-managed node/arc/application/traversal budgets may cause a
typed `nonconvergent`; wall-clock values remain diagnostics and never enter the contract. Remove
semantic acceptance of uncontrolled `expect_crash`; existing crash fixtures become red defects.

```csharp
public CSharpOracleOutcome RunSemanticCase(
    string grammarPath,
    string word,
    OracleExecutionLimits limits
);
```

- [ ] **Step 4: Migrate the complete harness and protocol**

Remove `budget_ms` and `expect_crash` from the semantic fixture schema, loader, manifests,
materialization, runners, adapter comparison, CLI reporting, README, and protocol. Extend the single
adapter result format with typed statuses while preserving structured analysis identity. An
operational process-kill timeout may protect CI, but it always becomes red `harnessFailure`; it is
never fixture data or semantic credit. Add migration tests proving that old crash/time fields fail
validation rather than remaining silently active.

- [ ] **Step 5: Add engine-owned deterministic counters**

`Morpher` creates a per-call `HermitCrabExecutionContext`; stratum cascades charge rule
applications and produced candidates, rewrite/metathesis loops charge pattern traversals and shape
nodes, and compounding/template recursion charges arcs/applications. Limits are engine configuration,
not `words.yaml`, adapter protocol, or expected results. Tests set tiny limits to deterministically
exercise each counter and assert `nonconvergent`; no test asserts elapsed time.

- [ ] **Step 6: Fix C# defects exposed by required obligations**

For each blocked obligation, first add the fixture/evidence test, then fix the C# loader or engine:
allomorph-environment alpha variables, grouped/quantified simultaneous RHS handling, compounding
variable binding, typed invalid input, and controlled nonconvergence. Do not encode a crash as oracle
truth.

- [ ] **Step 7: Run focused tests and commit each defect separately**

Commits use `fix(hermitcrab): ...`; the final outcome-model commit is
`feat(conformance): report typed C# oracle outcomes`.

### Task 7: Deterministic reports, CLI, and staleness gate — SUPERSEDED (see status update above)

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticCoverageReport.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/SemanticCoverageReportTests.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/Program.cs`
- Modify: `.github/workflows/ci.yml`
- Modify: `local_check.sh`
- Create: `conformance/generated/xml-surface.json`
- Create: `conformance/generated/obligations.json`
- Create: `conformance/generated/semantic-coverage.md`

- [ ] **Step 1: Write failing report and CLI tests**

Pin deterministic ordering, LF newlines, source/catalog/profile hashes, counts by disposition, red
defects, zero-unaccounted output, and exit codes: 0 only for a complete current audit, 1 for semantic
reds, and 2 for malformed invocation/input. `--check` must never rewrite files.

```csharp
public sealed record SemanticCoveragePaths(
    string DtdPath,
    string SourceRoot,
    string FixturesRoot,
    string CatalogPath,
    string OutputDirectory
);

public static class SemanticCoverageCommand
{
    public static int Run(string action, SemanticCoveragePaths paths, TextWriter output);
}
```

- [ ] **Step 2: Verify RED**

- [ ] **Step 3: Implement `--semantic-coverage generate|check`**

Inputs are explicit DTD, source-root, fixtures-root, and catalog paths with repository defaults.
`generate` atomically writes outputs; `check` regenerates in memory and byte-compares. Both always
run C# evidence validation; absence of C# is an error.

- [ ] **Step 4: Generate and review the repository outputs**

The report prints exact numerator/denominator counts for DTD surfaces, loader accesses, model kinds,
marked branches, atomic leaves, pair/triple/schedule obligations, evidence, retirements, blocked C#
defects, stale results, and unaccounted items.

- [ ] **Step 5: Generate the narrative catalog and add CI checks**

Render `docs/hermit-crab-xml-semantic-coverage-catalog.md` from the catalog plus immutable prose
sections stored in the catalog. Add `--semantic-coverage check` to `local_check.sh` and
`.github/workflows/ci.yml`; CI checks and never rewrites generated files.

- [ ] **Step 6: Run focused tests and commit**

Commit: `feat(conformance): add generated semantic coverage audit`

### Task 8: Grammar recipes and bounded witness discovery — SUPERSEDED (see status update above)

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/GrammarRecipe.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/WitnessSearch.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/GrammarRecipeTests.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/WitnessSearchTests.cs`
- Add: `conformance/recipes/*.yaml`

- [ ] **Step 1: Write failing composition and minimal-witness tests**

A tiny recipe composes two compatible rules, emits DTD-valid XML, enumerates bounded candidate words,
runs C# `ParseWord`, compares the neutralized/reordered controls, and chooses the shortest ordinal
discriminating witness. An unsatisfied bound returns a red `witness-not-found`, never a retirement.

- [ ] **Step 2: Verify RED**

- [ ] **Step 3: Implement composable recipes**

Recipes reference catalog feature/profile IDs and typed carriers. Shared feature systems, character
tables, lexicon entries, IDs, and strata are generated deterministically. Loader/default tests use
direct XML mutations rather than a writer that could erase the syntax under test.

```csharp
public sealed record GrammarRecipe(
    string Id,
    IReadOnlyList<string> FeatureProfiles,
    IReadOnlyList<string> CarrierProfiles
);

internal static class GrammarRecipeComposer
{
    public static XDocument Compose(GrammarRecipe recipe, SemanticCatalog catalog);
}
```

- [ ] **Step 4: Implement bounded discovery**

Seed from declared roots and C# generation only as discovery; validate every selected witness through
the full C# parse pipeline. Bounds are deterministic word length/candidate/application counts owned by
the search engine, not semantic time limits.

```csharp
public sealed record WitnessSearchLimits(
    int MaximumWordLength,
    int MaximumCandidates,
    int MaximumApplications
);

public sealed record WitnessSearchResult(
    string Status,
    string? Word,
    IReadOnlyDictionary<string, CSharpOracleOutcome> VariantOutcomes
);

internal static class WitnessSearch
{
    public static WitnessSearchResult Find(
        CoverageObligation obligation,
        IReadOnlyList<GrammarVariant> variants,
        WitnessSearchLimits limits
    );
}
```

- [ ] **Step 5: Run focused tests and commit**

Commit: `feat(conformance): generate HC fixture recipes and witnesses`

### Task 9: Populate every obligation and make failures red — SUPERSEDED (see status update above)

**Files:**
- Modify: `conformance/semantic-catalog.yaml`
- Modify/Add: `conformance/recipes/*.yaml`
- Modify/Add: `conformance/languages/*/grammar.xml`
- Modify/Add: `conformance/languages/*/words.yaml`
- Modify/Add: `conformance/edge-cases/*/grammar.xml`
- Modify/Add: `conformance/edge-cases/*/words.yaml`
- Regenerate: `conformance/generated/*`

- [ ] **Step 1: Run the full audit and capture every red row**

Group failures by atomic surface, within-rule profile, cross-rule pair, connected triple, scheduling
topology, loader outcome, and C# defect. Checked-in results are reusable only when their grammar,
words, catalog profile, and C# source hashes remain current.

Freeze the early census into deterministic population shards: document/loader, feature systems and
representation, lexicon/allomorphy, ordinary/realizational morphology, compounding, templates and
strata, rewrite, metathesis, constraints, cross-family pairs, repeated pairs, connected triples, and
schedule topologies. Each shard has exact obligation IDs and counts from the generator; an obligation
may appear in exactly one population shard. CI reports shard and global numerators/denominators.

- [ ] **Step 2: Close atomic and loader gaps**

Add discriminating evidence for every feature leaf and typed loader outcome. No `expect_skip`,
empty analysis, or merely DTD-valid grammar receives positive semantic credit.

- [ ] **Step 3: Close all compatible pair and connected-triple gaps**

Use generated recipes where possible and reviewed XML otherwise. Repetition (`AA`, `AAA`, `AAB`)
and every legal profile/carrier combination remain in the denominator.

- [ ] **Step 4: Close ordering and unordered gaps**

Supply reversal evidence for load-bearing order and all declaration permutations for unordered
unions. Retire only structurally impossible or proven orthogonal candidates with cited reasons.

- [ ] **Step 5: Fix every remaining C# red and regenerate**

The gate remains red until C# behaves correctly; other engines never define expected behavior.

- [ ] **Step 6: Run the complete Machine verification and commit**

Run the focused SemanticCoverage tests, all HermitCrab tests, C# conformance self-check, semantic
coverage `check`, and existing conformance parity check. Expected: zero failures, zero unaccounted,
zero stale outputs, and zero blocked defects.

Commit: `test(conformance): complete generated HC semantic coverage`

### Task 10: Rebase and PanGloss consumption — SUPERSEDED (see status update above)

**Superseded, not merely reordered.** PanGloss consumption happened by a different route than this
task describes: `docs/superpowers/plans/2026-08-13-hc-semantic-products-implementation-plan.md`
shipped a manifest-based handoff (`conformance/generated/hc-conformance-manifest.v1.json`) with no
catalog/obligation ledger involved, and `docs/pangloss-handoff.md` records what was actually handed
over. The steps below describe rebasing a branch and a ledger that no longer exist as specified; kept
as the record of the originally planned integration path.

**Files:**
- Machine: only conflict resolutions required by the other conformance branch
- PanGloss: a separate consumer change after the Machine commit is integrated

- [ ] **Step 1: Fetch and identify the other Machine integration tip**

Do not infer it from the shared detached checkout. Record both SHAs and compare overlapping files.

- [ ] **Step 2: Rebase the clean Machine branch as its final branch operation**

Resolve conformance conflicts in favor of the combined contract, rerun generated outputs, and keep
Machine as the sole owner of catalog IDs, grammars, words, and C# results.

- [ ] **Step 3: Run the full Machine gate on the rebased tip**

- [ ] **Step 4: Update PanGloss in a separate isolated change**

PanGloss imports Machine’s generated catalog/ledger and maps backend claims to those IDs. It does not
copy, fork, or regenerate the semantic denominator.

- [ ] **Step 5: Run PanGloss backend matrix and merge review**

Every claimed backend runs every applicable obligation. Unsupported is an explicit capability refusal;
deviation, missing C#, missing result, crash, or stale evidence is red.

Commit Machine and PanGloss changes separately so either repository can identify its ownership
boundary and revert independently.

## Final verification checklist

**Superseded as a completion gate for this plan** along with Tasks 3-5 and 7-10: the checklist below
was written against the obligation-ledger design and cannot be satisfied by it (no `CoverageObligation`
exists to satisfy items 4, 5, 9, or 10). `docs/coverage-strategy.md`'s own layers — unit surfaces
(113/264, `conformance/semantic-coverage-counterfactuals.tsv`), integration/edge
(`conformance/interface-inventory.tsv`), integration/chain (in progress), and the capped hand-crafted
set — are the current completion criteria. Items 1, 2, 3, 6, 7, and 8 remain meaningful statements
about the unit layer and are already satisfied there; kept below unedited as the record of the
original, broader completion bar.

- [ ] Generated DTD counts match the source declaration census and every surface maps exactly once.
- [ ] Every loader access, model rule kind, enum member, and marked execution branch maps exactly once.
- [ ] Every feature has analysis-candidate, synthesis-confirmation, and final-parse effects.
- [ ] Every candidate obligation has a required, retired, duplicate, orthogonal, or blocked status.
- [ ] Every required interaction and ordering obligation has discriminating C# evidence.
- [ ] C# is always present and is the only expected-result oracle.
- [ ] No uncontrolled crash, wall-clock timeout, empty-analysis ambiguity, or partial load is green.
- [ ] Generated files are byte-current with source/catalog/profile hashes.
- [ ] Machine owns the catalog and evidence; PanGloss only consumes and maps backend support.
- [ ] A fresh independent review finds no silent denominator escape hatch.
