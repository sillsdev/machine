# How it works: an operator's reference

Every regenerable file under `conformance/` in one place: what it holds, the exact command that
regenerates it, and which test gates it against drift. Commands are quoted verbatim from
`src/SIL.Machine.Morphology.HermitCrab.Conformance/Program.cs`'s own `--help` text and argument
parser — run `dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --help` to
get the same text live. Every command below takes `--fixtures conformance`
and (for the flags in the second table) an implicit `--repository-root` it discovers automatically
from the current directory; pass `--repository-root <path>` explicitly if you're running from
somewhere else.

Two command shapes, non-writing and writing, exist for every ledger below: the bare flag
(`--interface-inventory`) recomputes and diffs against the checked-in file, exiting nonzero if stale
— this is what a drift test runs. The `--write-*` variant recomputes and overwrites the checked-in
file. Regenerating never flips a run's own exit code to green: every one of these prints its
findings *before* writing, specifically so that regenerating a stale ledger can never be mistaken for
having fixed whatever made it stale.

## Coverage report (fast, no engine reference needed)

| file | regenerate | gated by |
|---|---|---|
| `coverage.csv` | `dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --coverage-report` | printed construct-coverage check in the same run (fails the run if any in-scope `constructs.txt` entry has zero coverage) |
| `rules.csv` | same command (both are written together) | printed dead-rule check in the same run (fails if any `grammar.xml` rule id is exercised by zero words) |

`coverage.csv` is one row per (language, word, construct); `rules.csv` is one row per (language, rule
id, exercising words) — a rule with an empty `words` column is a dead rule.

## Semantic coverage (the four layers of `how-it-is-computed.md`)

| file | regenerate | gated by | cost |
|---|---|---|---|
| `semantic-coverage-baseline.txt` | `--write-coverage-baseline` | `--semantic-coverage` (no `[Explicit]` — cheap: reads the DTD and every `grammar.xml`, no re-parsing) | cheap |
| `semantic-coverage-counterfactuals.tsv` | `--write-counterfactual` | `--counterfactual`, and pinned against drift by `CounterfactualGateTests.cs` | **expensive** — re-parses every fixture once per surface it declares; the `[Explicit("re-parses every fixture once per surface")]` test is not part of a default run |
| `semantic-coverage-evidence.tsv` | `--write-coverage-evidence` | `--coverage-evidence`, pinned by `EvidenceLedgerFreshnessTests.cs` | **expensive** — `[Explicit("re-parses every fixture once per Surface item plus once per Ordering adjacent pair")]`; always run `--write-counterfactual` first, since this command reads the Surface ledger back off disk rather than recomputing it |
| `interface-inventory.tsv` | `--write-interface-inventory` | `--interface-inventory`, pinned by `InterfaceInventoryLedgerTests.cs` | cheap — resolves declared IDREFs against the corpus, no re-parsing |
| `interface-witness.tsv` | `--write-coverage-traceability` (see below — bundled) | `--coverage-traceability`, pinned by `InterfaceWitnessLedgerTests.cs` | **expensive** — `[Explicit("re-parses every present interface x fixture pair; ~2-3 minutes")]`, one severance re-parse per present interface × fixture |
| `interaction-chains.tsv` | `--write-interaction-chains` | `--interaction-chains`, pinned by `InteractionChainLedgerTests.cs` | cheap — a static co-occurrence/junction computation over `interface-inventory.tsv` and the DTD, no re-parsing |
| `dataflow-obligations.tsv` | `--write-dataflow-obligations` | `--dataflow-obligations`, pinned by `DataflowObligationLedgerTests.cs` | cheap — derived from `interaction-chains.tsv`, no re-parsing |
| `construct-claim-corroboration.tsv` | `--write-coverage-traceability` (bundled) | `--coverage-traceability`, pinned by `ConstructClaimCorroborationTests.cs` | cheap — a structural join against `interface-witness.tsv`, already computed; the test itself notes "no reparse, so this runs on every pass rather than needing `[Explicit]`" |
| `grammar-coverage-ledger.tsv` | `--write-coverage-traceability` (bundled) | `--coverage-traceability`, pinned by `GrammarCoverageLedgerTests.cs` | cheap — joins three already-checked-in ledgers, no re-parse of its own |
| `fold-in-candidates.tsv` | `--write-coverage-traceability` (bundled) | `--coverage-traceability`, pinned by `FoldInCandidateLedgerTests.cs` | cheap — reads `semantic-coverage-counterfactuals.tsv` back rather than reparsing |
| `evidence-cards/*.md`, `evidence-cards/index.tsv` | `--write-evidence-cards` | `--evidence-cards` | cheap — purely a rendering of already-checked-in ledger facts; never recomputes anything itself, so its only failure mode is going stale relative to what it renders |
| `generated/hc-conformance-manifest.v1.json` | `--generate-manifest` | `--check-manifest`, pinned by `ConformanceManifestTests.cs` | cheap — hashes and validates every fixture, no engine parsing |

**`--write-coverage-traceability` writes four files in one invocation**, and its non-writing form
(`--coverage-traceability`) is the one expensive step in this group: it re-sweeps
`interface-witness.tsv` (the severance sweep above) and then performs three cheap joins on top of the
freshly swept result — `grammar-coverage-ledger.tsv`, `construct-claim-corroboration.tsv`, and
`fold-in-candidates.tsv`. Because even the *check* (non-writing) path re-runs the severance sweep,
this whole command is, in `Program.cs`'s own words, "a deliberate, occasional command — never part of
ordinary CI — not a cheap drift check." Run it by hand when you need those four files current, not as
a routine gate.

**`rule-interaction-pairs.tsv`** (`--write-rule-interaction-pairs`, checked by
`--rule-interaction-pairs`) is cheap to regenerate — a structural enumeration of pipeline-permitted
rule pairs per stratum, no re-parsing — but is **not** one of the four coverage layers and is not a
coverage denominator at all; see `how-it-is-computed.md` and `decisions-and-lessons.md` for why. It
exists purely as a per-grammar pruning tool (which adjacent rule pairs are even worth a counterfactual
run), and has no completeness gate of its own beyond the drift check.

## Files that are checked, not generated

Three files in this group are **hand-authored** — a person writes and edits them — and are validated
mechanically rather than produced by a `--write-*` flag:

- **`semantic-coverage-proofs.tsv`** — impossibility proofs. Each row names a proof kind
  (`dtd-default`, `no-consumer`, `not-in-signature`, `blocked-by-defect`, …) the gate *recomputes*
  from the engine/DTD rather than trusting the prose; a stale or rejected proof fails
  `--counterfactual`'s completeness check, reported as `STALE PROOF` / `REJECTED PROOF`.
- **`semantic-coverage-presence-waivers.txt`** — surfaces knowingly counted as covered on presence
  alone, with the reason written next to each. `--write-coverage-baseline`'s run requires this list
  to equal the recomputed presence-only set exactly (`UNWAIVED` / `STALE PRESENCE WAIVER` findings),
  so a waiver cannot silently accumulate and a fixed line must be deleted by hand.
- **`semantic-catalog.yaml`** — the curated semantic inventory scope. There is no `--write-*` flag for
  it at all: `--propose-semantic-catalog` prints a review-only proposal to stdout and explicitly
  "never changes `conformance/semantic-catalog.yaml`" (`Program.cs`'s own usage text). A human reviews
  the proposal and edits the checked-in catalog by hand; `--semantic-coverage` then audits the
  checked-in file for internal consistency (an unmapped surface fails the audit) but never rewrites
  it.

`words.yaml` and `grammar.xml` themselves are, of course, also hand-authored — that is the entire
point of the suite — and are validated on every `--generate-manifest`/`--check-manifest` run against
`schema/words.schema.json` and `HermitCrabInput.dtd` respectively, before anything is written.

## Non-ledger contents of `conformance/`

- **`adapters/hc-dotnet-wrapper.sh`** — a one-line wrapper making C#'s own reference tool
  (`hc.dll`) satisfy the 3-positional-argument `batch` adapter contract it doesn't natively expose
  (`PROTOCOL.md` section 7). Not needed for self-check mode; only for pointing `--adapter` at `hc.dll`
  itself.
- **`skills/`** — the agent-facing authoring/review loop (`author-coverage-cell`,
  `review-coverage-claim`, `measure-authoring-quality`, `revise-coverage-harness`) used to fill
  `dataflow-obligations.tsv` gaps. Its own `README.md` in that directory is the reference; it produces
  `words.yaml` content and coverage claims, never a ledger, and its output is subject to every gate
  above like any other authored fixture content.

## Running the harness itself (not a ledger — the conformance run proper)

```
# self-check: in-process C# oracle against every fixture
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --include-pathological

# adapter mode: materializes words.txt/expected.tsv to temp, runs an external engine
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --adapter "<command template>"
```

`--include-pathological` is required to run the one `budget_ms` fixture; omitted, it's excluded and
reported as such rather than silently skipped without comment. Neither of these two invocations
writes or checks any ledger — they run the conformance fixtures themselves, per `PROTOCOL.md`.
