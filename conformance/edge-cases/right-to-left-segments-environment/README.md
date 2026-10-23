# right-to-left-segments-environment: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: right-to-left-segments-environment

## Why this fixture exists

`openspec/changes/plan-construct-coverage-completion` task 4.2 extends
`crate::replace::compile_rtl_branch_net`'s reversal-plus-safety-net-union construction to pattern
shapes `crate::replace::pattern_slots` used to refuse unconditionally, for EVERY rewrite-rule
compile (RTL or not): `PatternNode::Segments` and `PatternNode::Anchor`. This fixture pins the
`Segments` shape specifically — a `Dir::RightToLeft` rewrite rule whose subrule's own right
environment is authored as an INLINE pre-segmented literal
(`<Segments><PhoneticShape>y</PhoneticShape></Segments>`) instead of an ordinary
`<SimpleContext>`/`<Segment>` reference, with NO `characterDefinitionTable` attribute (so it
defaults to the pattern's own table — the SAME-table case task 4.2 accepts; a `Segments` node
referencing a DIFFERENT table stays refused, `rust/crates/pg-foma/src/capability.rs`'s own
`right_to_left_predicate_refuses_cross_table_segments_shaped_rule` unit test pins that residual
case).

1. **The structural characterization.** `pg_foma::capability::rtl_reversal_diagnosis`
   (`RightToLeftRewriteDetail::reversal_construction_attempted`) now finds this shape SUPPORTED —
   `true`, where before task 4.2 it was `false` (ANY `Segments` node refused `pattern_slots`
   unconditionally, `crate::lower::UnsupportedPatternNode::Segments`'s own doc).
2. **The capability gate's own verdict.** `RightToLeftRewriteFaithfulReversalPredicate`
   (`right-to-left-rewrite.faithful-reversal-construction`) returns `ConfirmOnly` for this grammar
   — NOT `Refuse`.
3. **The oracle's own correct behavior.** `pg_parse::Morpher` correctly requires the literal "y"
   context for the obligatory rewrite to fire (see "What it pins" below).

## A design note: why `Segments` sits in the ENVIRONMENT, not the LHS

An earlier draft of this fixture authored the LHS itself as one inline literal
(`<Segments><PhoneticShape>aa</PhoneticShape></Segments>`, reusing the SAME two-adjacent-identical-
segment ambiguity `rust/crates/pg-foma/tests/phase_c_right_to_left.rs`'s own
`rtl_distinct_leftmost_rightmost_differs_from_ltr_and_is_recall_safe_against_the_current_oracle`
test proves genuinely direction-discriminating, to get BOTH "Segments now compiles" and "the RTL
construction genuinely differs from LeftToRight" out of one grammar). That draft's underlying root
("aaa") parsed successfully UNCHANGED through `pg_parse::Morpher`, and neither rewritten candidate
("ab"/"ba") parsed at all — i.e. the oracle never applies a rewrite rule whose LHS is a `Segments`
node at all. This is **not a new gap this task introduces or needs to fix**: it is the SAME
pre-existing `pg_rules::rewrite::width_matches` limitation
`rust/crates/pg-foma/src/replace.rs`'s own module doc already documents for a `Quantifier`-shaped
LHS/RHS focus ("Confirm-engine finding" section) — the analysis/synthesis engine's own width guard
compares the matched span's PHYSICAL width against the rule's raw `lhs.nodes.len()`, a plain NODE
count that is `1` for one `Segments` node regardless of how many physical segments (here, 2) it
actually spans. Entirely outside `replace.rs`'s single-owner boundary (this is `pg-rules`, a
different crate), and direction-independent (the same limitation would affect a `LeftToRight`-
declared rule with a `Segments`-shaped LHS identically).

This fixture instead places `Segments` in the ENVIRONMENT, where `pg_rules::rewrite`'s own
environment matching (`bridge.rs`'s `PatternBridge::compile_pattern`, `left_env_match`/
`right_env_match`) tests first-match EXISTENCE only, never a positional per-node width array — the
SAME reason `tests/phase_c_quantifier.rs`'s own bounded-quantifier fixture places ITS quantifier in
an environment rather than the LHS/RHS focus (`right-to-left-bounded-quantifier-rewrite`'s own
STAGING.md makes the parallel point for `Quantifier`). Verified empirically before committing to
this design (a throwaway probe, `ay`/`ey`/`a`/`e` against a Segments-environment-only grammar,
deleted after confirming the shape round-trips correctly).

The Segments-as-LHS ambiguity case (proving the RTL construction genuinely differs from a
`LeftToRight` compile, for THIS shape specifically) is pinned separately, at the FST/automaton
level with no oracle dependency at all, by `tests/phase_c_right_to_left.rs`'s own
`rtl_segments_lhs_differs_from_left_to_right_at_the_fst_level` — the same "FST-only, not a
`pg_parse::Morpher` containment check" style `rtl_epenthesis_construction_is_correct_at_the_fst_
level` already uses for a different, unrelated oracle gap.

## What it pins

- `ey`: ROOT1's own correctly-rewritten surface form ("ay" underlying, "a" rewritten to "e" because
  followed by the Segments-authored literal "y").
- `a`: ROOT2's own (unchanged) spelling — no "y" follows at all, so the environment correctly fails
  to match and the rule does not fire.
- `ay`: **`expect_fail: true`** — ROOT1's own RAW, un-rewritten underlying shape, queried directly.
  Since the rule is obligatory wherever its environment matches, this string is NOT a valid surface
  form for ROOT1 at all — proving the rule genuinely fires via the Segments-authored environment.
- `e`/`"y"`: **`expect_fail: true`** each — structurally-invalid strings (no root produces either).

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured by driving `pg_parse::Morpher::parse_word` directly over
every word (a throwaway test, deleted after transcription — see "Verification").

## Verification

Signatures were captured via a throwaway test
(`rust/crates/pg-parse/tests/zz_throwaway_sig_dump.rs`, deleted after transcription) driving
`pg_parse::Morpher::parse_word` directly over every word in `words.yaml`, using the SAME grammar
this directory's `grammar.xml` ships. Cross-checked in-repo by
`rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s `all_discovered_fixtures_match_oracle`
test (dual-root discovery, default `cargo test --workspace` suite) — that test is what actually
gates CI. The capability-gate `ConfirmOnly` verdict is additionally pinned directly by
`rust/crates/pg-foma/src/capability.rs`'s own
`right_to_left_predicate_confirm_only_for_same_table_segments_shaped_rule` unit test, and the
direction-divergence proof (Segments-as-LHS) by `rust/crates/pg-foma/tests/
phase_c_right_to_left.rs`'s `rtl_segments_lhs_differs_from_left_to_right_at_the_fst_level`.

## Founding-oracle verification (update)

hc.dll originally could not even LOAD this grammar: it crashed with a bare `NullReferenceException`.
Root cause, found by reading `XmlLanguageLoader.LoadRewriteSubrule`
(`src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs`): it calls `LoadPhoneticTemplate` for
an `Environment`'s `LeftEnvironment`/`RightEnvironment` with NO `defaultTable` argument (unlike the
main LHS/RHS load path), so `GetTable` resolves to `null` whenever the environment's own `<Segments>`
element omits `characterDefinitionTable` -- contrary to this fixture's original assumption that the
attribute would "default to the pattern's own table," there IS no such default for an
environment-embedded `Segments` node; omitting it crashes the loader. Fixed by setting
`characterDefinitionTable="t1"` explicitly (the grammar's own intent, now made explicit rather than
assumed), with no linguistic content change; the header comment was corrected to describe the actual
mechanism instead of the disproven assumption. Re-verified against the C# founding oracle (hc.dll,
via `hc-conformance.exe` self-check): the `ey` signature matches exactly, and its `rules: []` field
has been filled in from the oracle's own trace (`[prRtlSegEnv]`). `words.yaml`'s header now reads
`oracle-provenance: founding-oracle`.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/right-to-left-segments-environment/`. On acceptance, delete this
staged copy in the same change (graduation guard enforces this mechanically).
