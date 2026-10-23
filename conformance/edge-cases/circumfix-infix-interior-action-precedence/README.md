# circumfix-infix-interior-action-precedence: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: circumfix-infix-interior-action-precedence

## Why this fixture exists

Pins census **C3** (`docs/conformance/circumfix-structural-composite-census.md`,
`openspec/changes/plan-construct-coverage-completion` task 4.3b): `crate::emit::classify_affix`'s
interior-action test used to run BEFORE its leading-AND-trailing test, so an RHS that is
SIMULTANEOUSLY circumfixing (an `InsertSegments` before the first `CopyFromInput`, another after
the last) AND infixing (a THIRD `InsertSegments` sitting strictly between two `CopyFromInput`
actions) classified `Role::Infix` — the wrong taxonomic label for a morph that genuinely wraps the
root on both sides — and was routed to `crate::preexpand` instead of `build_structural_composites`,
whose own comment names circumfix explicitly as needing its unconditional
(`probe_would_refuse`-independent) path. After the fix, the leading-AND-trailing test runs FIRST, so
this shape now classifies `Role::CircumfixPrefix` and is routed to `build_structural_composites`
instead.

**Important correction, found by testing rather than assuming:** an earlier draft of this document
claimed `crate::preexpand` "can only splice literal text, never wrap a root on both sides." That is
WRONG. `crate::preexpand::extend` (its own module doc) ALSO calls
`pg_rules::morph::synthesize_cached` — the SAME real engine `build_structural_composites` uses — so
it is NOT incapable of producing this exact surface. Confirmed directly: temporarily reverting
`classify_affix`'s reordering and re-running `circumfix_infix_interior_action_recall_parity` (below)
showed it PASSING even without the fix — recall for `kebzatan` was never actually lost. What the fix
demonstrably changes is OWNERSHIP: `circumfix_infix_ownership_handoff_is_clean` (below) DOES fail
without the fix. See "Recall argument" for the corrected, narrower claim the fix actually rests on.

`grammar.xml`'s one `MorphologicalRule` (`mrCircInfix`) has a two-part LHS (`p1` = the root's first
segment, `p2` = the remainder) so its RHS can place an `InsertSegments` ("z") strictly between the
two `CopyFromInput` actions while ALSO leading with one `InsertSegments` ("ke") and trailing with
another ("an") — the exact simultaneously-circumfixing-and-infixing shape census C3 names.

## What it pins

- `bat` (bare root): a plain control.
- `kebzatan` (root + `mrCircInfix`): `classify_affix` classified `subCircInfix`'s RHS `Role::Infix`
  before the fix (because of the interior "z" insert), routing `mrCircInfix` to `crate::preexpand`
  rather than `build_structural_composites` — a MISCLASSIFICATION, not a recall failure: `preexpand`
  already resynthesizes this surface correctly today (see the correction above). After the fix,
  `classify_affix` reads this RHS as `Role::CircumfixPrefix` and `mrCircInfix` is admitted into
  `build_structural_composites` instead.
- A companion Rust test (`rust/crates/pg-foma/tests/circumfix_candidate_selection.rs`) proves the
  proposer-to-confirm containment directly against the compiled FST (every analysis
  `pg_parse::Morpher` finds for `kebzatan` is reachable in `emit::emit`'s output — true both before
  and after the fix, per the correction above) and separately confirms the ownership handoff is
  clean (true ONLY after the fix): `emit::composite_candidate_rules` reports `mrCircInfix` in the
  structural set and NOT in `crate::preexpand`'s own candidate set (via that same public
  diagnostic), so the two composite mechanisms never both claim this rule and never both drop it.

## Recall argument (task 4.3b's own obligation)

The load-bearing claim is narrower than "this closes a recall gap" — checked empirically, not
assumed: `crate::preexpand::extend` (its own module doc) ALSO calls
`pg_rules::morph::synthesize_cached`, the SAME real engine `build_structural_composites`'s
`struct_extend` calls (`pg_rules::morph::synthesize(ctx.g, base_word, rule)`,
`rust/crates/pg-foma/src/emit.rs`). Both are pure functions of `(Grammar, Word, MorphRuleDef)` that
execute every `OutputAction` in RHS document order, with no reference to `Role`/`classify_affix` and
no assumption that a `Copy` run is contiguous — confirmed by reading `pg-rules/src/morph.rs` and by
directly testing it: temporarily reverting this fix and re-running
`circumfix_infix_interior_action_recall_parity` showed `kebzatan` STILL reachable, because
`crate::preexpand` (which `Role::Infix` routed the rule to) already covers it. So an interior
non-`Copy` action between two `Copy`s adds no new representational burden to EITHER composite
mechanism once a rule reaches it — that part of the original argument holds, just not as a
recall-gap argument.

What the fix actually rests on is narrower and still real: (1) taxonomic correctness — the rule
genuinely wraps the root on both sides, and `Role::Infix` is simply the wrong label independent of
which mechanism ends up covering it; (2) robustness — `build_structural_composites`'s
`CircumfixPrefix` admission is unconditional (`is_structural_rule`'s own comment), while
`crate::preexpand`'s coverage of this shape, though real, is incidental to a module whose own doc
frames its mechanism as interdigitation/boundary-fusion, never circumfix; (3) the capability layer
(`crate::capability::CircumfixStructuralCompositePredicate`) reads `is_structural_rule` as its own
ground truth — before the fix, a rule misclassified `Infix` here would make that predicate `Refuse`
a grammar `crate::preexpand` was already covering, an over-refusal consistent with the census's own
"every gap fails over-refusing, never a silent overclaim" finding.

**What would falsify the narrower claim:** discovering that `pg_rules::morph::synthesize` (or
anything `struct_extend`/`preexpand::extend` calls) special-cases the RHS action sequence's SHAPE
rather than just replaying it in order — e.g. some assumption baked into LHS/RHS matching that a
"circumfix" allomorph has exactly one contiguous copied span. No such special-casing exists in
either caller today; if one is ever added, this argument must be re-checked.

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
`machine/conformance/edge-cases/circumfix-infix-interior-action-precedence/`. On acceptance, delete
this staged copy in the same change (graduation guard enforces this mechanically).

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.
