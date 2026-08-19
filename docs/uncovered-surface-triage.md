# Triage of the 70 uncovered semantic-coverage surfaces

Source: `conformance/semantic-coverage-baseline.txt`. Every line was read, every classification
claim was independently re-derived from `src/SIL.Machine.Morphology.HermitCrab/` and
`conformance/HermitCrabInput.dtd`, and cross-checked against
`conformance/semantic-coverage-counterfactuals.tsv` and `conformance/semantic-coverage-proofs.tsv`.
No fixture, grammar, baseline, ledger, CSV, or source file was modified.

## 1. The count

**70**, confirmed by `grep -v '^#' ... | grep -v '^$' | wc -l` — matches the task brief exactly.
Breakdown by the ledger's own classification (`awk -F'\t' '{print $2}' | sort | uniq -c`):

| Classification | Count |
|---|---|
| `alphabet-quotient` | 37 |
| `dtd-default` | 24 |
| `dead-schema` | 9 |
| `todo` | **0** |

That last row is the headline fact: as of today, the ledger's own fixture-authoring worklist
(`todo`) is **empty**. Every one of the 70 already carries a claimed justification for exclusion.
The real work in this triage was therefore not "pick the next fixture off a todo list" — it was
verifying whether those 70 justifications actually hold, per the standing warning that every
previously-claimed impossibility except the three in `semantic-coverage-proofs.tsv` turned out to
be a tooling defect.

**Result of that verification: 22 of the 24 `dtd-default` lines are misclassified.** Their stated
justification ("indistinguishable from omission, so no word can discriminate them") is true but
irrelevant to whether they can be *evidenced* — and the ledger's own counterfactual file already
proves, for the identical situation, that a DTD-default value can be evidenced by a completely
different route: write it explicitly on a load-bearing declaration, then mutate it away. That is
exactly how `dtd:enum/Stratum/morphologicalRuleOrder/linear` — itself a default value — got
evidenced. All 22 misclassified lines have a sibling value already *proven live* (an `Evidenced`,
`EvidencedJointly`, or `RequiredToLoad` verdict in the counterfactuals file), which is the only
thing standing between "nobody has tried yet" and "genuinely impossible." Detail in §2c below.

## 2. Categorisation

### (a) Needs a fixture, and an existing fixture is close — 22 surfaces

All 22 are `dtd-default` lines whose owning attribute is **proven live** because a sibling value of
the *same* (element, attribute) already has an `Evidenced`/`EvidencedJointly`/`RequiredToLoad`
verdict in `semantic-coverage-counterfactuals.tsv`. The fix in every case is the same recipe that
already worked for `Stratum/morphologicalRuleOrder/linear`: add the attribute **explicitly**, set to
its own DTD default value, on a declaration in the named fixture that is load-bearing for some word
— then the existing counterfactual sweep can mutate it away and produce a verdict on its own; no new
fixture, no new machinery.

| Surface | Fixture to extend | Live sibling evidence (why the attribute is proven to matter) |
|---|---|---|
| `ComplexFeature/isActive/yes` | `edge-cases/feature-system-breadth` | `isActive/no` → `EvidencedJointly` ('tal') |
| `FeatureValue/isActive/yes` | `edge-cases/feature-system-breadth` | `isActive/no` → `Evidenced` ('ts') |
| `MetathesisRule/isActive/yes` | `edge-cases/feature-system-breadth` | `isActive/no` → `Evidenced` ('ka') |
| `MetathesisRule/multipleApplicationOrder/leftToRightIterative` | `edge-cases/feature-system-breadth` | `.../rightToLeftIterative` → `Evidenced` ('tlk') |
| `PhonologicalRule/isActive/yes` | `edge-cases/feature-system-breadth` | `isActive/no` → `RequiredToLoad` |
| `PhonologicalSubrule/isActive/yes` | `edge-cases/feature-system-breadth` | `isActive/no` → `Evidenced` ('at') |
| `SegmentNaturalClass/isActive/yes` | `edge-cases/feature-system-breadth` | `isActive/no` → `EvidencedJointly` ('sal') |
| `SymbolicFeature/isActive/yes` | `edge-cases/feature-system-breadth` | `isActive/no` → `EvidencedJointly` ('ik') |
| `RealizationalRule/isActive/yes` | `edge-cases/morphotactic-attribute-breadth` | `isActive/no` → `Evidenced` ('kulsi') |
| `MorphologicalRule/blockable/true` | `edge-cases/morphotactic-attribute-breadth` | `blockable/false` → `Evidenced` ('bakgi') |
| `RealizationalRule/blockable/true` | `edge-cases/morphotactic-attribute-breadth` | `blockable/false` → `Evidenced` ('simru') |
| `MorphologicalRule/partial/false` | `edge-cases/morphotactic-attribute-breadth` | `partial/true` → `Evidenced` ('pogliti') |
| `MorphologicalRule/multipleApplication/1` | `edge-cases/morphotactic-attribute-breadth` | `.../0` and `.../2` both `Evidenced` |
| `CompoundingRule/isActive/yes` | `edge-cases/compounding-breadth` | `isActive/no` → `Evidenced` ('kaptu') |
| `CompoundingSubrule/isActive/yes` | `edge-cases/compounding-breadth` | `isActive/no` → `Evidenced` ('kaitu') |
| `CompoundingRule/multipleApplication/1` | `edge-cases/compounding-breadth` | `.../0` → `Evidenced` ('kamtu') |
| `PhoneticTemplate/finalBoundaryCondition/false` | `edge-cases/right-to-left-anchor-environment` | `.../true` → `Evidenced` ('aae') |
| `PhonologicalRule/multipleApplicationOrder/leftToRightIterative` | `edge-cases/right-to-left-anchor-environment` | `.../rightToLeftIterative` → `Evidenced` ('eeae') |
| `PhonologicalFeatureSystem/isActive/yes` | `edge-cases/loader-isactive` | `isActive/no` → `RequiredToLoad` |
| `PhoneticTemplate/initialBoundaryCondition/false` | `languages/suffixing-vowel-harmony` | `.../true` → `Evidenced` ('kutagida') |
| `Slot/optional/false` | `edge-cases/diacritic-segments` | `.../true` → `Evidenced` ('año') |
| `MorphologicalOutput/redupMorphType/implicit` | `languages/metathesis-phase-isolation` (two independent `redupMorphType="prefix"` declarations already exist, lines 356 and 371) | `prefix`/`suffix` both `Evidenced`; mutating one existing `prefix` rule to `implicit` was already the mutation the sweep tried and it changed 'tulatula' (RDPL+TULA → TULA+RDPL) — crediting currently lands on `prefix` (the mutated-FROM value), not `implicit` |

What has to be added, concretely, for each: an explicit `isActive="yes"` / `blockable="true"` /
`partial="false"` / `multipleApplication="1"` / `multipleApplicationOrder="leftToRightIterative"` /
`{initial,final}BoundaryCondition="false"` / `optional="false"` / `redupMorphType="implicit"`
attribute on a declaration that is *already* load-bearing for a word in that fixture (per the table
above), written out instead of relying on the default. No new word is needed if the declaration is
already load-bearing on its own; if the fixture's *only* load-bearing declaration of that kind
already carries the non-default sibling value (this is the case for `MorphologicalOutput`, where
`suffix`/`suffix`-alike `implicit` are output-identical in `suffixing-extension-slot-ordering`, per
that file's own header comment at line 381 — "'suffix' (here) and 'implicit' both produce
ROOT-then-RED" — so that fixture is a dead end for this specific surface), a second declaration or a
second discriminating word may be needed instead, as noted in the `metathesis-phase-isolation`
row.

**Caveat, stated plainly:** I did not make these edits or run the gate — the task is read-only. This
is a **high-confidence prediction**, not a verified result: it follows the identical logical shape
already used successfully for `Stratum/morphologicalRuleOrder/linear`, applied to 22 lines whose
attribute is independently proven live by a sibling's real counterfactual verdict. The one place I
found a documented reason a naive attempt would fail (`MorphologicalOutput/redupMorphType/implicit`
in `suffixing-extension-slot-ordering`) is called out above so the next attempt doesn't repeat it.

### (b) Needs a new fixture — 0 surfaces

None. Every surface that needed a discriminating word could be reached by extending a fixture that
already exercises the same element/attribute pair (bucket a) or is genuinely excluded (bucket c).

### (c) Plausibly impossible to evidence — 48 surfaces

**9 `dead-schema` — verified `no-consumer`, matches the gate's own algorithm exactly.**
`DeadSchemaDetector.FindUnreferenced` (`src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/DeadSchemaDetector.cs:33-50`)
looks for a quoted string literal `"ElementName"` or `nameof(ElementName)` anywhere under
`src/SIL.Machine.Morphology.HermitCrab/*.cs`. I ran the same search by hand for all 7 owning
elements and got zero hits for every one, confirming the classification:

- **Subcategorization cluster (6 surfaces, one root cause):** `SyntacticRule`, `SyntacticRules`,
  `OutputSubcategorizationOverride`, `OutputSubcategorizationOverrides`, plus
  `OutputSubcategorizationOverride/isActive/{yes,no}`. The DTD wires these through
  `requiredSubcategorizedRules`, `subcategorizations`, `outputSubcategorization`,
  `headSubcategorizedRules`, `nonHeadSubcategorizedRules` (`conformance/HermitCrabInput.dtd:255,285,341,343,372-373,377`)
  — I grepped all five attribute names against the engine source and got **zero hits for every
  one**, confirming the entire syntactic-subcategorization feature is unimplemented end to end, not
  just the `SyntacticRule` element in isolation.
- **Cross-word-context cluster (3 surfaces):** `NextWord`, `PreviousWord`, `Null`
  (`conformance/HermitCrabInput.dtd:203-215`) — a `PhoneticTemplate`-or-`Null` choice describing an
  adjacent word's shape. Zero engine references to any of the three names.

**2 `dtd-default` backed by a real proof (not a tooling defect):**
`Stratum/cyclicity/noncyclic` and `Stratum/phonologicalRuleOrder/linear`. Unlike the 22 above, these
two are correctly excluded — `semantic-coverage-proofs.tsv:34-35` records a hand-verified
`no-consumer` proof for their *non-default* siblings (`cyclic`, `simultaneous`): "No Cyclicity
property exists on Stratum.cs and the identifier appears nowhere in the engine... mutating both
attributes produced a byte-identical `InfiniteLoopException`." Because the sibling is proven dead
rather than proven live, there is no live mechanism to mutate the default value against, so these
two are the genuine article: the same shape of claim as the 22 misclassified lines, but actually
verified, and they should stay `dtd-default`.

**37 `alphabet-quotient` — human-judgment classification, spot-verified plausible, but the single
category most worth continued scrutiny.** `GrammarCoverageGate.Classify`
(`src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/GrammarCoverageGate.cs:275-278`)
says explicitly: *"an existing alphabet-quotient decision is human judgement and is carried forward"*
— unlike `dead-schema` and `dtd-default`, this classification is **not** independently re-derived
each run; the gate only checks that a sibling of the same (element, attribute) is covered
(`UnbackedQuotients`, same file lines 206-231), never that the "same mechanism" claim is actually
true. That is precisely the kind of claim the task's prior warning is about (the removed
`label-symmetry` proof kind, discussed below, was exactly this shape and was wrong). I verified both
clusters by reading the consuming code rather than trusting the label:

- **`multipleApplication` count quotient (15 surfaces: `CompoundingRule` 2-9, `MorphologicalRule`
  3-9).** The consumer is a flat numeric bound check —
  `input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount`
  (`src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisAffixProcessRule.cs:46`, and
  the equivalent in `SynthesisCompoundingRule.cs:50`) — with no branch on the specific magnitude.
  The evidenced counterfactual for `CompoundingRule/multipleApplication/0` (`edge-cases/compounding-breadth`)
  shows `0` is a genuinely distinct code path (blocks the rule outright, since `0 >= 0` is
  immediately true), which is exactly why `0` needed its own evidence and is *not* quotiented — but
  `2` through `9` are all just larger integers hitting the identical `>=` comparison, so quotienting
  them against the evidenced `2`(MorphologicalRule)/`0`(CompoundingRule, per `UnbackedQuotients`'
  sibling-coverage check) is sound.
- **`VariableFeature/name` Greek-letter quotient (22 surfaces, γ through ω).** The name is loaded
  into a `Dictionary<string,Tuple<string,SymbolicFeature>>` keyed by ID and consumed purely as a
  string key for `SymbolicFeatureValue`'s variable-binding unification
  (`src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs:1370-1374,1450-1458`) — the specific
  letter never drives a branch, only its identity as a dictionary key does.
  `conformance/edge-cases/alpha-variable-name-collision/grammar.xml:1-25` documents exactly why this
  is a *narrower* and correct claim, distinct from the **removed** `label-symmetry` proof kind
  (`semantic-coverage-proofs.tsv:26-30`): `label-symmetry` claimed all 24 names were
  interchangeable with each other (false — two co-occurring variables sharing a name collide and
  reverse the parse, which this fixture proves with 'au'/'ia'). `alphabet-quotient` claims something
  weaker and true: a *third, non-colliding* name (e.g. γ) exercises the identical single-variable
  binding mechanism that the covered pair (α, β) already demonstrates. I re-derived this from the
  code rather than trusting the fixture's own comment, and it holds.

### (d) Unclear — 0 surfaces

Every surface resolved to (a) or (c) with a specific, checkable reason. Nothing was left as "I
couldn't determine this."

## 3. Clusters (the actionable grouping)

| Cluster | Surfaces | Bucket | One fixture edit closes... |
|---|---|---|---|
| `isActive="yes"` default, live-consumer elements | 11 | (a) | up to 8 at once (`feature-system-breadth`) |
| `blockable`/`partial`/`multipleApplication`/`multipleApplicationOrder` defaults on morphological/compounding/phonological rules | 8 | (a) | up to 5 at once (`morphotactic-attribute-breadth`) |
| `PhoneticTemplate` boundary-condition defaults | 2 | (a) | 1 each, two different fixtures |
| `Slot/optional` default | 1 | (a) | 1 |
| `MorphologicalOutput/redupMorphType` default | 1 | (a) | 1, with a known dead end to avoid |
| Syntactic subcategorization (element+enum) | 6 | (c) dead-schema | none — genuinely unimplemented |
| Cross-word phonological context | 3 | (c) dead-schema | none — genuinely unimplemented |
| `Stratum` ordering-mode defaults | 2 | (c) dtd-default, backed by real no-consumer proof | none — proven dead |
| `multipleApplication` magnitude alphabet | 15 | (c) alphabet-quotient | none — quotiented, verified sound |
| `VariableFeature/name` Greek-letter alphabet | 22 | (c) alphabet-quotient | none — quotiented, verified sound |

## 4. Ranked top-10 next actions

Ranked by surfaces retired per fixture touched, restricted to bucket (a) since that is the only
bucket with real remaining leverage:

1. **Extend `edge-cases/feature-system-breadth`** — write the default value explicitly on the
   already-load-bearing `ComplexFeature`, `FeatureValue`, `MetathesisRule` (×2 attributes),
   `PhonologicalRule`, `PhonologicalSubrule`, `SegmentNaturalClass`, and `SymbolicFeature`
   declarations that a sibling value already proves live. **Up to 8 surfaces.**
2. **Extend `edge-cases/morphotactic-attribute-breadth`** — same recipe for `RealizationalRule`
   (isActive, blockable), `MorphologicalRule` (blockable, partial, multipleApplication). **Up to 5
   surfaces.**
3. **Extend `edge-cases/compounding-breadth`** — same recipe for `CompoundingRule` (isActive,
   multipleApplication) and `CompoundingSubrule` (isActive). **Up to 3 surfaces.**
4. **Extend `edge-cases/right-to-left-anchor-environment`** — `PhoneticTemplate/finalBoundaryCondition`
   and `PhonologicalRule/multipleApplicationOrder`. **2 surfaces.**
5. **Extend `edge-cases/loader-isactive`** — `PhonologicalFeatureSystem/isActive/yes`. **1 surface,**
   but cheapest of the singles since the fixture already exists solely for this attribute.
6. **Extend `languages/suffixing-vowel-harmony`** — `PhoneticTemplate/initialBoundaryCondition`.
   **1 surface.**
7. **Extend `edge-cases/diacritic-segments`** — `Slot/optional/false`. **1 surface.**
8. **Extend `languages/metathesis-phase-isolation`** — `MorphologicalOutput/redupMorphType/implicit`,
   using one of its two existing `prefix` declarations (avoid `suffixing-extension-slot-ordering`,
   which is a documented dead end for this specific value). **1 surface, but non-trivial:** needs
   checking that the *other* `prefix` declaration stays independently load-bearing so `prefix`
   doesn't regress to unevidenced.
9. **Build a mechanical re-deriver for `alphabet-quotient`**, mirroring `DeadSchemaDetector`'s
   "recomputed, never trusted" pattern, so the 37 quotient lines stop being pure human judgement.
   Not a coverage win (they are already correctly excluded, per §2c), but it retires the one
   remaining category where a future misclassification could hide undetected, the same way
   `label-symmetry` did.
10. **Re-check `conformance/semantic-coverage-presence-waivers.txt`'s four `isActive/no` "no word
    targets this" entries** (`CharacterDefinitionTable`, `Family`, `FeatureNaturalClass`,
    `MorphologicalPhonologicalRuleFeature`) against the counterfactuals file: all four now carry an
    `EvidencedJointly` verdict via the three-run joint-mutation protocol documented in
    `edge-cases/loader-isactive-breadth/grammar.xml:22-34`, which looks like real evidence, not
    presence — the waiver file may itself be stale. This is outside the 70-surface scope of this
    triage (waivers don't affect the baseline's uncovered count) but was found along the way and is
    cheap to confirm.

## 5. Baseline staleness check

Cross-referenced all 70 surface IDs against `semantic-coverage-counterfactuals.tsv` directly (every
row keyed by the same surface-ID scheme): **zero matches.** None of the 70 baseline-uncovered
surfaces already has a counterfactual verdict recorded, so the baseline file itself is not stale
against the counterfactuals ledger — nothing on the uncovered list is secretly already evidenced.
The staleness that *does* exist is upstream of the baseline: the `dtd-default` classification
attached to 22 of the 70 lines rests on a justification (§1, §2a) that the ledger's own evidence
elsewhere contradicts, and separately (§4 item 10) the presence-waivers file may be stale for four
unrelated `isActive/no` surfaces.
