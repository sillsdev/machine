# pattern-root-required-environment: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: pattern-root-required-environment

## Why this fixture exists

Commit 620863f6 made `EmissionStrategy::TunedSurfaceProbed`'s `collect_roots`
(`rust/crates/pg-foma/src/emit.rs`) compile an unbounded pattern root shape (`[Any]*`) as a
compiled regex entry instead of leaving it uncovered, closing a real gap for the common case. But
`collect_roots`' regex route only applies when the allomorph carries no `<RequiredEnvironments>`
(`pattern_regex_body`'s own scope limit: the precision flags a required environment needs have no
literal surface to check on an unbounded shape). No staged or upstream fixture exercised that
combination, so after 620863f6:

- `surface-probe.root-spelling-cap` (`capability.rs`'s `EagerRouteDropsRootSpellingsCheck`, backed
  by `crate::emit::eager_route_drops_root_spellings`) lost its only negative witness across the
  whole 61-fixture suite that fires through the `VariantLimit::Unbounded` branch (the
  `VariantLimit::BytesExhausted` branch is unconditional and still had other witnesses, but the
  `Unbounded` branch specifically requires a non-empty `allo.environments`, and nothing had one).
- `envelope_agrees_with_compiler_gate`'s `report_uncovered_constructs_behind_surface_probe_divergence`
  and `the_published_root_spelling_fact_never_over_claims_a_drop` both went vacuous for the same
  reason: `TunedSurfaceProbed` refused nothing in the entire suite.

This fixture restores both witnesses without touching production code: a lexical PATTERN root
allomorph (`ePattern`, `[Any]*` over the empty-FS `Any` natural class, same construct
`edge-cases/guesser-pattern-root-fallback` and `edge-cases/backend-strata-generic` already pin as
guess-only/never ordinarily trie-indexed) that additionally carries a `<RequiredEnvironments>`
constraint. `collect_roots` cannot route it through the regex path (non-empty `environments`), and
it has no other finite representation, so it is left uncovered — `TunedSurfaceProbed` refuses the
grammar, naming the construct via `CapabilityDiagnostic`.

## What it pins

- **`ePattern`** (the construct): an iterative pattern root allomorph with a `RequiredEnvironments`
  constraint. Guess-only in this port's semantics (`RootAllomorphDef::is_pattern`), so it never
  participates in ordinary (non-guess) lookup — no word in `words.yaml` is expected to reach it,
  and its `RequiredEnvironments` (a `RightEnvironment` over a vowel class) is never actually tested
  by any parse. Its sole role is to make `EmissionStrategy::TunedSurfaceProbed` refuse the grammar
  at compile time, giving `surface-probe.root-spelling-cap` and the `envelope_agrees_with_compiler_
  gate` report a live witness again.
- **`eTik`/`eBon`** (positive controls): two ordinary literal roots, trie-indexed normally, each
  combining with the one unconstrained suffix rule (`mrPast`, `-us`) to give this fixture real
  adapter-visible analyses (`tik`, `tikus`, `bon`, `bonus`) entirely independent of `ePattern` —
  proving the pattern root's presence changes nothing about ordinary parsing.
- **`zim`/`tikbon`** (negative controls): well-formed surfaces with zero valid analyses — neither
  matches a literal root, the suffix's output shape, or (guessing being off) the pattern root.

## Oracle discipline

Verified against the C# founding oracle (`rust/tools/oracle-conformance.ps1 -Scope local`), per
CLAUDE.md's "oracle hierarchy" and this repo's own `conformance-grammars` skill. See the
Verification section below for the run result; `words.yaml`'s header carries
`# oracle-provenance: founding-oracle` once that run passed.

## Verification

- `rust/tools/oracle-conformance.ps1 -Scope local`: self-check PASS against `hc-conformance.exe`
  (see the header line this staging note's companion `words.yaml` carries for the exact machine
  commit and date).
- `rust/tools/pg.ps1 -Mode test -Package pg-parse -TestTarget conformance_fixtures_gate`: HC-Rust
  matches the oracle on every adapter-visible word (`tik`, `tikus`, `bon`, `bonus`, `zim`,
  `tikbon`).
- `rust/tools/pg.ps1 -Mode test -Package pg-foma -TestTarget predicate_negative_witness_gate`:
  `surface-probe.root-spelling-cap` has a live negative witness again.
- `rust/tools/pg.ps1 -Mode test -Package pg-foma -TestTarget envelope_agrees_with_compiler_gate`:
  `report_uncovered_constructs_behind_surface_probe_divergence` is non-vacuous again;
  `the_published_root_spelling_fact_never_over_claims_a_drop` restored to its one-way shape
  (`claimed > 0`, every claimed refusal really refuses).
- `rust/tools/pg.ps1 -Mode test -Package pg-foma -TestTarget backend_scoreboard_gate`: denominator
  61 -> 62 (exact/misses/refused/unmeasurable per strategy). `TunedSurfaceProbed` moves
  `59/2/0/0` -> `59/2/1/0` (this fixture is the one new refusal). `TemplatedUnderlyingTokens`
  moves `36/5/20/0` -> `36/5/21/0` (it already refuses every pattern-shaped root allomorph
  regardless of environments, so this fixture is a new refusal there too). `PlanComposed` moves
  `20/2/36/3` -> `21/2/36/3` (its own route is unaffected by this construct; it measures the
  fixture `oracle_exact`).
- `rust/tools/pg.ps1 -Mode test -Package pg-foma -TestTarget faithfulness_coverage_gate`: unchanged
  (`NoMoreThan { failures: 23 }` still holds; this fixture contributes no faithfulness failure).
- `rust/tools/pg.ps1 -Mode test -Package pg-foma -TestTarget conformance_coverage_gate`: unchanged
  (`supported_construct_conformance_coverage_has_no_gaps` still passes).

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/pattern-root-required-environment/`. On acceptance, delete this
staged copy in the same change (graduation guard enforces this mechanically).

## Shared construct coverage note

The shared tags record only ordinary mrPast affix analyses. No word reaches the guess-only
ePattern root or tests its required environment; its role is to make the surface-probe refusal
observable, as described above.
