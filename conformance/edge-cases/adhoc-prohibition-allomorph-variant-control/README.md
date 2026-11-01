# adhoc-prohibition-allomorph-variant-control

Synthetic. Control for the morpheme-kind variant cases: an ad hoc ALLOMORPH prohibition between the stem allomorph of a variant entry and a Verb-only prefix allomorph. HCLoader loads the variant once per main MSA, each copy with its own allomorph object (LoadLexEntryOfVariant, HCLoader.cs:782-852), and keys an allomorph prohibition by that allomorph (LoadAllomorphCoOccurrenceRules, HCLoader.cs:2212-2236), so the one MoAlloAdhocProhib [variant allomorph, ta-] becomes one AllomorphCoOccurrenceRule per variant copy and never reaches the main entry's allomorph.

## What it pins

- `la`, `li`: parse as Noun and Verb.
- `tala` (control): the prohibition names the variant allomorph only, so the Verb-only prefix still attaches to the main stem: `TA+LAV` once.
- `tali`: variant allomorph first: the variant's Verb copy is excluded with `ta-`: no analysis. A loader that dropped the prohibition would parse `TA+LAV|tali`; one that spread it to the main entry would also lose `tala`.

Not covered here: morpheme-kind prohibitions over variants (the two enclitic cases and `adhoc-prohibition-variant-msa-copies`).

## Oracle

hc.dll at Machine `aab4c59819adb7b366915e09df2fb28c62b8c03a`, self-check (`hc-conformance --fixtures`) PASS on signatures and traced rules. Provenance: `founding-oracle machine-commit=aab4c59819adb7b366915e09df2fb28c62b8c03a verified-date=2026-10-31`.

## FieldWorks source witness

`fieldworks/project.fwdata` is backed out of `grammar.xml` via LibLCM, not hand-edited: `tools/xample-projector` `author`, with `--msa-copies` and `--variant-of`. FieldWorks 9.3.10.1452, LibLCM 11.0.0.55173.
`author --grammar grammar.xml --name adhocprohibitionallomorphvariantcontrol --msa-copies eLaV,eLaN --variant-of eLiV=eLaV --variant-of eLiN=eLaN`
`phonology-mutations.yaml` `base_sha256` matches the project; the `empty-phoneme-inventory` and `remove-i-only` cases ran through `mutate` (exit 0).

## Verification

FieldWorks project through projector and hc.dll (FieldWorks 3.8.2.0 and the pinned build give the same counts), and `grammar.xml` through `hc-dotnet-wrapper.sh`: la/li 2 each, tala 1, tali 0. Controls (not kept here): no prohibition: `tali` parses once (TA+LAV).

Source: sillsdev/machine #480 (ad hoc prohibition conformance); the PanGloss staging copy is conformance-staging/edge-cases in sillsdev/PanGloss.
