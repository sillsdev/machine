# compounding-non-recursive: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: compounding-non-recursive

## Why this fixture exists

`openspec/changes/cover-compounding`'s conformance kit item (design.md's own scope: promote
`MorphRuleDef::Compounding`'s non-recursive case from `Disposition::FailClosed` to the
`ConfigPredicate` landing spot — `compounding.non-recursive` → `ConfirmOnly`, once a license-gated
propose shape and a recursion-reachability check both exist). This fixture pins:

1. **A single, non-recursive `CompoundingRuleDef`** compiles and runs correctly end to end (bare
   roots AND compounds), matching `crate::capability::compounding_recursive`'s own characterization
   (a lone rule, `multipleApplication` at its DTD default of 1, is never flagged recursive).
2. **The rule-level MPR gate is group-unaware** (design.md D4, the load-bearing trap): the rule's
   `headProdRestrictionsMprFeatures` is tested with a flat overlap (`MprSet::compound_match`,
   mirroring C#'s `CompoundMprFeaturesMatch`), so a head carrying ONE member of an all-type group
   is admitted. Using the group-aware `Grammar::mpr_group_ok` on this RULE-level field would
   silently REFUSE a stem the engine admits — a genuine recall-loss bug. Both members of the group
   are witnessed (`fasubel` via `mpr1`, `tikubel` via `mpr2`) so the pin does not rest on one
   feature.
3. **A left-to-confirm syntactic-FS gate** (`nonHeadPartsOfSpeech`, design.md D3): a stem MPR-
   licensed as a non-head but disagreeing on part of speech has no valid derivation.

## FieldWorks shape (converted 2026-09-16)

Everything above is what HCLoader emits for a compound rule: `headProdRestrictionsMprFeatures` from
the head MSA's productivity restrictions (HCLoader.cs:1854-1857,1888), `nonHeadPartsOfSpeech` from
the non-head MSA, the features in an `exceptionFeatures`-shaped group (matchType all, outputType
overwrite; HCLoader.cs:168-192) and an Unordered stratum (HCLoader.cs:227). Three things the
fixture used to carry cannot come from a FieldWorks project and were removed: the SUBRULE-level
`HeadMorphologicalInput requiredMPRFeatures` gate (never set by either compounding loader,
HCLoader.cs:1898-1910/1959-1998), the two custom `outputType="append"` groups, and
`morphologicalRuleOrder="linear"`. The group-AWARE subrule gate that made `tikubel` fail is pinned
upstream by `languages/fusional-realizational-morphology` (`mitlav`/`ficlav`); append-versus-
overwrite is pinned upstream by `edge-cases/mpr-overwrite-order-dependence`. `tikubel` therefore
flipped from `expect_fail` to a positive witness — the C# oracle's own ground truth for the new
grammar, not a regression.

## What it pins

- `fasu`/`bel`/`zon`/`numo`/`tiku`: five plain bare-root controls (one per lexical entry), proving
  ordinary lookup is unaffected by the compounding rule's presence.
- `fasubel` (headA `fasu` + nonHeadOk `bel`): headA carries only `mpr1` of the `{mpr1,mpr2}`
  all-type group `headProdRestrictionsMprFeatures` names — admitted by the flat overlap.
- `tikubel` (headB `tiku` + nonHeadOk `bel`): headB carries only `mpr2`, the other member — also
  admitted, so the flat overlap is pinned on both members.
- `fasuzon` (headA + nonHeadBadPos `zon`, `posOther`): **`expect_fail: true`** — MPR-licensed as a
  non-head, but `nonHeadPartsOfSpeech="posHead"` rejects `zon`'s own `posOther`.
- `numobel` (headC `numo`, no MPR features at all, + nonHeadOk `bel`): **`expect_fail: true`** —
  the rule-level gate's negative control, proving `headProdRestrictionsMprFeatures` genuinely
  restricts something rather than vacuously admitting every root.

## Oracle provenance (re-verified 2026-09-16)

`rust/tools/oracle-conformance.ps1` ran the hc-conformance.exe self-check (C# founding oracle,
machine commit f150e2a005ce639f7d68ef17fb0db25b2f6aaa3c) against the converted grammar: PASS,
every word's signature and traced `rules:` list matched. The fixture was first authored against
`pg_parse::Morpher` alone and reconciled against the oracle on 2026-08-31; the same five
load-bearing scenarios are independently proven propose-vs-confirm in
`rust/crates/pg-foma/tests/cover_compounding.rs`, which keeps its own copy of the PRE-conversion
grammar (with the subrule gate) because it tests the engine's group-aware subrule path directly.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/compounding-non-recursive/`. On acceptance, delete this staged copy
in the same change (graduation guard enforces this mechanically).
