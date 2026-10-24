# cross-stem-material-determination: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: cross-stem-material-determination

## Why this fixture exists

Makes a formerly unmeasured containment boundary measurable. The backend-routing principle says
the proposer FST may drop long-distance *agreement* that confirm can check, but it must preserve
long-distance *material* determination: a suffix's form depends on which prefix was chosen, not
merely whether the pairing is licensed. A proposer that picks one variant can silently
undergenerate. No existing fixture pins this: `mpr-gated-exception` (upstream)
exercises MPR-feature gating, but with the feature LEXICALLY pre-assigned to a root
(`ruleFeatures=` on the `LexicalEntry`), not RULE-assigned across a derivation, and it gates a
single rule's applicability (agreement/licensing), never a rule's own *choice of allomorph form*.

This fixture's `mrSfx` has two allomorphs, `subSfxKa` (realizes `-ka`) and `subSfxKo` (realizes
`-ko`), gated respectively on `requiredMPRFeatures="mprPfxA"`/`"mprPfxB"` — MPR features assigned
only by `mrPfxA`/`mrPfxB`'s own `MorphologicalOutput MPRFeatures=` (rule-assigned, propagated
across the intervening root via the derivation's accumulated MPR-feature set, not by adjacency).
This is genuinely cross-stem: the prefix and the material-determining suffix are never adjacent in
the surface string, the root sits between them, and the dependency is carried through the
derivation state rather than through any local phonological environment.

## FieldWorks shape (converted 2026-09-16)

This is FieldWorks' "class-changing affix" pattern. Each derivational prefix writes an inflection
class (`ToInflectionClassRA` → `OutMprFeatures`, HCLoader.cs:954-955); `mrSfx` is an inflectional
affix in a template slot whose two allomorphs each name a different `InflectionClassesRC`, added to
that allomorph's `RequiredMprFeatures` alone (HCLoader.cs:1113-1123). The features sit in an
`inflClasses`-shaped group. The fixture was first staged with `mrSfx` on the stratum's own rule
list and `fieldworks_producible: false`; the note behind that verdict cited only the MSA-level
list at HCLoader.cs:966-969 and missed the per-allomorph addend. Moving the rule into the slot
changed no word's signature or traced rules (oracle re-run below).

## What it pins

- `tolu` (bare root): a plain control.
- `patoluka` (valid combination 1): `mrPfxA` ("pa-") assigns `mprPfxA`, then `mrSfx`'s `subSfxKa`
  (requires `mprPfxA`) realizes "-ka".
- `butoluko` (valid combination 2, the mirror branch): `mrPfxB` ("bu-") assigns `mprPfxB`, then
  `mrSfx`'s `subSfxKo` (requires `mprPfxB`) realizes "-ko".
- `patoluko` (INVALID cross-combination, `expect_fail`): `mrPfxA` fires (assigns `mprPfxA` only),
  but "-ko" can only be realized by `subSfxKo`, which requires `mprPfxB` — absent here. No valid
  derivation.
- `butoluka` (INVALID cross-combination, `expect_fail`, the mirror case): `mrPfxB` fires (assigns
  `mprPfxB` only), but "-ka" can only be realized by `subSfxKa`, which requires `mprPfxA` — absent
  here. No valid derivation.

Read together, the two valid and two invalid words are exactly the cross-product the D1 boundary
case names: a proposer FST that factorizes the prefix-side and suffix-side chains independently
(per D1's own routing principle) cannot statically know, while emitting the suffix slot, which
prefix was chosen — so staying recall-safe requires over-proposing BOTH suffix variants for BOTH
prefixes (all four surface strings as candidates), and only confirm can reject the two that have no
real derivation. A proposer that instead "picks one variant" per prefix would silently under-
generate one of the two valid words — the exact failure shape D1 warns about.

## Oracle discipline

**Oracle: the C# founding oracle (`SIL.Machine.Morphology.HermitCrab.Tool`/`hc.dll`), run through
`machine/conformance/adapters/hc-dotnet-wrapper.sh`, cross-checked against `pangloss`
(`pg_parse::Morpher`, driven directly).** `machine/src` was widened into this worktree's sparse
`machine/conformance` checkout for this authoring session only, narrowed back to `conformance`
alone once transcription was done. Both runs agreed on every word's status and signature.

## Verification

C# oracle run (`hc-dotnet-wrapper.sh batch grammar.xml words.txt out.tsv`):

```
0	tolu	99	ok	ROOT|tolu
1	patoluka	44	ok	PFXA+ROOT+SFX|patoluka
2	butoluko	7	ok	PFXB+ROOT+SFX|butoluko
3	patoluko	5	ok	-
4	butoluka	4	ok	-
```

Cross-checked against `pg_parse::Morpher` via the same throwaway `pg-parse` test as
`circumfix-in-template-slot` (`zz_throwaway_transcribe_new_fixtures.rs`, deleted once
transcription was confirmed). Both oracles agree exactly; the C# output above is transcribed
verbatim into `words.yaml`.

`pg.ps1 -Mode test -Package pg-parse -TestTarget conformance_fixtures_gate` confirms this fixture
replays inside the default suite. `pg.ps1 -Mode test -Package pg-foma -TestTarget
faithfulness_coverage_gate -Filter report_faithfulness_coverage -NoNextest` reports this fixture's
own containment outcomes. Isolated per-fixture measurement (a throwaway `pg-foma` test calling
`crate::faithfulness_coverage::observe_fixture_containment` directly, deleted after reporting)
found:

```
=== staging:edge-cases/cross-stem-material-determination ===
kinds: [Affixation, UnorderedMorphRuleApplication, NaturalClassDefinition]
  plan-composed: Held
  tuned-surface-probed: Held
  templated-underlying-tokens: Held
```

All three backends HOLD containment on this construct today — no new failure triple. Read
together with D1's own reasoning, this is itself a meaningful (if less dramatic) result: it
confirms that today's proposers already do the safe thing on this cross-stem material-
determination shape (over-propose rather than silently pick one variant), rather than merely
asserting that they should.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/cross-stem-material-determination/`. On acceptance, delete this
staged copy in the same change (graduation guard enforces this mechanically).

## Oracle provenance (re-verified 2026-09-16)

`rust/tools/oracle-conformance.ps1` ran the hc-conformance.exe self-check (C# founding oracle,
machine commit f150e2a005ce639f7d68ef17fb0db25b2f6aaa3c) against the template-slot grammar:
PASS, every word's signature and traced `rules:` list matched. The "Oracle discipline" and
"Verification" sections above describe how the fixture was originally authored (stratum-level
`mrSfx`), not its current verification status.
