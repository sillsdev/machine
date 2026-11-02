# The grammars

Every fixture directory under `languages/` and `edge-cases/` is listed here once, grouped by area, with
one line saying what its grammar and words exercise. Each fixture has a `grammar.xml` and a
`words.yaml`; the larger ones also have a `README.md` with their rationale.

Every grammar here is one a real FieldWorks project can produce. A grammar that FieldWorks cannot
produce does not belong in the suite, however expressible it is in the XML; see
[what FieldWorks cannot produce](../README.md#what-the-xml-can-say-but-fieldworks-cannot-so-we-do-not-test-it).

A test fails if a fixture directory is missing from this page, listed twice, or listed here but gone.
The list has 127 fixtures: 4 languages and 123 edge cases.

## The four languages

Whole-language grammars with a realistic lexicon and several interacting processes.

- `languages/metathesis-phase-isolation`: infixation, circumfixation, reduplication, metathesis and truncation, one demonstration per part of speech (26 words).
- `languages/polysynthetic-stratal-derivation-chain`: derivation then inflection across strata, with compounding, boundary markers and root guessing (24 words).
- `languages/suffixing-evidential-adjacency-chain`: a chain of evidential suffixes in template slots, morpheme co-occurrence exclusions and homophonous roots (30 words).
- `languages/templatic-root-modification`: stem names and templatic root modification, with epenthesis, prothesis, feeding and geminate rules (25 words).

## Affix shapes: circumfix, infix, null, truncation

- `edge-cases/circumfix-conditioned-halves`: a circumfix built as a cross product of two prefix halves and two suffix halves, each pairing conditioned on the stem edges.
- `edge-cases/circumfix-cross-product-and-infix-drop`: a two-prefix by two-suffix circumfix cross product, and an infix rule that drops material.
- `edge-cases/circumfix-in-template-slot`: a circumfix rule that sits in an affix template slot.
- `edge-cases/circumfix-infix-interior-action-precedence`: one allomorph that is both circumfixing and infixing in the same output.
- `edge-cases/circumfix-non-first-allomorph-selection`: a circumfix allomorph that is chosen after an ordinary suffix allomorph of the same rule fails.
- `edge-cases/circumfix-reduplication-precedence`: one allomorph that is both circumfixing and reduplicating in the same output.
- `edge-cases/discontinuous-morph-environment`: a root environment checked on each contiguous piece of a root split by a two-infix rule.
- `edge-cases/flex-null-affix-environments`: null prefixes whose environments look at the neighbouring prefix.
- `edge-cases/infix-interdigitation`: infixes inserted around a copied root, in a templatic Semitic shape.
- `edge-cases/process-morphology-in-place-mutation`: tense expressed only by changing a segment of the root, with no affix.
- `edge-cases/truncate-morphotactic`: rules that remove a final or initial segment of the stem, matched by literal segment, with optional insertion.
- `edge-cases/truncate-morphotactic-copy-u`: the truncation grammar with explicit morpheme ids and a copying suffix rule, so one surface form has several parses with different rule histories.
- `edge-cases/truncate-morphotactic-identity`: the truncation grammar with explicit morpheme ids, so repeated signatures stay distinct rows.
- `edge-cases/truncate-morphotactic-subsumption`: a prefix rule, a leading truncation and an insertion rule over the root `ag`, so a rule can remove what an earlier rule added.

## Templates, slots and parts of speech

- `edge-cases/final-template-merge-key`: a final template in a lower stratum, reached from two analysis orders that must merge into one parse.
- `edge-cases/final-template-partial-discriminators`: rules that may or may not follow a final template, depending on whether they are partial.
- `edge-cases/final-template-partial-root`: a partial root with a final template, and ordinary rules blocked after the template.
- `edge-cases/morphotactic-attribute-breadth`: repetition caps, partial rules and entries, bound allomorphs, non-final templates, co-occurrence rules and MPR groups.
- `edge-cases/optional-template-composite`: optional and mandatory template slots that combine into several parses of one word.
- `edge-cases/shared-template-unconstrained-suffix`: a template with a mandatory suffix shared by roots of different parts of speech.
- `edge-cases/subrule-morphosyntactic-gating`: a phonological subrule that applies only after a rule has changed the part of speech.
- `edge-cases/template-category-sharing`: one template used by several parts of speech.
- `edge-cases/deep-optional-affix-nesting`: twelve optional prefix slots that insert the same letter, so one word has many slot subsets that explain it.

## Clitics

- `edge-cases/clitic-enclitic-and-proclitic`: two unordered clitics, one before and one after the final template.
- `edge-cases/clitic-flex-layout-blocking-morphology`: a final template followed by an obligatory epenthesis in a stratum that holds a phonological rule.
- `edge-cases/clitic-proclitic-after-final-template`: a rule that is not a clitic and is refused after the final template.
- `edge-cases/clitic-rule-designation`: rules inside and outside the clitics stratum, checked against the final template.
- `edge-cases/clitic-stem-entry`: a clitic stem entry that FieldWorks loads into the clitics stratum.
- `edge-cases/non-clitic-after-final-template-layout`: a clitic stratum that holds only clitics, after a non-blocking stratum with a final template.

## Allomorphs, stem names and exceptions

- `edge-cases/disjunctive-recheck`: allomorphs chosen in order, where the first allomorph's right environment is re-checked against the rest of the word.
- `edge-cases/free-fluctuating-allomorph-pair`: two allomorphs that can both apply, so a word has one parse per allomorph.
- `edge-cases/mpr-gated-exception`: a phonological rule that a root or an affix switches off with a morphological feature.
- `edge-cases/stem-name-restricted-root-allomorph`: a root allomorph that is used only when a stem name matches the word's features.
- `edge-cases/pattern-root-required-environment`: a lexical pattern entry and a literal root that has a required environment.
- `edge-cases/guesser-pattern-root-fallback`: a lexical pattern entry that guesses roots only when no listed root matches.
- `edge-cases/guess-roots-settings`: root guessing turned on and off for the same words.
- `edge-cases/loader-pattern-shapes`: lexical entries whose shape names a natural class.

## Ad hoc prohibitions

- `edge-cases/adhoc-prohibition-affix-environment-copies`: a prohibition on a prefix allomorph that has two environments and two morphosyntactic analyses.
- `edge-cases/adhoc-prohibition-affix-msa-copies`: a prohibition on a prefix entry that has two morphosyntactic analyses.
- `edge-cases/adhoc-prohibition-allomorph-variant-control`: a control for the variant cases, with an allomorph prohibition between a variant stem and a prefix.
- `edge-cases/adhoc-prohibition-circumfix-pairings`: a prohibition that names a circumfix, which pairs with each of its prefix and suffix halves.
- `edge-cases/adhoc-prohibition-stem-msa-copies`: a prohibition on a stem entry that has two morphosyntactic analyses.
- `edge-cases/adhoc-prohibition-variant-enclitic-first`: a morpheme prohibition that starts with an enclitic, over a stem that has a variant entry.
- `edge-cases/adhoc-prohibition-variant-main-first-enclitic`: a morpheme prohibition that ends with an enclitic, over a stem that has a variant entry.
- `edge-cases/adhoc-prohibition-variant-msa-copies`: a morpheme prohibition on one analysis of a stem that has a variant entry.

## Compounding

- `edge-cases/compounding-breadth`: head and non-head feature requirements, the repetition cap and a switched-off compounding rule, using bounded patterns (30 words).
- `edge-cases/compounding-non-recursive`: one non-recursive compounding rule with head and non-head lexicon subsets chosen by licence features.
- `edge-cases/head-ambiguous-compounding`: compounds in which either root could be the head.
- `edge-cases/recursive-endocentric-compounding`: a compounding rule that applies to its own output.
- `edge-cases/cross-stem-material-determination`: a suffix whose form depends on which prefix was chosen, not only on whether the pair is allowed.

## Phonological rules: deletion, epenthesis, merge, expansion

- `edge-cases/class-deletion-reinsertion-strrep`: a deletion whose target is a natural class, in a grammar with no phonological features.
- `edge-cases/deletion-reapplications`: hiatus resolution that deletes a run of vowels, analysed with the deletion reapplication limit.
- `edge-cases/deletion-reduplication-exception-composite`: deletion, reduplication and a feature-conferred exception in one word.
- `edge-cases/deletion-reinserted-feature-unapply`: final devoicing followed by cluster simplification, undone in the right order on analysis.
- `edge-cases/epenthesis-fed-merge`: an epenthesis whose output is merged by a later rule.
- `edge-cases/expansion-interposed-optional`: an expansion rule undone across an optional segment between its two output segments.
- `edge-cases/expansion-overlapping-targets`: an expansion whose two-segment output overlaps itself in a run of vowels.
- `edge-cases/feature-rule-before-merge`: a feature-changing rule that feeds a later merge.
- `edge-cases/geminate-under-merge`: a long consonant shortened, then fused with a vowel.
- `edge-cases/interposed-optional-multi-rhs`: a rule with several output segments and an optional segment between them.
- `edge-cases/iterative-epenthesis-cascade`: two iterative epentheses with empty targets that feed each other.
- `edge-cases/merge-then-metathesis`: a metathesis followed by a merge, undone in order on analysis.
- `edge-cases/simultaneous-epenthesis-cascade`: a simultaneous epenthesis that inserts a vowel after any high vowel.
- `edge-cases/simultaneous-expansion-terminates`: a simultaneous expansion that must be undone at two sites in one pass.
- `edge-cases/simultaneous-feeding`: a simultaneous feature-changing rule whose sites overlap.
- `edge-cases/simultaneous-feeding-control-iterative`: the same feature-changing rule applied iteratively, as a control.
- `edge-cases/simultaneous-subrule-genuine-overlap`: two subrules that both match one position, where the first subrule wins.
- `edge-cases/two-merges-cascade-env`: a vowel-fronting rule feeding two merges, with and without a morpheme boundary.
- `edge-cases/merge-subrules-pos-ltr-template`: a left-to-right merge with subrules gated by part of speech and a verb template.
- `edge-cases/merge-subrules-pos-rtl-template`: the same merge applied right to left.
- `edge-cases/merge-subrules-pos-simultaneous-template`: the same merge applied simultaneously.

## Boundaries, direction and string representation

- `edge-cases/boundary-first-in-narrowing-lhs`: a merge whose target starts with a morpheme boundary.
- `edge-cases/boundary-first-in-narrowing-lhs-strrep`: the same merge in a grammar with no phonological features.
- `edge-cases/boundary-leftover-in-narrowing-lhs-strrep`: a merge that absorbs a nasal and a boundary, with no phonological features.
- `edge-cases/boundary-mid-in-narrowing-lhs-strrep`: a merge with a boundary in the middle of its target, with no phonological features.
- `edge-cases/boundary-narrowing-probe`: a suffix-initial consonant that becomes a nasal after a nasal.
- `edge-cases/literal-hash-affix-context`: a literal `#` marker in an environment, which is not a word-edge anchor.
- `edge-cases/word-boundary-affix-rewrite`: a word-edge anchor in a rule that rewrites the edges of prefixes and roots.
- `edge-cases/right-to-left-anchor-environment`: a spreading rule whose reach depends on the scan direction.
- `edge-cases/right-to-left-bounded-quantifier-rewrite`: a right-to-left rewrite with a bounded repetition in its environment.
- `edge-cases/right-to-left-metathesis-reversal`: a right-to-left metathesis with the switch groups tagged in the opposite order.
- `edge-cases/unbounded-iterative-quantifier-expansion`: a rewrite with an unbounded repetition in its environment, applied iteratively.
- `edge-cases/standalone-combining-mark`: a tone mark defined as its own segment, written as a combining character.
- `edge-cases/diacritic-segments`: roots with precomposed Latin diacritics, in the middle and at the end of the stem.
- `edge-cases/strrep-identity`: a grammar with no phonological features, where each segment is identified by its spelling.
- `edge-cases/strrep-rewrite-unapplication`: a rewrite undone on analysis in a grammar with no phonological features.
- `edge-cases/chained-output-feature-override-loss`: two rules that each set an output feature, and a word that needs both.
- `edge-cases/alpha-variable-name-collision`: two vowels that flip different features under one alpha variable.

## Alpha variables in environments

These families repeat one pattern across rewrite direction and environment side. Each fixture has 16 or 17
words over a small vowel set. The names say which cell they fill: `bounded` or `unbounded` is whether the
repeated stretch has a cap, `ltr` or `rtl` is the rewrite direction, and `left` or `right` is the side
that holds the environment.

### Nullable environment with disagreeing alpha values

- `edge-cases/nullable-disagree-bounded-ltr-left`
- `edge-cases/nullable-disagree-bounded-ltr-right`
- `edge-cases/nullable-disagree-bounded-rtl-left`
- `edge-cases/nullable-disagree-bounded-rtl-right`
- `edge-cases/nullable-disagree-plain-ltr-right`
- `edge-cases/nullable-disagree-unbounded-ltr-left`
- `edge-cases/nullable-disagree-unbounded-ltr-right`
- `edge-cases/nullable-disagree-unbounded-rtl-left`
- `edge-cases/nullable-disagree-unbounded-rtl-right`

Each is a back and round disagreement over a four-vowel class, with a nullable stretch in the environment
that must still allow the rewrite at the word edge, so `ia` is the one surface word.

### Partial explicit class with disagreeing alpha values

- `edge-cases/partial-class-disagree-bounded-ltr-left`
- `edge-cases/partial-class-disagree-bounded-rtl-left`
- `edge-cases/partial-class-disagree-bounded-rtl-right`
- `edge-cases/partial-class-disagree-unbounded-ltr-right`
- `edge-cases/partial-class-disagree-unbounded-rtl-left`
- `edge-cases/partial-class-disagree-unbounded-rtl-right`

Each uses an explicit vowel class that omits two of the four vowels, so the engine accepts only `ia` of the 16 words.

### Ambiguous disagreement

- `edge-cases/ambiguous-disagree-feature-class-bounded-rtl`
- `edge-cases/ambiguous-disagree-feature-class-unbounded-ltr`
- `edge-cases/ambiguous-disagree-mixed-plus-class`
- `edge-cases/ambiguous-disagree-repeated-minus-partial-focus`

Each combines a minus alpha on a complete class with an agreeing occurrence on a partial class, or on a
feature-defined class, in a bounded or unbounded environment.

### Overridden alpha

- `edge-cases/overridden-alpha-bounded-ltr-left`
- `edge-cases/overridden-alpha-bounded-rtl-left`
- `edge-cases/overridden-alpha-plain-ltr-left`
- `edge-cases/overridden-alpha-unbounded-ltr-left`

Each neutralises a feature that an alpha variable also governs, so the engine requires `ii` from `iu`.

### Quantified alpha agreement

- `edge-cases/quantified-alpha-bounded-ltr-left`
- `edge-cases/quantified-alpha-bounded-ltr-right`
- `edge-cases/quantified-alpha-bounded-rtl-left`
- `edge-cases/quantified-alpha-bounded-rtl-right`
- `edge-cases/quantified-alpha-bounded-zero-left`
- `edge-cases/quantified-alpha-bounded-zero-right`
- `edge-cases/quantified-alpha-unbounded-ltr-left`
- `edge-cases/quantified-alpha-unbounded-ltr-right`
- `edge-cases/quantified-alpha-unbounded-rtl-left`
- `edge-cases/quantified-alpha-unbounded-rtl-right`
- `edge-cases/quantified-alpha-unbounded-zero-left`
- `edge-cases/quantified-alpha-unbounded-zero-right`

Each repeats a segment in the environment with an alpha value that must agree across the repetition; the
`zero` cells allow no repetition at the word edge.
