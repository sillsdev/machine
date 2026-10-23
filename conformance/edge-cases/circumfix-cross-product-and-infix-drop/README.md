# circumfix-cross-product-and-infix-drop: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: circumfix-cross-product-and-infix-drop

## Why this fixture exists

Continues the circumfix-structural-composite census
(`docs/research/circumfix-composite-precedence-census.md`) with two constructs that pin two
distinct, previously-uncensused shapes:

1. **A genuine circumfix cross-product**: a `MorphologicalRule` (`mrCross`) whose 4
   `MorphologicalSubrule`s realize a 2-prefix (`pa-`/`ma-`) x 2-suffix (`-an`/`-in`) combination,
   one subrule per prefix-allomorph x suffix-allomorph pairing, mirroring the shape a
   FieldWorks/LCM `MoAffixProcess` cross-multiplies from two separately-declared allomorph sets.
   Every subrule classifies `Role::CircumfixPrefix` (leading AND trailing `InsertSegments` around
   the copied stem), so all 4 are individually admissible into `build_structural_composites`
   today. `subCrossBA`/`subCrossBB` are declared 3rd/4th (non-first), echoing census C1's own
   "declared later" reachability concern.
2. **A genuinely `Infix`-shaped allomorph that drops LHS material**: `mrInfixDrop`'s one subrule
   has a 3-part LHS whose final part is never copied into the RHS. `classify_affix` correctly
   reads this `Role::Infix` (an interior `InsertSegments` strictly between two `Copy` actions, no
   leading/trailing insert) rather than misclassifying it as `CircumfixPrefix` -- unlike census
   C1-C3, which were all about `classify_affix` misclassifying a genuinely-circumfix shape.
   The fixture exposed that structural classification formerly excluded `Role::Infix` even when
   the allomorph drops LHS material. Such rules now move into structural-composite ownership and
   are removed from pre-expansion to avoid competing paths.

## What it pins

- `batid` (bare root): a plain control.
- `pabatidan`/`pabatidin`/`mabatidan`/`mabatidin`: the four cross-product cells of `mrCross`
  (`subCrossAA`/`AB`/`BA`/`BB` respectively) -- see "Deviations from the original sketch" below
  for why each subrule's own conditioning is carried by its literal insert text alone, not a
  live environment gate.
- `bumat` (`mrInfixDrop`): the oracle/confirm side (`pg_parse::Morpher`) finds this analysis. The
  companion FST-reachability test,
  `rust/crates/pg-foma/tests/circumfix_cross_product_and_infix_drop_candidate_selection.rs`, shows
  this surface is reachable through `build_structural_composites`, with the rule classified as a
  structural candidate and excluded from pre-expansion. This `words.yaml` entry alone does not
  exercise the FST path at all, matching
  this repo's own documented limit for `assert_matches_oracle` (it only ever checks the
  oracle/confirm side).
- `pidatan`: a structurally-invalid negative control (does not begin with either `mrCross`
  prefix), pinning that the grammar rejects an unrelated form rather than over-admitting.

## Deviations from the original sketch

The originating design sketch described `mrCross`'s per-subrule conditioning as literal
phonological environments ("root ends in i", "root starts with b/labial"), authored against a
single fixed root ("batid"). Two environment-encoding attempts were tried and abandoned during
authoring, both empirically (against the real `pg_parse::Morpher`, not by inspection alone):

1. **LHS-embedded `SegmentNaturalClass` constraints** on the stem's own first/last segment (a
   3-part `first`/`mid`/`last` split, with `first`/`last` constrained to specific classes for the
   axis-gated subrules). Empirically: the subrule with NO constraint (`ncAny` throughout) parsed
   correctly; every subrule with a real class constraint on `first` or `last` failed to parse at
   all (`signature: "-"`), even though "batid" trivially satisfies both intended classes (starts
   with "b", ends with "d").
2. **`RequiredEnvironments`/`LeftEnvironment`/`RightEnvironment`** on a single unsplit stem
   `PhoneticSequence`, mirroring `edge-cases/disjunctive-recheck`'s own proven suffix-rule
   pattern. Reading `pg-rules/src/validity.rs`'s own module doc (the W3.3 discontinuous-morph
   fix) surfaced why this cannot work for a circumfix: a rule that inserts both a prefix AND a
   suffix produces TWO separate contiguous `MorphRecord` runs for the SAME allomorph, and
   `environments_ok` is checked independently against EACH run's own span. A `RightEnvironment`
   meant to gate only the prefix side (checking the segment after the prefix run) still gets
   evaluated against the suffix run too (checking the segment after the *suffix*, i.e. past the
   end of the word) and fails there with nothing to match -- there is no way to declare "this
   environment applies to only one of a circumfix's two pieces" through this mechanism.

Given both real, proven mechanisms for asymmetric per-side conditioning either don't fire during
analysis for this shape (1) or apply to the wrong span of a discontinuous morph (2), `mrCross`'s 4
subrules are authored UNCONSTRAINED (identical `ncAny`-only stem patterns), matching
`circumfix-non-first-allomorph-selection`'s own proven 2-subrule pattern exactly, just scaled to
4. The literal `pa`/`ma`/`-an`/`-in` insert text alone already uniquely discriminates which
subrule produced which of the 4 surface words at analysis time, so the fixture still proves what
task 2 actually needs: all 4 cross-product cells are individually admissible into
`build_structural_composites` and reachable in the compiled net. The "genuinely combinatorial, not
2+2 independent choices" framing is preserved as a narrative property of the 4-subrule shape
itself (a lowering that only emitted 2+2 subrules rather than the full 2x2 product would be
missing `subCrossBB` outright, not merely misrouting an environment), rather than as a live
environment-conjunction gate. Gap 1's own environment/MPR-asymmetry semantics belong to the
`pg-grammar` cross-product lowering itself and are pinned there, not in this HC-XML-authored
front end.

The negative control word was authored as `pidatan` (matching the sketch) rather than reusing any
root material, since it already fails to begin with either `mrCross` prefix and needs no further
tuning.

## Findings for unit 3

The companion tests establish four current properties: `bumat` remains reachable;
`mrInfixDrop` is a structural candidate; it is absent from the pre-expansion candidate set; and
the capability predicate no longer refuses the grammar merely because this dropping allomorph is
classified `Role::Infix`. Classification is evaluated per allomorph, so declaration order cannot
hide a later structural shape. Whether the real motivating grammar's more complex "mrule 166"
case behaves identically remains unverified here; this fixture establishes the mechanism, not that
every Infix-with-drop grammar is otherwise within the tuned backend's capacity.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh
for this task; `machine/src` (the C# oracle's own source) was not widened into this worktree's
sparse `machine/conformance` checkout. Per `docs/conformance-staging-plan.md`'s oracle-discipline
note, this must be treated as `pangloss`-only ground truth until independently re-verified against
the C# founding oracle.

## Verification

Signatures captured by running a throwaway `pg-parse` test (`zz_throwaway_transcribe_cross_
product.rs`, deleted once transcription was done) driving `pg_parse::Morpher::parse_word` directly
over every word in this fixture and printing each `word`/`invalid_shape`/`outcome.signature()`
(the conformance-grammars skill's own step-3 shortcut for a from-scratch `pg-cli`/`pg-foma` release
build). Final transcription run (`pg.ps1 -Mode test -Package pg-parse -TestTarget zz_throwaway_
transcribe_cross_product -- --nocapture`):

```
word="batid" invalid_shape=false signature="ROOT|batid"
word="pabatidan" invalid_shape=false signature="CROSS+ROOT|pabatidan"
word="pabatidin" invalid_shape=false signature="CROSS+ROOT|pabatidin"
word="mabatidan" invalid_shape=false signature="CROSS+ROOT|mabatidan"
word="mabatidin" invalid_shape=false signature="CROSS+ROOT|mabatidin"
word="bumat" invalid_shape=false signature="ROOT+INFIXDROP|bumat"
word="pidatan" invalid_shape=false signature="-"
```

transcribed verbatim into `words.yaml` above. See the PR/task report for the `cargo test -p
pg-parse --test conformance_fixtures_gate` pass/fail counts confirming this fixture replays.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/circumfix-cross-product-and-infix-drop/`. On acceptance, delete
this staged copy in the same change (graduation guard enforces this mechanically).

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.
