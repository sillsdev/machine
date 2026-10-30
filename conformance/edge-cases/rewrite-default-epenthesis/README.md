# Rewrite defaults: epenthesis

Derived from `loader-default-symbol`, this fixture uses invented roots and an epenthesis rule with a `nas=-` target between `b` and `t`. Surface `a` lacks `nas` and defaults to `+`, so `bat` is rejected in analysis. Surface `d` explicitly has `nas=-` and is accepted; the synthesized feature-only node renders as `[ad(aa)]`.

The seven expectations were recorded with the C# oracle at Machine commit `aab4c59819adb7b366915e09df2fb28c62b8c03a`. Before the fix for [PanGloss issue #41](https://github.com/sillsdev/PanGloss/issues/41), PanGloss accepted `bat` as `|b[ad(aa)]t`; C# found no parse. The other six words agreed. After the fix all seven agreed. FieldWorks HCLoader does not currently emit `defaultSymbol`.

Source: [PanGloss PR #86](https://github.com/sillsdev/PanGloss/pull/86).
