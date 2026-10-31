# adhoc-prohibition-circumfix-pairings

Synthetic. An ad hoc allomorph prohibition naming a circumfix lexeme form. A FieldWorks circumfix entry has a circumfix-type lexeme form plus prefix and suffix alternate forms; HCLoader emits one allomorph per prefix half x suffix half pairing (HCLoader.cs:1097-1122; here ta/to x ka/ki = four subrules) and registers all of them under `entry.LexemeFormOA`, so the prohibition reaches every pairing (HCLoader.cs:2212-2236).

## What it pins

- `da`, `ba`, `ga`: parse.
- `tagaka`, `tagaki`, `togaka`, `togaki`: one analysis each. `ga` also carries FieldWorks prohibitions that name only the prefix half `ta` (half first and stem first). A half is not a key of HCLoader's allomorph table, so they resolve to no rule: this is the "still parses" control, and `grammar.xml`, as HCLoader emits it, has no rule for them.
- `tadaka`, `tadaki`, `todaka`, `todaki`: circumfix first, one rule per pairing: no analysis.
- `tabaka`, `tabaki`, `tobaka`, `tobaki`: stem first, one rule per pairing: no analysis.

## Oracle

hc.dll at Machine `aab4c59819adb7b366915e09df2fb28c62b8c03a`, self-check PASS on signatures and traced rules. Provenance: `founding-oracle machine-commit=aab4c59819adb7b366915e09df2fb28c62b8c03a verified-date=2026-10-30`.

## FieldWorks witness

`fieldworks/project.fwdata` is backed out of `grammar.xml` via LibLCM, not hand-edited: `tools/xample-projector` `author`, extended (extended with MSA-copies, environment-copies, circumfix and variant-entry options; the patch is not part of this repository) so that a rule whose subrules are the prefix x suffix cross product becomes one circumfix entry (created through `ILexEntryFactory.Create`, which makes the first prefix and suffix halves itself; the other halves are added) and `--half-prohib` adds the half-only prohibitions. FieldWorks 9.3.10.1452, LibLCM 11.0.0.55173.
`author --grammar grammar.xml --name AdhocCirc --half-prohib aGa=mrCirc:ta`
The authored project has one circumfix entry (lexeme form `ta- -ka`, halves ta, to, ka, ki), two MoAlloAdhocProhibs naming the circumfix lexeme form (one per direction) and two naming the half `ta` on `ga`. Loaded back through HCLoader it emits four pairing subrules and eight AllomorphCoOccurrenceRules; the half-only prohibitions add none.
`phonology-mutations.yaml` `base_sha256` matches the project; the `remove-g-only` case ran through `mutate` (exit 0).

## Verification

grammar.xml on hc.dll: ga words 1 analysis each, da/ba words none. FieldWorks project through projector and hc.dll: same (da 1, ba 1, ga 1, ga words 1 each, all eight da/ba words 0).
Controls : rule on the ta-...-ka pairing only: only tadaka and tabaka are lost; rule on the to-...-ki pairing only: only todaki and tobaki are lost; no rule: all sixteen parse.

Source: PanGloss issues #99 and #100 (fixes in PanGloss PRs #101 and #102).
