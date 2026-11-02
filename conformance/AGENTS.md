# Updating the conformance suite

This file travels with `conformance/`. It says how to change the suite and what to run before the change
leaves your machine. Commands run from the root of the repository that holds the engine and the
`src/SIL.Machine.Morphology.HermitCrab.Conformance` project. `<hc>` below means
`dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -c Release --`.

## 1. The rule

The suite covers only grammars a real FieldWorks project can produce. FieldWorks builds every grammar
through `HCLoader.cs`, and that loader reaches less than the HermitCrab XML can say. A grammar that needs
a construct the loader never writes does not belong here, however expressible it is in the XML and
however well the engine handles it.

- Before you author, change or keep a fixture, check each construct it uses against the table in
  [README.md](README.md#what-the-xml-can-say-but-fieldworks-cannot-so-we-do-not-test-it) and against
  [PROTOCOL.md](PROTOCOL.md) section 9.
- If the grammar needs a construct in that table, stop. Rewrite the grammar without it or do not add it.
  Do not relax `fieldworks_producible`; `ConformanceFixtureGateTests` fails on any fixture that is not `true`.
- Do not author a fixture to close a `todo` line in `semantic-coverage-baseline.txt` or a row in
  `semantic-coverage-gap-baseline.tsv` if only a construct in that table could close it.

## 2. Adding, changing or deleting a fixture

A fixture is a directory under `languages/` or `edge-cases/` holding `grammar.xml` and `words.yaml`, and
optionally a `README.md`. [docs/what-it-is.md](docs/what-it-is.md) and the README section "Consuming the
suite from another repository" describe the files; `schema/words.schema.json` and `HermitCrabInput.dtd`
validate them.

1. Check producibility (section 1). Name the `HCLoader.cs` function that writes each construct you rely on.
2. Write `grammar.xml` and `words.yaml`. Put `fieldworks_producible: true` directly after `requires`.
   Give each word a `note` that says what it pins.
3. Take the expected rows from the oracle, never from memory. Run `<hc> --fixtures conformance
   --include-pathological --propose`: on a mismatch it prints the `words.yaml` patch and writes nothing.
   Read every row against the rules before you accept it. A row the oracle produces and you cannot explain
   is a finding, not a pin.
4. Update [docs/grammars.md](docs/grammars.md): one bullet per fixture, in the group it belongs to, in the
   form `` - `edge-cases/name`: one plain line. `` A test fails if a fixture is missing, listed twice or gone.
5. If you ruled a construct out, add a row to the README section named in section 1 and, when the loader
   verdict changed, edit `tools/generate-fieldworks-producibility.ps1` and regenerate it
   (`pwsh conformance/tools/generate-fieldworks-producibility.ps1`). Cite the `HCLoader.cs` function and
   line, and the FieldWorks commit you read. If you delete a fixture for this reason, add its row to the
   table in [docs/engine-only-fixture-retirement.md](docs/engine-only-fixture-retirement.md).
6. Regenerate every file the change touches (section 2a), then read the `git diff`. A ledger that moved
   because of your change is expected; one that moved for another reason is not.
7. Delete a fixture by removing its directory, its line in `docs/grammars.md`, then regenerating. Remove a
   `dead-rules-allowed.tsv` or `semantic-coverage-presence-waivers.txt` line that names it.

### 2a. Generated files

[docs/how-it-works.md](docs/how-it-works.md) lists every generated file with its gate and cost. Each
`--write-*` flag rewrites the file; the bare flag checks it and exits nonzero if stale. Regenerate in this
order, with `--fixtures conformance` on each. The flags that read other ledgers find the repository root from
the current directory; pass `--repository-root <path>` if you run from elsewhere:

```
<hc> --fixtures conformance --generate-manifest
<hc> --fixtures conformance --coverage-report
<hc> --fixtures conformance --write-coverage-baseline
<hc> --fixtures conformance --write-interface-inventory
<hc> --fixtures conformance --write-rule-interaction-pairs
<hc> --fixtures conformance --write-interaction-chains
<hc> --fixtures conformance --write-dataflow-obligations
<hc> --fixtures conformance --write-engine-gate-inventory
<hc> --fixtures conformance --write-gate-obligations
<hc> --fixtures conformance --write-counterfactual
<hc> --fixtures conformance --write-coverage-evidence
<hc> --fixtures conformance --write-coverage-traceability
<hc> --fixtures conformance --write-evidence-cards
```

- `--write-counterfactual` must run before `--write-coverage-evidence`; the second reads the first off disk.
- `--write-coverage-traceability` writes four files: `interface-witness.tsv`,
  `grammar-coverage-ledger.tsv`, `construct-claim-corroboration.tsv` and `fold-in-candidates.tsv`.
- `--generate-manifest` hashes only `grammar.xml` and `words.yaml`. Editing a fixture `README.md`, any file
  under `docs/`, or this file changes no generated file.
- The three commands that re-parse every fixture (`--write-counterfactual`, `--write-coverage-evidence`,
  `--write-coverage-traceability`) take tens of minutes. Skip them when only documentation changed.

### 2b. Hand-edited files with no generator

Edit these by hand; the gate named beside each fails if you forget.

| File | Change it when | Gate |
|---|---|---|
| `semantic-coverage-gap-baseline.tsv` | evidence or a valid proof closes a row (delete the row). Never add a row to hide a gap; a new or changed gap needs review. | `CoverageGapRatchetTests` |
| `dead-rules-allowed.tsv` | a fixture declares a rule on purpose that no word exercises (add a row with the reason), or a word now exercises an allowed rule (delete the row). | `--coverage-report` exits 1 |
| `semantic-coverage-proofs.tsv` | a surface cannot be evidenced and you can name why. The gate recomputes the proof; do not write one it will reject. | `--counterfactual` |
| `semantic-coverage-presence-waivers.txt` | a surface is knowingly counted on presence alone, or a fixed line must go. | `--write-coverage-baseline` |
| `semantic-catalog.yaml` | the curated inventory scope changes. `--propose-semantic-catalog` prints a proposal and writes nothing. | `--semantic-coverage` |
| `constructs.txt` | the checklist of constructs gains or loses an entry. | `--coverage-report` |

## 3. Optional FieldWorks witnesses

A `fieldworks_producible: true` fixture may carry a `fieldworks/` directory with a real FieldWorks project.
[PROTOCOL.md](PROTOCOL.md) section 10 defines it. This repository stores witnesses and checks their shape;
it never opens one. The PanGloss `xample-projector` tool produces and reads them.

- Add one only when a real project backs the claim. Do not hand-edit `project.fwdata`.
- The directory holds exactly `project.fwdata`, `phonology-mutations.yaml` and `WritingSystemStore/`, and
  `WritingSystemStore/` holds only `.ldml` files.
- Keep each `.ldml` minimal: only the elements that define the writing system (`identity`, `layout`,
  `characters`, `delimiters`, `collations`, `special`). Remove the CLDR locale data LibLCM copies in
  (dates, numbers, units, display names); nothing reads it.
- `phonology-mutations.yaml` carries `version: 1` and a `base_sha256` equal to the SHA-256 of
  `project.fwdata`. Recompute it whenever the project changes. `ConformanceFixtureGateTests` checks it.
- A `*.fwdata` file may be excluded by your ignore rules; add it with `git add -f`.

## 4. Before you push

Run each command from the repository root and read the output. Do not push on a partial run.

```
pwsh ./scripts/comment-hygiene.ps1
dotnet csharpier check .
dotnet build tests/SIL.Machine.Morphology.HermitCrab.Tests -c Release
<hc> --fixtures conformance --include-pathological
<hc> --fixtures conformance --check-manifest
<hc> --fixtures conformance --coverage-report
git diff --exit-code -- conformance/coverage.csv conformance/rules.csv
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests -c Release
```

- `./local_check.sh --agent-strict` runs the repository-wide version of the same checks. The conformance
  workflow runs the self-check, the coverage report and the `git diff` check.
- The full HermitCrab test project takes about 14 minutes. `[Explicit]` tests do not run in it.
- If you changed `grammar.xml`, `words.yaml`, the DTD or engine behaviour, run the three ledger freshness
  tests below. Each is `[Explicit]`, so a filter by name runs it:

  ```
  dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests -c Release --no-build --filter "FullyQualifiedName~<name>"
  ```

  | `<name>` | Takes (measured) | Needed because |
  |---|---|---|
  | `InterfaceWitnessLedgerTests.TheCheckedInWitnessLedgerMatchesAFreshRecomputeExactly` | 3 to 5 minutes | the severance sweep behind `interface-witness.tsv` and the three ledgers joined from it reads every fixture |
  | `CounterfactualGateTests.TheCheckedInLedgerMatchesAFreshRecomputeExactly` | 40 to 45 minutes | `semantic-coverage-counterfactuals.tsv` records what happens when each surface is neutralized in each fixture |
  | `EvidenceLedgerFreshnessTests.TheCheckedInEvidenceLedgerMatchesAFreshRecomputeExactly` | 45 to 50 minutes | `semantic-coverage-evidence.tsv` combines the counterfactual ledger with an ordering sweep over every fixture |

  Run them one at a time. If you changed only documentation, `grammars.md`, `README.md` or a hand-edited file
  in section 2b, the generated ledgers cannot have moved; say that instead of running them.
