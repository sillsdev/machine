# adhoc-prohibition-affix-environment-copies

Synthetic. An ad hoc allomorph prohibition on a prefix allomorph that has two environments and two equal MoInflAffMsas (MergeMSAs off). HCLoader registers one allomorph per MSA per environment (HCLoader.cs:1160-1178), so the one FieldWorks allomorph compiles to four copies (`subTa1a`, `subTa1b`, `subTa2a`, `subTa2b`), and the prohibition is attached to every copy, on the first side and on the rest side (HCLoader.cs:2212-2236). The model is PanGloss's `adhoc-affix-environment-copies` fixture (PR #102).

## What it pins

- `da`, `ba`, `ga`: parse.
- `taga` (no prohibition): two analyses, one per prefix MSA, both through the before-consonant environment (the after-`a` environment never matches a word-initial prefix).
- `tada`: prefix first, one rule per copy: no analysis. Only the before-consonant copies match, so a rule on the first environment copy alone does not suppress it.
- `taba`: stem first, one rule per copy on the rest side: no analysis.

## Oracle

hc.dll at Machine `aab4c59819adb7b366915e09df2fb28c62b8c03a`, self-check (`hc-conformance --fixtures`) PASS on signatures and traced rules. Provenance: `founding-oracle machine-commit=aab4c59819adb7b366915e09df2fb28c62b8c03a verified-date=2026-10-30`.

## FieldWorks witness

`fieldworks/project.fwdata` is backed out of `grammar.xml` via LibLCM, not hand-edited: `tools/xample-projector` `author`, extended (extended with MSA-copies, environment-copies, circumfix and variant-entry options; the patch is not part of this repository) with `--msa-copies`, `--env-copies`, `--variant-of`, circumfix rules and `--half-prohib`. FieldWorks 9.3.10.1452, LibLCM 11.0.0.55173.
`author --grammar grammar.xml --name AdhocEnv --msa-copies mrTa1,mrTa2 --env-copies subTa1a,subTa1b --env-copies subTa2a,subTa2b   (ParserParameters gets MergeMSAs=false)`
The authored project has one prefix allomorph with two PhoneEnvRC environments, two MoInflAffMsas, and two MoAlloAdhocProhibs (one per direction). Loaded back through HCLoader it emits the same four copies per MSA pair and eight AllomorphCoOccurrenceRules.
`phonology-mutations.yaml` `base_sha256` matches the project; the `remove-t-only` case ran through `mutate` (exit 0). (`g` is referenced by the environment's natural class, so the removal case names `t`.)

## Verification

grammar.xml on hc.dll: da, ba, ga parse; taga TA1+GA, TA2+GA; tada, taba none. FieldWorks project through projector and hc.dll: same counts (the HC XML has no morph names, so only counts compare: da 1, ba 1, ga 1, taga 2, tada 0, taba 0).
Controls (not kept here): rule only on the two after-`a` copies (the first environment pass of each MSA): tada and taba both parse twice; rule on the first MSA's before-consonant copy only: tada TA2+DA, taba TA2+BA; rule on the last copy only: tada TA1+DA, taba TA1+BA.

Source: PanGloss issues #99 and #100 (fixes in PanGloss PRs #101 and #102).
