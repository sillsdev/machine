# multi-table-metathesis-shared-representation: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: multi-table-metathesis-shared-representation

## Why this fixture exists

`docs/conformance/multitable-shared-representation-design.md`'s own "Residual gap this fix does NOT
close" section: `crate::replace::compile_metathesis_swap_net` used to render every switch-position
token DIRECTLY (`SegAlphabet::token`, table-blind, no cross-table alias expansion) rather than
through the alias-expanded path `two-table-shared-representation-recall` (task 4.4b) built for
ordinary rewrite rules. A `MetathesisRule` in a grammar whose tables share a normalized
representation therefore kept exactly the false negative 4.4b already fixed everywhere else. This
fixture combines `two-table-shared-representation-recall`'s own two-table, misaligned-shared-
representation structure with `right-to-left-metathesis-reversal`'s own multi-member-natural-class
`MetathesisRule` shape, per that closing task's own instructions.

Structural shape: two `CharacterDefinitionTable`s (`t0`="Inner", `t1`="Outer"), each stratum's own
`StratumDef::table` pointing at a different one. BOTH switch spellings ("m" and "x") are declared in
EACH table, at DELIBERATELY MISALIGNED raw indices (`t0`: m=0,x=1; `t1`: z=0[decoy],m=1,x=2,w=3
[decoy]). ROOT1 is entered on the Inner stratum (table `t0`), spelled "mx"; the `MetathesisRule`
lives on the Outer stratum (table `t1`) and must swap ROOT1's material to "xm" even though its own
natural classes (`ncSwitchA`={m,w}, `ncSwitchB`={x}) are resolved against `t1`'s different raw
indices for the same two spellings. ROOT2 (Outer stratum, spelled "wx" using `ncSwitchA`'s OTHER,
table-t1-only member "w") is a same-table positive control.

## What it pins

- `xw`: ROOT2 (Outer stratum), correctly metathesized. Positive, same-table control -- proves
  ordinary same-table metathesis recall is untouched by the fix.
- `wx`: ROOT2's own raw (un-metathesized) spelling -- `expect_fail: true` (metathesis is obligatory).
- `xm`/`mx`: ROOT1's (Inner stratum) expected metathesized/raw spellings -- see the finding below;
  both `expect_fail: true` as TRANSCRIBED, not as "should be."
- `z`: table `t1`'s own decoy segment -- `expect_fail: true`, a plain negative control.

## A second, separate discovered finding (transcribed honestly, not hidden)

Authoring this fixture surfaced a DIFFERENT, pre-existing gap from the one this task closes --
entirely inside `pg_rules::metathesis`/`pg_rules::bridge` (the oracle), never `pg_foma::replace`
(this task's own single-owner boundary). `pg_parse::Morpher` finds **zero** analyses for "xm" (ROOT1's
correctly-metathesized surface) even though the grammar is a faithful, DTD-legal multi-table
metathesis grammar and the corresponding same-table case (ROOT2/"xw") works correctly.

Two things were confirmed by direct experiment while narrowing this down:

1. `pg_rules::metathesis::synthesize`/`analyze` (`metathesis.rs:497,646`) hardcode
   `let table_id = TableId(0);` regardless of which table the rule's own stratum actually owns --
   the SAME "implicit table-zero default" antipattern `docs/conformance/
   multitable-shared-representation-design.md`'s whole narrative is about, just in the oracle instead
   of the proposer.
2. That hardcoding is NOT the sole cause: a throwaway probe that reordered the grammar's own
   `<CharacterDefinitionTable>` declarations so the rule's real table coincidentally became
   `TableId(0)` (confirmed via `g.strata[1].table == TableId(0)`) still found zero analyses for the
   cross-table root's swapped surface, with the misalignment otherwise unchanged. A THIRD throwaway
   probe with the raw indices for "m"/"x" made to coincide across both tables (no misalignment at
   all) found ROOT1 correctly (`"ROOT1|xm"`, 1 analysis) -- confirming cross-stratum metathesis
   threading itself works, and isolating the failure specifically to raw-index misalignment,
   independent of the `TableId(0)` hardcoding's own correctness. `pg_rules::bridge::nat_class_lanes`'s
   `NaturalClassKind::Feature` branch (used by this fixture's `FeatureNaturalClass`-based
   `ncSwitchA`/`ncSwitchB`) does not read `self.table` at all and `PatternBridge::feature_width` is
   grammar-wide, so the exact remaining mechanism was not fully isolated within this task's own
   `pg-foma`-only boundary -- reported as a real, reproducible, NOT-yet-root-caused finding rather
   than silently worked around or guessed at.

All three throwaway probes were deleted after recording this finding here, per this repo's own
established convention (mirrors `two-table-shared-representation-recall`'s own STAGING.md
precedent for its own unrelated discovered anomaly). This is entirely orthogonal to, and does not
block, the `pg_foma::replace`-level fix this fixture's own task was scoped to (routing
`compile_metathesis_swap_net`'s token rendering through the SAME alias-expanded path
`compile_rewrite_rule_subset` already uses) -- that fix is demonstrated and verified directly against
the compiled net in `rust/crates/pg-foma/tests/multi_table_metathesis_shared_representation.rs`,
bypassing this separate oracle gap entirely (the same "hand-render the pre-fix-equivalent net
directly" technique `two_table_shared_representation_recall.rs`'s own steps 1-2 already established).

## 2026-07-27 follow-up: the oracle-side `TableId(0)` defect is now fixed; "xm" still fails, root
## cause now precisely pinned (not just "not fully isolated")

The oracle-wide sweep this finding above called for landed (`rust/crates/pg-rules/src/cache.rs`,
`rust/crates/pg-rules/src/metathesis.rs`): every phonological-rule/allomorph char-def resolution in
`pg-rules` now threads the rule's/allomorph's OWN owning stratum's `TableId` (new
`owning_table_for_prule`/`owning_table_for_metathesis_rule`/`owning_table_for_allomorph` helpers in
`cache.rs`, mirroring `pg_foma::replace::owning_table`'s contract) instead of the module-level
`const TABLE: TableId = TableId(0)` constants finding-item-1 above named. Point 2's own "coincidence"
probe result is now simply always true: `metathesis::synthesize`/`analyze` resolve
`mrCrossTableSwap`'s real owning table (`t1`/Outer) correctly by construction, not by accident.

Re-running the oracle against this fixture's own `grammar.xml` after that fix (a throwaway probe
driving `pg_parse::Morpher::parse_word` over every word, deleted after transcription, mirroring this
file's own "Oracle discipline" section below): **`"xm"` still finds zero analyses.** `words.yaml`'s
`xm`/`mx` entries are UNCHANGED (`expect_fail: true` stands, transcribed as observed, not flipped).

This time the remaining mechanism WAS fully isolated (direct pipeline instrumentation, reverted
after transcription): the candidate for "xm" is correctly found by root-allomorph lookup and
correctly resynthesizes to a valid, passing word -- it is rejected only at the FINAL surface-match
gate, `pg_parse::Morpher::is_match_traced` (`pg-parse/src/morpher.rs`), which renders the accepted
candidate's concrete char-def identities via `matching_reps_for_node`
(`pg-parse/src/surface.rs`) using the grammar's OUTERMOST stratum's table unconditionally
(`g.strata[n-1].table` -- itself a *third*, independent instance of the same "assume one table,
usually table 0 or the last one" antipattern family, this time in `pg-parse`, which this task did not
own). That table mismatch is only exposed here because `pg_rules::metathesis::synthesis_reorder`
(`pg-rules/src/metathesis.rs`) -- unlike every rewrite-rule synthesis path that changes a node's
identity (`pg_rules::rewrite::syn_feature`/`sim_feature`, which reset a changed node's `char_def` to
`NO_CHAR_DEF`) -- physically relocates a segment without ever resetting its `char_def`, so a
metathesized root's segments keep carrying their ORIGIN table's raw char-def indices
(ROOT1/Inner/`t0`) all the way to `is_match`, which then compares them against `t1`'s own raw
indices -- an apples-to-oranges collision specific to metathesis (the only rule kind that moves
material without also erasing its concrete identity). The same-table ROOT2 control never exercises
this (its segments are `t1`-native already), and the sibling `two-table-shared-representation-recall`
fixture's own passing rewrite-rule case never exercises it either (its feature-changing rule resets
to `NO_CHAR_DEF` before `is_match` ever runs).

**Not fixed here**: this is a distinct defect from the one `pg-rules/src/cache.rs`/`metathesis.rs`'s
fix closes, it spans two files across two crates (`pg_rules::metathesis::synthesis_reorder`,
`pg-rules`; `pg_parse::Morpher::is_match_traced`/`matching_reps_for_node`, `pg-parse`), and the
second crate was outside this task's ownership. Recorded here, transcribed honestly, for a follow-on
-- not silently worked around, and not blocking the `TableId(0)` fix this file's earlier finding
already asked for (which IS now fully applied, and does not regress `two-table-shared-representation-
recall`'s own already-passing oracle recall or this fixture's own `xw`/`wx`/`z` controls).

## 2026-07-27 follow-up #2: both remaining `TableId(0)`/stale-identity antipathogen instances closed;
## "xm" now analyzes

Both instances this file's own follow-up above named as still-open are now fixed:

1. **`pg-rules/src/morph.rs`'s own module-level `const TABLE: TableId = TableId(0)`** (the last
   `pg-rules`-side instance, backing `compile_parts`/`cd_lanes`/`ctx_pins`/`ctx_lanes`/`ctx_cd_set`/
   `segs_of`'s id lane, and hence an affix allomorph's own LHS/RHS *pattern* — the environment half
   was already fixed by this file's earlier follow-up, but the pattern half was not). Every function
   that read that constant now takes an explicit `table: TableId`, resolved once per rule
   application via `crate::cache::owning_table_for_morpheme` (`AffixProcess`/`Realizational`) or
   `crate::cache::owning_table_for_mrule`/`owning_table_for_compounding_rule` (`Compounding`, which
   mints no `AllomorphOwner` for `owning_table_for_allomorph` to resolve through) and threaded down,
   never re-resolved per helper call. `pg-rules/src/validity.rs`'s own `const TABLE` was confirmed
   already correct as a side effect of the earlier sweep (its cached production path reads
   `cache.allomorph(id).envs`, built with the right table; only its standalone, non-grammar-resident
   entry point still hardcodes table 0, which is the honest, documented fallback for that case) and
   was left alone.
2. **The cross-table surface-match gate** this section's predecessor root-caused but did not fix
   (outside this task's original crate boundary): `pg_rules::metathesis::synthesis_reorder`
   (`pg-rules/src/metathesis.rs`) now resets a relocated segment's `char_def` to `NO_CHAR_DEF` when
   marking it dirty, mirroring `pg_rules::rewrite::syn_feature`/`sim_feature`'s identical convention
   for every OTHER identity-changing rewrite path. This was chosen over teaching
   `pg_parse::Morpher::is_match_traced`/`surface::matching_reps_for_node` to resolve "the node's own
   owning table" instead of the outermost stratum's: no per-node table-provenance metadata exists
   anywhere in `pg_shape::Shape`/`MutNode` today, so that alternative would have required inventing a
   new field threaded through every shape/node consumer (`root_trie.rs`, `guess.rs`,
   `RootAllomorphIndex`, and more), a much larger and riskier change, and would not have removed the
   actual staleness -- only taught one comparison site to tolerate it, which risks a coincidental
   raw-index collision producing a false POSITIVE elsewhere (worse than this bug's false negative).
   Resetting `char_def` at the point of relocation is the minimal, precedent-matching fix, and (per
   `MutShape::to_shape`'s existing plain `push_segment_with_lanes` path, already relied on by
   `syn_feature`/`sim_feature`) requires no new machinery at all.

Re-running the oracle after both fixes (a throwaway probe driving `pg_parse::Morpher::parse_word`
over every word in this fixture, deleted after transcription, mirroring this file's own "Oracle
discipline" section): `"xm"` now analyzes as `"ROOT1|xm"`. `"mx"`/`"wx"`/`"z"` are unchanged (still
correctly `expect_fail: true`); `"xw"` is unchanged (`"ROOT2|xw"`). `words.yaml`'s `xm` entry has been
flipped from `expect_fail: true` to a `parses:` entry with `signature: "ROOT1|xm"`, reflecting this
observed, re-derived (not hand-derived) result.

**Possible related, NOT-yet-isolated finding, flagged rather than chased**: `synthesis_reorder`'s
fix closes the one case this fixture demonstrates (a metathesis-relocated node crossing a table
boundary). Whether an ORDINARY (non-metathesis, non-feature-changing) morphological copy-through
(`pg_rules::morph::copy_part`'s plain `CopyFromInput` branch, which also never resets `char_def`)
could carry the same kind of stale, origin-table-relative identity across a stratum/table boundary
for a root that is never otherwise touched by an identity-resetting rule is a real question this
fixture's own two-root design does not exercise (`ROOT2` never crosses tables at all; `ROOT1` is only
ever observed via the metathesis path this fix already closes) — recorded here as a candidate for a
follow-on isolation, not fixed speculatively without a reproducing fixture.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured via a throwaway test driving
`pg_parse::Morpher::parse_word_opts` directly over every word, deleted after transcription.

## Verification

Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) -- that test is what actually gates CI, and only ever checks this fixture's words against the
ORACLE (never the FST proposer). The `pg_foma::replace`-level recall fix this fixture exists to pin
(loss reproduced, fix confirmed, switch-position identity preserved under aliasing) is pinned
directly by `rust/crates/pg-foma/tests/multi_table_metathesis_shared_representation.rs`, which also
demonstrates the fix over the real production compile path (`pg_foma::replace::
compile_and_compose_rules_with_budget`), not a hand-rolled token-math simulation, for every claim
that fixture-word's own oracle recall does NOT already block (the same-table ROOT2 case).

## Founding-oracle verification (update)

Re-verified against the C# founding oracle (hc.dll, via `hc-conformance.exe` self-check): both
signatures match exactly, and the `rules: []` fields for `xw`/`xm` have been filled in from the
oracle's own trace (`[mrCrossTableSwap]` for both). `words.yaml`'s header now reads
`oracle-provenance: founding-oracle`.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/multi-table-metathesis-shared-representation/`. On acceptance,
delete this staged copy in the same change (graduation guard enforces this mechanically).
