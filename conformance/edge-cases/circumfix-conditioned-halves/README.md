# circumfix-conditioned-halves: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: circumfix-conditioned-halves

## Why this fixture exists

Upstream `machine/conformance/edge-cases/circumfix-cross-product-and-infix-drop` pins the bare
2-prefix x 2-suffix circumfix cross-product with NO phonological conditioning on either half. This
fixture is the conditioned counterpart: each half (prefix `pu-`/`ki-`, suffix `-mo`/`-zo`) is gated
by the adjacent stem edge, built the way HCLoader's `IsCircumfix()` path actually builds one
(`HCLoader.cs:1051-1082`): one `MorphologicalSubrule` per prefix-half x suffix-half pairing, with
each half's phonological context embedded as literal mandatory pattern nodes inside the ONE shared
stem `Lhs` (`HCLoader.cs:1289-1290` for the prefix-side first stem segment, `:1298-1299` for the
suffix-side last stem segment) -- never as a `RequiredEnvironments` element, which
`LoadCircumfixAffixProcessAllomorph` never stacks more than once per allomorph
(`HCLoader.cs:1273-1332`, `hcAllo.Environments.Add` at `:1322`).

An earlier version of this fixture encoded the same intent as two separate
`RequiredEnvironments`/`Environment` entries stacked on one `MorphologicalSubrule` (a `Right`-only
prefix-side gate plus a `Left`-only suffix-side gate). That shape has no HCLoader code path at all
and was ruled out by the repository owner; this fixture now pins only the shape FieldWorks can
actually build.

## What it pins

- `aa` (bare root): a plain control.
- `puaamo`: `mrCirc`'s `subVV` (prefix `pu-`, suffix `-mo`) applied to a V-initial, V-final stem
  (`eAA`, "aa") -- both of `subVV`'s embedded stem-pattern gates (first segment `[V]`, last segment
  `[V]`) hold.
- `puabzo`: `subVC` (prefix `pu-`, suffix `-zo`) applied to a V-initial, C-final stem (`eAB`, "ab").
- `kibamo`: `subCV` (prefix `ki-`, suffix `-mo`) applied to a C-initial, V-final stem (`eBA`, "ba").
- `kibbzo`: `subCC` (prefix `ki-`, suffix `-zo`) applied to a C-initial, C-final stem (`eBB`, "bb").
- `pubbmo`/`puabmo`: over-generation controls proving `subVV`'s own embedded first/last stem-pattern
  gates still correctly reject a stem that violates one or both sides (both gates required, not
  "one side is enough").
- `aamo`: a structurally-invalid control (missing the required prefix material entirely).

## Grammar shape

One `MorphologicalRule` (`mrCirc`) with four `MorphologicalSubrule`s, one per
(prefix half, suffix half) combination. Each subrule's `MorphologicalInput` splits the stem into
three `PhoneticSequence` parts: a mandatory first segment carrying a `[V]`/`[C]` `SimpleContext`
gate (the prefix half's condition), an ungated `OptionalSegmentSequence` interior (`min="0"
max="-1"`), and a mandatory last segment carrying its own `[V]`/`[C]` `SimpleContext` gate (the
suffix half's condition). `MorphologicalOutput` wraps the three copied parts with the literal
prefix/suffix `InsertSegments`. This is the literal-HC-XML equivalent of `IsCircumfix()`'s
cross-product build (`HCLoader.cs:1051-1082`, `:1289-1299`): each half's environment is a pattern
node embedded in the one shared stem `Lhs`, never a `RequiredEnvironments` element. Four root
entries (`aa`/`ab`/`ba`/`bb`) cover the four initial/final V-or-C combinations.

## Conversion (2026-09-16)

The prior grammar gated each half via two stacked `RequiredEnvironments`/`Environment` entries on
one `MorphologicalSubrule`. FieldWorks' `HCLoader` never emits that shape
(`LoadCircumfixAffixProcessAllomorph`, `HCLoader.cs:1273-1332`, calls `hcAllo.Environments.Add` at
most once, `:1322`), so the repository owner ruled that encoding, and the rejection it produced for
`puabzo`/`kibamo`/`kibbzo` (via `pg-rules/src/validity.rs`'s disjunctive-allomorph re-check), out of
scope for this fixture.

Rewriting the grammar to embed each half's `[V]`/`[C]` gate directly in the stem pattern (the
`IsCircumfix()`-faithful shape) and re-running against the C# founding oracle flips the outcome for
those three words: the oracle now PARSES `puabzo`, `kibamo`, and `kibbzo` (each satisfies its own
subrule's embedded first/last stem-pattern gates, with no cross-subrule disjunctive-allomorph check
in play, since there is no `RequiredEnvironments` at all in this shape). `pubbmo` and `puabmo`
remain rejected, since neither satisfies `subVV`'s embedded gates on both stem edges. This is the
expected, intended result of the grammar change -- ground truth from the oracle for the new grammar
shape -- not a regression: `words.yaml`'s `parses:` entries for the three flipped words were derived
from `rust/tools/oracle-conformance.ps1 -Propose`'s output against the rewritten grammar.

## Oracle discipline

**Oracle: the C# founding oracle (`hc-conformance.exe`), self-check mode.** Command run:
```
rust\tools\oracle-conformance.ps1 -Propose
```
Against the rewritten grammar, this reported:
```
[FAIL] edge-cases/circumfix-conditioned-halves (14ms) 3/8 word(s) mismatched
    word 'puabzo': expected [] got [CIRC+ROOTAB|puabzo]
    word 'kibamo': expected [] got [CIRC+ROOTBA|kibamo]
    word 'kibbzo': expected [] got [CIRC+ROOTBB|kibbzo]
```
i.e. the C# oracle ITSELF now produces an analysis for those three words (they were previously
`expect_fail: true`). `words.yaml` was updated with the proposed `parses:` entries (`rules: [mrCirc]`,
confirmed by re-running without `-Propose` -- no `declared rules [...] != traced [...]` mismatch was
reported). `pubbmo`/`puabmo` remained rejected and needed no change. The self-check now reports:
```
[PASS] edge-cases/circumfix-conditioned-halves (16ms)
```
with zero divergence for this fixture, and the overall run ends `PASSED` (the one `[FAIL]` line
remaining, `edge-cases/head-ambiguous-compounding`, is a pre-existing baselined
rules-attribution-only divergence unrelated to this fixture). `machine` checkout commit
`f150e2a005ce639f7d68ef17fb0db25b2f6aaa3c`. This fixture's `words.yaml` carries
`# oracle-provenance: founding-oracle`.

## Verification

Signatures for the now-parsing words (`puabzo`, `kibamo`, `kibbzo`) came directly from
`rust/tools/oracle-conformance.ps1 -Propose`'s `# --propose:` block, which runs the C# oracle
against the rewritten grammar. The `rules: [mrCirc]` attribution was confirmed by re-running without
`-Propose` and observing no rules-mismatch report (the harness prints `declared rules [...] !=
traced [...]` when the declared list is wrong). The `expect_fail` words (`pubbmo`, `puabmo`, `aamo`)
were confirmed by the same self-check run producing no mismatch for them.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/circumfix-conditioned-halves/`. On acceptance, delete this staged
copy in the same change (graduation guard enforces this mechanically).
