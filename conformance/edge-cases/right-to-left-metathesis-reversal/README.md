# right-to-left-metathesis-reversal: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: right-to-left-metathesis-reversal

## Why this fixture exists

`docs/conformance/representative-typology-basis.md` S1.2.4 names `Dir::RightToLeft` metathesis as a
genuine gap: `crate::replace::compile_metathesis_rule` used to return `Ok(None)` unconditionally for
any non-`LeftToRight` rule (an honest scope boundary, module doc's "Scope" section, never a wrong
compile). `docs/conformance/needs-decision-resolutions.md` row 8 resolved this **PROVABLE -- build
it**: the same mirror-and-reverse construction `compile_rtl_branch_net` already uses for RTL
*rewrite* rules (`openspec/changes/compile-right-to-left-rewrites`) is not rewrite-specific -- it
operates on `Vec<Slot>` and `Fsm`, and transfers directly. `openspec/changes/
plan-construct-coverage-completion` task 4.6 built it: mirror the pattern (`reversed_slots`), remap
`left_switch`/`right_switch` to their mirrored indices, run the existing swap construction on the
mirror, `fsm_reverse` the result, and `fsm_union` with the plain net -- see `rust/crates/pg-foma/src/
replace.rs`'s module doc ("Metathesis" section) for the full construction and
`crate::capability::MetathesisFaithfulSwapPredicate`'s doc for the disposition.

1. **The structural characterization.** `crate::capability::metathesis_swap_construction_attempted`
   no longer gates on `Dir::LeftToRight` at all (task 4.6's own change) -- the structural admission
   floor (resolvable owning table, `left_switch != right_switch` both in bounds, whole pattern a
   shape `crate::replace::pattern_slots` accepts with no `Slot::Alpha`/`Slot::Repeat` occurrence) is
   now Dir-agnostic, exactly mirroring `rtl_reversal_construction_attempted`'s own convention for
   ordinary rewrite rules. This grammar's own `MetathesisDetail::swap_construction_attempted`
   characterizes `true`.
2. **The capability gate's own verdict.** `MetathesisFaithfulSwapPredicate`
   (`metathesis.faithful-swap-construction`) returns `ConfirmOnly` for this grammar -- the SAME
   verdict `RightToLeftRewriteFaithfulReversalPredicate` gives RTL rewrite rules, for the identical
   reason: the compiled relation (`plain ∪ reverse(mirror)`) is a proven SUPERSET of the true RTL
   relation, sound under propose-and-confirm, never proven exact -- so never `Admit`.
3. **The oracle's own recall**, contained in the FST proposer's candidate set -- see "What it pins"
   below.

## Empirical finding: `pg_rules::metathesis` is direction-blind for an overlapping switch window

While authoring this fixture, a throwaway probe (`rust/crates/pg-foma/tests/
zzz_scratch_metathesis_dir_probe.rs`, deleted after the finding was recorded) declared the SAME
two-adjacent-same-class-switch `MetathesisRule` twice, once `Dir::LeftToRight` and once
`Dir::RightToLeft`, and called `pg_rules::metathesis::synthesize` directly on an OVERLAPPING-window
input ("pqp", where positions 0-1 and 1-2 both match the switch pattern). Both declarations
synthesized identically ("qpp", the LEFTMOST window's swap) -- `pg_rules::metathesis`'s own
`match_candidates` (`rust/crates/pg-rules/src/metathesis.rs`) sorts candidates ascending
(leftmost-first) REGARDLESS of `rule.dir`, and `synthesize_with_pattern`'s application loop always
takes the first (i.e. leftmost) candidate from that sorted list. This is the SAME empirical shape
`rust/crates/pg-foma/tests/phase_c_right_to_left.rs`'s own top doc found (**before** its own fix) for
ordinary `Iterative` rewrite rules -- direction-blind pick-order, not direction-aware.

This grammar's own two lexical entries ("qs"/"rt") each have exactly ONE valid switch window (no
overlap), so the empirical direction-blindness above never affects what THIS fixture's oracle
recalls -- `Dir::RightToLeft` and `Dir::LeftToRight` would give byte-identical oracle behavior for
these two words. That is a deliberate, honest choice: this fixture's job is the Stage-2 containment
obligation (oracle recall ⊆ FST propose), which holds regardless of whether the oracle happens to be
direction-aware or -blind for the words it is asked about. The complementary "the FST construction
itself genuinely differs from compiling as if `LeftToRight`" witness -- which DOES need an
overlapping-window scenario -- lives as a bare-automaton, oracle-free test in `rust/crates/pg-foma/
tests/phase_c_metathesis.rs` (mirroring `tests/phase_c_right_to_left.rs`'s own "aa -> b" worked
example), not in this conformance fixture.

## What it pins

- `sq`/`tr`: ROOT1/ROOT2's correctly-metathesized surface forms (two independent roots, so the
  negative controls below are a genuine cross-contamination check, not merely "no root has this
  shape at all").
- `qs`/`rt`: **`expect_fail: true`** each -- ROOT1/ROOT2's own RAW, un-metathesized underlying
  shapes, queried directly. Since metathesis is obligatory wherever the pattern matches, these are
  NOT valid surface forms for their respective roots (proving the rule genuinely fires).
- `sr`/`tq`: **`expect_fail: true`** each -- the two OTHER combinations of SwitchA={q,r} x
  SwitchB={s,t} this rule's own pattern could match against some root, but neither ROOT1 nor ROOT2
  has this shape. Mirrors `phase_c_metathesis.rs`'s own LeftToRight
  `metathesis_multi_member_classes_transpose_precisely_not_naively` precision witness, now checked
  against the conformance oracle under `Dir::RightToLeft` too.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured by driving `pg_parse::Morpher::parse_word_opts` directly
over every word (a throwaway test, `rust/crates/pg-foma/tests/zz_throwaway_sig_dump.rs`, deleted
after transcription).

## Verification

Signatures transcribed verbatim from the throwaway dump above, using the SAME `grammar.xml` this
directory ships. Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) -- that test is what actually gates CI. The capability-gate `ConfirmOnly` verdict and the
FST-proposer containment (every oracle analysis above is a member of
`crate::replace::compile_metathesis_rule`'s own compiled candidate set) are additionally pinned
directly by `rust/crates/pg-foma/tests/phase_c_metathesis.rs`'s dedicated `Dir::RightToLeft`
containment witness, which re-derives every word's oracle analysis as an explicit regression gate.

## Founding-oracle verification (update)

Re-verified against the C# founding oracle (hc.dll, via `hc-conformance.exe` self-check): both
signatures match exactly, and the `rules: []` fields for `sq`/`tr` have been filled in from the
oracle's own trace (`[mrRtlReversal]` for both). `words.yaml`'s header now reads
`oracle-provenance: founding-oracle`.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/right-to-left-metathesis-reversal/`. On acceptance, delete this
staged copy in the same change (graduation guard enforces this mechanically).
