# HC Semantic Compilation Graph Design

Status: approved and reader-tested, including PanGloss importer formats
Date: 2026-08-13

## Purpose

The HermitCrab conformance system must generate a complete, deterministic description of the HC semantic domain for coverage auditing and for PanGloss FST-path planning. That system is development and CI tooling, not production runtime code.

`SIL.Machine` and `SIL.Machine.Morphology.HermitCrab` remain `netstandard2.0` libraries. The conformance executable and its tests may use .NET 10 because they are never linked into an end product. PanGloss consumes a checked-in, versioned domain inventory; it does not consume MSBuild, Roslyn objects, or the conformance runner.

This design replaces the current approximate source snapshot with the compiler's actual four-project input graph. It removes the need for repository-path and compiler-diagnostic exemptions and establishes the trustworthy source authority required before publishing the PanGloss inventory milestone.

## Decision

Use an internal, out-of-process `dotnet msbuild` adapter to capture the final C# compiler inputs for the fixed owned project graph. Build separate Roslyn compilations for each project and connect owned dependencies with `CompilationReference` objects. Reject every syntax or semantic compiler error before execution closure.

The deep module seam is:

```csharp
internal interface IRepositoryCompilationGraphLoader
{
    ValueTask<RepositoryCompilationGraph> LoadAsync(
        RepositoryRoot root,
        CancellationToken cancellationToken);
}
```

One call loads the complete fixed project/profile matrix. `BuildProfile` is an internal closed value with exactly the four variants below; callers cannot select, omit, or add profiles. `RepositoryCompilationGraph` is an immutable domain model containing all sixteen nodes. It contains no public Roslyn or MSBuild types. Roslyn remains an implementation detail, and the public consumer seam remains the generated semantic inventory.

## Owned Project Graph

The loader admits exactly these project/target-framework nodes:

```text
SIL.Machine (netstandard2.0)
└── SIL.Machine.Morphology.HermitCrab (netstandard2.0)
    └── SIL.Machine.Morphology.HermitCrab.Tool (net10.0)
        └── SIL.Machine.Morphology.HermitCrab.Conformance (net10.0)

Conformance also directly references HermitCrab.
```

Missing, extra, cyclic, ambiguous multi-targeted, or repository-external project references fail closed. All owned source is compiled as source. No owned `bin` or `obj` assembly may be treated as trusted metadata.

The four deliberate conditional-symbol profiles remain authoritative:

1. base
2. `SINGLE_THREADED`
3. `OUTPUT_ANALYSES`
4. `SINGLE_THREADED` plus `OUTPUT_ANALYSES`

Each profile adds its symbols to the project's evaluated Release defines; it does not replace SDK or project defines.

The canonical project IDs, paths, and edges are fixed:

| ID | Project path | TFM | Direct owned references |
|---|---|---|---|
| `machine` | `src/SIL.Machine/SIL.Machine.csproj` | `netstandard2.0` | none |
| `hc` | `src/SIL.Machine.Morphology.HermitCrab/SIL.Machine.Morphology.HermitCrab.csproj` | `netstandard2.0` | `machine` |
| `hc-tool` | `src/SIL.Machine.Morphology.HermitCrab.Tool/SIL.Machine.Morphology.HermitCrab.Tool.csproj` | `net10.0` | `hc` |
| `hc-conformance` | `src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj` | `net10.0` | `hc`, `hc-tool` |

Every project node is evaluated and compiled under every profile. Node identity is `(project ID, TFM, profile ID)`; the initial graph therefore has exactly sixteen nodes. An owned project may not appear as a metadata reference in any node.

## Compiler-Input Acquisition

The loader invokes the repository-selected `dotnet msbuild` using `ProcessStartInfo.ArgumentList`, never a shell. The working directory is the canonical repository root. The invocation:

- selects Release and the explicit target framework;
- sets `BuildProjectReferences=false`;
- performs no restore;
- sets `SkipCompilerExecution=true` and `ProvideCommandLineArgs=true`;
- disables node reuse;
- applies a timeout and terminates the process tree on failure;
- imports a repository-owned capture target through `CustomAfterMicrosoftCommonTargets`;
- redirects generated query output to a private temporary intermediate directory while reading the existing restored assets explicitly;
- requests MSBuild's native JSON output for properties and items.

The capture target contains no custom task and does not invoke the compiler. Standard SDK/MSBuild tasks may run because accurate reference resolution, generated inputs, and C# command-line construction require them. The target arranges the compiler-input-producing target dependencies and exposes a protocol marker; it does not serialize its own JSON.

The query captures exactly these item and property families:

- `CscCommandLineArgs`;
- `Compile`;
- `ProjectReference`;
- `ReferencePathWithRefAssemblies`;
- `Analyzer`;
- `AdditionalFiles`;
- `EditorConfigFiles`;
- `Using`;
- `MSBuildAllProjects`;
- assembly name, target framework, output type, language version, nullable mode, defines, unsafe and overflow options;
- SDK, MSBuild, C# compiler, and compiler-tool-path identities.

Exactly one valid native JSON document and the expected protocol marker are required. Nonzero exit, non-whitespace standard error, malformed or oversized output, timeout, missing assets, unresolved references, unknown compiler switches, or unsupported compiler profiles fail with a controlled diagnostic.

The implementation parses `CscCommandLineArgs` with Roslyn's public `CSharpCommandLineParser`. It rejects every parser diagnostic and derives the final parse options, compilation options, sources, metadata references, analyzers, and auxiliary inputs from those arguments rather than trying to reproduce SDK logic.

### Capture protocol

The repository-owned target is `eng/HcSemanticCompilerInputs.targets`. It defines `_PanGlossCaptureCompilerInputs`, whose dependency chain is `ResolveReferences;ResolveKeySource;SetWin32ManifestProperties;FindReferenceAssembliesForReferences;BeforeCompile;CoreCompile`. `CoreCompile` runs with compiler execution skipped; SDK tasks that construct compiler inputs are allowed to run. The target sets `PanGlossCompilerInputProtocol=hc-semantic-msbuild/v1` and contains no serialization task.

The process arguments are this exact logical sequence, with paths supplied as individual `ArgumentList` entries:

```text
dotnet msbuild <project>
  --noAutoResponse /nologo /nr:false /v:quiet
  /t:_PanGlossCaptureCompilerInputs
  /p:Configuration=Release
  /p:TargetFramework=<exact-tfm>
  /p:BuildProjectReferences=false
  /p:RestoreIgnoreFailedSources=false
  /p:SkipCompilerExecution=true
  /p:ProvideCommandLineArgs=true
  /p:CustomAfterMicrosoftCommonTargets=<capture-target>
  /p:IntermediateOutputPath=<private-profile-intermediate>/
  /p:MSBuildProjectExtensionsPath=<repository-project-obj>/
  /p:ProjectAssetsFile=<repository-project-obj>/project.assets.json
  /p:DefineConstants=<evaluated-base-defines-plus-profile-symbols>
  -getProperty:PanGlossCompilerInputProtocol,MSBuildAllProjects,AssemblyName,TargetFramework,LangVersion,Nullable,DefineConstants,AllowUnsafeBlocks,CheckForOverflowUnderflow,OutputType,NETCoreSdkVersion,MSBuildVersion,CscToolPath,RoslynAssembliesPath,GeneratedAssemblyInfoFile,TargetFrameworkMonikerAssemblyAttributesPath
  -getItem:CscCommandLineArgs,Compile,ProjectReference,ReferencePathWithRefAssemblies,Analyzer,AdditionalFiles,EditorConfigFiles,Using
```

The loader first performs a property-only evaluation to obtain base `DefineConstants`; the targeted invocation receives their canonical union with the closed profile symbols. It never accepts arbitrary caller-supplied MSBuild properties.

The loader requires one native MSBuild JSON result with `Properties` and `Items`, the exact marker value, and all requested fields. Additional top-level MSBuild envelope fields may be ignored; duplicate or missing requested fields are fatal. Standard output is limited to 64 MiB, the process timeout is 120 seconds per node, and any non-whitespace standard error, nonzero exit, invalid UTF-8/JSON, or process-tree termination failure is fatal.

Assets are never restored implicitly. The explicit `project.assets.json` and generated NuGet props/targets must exist, be parseable, name the selected TFM, and resolve consistently through `ResolveReferences`. Staleness is semantic, not timestamp-based: a missing import/reference, unresolved asset, project/TFM mismatch, or MSBuild resolution diagnostic is fatal. Canonicalized assets JSON participates in `GraphInputHash`, so an explicitly restored dependency change cannot masquerade as the previous graph.

## Generated Inputs and Analyzers

The source set is the final compiler source set, including SDK-generated assembly-information and target-framework attribute files. Each source is classified as:

- `Owned`: repository-authored source that can contribute semantic coverage;
- `GeneratedSupport`: compiler input that binds the compilation but does not enter the coverage denominator.

Ordinary analyzers, analyzer configuration, additional files, and editor configuration participate in freshness hashing. A source generator normally fails with `unsupported-source-generator`; generator output may never be silently omitted. The only initial exception is the exact five generator assemblies supplied implicitly beneath the selected `Microsoft.NETCore.App.Ref/<pack-version>/analyzers/dotnet/cs` reference-pack directory: COM interface, JavaScript import, library import, `System.Text.Json`, and regular-expression generation. Their canonical path, assembly identity, and SHA-256 are retained as provenance evidence. They are executed against the captured compilation in the graph-construction stage, which requires zero generated trees and zero generator diagnostics. Any output, diagnostic, load failure, path/provenance mismatch, unexpected generator, or custom generator fails closed.

An input is `Owned` only when its canonical path is a non-generated `Compile` item within its admitted project directory and repository root. An input under the newly created private intermediate directory is `GeneratedSupport` only when its canonical path exactly equals MSBuild's captured `GeneratedAssemblyInfoFile` or `TargetFrameworkMonikerAssemblyAttributesPath`, and both captured paths must remain within that private directory. Immediately before reading either file, the loader revalidates every component from the private root through the file itself and rejects any symlink or reparse point. Pattern-compatible custom names are rejected. The private directory is the provenance boundary because evaluated SDK `Compile` items do not consistently carry `Generated` or `AutoGen` metadata. Any compiler source outside those sets is `unsupported-compiler-source`. A nonempty `Using` item set is `unsupported-implicit-global-using` until generated global-using support is explicitly modeled.

Analyzer assemblies are first inspected as PE metadata without loading them. Any type implementing `Microsoft.CodeAnalysis.ISourceGenerator` or `Microsoft.CodeAnalysis.IIncrementalGenerator`, or generator registration metadata that cannot be classified conclusively, causes `unsupported-source-generator` unless it is one of the five exact SDK reference-pack generators above. Admitted SDK generators carry a `requires-zero-output-probe` disposition until graph construction executes them. Custom-target generated compiler sources follow the same source-classification rules and therefore fail unless explicitly admitted as generated support.

## Compilation and Symbol Bridge

The loader builds one Roslyn compilation per project/profile node in topological order. External references use the exact MSBuild-resolved compiler reference assemblies, preserving aliases and `EmbedInteropTypes`. Every resolved owned reference is replaced with a `CompilationReference`, including transitive owned references and the executable Tool reference used by Conformance.

The graph-wide symbol bridge identifies owned definitions by a canonical key containing project identity, assembly identity, and original-definition signature. It must not rely on `SymbolEqualityComparer` across compilations because downstream symbols may be retargeted wrappers.

The key grammar is `owned:<project-id>/<assembly-identity>/<canonical-symbol-id>`. Assembly identity uses simple name, version, culture, and public-key token in ordinal normalized form. `canonical-symbol-id` uses the existing catalog encoding for namespaces, named types (including arity and containing type), methods, constructors, operators, conversions, properties, indexers, fields, and events. Parameter modifiers and canonical parameter types are part of member IDs. Ownership is determined from resolved project-reference metadata and the fixed project table before a binary reference is created. Retargeted symbols are reduced to original definitions and re-keyed; ambiguous or missing keys are fatal.

Execution closure begins only after every owned compilation in every profile has zero error diagnostics. Every error is fatal regardless of path, audited scope, or diagnostic ID. The following compatibility mechanisms are deleted:

- repository `src/` path exemptions;
- audited-scope-only compiler-error filtering;
- compiler diagnostic-code allowlists;
- runtime/TPA metadata fallback for owned projects.

Raw execution edges remain evidence. A later boundary-audit layer classifies them as expanded, reviewed, profile-bound, or unresolved; it does not make the inventory green by suppressing facts.

All MSBuild warnings and errors produced by capture are fatal because they may signal changed compiler inputs. C# diagnostics with effective severity `Error` are fatal, including warnings promoted by evaluated compilation options. Non-error compiler warnings are retained as non-authoritative diagnostics and do not affect reachability. Ordinary analyzer assemblies are not executed in this slice; their bytes and configuration affect freshness. Only the admitted SDK reference-pack generators are executed, solely to prove that they produce zero trees and zero diagnostics for every graph node. Metadata and reference parser diagnostics are fatal.

## Determinism and Freshness

Pin the repository SDK exactly with `global.json` and disable roll-forward:

```json
{
  "sdk": {
    "version": "10.0.303",
    "rollForward": "disable",
    "allowPrerelease": false
  }
}
```

The conformance and test projects reference `Microsoft.CodeAnalysis` and `Microsoft.CodeAnalysis.CSharp` directly from the pinned SDK's `$(MSBuildToolsPath)/Roslyn/bincore` through `eng/HcRoslynCompilerReferences.props`. A public NuGet package with the same `5.6.0.0` assembly version is not assumed to contain the SDK compiler's bytes. The capture records `RoslynAssembliesPath`; the loader compares the queried compiler content with the parser assembly content and fails `incompatible-compiler-toolchain` on any mismatch rather than downgrading language options.

Produce separate hashes:

- `GraphInputHash`: canonical source and generated-source content, project topology, evaluated settings, imported project/props content, external reference content and identity, analyzers, auxiliary files, assets relevant to resolution, and capture-target content;
- `ToolchainHash`: pinned SDK, MSBuild, Roslyn/compiler, and loader implementation identities and content;
- `GraphHash`: ordered node/profile hashes and dependency edges.

Absolute repository, SDK, NuGet-cache, and temporary paths are replaced by logical tokens before hashing. Timestamps and host-local paths never participate. Text inputs normalize CRLF and lone CR to LF. Canonical path comparisons reject traversal, symlink/reparse escape, and case-colliding inputs.

Native OS, host, and RID details may be recorded separately as non-authoritative environment evidence. Under the pinned toolchain, Windows and Linux must produce the same authoritative graph and inventory hashes.

All authoritative hashes are SHA-256 serialized as lowercase hexadecimal. Inputs use UTF-8 canonical JSON with ordinal-sorted object keys. Set-like arrays sort by canonical identity; compiler-order-sensitive arrays, such as command-line switches and source order, preserve evaluated order and carry an ordinal. Logical paths use only `repo:/`, `sdk:/`, `nuget:/`, `generated:/`, and `ancestor-editorconfig:/` tokens followed by normalized `/`-separated relative paths. The last token exists because MSBuild's `EditorConfigFiles` item walks up to every ancestor directory of the project, which can fall outside every other admitted root when the repository is checked out inside another one (this repository's own worktree layout does exactly that); such a file's content still affects compilation and is hashed, but it is identified only by `ancestor-editorconfig:/<N>`, where `<N>` is the number of directory levels separating it from the repository root, never by its physical path, so relocating either checkout leaves the hash unchanged. Every other path outside the admitted roots still fails closed. More than one restored physical NuGet package folder may contribute inputs. Every package-folder path maps into the single canonical `nuget:/<package-id>/<version>/<relative-path>` namespace; physical package-root names and precedence never participate. Duplicate canonical package entries are admitted only when identity and content agree exactly, and conflicts fail closed. The discriminator `hc-compilation-graph/v1`, project/profile IDs, and effective profile symbols participate in `GraphInputHash`. If a NuGet lock file exists it participates as a canonical input; its absence is represented explicitly. Consumer schema and inventory format versions participate in the generated inventory content hash, not the compiler graph hash.

The loader never restores packages. Missing or stale assets produce a controlled error directing the operator to restore explicitly. NuGet lock files are recommended for clean-machine reproducibility but are a separate repository policy decision.

## Security and Containment

Out-of-process MSBuild provides process and assembly-load isolation; it is not a security sandbox. MSBuild project evaluation and standard hooks can execute code. Therefore the loader accepts only the explicitly trusted Machine repository and fixed project allowlist. Supporting arbitrary repositories is out of scope and would require operating-system sandboxing.

All paths returned by MSBuild are canonicalized and validated against their allowed roots before reading. Repository source and imports must remain within the repository. External references may reside only in the selected SDK/reference-pack or package-cache roots recorded by the loader. Query artifacts use a newly created private temporary directory and are removed through a bounded, verified cleanup path.

The trusted roots are the canonical repository root, the selected SDK root reported by pinned `dotnet`, reference-pack roots beneath that SDK, the effective NuGet package root, and one newly created private query directory. The four project paths above are the complete project allowlist. The SDK and package cache are trusted restored inputs, not sandboxed content. Each process receives the 120-second and 64-MiB limits above. Failure to terminate a timed-out child or remove the verified private query directory is a controlled fatal error reported with the exact bounded path; cleanup never targets repository `bin`, `obj`, or a computed broad directory.

## PanGloss Consumer Milestone

After the compilation graph and execution-boundary audit are trustworthy, the conformance tool publishes two checked-in, versioned products with schemas:

1. a semantic-domain inventory describing what HermitCrab can express and which execution facts remain constrained;
2. a normalized conformance manifest recording per-fixture grammar and words provenance and integrity (paths, hashes, case counts) over the canonical `words.yaml`/`grammar.xml` fixtures PanGloss reads directly and runs as tests.

The corpus replaces legacy example corpora such as Sena as the organizing cross-engine correctness suite. Historical corpora may remain independent regression data, but PanGloss conformance is driven by the synthetic `conformance/languages/` grammars and focused `conformance/edge-cases/` grammars whose migration ledger preserves the legacy coverage floor.

This milestone intentionally precedes completion of every semantic classification, obligation, evidence, and proof. It unblocks PanGloss without overstating conformance completeness. The final conformance gate still requires zero unclassified surfaces and complete evidence/proofs.

The production-consumer files are:

- `conformance/generated/hc-semantic-inventory.v1.json`;
- `conformance/schema/semantic-inventory.schema.json`;
- `conformance/generated/hc-conformance-manifest.v1.json`;
- `conformance/schema/conformance-manifest.schema.json`.

The authoring and reference formats also have explicit contracts:

- `conformance/schema/words.schema.json` describes `words.yaml` as `hc-conformance-words/v1`; the strict YAML loader accepts the JSON-Schema data model plus the documented plain-YAML restriction (no aliases, anchors, merge keys, or custom tags);
- every `grammar.xml` is labeled externally as `sil-machine-hermit-crab-input-xml/v1` and validates against the published `conformance/HermitCrabInput.dtd`. The library keeps its own copy at `src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd` because `XmlLanguageLoader` reads it as an embedded resource by manifest name; a test holds the two byte-identical. The manifest entry records each grammar's canonical relative path plus XML SHA-256, and the published DTD's own path and SHA-256 at the top level.

Format identifiers are opaque exact strings. A consumer supports a format only by explicitly accepting that identifier. Backward-compatible additions retain the identifier; any incompatible structural or semantic change introduces `/v2` files and schemas. The generator never rewrites existing `/v1` files into a different meaning.

The JSON root contains `formatVersion: "hc-semantic-inventory/v1"`, graph/source/toolchain hashes, ordered surfaces, ordered execution edges, and ordered `constraints`. Each constraint has a stable ID, kind (`unresolved` or `blocked`), a nonempty `affectedFacts` array of `{ "kind": "surface" | "edge", "id": "..." }`, a nonempty explicit list of the four configuration IDs to which it applies, and a reason. Wildcards and an empty configuration list are forbidden. Every affected fact must resolve exactly once in the same inventory.

A PanGloss candidate path declares its required fact pairs and configuration. It is excluded exactly when a constraint with the same configuration contains an affected fact pair equal to one of those required pairs. Constraints do not imply anything about facts they do not name. The conformance runner, MSBuild adapter, and Roslyn graph are never packaged into production.

The manifest root contains:

```json
{
  "formatVersion": "hc-conformance-manifest/v1",
  "wordsAuthoringFormatVersion": "hc-conformance-words/v1",
  "grammarFormatVersion": "sil-machine-hermit-crab-input-xml/v1",
  "dtdPath": "conformance/HermitCrabInput.dtd",
  "dtdSha256": "<sha256>",
  "sourceHash": "<sha256>",
  "fixtures": []
}
```

All paths are canonical repository-relative paths using `/`; they are never relative to the manifest itself. The manifest is provenance and integrity only — it carries no case data. Each fixture entry has a stable `fixtureId` equal to its directory (`languages/<name>` or `edge-cases/<name>`), `category` (`language` or `edge-case`, derived from that directory), `displayLanguage` (`words.yaml`'s `language`), `grammarPath` and `grammarSha256`, `wordsPath` and `wordsSha256`, `caseCount` (the word count in `words.yaml`), and `expectedCrash`. A fixture whose `expectedCrash` is `true` must have `caseCount: 1`. No fixture directory may contain a second grammar or words authority.

The schema is Draft 2020-12 JSON Schema, uses `additionalProperties: false` on every object, and declares required fields, scalar types, the crash-cardinality constraint above, and the SHA-256/path patterns. `fixtures` is ordinal by `fixtureId`; nothing else in the manifest is order-sensitive.

The manifest does not restate case data, so there is no field-by-field mapping from `words.yaml` cases into it — a consumer reads `words.yaml` directly for inputs, parses, rejections, skips, and `neutralizes`. `words.schema.json` remains authoritative for that data model: top-level `language` and a nonempty `words` array are required; every word requires nonempty `word` and `note`; ordinary words require nonempty `parses`; rejected/skipped/crash cases forbid parses; every parse requires nonempty `signature` and a `rules` array, which may be empty. The existing strict-loader rules for mutual exclusion and field dependencies are expressed with schema `oneOf`/`if`/`then` constraints. `words.schema.json` allows `neutralizes` on a word regardless of whether it parses, is rejected, or is skipped, because the authored fixtures put it on all three reachable outcomes: measured across the 28 checked-in fixtures, 15 occurrences are on rejections, 8 on parsing words, and 2 on skips. `blocked_by` genuinely is rejection-only (69 of 69), and the schema constrains it that way. Restricting `neutralizes` the same way `blocked_by` is restricted would silently exclude 10 of 25 observations, which is the same inheritance failure the coverage gates elsewhere in this repository exist to prevent.

The manifest records provenance, not a normalization of `words.yaml`'s content, and it does not interpret or duplicate the grammar either. `grammar.xml` remains the authoritative HC grammar representation and `words.yaml` the authoritative case data; PanGloss reads both directly rather than through a generated intermediate. For XML validation, the only admitted external subset is the exact system identifier `HermitCrabInput.dtd`; the validating resolver maps it to the published `conformance/HermitCrabInput.dtd` path the manifest records, regardless of fixture directory. Any other external entity, network access, or filesystem fallback is forbidden. Existing XML files retain their DOCTYPE unchanged. PanGloss applies the same resolver rule and owns its importer from `sil-machine-hermit-crab-input-xml/v1` and `hc-conformance-words/v1` into its internal model, plus its importer for the manifest.

### Deferred data-only NuGet distribution

After the checked-in products pass the published-tool, cross-platform byte-identity, schema, XML, and independent-review gates, Machine CI packs them as `SIL.Machine.Morphology.HermitCrab.Conformance.Data`. This is a distribution envelope, not a new source authority and not a reason to move the producer out of Machine.

The package is content-only. Under `contentFiles/any/any/hc-conformance/` it contains the two generated JSON products, the three versioned schemas, the authoritative fixture `grammar.xml` and `words.yaml` files, the pinned `HermitCrabInput.dtd`, `coverage.csv`, `rules.csv`, `README.md`, and `PROTOCOL.md`. It contains no `lib/`, `ref/`, `runtimes/`, `tools/`, `build/`, or `buildTransitive/` assets; no DLL, executable, PDB, Roslyn/MSBuild implementation, or package dependency is admitted.

The NuGet package version identifies an immutable release and is independent of the embedded format identifiers. Compatible content updates may retain `/v1`; an incompatible schema or interpretation change requires new `/v2` format identifiers and product names. The NuGet repository metadata records the exact Machine commit, while the packaged products retain their canonical source, graph, toolchain, DTD, and corpus hashes. PanGloss downloads and unpacks the `.nupkg` as a ZIP and validates the embedded schemas/products; it does not add a .NET runtime dependency.

Package creation runs only after in-memory generation and byte-exact `check`. A package inspection test enforces the exact entry whitelist, rejects executable/build assets and dependencies, revalidates the packaged products and XML/DTD references, and proves the package bytes are reproducible for the same version and source commit. Existing tag publication publishes this package alongside Machine's normal NuGet packages; non-tag CI uploads it only as a verification artifact.

`hc-conformance --semantic-coverage generate` writes both canonical JSON products and regenerates `conformance/coverage.csv` and `conformance/rules.csv` from the same validated in-memory fixture model before writing any artifact. It rejects duplicate display-language keys as well as duplicate fixture/case IDs or inputs. `hc-conformance --semantic-coverage check` performs generation into memory, requires the checked-in JSON and CSV bytes to match, validates all three JSON schemas, validates every XML grammar against the pinned DTD using the resolver above, audits boundary exactness and fixture referential integrity, and exits nonzero on drift.

Migration parity is computed directly inside the corpus generator from the validated fixture model, `constructs.txt`, and the migration ledger; it does not read `coverage.csv`. The proof enforces exact provenance carry-forward, per-construct coverage, the phonology-free/XAmple floor, and the permanent absolute construct floor. `conformance/parity-check.py` remains an independent cross-check and is run only after the byte-fresh CSV files have been established. The corpus source hash includes every contributing grammar, word file, schema, DTD, `constructs.txt`, migration ledger, and parity checker.

For this milestone, a trustworthy execution-boundary audit means: every emitted edge appears exactly once; every owned edge expands to an owned definition; every external or profile edge has exactly one live catalog disposition; no catalog row is stale or duplicated; every dispatch and callback target set matches; and every edge not closed by a reviewed disposition appears as an explicit consumer constraint. Unreviewed, silently omitted, or stale facts are forbidden. Explicit blocked or open facts are permitted only as constraints.

## Rejected Alternatives

### `MSBuildWorkspace` and `Microsoft.Build.Locator`

This can model projects accurately, but it adds Workspaces build-host deployment, MSBuild registration/global state, assembly-loading/version coupling, and another partial-load diagnostic channel. Those costs are unnecessary for a fixed four-project graph and make the published tool harder to deploy reproducibly.

### Direct in-process Microsoft.Build evaluation

This retains package and MSBuild-version coupling, introduces process-global state and load-context hazards, and still requires targets to obtain compiler-ready references and generated inputs. It provides less isolation without simplifying the authority model.

### Repository-specific `.csproj` parsing

SDK-style projects derive compile globs, implicit sources, framework defines, conditional items, and resolved references from imported SDK and NuGet targets. Reimplementing that behavior would create a second, incomplete build system and cannot support the completeness claim.

## Acceptance Criteria

1. The graph contains exactly the four permitted project/TFM nodes and expected edges; extra, missing, cyclic, outside-root, or ambiguous nodes fail.
2. All four conditional-symbol profiles retain the evaluated Release defines and provenance.
3. Final compiler sources include SDK-generated assembly-info and target-framework inputs, classified as generated support.
4. Netstandard projects use resolved netstandard reference assemblies; no runtime/TPA approximation remains.
5. No owned output DLL is read; every owned reference becomes a `CompilationReference`.
6. Cross-project tests prove Machine rule symbols, HC inheritance, Tool calls, and Conformance's executable project reference resolve to owned definitions.
7. Every syntax or semantic compiler error in owned or generated source fails graph construction with a canonical location. No path, scope, or diagnostic-code exemption exists.
8. Unknown compiler arguments, incompatible Roslyn/SDK versions, implicit global usings not yet modeled, and non-admitted source generators fail explicitly; admitted SDK reference-pack generators must prove zero output and diagnostics.
9. Missing or stale assets fail without network access or an implicit restore.
10. A synthetic multi-targeted project rejects ambiguity and accepts only an explicit compatible selection.
11. Source, import, reference, analyzer, compiler, and loader changes alter the appropriate hash; relocation, path separators, and line endings do not.
12. Windows and Linux CI produce identical authoritative graph and inventory hashes under the pinned toolchain.
13. Querying leaves tracked files and repository `bin`/`obj` unchanged; generated query artifacts remain in private temporary storage.
14. Traversal, reparse/symlink escape, case collision, shell injection, oversized output, timeout, and process-tree termination tests pass.
15. A published-tool smoke test proves the capture target and matching Roslyn dependencies deploy correctly and SDK selection begins at the repository root.
16. The repository-path regression `RepositorySourcePathCannotBypassSemanticCompilationFailure` passes after all exemptions are removed.
17. Versioned, schema-valid, deterministic semantic-inventory and conformance-corpus products are generated and checked in before PanGloss handoff; every XML grammar and `words.yaml` authoring file validates against its labeled format contract.

## Verification Evidence

Acceptance is recorded by named test fixtures and commands rather than an informal report:

- `DotnetMsbuildCompilationGraphLoaderTests`: protocol parsing, exact node/profile matrix, compiler arguments, generated support, assets, unsupported generators/usings, diagnostics, process limits, and containment;
- `RepositoryCompilationGraphTests`: topology, owned-reference replacement, cross-project symbol bridge, canonical keys, and hash sensitivity and invariance;
- `RepositorySourceSnapshotTests`: live four-project composition, zero compiler errors, stable hashes, unchanged repository outputs, and the repository-path regression;
- `SemanticInventoryArtifactTests`: schema validation, canonical serialization, byte-for-byte generation/check, constraint semantics, and boundary exactness;
- `ConformanceCorpusArtifactTests`: words-schema validation, XML/DTD validation, exact format labels, normalized case outcomes, reference integrity, source hashing, path containment, and byte-for-byte generation/check;
- focused local gate: `dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests/SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore --filter "FullyQualifiedName~CompilationGraph|FullyQualifiedName~RepositorySourceSnapshot|FullyQualifiedName~ExecutionClosure|FullyQualifiedName~SemanticInventoryArtifact|FullyQualifiedName~ConformanceCorpusArtifact"`;
- published-tool smoke gate: publish Conformance and invoke the published `hc-conformance --semantic-coverage check` from the repository root;
- cross-platform CI gate: Windows and Linux jobs upload both generated products plus graph/source/toolchain/corpus hashes, followed by a comparison job that requires byte identity.

The slice is complete only when all seventeen acceptance criteria pass, both CLI modes operate on the checked-in files above, the published-tool and cross-platform gates pass, and independent spec and quality reviews have no open critical or important findings.

## Non-Goals

- Retargeting `SIL.Machine` or HermitCrab from `netstandard2.0`.
- Shipping the conformance runner or compiler graph in an end product.
- Supporting arbitrary repositories or untrusted MSBuild projects.
- Supporting generated syntax trees or executing arbitrary/custom source generators in the first graph-loader version.
- Completing catalog classification, obligation generation, typed evidence, or proof coverage in this architecture slice.
- Exposing Roslyn or MSBuild types to PanGloss.
