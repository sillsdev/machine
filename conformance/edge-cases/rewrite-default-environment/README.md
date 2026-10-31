# Rewrite defaults: environment

Derived from `loader-default-symbol`, this fixture uses invented roots and a rewrite rule whose left environment matches `nas=-`. The feature defaults to `+`: `a` omits it, while `d` explicitly has `nas=-`. Thus `bat` retains `t`, while `bdt` deletes it.

The seven expectations were recorded with the C# oracle at Machine commit `aab4c59819adb7b366915e09df2fb28c62b8c03a`. Before the fix for [PanGloss issue #41](https://github.com/sillsdev/PanGloss/issues/41), PanGloss rejected `bat` and accepted `ba`; C# produced `|bat` and no parse respectively. The other five words agreed. After the fix all seven agreed. FieldWorks HCLoader does not currently emit `defaultSymbol`.

Source: [PanGloss PR #86](https://github.com/sillsdev/PanGloss/pull/86).
