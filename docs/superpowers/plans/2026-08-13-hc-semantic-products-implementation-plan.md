# HC Semantic Products Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a compiler-faithful HermitCrab semantic inventory and a normalized XML-backed conformance corpus that PanGloss can import from labeled, versioned, schema-validated JSON products.

**Architecture:** A .NET 10 conformance tool queries the pinned SDK for the exact compiler inputs of sixteen fixed project/profile nodes, builds zero-error Roslyn compilations connected by owned `CompilationReference` edges, audits every execution boundary, and writes two canonical consumer products. Machine and HermitCrab remain `netstandard2.0`; PanGloss consumes only checked-in JSON, schemas, and the referenced authoritative XML grammars.

**Tech Stack:** .NET SDK 10.0.303, its byte-matched C#/.NET 10 Roslyn toolchain, `dotnet msbuild` native JSON queries, YamlDotNet, XML DTD validation, JsonSchema.Net 9.4.0 with Draft 2020-12, NUnit, canonical UTF-8 JSON and SHA-256.

**Governing spec:** `docs/superpowers/specs/2026-08-13-hc-semantic-compilation-graph-design.md`

---

## Status

**Tasks 1 through 6 are implemented and locked by tests, including the Task 5 Step 4 cutover.**
Both production census sites — the `--semantic-coverage` authority and `--propose-semantic-catalog`
— call `GraphSemanticCensus`, which captures compiler inputs through MSBuild, builds the sixteen
`RoslynCompilationGraph` nodes, and censuses those compilations directly. Sources, preprocessor
symbols, parse options, compilation options and references are all the pinned compiler's own, and
the inventory's toolchain fingerprint is the graph hash. Neither fallback named in the File and
Module Structure section is reachable from a production path any more.

**Tasks 7 and 8 are cut** (see their headings below for the full reasoning) rather than carried
forward: the census splits into 1059 `dtd:` surfaces — the grammar language itself, derived from the
DTD alone, already shipping via `DtdInventoryReader` — and 592 `decision:`/`loader:`/`model:`/
`source:` surfaces belonging to the C# engine's internals. Task 7 would have required 233
hand-written boundary dispositions plus permanent upkeep to cover the second group, and the
completeness claim it would have served was unreachable regardless, because `SIL.Machine` — where
the pattern matcher and feature-unification engine live — is never censused
(`GraphSemanticCensus.CensusedProjects = { "hc", "hc-tool" }`). The decision: the grammar format and
its interpretation are what matter, and the HermitCrab implementation is the "golden" engine but
nothing about it is special except that it covers the whole grammar. `LiveExecutionRootsHaveNoUnresolvedExecutionEdges`,
the test that gated Task 7's boundary classification, went away with the dead
`RepositorySourceSnapshot` path it exercised (see below) rather than being fixed; nothing now gates
regressions in C# execution-closure coverage, which is accepted because that census is not what this
handoff is about.

**Task 9 shipped and then was superseded.** It originally landed as the corpus product
(`conformance/generated/hc-conformance-corpus.v1.json`, 28 fixtures and 414 cases, plus two
published Draft 2020-12 schemas), derived from the fixtures alone rather than from the census or
Task 7. Commit 93cf1e5b replaced it: `words.yaml` is the single canonical authored format, and the
409KB corpus was a second representation of ground truth it already held — larger while carrying
less, since the authored files hold 1011 comment lines of reasoning no generated JSON can express,
and two representations meant a permanent drift gate. It and `conformance/schema/conformance-corpus.schema.json`
are deleted. What the corpus genuinely contributed was provenance, not case data, so that is what
survives as the 13KB `conformance/generated/hc-conformance-manifest.v1.json`
(`hc-conformance-manifest/v1`, `conformance/schema/conformance-manifest.schema.json`): one entry per
fixture carrying `fixtureId`, `category`, `displayLanguage`, `grammarPath`, `grammarSha256`,
`wordsPath`, `wordsSha256`, `caseCount`, `expectedCrash`, plus top-level `dtdPath`, `dtdSha256`,
`sourceHash`, and the three format-version identifiers. The CLI flags are renamed
`--generate-manifest` / `--check-manifest`; both validate every grammar against the DTD and every
`words.yaml` against `conformance/schema/words.schema.json` before writing anything. The DTD is now
also published at `conformance/HermitCrabInput.dtd`, because a consumer receives `conformance/`
alone (PanGloss sparse-checks-out exactly that path) and a path a published product names must
resolve inside it; the library's copy stays because `XmlLanguageLoader` reads it as an embedded
resource by manifest name, and `PublishedDtdMatchesTheLibraryResource` holds the two byte-identical.
Removing `RepositorySourceSnapshot` (dead after this cutover) also removed
`LiveExecutionRootsHaveNoUnresolvedExecutionEdges`, per the Task 7 note above.

**Remaining order to a PanGloss handoff:**

1. **Task 9 leftovers** — the remaining resolver cases and a single derivation of
   `coverage.csv`/`rules.csv`.
2. **Task 10 is cut as specified** — its two modes exist as the manifest flags above.
3. **Task 11** — cross-platform identity for the manifest product.
4. **Task 12 is deferred** — the data-only NuGet package; PanGloss consumes via the sparse submodule
   today, and a package may follow later.
5. **Task 13 is done, by a different document than specified** — `docs/pangloss-handoff.md` is the
   handoff record; see Task 13's own note for why it did not update the plan named there.

**Where the closure still stops, and why deliberately.** Owned projects are now bound as
`CompilationReference`s, so a symbol reached from another owned project carries real syntax rather
than metadata. `CSharpInventoryReader.ModelOrNull` treats a tree the current context has no model
for as outside the censused source and does not follow it, which keeps the closure exactly as wide
as it was before the cutover. Widening it across compilations — via the raw-edge model and the
`OwnedSymbolKey` bridge — was to have been Task 7 Step 6; with Task 7 cut, this closure stays as
described here permanently rather than as an interim state.

**Approximation compensations, now confined to partial source sets.** `CSharpInventoryReader.Read`
still serves synthetic in-memory source sets (roughly a hundred unit tests, and any census of a
subset of a project's files). For those, `SemanticCoverageSourceSet.CompleteProjects` drops a fully
censused project from the reference set, and `ApproximationOnlyErrorIds` is tolerated. Neither
applies on the graph path, which passes `referencesAreExact`.

---

## File and Module Structure

The external seam remains the two JSON products. Compiler/MSBuild complexity stays behind one internal deep module.

- `global.json`: exact SDK selection.
- `eng/HcSemanticCompilerInputs.targets`: compiler-input capture target; no custom tasks or JSON writing.
- `SemanticCoverage/CompilationGraphModels.cs`: immutable fixed graph, project, profile, source, reference, and diagnostic domain records.
- `SemanticCoverage/MsBuildCaptureProtocol.cs`: strict native-JSON DTO parsing and protocol validation.
- `SemanticCoverage/MsBuildProcessRunner.cs`: bounded shell-free child-process adapter.
- `SemanticCoverage/RepositoryCompilationGraphLoader.cs`: `IRepositoryCompilationGraphLoader` implementation and fixed project/profile orchestration.
- `SemanticCoverage/CSharpCommandLineInputParser.cs`: Roslyn command-line parsing and option extraction.
- `SemanticCoverage/CompilerSourceClassifier.cs`: owned/generated-support admission.
- `SemanticCoverage/AnalyzerMetadataInspector.cs`: unloaded PE inspection and source-generator rejection.
- `SemanticCoverage/RoslynCompilationGraph.cs`: topological compilations and owned-reference replacement.
- `SemanticCoverage/OwnedSymbolKey.cs`: cross-compilation canonical identity.
- `SemanticCoverage/CompilationGraphHashing.cs`: canonical graph/input/toolchain hashes.
- `SemanticCoverage/ExecutionEdgeModels.cs`: raw edge facts and consumer constraints.
- `SemanticCoverage/ExecutionBoundaryAudit.cs`: exact-once catalog join and constraint production.
- `SemanticCoverage/SemanticInventoryArtifact.cs`: versioned semantic product model and canonical writer.
- `SemanticCoverage/ConformanceCorpusArtifact.cs`: versioned corpus model and canonical writer.
- `SemanticCoverage/ConformanceCorpusGenerator.cs`: strict YAML-to-JSON normalization, source-derived parity, CSV/rules derivation.
- `SemanticCoverage/GrammarValidation.cs`: contained XML/DTD resolver and validation.
- `SemanticCoverage/ArtifactCommand.cs`: validate-first `generate` and byte-exact `check` orchestration.
- `conformance/schema/*.schema.json`: owned Draft 2020-12 format contracts.
- `conformance/generated/*.v1.json`: PanGloss consumer products.

No implementation task may make the production CLI fall back to `RepositorySourceSnapshot`'s current directory scan or `CSharpCompilationProfile`'s runtime/TPA reference approximation.

---

### Task 1: Pin and capture the compiler contract

**Files:**
- Create: `global.json`
- Create: `eng/HcSemanticCompilerInputs.targets`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/MsBuildCaptureProtocol.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/DotnetMsbuildCompilationGraphLoaderTests.cs`

- [x] **Step 1: Write locked tests for the SDK/compiler pin and protocol marker**

Add tests that load `global.json`, the Conformance project, and the target file and assert exact values:

```csharp
Assert.That(globalJson["sdk"]!["version"]!.GetValue<string>(), Is.EqualTo("10.0.303"));
Assert.That(globalJson["sdk"]!["rollForward"]!.GetValue<string>(), Is.EqualTo("disable"));
Assert.That(compilerReferences, Does.Contain("$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll"));
Assert.That(targetText, Does.Contain("PanGlossCompilerInputProtocol"));
Assert.That(targetText, Does.Contain("hc-semantic-msbuild/v1"));
Assert.That(targetText, Does.Not.Contain("WriteLinesToFile"));
```

- [x] **Step 2: Run the tests and verify RED**

Run:

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~DotnetMsbuildCompilationGraphLoaderTests
```

Expected: failure because the pin, target, and parser do not exist.

- [x] **Step 3: Add the exact SDK pin, SDK Roslyn references, and capture target**

Create `global.json` with the spec's exact SDK object. Remove the independent Roslyn package reference and import `eng/HcRoslynCompilerReferences.props` so the conformance and test projects compile against the pinned SDK's exact Roslyn binaries. Capture `RoslynAssembliesPath` and verify byte equality with the loaded parser. Define `_PanGlossCaptureCompilerInputs` with the exact dependency chain and property marker from the spec; set no custom tasks and emit no file.

Run the explicit package restore once after changing the project; later semantic commands remain `--no-restore` and fail clearly when assets are absent:

```powershell
dotnet restore Machine.sln
```

- [x] **Step 4: Implement strict native JSON protocol parsing**

Expose an internal parser shaped as:

```csharp
internal static class MsBuildCaptureProtocol
{
    internal const string Version = "hc-semantic-msbuild/v1";
    public static CapturedCompilerInputs Parse(ReadOnlySpan<byte> utf8Json);
}
```

Reject malformed JSON, missing/duplicate requested fields, a wrong marker, null item identities, and unknown item families. Preserve item order and metadata without resolving paths yet.

- [x] **Step 5: Run the focused gate and commit**

Expected: all `DotnetMsbuildCompilationGraphLoaderTests` pass.

Commit:

```text
build(conformance): pin compiler graph capture contract
```

---

### Task 2: Model the closed sixteen-node graph

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CompilationGraphModels.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/RepositoryCompilationGraphTests.cs`

- [x] **Step 1: Write locked graph-matrix tests**

The test must assert the exact four project records, four profile records, sixteen node keys, and direct edge table from the spec. Add negative cases for extra/missing/outside/cyclic/ambiguous nodes, plus a positive multi-target case proving that one explicitly configured compatible target is accepted.

```csharp
Assert.That(graph.Nodes, Has.Count.EqualTo(16));
Assert.That(graph.Nodes.Select(n => n.Key.ProfileId).Distinct(),
    Is.EquivalentTo(new[] { "base", "single-threaded", "output-analyses", "combined" }));
Assert.That(graph.ProjectEdges, Is.EqualTo(ExpectedOwnedEdges));
```

- [x] **Step 2: Run and verify RED**

Run:

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~RepositoryCompilationGraphTests
```

Expected: compile failure because the graph types do not exist.

- [x] **Step 3: Implement immutable closed models and validation**

Use a no-public-Roslyn model:

```csharp
internal sealed record RepositoryCompilationGraph(
    IReadOnlyList<RepositoryGraphNode> Nodes,
    IReadOnlyList<RepositoryProjectEdge> ProjectEdges);

internal sealed record BuildProfile(string Id, IReadOnlyList<string> AdditionalSymbols);
```

Profile construction is internal and fixed. The loader interface accepts only repository root and cancellation token.

- [x] **Step 4: Run the Step 2 command and require GREEN, then commit**

Commit:

```text
feat(conformance): model fixed HermitCrab compilation graph
```

---

### Task 3: Capture MSBuild inputs through a bounded process adapter

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/MsBuildProcessRunner.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/RepositoryCompilationGraphLoader.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/DotnetMsbuildCompilationGraphLoaderTests.cs`

- [x] **Step 1: Lock process, argument, assets, and containment behavior**

Inject a fake process runner. Assert `UseShellExecute=false`, every argument is a separate `ArgumentList` entry, cwd is repository root, `/nr:false`, no restore, exact target/TFM/profile values, 120-second timeout, 64-MiB stdout limit, private intermediate directory, and explicit existing assets paths. Add failures for stderr, nonzero exit, timeout, process-tree termination failure, oversized output, path escape, reparse point, missing assets, and protocol mismatch.

- [x] **Step 2: Verify RED**

Run:

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~DotnetMsbuildCompilationGraphLoaderTests
```

Expected: compile failure because no process or loader implementation exists.

- [x] **Step 3: Implement the process adapter and fixed orchestration**

The process seam is:

```csharp
internal interface IMsBuildProcessRunner
{
    ValueTask<ProcessCapture> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        int maxStandardOutputBytes,
        CancellationToken cancellationToken);
}
```

Capture stdout/stderr asynchronously, kill the tree on timeout, fail when termination is unconfirmed, and never invoke a shell. Validate every resolved path before reading. Delete only the verified private temporary directory.

- [x] **Step 4: Add one live query test**

Against the already-restored repository, query all sixteen nodes and assert the marker, selected TFM, exact project IDs, nonempty compiler arguments, and zero repository writes outside the private query directory.

- [x] **Step 5: Run the Step 2 command and require GREEN, then commit**

Commit:

```text
feat(conformance): capture compiler inputs through MSBuild
```

---

### Task 4: Normalize compiler inputs and admit generated support exactly

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CompilerInputModel.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CSharpCommandLineInputParser.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CompilerSourceClassifier.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/AnalyzerMetadataInspector.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/DotnetMsbuildCompilationGraphLoaderTests.cs`

- [x] **Step 1: Lock command-line and source-classification tests**

Use captured/synthetic compiler arguments to assert final parse/compilation options, reference aliases, `EmbedInteropTypes`, compile order, analyzer/config/additional files, and base-plus-profile symbols. Assert only sources whose canonical paths equal captured `GeneratedAssemblyInfoFile` or `TargetFrameworkMonikerAssemblyAttributesPath` become `GeneratedSupport`; implicit `Using`, pattern-spoofed/custom generated source, custom/unexpected source generators, unknown switches, and inconclusive analyzer metadata fail with exact codes. Admit only the exact five generator basenames supplied under the selected `Microsoft.NETCore.App.Ref/<pack-version>/analyzers/dotnet/cs` directory, recording canonical path, assembly identity, SHA-256, and a `requires-zero-output-probe` disposition. Add a queried SDK/compiler identity mismatch case and require the exact `incompatible-compiler-toolchain` diagnostic before parsing proceeds. Add reference-parser diagnostics and analyzer/metadata-reader diagnostics as fail-closed cases.

- [x] **Step 2: Verify RED**

Run:

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~DotnetMsbuildCompilationGraphLoaderTests
```

Expected: the new toolchain, parser-diagnostic, and generated-source assertions fail.

- [x] **Step 3: Parse with `CSharpCommandLineParser` and inspect analyzers without loading**

The parser returns a domain record and fails on every Roslyn parser diagnostic. PE inspection uses `PEReader`/`MetadataReader`; it never calls `Assembly.LoadFrom`. It classifies the five provenance-bound SDK generators for the mandatory Task-5 probe without executing them here.

- [x] **Step 4: Run the Step 2 command and require synthetic plus live capture GREEN, then commit**

Commit:

```text
feat(conformance): normalize exact compiler inputs
```

---

### Task 5: Build zero-error owned compilations and symbol bridge

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/RoslynCompilationGraph.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/OwnedSymbolKey.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CompilationDiagnostics.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CSharpInventoryReader.cs`
- Retire: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CSharpCompilationProfile.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/CSharpInventoryReaderTests.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ExecutionClosureTests.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/RepositorySourceSnapshotTests.cs`

- [x] **Step 1: Lock cross-project and fail-closed tests**

Test Machine `IRule` ownership, HC inheritance, Tool symbols, Conformance's executable project reference, transitive references, retargeted symbols, aliases, and canonical member keys. Require every compiler error anywhere—including generated and non-audited source—to abort graph construction. Keep `RepositorySourcePathCannotBypassSemanticCompilationFailure` red until exemptions are gone. Assert ordinary C# warnings remain in the graph diagnostics and warnings promoted by effective C# compiler options are fatal. Every MSBuild warning/error and every metadata/reference-parser diagnostic is preserved with stable origin/severity/code/message data and is fatal unconditionally. Execute every Task-4 `requires-zero-output-probe` SDK generator through Roslyn against the node's captured syntax trees, references, parse options, analyzer configuration, and additional files; require zero generated trees and zero generator diagnostics, with output, exceptions, or load failures fatal.

- [x] **Step 2: Verify RED**

Run:

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter "FullyQualifiedName~CSharpInventoryReaderTests|FullyQualifiedName~ExecutionClosureTests|FullyQualifiedName~RepositorySourceSnapshotTests"
```

Expected: graph/symbol types are absent and the repository-path compiler-error regression remains red.

- [x] **Step 3: Implement topological compilations**

Replace every owned resolved binary reference with the already-built upstream `CompilationReference`. Reject any owned `bin`/`obj` DLL. Run the provenance-bound SDK generator probe after the exact node compilation is assembled and before semantic inventory begins; generated output is unsupported in v1 and therefore fatal rather than added to the coverage denominator. Build canonical keys as:

```text
owned:<project-id>/<assembly-identity>/<canonical-symbol-id>
```

- [x] **Step 4: Cut compilation and surface-census authority over to the graph**

Remove `IsActionableCompilationError`, `IsRepositoryImplementationSource`, diagnostic-ID exclusions, audited-scope compiler filtering, and runtime/TPA fallback from graph-backed compilation and surface census. Synthetic `CSharpInventoryInput` remains available only as an explicit test seam and cannot be selected by CLI authority.

At this checkpoint, `RoslynCompilationGraph` is the sole authority for compilations, references, symbol ownership, and surface-census facts. The existing per-compilation `BuildExecutionClosure` may remain only as an explicitly labeled compatibility path. Do not merge its results with graph facts or describe them as graph-wide, source-complete, or complete coverage. Production commands that require cross-project execution reachability remain unavailable until Task 7 emits and audits the graph-wide raw-edge facts.

- [x] **Step 5: Run the Step 2 command plus `FullyQualifiedName~RepositoryCompilationGraphTests`, require GREEN, and commit**

Commit:

```text
feat(conformance): compile complete owned source graph
```

---

### Task 6: Canonical graph hashing

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CanonicalJson.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/LogicalPathTokens.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CompilationGraphHashInputs.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CompilationGraphHashing.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CompilationGraphModels.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/RepositoryCompilationGraphLoader.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/RepositoryCompilationGraphTests.cs`

- [x] **Step 1: Lock sensitivity and invariance tests**

Require SHA-256 lowercase hex and changes for source/import/assets/reference/analyzer/compiler/loader/profile changes. Require invariance under repository relocation, Windows/Linux separators, timestamps, CRLF/LF, and native host/RID. Assert canonical path tokens and case-collision rejection.

- [x] **Step 2: Verify RED**

Run:

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~RepositoryCompilationGraphTests
```

Expected: hash types and invariance assertions fail.

- [x] **Step 3: Implement canonical UTF-8 JSON hashing**

Introduce a loader-owned immutable `CompilationGraphHashInputs` snapshot. It contains typed project, profile, node, edge, file, reference, analyzer, asset/lock-file, and toolchain records. Every file record carries an admitted logical path plus immutable content or its content digest captured during `LoadAsync`; the hasher never reopens a live path. Preserve compiler argument and source order with explicit ordinals. Sort only set-valued arrays by canonical identity.

Before deleting the private capture directory, retain generated-support and analyzer-configuration bytes. Also retain each `MSBuildAllProjects` import and its content, canonicalized `project.assets.json`, generated NuGet props/targets, explicit lock-file presence/content, `EditorConfigFiles`, `ProjectReference` metadata, external-reference identity/content/aliases/embed-interop properties, capture-target content, and stable evaluated parse/compilation settings. Toolchain inputs retain the pinned SDK, MSBuild, Roslyn/compiler, and loader identities plus content hashes; paths alone are never identity. Reject unknown roots and case-colliding logical paths while constructing the snapshot. Admit every physical package folder actually used by resolved compiler inputs, but map all of them into `nuget:/<package-id>/<version>/<relative-path>`; never hash the physical package-root name or precedence, and reject canonical package collisions unless identity and content agree.

The process adapter's synthetic unit-test runner may inject an explicit immutable hash-environment/snapshot builder. That seam is test-only infrastructure supplied through construction; production always uses the validated filesystem capture and has no fallback identity, placeholder hash, or omission path.

Normalize textual content to UTF-8 with LF endings for hashing while leaving compilation bytes unchanged. Sort canonical JSON object keys and set-valued arrays ordinally; preserve compiler-order-sensitive arrays with explicit ordinals. Produce `GraphInputHash`, `ToolchainHash`, and `GraphHash` independently.

At this slice, add both the immutable hash-input snapshot and computed `GraphHashes` value to `RepositoryCompilationGraph` through a final validated construction step; earlier graph construction intentionally has no placeholder or fake hash. `CompilationGraphHashing.Compute` accepts only the snapshot, never a repository root or filesystem service.

- [x] **Step 4: Run the Step 2 command twice from distinct temporary repository roots, require GREEN, and commit**

Commit:

```text
feat(conformance): hash semantic compiler authority deterministically
```

---

### Task 7: Emit and audit every execution boundary — CUT (see Status)

**Cut, not shipped.** The census splits into 1059 `dtd:` surfaces (already shipping via
`DtdInventoryReader`) and 592 engine-internal surfaces; this task would have required 233
hand-written boundary dispositions plus permanent upkeep to cover the latter, in service of a
completeness claim that was unreachable anyway (`SIL.Machine` is never censused). The body below is
kept as the record of what was considered.

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/ExecutionEdgeModels.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/ExecutionBoundaryAudit.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/InventoryModels.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CSharpInventoryReader.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticCatalog.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticCoverageAudit.cs`
- Modify: `conformance/semantic-catalog.yaml`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ExecutionClosureTests.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ExecutionBoundaryAuditTests.cs`

- [ ] **Step 1: Lock raw-edge emission tests**

Assert every source, metadata, interface/virtual/delegate, callback, dynamic, construction, and profile-bound operation emits one stable edge. Raw evidence must remain nonempty even when later audit dispositions pass.

- [ ] **Step 2: Run the raw-edge tests and verify RED**

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~ExecutionClosureTests
```

Expected: the new stable edge assertions fail because edge facts are not emitted.

- [ ] **Step 3: Add edge facts to the reader and require the Step 2 command GREEN**

Use stable edge IDs derived from caller, operation kind, canonical callee, normalized operation syntax, and same-kind ordinal. Source locations are display evidence, not identity.

- [ ] **Step 4: Lock exact-audit tests and verify RED**

Test exact-once catalog rows, stale/duplicate rows, dispatch/callback-set drift, owned expansion, reviewed opaque/callback dispositions, and explicit unresolved/blocked constraints.

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~ExecutionBoundaryAuditTests
```

Expected: compile failure because `ExecutionBoundaryAudit` does not exist.

- [ ] **Step 5: Implement `ExecutionBoundaryAudit` and require the Step 4 command GREEN**

Return an audited report containing all edges, exact dispositions, and constraints. Missing/unreviewed/stale/duplicate facts are failures; legitimate open/blocked facts survive only as constraints.

- [ ] **Step 6: Replace the compatibility closure, classify the live edge manifest, and run the live gate**

Drive cross-project execution reachability from the graph-wide raw-edge model and `OwnedSymbolKey` bridge. Delete or isolate the old per-compilation compatibility closure so no production authority path can select it. Do not suppress diagnostics to make the gate green. Extend catalog entries with reason and callback/dispatch set pins until the live audit has zero unreviewed/stale/duplicate facts. Only after this step may a production command describe execution coverage as graph-wide; the final `complete coverage` claim still also requires Tasks 8-13 and the governing complete-coverage plan's completion audit.

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter "FullyQualifiedName~ExecutionClosureTests|FullyQualifiedName~ExecutionBoundaryAuditTests|FullyQualifiedName~SemanticCoverageAudit"
```

- [ ] **Step 7: Obtain fresh Sol spec review, then quality review, correct, re-review, and commit**

Commit:

```text
feat(conformance): audit every semantic execution boundary
```

---

### Task 8: Publish the versioned semantic-domain product — CUT (see Status)

**Cut along with Task 7**, which this task depended on for its audited graph. The grammar format and
its interpretation are what matter; the HermitCrab implementation is the "golden" engine but nothing
about it is special except that it covers the whole grammar, so a semantic-domain product describing
the engine's internal execution facts was not worth the Task 7 cost above. The body below is kept as
the record of what was considered.

**Files:**
- Create: `conformance/schema/semantic-inventory.schema.json`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticInventoryArtifact.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/SemanticInventoryArtifactGenerator.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/SemanticInventoryArtifactTests.cs`
- Generate: `conformance/generated/hc-semantic-inventory.v1.json`

- [ ] **Step 1: Lock schema, validator pin, and canonical-product tests**

Require the project package reference `JsonSchema.Net` exactly `9.4.0`, Draft 2020-12, `additionalProperties:false`, exact format ID, four explicit configuration IDs, referentially valid nonempty constraint fact lists, stable ordering, canonical bytes, and graph/source/toolchain hashes. Test the operational constraint intersection predicate used by PanGloss.

- [ ] **Step 2: Verify RED**

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~SemanticInventoryArtifactTests
```

Expected: schema, product types, and exact validator package reference are absent.

- [ ] **Step 3: Pin and implement the model, schema validator, and canonical writer**

Add an exact `JsonSchema.Net` `9.4.0` package reference and validate with its Draft 2020-12 implementation. Do not hand-roll schema validation. The schema and generator must agree on exact required fields and version policy.

Run the explicit package restore after changing the project:

```powershell
dotnet restore Machine.sln
```

- [ ] **Step 4: Run the Step 2 command, generate from the live audited graph, and check byte identity**

- [ ] **Step 5: Commit**

```text
feat(conformance): publish versioned semantic inventory
```

---

### Task 9: Publish the normalized XML-backed conformance corpus — superseded by the manifest (see Status)

**Shipped, then superseded.** This task originally produced `conformance/generated/hc-conformance-corpus.v1.json`
(28 fixtures, 414 cases), schema-validated against `conformance/schema/conformance-corpus.schema.json`
in test and gated against drift by `TheCheckedInCorpusMatchesRegeneration` plus
`hc-conformance --check-corpus`. Commit 93cf1e5b deleted both the product and the schema and replaced
them with the 13KB `conformance/generated/hc-conformance-manifest.v1.json`
(`hc-conformance-manifest/v1`, `conformance/schema/conformance-manifest.schema.json`), regenerated
with `--generate-manifest` and checked with `--check-manifest`; see Status above for the full
reasoning. The steps below describe the original corpus shape and are kept as the historical record
of what Task 9 built before that cutover; they are no longer the current product. Still open in the
steps left unticked, now against the manifest rather than the corpus:

- Step 2 covers traversal only. Reparse-point/symlink and case-collision resolver cases are not
  written.
- Step 3's RED verification never happened: the tests and the implementation were written together
  rather than test-first, so none of them has been observed failing for the right reason. The one
  exception is unplanned and worth more than the procedure — validating the authored files against
  the published schema failed on four fixtures, and the fixtures turned out to be right and the
  product wrong. See the `neutralizes` note in the governing spec.
- Step 5's derived-output half is not done, and Step 6 is untouched: `coverage.csv` and `rules.csv`
  still come from `CoverageReport.WriteCsvs` rather than from the validated corpus model, so the
  same facts are derived twice.

**Files:**
- Create: `conformance/schema/words.schema.json`
- Create: `conformance/schema/conformance-corpus.schema.json`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/ConformanceCorpusArtifact.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/ConformanceCorpusGenerator.cs`
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/GrammarValidation.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/WordsYamlLoader.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ConformanceCorpusArtifactTests.cs`
- Generate: `conformance/generated/hc-conformance-corpus.v1.json`

- [x] **Step 1: Lock the three format contracts and fixture-discovery boundary**

Test exact identifiers `hc-conformance-words/v1`, `sil-machine-hermit-crab-input-xml/v1`, and `hc-conformance-corpus/v1`; repository-relative paths; grammar/DTD/words hashes; uppercase UTF-8 percent-encoded case IDs; unique input per fixture; strict field mapping; all four outcome discriminators; crash cardinality; set/order semantics; and `additionalProperties:false`. Discovery accepts only direct fixture directories under `conformance/languages/*` and `conformance/edge-cases/*`; category and `fixtureId` derive from those exact roots. Add a legacy Sena-style directory outside those roots and prove it cannot become an organizing fixture.

- [ ] **Step 2: Lock XML resolver and path-containment tests**

Accept only `SYSTEM "HermitCrabInput.dtd"`, remap it to the pinned repository DTD, disable network/filesystem fallback, and reject every other external entity. Add traversal, reparse/symlink, and case-collision cases.

- [ ] **Step 3: Run format/discovery/resolver tests and verify RED**

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~ConformanceCorpusArtifactTests
```

Expected: corpus/schema types are absent and discovery/XML assertions fail.

- [x] **Step 4: Implement schema-backed YAML normalization and XML validation; require the Step 3 command GREEN**

Reuse `WordsYaml`/`WordsYamlLoader`; do not create a second interpretation. Validate the YAML representation against `words.schema.json`, then apply the exact mapping table from the spec. Keep XML authoritative and reference it by path/hash only.

- [x] **Step 5: Lock source-hash and derived-output tests, then verify RED**

Assert `sourceHash` changes independently for each grammar, each words file, each schema, the DTD, `conformance/constructs.txt`, `docs/conformance-migration-ledger.md`, and `conformance/parity-check.py`, while remaining path/separator/EOL invariant. Assert `coverage.csv`, `rules.csv`, and parity results are derived without reading either checked-in CSV as input.

Run the Step 3 command. Expected: source-hash sensitivity and derived-output assertions fail.

- [ ] **Step 6: Derive parity and CSV/rules from one validated model; require the Step 3 command GREEN**

Compute provenance, construct, phonology-free, and absolute floors without reading `coverage.csv`. Reject duplicate display-language keys. Generate `coverage.csv`, `rules.csv`, and corpus JSON together in memory; only write after every validation passes. Run the legacy parity checker after byte-fresh CSV verification.

- [x] **Step 7: Generate the live product, schema-validate, and commit**

```text
feat(conformance): publish normalized PanGloss corpus
```

---

### Task 10: Add validate-first `generate` and byte-exact `check` — CUT as specified (see Status)

**Cut as its own task**, because its two modes already exist in narrower form: `--generate-manifest`
and `--check-manifest` (renamed from `--generate-corpus`/`--check-corpus`), both validating every
grammar against the DTD and every `words.yaml` against `words.schema.json` before writing anything.
The unified `ArtifactCommand` seam described below was not built as a separate task. The body below
is kept as the record of what was considered.

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/ArtifactCommand.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/Program.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/CoverageCliAuthorityTests.cs`
- Modify: `conformance/README.md`
- Modify: `conformance/PROTOCOL.md`

- [ ] **Step 1: Lock subprocess tests for both modes**

Test:

```text
hc-conformance --semantic-coverage generate --repository-root <root>
hc-conformance --semantic-coverage check --repository-root <root>
```

Generation validates every input and artifact in memory before an atomic write. Check performs no writes and exits nonzero on byte drift, schema failure, invalid XML/YAML, stale CSV, duplicate IDs, source-hash drift, or boundary mismatch. Malformed input produces controlled exit 2 without a stack trace.

- [ ] **Step 2: Verify RED**

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~CoverageCliAuthorityTests
```

Expected: subprocess assertions fail because the unified artifact command does not exist.

- [ ] **Step 3: Implement one command seam and update documentation**

Keep `Program` as argument routing only. `ArtifactCommand` loads the graph once, generates both products and CSVs from one model, validates, then either atomically writes or byte-compares.

- [ ] **Step 4: Run the Step 2 command, then generate followed by check twice and commit**

```powershell
dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj -- --semantic-coverage generate --repository-root .
dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj -- --semantic-coverage check --repository-root .
dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj -- --semantic-coverage check --repository-root .
```

```text
feat(conformance): add semantic product generate and check
```

At this point the content is locally PanGloss-consumable, but do not announce the milestone until Task 11's published and cross-platform gates plus independent reviews pass.

---

### Task 11: Prove deployment and cross-platform identity

**Files:**
- Modify: `.github/workflows/ci.yml`
- Modify: `local_check.sh`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/CoverageCliAuthorityTests.cs`

- [ ] **Step 1: Lock a published-output smoke test**

Publish to a verified private temporary directory, start from repository root, and invoke the published DLL's check mode. Assert target/schema deployment and SDK selection.

- [ ] **Step 2: Pin CI SDK and add artifact comparison**

Change `10.0.x` to `10.0.303`. Windows and Linux jobs generate/check both JSON products and upload them plus graph/source/toolchain/corpus hashes. A comparison job requires byte identity.

- [ ] **Step 3: Add the local authoritative gate**

Run the focused tests, published smoke check, fixture self-check, source-derived parity, and semantic artifact check without implicit restore.

- [ ] **Step 4: Run full local verification**

Required evidence:

```powershell
dotnet csharpier check .
dotnet build --no-restore -c Release
dotnet test --no-restore -c Release
dotnet publish src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj --no-restore -c Release -o <verified-private-output>
dotnet <verified-private-output>/hc-conformance.dll --semantic-coverage check --repository-root .
python conformance/parity-check.py
git diff --check
```

- [ ] **Step 5: Fresh independent reviews and correction loop**

Dispatch one fresh Sol/xhigh spec reviewer and, only after spec approval, one fresh Sol/xhigh quality reviewer over the complete compiler-authority-to-artifact range. Return every finding to the responsible implementation agent, rerun focused gates, and obtain re-approval. Then run the scheduled direction challenge: “Are we doing what we think we are doing? Are we getting off track?” against the original complete-coverage objective and PanGloss milestone.

- [ ] **Step 6: Commit the gate and record the verified product baseline**

Commit:

```text
ci(conformance): verify portable semantic products
```

Record the exact commit, four product/schema paths, format IDs, validation command, hashes, and current explicit constraints. Do not announce the final consumer milestone until the deferred data-only NuGet package in Task 12 is independently inspected and reproducible.

---

### Task 12: Pack and publish the data-only PanGloss distribution — DEFERRED (see Status)

**Deferred, not cut.** PanGloss consumes the fixtures via the sparse `machine/conformance` submodule
checkout today, which is why this task was not needed to reach the handoff milestone; a data-only
NuGet package may still follow later if a non-submodule distribution channel becomes worth building.

**Files:**
- Create: `packaging/SIL.Machine.Morphology.HermitCrab.Conformance.Data/SIL.Machine.Morphology.HermitCrab.Conformance.Data.csproj`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ConformanceDataPackageTests.cs`
- Modify: `.github/workflows/ci.yml`
- Modify: `conformance/README.md`

- [ ] **Step 1: Lock the exact package interface and verify RED**

Create a test that packs to a private temporary directory, opens the `.nupkg` with `ZipArchive`, and compares normalized entries to an exact expected set rooted at `contentFiles/any/any/hc-conformance/`. The expected set contains the two generated JSON products; `semantic-inventory.schema.json`, `conformance-corpus.schema.json`, and `words.schema.json`; `HermitCrabInput.dtd`; `coverage.csv`; `rules.csv`; `README.md`; `PROTOCOL.md`; and every direct `languages/*` and `edge-cases/*` fixture's `grammar.xml` and `words.yaml`.

Assert package ID `SIL.Machine.Morphology.HermitCrab.Conformance.Data`, the supplied exact package version, and NuGet repository commit metadata. Assert there are no entries under `lib/`, `ref/`, `runtimes/`, `tools/`, `build/`, or `buildTransitive/`; no `.dll`, `.exe`, or `.pdb`; and no dependency groups in the nuspec. Extract privately, validate both JSON products against the packaged schemas, and validate every referenced XML/DTD and words path without consulting repository files.

Run:

```powershell
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter FullyQualifiedName~ConformanceDataPackageTests
```

Expected: failure because the packaging project does not exist.

- [ ] **Step 2: Implement the content-only pack project**

Create an SDK pack project targeting `net10.0` with `IncludeBuildOutput=false`, `SuppressDependenciesWhenPacking=true`, and package ID `SIL.Machine.Morphology.HermitCrab.Conformance.Data`. Include only the Step 1 whitelist with `Pack=true` and `PackagePath=contentFiles/any/any/hc-conformance/...`. Require explicit `PackageVersion` and `RepositoryCommit`; fail packing when either is absent. The project must not reference the conformance runner or any package/project dependency.

- [ ] **Step 3: Require package reproducibility and GREEN**

Pack twice from the same verified tree with the same package version and repository commit into two private directories. Normalize only ZIP container timestamps during comparison; require identical entry names, uncompressed bytes, CRCs, nuspec metadata, and SHA-256 over the canonical entry-name/content projection. Run the Step 1 command and require GREEN.

- [ ] **Step 4: Add CI verification and tag publication**

After semantic-product `check`, pack with the workflow commit SHA as `RepositoryCommit`, run `ConformanceDataPackageTests`, and upload the `.nupkg` on ordinary CI. In the existing tag-only NuGet push step, publish this package alongside Machine packages. Never publish from an unverified or dirty generated-product state.

- [ ] **Step 5: Independently inspect the package and announce the milestone**

Dispatch a fresh Sol/xhigh read-only reviewer over the package project, exact package contents, nuspec, CI ordering, and an independently created `.nupkg`. Correct findings and obtain approval. Then announce “PanGloss consumer milestone ready” with the exact Machine commit, package ID/version, NuGet source, SHA-256, embedded format IDs, validation command, and explicit semantic constraints.

- [ ] **Step 6: Commit**

```text
feat(conformance): publish data-only conformance package
```

---

### Task 13: Hand the product milestone back to the complete-coverage program — DONE, by a different document than specified

**Superseded as specified, 2026-08-14.** This task named
`docs/superpowers/plans/2026-08-11-generated-hc-semantic-coverage-plan.md` as "the governing
complete-coverage plan" and said the original objective completes only when that plan's
obligation/pair/triple ledger passes its own completion audit. `docs/coverage-strategy.md` is now the
actual governing statement for the coverage claim, and it replaces that ledger design outright (see
the 2026-08-11 plan's own status update for why pairwise/triple enumeration was the wrong axis, not
merely unfinished). The handoff this task called for happened, just not through Steps 1-3 below:
`docs/pangloss-handoff.md` records the delivered product baseline in the honest form
`docs/coverage-strategy.md` requires — unit-layer surfaces (113 of 264 carry a real parse-time delta,
11 more are named exceptions), integration/edge (`conformance/interface-inventory.tsv`, 60 interfaces,
42 exercised/18 not), and integration/chain (in progress, not yet landed) — rather than as an
obligation-ledger status update.

**Files:**
- Modify: `docs/superpowers/plans/2026-08-11-generated-hc-semantic-coverage-plan.md`
- Modify: `docs/superpowers/specs/2026-08-13-hc-semantic-compilation-graph-design.md`

- [ ] **Step 1: Record the delivered authority and product baseline**

Update the governing complete-coverage plan with the exact milestone commit, artifact paths, format IDs, graph/source/toolchain/corpus hashes, validation command, and any still-open constraints. Mark only superseded compiler-acquisition tasks complete; do not mark semantic classifications, obligations, evidence, or proof coverage complete merely because the consumer products exist.

- [ ] **Step 2: Reconcile the governing spec**

Replace implementation-future language in the compiler-graph and product sections with the delivered contract. Preserve the explicit distinction between the PanGloss consumer milestone and complete semantic coverage.

- [ ] **Step 3: Commit the bounded handoff**

Commit:

```text
docs(conformance): hand off semantic product baseline
```

The remaining carrier classification, obligation, evidence, and proof-registry work continues under `docs/superpowers/plans/2026-08-11-generated-hc-semantic-coverage-plan.md`. The original objective is complete only when that governing plan's completion audit passes; the earlier PanGloss milestone is a deliberately honest, useful intermediate release.

**The paragraph immediately above, and Steps 1-3, are kept as the record of the originally planned
handoff mechanism.** Do not execute them as written: the "governing complete-coverage plan" they name
no longer owns the obligation/pair/triple ledger they describe, because that ledger was rejected, not
merely relocated. Remaining coverage work is now tracked against `docs/coverage-strategy.md`'s four
layers directly — most concretely, closing the 18 named gaps in
`conformance/interface-inventory.tsv` and landing the integration/chain ledger — not against this
task's completion criteria.
