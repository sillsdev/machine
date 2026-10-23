# circumfix-in-template-slot: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: circumfix-in-template-slot

## Why this fixture exists

Pins a formerly unmeasured containment hole identified during backend-routing review. No existing
fixture puts a genuinely circumfix-shaped `MorphologicalRule` (a subrule whose output is
`[InsertSegments, CopyFromInput, InsertSegments]`, classifying `Role::CircumfixPrefix` per
`crate::emit::rule_role`/`classify_affix`) inside an `<AffixTemplate>` `<Slot>`:
`suffixing-vowel-harmony` keeps its own circumfix off-template, on a different part of speech, and
`fusional-realizational-morphology`'s `mrCircumfixGeT` is loose/unordered at the Stratum level,
never template-referenced. This fixture's `mrCircum` is reachable ONLY through
`templateMain`'s `slotMain` (deliberately NOT listed in the Stratum's own `morphologicalRules=`
attribute — `template-category-sharing`'s own proven finding is that Slot membership alone does
not create exclusivity; omitting the Stratum-level listing is what does).

The fixture exposed two implementation assumptions that are now pinned by regression tests:
template slots must expand a circumfix into both insertion halves, and structural classification
must inspect every allomorph rather than only the first. The latter matters when an ordinary
allomorph precedes a circumfix allomorph in the same rule.

## What it pins

- `lodi` (bare root): a plain control — `slotMain` is optional, so the bare root is valid with no
  slot filled.
- `talodien` (`mrCircum` occupying `slotMain`): the load-bearing word — a circumfix rule (`ta-...-
  en`) reachable only through the template. Before the fix, `TemplatedUnderlyingTokens` offered no
  matching proposal while the other two backends held. It now holds on all three backends because
  the template-slot chain preserves both circumfix insertions.
- `mulodi` (`mrOrdPfx` occupying the SAME slot): the control proving `templateMain` itself works
  correctly on every backend, including tuned, when the occupying rule is an ordinary
  (non-discontinuous) affix rather than a circumfix.
- `enlodi` (negative control, `expect_fail`): the circumfix's trailing material alone, with no
  matching leading piece — no subrule in `slotMain` produces this shape (a circumfix's two pieces
  are inseparable within one subrule application), so it has no valid derivation. Pins that the
  grammar doesn't over-generate a "half a circumfix" analysis.

## Oracle discipline

**Oracle: the C# founding oracle (`SIL.Machine.Morphology.HermitCrab.Tool`/`hc.dll`), run through
`machine/conformance/adapters/hc-dotnet-wrapper.sh`, cross-checked against `pangloss`
(`pg_parse::Morpher`, driven directly).** `machine/src` (the oracle's own source) was widened into
this worktree's sparse `machine/conformance` checkout for this authoring session only
(`git -C machine sparse-checkout set conformance src`, narrowed back to `conformance` alone once
transcription was done — see the conformance-grammars skill's own oracle-discipline section), and
`dotnet build machine/src/SIL.Machine.Morphology.HermitCrab.Tool` succeeded (dotnet 10.0.302). Both
runs agreed on every word's status and signature.

## Verification

C# oracle run (`hc-dotnet-wrapper.sh batch grammar.xml words.txt out.tsv`):

```
0	lodi	99	ok	ROOT|lodi
1	talodien	27	ok	CIRCUM+ROOT|talodien
2	mulodi	2	ok	ORDPFX+ROOT|mulodi
3	enlodi	1	ok	-
```

Cross-checked against `pg_parse::Morpher` via a throwaway `pg-parse` test
(`zz_throwaway_transcribe_new_fixtures.rs`, deleted once transcription was confirmed), run via
`pg.ps1 -Mode test -Package pg-parse -TestTarget zz_throwaway_transcribe_new_fixtures`. Both
oracles agree exactly; the C# output above is transcribed verbatim into `words.yaml`.

`pg.ps1 -Mode test -Package pg-parse -TestTarget conformance_fixtures_gate` confirms this fixture
replays inside the default suite. `pg.ps1 -Mode test -Package pg-foma -TestTarget
faithfulness_coverage_gate -Filter report_faithfulness_coverage -NoNextest` reports this fixture's
containment outcomes; the gate's own `FaithfulnessRequirement::NonVacuity` reads failures without
breaking the build on them. Isolated per-fixture measurement (a throwaway `pg-foma` test calling
`crate::faithfulness_coverage::observe_fixture_containment` directly, deleted after reporting) found:

```
=== staging:edge-cases/circumfix-in-template-slot ===
kinds: [Affixation, UnorderedMorphRuleApplication, NaturalClassDefinition]
  plan-composed: Held
  tuned-surface-probed: Held
  templated-underlying-tokens: Failed { word: "talodien", detail: "word \"talodien\": oracle identity (morphemes=[0, 2], root_index=1) required multiplicity 1, proposal set offered 0" }
```

The block above records the pre-fix measurement. After the fix, no containment-failure triple is
expected for this fixture; the committed regression test asserts the proposal directly.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/circumfix-in-template-slot/`. On acceptance, delete this staged
copy in the same change (graduation guard enforces this mechanically).

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.
