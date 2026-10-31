# head-ambiguous-compounding: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: head-ambiguous-compounding

## Why this fixture exists

`rust/crates/pg-parse/src/identity.rs`'s `AnalysisIdentity` makes `root_index` — "which morpheme is
the root" — a load-bearing field of an analysis's identity, distinct from `morphemes` (the ordered
sequence) and `category`. The Sena defect that led to `uflexc` (the `PlanComposed` lexicon emitter)
gaining a bounded compound loop (`feat(pg-foma): give uflexc a bounded compound loop`, 97d0ef7) was
first diagnosed as "a compound went missing" and only became precise once root position was
considered: the FST proposer's continuation graph was structurally single-root, so it could never
propose ANY compound, regardless of headedness. The existing RED-1 gate
(`cross_compiler_equivalence_gate.rs::plan_composed_cannot_represent_compounding_construct_red1`,
using `conformance-staging/edge-cases/compounding-non-recursive`) pins the *generic* recall gap that
fix closed, but that fixture has only ONE compounding rule and no headedness ambiguity — nothing in
the existing suite exercises **two analyses of the same word, with the same morpheme sequence and
the same category, differing only in `root_index`**, which is exactly the shape that turned a vague
symptom into a precise diagnosis on the real defect. This fixture closes that gap.

## What it pins

`HeadAmbiguousCompounding`'s grammar has two parts of speech (`posH`, `posN`) and two mirrored,
POS-symmetric `CompoundingRule`s over one lexical entry per POS (`dak`:`posH`, `imo`:`posN`):

- `crLeftHead` (`headPartsOfSpeech="posH"` `nonHeadPartsOfSpeech="posN"` `outputPartOfSpeech="posH"`,
  output copies head-then-nonhead) instantiates to `dak`(head)+`imo`(nonhead) = surface `dakimo`,
  root/head = `dak` (`root_index` 0).
- `crRightHead` (`headPartsOfSpeech="posN"` `nonHeadPartsOfSpeech="posH"` `outputPartOfSpeech="posH"`,
  output copies nonhead-then-head) instantiates to `dak`(nonhead)+`imo`(head) = surface `dakimo`
  (same surface — nonhead is copied first), root/head = `imo` (`root_index` 1).

Both rules fire on the SAME surface order deliberately — that symmetry is the entire point. The
resulting two analyses of `dakimo` share the identical ordered morpheme sequence `[DAK, IMO]` and the
identical output category (`posH`, from both rules' `outputPartOfSpeech`), differing ONLY in
`root_morpheme_index` (0 vs. 1).

- `dak`/`imo`: bare-root controls, one per lexical entry.
- `dakimo`: **the headedness-ambiguity witness** — two analyses, `root_index` 0 and 1.
- `imodak`: **`expect_fail: true`** negative control — both rules always copy material in an order
  that yields `dakimo` (never the reverse), so this surface has no valid derivation. Proves the two
  mirrored rules do not also license the mirror-order surface, keeping the fixture's ambiguity
  confined to headedness alone.

## Single-segmentation verification

The fixture's headedness pin is only clean if `dakimo` has exactly one morpheme split — otherwise a
second segmentation would reintroduce generic ambiguity and the fixture would stop isolating root
position specifically. Verified by construction: the lexicon contains exactly two entries, `dak`
and `imo`, each 3 characters, and no affixation rules exist at all (compounding is the only
morphological construct in this grammar). The only concatenations of the two entries (each used at
most once, per the two subrules' own single-head/single-nonhead shape) are `dak+dak="dakdak"`,
`imo+imo="imoimo"`, `imo+dak="imodak"`, and `dak+imo="dakimo"` — of these four, only `dak+imo`
equals the surface string `dakimo`, and it equals it in only one way (there is no other partition of
the 6-character string `dakimo` into two known lexical entries: `da`/`kimo`, `d`/`akimo`, etc. do not
match either entry). This was additionally confirmed empirically (not just by hand-argument): the
throwaway oracle dump below returned exactly `morpheme_ids=[0, 1]` (`DAK` then `IMO`) for BOTH
`dakimo` analyses — never a different split — and `imodak` returned zero analyses, confirming the
mirror-order surface has no derivation either.

## Why the pin is a Rust assertion, not `words.yaml`

`words.yaml`'s `parses[].signature` field is the flat `BatchCommand`-style string
(`pg_parse::result_signature`: bare `MorphemeId`s joined with `+`, then `|surface`) — it carries no
root marker at all. Both `dakimo` readings here render to the byte-identical string
`"DAK+IMO|dakimo"`, so `words.yaml` can only record that the oracle returns **two** analyses for
`dakimo` (matching this suite's own documented "identical rendered signatures are kept not deduped"
convention — see `pg_parse::result_signature`'s doc comment) — it cannot record that those two
analyses differ in headedness. That would be an ANNOTATION: a human note asserting a distinction
nothing machine-checks, exactly the failure mode this task was written to avoid.

The first-class assertion lives in
`rust/crates/pg-foma/tests/cross_compiler_equivalence_gate.rs`'s
`plan_composed_distinguishes_headedness_ambiguity_red2` (RED-2): it compares deduplicated
`pg_parse::identity::AnalysisIdentity` SETS via `certify_corpus` (`AnalysisIdentity` carries
`root_index`, `morphemes`, and `category` as typed fields), first asserting the oracle itself
retains both `root_index` values (0 and 1) for `dakimo`, then asserting each of the three emission
strategies' own final, oracle-certified candidate set matches — i.e. genuinely offers BOTH readings,
not one duplicated.

## Deliberate choice: hand-built `CandidatePlan`s, not `Registry`/`Applicability`

Like RED-1, RED-2 builds each strategy's `CandidatePlan` directly rather than going through
`Registry::seeded()`/`recipe_registry::Applicability`: this fixture declares no phonological rules
and no templates, so `Applicability::HasPhonologyOrTemplates` would never auto-offer
`TemplatedUnderlyingTokens` for it — that gate controls what the OPTIMIZER auto-proposes, not what a
compiler can legally be asked to build. Sharing one baseline `Plan` across all three strategies is
safe for the same reason RED-1 cites: `recipe_runtime::evaluate_plans_marked_with_cache_mode`'s own
dispatch shows the two whole-grammar strategies (`TunedSurfaceProbed`, `TemplatedUnderlyingTokens`)
ignore the `plan` field entirely, and only `PlanComposed` ever reads it.

## Oracle discipline

**Oracle: the C# founding oracle (hc.dll), via `hc-conformance.exe` self-check.** hc.dll originally
could not even LOAD this grammar: the two `CompoundingRule`s each declared a `PhoneticSequence` with
`id="h0"`/`id="n0"`, and XML's `ID` type requires document-wide uniqueness -- reusing the same two
ids across both rules is a duplicate-ID violation, reported as "The 'id' attribute has an invalid
value according to its data type." Fixed by renaming the second rule's ids to `h1`/`n1`, with no
linguistic content change.

Re-verified against the C# founding oracle: both `dakimo` signature strings match exactly. The
`rules:` attribution CANNOT be verified by `hc-conformance.exe`'s self-check for a structural reason,
not a grammar or transcription defect: both declared parses render to the IDENTICAL signature
"DAK+IMO|dakimo", so the self-check's per-signature rule-attribution comparison
(`Runner.cs`, which groups actual results by signature string and checks EVERY declared parse against
EVERY actual result sharing that signature) always reports exactly one mismatch per declared entry
when two derivations share a signature but have different rule sets -- verified empirically by trying
both possible assignments; neither passes. `words.yaml`'s `rules:` values instead follow this
fixture's own documented semantics (`crLeftHead` ~ `root_index` 0 ~ DAK-headed; `crRightHead` ~
`root_index` 1 ~ IMO-headed), which is independently confirmed by `cross_compiler_equivalence_gate.rs`'s
RED-2 test below. `words.yaml`'s header reads `oracle-provenance: founding-oracle` (the
signatures — the only thing that DOES have a well-defined ground truth here — match); the
known-divergences baseline records this specific tool limitation.

## Verification

Signatures and `root_morpheme_index` values were captured via a throwaway test
(`rust/crates/pg-parse/tests/zz_throwaway_red2_sig_dump.rs`, deleted after transcription) driving
`pg_parse::Morpher::parse_word` directly over `dak`, `imo`, `dakimo`, and `imodak`, using the SAME
grammar this directory's `grammar.xml` ships. Observed output (transcribed verbatim):

```
word="dak" invalid_shape=false signature="DAK|dak"
  [0] morpheme_ids=[0] root_morpheme_index=0 pos_id=Some(0)
word="imo" invalid_shape=false signature="IMO|imo"
  [0] morpheme_ids=[1] root_morpheme_index=0 pos_id=Some(1)
word="dakimo" invalid_shape=false signature="DAK+IMO|dakimo;DAK+IMO|dakimo"
  [0] morpheme_ids=[0, 1] root_morpheme_index=0 pos_id=Some(0)
  [1] morpheme_ids=[0, 1] root_morpheme_index=1 pos_id=Some(0)
word="imodak" invalid_shape=false signature="-"
```

This confirms the fixture's whole premise empirically: `dakimo` yields exactly two analyses, same
`morpheme_ids`, same `pos_id`, `root_morpheme_index` 0 and 1 — and `imodak` yields none.
`plan_composed_distinguishes_headedness_ambiguity_red2` (RED-2, see above) additionally runs all
three `EmissionStrategy` pipelines against this same fixture and reports — **as observed, not
predicted** — that all three retain both readings, matching the oracle's own two-`AnalysisIdentity`
set exactly. The test is committed un-`#[ignore]`d as a green regression guard.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/head-ambiguous-compounding/`. On acceptance, delete this staged copy
in the same change (graduation guard enforces this mechanically).

## Also depended on by task 7.7 (added 2026-08-03)

A SECOND Rust gate now depends on this fixture's `dakimo` witness, alongside
`cross_compiler_equivalence_gate.rs`'s RED-2:
`rust/crates/pg-foma/tests/morphotactics_boundary_cleanup_slice.rs`'s
`root_index_discriminates_two_readings_of_one_surface`, which is task 7.7's falsifier for its
"`root_index` is load-bearing" requirement.

It is **not** one of 7.7's four exercises (those are two template and two cleanup fixtures) — it is
the property witness, used because this is the only staged fixture whose two readings of one surface
agree on morpheme sequence AND category and differ ONLY in root position, which is exactly why no
`words.yaml` signature diff can pin the discrimination. That test asserts both halves: the full
identity relation keeps two members, and the root-BLIND projection of the same set collapses to one,
strictly fewer. It therefore cannot pass if root position is ignored.

Consequence for editing: if `dakimo` ever stops being ambiguous in exactly that way — a grammar edit
that changed either compounding rule's output category, or made the two readings' morpheme sequences
differ — that test fails as a FIXTURE regression, not an engine one.
