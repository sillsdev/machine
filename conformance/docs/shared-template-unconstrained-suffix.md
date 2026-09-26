# Shared template with an unconstrained suffix

This grammar preserves a valid verbalized noun when two different templates share one suffix.
It also rejects words missing required inflection or using a suffix with the wrong root category.

## Derivation and complete expected identities

The noun `mi` can stand alone because neither verb template applies to N. The ordinary verbalizer
`IVERB` changes N to IV and adds `v`; the IV template then requires the shared `PAST` suffix `d`.
Thus `mivd` has exactly `NROOT+IVERB+PAST|mivd`. The TV root `ti` uses the other template, producing
exactly `TVROOT+PAST|tid`. The unrelated A root `ki` stands alone as `AROOT|ki`.

`miv` and `ti` cannot stand alone: an applicable obligatory template was not completed. `mid`
and `kid` cannot use either template; the shared suffix is absent from the ordinary stratum list.
`kivd` cannot apply the N-requiring verbalizer to A. `mivdd` would require a second application
of the same suffix, beyond its one-slot/one-application availability. There are no other roots,
allomorphs or ordinary rules that could justify extra identities for these ten words.

The shared suffix has neither required nor output syntactic features. This is the unconstrained
variant of `AffixTemplateTests.SameRuleUsedInMultipleTemplates`, not the weaker variant where the
suffix already carries the union of both templates' categories. Both slots are mandatory.

## FieldWorks reachability

The grammar uses loader-producible combinations. `HCLoader.LoadMorphologicalRule` keeps inflectional
MSAs with slots out of the ordinary stratum list. `LoadInflAffixProcessRule` permits an empty
required feature structure when the MSA has no part of speech or inflectional features and does
not set output features. The same MSA may refer to the TV and IV slots; LibLCM's
`MoInflAffixSlot.Affixes` follows those slot references without requiring an MSA part of speech.
`LoadAffixTemplate` reuses these rule objects and supplies each owning category's required POS.
`LoadDerivAffixProcessRule` supplies the ordinary N-to-IV change. Each slot can be owned by its
template's category, so no out-of-scope slot or output-feature-setting inflection is needed.
This is a loader-reachability argument, not a claimed real-project FieldWorks round trip.

## Verification scope

The expected identities were derived above, then checked with the Machine conformance harness
built from `a20bce1231fab308810fa5a531257eef24f5df1b`, which includes merged #493. Ten rows pass
with memoization enabled and disabled, for both TV/IV and IV/TV template declaration order.
Each replay reports one passed fixture, no failures and no skips. The reverse-order probe changes
only the two template elements; the same `words.yaml` is retained.

Run the harness with `--fixtures <root-containing-only-this-edge-case>` and repeat with
`--no-memoization`. The default committed order is TV then IV. Reverse only those two template
elements in a scratch copy for the second pair of runs. A focused replay must discover exactly
one fixture and check all ten rows, not merely return exit zero on an empty root.

PanGloss's full-parse probe also passes before its gate-only alignment: existing widening can
mask the intermediate-state mismatch. This grammar is preservation coverage, not proof that
Rust widening is necessary or that the broader collision/correlation questions in
[issue #505](https://github.com/sillsdev/machine/issues/505) are resolved. Its load-bearing
state-level regression is tracked separately in PanGloss.
