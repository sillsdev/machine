# Parser settings variants

A fixture can run the same grammar and ordered words under named `variants`. Each run has its own non-empty `morpher` block and its own complete `words` expectations. Omitted settings take engine defaults; variants do not inherit the default run's overrides. Full signature multisets and statuses are compared. Variant rows do not add semantic-coverage credit.

The following observations were collected from C# HC on 2026-10-22, using commit `1836d23ccb86f1e9da123a43feb661ea76e85d0b` (harness draft atop PR #480 head `72dbd4f9bd686d1193d11cf8f908d50ffa878ba1`; the HC engine is unchanged), with `maxDegreeOfParallelism: 1`. The HC assembly SHA-256 was `2547068882BEC56873F0678922D11D7F81ED4D68B3B99DCDB71EBF5863714444`. Expected signatures were transcribed from actual `Morpher.ParseWord` results, preserving duplicates.

| Setting | Fixture and witness | Observed result |
| --- | --- | --- |
| `DelReapps` | `edge-cases/deletion-reapplications`, `tuki` | 0: `TUKI`; 1: `TUKI`, `TUIIKI`, `TUIIIKI`; 2 adds `TUIIIIKI`. Every signature ends in `|tuki`. |
| `MaxRoots` | `edge-cases/compounding-breadth`, `katu` | 1: zero analyses; 2: `KA+TU|katu`. Bare `ka` and `tu` remain controls. |
| `MakePartialsFinal` | `edge-cases/final-template-partial-discriminators`, `pilfbvb` | false: `PIL+FINTAGB+GATE2PARTIAL|pilfbvb`; true: zero analyses. `pilfb` still parses. |
| `MergeAnalyses` | `edge-cases/final-template-merge-key`, `sagdss` | Both values: exactly `SAG+FINAL+TEMPLATE+NOM|sagdss`. This checks output preservation; it does not measure merge-fold counts. |
| `GuessRoots` | `edge-cases/guess-roots-settings`, `takuvuq` | false: zero analyses; true: two `taku+INFL|takuvuq` plus two `takuvuq|takuvuq`. `nuna` retains its lexical analysis, and `!` remains `SKIPPED`. |

`GuessRoots` is a per-call argument, rather than a Morpher property. Its explicit presence permits adapter-mode guessing; legacy fixtures containing `guess: true` without that explicit setting remain self-check-only for those words.

`MaxAlternatives` and `MergeMSAs` are not supported by this XML-fixture settings channel. PanGloss's work cap does not implement HC's typed `MaxAlternativesExceededException`, and `MergeMSAs` is a FieldWorks project-loader option. Both names are rejected instead of being silently ignored. A `.fwdata` loader comparison remains separate work; these XML runs do not demonstrate it.

See [PROTOCOL.md](../PROTOCOL.md) for the authoring and adapter contracts.

Validation observed on 2026-10-22:

- C# strict local check: Release build and all project tests passed; HermitCrab 634 passed, SIL.Machine 887 passed / 3 skipped, Thot 93 passed, SentencePiece 4 passed. CSharpier also checked the 23 changed C# files explicitly.
- C# self-check: all 72 runs across 62 fixtures passed, including pathological fixtures.
- Selected option matrix: five fixtures, each with a default and two variants; all 15 runs / 81 word-run rows passed in C# HC and in the live PanGloss external adapter. Each side compared complete signature multisets and statuses.
- PanGloss harness package: 53 tests passed, including malformed settings/expectations, deliberately wrong named expectations, and a capped parse that must not pass as a completed zero-analysis result.

Reproduce the full C# self-check from the Machine checkout:

```powershell
dotnet run -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --include-pathological
```

For the option matrix, copy only the five fixture directories named above under a temporary `edge-cases/` root, preserving each `grammar.xml` and `words.yaml`, and pass that root with `--fixtures`. Run it once as self-check, then with `--capabilities phonology,morpher-settings --adapter <command-template>`. The PanGloss `pg-conformance-adapter` accepts `--grammar {grammar} --words {words} --output {output} --morpher {morpher}`; its settings JSON is supplied separately for every materialized run. PanGloss's managed adapter launch instructions are in `docs/conformance/parser-settings-variants.md` in that repository.
