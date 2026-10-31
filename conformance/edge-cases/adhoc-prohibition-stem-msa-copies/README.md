# adhoc-prohibition-stem-msa-copies

Synthetic. An ad hoc allomorph prohibition on a stem entry with two stem MSAs (Verb first, Noun second) and a Verb-only prefix ta-. HCLoader attaches the prohibition to every MSA copy of the stem allomorph (HCLoader.cs:2163-2189), in both directions; a loader that kept one allomorph per allomorph identity wired it to the last (Noun) copy only and still parsed the prefixed word through the Verb copy.

## What it pins

- `da`, `ba`, `ga`: parse as Noun and Verb.
- `taga` (no prohibition): parses once, on the Verb copy.
- `tada`: prefix-first prohibition, rest side has both copies of da: no analysis.
- `taba`: stem-first prohibition on each copy of ba: no analysis.

## Oracle

hc.dll at Machine `aab4c59819adb7b366915e09df2fb28c62b8c03a`, self-check (`hc-conformance --fixtures`) PASS on signatures and traced rules. Provenance: `founding-oracle machine-commit=aab4c59819adb7b366915e09df2fb28c62b8c03a verified-date=2026-10-31`.

## FieldWorks witness

`fieldworks/project.fwdata` is backed out of `grammar.xml` via LibLCM, not hand-edited: `tools/xample-projector` `author`, extended with `--msa-copies` and `--variant-of` (extended with an MSA-copies and variant-entry option; the patch is not part of this repository). FieldWorks 9.3.10.1452, LibLCM 11.0.0.55173.
`author --grammar grammar.xml --name AdhocStem --msa-copies eDaV,eDaN --msa-copies eBaV,eBaN --msa-copies eGaV,eGaN`
`phonology-mutations.yaml` `base_sha256` matches the project; the `remove-g-only` case ran through `mutate` (exit 0).

## Verification

Replaying the fieldworks project through `XampleProjector project` (HCLoader.Load + XmlLanguageWriter) and hc.dll: da 2 analyses, ba 2, ga 2, taga 1, tada 0, taba 0, matching grammar.xml. Control with the rules on the last copy only (not kept here): tada parses as TA+DAV, taba as TA+BAV.

Source: PanGloss issues #99 and #100 (fixes in PanGloss PRs #101 and #102).
