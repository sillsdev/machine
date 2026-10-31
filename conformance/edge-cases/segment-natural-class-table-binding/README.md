# segment-natural-class-table-binding: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: segment-natural-class-table-binding

## Why this fixture exists

Closes a conformance-**suite-design** blind spot (not a code bug in this crate): every existing
multi-table fixture (`two-table-shared-representation-recall`,
`multi-table-metathesis-shared-representation`, `bistratal-overlapping-segment-representation`)
builds its rules' natural classes from **`FeatureNaturalClass`** only. `pg_rules::bridge::
PatternBridge`'s `nat_class_lanes` `NaturalClassKind::Feature` branch never reads `self.table` at
all -- a `SymbolicFeature`'s bit assignment is grammar-wide, not per-table, so feature-kind lanes are
table-agnostic by construction. That means **no fixture in the suite could ever detect a wrong-table
char-def resolution**: a rule wrongly compiled against table 0 instead of its own owning stratum's
table would still resolve every `FeatureNaturalClass` identically, and every existing multi-table
fixture would keep passing regardless. This is precisely how an eleven-`TableId(0)`-site defect in
the confirm engine (including the production entry points for every ordinary rewrite rule) survived
undetected by the whole conformance suite -- the only thing that ever caught it was a unit test
written specifically to exercise the table-**dependent** path
(`rust/crates/pg-rules/src/cache.rs`'s `owning_table_tests` module).

Only `NaturalClassKind::Segments` (`<SegmentNaturalClass>`) is genuinely table-**dependent**: its
members are raw per-table `CharDefId`s with no table of their own, resolved via
`self.table.get(cd)` -- a real, observable dependency on which table the caller passes in. This
fixture's own `grammar.xml` transplants `cache.rs`'s inline two-table/two-stratum probe grammar into
a real, lexicon-bearing conformance fixture (rather than a hand-built `Grammar` value only a unit
test can see), so the SAME table-dependent property becomes visible to **conformance**, closing the
gap `cache.rs`'s comment already named ("this module's fix for the 'implicit table-zero default'
antipattern class... the functions below are its confirm-side/oracle equivalent").

## Structural shape

- `Grammar::char_tables.len() == 2`: `t0` ("Inner", 1 segment "z", `f`=+), `t1` ("Outer", 2 segments:
  "k" `f`=-, "g" `f`=+).
- `Grammar::strata.len() == 2`: S0 (Inner, table t0, no rules) is FIRST; S1 (Outer, table t1, owning
  `prKtoG`) is SECOND (non-first) -- also the grammar's LAST stratum, so ROOT2 (entered directly on
  S1) is reachable at the surface (this codebase's own surface-tokenization convention segments the
  initial input only against the grammar's LAST stratum's table --
  `bistratal-overlapping-segment-representation`'s own STAGING.md documents this same architectural
  fact).
- `prKtoG` (obligatory, no environment: `SegmentNaturalClass` `ncK`={k} -> `ncG`={g}) is the ONE
  phonological rule, wired only into S1's own `phonologicalRules` cascade.
- `ncK`'s one member ("k") sits at table 1's raw index 0 -- the SAME raw index table 0's own sole
  segment ("z") sits at, but with the OPPOSITE feature value (`f`=- vs. `f`=+), deliberately, so a
  wrong-table (table-0) resolution of `ncK` can never accidentally still match a real table-1 "k".
  This mirrors every existing multi-table fixture's own "deliberately misaligned raw indices"
  convention, and `cache.rs`'s own probe grammar's identical choice, verbatim.
- ROOT1 (Inner stratum, table t0, spelled "z") is a same-table, rule-untouched decoy proving the
  "more than one table, one per stratum" construct has genuine substance on BOTH tables, not merely
  the rule-bearing one.

## What it pins

- `g`: ROOT2 (Outer stratum, table t1), correctly voiced ("k" -> "g") by the obligatory rule whose
  own `SegmentNaturalClass`es are resolved against t1. **Positive** -- this is the word that is
  UNREACHABLE under a wrong-table resolution (see "Discriminating power" below).
- `k`: ROOT2's own raw (unvoiced) spelling -- `expect_fail: true` (the rule is obligatory, no
  optionality).
- `z`: ROOT1 (Inner stratum, first/non-final table) -- `expect_skip: true` per the surface-
  tokenization convention above (a bare Inner-stratum root is never a complete, tokenizable surface
  word in this architecture -- the same fact `bistratal-overlapping-segment-representation`'s own
  STAGING.md documents, not a finding specific to this fixture).
- `gk`: a well-formed string over t1's own alphabet ("g"/"k" both legitimate t1 segments) that is not
  any real lexical entry's own shape -- `expect_fail: true`, a plain negative control.

## Discriminating power (proof, not assertion)

A fixture that would ALSO pass under a wrong-table resolution has exactly the blind-spot problem
this fixture exists to close -- so its discriminating power must be demonstrated, not merely
claimed. `rust/crates/pg-foma/tests/segment_natural_class_table_binding_discriminates.rs` does this
directly, via `pg_rules::bridge::PatternBridge`'s own PUBLIC API (`with_table`/`compile_pattern`) --
the exact seam `nat_class_lanes`'s `Segments` branch lives behind -- rather than by editing any
crate's `src/` (this task does not own `pg-rules/src`, and the real per-word cached call path,
`RuleCache`/`synthesize_with_mpr_cached`, is `pub(crate)`-only inside `pg-rules`, unreachable from a
`pg-foma` test without such an edit). `PatternBridge::new` itself defaults to `TableId(0)` (its own
doc: "resolving against table `TableId(0)`") -- literally the antipattern default this whole bug
class is about -- so `PatternBridge::new(&g)` with no `.with_table(..)` call IS the bug's own
resolution, reused directly as the "wrong" comparison arm; `.with_table(TableId(1))` is the rule's
real owning table (S1, the same table `RuleCache::build`'s `owning_table_for_prule` resolves this
rule to in the real, already-fixed production path).

Compiling `prKtoG`'s LHS (`ncK`) both ways and comparing against a REAL table-1 "k" segment's own
feature lanes, both outputs observed (`cargo test -p pg-foma --test
segment_natural_class_table_binding_discriminates -- --nocapture`):

```
OUTPUT 1 (correct table, TableId(1)): ncK lanes = [2, 1], real "k" lanes = [2, 1], flat_unifiable = true
OUTPUT 2 (wrong table, TableId(0), PatternBridge::new's own default): ncK lanes = [1, 1], real "k" lanes = [2, 1], flat_unifiable = false
```

Resolved against its own table (1), `ncK`'s compiled constraint equals table 1's own "k" lanes
exactly and matches a real "k" segment -- reachable, matching `words.yaml`'s `g` -> `"ROOT2|g"`.
Resolved against table 0 instead (the bug), `ncK`'s compiled constraint becomes table 0's own "z"
lanes -- a different, non-unifiable value -- and a real table-1 "k" segment no longer matches it at
all. `pg_fst::CompileNode::Constraint`'s own doc states its match rule is exactly
`pg_featstruct::flat_unifiable` -- this is not a hand-rolled stand-in predicate, it is the literal
per-arc match rule every FST this bridge compiles uses. Concretely: had the eleven-site
`TableId(0)`-default defect existed in `SegmentNaturalClass` resolution, `prKtoG` could never fire on
a real "k", ROOT2 could never surface as "g", and this fixture's own `g` -> `"ROOT2|g"` ground truth
would be unreachable -- exactly the failure class this fixture exists to catch, demonstrated over
the real production compile seam, not a hand-rolled simulation of it.

## `NaturalClass: Segments vs FeatureNaturalClass/SegmentNaturalClass precision` (constructs.txt row
## 34) as its own coverage tag

Tagged (alongside row 36, the multi-table construct) on `g`/`k`, the two words whose ground truth
actually depends on `SegmentNaturalClass`'s table-bound resolution. `z`/`gk` tag only row 36 (they
exercise the multi-table structural shape but not the table-dependent natural-class resolution
itself -- `z` never reaches `prKtoG` at all, and `gk`'s negative-control status doesn't depend on
which table `ncK`/`ncG` resolved against).

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured via a throwaway test
(`rust/crates/pg-parse/tests/zz_throwaway_sig_dump.rs`, deleted after transcription) driving
`pg_parse::Morpher::parse_word` directly over every word.

## Verification

Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) -- that test is what actually gates CI, and runs against the REAL, already-fixed production
path (`pg_parse::Morpher`, which threads `RuleCache::build`'s `owning_table_for_prule` resolution).
The discriminating-power claim above is pinned directly by `rust/crates/pg-foma/tests/
segment_natural_class_table_binding_discriminates.rs`.

## `structural_witness_gate.rs`: considered, not extended

That gate's own job is narrower than it might look: its three registered witnesses each answer "does
some PASSING fixture's grammar structurally exhibit construct X, so its shared `constructs.txt` row
id isn't resting solely on a coarser sibling's evidence?" -- for row ids that are shared between TWO
different `CharacteristicKind`s (`pg_foma::conformance_coverage::construct_ids_for`'s "shared id" set:
`Stratum (Linear/Unordered rule order)`, the iterative-rewrite-rule row, and
`AffixProcessRule: prefix/suffix/circumfix/infix`). Row 34 (`NaturalClass: Segments vs
FeatureNaturalClass/SegmentNaturalClass precision`) and row 36 (`CharacterDefinitionTable: more than
one table, one per stratum`) are each mapped from exactly ONE `CharacteristicKind` apiece in
`construct_ids_for` -- neither is a shared id at risk of the specific silent-inheritance failure mode
that gate exists to close (one `CharacteristicKind`'s `Covered` status resting on a DIFFERENT
`CharacteristicKind`'s fixture). Extending that gate's own registry to a fourth, unshared-id entry
would not be wrong, but it would not be answering the question that gate was built to ask either --
it already effectively "just works" (`conformance_coverage_gate.rs`'s own coverage report) since a
single characteristic's `Covered` verdict already requires a real passing fixture tagging its own
row id, no sibling-inheritance possible when there is no sibling. This fixture's own real
discriminating-power risk (a fixture that would ALSO pass under a wrong-table resolution) is a
DIFFERENT question from what `structural_witness_gate.rs` checks, and is instead answered directly by
`segment_natural_class_table_binding_discriminates.rs` above -- the right tool for this specific
claim, not a forced fit into the witness-gate's own narrower contract.

## Founding-oracle verification (update)

hc.dll originally could not even LOAD this grammar: `PhonologicalFeatureSystem` was declared before
`PartsOfSpeech`, but hc.dll's DTD requires `PartsOfSpeech` first (`Language`'s content model is
`(Name, PartsOfSpeech, PhonologicalFeatureSystem*, ...)`), and `PhonologicalSubrule`'s own `id`
attribute (`subKtoG`) is not declared in the DTD at all (`PhonologicalSubrule` carries
`isActive`/`requiredPartsOfSpeech`/`requiredMPRFeatures`/`excludedMPRFeatures` only -- unlike
`MorphologicalSubrule`). Both were structural XML fixes with no linguistic content change.

Re-verified against the C# founding oracle (hc.dll, via `hc-conformance.exe` self-check): **hc.dll
itself produces an ADDITIONAL analysis this fixture's own thesis expected to be impossible.** For
word `g`, hc.dll reports TWO analyses -- `"ROOT2|g"` (as authored) AND `"ROOT1|z"` -- i.e. the
wrong-table `SegmentNaturalClass` collision this fixture was built to prove could never happen (ncK's
member "k" sits at t1 raw index 0, which t0's "z" also occupies, with the opposite feature value) is,
per the founding oracle, a REAL ambiguity in hc.dll. Per this repo's oracle hierarchy (CLAUDE.md),
hc.dll is ground truth for this suite regardless: `words.yaml` now records both analyses (both traced
to `[prKtoG]`), and `pg_parse::Morpher`'s own single-analysis behavior for `g` is a divergence FROM
the founding oracle, not a bug this task fixed -- see whether
`rust/crates/pg-parse/tests/conformance_fixtures_gate.rs` reproduces the second analysis.
`words.yaml`'s header now reads `oracle-provenance: founding-oracle`.

This finding does not weaken the fixture's own discriminating-power argument (the "Discriminating
power" section above, and `segment_natural_class_table_binding_discriminates.rs`): that argument is
about the `PatternBridge`-level compiled-constraint difference between resolving `ncK` against the
right vs. wrong table, and holds independent of whether hc.dll's OWN engine also happens to produce
the cross-table analysis for an unrelated reason inside its own rule-cascade traversal.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/segment-natural-class-table-binding/`. On acceptance, delete this
staged copy in the same change (graduation guard enforces this mechanically).
