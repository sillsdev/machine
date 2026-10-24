# recursive-endocentric-compounding: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: recursive-endocentric-compounding

## Why this fixture exists

`docs/conformance/representative-typology-basis.md` S1.2.1 identifies recursive/self-feeding
endocentric compounding as a genuine gap: no fixture anywhere exercises a `CompoundingRuleDef` whose
own output part-of-speech re-enters its own input part-of-speech set (unlike
`conformance-staging/edge-cases/compounding-non-recursive`, which deliberately keeps input/output PoS
disjoint and stays capped at `multipleApplication`'s DTD default of 1). This fixture pins:

1. **The structural characterization.** `pg-foma::capability::compounding_recursive`'s rule-graph
   reachability pass marks a `CompoundingRuleDef` "recursive" purely from `max_apps > 1` (this
   fixture's `cr1` declares `multipleApplication="9"`) -- a fact about the RULE DEFINITION, true
   regardless of whether any actual word in this fixture exercises 3+ levels of nesting.
2. **The capability gate's honest, unconditional Refuse.** `CompoundingRecursionSafePredicate`
   (`compounding.non-recursive`) Refuses this grammar via `evaluate_capability`/`compose_envelope`,
   citing `compounding.recursive` -- `crate::emit::compound_license`'s license-gated propose shape
   is only proven for the non-recursive case (design.md D2 item 3's own scope cut).
3. **An independently-discovered oracle resource ceiling.** `pg_parse::Morpher::parse_word_opts`
   hardcodes `AnalyzerConfig::max_stem_count = 2` (mirroring C#'s `Morpher.MaxStemCount` default,
   `rust/crates/pg-rules/src/stratum.rs`'s own doc: "at most one non-head may ever be split off, so a
   word can be de-compounded once, never recursively re-compounded"). This means the STANDARD oracle
   this fixture's own ground truth is drawn from ALSO cannot confirm a genuinely 3-stem self-feeding
   compound today, entirely independently of the FST capability gate's own Refuse verdict. Both
   layers currently refuse the same construct, for different, independently-verifiable reasons -- see
   `words.yaml`'s `tevimaflisra` entry for the empirical finding.

## What it pins

- `tevi`/`mafl`/`isra`: three plain bare-root controls, proving ordinary lookup is unaffected.
- `tevimafl`/`maflisra`: two depth-1 (single-application) compounds, proving the self-feeding-CAPABLE
  rule shape (`headPartsOfSpeech`/`nonHeadPartsOfSpeech`/`outputPartOfSpeech` all = `posRoot`) still
  works completely normally at the non-recursive depth `compounding-non-recursive` already covers.
- `tevimaflisra`: **`expect_fail: true`** -- the load-bearing recursive/self-feeding witness. Zero
  analyses from `pg_parse::Morpher`, for the resource-ceiling reason above -- NOT hand-asserted, see
  "Verification" below.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured by driving `pg_parse::Morpher::parse_word_opts` directly
over every word (a throwaway test, deleted after transcription -- see "Verification").

## Verification

Signatures were captured via a throwaway test (`rust/crates/pg-foma/tests/zz_throwaway_sig_dump.rs`,
deleted after transcription) driving `pg_parse::Morpher::parse_word_opts` directly over every word in
`words.yaml`, using the SAME grammar this directory's `grammar.xml` ships. The `tevimaflisra` = zero
analyses finding was cross-checked against `rust/crates/pg-rules/src/stratum.rs`'s own
`AnalyzerConfig::max_stem_count` field doc, which independently confirms the mechanism (a `>= 2`
non-head-count gate on any further compounding-rule application) -- not merely an unexplained empirical
zero. Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) -- that test is what actually gates CI. The capability-gate Refuse verdict (structural, word-
independent) is additionally pinned directly by `rust/crates/pg-foma/tests/
cover_recursive_endocentric_compounding.rs`, which asserts `evaluate_capability` returns
`CompileDecision::Refuse` naming `Compounding`/`compounding.recursive`, and separately re-asserts the
oracle's own `tevimaflisra` = zero-analyses finding as an explicit regression gate -- this is the test
that should FAIL (prompting deliberate review, not silent staleness) the day either layer is promoted
to actually support this construct.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/recursive-endocentric-compounding/`. On acceptance, delete this staged
copy in the same change (graduation guard enforces this mechanically).

## G11 addendum (2026-07-25)

The `AnalyzerConfig::max_stem_count = 2` ceiling described above turned out to be a **faithful
default, not a permanent wall**: C#'s own `Morpher.MaxStemCount` (`Morpher.cs:72`) is a settable
per-instance property (ctor default `2`, `Morpher.cs:56`) that C#'s own
`CompoundingRuleTests.SimpleRules` (cs:87,105) raises to `3` to confirm a genuine 3-root compound.
`pg_parse::Morpher` previously hardcoded `2` with no way for any caller to raise it -- that missing
knob (not the value `2` itself) was the actual gap; see `pg-parse/src/morpher.rs`'s
`Morpher::with_max_stem_count`. This fixture's own ground truth is UNCHANGED: it still drives the
*default* `Morpher::new` (no override), so `tevimaflisra`'s zero-analyses pin above remains accurate
and was not touched. A genuine 3-stem compound of this exact shape DOES confirm today if a caller
opts in via `.with_max_stem_count(3)` -- pinned not here (this fixture is about the default oracle),
but in `rust/crates/pg-parse/tests/csharp_port_compounding.rs`'s
`simple_rules_4_three_root_compound_single_rule`/`simple_rules_5_three_root_compound_two_rules`,
which port C#'s own previously-omitted `MaxStemCount = 3` reconfiguration directly.

## Task 4.1 addendum (2026-07-27, `openspec/changes/plan-construct-coverage-completion`)

Design.md row 2 asked for three things against `Compounding`'s recursive split: (1) bound the
self-feeding depth, (2) a depth-budgeted faithful cross-product construction, (3) a no-false-negative
containment proof. (1) is DONE: `pg_foma::capability::compounding_max_depth` (`CompoundingDetail::
max_depth`) turns the boolean `recursive` flag this fixture already pins into an exact, always-finite
stem-count bound -- for `cr1` (`multipleApplication="9"`) that bound is exactly **10 stems**
(`1 + 9`), now asserted directly in the capability gate's own `Refuse` diagnostic text (`rust/crates/
pg-foma/tests/cover_compounding_recursive_depth_bound.rs::
capability_gate_diagnostic_reports_the_computed_depth_bound`).

(2)/(3) do NOT close, for a checked, structural reason, not an unproven one: `crate::emit`'s "bounded
compound loop" -- the ONLY compiled FST construction that exists for `Compounding` today, non-recursive
included -- hardcodes exactly ONE extra root regardless of any rule's `max_apps`/computed `max_depth`.
`rust/crates/pg-foma/tests/cover_compounding_recursive_depth_bound.rs::
unmodified_compound_loop_cannot_propose_the_bounded_recursive_shape` proves this directly against the
REAL production `FomaProposer` (not merely argued): it proposes zero candidates for `tevimaflisra`,
while the same compiled network still proposes the ordinary depth-1 compounds fine. The companion test
`raised_cap_oracle_finds_the_recursive_analysis_confirm_at_default_would_miss` makes the comparison
non-vacuous exactly per this task's own caveat: at the DEFAULT `max_stem_count` (2), confirm also
returns zero, so a containment check at the default would be vacuously true (0 subset-of 0, proving
nothing); raised to 3 (`Morpher::with_max_stem_count(3)`, mirroring C#'s own
`CompoundingRuleTests.SimpleRules` reconfiguration), confirm genuinely accepts exactly one analysis --
making the proposer's zero-candidate result a REAL, checked recall gap. Promoting
`compounding.recursive` to `ConfirmOnly` today would therefore be a genuine false negative, not a
permanent carve-out and not a resource-ceiling carve-out (design.md row 3's `unordered-application.
unbounded` shape) -- exactly the vacuous-promotion trap this task's own brief warns against.

This fixture's own ground truth (`tevimaflisra` = zero analyses at the default oracle) remains
byte-for-byte unchanged and was not touched by this task -- only the capability-gate diagnostic text
gained the computed depth number, and this task's own `cover_compounding_recursive_depth_bound.rs`
test file was added alongside `cover_recursive_endocentric_compounding.rs` (not merged into it, to
keep the pre-existing regression gate's own diff minimal). Extending `crate::emit`'s "bounded compound
loop" to actually consume `max_depth` (the construction piece 2/3 need) is out of this task's own
file-ownership scope (`crate::emit` was explicitly excluded from task 4.1's assignment) -- a follow-on
task's job, now with an exact precomputed bound ready to consume via `crate::compose_budget::
ComposeBudget::check_chain_depth`'s existing mechanism the moment that construction exists.

## Task 4.1 pieces 2/3 addendum (2026-07-27, `openspec/changes/plan-construct-coverage-completion`)

The follow-on task the addendum above named is now done. `crate::emit`'s "bounded compound loop"
(that module's own doc) no longer hardcodes exactly one extra root: `build_compound_chain` unrolls
`max_depth - 1` extra (non-head) root LEVELS, consuming `crate::capability::compounding_max_depth`'s
precomputed bound directly (one source of truth -- the construction never re-derives it). For THIS
fixture's `cr1` (`multipleApplication="9"`, bound 10 stems), the real, unmodified, production
`FomaProposer` now proposes at least one candidate for `tevimaflisra` -- the exact
ROOT1+ROOT2+ROOT3 sequence, never a spurious one
(`rust/crates/pg-foma/tests/cover_compounding_recursive_depth_bound.rs::
depth_budgeted_compound_loop_now_proposes_the_bounded_recursive_shape`, renamed from
`unmodified_compound_loop_cannot_propose_the_bounded_recursive_shape`, which this comment's earlier
reference to by that name now supersedes). Containment against the raised-cap oracle
(`Morpher::with_max_stem_count(3)`, the same non-vacuous reconfiguration the earlier addendum already
established) is now proven directly, not merely argued:
`depth_budgeted_compound_loop_contains_the_raised_cap_oracle_analysis` checks propose's candidate set
contains the EXACT morpheme-id sequence the raised-cap oracle independently accepts.

`crate::capability::CompoundingRecursionSafePredicate` now reaches `ConfirmOnly` UNCONDITIONALLY for
every observed `Compounding` rule, recursive or not (no more `compounding.recursive`-vs-`compounding.
non-recursive` split at the verdict level -- both land `ConfirmOnly`, mirroring
`MprGroupAppendNonNarrowingPredicate`'s own "no further split" shape). This fixture's own capability
verdict therefore flips from `Refuse` to `ConfirmOnly`
(`rust/crates/pg-foma/tests/cover_recursive_endocentric_compounding.rs::
capability_gate_is_confirm_only_for_recursive_compounding_shape`, renamed from
`capability_gate_refuses_recursive_compounding_shape` -- that test's own prior doc predicted exactly
this: "this is the test that should FAIL... the day either layer is promoted", and it did, so it was
re-authored rather than deleted, per this crate's own convention for a superseded regression pin).

**What did NOT change:** the ORACLE layer. `pg_parse::Morpher`'s own `max_stem_count` default (2) was
never touched by this task -- `genuinely_recursive_three_stem_compound_currently_confirms_zero_
analyses` still pins the real, current, unchanged default-oracle behavior (`tevimaflisra` = zero
analyses at the default cap) and needed no edit at all. Promoting `compounding.recursive` to
`ConfirmOnly` is therefore honest, not vacuous, precisely because the raised-cap containment test
above exercises a REAL non-empty oracle-accepted set, not the (still-true, still-zero) default one.

**Cost stays separate, checked, and honest.** A `multipleApplication` value far beyond the DTD's
practical ceiling (9) would make `max_depth`/`compound_extra_levels` enormous; `crate::emit::
DEFAULT_COMPOUND_CHAIN_DEPTH_BUDGET` (200, its own dedicated dimension -- deliberately NOT
`ComposeBudget::chain_depth_cap`'s shared field, see that constant's own doc for why) refuses such a
grammar with a typed `FomaTier::Unsupported` outcome, checked BEFORE any lexc text is written, never a
hang or an OOM (`cover_compounding_recursive_depth_bound.rs::
compound_chain_depth_budget_trips_before_any_lexc_emitted`, a synthetic 60,000-`multipleApplication`
grammar, not this staged fixture -- this fixture's own bound (10) sits comfortably under the default
budget). A separate test (`depth_bound_is_respected_a_k_plus_one_stem_word_is_never_proposed`, its own
small synthetic fixture with an exact k=3 bound) proves the over-approximation stops exactly at the
computed bound: a k-stem word proposes, a (k+1)-stem word never does.

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.
