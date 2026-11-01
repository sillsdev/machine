# adhoc-prohibition-variant-main-first-enclitic

Synthetic. An ad hoc MORPHEME prohibition whose first morpheme is the Verb MSA (and, by a second prohibition, the Noun MSA) of a stem entry that has a variant entry, and whose rest is an ENCLITIC. HCLoader loads each FieldWorks enclitic entry twice, as a clitic MorphologicalRule (no part of speech, in the stratum named Clitics, applied from that stratum's rule list) and as a stem LexicalEntry copy in the same stratum, and loads the variant once per main MSA under the MAIN entry's MSA (LoadLexEntryOfVariant, HCLoader.cs:782-852). Each FieldWorks prohibition therefore becomes one MorphemeCoOccurrenceRule per cross-product choice of copies (LoadMorphemeCoOccurrenceRules, HCLoader.cs:2262-2290): each of the four stem copies (main and variant, Verb and Noun) excludes both enclitic copies.

## What it pins

- `la`, `li`: parse as Noun and Verb (the variant li borrows both MSAs of la).
- `lara`, `lira` (enclitic `=ra`, no prohibition): parse as Noun and Verb once the clitic rule `mrRa` applies.
- `ta`, `tapa`: the stem `ta`, named by no prohibition, parses bare and with `=pa` (`TA+PA`, once): `mrPa` is a live rule and `=pa` does attach, so the zeros below come from the prohibition.
- `lapa`: main-first prohibition naming the enclitic as the rest of the morphemes: no analysis.
- `lipa`: the same prohibition must hold for the variant li: no analysis.

Not covered here: the main-first direction only; the enclitic-first direction is `adhoc-prohibition-variant-enclitic-first`.

## Grammar shape

`grammar.xml` is the per-copy form: the main stratum holds the stems (la, li and their Noun copies, and `ta`), and a second stratum named `Clitics` holds the enclitics, each as a clitic MorphologicalRule (`mrPa`, `mrRa`; no part of speech, listed in that stratum's `morphologicalRules`) beside a stem entry copy (`ePa`, `eRa`) carrying the same `MorphemeId` (HCLoader.cs:243-244, 300-305). No `isCliticRule` attribute is set: a rule is a clitic because its stratum is named Clitics, which is how HCLoader designates it. HCLoader makes three strata (Morphology, Clitics, Surface); this fixture has two, Main and Clitics, and Surface would hold no rule here, so it is the Clitics designation that matches. There is no prefix and no affix template. The entry copies `ePa` and `eRa` can be stems too: the oracle also parses `pa` and `ra` as bare words (the entry copy alone) and `papa`, `para` and `rapa` with the entry copy as the stem and the clitic rule on it, but no word in this fixture pins those parses. For `lapa` and `lipa` the clitic rule copy is the load-bearing one: stripping the rules that name `ePa` changes nothing, and stripping the rules that name `mrPa` makes both words parse. The entry copies are kept because HCLoader emits them and the prohibitions are written in the per-copy form.

## Oracle

hc.dll at Machine `aab4c59819adb7b366915e09df2fb28c62b8c03a`, self-check (`hc-conformance --fixtures`) PASS on signatures and traced rules. Provenance: `founding-oracle machine-commit=aab4c59819adb7b366915e09df2fb28c62b8c03a verified-date=2026-10-31`.

## FieldWorks source witness

`fieldworks/project.fwdata` is backed out of `grammar.xml` via LibLCM, not hand-edited: `tools/xample-projector` `author`, extended with `--enclitic-of` (enclitic morph type entry per clitic rule; the PanGloss `tools/xample-projector` carries the extension). FieldWorks 9.3.10.1452, LibLCM 11.0.0.55173.
`author --grammar grammar.xml --name adhocprohibitionvariantmainfirstenclitic --msa-copies eLaV,eLaN --variant-of eLiV=eLaV --variant-of eLiN=eLaN --enclitic-of ePa=mrPa --enclitic-of eRa=mrRa`
`phonology-mutations.yaml` `base_sha256` matches the project; the `empty-phoneme-inventory` and `remove-r-only` cases ran through `mutate` (exit 0).

## Verification

FieldWorks project through projector and hc.dll (FieldWorks 3.8.2.0 and the pinned build give the same counts), and `grammar.xml` through `hc-dotnet-wrapper.sh`: la/li 2 each, lara/lira 2 each, ta 1, tapa 1, lapa 0, lipa 0. Controls (not kept here): no prohibition: `lapa` and `lipa` parse twice each; variant copies omitted from the prohibition: `lapa` is 0 but `lipa` parses twice; the clitic rule copy `mrPa` omitted from the rest side: `lapa` and `lipa` both parse twice, so the rule copy is the load-bearing one.

Source: sillsdev/machine #480 (ad hoc prohibition conformance); the PanGloss staging copy is conformance-staging/edge-cases in sillsdev/PanGloss.
