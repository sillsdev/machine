# FieldWorks HermitCrab out-of-process Server-GC worker — design

> Status: design, approved direction (Option 1 of 3 candidates), not yet implemented.
> Companion to `RUSTIFY.md` (which measured the underlying GC-mode ceiling) and
> `RUSTIFY-stage3-design.md`. This spans two repos: `machine` (this repo) and `FieldWorks`.

## 1. Problem & goal

Measured this session (`RUSTIFY.md` §"multithreaded scaling"): FieldWorks' "Parse All Words" bulk
HermitCrab parsing is capped at **~3–4× scaling under Workstation GC**, regardless of how much
in-process allocation work is done (`hc-rustify`'s allocation cuts already raised the *floor* at every
thread count, but not the *ceiling* — Gen2 is a single-collector-thread, stop-the-world bottleneck under
Workstation GC, confirmed by Gen2 counts climbing from 0 at low thread counts to several at dop=8/16).
Under **Server GC**, the same code and machine reach **10–12×**. FieldWorks itself is **net48** and is a
long-running interactive WinForms process — Server GC is a process-wide CLR setting fixed at startup, so
FieldWorks.exe cannot mix Workstation GC for its own UI and Server GC for parsing in the same process.

**Goal:** get Server-GC-level scaling for all HermitCrab parsing FieldWorks does (bulk "Parse All Words"
*and* interactive single-word parsing / Try-a-Word tracing) without changing FieldWorks.exe's own GC mode
and without a Rust rewrite.

**Existing FieldWorks-side evidence this is a known, already-managed problem:**
`Src\LexText\ParserCore\ParserWorker.cs`, `ParseAndUpdateWordforms` (lines 181–229), caps
`MaxDegreeOfParallelism` at `Environment.ProcessorCount - 1` (max 4) with a comment noting HC bulk parsing
only scales ~2.4–2.8× due to a ceiling "inside the HermitCrab morpher (shared compiled grammar /
allocation-bound)" and explicitly leaving cores free so the UI stays responsive during the run. That
comment is the team's own prior acknowledgment of exactly the ceiling this design fixes.

## 2. Chosen architecture (Option 1): single persistent Server-GC worker process

```
FieldWorks.exe (net48, Workstation GC)              HCWorker.exe (net48, Server GC — new project)
┌────────────────────────────────────┐               ┌───────────────────────────────────────┐
│ Src\LexText\ParserCore\HCParser.cs  │   WCF over    │  Hosts ONE SIL.Machine.Morphology.     │
│  ParseWord/GetTraceMorpher callers  │   net.pipe    │  HermitCrab.Morpher instance            │
│    ↓ (was: new Morpher(...) direct) │  (named pipe, │  app.config: <gcServer enabled="true"/> │
│  IHCWorkerClient (new, thin proxy)  │──────────────▶│  ServiceHost, same NetNamedPipeBinding   │
│                                     │               │  pattern as LexicalProviderManager.cs    │
│ Src\...\ParserWorker.cs             │               │  Internally parallel: Morpher's own      │
│  ParseAndUpdateWordforms            │──batch call──▶│  MaxDegreeOfParallelism (default =       │
│                                     │               │  ProcessorCount, no artificial cap —     │
│ HCWorkerProcessManager (new)        │  spawn +      │  the cap in ParserWorker.cs today exists │
│  spawn/watchdog, modeled on         │  watchdog     │  only to protect Workstation-GC UI       │
│  FLExBridgeHelper.cs                │◀─────────────▶│  responsiveness, which no longer applies │
└────────────────────────────────────┘               │  once parsing lives in its own process)  │
                                                       └───────────────────────────────────────┘
```

- **`machine` repo:** one new net48 console/worker project (`SIL.Machine.Morphology.HermitCrab.Worker` —
  a spiritual rebuild of the removed `SIL.Machine.Morphology.HermitCrab.Server`, net48 this time instead
  of net10, so it ships **no second runtime** — it reuses the .NET Framework FieldWorks already has
  installed). References `SIL.Machine.Morphology.HermitCrab` exactly as `ParserCore.csproj` does today.
  **No changes needed to `SIL.Machine`/`SIL.Machine.Morphology.HermitCrab` themselves** — they already
  target `netstandard2.0` and are net48-consumable as-is (confirmed: a vestigial `net461` conditional
  `ItemGroup` still exists in `SIL.Machine.csproj`, evidence the library multi-targeted .NET Framework
  directly before).
- **`FieldWorks` repo:** a WCF client proxy with the same call shape `HCParser.cs` already uses (minimizes
  churn), plus a process lifecycle manager modeled on `FLExBridgeHelper.cs`'s spawn+watchdog pattern.
  `ParserWorker.cs`'s existing `MaxDegreeOfParallelism` cap and its UI-responsiveness comment become
  irrelevant to *parsing* (that work now happens in a separate process) — FieldWorks' own thread stays
  free for the UI regardless of how hard the worker parses.

## 3. WCF service contract

One contract, covering both the bulk and interactive paths (per the "both bulk and single-word" scope
decision), hosted by the worker, consumed by FieldWorks' new client proxy:

```csharp
[ServiceContract]
public interface IHCWorkerService
{
    // Rebuilds the worker's Morpher for a grammar. Called on FieldWorks startup / whenever the
    // compiled HC grammar changes (mirrors HCParser.LoadParser's existing trigger conditions).
    [OperationContract]
    void UpdateGrammar(string compiledGrammarXml, int deletionReapplications);

    // Single-word interactive path (was: m_morpher.ParseWord(word, out _, guessRoots) in HCParser.cs:115).
    [OperationContract]
    WordAnalysisDto[] ParseWord(string word, bool guessRoots);

    // Bulk path (was: m_parser.ParseWord(word) per-word inside ParserWorker.ParseAndUpdateWordform).
    // One round trip for the whole batch — this is the call that benefits from the worker's internal
    // parallelism; FieldWorks no longer needs its own Parallel.ForEach over words at all.
    [OperationContract]
    IDictionary<string, WordAnalysisDto[]> ParseWordsBatch(string[] words);

    // Try-a-Word tracing path (was: GetTraceMorpher().ParseWord / ParseToXml in HCParser.cs).
    [OperationContract]
    string TraceWord(string word); // trace XML, same shape HCParser.ParseToXml already produces
}
```

`WordAnalysisDto` is a flat, serializable projection of `SIL.Machine.Morphology.WordAnalysis`
(`Category`, `RootMorphemeIndex`, `Morphemes` as string IDs/glosses) — the same shape the
`RustifyBenchmark`/`CompareBench` harnesses already project down to for signature comparison, so this
mapping is already validated as sufficient to represent an analysis result.

## 4. Worker lifecycle

- **Start:** FieldWorks' `HCWorkerProcessManager` (new) spawns `HCWorker.exe` lazily on first HC parse
  request per session (not eagerly at FieldWorks startup — avoids paying the worker's memory/startup cost
  for users who never touch the parser), via `Process.Start` with `UseShellExecute = false`, matching
  `FLExBridgeHelper.cs`'s existing spawn pattern.
- **Ready signal:** worker opens its named pipe and blocks on `UpdateGrammar` before any parse call is
  valid; FieldWorks sends `UpdateGrammar` immediately after spawn, using the same compiled grammar XML it
  already produces for the in-process `Morpher` today.
- **Health / crash recovery:** a background watchdog thread (mirrors `FLExBridgeHelper.cs`'s
  `process.WaitForExit()` pattern) detects worker exit. On crash mid-batch: the in-flight
  `ParseWordsBatch`/`ParseWord` call fails with a `CommunicationException`; the client proxy respawns the
  worker, replays `UpdateGrammar`, and retries the failed call **once** before surfacing an error to the
  UI (bulk parse: retry just the failed chunk, not the whole run — see §6).
- **Shutdown:** FieldWorks kills the worker process on its own exit (or after an idle timeout, e.g. no
  parse calls for N minutes, to release the Server-GC memory footprint when the user is doing other work).
- **Versioning:** the worker's `SIL.Machine`/`SIL.Machine.Morphology.HermitCrab` package version must match
  FieldWorks' own (both already pinned via `$(SilMachineVersion)` in `Directory.Packages.props`) — the
  worker ships from the same NuGet feed/version as `ParserCore.csproj` references, so this is a normal
  package-version-bump concern, not a new one.

## 5. Data flow

**Bulk parse ("Parse All Words"):** `ParserListener.OnParseAllWords` → `ParserWorker.
ParseAndUpdateWordforms` builds the word list → **one** `ParseWordsBatch(words[])` WCF call (replacing
today's in-process `Parallel.ForEach` over `ParseAndUpdateWordform`) → worker parses the whole batch using
its own internal parallelism (Server GC, no artificial DOP cap) → single `IDictionary<string,
WordAnalysisDto[]>` response → FieldWorks applies results back into LCM under its existing read/write-lock
discipline (unchanged — `HCParser.ParseWord`'s existing comment already separates "the expensive, CPU-bound
part" from the LCM-locked part; that split is exactly the machine-vs-LCM boundary this design formalizes
across a process, not just a code seam).

**Interactive single-word parse / Try-a-Word:** `HCParser.ParseWord`/`GetTraceMorpher` call sites become
`ParseWord`/`TraceWord` WCF calls. Local named-pipe round-trip latency is sub-millisecond to low-single-
digit ms — negligible against existing UI interaction latency.

**Grammar change:** whenever `HCParser.LoadParser`'s existing trigger conditions fire today (grammar
edited, project reload), FieldWorks calls `UpdateGrammar` instead of constructing a new in-process
`Morpher`.

## 6. Error handling

- **Pipe unavailable / worker not yet started:** client proxy spawns the worker on first call if not
  already running (lazy start), so this is the normal cold-start path, not an error path.
- **Worker crash mid-`ParseWordsBatch`:** the whole batch call fails as a unit (WCF request/response, not
  streamed per-word) — on retry, FieldWorks resubmits the **same** word list (idempotent: parsing a word
  twice produces the same result, no side effects), not the whole "Parse All Words" run from scratch. If
  the batch is very large, consider chunking client-side (e.g. 500-word chunks) purely to bound the
  resubmission cost of a mid-batch crash, not for parallelism (the worker already parallelizes internally).
- **Grammar version mismatch** (FieldWorks upgraded, worker still running an old build): `UpdateGrammar`
  is idempotent and cheap enough to call defensively before every bulk run, not just on detected changes.
- **Worker leaks/hangs:** same watchdog pattern as FLExBridge — a `process.WaitForExit()` background
  thread with a timeout is the existing precedent for "don't let a stuck child process silently block FLEx."

## 7. Testing

- **Reuse `Src\Utilities\pcpatrflex\DisambiguateInFLExDB\DisambiguateInFLExDBTests\
  ParserConcurrencyBenchmark.cs`** (existing `[Explicit]` headless benchmark, loads an `LcmCache` from
  `FW_BENCH_FWDATA`) as the before/after harness — it already isolates exactly the "Parse All Words"
  workload this design targets, so before/after numbers are directly comparable to what the team already
  tracks.
- **New unit tests** for the WCF contract in isolation (worker up, `ParseWordsBatch` against a small test
  grammar, assert results match the existing in-process `Morpher.ParseWord` — the same signature-diff
  technique this session used to prove `hc-rustify` byte-identical against the pre-Stage-3 baseline).
- **Crash-recovery test:** kill the worker process mid-batch (via test hook), assert FieldWorks retries and
  completes.
- **Existing `HCParser.cs` diagnostic counters** (`DiagMorpherParseTicks`, `DiagGetMorphsTicks`, lines
  44–48) extend naturally to also record IPC round-trip time, isolating "worker parse time" from "IPC
  overhead" from "LCM lock time" in the new architecture — the same instrumentation discipline this whole
  session's allocation work relied on.

## 8. Rollout considerations (not full scope here — flagged for the implementation plan)

- **Installer/packaging:** `HCWorker.exe` + its dependency closure ship alongside FieldWorks' existing
  binaries; no new runtime, no new machine-wide install step (unlike the original net10 worker, which the
  doc's Phase 5 removal explicitly cited "~100 MB worker, .NET-10-runtime deployment" as a real cost this
  net48 design avoids).
- **Server GC memory footprint of the worker specifically** (not FieldWorks.exe) should be measured on a
  representative machine before rollout — default is one heap per logical core; `GCHeapCount` is available
  to cap it if the idle/working-set cost of a 12–20-heap worker process is unwelcome, independent of
  FieldWorks' own memory budget since it's a separate process.
- **Rollout can be incremental:** ship the worker + bulk-parse routing first (biggest, most measurable win,
  matches the existing benchmark), route interactive/Try-a-Word through it in a follow-up once the bulk
  path is proven in the field.

## 9. What does NOT change

- `SIL.Machine`/`SIL.Machine.Morphology.HermitCrab` themselves — no API changes required; they're consumed
  by the new worker exactly as `ParserCore.csproj` consumes them today.
- FieldWorks.exe's own GC mode — stays Workstation GC, unchanged, no UI-responsiveness risk introduced.
- The LCM read/write-lock discipline around parsing (`HCParser.ParseWord`'s existing comment already
  isolates "the expensive, CPU-bound part" — that boundary is exactly where the process split lands).
