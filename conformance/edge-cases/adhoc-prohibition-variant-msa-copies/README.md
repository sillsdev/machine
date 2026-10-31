# adhoc-prohibition-variant-msa-copies

Synthetic. An ad hoc MORPHEME prohibition on the Verb MSA of a stem entry that has a variant entry (no senses, no MSAs, one variant LexEntryRef to the main entry). HCLoader registers the variant once per main MSA under the MAIN entry's MSA (AddEntry, HCLoader.cs:728-735; LoadLexEntryOfVariant, 782-852) and attaches one rule per copy (LoadMorphemeCoOccurrenceRules, 2262-2290), so the prohibition holds for the variant too. A loader that covered the main entry only still parsed the variant.

## What it pins

- `da`, `di`, `ba`, `bi`, `ga`, `gi`: parse as Noun and Verb.
- `taga`, `tagi` (no prohibition): parse once.
- `tada`, `tadi`: stem-first prohibition on the main Verb MSA: no analysis for main or variant.
- `taba`, `tabi`: prefix-first prohibition, one rule per rest-side copy (main, variant): no analysis.

Not covered: enclitic directions (the projector's author subset has no clitic morph types) and the allomorph-kind variant control.

## Oracle

hc.dll at Machine `aab4c59819adb7b366915e09df2fb28c62b8c03a`, self-check (`hc-conformance --fixtures`) PASS on signatures and traced rules. Provenance: `founding-oracle machine-commit=aab4c59819adb7b366915e09df2fb28c62b8c03a verified-date=2026-10-31`.

## FieldWorks witness

`fieldworks/project.fwdata` is backed out of `grammar.xml` via LibLCM, not hand-edited: `tools/xample-projector` `author`, extended with `--msa-copies` and `--variant-of` (extended with an MSA-copies and variant-entry option; the patch is not part of this repository). FieldWorks 9.3.10.1452, LibLCM 11.0.0.55173.
`author --grammar grammar.xml --name AdhocVariant --msa-copies eDaV,eDaN --msa-copies eBaV,eBaN --msa-copies eGaV,eGaN --variant-of eDiV=eDaV --variant-of eDiN=eDaN --variant-of eBiV=eBaV --variant-of eBiN=eBaN --variant-of eGiV=eGaV --variant-of eGiN=eGaN`
`phonology-mutations.yaml` `base_sha256` matches the project; the `remove-g-only` case ran through `mutate` (exit 0).

## Verification

FieldWorks project through projector and hc.dll: da/di/ba/bi/ga/gi 2 each, taga 1, tagi 1, tada/tadi/taba/tabi 0. Controls (not kept here): no rule: tadi and tabi parse; rule on the main entry only: tadi and tabi parse (TA+DAV, TA+BAV).

Source: PanGloss issues #99 and #100 (fixes in PanGloss PRs #101 and #102).
