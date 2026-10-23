# `truncate-morphotactic`: complete derivations for `a` and `ga`

This note documents the two added words and why their expected signatures preserve repeated
identities. The grammar is unchanged.

## Provenance and #506 behavior

The entries for `a` and `ga` are hand-derived from forward synthesis, not transcribed from the
unpatched C# oracle. At #480 head `fa8d9b2540962981add315d06203e928a11c1853`, the C# parser
occasionally loses tied, zero-width morph annotations when it copies a word (`Shape.CopyTo` reaches
`AnnotationList.GetNodes` and `BidirList.Find`). The resulting signatures are nondeterministic even
though the parser still returns two parses for `a` and five for `ga`. These rows intentionally make
the pre-fix #506 conformance check fail on incomplete outputs; a run can pass by chance if it retains
all annotations. Do not regenerate these expectations from an unpatched run. The underlying issue is
[Machine #506](https://github.com/sillsdev/machine/issues/506).

The expected multiset was observed on the directional-boundary fix
`d98d12a5d514ce589d7495c71d055830e061a418`, based at
`259920414132bdd5a85542722d9af5509aed8943`: ten fresh loads returned the complete two-parse and
five-parse sets. The fix is proposed in [Machine #556](https://github.com/sillsdev/machine/pull/556).
It is not included in this fixture branch.

## Features and allomorph identities

Every lexical entry has `partOfSpeech="posV"`; each of the three rules requires `posV`, and none
sets a new part of speech or head feature. Thus all seven paths retain `posV`. `HeadFeatures` is
empty, and no rule has an MPR requirement or output. The constructor-default realizational feature
structure is `ANY`; the MPR set is empty.

The phonological values used by the patterns are:

- `s` is `cons+`, `cont+`, `place=alveolar`; it is the only segment in `ncFricative`.
- `g` is `cons+`, `cont-`, `place=velar`; `mruleTruncTrail` matches it by literal segment identity.
- `a` is `cons-`, `height=low`.

The source identities below use the XML lexical allomorph ID `root32_1` and each rule's unique
`MorphologicalSubrule` ID. Each has exactly one runtime allomorph, index 0. The grammar intentionally
omits `<MorphemeId>` on the lexical entry and rules; their loaded morpheme IDs are null. HermitCrab
also creates internal allomorph GUIDs at load time, so those runtime GUIDs are not stable fixture
identities. The stable identity is the owning entry/rule and subrule listed below.

| Label | Stable source identity | Gloss | Forward action |
| --- | --- | --- | --- |
| root | `root32` / `root32_1` | `32` | underlying `sag`, POS `posV` |
| A | `mruleTruncTrail` / `msubTruncTrail` / allomorph 0 | `3SGA` | copy `ptt1` and drop final literal `g` |
| B | `mruleTruncLead` / `msubTruncLead` / allomorph 0 | `3SGB` | copy `ptl2` and drop initial `ncFricative` segment `s` |
| C | `mruleTruncOptIns` / `msubTruncOptIns` / allomorph 0 | `3SGC` | consume optional initial `s`, insert `g`, then copy `pto2` |

All rule applications below preserve POS `posV`. “Application order” is forward synthesis order;
“morph order” is the annotation order observed through `Word.AllomorphsInMorphOrder`. Those orders
can differ for zero-width process morphs. Every row also has a distinct allomorph identity chain,
even where the protocol signature serializes to the same text.

## Complete forward derivations

| Word | # | Forward application chain and surface transitions | Morph order | Signature |
| --- | ---: | --- | --- | --- |
| `a` | 1 | `root32_1: sag` → A: `sa` → B: `a` | B, root, A | `++|a` |
| `a` | 2 | `root32_1: sag` → B: `ag` → A: `a` | A, root, B | `++|a` |
| `ga` | 1 | `root32_1: sag` → A: `sa` → B: `a` → C: `ga` | B, root, C, A | `+++|ga` |
| `ga` | 2 | `root32_1: sag` → B: `ag` → A: `a` → C: `ga` | A, root, C, B | `+++|ga` |
| `ga` | 3 | `root32_1: sag` → B: `ag` → C: `gag` → A: `ga` | B, C, A, root | `+++|ga` |
| `ga` | 4 | `root32_1: sag` → A: `sa` → C: `ga` | A, C, root | `++|ga` |
| `ga` | 5 | `root32_1: sag` → C: `gag` → A: `ga` | C, A, root | `++|ga` |

The signature counts follow the number of morphs, not the glosses: three allomorphs (root plus two
rules) with null morpheme IDs serialize as `++|…`; four allomorphs (root plus three rules) serialize
as `+++|…`. The semicolon-joined signature therefore contains two `++|a` entries, three `+++|ga`
entries, and two `++|ga` entries. Neither repeated value is deduplicated.

## Exhaustiveness

Only `root32` can produce either target. `root33` (`sas`) can lose its initial `s` to B, producing
`as`, or C can consume that `s` and insert `g`, producing `gas`; neither result ends in `g`, so A
cannot produce `a` or `ga` from that root. `root42` (`bubibi`) matches neither A's final literal
`g` nor B's initial fricative `s`; C can only add a `g` before the unchanged root.

From `sag`, `a` requires both truncations A and B, in either order. C cannot occur in a derivation of
`a` because it inserts a leading `g`. For `ga`, C must occur once. It can apply after A and B have
reduced the stem to `a`, between B and A on `ag` (`ag` → `gag` → `ga`), or after A on `sa` (`sa` →
`ga`). It can also precede A directly (`sag` → `gag` → `ga`). These are exactly the five rows
above: three paths use A, B, C, and two use A, C. A second C adds another leading `g`; neither A
nor B can remove that leading `g`, so it cannot return `ga`. After each listed terminal form, no
remaining rule applies in a way that preserves the target. This rules out additional root/rule
chains rather than inferring completeness from a parser's count.

## Runtime comparison recorded for this slice

The unpatched #480 head returned `ok`, 2 parses for `a`, and 5 for `ga` in ten fresh parser loads,
but only 2/10 loads emitted both complete expected signature multisets. Other loads emitted `+|a`
or `|a`, and/or `+|ga` among the five `ga` results. The isolated directional-boundary candidate
returned the full multisets and the same seven application/morph-order identities on 10/10 loads.
The comparison used the same grammar and constructor-default `Morpher` settings, with one worker.
Every load compared the complete signature multiset, source allomorph owner/index, forward rule
application order, morph order, and feature state against the rows above.

## Counterfactual evidence and the unstable oracle

An unpatched semantic-coverage sweep reported apparent effects from deleting `Gloss`, deleting
the empty `HeadFeatures`, swapping adjacent rules within the unordered stratum, and removing
lexical or rule part-of-speech attributes. Its baseline already lost morph identities; a difference
between two such runs does not demonstrate that the grammar mutation changed semantics.

Each mutation was rerun against the corrected engine in ten fresh processes over all eleven fixture
words. Removing `Gloss`, empty `HeadFeatures`, lexical `partOfSpeech`, or rule
`requiredPartsOfSpeech`, and either adjacent rule swap, preserved every status and signature
multiset. Those apparent new evidence rows were rejected. Existing witnesses from other fixtures
remain intact. The interface ledger's unchanged outcomes now cover eleven words.

Changing the stratum from unordered to linear rule execution did change the `a` and `ga` results
consistently. The counterfactual ledger therefore retains this fixture as an additional witness for
that distinction. Construct claims still use the existing coarse truncation tag; its seven added
parse claims are recorded as unmapped, without claiming new semantic coverage.
