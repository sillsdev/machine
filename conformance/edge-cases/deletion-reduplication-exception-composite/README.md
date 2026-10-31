# deletion-reduplication-exception-composite: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# deletion-reduplication-exception-composite

Generic synthetic HC fixture combining deletion, reduplication repair, and lexical exceptions.
`prDelete` deletes a stop after a nasal introduced by `mrPrefix`; `mrRedup` copies one lexical span twice; and `mrSuffix` is blocked for the marked lexical entry by `excludedMPRFeatures` while the plain control remains productive.

All names, roots, and surfaces are invented and carry no actual-language data. `words.yaml` is oracle ground truth for `pg_parse::Morpher`. The pg-foma integration test builds the baseline and every applicable content-distinct seeded recipe Plan, then compares each result to full-HC identity and multiplicity. Build failures and mismatches are explicit non-certifying evidence.

Graduation target: `machine/conformance/edge-cases/deletion-reduplication-exception-composite/`.

## FieldWorks producibility (converted false -> true, 2026-09-16)

`mrSuf`/`mrSufAlt` moved off the stratum's own `morphologicalRules` list into one Slot of a
posMPR-gated `AffixTemplate` (`sufTemplate`/`sufSlot`), mirroring `mpr-gated-exception`'s own
conversion, so `mrSuf`'s `excludedMPRFeatures="mprException"` is now reachable via
`LoadAffixTemplate`'s slot-blocking loop (HCLoader.cs:1690-1731). `mrRedupFull` and the
deletion/nasal-assimilation rules are untouched.

## Oracle provenance (reconciled 2026-09-16)

`rust/tools/oracle-conformance.ps1` ran `hc-conformance.exe` self-check (C# founding oracle,
machine commit `f150e2a005ce639f7d68ef17fb0db25b2f6aaa3c`) directly against this fixture's
grammar.xml + words.yaml after the conversion above: PASS -- every word's signature and traced
`rules:` list matched, unchanged from the pre-conversion run. The fixture's `words.yaml` carries
`# oracle-provenance: founding-oracle`.
