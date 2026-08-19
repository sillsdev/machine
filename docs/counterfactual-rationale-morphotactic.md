# Counterfactual rationale: morphotactic-attribute-breadth / loader-isactive-breadth

25 DTD surfaces assigned, split across two fixtures:

- `conformance/edge-cases/morphotactic-attribute-breadth/` (16 surfaces)
- `conformance/edge-cases/loader-isactive-breadth/` (9 surfaces)

19 are now EVIDENCED (fixtures edited, confirmed against the C# founding oracle by hand-applying the
exact mutation `GrammarMutator` would apply, to a scratch copy of the grammar, and diffing the batch
output before/after). 6 are NOT EVIDENCEABLE, for two distinct, confirmed reasons: two attributes the
engine never reads at all (a product gap in the C# engine itself), and four collections whose IDREF
resolution is fail-fast in a way that makes the only viable construct break the fixture's own baseline
load (a methodological deadlock in what a fixture-only fix can do, not a modeling failure).

All work is committed on `conformance/edge-cases/loader-isactive-breadth/{grammar.xml,words.yaml}` and
`conformance/edge-cases/morphotactic-attribute-breadth/{grammar.xml,words.yaml}`.

## Method note

For every EVIDENCED surface below, the "before -> after" outcome was obtained by:

1. Copying the real fixture's `grammar.xml` to a scratch file.
2. Hand-applying the identical mutation `GrammarMutator.NeutralizeEnumValue`/`DeleteElement` would
   apply (verified against `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/
   GrammarMutator.cs`: an enum value is rewritten to the alphabetically-first *other* declared value
   of that attribute across the whole grammar-observable inventory, never a value picked by hand).
3. Running both the original and mutated grammar through
   `conformance/adapters/hc-dotnet-wrapper.sh batch` (the C# founding oracle, not `pangloss`) over the
   discriminating word(s), and diffing the two TSV outputs.

No signature below was hand-derived; every one is transcribed from an actual oracle run. The full
fixture (all words, not just the new ones) was re-run after each change and matches the pre-existing,
already-accepted signatures exactly (no regressions), and
`hc-conformance.dll --fixtures conformance` self-check passes 26/26 with both fixtures included.

---

## morphotactic-attribute-breadth

### AffixTemplate/final/false

**EVIDENCED.** Added `mrNF` (`MorphologicalRule`, suffix `-pu`), reachable *only* through
`nonFinalTemplate`'s own slot (never stratum-listed, never in `finalTemplate`). Word `kulpu`.

- Before (baseline, `final="false"`): `kulpu` -> no parse (`-`).
- After (mutated to `final="true"`): `kulpu` -> `ok::KUL+NF|kulpu`.

The original design reused `mrPartial` in `nonFinalTemplate`'s slot, which is *also* reachable through
`finalTemplate`'s own `partialSlot`. Applying the same rule object through either template produces a
structurally identical `Word` (same morphs, same shape), and `SynthesisAffixTemplatesRule.Apply`
dedups its output through a `HashSet<Word>` keyed on value equality — so the two paths collapsed to
one and `final="false"` never changed anything. Giving `nonFinalTemplate` an *exclusive* rule removes
the alternate route, and the counterfactual — flipping `final` to `true`, which lets the template's own
application close the derivation — is exactly the delta the DTD attribute is supposed to gate.

### AllomorphCoOccurrenceRule/adjacency/adjacentToLeft

**EVIDENCED.** Added a dedicated 4-rule chain `mrCoA`/`mrCoB`/`mrCoC`/`mrCoD` (suffixes `-sa`/`-ka`/
`-ta`/`-na`), isolated from every other construct (fresh root `top`, no shared rule IDs, no family), so
these four adjacency values and the two `isActive="no"` decoys below can all be tested on the *same*
word without fighting each other or touching the repetition/blocking controls. Rule:
`primaryAllomorph="subCoB" otherAllomorphs="subCoA" adjacency="adjacentToLeft"`. Word `topsakatana`
(COA+COB+COC+COD in that order).

- Before (`adjacentToLeft`): `topsakatana` -> `ok::TOP+COA+COB+COC+COD|topsakatana` (COA is
  immediately left of COB, satisfied).
- After (mutated to its DTD-inventory sibling, `adjacentToRight`): `topsakatana` -> no parse (`-`)
  (COA is not immediately *right* of COB).

### AllomorphCoOccurrenceRule/adjacency/adjacentToRight

**EVIDENCED.** Rule: `primaryAllomorph="subCoC" otherAllomorphs="subCoD" adjacency="adjacentToRight"`.
Same word, `topsakatana`.

- Before: `ok::TOP+COA+COB+COC+COD|topsakatana` (COD is immediately right of COC).
- After (mutated sibling `adjacentToLeft`): no parse (`-`).

### AllomorphCoOccurrenceRule/adjacency/somewhereToLeft

**EVIDENCED.** Rule: `primaryAllomorph="subCoD" otherAllomorphs="subCoA" adjacency="somewhereToLeft"`
— COA is somewhere to the left of COD, with COB/COC genuinely in between (this is what makes it a real
test of "somewhere" rather than a disguised "adjacent": if the two were already adjacent, flipping to
`adjacentToLeft` would not change anything).

- Before: `ok::TOP+COA+COB+COC+COD|topsakatana`.
- After (mutated sibling `adjacentToLeft`): no parse (`-`) — COC, not COA, is immediately left of COD.

### AllomorphCoOccurrenceRule/adjacency/somewhereToRight

**EVIDENCED.** Rule: `primaryAllomorph="subCoA" otherAllomorphs="subCoD" adjacency="somewhereToRight"`.

- Before: `ok::TOP+COA+COB+COC+COD|topsakatana`.
- After (mutated sibling, also `adjacentToLeft` — the sibling-selection is the alphabetically-first
  *other* declared value across the whole inventory, not a semantic opposite): no parse (`-`) — COD is
  nowhere near immediately left of COA (COD is the last morpheme, COA the first).

### AllomorphCoOccurrenceRule/isActive/no

**EVIDENCED.** Decoy: `isActive="no" type="exclude" primaryAllomorph="subCoB" otherAllomorphs="subCoC"
adjacency="anywhere"`, on the same `topsakatana`.

- Before (decoy inactive): `ok::TOP+COA+COB+COC+COD|topsakatana`.
- After (decoy activated): no parse (`-`) — COB and COC now exclude each other, and both are present.

The original fixture's only decoy for this surface (`aKul`/`aNap`) named a pair that can never
co-occur in *any* word here (`aKul` and `aNap` are separate lexical roots with no compounding rule to
combine them), so activating it changed nothing regardless of the DTD value. The fix was to give the
decoy a pair that *does* co-occur in a real word.

### LexicalEntry/partial/true

**EVIDENCED, at zero grammar cost.** No grammar change was needed — the discriminating word
(`napmo`) was reachable from the *existing*, unmodified grammar. `eNap` already carried
`partial="true"`, and `mrPartial` was already reachable only through an `AffixTemplate` slot.

- Before (`eNap partial="true"`): `napmo` -> no parse (`-`).
- After (mutated to `partial="false"`): `napmo` -> `ok::NAP+PART|napmo`.

Mechanism: `SynthesisAffixTemplatesRule.Apply` gates its *entire* template loop on
`!input.RootAllomorph.Morpheme.IsPartial` — a partial root never even attempts *any* template, so a
template-only rule (`mrPartial`) can never attach to it, while the identical rule attaches fine to a
non-partial root (`kul` -> `kulmo`). The only reason this was not already evidenced is that no existing
word combined a partial root with a template-only rule; `napmo` is exactly that combination and was
sitting unexercised in the existing lexicon.

### MorphemeCoOccurrenceRule/adjacency/anywhere

**EVIDENCED.** Rule: `primaryMorpheme="mrCoD" otherMorphemes="mrCoB" adjacency="anywhere"` (keyed on
rule IDs, not allomorph IDs — `MorphemeCoOccurrenceRule` resolves `Allomorph.Morpheme`, so a rule's own
`id` is the correct IDREF here, matching the existing `languages/suffixing-evidential-adjacency-chain`
fixture's own idiom). Word `topsakatana`.

- Before (`anywhere`): `ok::TOP+COA+COB+COC+COD|topsakatana` (COB present anywhere, satisfied).
- After (mutated sibling `adjacentToLeft`): no parse (`-`) — COC, not COB, is immediately left of COD.

### MorphemeCoOccurrenceRule/isActive/no

**EVIDENCED.** Decoy: `isActive="no" type="exclude" primaryMorpheme="mrCoA" otherMorphemes="mrCoC"
adjacency="anywhere"`.

- Before (decoy inactive): `ok::TOP+COA+COB+COC+COD|topsakatana`.
- After (decoy activated): no parse (`-`).

Same underlying defect as the Allomorph case above: the original decoy (`eKul`/`eNap`) named two
morphemes that can never co-occur in this no-compounding grammar.

### MorphologicalPhonologicalRuleFeatureGroup/isActive/no

**EVIDENCED.** Added a 3-rule chain `mrMB` (sets `mprB`) -> `mrMA` (sets `mprA`) -> `mrMReq` (requires
`mprB` still present), all members of `appendGroup` (`mprA mprB`, `outputType="append"`). Redefined the
existing decoy `groupDecoy` to claim the *same two* features as `appendGroup` (previously it named only
`mprA`, which made it structurally inert — see below) and left its `outputType` at the DTD default
(`overwrite`, the opposite of `appendGroup`'s `append`). Word `kulbubidu` (kul + mrMB + mrMA + mrMReq).

- Before (decoy inactive, `appendGroup` alone governs `mprA`/`mprB`): `kulbubidu` ->
  `ok::KUL+MB+MA+MREQ|kulbubidu` (setting `mprA` via `mrMA` does not evict `mprB`, since the owning
  group's output is `append`).
- After (decoy activated): no parse (`-`).

Mechanism: `MprFeatureGroup`'s loader assigns each `MprFeature`'s `.Group` pointer to whichever
*active* group last claims it (`MprFeaturesChanged`'s `CollectionChanged` handler; the two features'
memberships live on a shared field on the `MprFeature` object, not a per-group list). The original
decoy named only `mprA` — even if activated, it would only ever try to evict members of *its own*
one-feature collection (itself), so `mprB` was never at risk and the decoy was provably inert regardless
of `isActive`. Once the decoy claims *both* features, activating it hijacks their group membership
away from `appendGroup` into a same-named-features-but-`overwrite`-by-default group, which evicts
`mprB` the moment `mprA` is set — exactly mirroring the direct `outputType` mutation below.

### MorphologicalPhonologicalRuleFeatureGroup/outputType/append

**EVIDENCED**, using the same construct as above.

- Before (`appendGroup outputType="append"`): `kulbubidu` -> `ok::KUL+MB+MA+MREQ|kulbubidu`.
- After (mutated to `overwrite`): no parse (`-`) — setting `mprA` now evicts `mprB` (the group's
  `AddOutput` sweep removes any of its own members not present in the newly-added feature set), so
  `mrMReq`'s `requiredMPRFeatures="mprB"` check fails.

### MorphologicalRule/blockable/false

**EVIDENCED.** `mrUnblockable` (existing rule, `blockable="false"`) was given `OutputHeadFeatures`
(`featX=vA`, a newly-added `HeadFeatures` symbolic feature). Added a dedicated root `bak` in a new
family `famBlk`, with a suppletive sibling `dom` (same family, `AssignedHeadFeatures featX=vA` —
lexically pre-assigned the same feature `mrUnblockable`'s output would assign). Word `bakgi`
(`bak` + `mrUnblockable`).

- Before (`blockable="false"`): `bakgi` -> `ok::VAK+UNBLK|bakgi` (unblocked).
- After (mutated to `blockable="true"`): `bakgi` -> no parse (`-`) — `Word.CheckBlocking` now finds
  `dom` as a more-specific same-family competitor whose own features subsume the output, and
  synthesis-side resynthesis-confirmation rejects `bakgi` as a string no longer producible.

`kulgi` (the pre-existing word exercising `mrUnblockable`) is deliberately untouched by this: `eKul`
carries no `family`, so `Word.CheckBlocking` short-circuits (`family == null`) before ever reaching the
new feature — this is why a *dedicated*, isolated root was necessary rather than adding a family to
`eKul` directly, which would have put every other `eKul`-rooted control (repetition caps, etc.) at risk
of an unrelated blocking interaction.

### MorphologicalRule/partial/true

**EVIDENCED**, via a new second `Stratum` (`Second`, `morphologicalRuleOrder="unordered"`), separate
from `Main`. Added root `pog`, template `pogTemplate` (final, one slot -> `mrPogAffix`, suffix `-li`),
and a stratum-level-only rule `mrPO` (`partial="true"`, suffix `-ti`, never a member of any `Slot`).
Word `pogliti`.

- Before (`mrPO partial="true"`): `pogliti` -> `ok::POG+POGAFX+PO|pogliti`.
- After (mutated to `partial="false"`): `pogliti` -> no parse (`-`).

Mechanism, and why this needed its own stratum: `partial` on a `MorphologicalRule` only has any effect
on a rule with `IsTemplateRule == false` (`SynthesisAffixProcessRule.cs`'s two gating checks are both
guarded by `!_rule.IsTemplateRule`) — and *any* rule that is ever placed in an `AffixTemplate` `Slot`
has `IsTemplateRule` permanently set `true` on the shared rule object (`AffixTemplate.cs`'s
`SlotsChanged` handler), even where the *same* rule is *also* listed at `Stratum` level. So the rule
under test must never appear in any `Slot`, only in the `Stratum`'s own `morphologicalRules`. Separately,
the specific gate being tested — "a non-partial rule may not apply right after a final template,
unless the rule itself is partial" (`NonPartialRuleProhibitedAfterFinalTemplate`) — can only ever be
reached when a stratum-level rule is attempted *after* a template has already run, and
`SynthesisStratumRule.Apply`'s `ApplyTemplates`-then-`ApplyMorphologicalRules` recursion that produces
that ordering is only taken on an `Unordered` stratum (never `Linear`, which `Main` deliberately is, per
its own header comment about repeated-rule legibility). `pogliti` is the only string reachable through
that exact order: `mrPogAffix` (`li`) applied by the template first, `mrPO` (`ti`) applied second —
applying `mrPO` first instead yields `pogtili`, a different string, so there is no alternate path that
could make this a false control.

### RealizationalRule/blockable/false

**EVIDENCED**, same blocking mechanism as `MorphologicalRule/blockable/false`, applied to `mrReal`
(the existing `RealizationalRule`, `blockable="false"`) directly — `RealizationalRule` has no
`OutputHeadFeatures` element of its own, so the competing feature was placed directly on the lexical
entries instead: dedicated root `sim` (family `famBlk3`, `AssignedHeadFeatures featX=vA`) with
suppletive sibling `rog` (same family and feature). Word `simru` (`sim` + `mrReal`).

- Before (`blockable="false"`): `simru` -> `ok::SIM+REAL|simru`.
- After (mutated to `blockable="true"`): `simru` -> no parse (`-`).

### Stratum/cyclicity/cyclic

**NOT EVIDENCEABLE — confirmed engine gap, not a modeling gap.**

What was tried: every avenue that could plausibly make cyclicity observable requires the engine to
consult it during synthesis or analysis. `grep -rn "Cyclicity\|IsCyclic" src/SIL.Machine.Morphology.HermitCrab
--include=*.cs` returns zero hits anywhere in the engine (checked the whole namespace, not just the
loader). `Stratum.cs` — the class instantiated for every stratum — has no `Cyclicity` property at all;
its only rule-order-shaped property is `MorphologicalRuleOrder`. `XmlLanguageLoader.LoadStratum` reads
`characterDefinitionTable`, `morphologicalRuleOrder`, `phonologicalRules`, and `morphologicalRules` off
the `<Stratum>` element, and nothing else — `cyclicity` and `phonologicalRuleOrder` are parsed by
nothing, not even into a discarded local variable.

What would be required to fix this: the C# engine itself would need a `Cyclicity` property on
`Stratum` and a corresponding branch in the synthesis/analysis pipeline that behaves differently for
`cyclic` vs `noncyclic` — almost certainly re-running a stratum's own phonological rules against its
own newly-derived output before moving to the next stratum, which is the textbook meaning of cyclic
rule application in this architecture. That is an engine change, not a fixture change; no grammar
shape, phonological or otherwise, can make an attribute observable that literally no code path reads.

**Recommendation:** this is worth a real product-gap ticket against
`SIL.Machine.Morphology.HermitCrab`, not a fixture workaround. Stop trying to evidence it via a
fixture; that is the correct and acceptable outcome given the confirmed absence of any consuming code.

### Stratum/phonologicalRuleOrder/simultaneous

**NOT EVIDENCEABLE**, for the identical reason as `cyclicity` above, confirmed by the same grep
(`PhonologicalRuleOrder` — zero hits in the engine outside the DTD and the loader's own unread
attribute text) and the same absence of a `Stratum.cs` property. Note this is a *different* construct
from `RewriteRule`'s own `Simultaneous` `ApplicationMode` (a per-rule attribute that genuinely is
implemented, in `AnalysisRewriteRule.cs`/`SynthesisRewriteRule.cs`/`SimultaneousPhonologicalPatternRule.cs`)
— it would be easy to mistake the working per-rule mechanism for evidence that the *stratum-level*
attribute of the same name also does something, and it does not. Same recommendation as `cyclicity`.

---

## loader-isactive-breadth

### AffixTemplate/isActive/no

**EVIDENCED.** `decoyTemplate`'s slot was repointed from `mrPfx` (shared with `liveTemplate`) to a new
exclusive rule `mrTemplateOnly` (prefix `qu-`, never stratum-listed, never in any other template).
Word `qukul`.

- Before (`decoyTemplate isActive="no"`): `qukul` -> no parse (`-`).
- After (mutated to `isActive="yes"`): `qukul` -> `ok::QU+KUL|qukul`.

The original design reused `mrPfx` in `decoyTemplate`'s slot, with the comment that an
isActive-ignoring loader would give `takul` "a duplicate analysis" — but applying the identical rule
object through either template produces the *same* `Word` (same morphs, same shape), and that
duplicate is silently deduplicated by `Word`'s value equality before it ever reaches the signature
string, so activating `decoyTemplate` changed nothing observable. Exactly the same failure mode as
`AffixTemplate/final/false` above, and the same fix: an exclusive rule with no alternate route.

### BoundaryDefinition/isActive/no

**EVIDENCED.** Word `kul=`, containing the literal character declared only by the inactive `bDecoy`.

- Before (`bDecoy isActive="no"`): `kul=` -> `SKIPPED` (invalid shape — `=` is not a recognized
  character at all).
- After (mutated to `isActive="yes"`): `kul=` -> `ok::-` (now tokenizable, but no rule produces a bare
  `=`, so still no successful parse). The change from `SKIPPED` to `ok::-` is the delta.

Mirrors the existing `SegmentDefinition`/`zal` control in the same fixture exactly, one level down
(boundary characters vs. segment characters).

### BoundaryDefinition/isActive/yes

**EVIDENCED, via `RequiredToLoad`** (renamed `RequiredByLoader` when the verdict was later split into
`RequiredByDtd`/`RequiredByLoader`; this is the loader-throws kind, not the DTD-validation kind — see
`docs/coverage-levels.md`). Added `mrBoundaryPfx`, a new `MorphologicalRule` whose own output
inserts the *live* boundary character itself (`InsertSegments` of `mo`, then a separate
`InsertSegments` of `+`, then the copied stem — boundaries need their own `InsertSegments` element;
concatenating `mo+` into one `PhoneticShape` string fails to tokenize even when both characters are
individually declared). Reachable through a new optional slot on `liveTemplate`. Word `mo+kul`.

- Before (`bLive isActive="yes"`): `mo+kul` -> `ok::MO+KUL|mo+?kul`.
- After (mutated to `isActive="no"`): the grammar fails to **load** entirely — `Load Error: The
  shape, +, contains an undefined phoneme at 0` — because `mrBoundaryPfx`'s own `PhoneticShape` is
  parsed against the character inventory at rule-load time, not lazily at parse time. Per
  `CounterfactualVerdict.RequiredByLoader`'s own docstring, this is "equally conclusive" evidence, the
  same category the pre-existing `CharacterDefinitionTable/isActive/yes` (`tableLive`) surface already
  uses in this fixture.

### CharacterDefinitionTable/isActive/no

**NOT EVIDENCEABLE — confirmed structural deadlock, not a missed construct.**

What was tried: the only way an inactive `CharacterDefinitionTable` (`tableDecoy`) could ever change a
parse result is to be *referenced* by something (a `Stratum`'s `characterDefinitionTable` attribute)
that stays active. Built exactly that: a third, active `Stratum` in a scratch copy of this fixture,
`characterDefinitionTable="tableDecoy"`. Result, confirmed by running it through the oracle:

```
Load Error: The given key 'tableDecoy' was not present in the dictionary.
```

`XmlLanguageLoader.LoadStratum` resolves the `characterDefinitionTable` IDREF through
`_tables[(string)stratumElem.Attribute("characterDefinitionTable")]` — a direct `Dictionary` indexer,
which throws the moment the referenced table was never loaded (i.e., was inactive). There is no
tolerant lookup path anywhere in this collection's resolution.

Why this cannot be fixed by fixture construction: the reference has to exist in the *unmutated*
baseline grammar (the surface being tested is `isActive="no"`, and the counterfactual only ever flips
*this one* attribute — it cannot also introduce a new reference only in the mutated run). With
`tableDecoy` inactive (the baseline, correctly reflecting its declared value), any active reference to
it throws immediately, and `CounterfactualLedger.Sweep`'s own error handling marks **every surface in
the fixture** `Unobservable` when the baseline itself fails to load ("the fixture itself does not
load") — which would silently un-evidence the eight *other* surfaces already fixed in this same file.
There is no way to make this ONE surface observable without breaking all the others' baseline, so the
correct outcome is to leave `tableDecoy` unreferenced, exactly as the original fixture already does.

**Recommendation:** accept this as-is. It is not a product gap in the same sense as `cyclicity` — the
engine *does* correctly honor `CharacterDefinitionTable@isActive` (that is precisely why the reference
throws) — it is a gap in what a single-fixture, single-attribute counterfactual methodology can prove
about a fail-fast IDREF. A different harness design (e.g., one that could vary *two* attributes
together — activate the decoy *and* introduce its reference in the same mutation) could close this, but
that is a change to `CounterfactualGate`/`GrammarMutator`, not to this fixture.

### Family/isActive/no

**NOT EVIDENCEABLE**, identical reasoning and identical confirmation method as
`CharacterDefinitionTable/isActive/no` above. Pointed the *already-active* `eKul` entry's `family`
attribute at the inactive `famDecoy` in a scratch copy:

```
Load Error: The given key 'famDecoy' was not present in the dictionary.
```

`TryLoadLexEntry` resolves `family` through `_families[familyID]`, the same fail-fast indexer pattern.
Same recommendation: accept as-is; a fixture-only fix would break every other surface's baseline.

### FeatureNaturalClass/isActive/no

**NOT EVIDENCEABLE**, same reasoning. Retargeted the live `mrPfx` rule's own `SimpleContext
naturalClass` from `ncAny` to the inactive `ncDecoy`:

```
Load Error: The given key 'ncDecoy' was not present in the dictionary.
```

`NaturalClass` IDREF resolution (`_natClasses[natClassID]`) is the same fail-fast indexer pattern. Same
recommendation.

### MorphologicalPhonologicalRuleFeature/isActive/no

**NOT EVIDENCEABLE**, same reasoning. Pointed `eKul`'s `ruleFeatures` at the inactive `mprDecoy`:

```
Load Error: The given key 'mprDecoy' was not present in the dictionary.
```

`LoadMprFeatures` resolves each space-separated ID through `_mprFeatures[mprFeatID]`, the same
fail-fast indexer pattern (no `TryGetValue`, unlike `Stratum`'s own `morphologicalRules` resolution —
see `MorphologicalRule/isActive/no` below for the contrast that makes *that* surface separable). Same
recommendation.

### MorphologicalRule/isActive/no

**EVIDENCED.** Added `mrRuleOnly` (`isActive="no"`, prefix `qo-`), reachable *only* through a new
`ruleOnlySlot`, itself fully `isActive="yes"`. Word `qokul`.

- Before (`mrRuleOnly isActive="no"`): `qokul` -> no parse (`-`).
- After (mutated to `isActive="yes"`): `qokul` -> `ok::QO+KUL|qokul`.

This is the fix the task's own hint pointed at: the original `mrDecoy`/`decoySlot` pair is jointly
inactive, so flipping either one alone changes nothing (both must be active for `mrDecoy` to be
reachable). The key enabling fact, confirmed by reading `XmlLanguageLoader.LoadAffixTemplate`: a
`Slot`'s own `morphologicalRules` IDREFs are resolved through `Dictionary.TryGetValue`, not the
fail-fast indexer the four collections above use — an active slot naming an inactive rule loads
*fine*, with that rule simply absent from the slot's rule list. That tolerance is exactly what makes
this surface (and the next one) separable from the four `NOT EVIDENCEABLE` ones above, which use the
fail-fast pattern instead.

### Slot/isActive/no

**EVIDENCED**, via the same tolerant-lookup mechanism. Added `mrSlotOnly` (`isActive="yes"`, prefix
`qe-`), reachable *only* through a new `slotOnlySlot`, itself `isActive="no"`. Word `qekul`.

- Before (`slotOnlySlot isActive="no"`): `qekul` -> no parse (`-`) — the slot itself is never even
  loaded (`AffixTemplate`'s own `Elements("Slot").Where(IsActive)` filters it out before its
  `morphologicalRules` attribute is ever read), so the active `mrSlotOnly` has no route to attach.
- After (mutated to `isActive="yes"`): `qekul` -> `ok::QE+KUL|qekul`.

---

## Summary

| Fixture | Evidenced | Not evidenceable | Total |
|---|---|---|---|
| morphotactic-attribute-breadth | 14 | 2 | 16 |
| loader-isactive-breadth | 5 | 4 | 9 |
| **Total** | **19** | **6** | **25** |

The 6 not-evidenceable surfaces split into two genuinely different findings:

1. **Stratum/cyclicity, Stratum/phonologicalRuleOrder** — the C# engine never reads these attributes
   anywhere outside the DTD-default parse; `Stratum.cs` has no corresponding property. This is a real
   engine gap worth a product ticket.
2. **CharacterDefinitionTable, Family, FeatureNaturalClass, MorphologicalPhonologicalRuleFeature, all
   `isActive="no"`** — the engine *does* enforce `isActive` correctly here (confirmed: an active
   reference to an inactive one of these throws exactly as it should), but the only way to make that
   enforcement observable in a *parse-result* delta requires a live reference that would make the
   fixture's own unmutated baseline fail to load, which the counterfactual harness cannot distinguish
   from "this surface is untestable." This is a limitation of the single-attribute counterfactual
   methodology against fail-fast IDREF collections, not a defect in the fixtures or the engine.
