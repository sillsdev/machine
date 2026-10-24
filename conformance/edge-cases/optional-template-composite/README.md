# optional-template-composite: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: optional-template-composite

## Why this fixture exists

Mimics the **composite-explosion** pathology named in `docs/conformance-staging-plan.md`'s pathology
catalog, at deliberately miniature scale (8 roots / 3 templates / 6 rules vs. that reference corpus's
855 roots / 47 fusion-eligible rules — see
`docs/fst-plan/morphotactic-composite-pruning.md`'s 2026-07-18 "end-to-end result" section for
the measured finding this pins). The plan doc is explicit that a miniature version is the intended
scope here: "the point is exercising the code paths (pruning automaton, struct composites, fusion
classes), not reproducing the blow-up."

Three sub-pathologies, all mechanically checkable at Morpher-correctness level without needing
foma/emit scale:

1. **Composite-explosion structure**: three `AffixTemplate`s share one `requiredPartsOfSpeech`
   (`posAweti`) on an `Unordered` stratum, mostly-optional slots — the same shared-category idea as
   this repo's own `template-category-sharing` fixture, widened to 3 templates and a genuinely
   mandatory slot.
2. **Vacuous (zero-morph) rules in MANDATORY slots** — the recall trap in the morphotactic-pruning
   automaton that `docs/fst-plan/morphotactic-composite-pruning.md` names directly. `mrVacuous`
   occupies template2's one non-optional slot; its `MorphologicalOutput` is pure `CopyFromInput`,
   changing nothing in the surface string. `monu` (bare root) and `monu` (via template2 with ONLY
   the mandatory `mrVacuous` slot filled) are surface-identical but are two DISTINCT valid analyses.
   An engine/pruning-automaton that treats a silent-output rule as prunable-because-optional-looking
   loses this second analysis outright.
3. **Truncation-shaped rules (structural-composite path)**: `mrTrunc` drops a literal trailing
   segment from the copied stem rather than only appending to it — the same mechanism as
   `machine/conformance/edge-cases/truncate-morphotactic`'s `mruleTruncTrail`.

Plus **non-ASCII multi-codepoint glyphs in root spellings** (the tokenization-bug family): `eToa`'s
root is spelled with U+02BC MODIFIER LETTER APOSTROPHE (ʼ), exercised through a real affixed
derivation (`katoʼata`), not just a bare-tokenization smoke test.

All lexemes (`sipu`, `monu`, `waru`, `toʼa`, `kidi`, `nasu`, `lemo`, `dosi`) are invented; none are
copied from the real (gitignored, uncommitted) reference corpus.

## What it pins

- `monu` has **two** distinct analyses (bare, and template2-with-only-`mrVacuous`) — the load-bearing
  assertion for the vacuous-mandatory-slot recall trap. (Empirically, this generalizes: EVERY
  posAweti root gets this same extra silent analysis via template2 alone, since all three templates
  share one `requiredPartsOfSpeech` — so `sipu`/`waru`/`toʼa`'s own bare-root rows carry two analyses
  each too, not just `monu`'s. `monu` is called out in the pathology description because it is the
  cleanest single illustration, not because it is uniquely affected.)
- `pimonu` / `monuki` / `pimonuki` exercise template2's mandatory slot combined with each optional
  slot independently and together.
- `warmo` / `warmolu` exercise the truncation-shaped rule alone and combined with an ordinary suffix.
- `toʼa` / `katoʼata` exercise the non-ASCII glyph bare and through a real derivation.

## A finding this fixture required: templates must be made self-contained

An earlier draft listed every rule (`mrOpt1`...`mrOpt5`, `mrVacuous`, `mrTrunc`) in the Stratum's own
`morphologicalRules=` attribute, in addition to each rule's own template `Slot`. Measured
consequence: templates could recursively cross-compose (the same engine behavior found independently
while authoring `template-category-sharing` — see that fixture's own `STAGING.md` section on
this for the full mechanism citation). Concretely, `mrOpt3` (nominally "template2's own slot 1")
turned out to also be reachable as a free-standing rule WITHOUT template2's mandatory `mrVacuous` at
all, and several affixed words (`kasiputa`, `katoʼata`) produced the SAME signature two, four, or
eight times over (genuinely distinct internal derivation orders that happen to serialize identically)
— noise that would have buried this fixture's actual pin under an unrelated, harder-to-explain
phenomenon. Fixed by omitting the Stratum's `morphologicalRules=` attribute entirely (see
`grammar.xml`'s own `Stratum` comment) so every rule is reachable ONLY through its own template.
Re-verified: every affixed word now has exactly one signature; only the deliberately-bare-root words
(where template2's silent slot genuinely is a second, distinct, intended analysis) carry two.

## What this fixture deliberately does NOT attempt

This fixture pins Morpher-level (`pg_parse`) parse correctness only. It does **not** reproduce or
assert on the actual composite-generation/pruning-automaton COST behavior (`pg_foma::emit`,
`pg_foma::morphotactics`) that motivated the real reference-corpus investigation — doing so at any
representative scale would defeat the point of a small, committed, non-ignored, always-green fixture.
A future `pg-foma`-level test loading this same `grammar.xml` to exercise `emit()`/pruning directly
(checking it doesn't crash and reports the expected `Tier`) would be a natural, separately-scoped
follow-up; not built as part of this task (see the task's final report for what was and wasn't done).

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured by driving `pg_parse::Morpher::parse_word` directly (a
throwaway in-repo test — see "Verification" below). Per `docs/conformance-staging-plan.md`'s
oracle-discipline note, machine acceptance must re-verify against the C# founding oracle before
graduation.

## Verification

Signatures were captured via a throwaway test driving `pg_parse::Morpher::parse_word` directly over
every word in `words.yaml` (equivalent to `pangloss batch grammar.xml words.txt out.tsv`'s signature
column, without needing a release build of the `pg-cli` binary — a from-scratch release build in
this task's environment took over 30 minutes under heavy concurrent load and was abandoned in favor
of a debug-profile `pg-parse` test driving the same engine). Output transcribed into `words.yaml`
above. Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) — that test is what actually gates CI; the throwaway dump test was deleted after transcription.

## Founding-oracle verification (update)

hc.dll originally could not even LOAD this grammar: `mrTrunc`'s `MorphologicalInput` had a second
`PhoneticSequence` (the truncated `<Segment segment="cU" />`) with no `id` attribute. hc.dll's own
`AnalysisMorphologicalTransform` constructor builds `lhs.ToDictionary(p => p.Name)`, and while a
single `null`-named entry does not itself collide, the surrounding compile path
(`AffixTemplate.CompileAnalysisRule` -> `AnalysisAffixTemplateRule`) threw with
"Could not compile affix template named template3" for this shape; giving that `PhoneticSequence` an
explicit `id="stemTruncDrop"` (matching `machine/conformance/edge-cases/truncate-morphotactic`'s own
convention of naming every part of a multi-part `MorphologicalInput`, even ones never referenced by
`MorphologicalOutput`) fixed it, with no linguistic content change. Re-verified against the C#
founding oracle (hc.dll, via `hc-conformance.exe` self-check): every signature in this fixture
matches exactly, including the `monu`/vacuous-slot double analysis and the truncation words --
`words.yaml`'s header now reads `oracle-provenance: founding-oracle`.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/optional-template-composite/`. On acceptance, delete this staged
copy in the same change (graduation guard enforces this mechanically).

## Also depended on by task 7.7 (added 2026-08-03)

This is **complete-template exercise 2** of the first `Morphotactics -> BoundaryCleanup` vertical
slice, `rust/crates/pg-foma/tests/morphotactics_boundary_cleanup_slice.rs` (task 7.7 of
`openspec/changes/cleanup-and-recipe-parity`). It replaced `recipe-template-generic`, which the
dossier originally named but which ABORTS the test process and so cannot host a gate at all — see
`docs/fst-plan/subrecipes/morphotactics.md`'s exercise 2 for that finding.

Its load-bearing row there is `monu`: the bare root and the mandatory-but-silent `mrVacuous` slot must
be TWO distinct identities of one surface, distinguished by their morpheme sequence. The gate also
pins the whole-fixture shape — the silent slot doubles exactly the four bare roots and nothing else.

That gate reads every expected count OUT OF the `parses:` rows in this directory's `words.yaml` — it
hand-derives nothing — so editing a word entry here changes what it asserts. If you add, remove, or
re-count a `parses:` row, re-run that gate as well as `conformance_fixtures_gate`.
