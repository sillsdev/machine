# Rewrite defaults: explicit full set

Derived from `rewrite-default-epenthesis`, this fixture has the same grammar except that segment `a` authors `nas` explicitly with both symbols (`symNasPlus symNasMinus`) instead of omitting it. An explicit full set is a present value, so C# `UseDefaults` leaves it alone: unification binds the set itself, not the `+` default. The epenthesis target is `nas=-` between `b` and `t`; surface `a` carries `nas={+,-}`, which unifies with `nas=-`, so `bat` parses as `|b[ad(aa)]t` here, while in `rewrite-default-epenthesis` (where `a` omits `nas`) it is rejected.

The seven expectations were recorded with the C# oracle at Machine commit `aab4c59819adb7b366915e09df2fb28c62b8c03a`, using `conformance/adapters/hc-dotnet-wrapper.sh batch`; `rules:` comes from the same build's trace (`insert-oral` = `pr1` for both parsing words). Before the fix for [PanGloss issue #85](https://github.com/sillsdev/PanGloss/issues/85), PanGloss encoded the explicit full set as the same full mask as an absent feature, so `UseDefaults` substituted `+` and `bat` had no parse; C# produced `|b[ad(aa)]t`. The other six words agreed. After the fix all seven agree. FieldWorks HCLoader does not currently emit `defaultSymbol`.

Source: PanGloss#85 (follow-up to [PanGloss issue #41](https://github.com/sillsdev/PanGloss/issues/41)).
