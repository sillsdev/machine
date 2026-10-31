# Final-template partial root

This synthetic grammar checks whether a partial lexical entry can license a derivation that the
analysis-side final-template prune (#491) rejects. Issue [#551](https://github.com/sillsdev/machine/issues/551)
suspected it could, because a word inherits partiality from its root and synthesis lets a
non-partial rule follow a final template when the input word is partial.

It cannot. `SynthesisAffixTemplatesRule.Apply` skips every template when
`input.RootAllomorph.Morpheme.IsPartial`, so no template, final or not, ever applies to a word
with a partial root. The ordinary rule that the partial-input exception would admit after a final
template therefore has no final template to follow.

- DAK: non-partial root controls. `dakfa` parses; `dakfaga` is blocked.
- PAK: partial root in a lower stratum. `pakga` parses (no template); `pakfa` and `pakfaga` do not.
- PIB and NIB: partial and non-partial roots in the template's own stratum. `pibgb` and `nibfb`
  parse; `pibfb`, `pibfbgb` and `nibfbgb` do not.

The expected rows pass with the lexical-entry half of `Morpher.IsPartial` removed, which turns the
prune on for this grammar. Reaching a partial-rule-dependent parse needs a partial rule, which is
covered by `final-template-partial-discriminators` (`pilfbvb`).

Not covered: lexical-pattern guesses (their entries are already pruned by design, see
`Morpher.LexicalGuess`) and inflectional-family stem substitution.
