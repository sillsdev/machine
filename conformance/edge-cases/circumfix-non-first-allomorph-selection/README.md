# circumfix-non-first-allomorph-selection: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: circumfix-non-first-allomorph-selection

## Why this fixture exists

Pins census **C1** (`docs/conformance/circumfix-structural-composite-census.md`,
`openspec/changes/plan-construct-coverage-completion` task 4.3a): `crate::emit::rule_role`
classifies a rule's role from its FIRST allomorph only. Before the fix, `crate::emit::
is_structural_rule` inherited that first-allomorph view, so a rule whose allomorph 0 is an
ordinary Prefix/Suffix/None/Infix/Reduplication/Process shape but whose allomorph 1..n is
circumfix-shaped never had that later allomorph recognized as needing
`build_structural_composites` — the ONLY mechanism able to represent a discontinuous "wraps the
root on both sides" morph at all (the ordinary concatenative emission path is, by this crate's own
comment, "unconditionally unrepresentable" for that shape). This is a real, honest, fail-closed
recall gap: the affected allomorph is reported `uncovered` rather than silently mis-compiled, but
it is nonetheless never reachable from the proposer. The gap is order-of-declaration-dependent — it
appears and disappears purely as a grammar author reorders a rule's allomorphs in the XML, which is
what makes it worth fixing rather than documenting as a permanent boundary.

`grammar.xml`'s one `MorphologicalRule` (`mrMixed`) declares exactly this shape: allomorph 0
(`subSuffix`) is an ordinary suffix ("-s"); allomorph 1 (`subCircum`, declared SECOND) is the
circumfix ("ke-...-an", leading AND trailing `InsertSegments` around one `CopyFromInput`).

## What it pins

- `mits` (allomorph 0, the ordinary suffix): the ordinary allomorph rides along once the rule is
  admitted as a structural candidate — over-inclusion here is safe by construction
  (`build_structural_composites` delegates to the real morphological engine,
  `pg_rules::morph::synthesize`, for every allomorph of an admitted rule, never re-deriving from a
  role label), mirroring the census's own "opposite order is safe" observation.
- `kemitan` (allomorph 1, the circumfix): THE load-bearing word. Before census C1's fix, this
  surface was unreachable from the FST proposer at all (`is_structural_rule` returned `false` for
  `mrMixed`, because `rule_role`'s allomorph-0 view reported `Role::Suffix`, and
  `Role::Suffix`'s only route to `true` — `rhs_drops_lhs_material` — does not hold for
  `subSuffix`'s LHS-preserving shape). After the fix (an allomorph-wise scan for
  `Role::CircumfixPrefix` added ahead of `rule_role`'s own classification), `mrMixed` is admitted and
  `kemitan` becomes reachable.
- A companion Rust test (`rust/crates/pg-foma/tests/circumfix_candidate_selection.rs`) proves the
  proposer-to-confirm containment directly against the compiled FST (every analysis
  `pg_parse::Morpher` finds for `kemitan` is reachable in `emit::emit`'s output), and separately pins
  the ORDER-INDEPENDENCE invariant the bug violated: the same rule with its two allomorphs declared
  in the OTHER order (circumfix first, suffix second) must be selected as a structural candidate
  identically. That inline-XML variant is not a second staged fixture (mirrors
  `rust/crates/pg-foma/tests/phase_c_circumfix.rs`'s own precedent of hand-authored inline XML for
  an internal-invariant check that isn't itself a new conformance corpus entry).

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; no `dotnet`/C# toolchain available in this environment. Per `docs/
conformance-staging-plan.md`'s oracle-discipline note, this must be treated as `pangloss`-only
ground truth until independently re-verified against the C# founding oracle.

## Verification

Signatures captured by running `cargo test -p pg-parse --test conformance_fixtures_gate --
--nocapture`, which discovers this staged fixture automatically and replays every word against
`pg_parse::Morpher` — the mismatch panic message on the first pass reported the engine's own actual
signature, transcribed verbatim into `words.yaml` above (not hand-derived). Final run: `3 passed; 0
failed`, `346 words checked across 29 fixtures` (the whole default corpus, this fixture included).

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/circumfix-non-first-allomorph-selection/`. On acceptance, delete
this staged copy in the same change (graduation guard enforces this mechanically).

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.
