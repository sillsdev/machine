# right-to-left-bounded-quantifier-rewrite: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: right-to-left-bounded-quantifier-rewrite

## Why this fixture exists

`docs/conformance/representative-typology-basis.md` S1.2.2 identifies additional in-scope
`RightToLeftRewrite` pattern shapes as a genuine gap: the 3 already-covered RTL fixtures
(`rtl_plain_rule`/`rtl_feature_environment_swap`/`rtl_deletion`, per design.md's own citation) never
exercise a `PatternNode::Quantifier` node anywhere in the rule's own LHS/RHS/environment. This
fixture pins the `Quantifier`-in-environment case specifically (directional stress/harmony rule
scanning with a bounded lookahead window, per the metrical-stress-typology citations in the research
doc):

1. **The structural characterization.** `pg-foma::capability::rtl_reversal_construction_attempted`
   re-runs `crate::replace::pattern_slots` over this rule's own LHS/RHS/environment. Contrary to
   this fixture's ORIGINAL premise (see "A correction" below), it finds the bounded
   `OptionalSegmentSequence` (in the subrule's own `RightEnvironment`) SUPPORTED --
   `RightToLeftRewriteDetail::reversal_construction_attempted == true`.
2. **The capability gate's own (already-correct) `ConfirmOnly` verdict.**
   `RightToLeftRewriteFaithfulReversalPredicate`
   (`right-to-left-rewrite.faithful-reversal-construction`) returns `ConfirmOnly` via
   `best_case_across_backends_for_grammar` for this grammar -- NOT `Refuse`.
3. **The oracle's own correct, bound-aware behavior.** `pg_parse::Morpher` correctly applies the
   alternation exactly up to (and not beyond) the bounded quantifier's own `max="2"` -- see "What it
   pins" below.

## A correction to the research doc's own premise

The earlier research premise grouped `Quantifier` with other RTL gaps, but the
current lowering accepts both bounded and genuinely unbounded alpha-free quantifiers in RTL
patterns. This fixture's bounded-in-environment shape therefore has the expected `ConfirmOnly`
verdict, not `Refuse`, and is a useful conformance witness that the already-correct RTL
propose-and-confirm pipeline handles quantifier environments. The quantifier case is closed; any
remaining RTL pattern-shape gaps are separate from this fixture.

## What it pins

- `acet`/`ecct`: ROOT1/ROOT2's correctly-rewritten surface forms, exercising 0 and (saturating) 2
  intervening consonants respectively -- the bound is genuinely reachable, not vacuously satisfied
  only at 0.
- `accct`: ROOT3's surface form, UNCHANGED from its own underlying shape -- 3 intervening consonants
  is one past the bound, so the obligatory rule correctly does NOT fire. The load-bearing negative
  witness that the quantifier is genuinely bounded, not silently unbounded.
- `acat`/`acct`: **`expect_fail: true`** each -- ROOT1/ROOT2's own RAW, un-rewritten underlying
  shapes, queried directly as surface strings. Since the rule is obligatory wherever its environment
  matches, these strings are NOT valid surface forms for their respective roots at all (proving the
  rule genuinely fires, rather than being vacuously inapplicable).

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured by driving `pg_parse::Morpher::parse_word_opts` directly
over every word (a throwaway test, deleted after transcription -- see "Verification").

## Verification

Signatures were captured via a throwaway test (`rust/crates/pg-foma/tests/zz_throwaway_sig_dump.rs`,
deleted after transcription) driving `pg_parse::Morpher::parse_word_opts` directly over every word in
`words.yaml`, using the SAME grammar this directory's `grammar.xml` ships. An early all-featureless
draft (matching `subrule-morphosyntactic-gating`'s own identical finding) produced bracket-collapsed
signatures until a fully-specified `PhonologicalFeatureSystem` was added -- see grammar.xml's own G2a
comment. Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) -- that test is what actually gates CI. The capability-gate `ConfirmOnly` verdict is
additionally pinned directly by `rust/crates/pg-foma/tests/
cover_right_to_left_bounded_quantifier_rewrite.rs`, whose
`capability_gate_confirms_only_for_bounded_quantifier_in_rtl_environment` asserts
`best_case_across_backends_for_grammar` returns `CompileDecision::ConfirmOnly`, and which separately
re-derives every word's oracle analysis as an explicit regression gate -- this is the test that
should FAIL (prompting deliberate review) the day this shape either regresses to `Refuse` or is
promoted to `Admit`.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/right-to-left-bounded-quantifier-rewrite/`. On acceptance, delete this
staged copy in the same change (graduation guard enforces this mechanically).

## Founding-oracle verification (update)

Re-verified against the C# founding oracle (hc.dll, via `hc-conformance.exe` self-check): both
signatures match exactly, and the `rules: []` fields for `acet`/`ecct` have been filled in from the
oracle's own trace (`[prRtlBq]` for both). `words.yaml`'s header now reads
`oracle-provenance: founding-oracle`.

## Coverage-tag correction (post-G9)

`constructs.txt` row 30 (`sillsdev/machine` PR #465, "G9") added
`"RewriteRule direction (Dir): right-to-left"` as this construct's own dedicated row. `words.yaml`'s
`exercises:` entries here previously read the bare characteristic name `"RightToLeftRewrite"`, which
is NOT a `constructs.txt` row id and therefore matched nothing in
`conformance_coverage::construct_ids_for`'s byte-for-byte cross-check -- the tag silently contributed
zero coverage despite this fixture genuinely exercising the construct. Fixed to the exact row-30
string; no signature, `parses:`, or ground truth changed.
