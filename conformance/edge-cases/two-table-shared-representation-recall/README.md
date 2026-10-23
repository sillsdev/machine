# two-table-shared-representation-recall: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: two-table-shared-representation-recall

## Why this fixture exists

`plan-construct-coverage-completion` task 4.4b / `docs/conformance/multitable-shared-representation-
design.md`: the RECALL-side counterpart to `conformance-staging/edge-cases/
bistratal-overlapping-segment-representation/` (which pins the REFUSAL/verdict side only, with each
stratum's own lexicon deliberately independent -- no rule threads material between its two tables).
This fixture is the one that DOES thread material across tables: a root entered on the Inner
stratum (table t0) is devoiced by the Outer stratum's (table t1) own obligatory phonological rule,
and table t1 spells the shared segment ("x") at a DIFFERENT raw index than table t0 does.

`pg_foma::replace::SegAlphabet::token` (`PUA_BASE + cd.0`) is a pure function of a char-def's raw
per-table index, blind to which table it came from. Pre-fix, a rule compiled against table t1
rendered its "x" atom using table t1's own index; a root emitted from table t0 carries table t0's
own index for "x". These differ, so the rule never fired on the Inner-stratum root's material, and
the FST proposer never proposed the devoiced ("y") surface form the oracle (`pg_parse::Morpher`,
which resolves every segment via genuine feature-lane unification, never a raw-index comparison)
correctly finds. `crate::replace::RepresentationAliasMap`/`SegAlphabet::render_tokens` (render-time
cross-table token aliasing, consumed by `crate::lower::render_slots`) closes exactly this gap.

This fixture pins:

1. **The structural characterization.** Two tables (`Grammar::char_tables.len() == 2`), NOT
   pairwise-disjoint (both declare "x"), each stratum's own `StratumDef::table` pointing at a
   DIFFERENT table.
2. **The capability gate's `ConfirmOnly` verdict** (same as `bistratal-overlapping-segment-
   representation`'s own pin, task 4.4b's flip from the old `Refuse`) -- not re-pinned by a
   dedicated Rust test here (that predicate-level pin already lives on the OTHER fixture); this
   fixture's own paired Rust test (`rust/crates/pg-foma/tests/
   two_table_shared_representation_recall.rs`) instead exercises the MECHANISM the verdict flip
   rests on.
3. **The oracle's own correct, cross-table analysis** of every word below (`pg_parse::Morpher`).
4. **The FST proposer's own recall**, demonstrated in three steps by the paired Rust test:
   - the pre-fix-equivalent (table-blind, unaliased) rule net provably MISSES the Inner-stratum
     root's material (never rewrites it to "y"),
   - the CURRENT (fixed) compile provably CATCHES it, and
   - end-to-end containment against the oracle holds for every word in this file.

## What it pins

- `y`: ROOT1 (Inner stratum), correctly devoiced through the Outer stratum's own rule. Positive,
  the cross-table recall case.
- `x`: ROOT1's own raw (undevoiced) spelling -- `expect_fail: true` (the rule is obligatory).
- `z`: table B's own decoy segment (declared FIRST in `t1`'s `SegmentDefinitions`, so `t1`'s own
  "x" sits at raw index 1, not 0 -- the deliberate misalignment) -- `expect_fail: true`, a plain
  negative control.
- `q`: ROOT2 (Outer stratum, table B) -- a same-table positive control, unaffected by the rule.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured via a throwaway test (`rust/crates/pg-foma/tests/
zz_throwaway_sig_dump.rs`, deleted after transcription) driving `pg_parse::Morpher::parse_word_opts`
directly over every word.

## A discovered finding, since fixed

`morpher.parse_word_opts("y", ..).signature()`'s SURFACE half used to render empty (`"ROOT1|"`, not
`"ROOT1|y"`), even though the underlying MORPHEME-level analysis (root identity) was already exactly
correct -- `n_analyses == 1`, correctly naming ROOT1. Root cause: `pg_rules::stratum::
synthesize_stratum_traced` never assigned a candidate `Word`'s own `.stratum` field the way the
ANALYSIS-direction `analyze()` does, so `Morpher::surface_of`'s `g.strata[w.stratum.0].table` lookup
resolved the WRONG table for a root synthesized past its own entry stratum. Fixed by assigning
`.stratum` on stratum entry during synthesis too, mirroring `analyze()`. `words.yaml`'s `y` entry
now pins the corrected signature (`"ROOT1|y"`).

## Verification

Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) -- that test is what actually gates CI. The deeper FST-proposer-vs-oracle containment claim
(items 3-4 above) is pinned directly by `rust/crates/pg-foma/tests/
two_table_shared_representation_recall.rs`, which also demonstrates the pre-fix loss and the fix
closing it over the real production compile path (`pg_foma::replace`), not a hand-rolled token-math
simulation.

## Founding-oracle verification (update)

hc.dll originally could not even LOAD this grammar: `PhonologicalFeatureSystem` was declared before
`PartsOfSpeech` (hc.dll's DTD requires `PartsOfSpeech` first), and `PhonologicalSubrule`'s own `id`
attribute (`subXtoY`) is not declared in the DTD at all. Both were structural XML fixes with no
linguistic content change.

Re-verified against the C# founding oracle (hc.dll, via `hc-conformance.exe` self-check): **hc.dll
diverges from the "discovered finding, since fixed" section above.** For word `y`, hc.dll's own
signature is `"ROOT1|"` (empty surface half), not `"ROOT1|y"`. The Rust-side fix described above
(`pg_rules::stratum::synthesize_stratum_traced` assigning `.stratum` on synthesis entry) changed
HC-Rust's own output from `"ROOT1|"` to `"ROOT1|y"` -- which is a divergence FROM the founding
oracle, not the correctness fix it was believed to be at the time. Per this repo's oracle hierarchy
(CLAUDE.md), hc.dll is ground truth: `words.yaml`'s `y` entry now reads `"ROOT1|"` (traced to
`[prXtoY]`), and the Rust-side behavior needs re-examination -- see whether
`rust/crates/pg-parse/tests/conformance_fixtures_gate.rs` reproduces `"ROOT1|"` or still produces
`"ROOT1|y"`. `words.yaml`'s header now reads `oracle-provenance: founding-oracle`.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/two-table-shared-representation-recall/`. On acceptance, delete
this staged copy in the same change (graduation guard enforces this mechanically).
