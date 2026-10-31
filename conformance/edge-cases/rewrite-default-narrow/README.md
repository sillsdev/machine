# Rewrite defaults: narrow

Derived from `loader-default-symbol`, this fixture uses invented roots and a narrowing rule whose `nas=-` target deletes the explicitly oral `d` in `bdt`, yielding `bt`. The unspecified `a` in `bat` defaults to `nas=+` and is retained.

The seven expectations were recorded with the C# oracle at Machine commit `aab4c59819adb7b366915e09df2fb28c62b8c03a`. Before the fix for [PanGloss issue #41](https://github.com/sillsdev/PanGloss/issues/41), PanGloss rejected `bat` and returned two `|bt` analyses for `bt`; C# produced `|bat` and one `|bt`. The other five words agreed. After the fix all seven agreed. FieldWorks HCLoader does not currently emit `defaultSymbol`.

Source: [PanGloss PR #86](https://github.com/sillsdev/PanGloss/pull/86).
