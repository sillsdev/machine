# unbounded-iterative-quantifier-expansion: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: unbounded-iterative-quantifier-expansion

## Why this fixture exists

`docs/conformance/representative-typology-basis.md` S1.2.6 identifies the genuinely UNBOUNDED
`PatternNode::Quantifier` (`<OptionalSegmentSequence min max="-1">`, the DTD's own Kleene sentinel)
as an open gap: every other quantifier fixture in this suite (`right-to-left-bounded-quantifier-
rewrite`, and `rust/crates/pg-foma/tests/phase_c_quantifier.rs`'s own containment fixture) exercises
a FINITELY bounded quantifier only. `openspec/changes/build-unbounded-quantifier-support` (tasks.md
4.5) closes that gap in the compiler itself (`rust/crates/pg-foma/src/lower.rs`'s `Slot::Repeat.max`
widened to `Option<u32>`, rendered via foma's own native `E*`/`E^>N` xre operator instead of
`E^{min,max}`); this fixture is the representative conformance witness for the now-supported
construct, per the research doc's own §1.2.6 "Proposed fixture name."

1. **The structural characterization.** `pg-foma::capability::rtl_reversal_construction_attempted`
   (reused verbatim as `QuantifierPatternDetail::compile_attempted`'s own computation) re-runs
   `crate::replace::pattern_slots` over this rule's own LHS/RHS/environment. It now finds the
   genuinely unbounded `OptionalSegmentSequence` (in the subrule's own `RightEnvironment`)
   SUPPORTED -- `compile_attempted == true`.
2. **The capability gate's own verdict.** `QuantifierBoundedExpansionPredicate`
   (`quantifier.bounded-expansion`) returns `ConfirmOnly` for this grammar under BOTH
   `--engine=default` and `--engine=foma` -- verified directly via `pangloss batch` (see
   "Verification" below), never `Refuse`, and no `--allow-unproven` override is needed.
3. **The oracle's own correct, bound-aware behavior.** `pg_parse::Morpher` correctly applies the
   alternation for ANY occurrence count at or above the quantifier's own `min="1"` -- including a
   count (5) well past what any small finite bound would plausibly cover -- and correctly withholds
   it below `min` (0 occurrences). See "What it pins" below.

## What it pins

- `ect`/`ecct`: ROOT1/ROOT2's correctly-rewritten surface forms, exercising exactly 1 and 2
  intervening consonants respectively -- the `min="1"` boundary is genuinely reachable, and one past
  it also matches (distinguishing "1 or more" from an accidental "exactly 1" compile).
- `eccccct`: ROOT3's correctly-rewritten surface form, exercising 5 intervening consonants -- the
  load-bearing GENUINE-unboundedness witness. Finite and unbounded quantifiers use their native
  lowering paths; a compile that silently replaced this unbounded repetition with a small finite
  cutoff would fail here, not merely at the min/min+1 boundary the other two words alone would catch.
  Semantic malformed cases (inverted bounds, empty children, and alpha-nested quantifiers) remain
  unsupported.
- `at`: ROOT4's own surface form, UNCHANGED from its own underlying shape -- ZERO intervening
  consonants, below the quantifier's own `min="1"`, so the environment genuinely fails to match and
  the obligatory rule correctly does not fire. The load-bearing negative witness that the lower
  bound is real, not vacuously satisfied at 0 (mirrors `right-to-left-bounded-quantifier-rewrite`'s
  own below-min control).
- `act`/`acct`/`accccct`: **`expect_fail: true`** each -- ROOT1/ROOT2/ROOT3's own RAW, un-rewritten
  underlying shapes, queried directly as surface strings. Since the rule is obligatory wherever its
  environment matches, these strings are NOT valid surface forms for their respective roots at all
  (proving the rule genuinely fires, rather than being vacuously inapplicable).

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured by running `pangloss batch` directly over every word in
this file, against this directory's own `grammar.xml` -- see "Verification" below.

## Verification

Signatures were captured by building `pg-cli` in release mode and running:

```
pangloss batch conformance-staging/edge-cases/unbounded-iterative-quantifier-expansion/grammar.xml <words.txt> out.tsv --threads 1
```

against a word list covering every entry above, under BOTH `--engine=default` and `--engine=foma`
(no `--allow-unproven` needed for either -- `capability: ConfirmOnly` in both cases). The two
engines' output was byte-identical:

```
0  ect       ROOT1|ect
1  ecct      ROOT2|ecct
2  eccccct   ROOT3|eccccct
3  at        ROOT4|at
4  act       -           (no parse -- expect_fail)
5  acct      -           (no parse -- expect_fail)
6  accccct   -           (no parse -- expect_fail)
```

Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) -- that test is what actually gates CI going forward.

## Founding-oracle verification (update)

Re-verified against the C# founding oracle (hc.dll, via `hc-conformance.exe` self-check): all three
signatures (`ect`/`ecct`/`eccccct`) match exactly, and their `rules: []` fields have been filled in
from the oracle's own trace (`[prIterUnbounded]` for all three). `words.yaml`'s header now reads
`oracle-provenance: founding-oracle`.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/unbounded-iterative-quantifier-expansion/`. On acceptance, delete
this staged copy in the same change (graduation guard enforces this mechanically).
