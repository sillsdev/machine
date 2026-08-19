# Counterfactual rationale: feature-system, compounding, feature-gating, and two languages/ fixtures

15 DTD surfaces assigned, across six fixtures, plus one challenged impossibility proof:

- `conformance/edge-cases/feature-system-breadth/` (7 surfaces)
- `conformance/edge-cases/compounding-breadth/` (3 surfaces)
- `conformance/edge-cases/feature-gating-breadth/` (2 surfaces)
- `conformance/languages/fusional-realizational-morphology/` (1 surface)
- `conformance/languages/suffixing-extension-slot-ordering/` (1 surface)
- `conformance/edge-cases/right-to-left-anchor-environment/` (1 surface)
- `conformance/semantic-coverage-proofs.tsv`'s `dtd:enum/VariableFeature/name/%CE%B1` label-symmetry
  claim (challenged, not edited — that file was left untouched per instructions)

10 of the 15 surfaces are now EVIDENCED. 5 are NOT EVIDENCED, for two distinct, source-confirmed
reasons (never "I couldn't think of a word"). The challenged proof turned out to be **wrong**: a
constructed counterexample reverses it against the C# oracle, deterministically, on both sides.

## Method note

For every EVIDENCED surface below, the "before → after" outcome was obtained by:

1. Copying the real fixture's `grammar.xml` to a scratch file.
2. Hand-applying the identical mutation `GrammarMutator.DeleteElement`/`NeutralizeEnumValue` would
   apply (verified against `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/
   GrammarMutator.cs`: element surfaces delete every matching element in the document; enum surfaces
   rewrite every matching `element@attribute=value` occurrence to the alphabetically-first *other*
   declared value of that attribute across the whole grammar-observable inventory).
3. Running both the original and mutated grammar through
   `conformance/adapters/hc-dotnet-wrapper.sh batch` (the C# founding oracle, not `pangloss`) over the
   discriminating word(s), and diffing the two TSV outputs.

No signature below was hand-derived; every one is transcribed from an actual oracle run, and every
fixture's *full* word list (not just the new words) was re-run after each change and diffed against
the pre-existing outputs to confirm no regression (differences, when present, were confined to the
`ms` timing column).

---

## `edge-cases/feature-system-breadth`

### ComplexFeature (element)

**EVIDENCED.** `cfAgr` was declared but referenced by nothing, so deleting it changed nothing. Wired
it into `eAk`'s `AssignedHeadFeatures` (`<FeatureValue feature="cfAgr"><FeatureValue feature=
"hfNumber" symbolValues="numSg" /></FeatureValue>`), a real IDREF consumer that doesn't gate on
anything so it doesn't disturb `ak`'s existing phonology.

- Before: grammar loads normally; all 9 original words parse as before.
- After (both `<ComplexFeature>` elements deleted): `Reference to undeclared ID is 'cfAgr'` — the
  document no longer validates. `RequiredToLoad`, which the gate accepts as evidence identically to
  `Evidenced` (`CounterfactualGate.cs`'s `Unaccounted` filters on `Verdict is not (Evidenced or
  RequiredToLoad)`).

### ComplexFeature/isActive="no"

**NOT EVIDENCED.** `cfDecoy` is a second, inactive `ComplexFeature`. Making its activation observable
would need a `FeatureValue` referencing `cfAgr`-shaped nesting under `cfDecoy` specifically — but
`XmlLanguageLoader.LoadPhonologicalFeatureSystem`/`LoadSyntacticFeatureSystem` only add a feature to
the lookup dictionary when `IsActive` is true (`.Where(IsActive)`, `XmlLanguageLoader.cs` lines
262–274), and `LoadFeatureStruct` resolves a `FeatureValue`'s `feature` IDREF via that same
dictionary unconditionally at load time — there is no lazy/conditional resolution. Empirically
confirmed for the sibling construct (`SegmentNaturalClass`, same mechanism, see below): referencing
an inactive declaration throws `KeyNotFoundException` immediately, before any word is ever parsed.
So the *only* way to reference `cfDecoy` is from something that is *itself* inactive too — which
means the reference is filtered out before it's ever resolved, and activating `cfDecoy` alone (the
counterfactual under test) still leaves it referenced by nothing. There is no "already valid,
already referenced, decoy override" state available here, unlike `FeatureValue/isActive="no"` below
(where the decoy is an override on an *already-active, already-referenced* segment). This is a
genuine structural gap: any freestanding, non-self-contained DTD declaration (a *type*, not a
*rule*) that only becomes lookupable-by-reference when active can never have its own
`isActive="no"` value evidenced without an accomplice that's active in both the baseline and the
mutation — and no such accomplice can exist while the declaration itself stays inactive. Whoever
owns `semantic-coverage-proofs.tsv` may want a proof kind for exactly this shape (it is not
`no-consumer` — the engine *does* consume it once active+referenced — and not `blocked-by-defect`
either — nothing is broken, the load-order requirement is by design).

### ExcludedEnvironments (element)

**EVIDENCED.** `eSal`'s allomorph carries `ExcludedEnvironments` (excluded before `ncK`), but this
grammar had no morphological composition at all — every word was a bare root, so no morpheme
boundary ever existed for the exclusion to fire at. Added `mrSuffixK`, a suffix rule inserting a bare
"k" segment, applicable to every `posN` root (Stratum: `morphologicalRules="mrSuffixK"`). Word
`salk`.

- Before: `salk` → no parse (`-`); `sal` alone still parses (`SAL|sal`), unaffected.
- After (`<ExcludedEnvironments>` deleted): `salk` → `ok::SAL+SUF|salk`.

### FeatureValue/isActive="no"

**EVIDENCED.** `cT`'s decoy `<FeatureValue feature="featHigh" symbolValues="hiPlus" isActive="no" />`
was referenced by nothing that could observe it: every natural class in this grammar is an explicit
`SegmentNaturalClass` membership list (`ncT`, `ncK`, …), never a feature-value bracket, so nothing
ever consults `cT`'s own `featHigh` value. (The fixture's own prior comment on word `at` claimed this
*was* observable via a "contradictory value" — checked directly against the C# oracle and found
false; corrected in both files, see the commit.) Added `ncHighCons`, a `FeatureNaturalClass`
requiring `featHigh=hiPlus` **and** `featCons=consPlus` together (no active segment satisfies both
today — `cI` is high but a vowel, `cT` is a consonant but not high), and `prHighTrigger`, a rule that
lowers "s" to "l" after a member of `ncHighCons`. Word `ts`.

- Before: `ts` → `ok::TS|ts` (cT is `hiMinus`, not a member of `ncHighCons`, rule never fires).
- After (`isActive` rewritten `no` → `yes`): `AddValue`'s dictionary-set semantics
  (`FeatureStruct.cs` line 176, `_definite[feature] = value`) mean the *later* `FeatureValue` in
  document order wins outright — cT's `featHigh` becomes `hiPlus`, cT now qualifies for
  `ncHighCons`, and `ts` → no parse (`-`). All 15 other words unchanged.

### MetathesisRule/multipleApplicationOrder/rightToLeftIterative

**EVIDENCED.** `mtSwap`'s structural description (`[T][I]`) has exactly one candidate window per
input (a 2-class pattern with disjoint classes can never have two candidate windows overlap — shown
by direct construction: two windows sharing one segment position require that segment to
simultaneously satisfy both classes, i.e. the classes must intersect), so scan direction never
changed which pair it swaps; that's why the *existing* rule's ordering value stayed unevidenced.
Added `mtSwapOverlap`, whose two switch classes deliberately **share a segment** (`ncOvA={t,k}`,
`ncOvB={k,l}`), so a 3-segment run like `tkl` genuinely has two *overlapping* candidate windows
(t-k and k-l), and which one direction finds first decides the result. Word `tkl` (root), `tlk`,
`ktl`.

- Before (`rightToLeftIterative`, this fixture's declared order): `tlk` → `ok::TKL|tlk` (scanning
  right to left, the rightmost window k-l is found first and swapped: `t`+`lk`); `ktl` → no parse.
- After (mutated to `leftToRightIterative`): `tlk` → no parse; `ktl` → `ok::TKL|ktl` (scanning left
  to right, the leftmost window t-k is found first and swapped: `k`+`tl`).

Verified by hand-mutating a copy of the *real, committed* file (both `mtSwap` and `mtSwapOverlap`
share the value, so the real sweep flips both at once — confirmed this doesn't reintroduce ambiguity,
since `mtSwap` needs an "i" segment nowhere present in `tkl`/`tlk`/`ktl`).

### SegmentNaturalClass/isActive="no"

**NOT EVIDENCED.** Same structural reason as `ComplexFeature/isActive="no"` above, for the same
mechanism (`XmlLanguageLoader.cs` line 286: `.Elements().Where(IsActive)` builds `_natClasses` from
active declarations only; `_natClasses[natClassID]` — a plain dictionary index, no `TryGetValue`
fallback — resolves every `naturalClass` IDREF unconditionally at load time). Empirically confirmed
by directly hand-editing a copy of `feature-system-breadth/grammar.xml` to reference `ncDecoy` from
an active rule's `PhoneticInput`: `Load Error: The given key 'ncDecoy' was not present in the
dictionary.` — an immediate, whole-document load failure, before any word is parsed. `ncDecoy` (a
freestanding type declaration) can only be referenced by something *itself* inactive, which means
the reference is filtered out before resolution either way; activating `ncDecoy` alone (the
counterfactual under test) leaves it referenced by nothing regardless.

### SymbolicFeature/isActive="no"

**NOT EVIDENCED.** Identical mechanism and identical argument to the two above:
`LoadPhonologicalFeatureSystem` (`XmlLanguageLoader.cs` line 622) builds
`_language.PhonologicalFeatureSystem` from `.Where(IsActive)` only, and any `FeatureValue`
referencing `featDecoy` while it stays inactive would throw the same way at load time. Additionally
checked whether `featDecoy`'s mere *existence* (without any direct reference) could matter through
`useDefaults`/`DefaultValue` filling during unification (`FeatureStruct.cs` lines 855/946/994/1043):
that mechanism only fires for a feature that's *already present* in at least one of the two structs
being compared, so an entirely unreferenced active feature — even with a `defaultSymbol` attribute
added — never enters any actual `FeatureStruct` instance and stays fully inert either way. Confirmed
by source inspection, not just this fixture's own shape.

---

## `edge-cases/compounding-breadth`

### CompoundingSubrule/isActive="no"

**EVIDENCED**, but not the way originally drafted. Adding a second, decoy `CompoundingSubrule`
*inside* the live `crJoin` (its declared home) does not work: hand-mutating a copy showed that when
a `CompoundingRule` has two active subrules, `AnalysisCompoundingRule`'s search only ever finds an
unapplication through the **first** one declared — confirmed by swapping declaration order twice (a
word needing the *first* subrule to fire succeeded only when it was physically first, regardless of
which one the counterfactual activated). Fixed by isolating the decoy into its own single-subrule
rule, `crJoinInsert` (no ambiguity possible with only one subrule). Word `kaitu`.

- Before (`isActive="no"` on `crJoinInsert`'s own subrule): `kaitu` → no parse.
- After (`isActive` rewritten `no` → `yes`): `kaitu` → `ok::KA+TU|kaitu`.

### HeadRequiredFootFeatures (element)

**EVIDENCED.** The existing head (`ka`) only ever *satisfies* this requirement — removing it changed
nothing, since a satisfied-but-absent requirement and an absent requirement look identical. Added
`ePa` ("pa"), a root with the **wrong** head foot class (`clsB` where the rule requires `clsA`). Word
`patu` (pa + tu).

- Before: `patu` → no parse.
- After (`<HeadRequiredFootFeatures>` deleted): `patu` → `ok::PA+TU|patu`.

### HeadRequiredHeadFeatures (element)

**EVIDENCED**, same pattern. Added `eKu` ("ku"), a root with the wrong head number (`pl` where the
rule requires `sg`). Word `kutu`.

- Before: `kutu` → no parse.
- After (`<HeadRequiredHeadFeatures>` deleted): `kutu` → `ok::KU+TU|kutu`.

---

## `edge-cases/feature-gating-breadth`

### OutputFootFeatures (element)

**EVIDENCED.** `mrAgr`'s own `OutputFootFeatures` (`featStress=strYes`) is redundant with the value
it *also requires as input* (a plural, stressed root stays stressed whether or not the rule
reasserts it) — removing it changed nothing, confirmed foot features persist across a derivation
unless a rule overrides them. Added `mrLoud` (no input gate at all, **overrides** the derived word's
foot feature via its own `OutputFootFeatures`) and `mrEmph` (downstream, gated on the overridden
value), plus `eNal`, a root that starts out **unstressed** — the only way to show the override
matters, since `mrAgr`'s own root (`kal`) is already stressed on its own. Word `nalnomu`
(nal+LOUD+EMPH).

- Before: `nalnomu` → `ok::NAL+LOUD+EMPH|nalnomu` (mrLoud's own `OutputFootFeatures` makes "nal"
  stressed, satisfying mrEmph's gate).
- After (**both** `OutputFootFeatures` elements deleted, since the mutation is document-wide):
  `nalnomu` → no parse (the derived word stays unstressed, mrEmph's gate fails). All 10 other words
  unchanged.

### Properties (element)

**NOT EVIDENCED.** Source-confirmed `no-consumer`: `LoadProperties` (`XmlLanguageLoader.cs` line
590) fills a plain `Dictionary<string, object>` on `LexEntry`/`Allomorph`/rule objects
(`Morpheme.cs`, `Allomorph.cs`, `HCRuleBase.cs`), and the only other reader anywhere in the codebase
is `Morpher.cs` lines 403/423, which *copies* the dictionary onto a freshly-synthesized guess entry
during unknown-word guessing — never reads a value back out for a parse decision. No parse, no
signature, and no trace output anywhere consults it. This maps cleanly to the `no-consumer` proof
kind already defined in `semantic-coverage-proofs.tsv`'s own header (not added there, per
instructions, but worth recording as a candidate entry).

---

## `languages/fusional-realizational-morphology`

### CompoundingRule/blockable="false"

**EVIDENCED.** `mrCompoundConstrained`'s own `blockable="true"` was already pinned by the existing
`genlav` word (via family `famCompBlock`, GEN/NOV). Neither of the *other* two `CompoundingRule`s —
`mrCompoundHN`, `mrCompoundNH`, both declared `blockable="false"` — had a family sibling at all, so
rewriting their value to `"true"` changed nothing. Added `famHNBlock` with a head (`eBlockHeadHN`,
"cor") and a sibling (`eBlockSibHN`, "zon") that declares **no** `AssignedHeadFeatures`: per
`Word.CheckBlocking` (`Word.cs` line 472), `SyntacticFeatureStruct.Subsumes(entry.
SyntacticFeatureStruct)` — an empty struct trivially subsumes an empty one, so blocking here depends
only on family membership, needing no `OutputHeadFeatures` on the rule at all. Word `corriv`
(cor+riv). Verified all 55 pre-existing words unchanged (signature-for-signature, only the `ms`
column differed).

- Before (`blockable="false"`, both `mrCompoundHN`/`mrCompoundNH`): `corriv` →
  `ok::BLOCKHEAD+BLOCKNON|cor+?riv`.
- After (both rewritten to `"true"`): `corriv` → no parse (`CheckBlocking` substitutes
  `eBlockSibHN`'s own bare entry, "zon", which doesn't match the input shape).

---

## `languages/suffixing-extension-slot-ordering`

### MorphologicalOutput/redupMorphType="suffix"

**NOT EVIDENCED.** Source-confirmed `no-consumer`, and the existing fixture's own comment was
actively wrong about it. `words.yaml` claimed the value was "confirmed correct by an isolated probe"
to control whether the reduplicated word's morph chain reads `ROOT+RED` vs the reverse. Grepped the
whole engine for readers of `Allomorph.ReduplicationHint` (the field this attribute sets): only
`XmlLanguageLoader.cs` (sets it) and `XmlLanguageWriter.cs` (reads it back for round-tripping) touch
it at all — nothing in analysis, synthesis, or signature formatting ever consults it. Rewrote
`redupMorphType="suffix"` to `"implicit"` on a copy and re-ran `kimbiakimbia` through the oracle: the
signature came back byte-identical (`KIMB+RED|kimbiakimbia`). Corrected the false claim in both
`grammar.xml`'s and `words.yaml`'s comments (no semantic change to the grammar or word list) rather
than leave it standing, since it directly contradicts what a future reader would find if they
re-checked it.

---

## `edge-cases/right-to-left-anchor-environment`

### PhonologicalRule/multipleApplicationOrder/rightToLeftIterative

**EVIDENCED.** `prRtlAnchor`'s anchored environment (`finalBoundaryCondition="true"`, matching only
the word-final position) can only ever match one position regardless of scan direction — that's why
this fixture's *headline* construct never evidenced its own ordering value. Added `prSpread`, an
unanchored iterative rule ("a" → "e" immediately after "e"): `IterativePhonologicalPatternRule.Apply`
(`IterativePhonologicalPatternRule.cs`) re-matches against the *live, already-modified* word within a
single pass, scanning in `Matcher.Direction`, so a rewrite at one position can feed the next
candidate's own left-environment check *in the scan direction* — a cascade that a single anchored
match can never produce. Root `ROOT3` ("eaaa"): `prRtlAnchor` first rewrites the final "a" → "e"
("eaaa" → "eaae"), then `prSpread` runs over "eaae".

- Before (`rightToLeftIterative`, this fixture's declared order): word `eeae` →
  `ok::ROOT3|eeae` — scanning right to left, the rightmost "a" checks its (still unmodified) left
  neighbor first and doesn't yet qualify, so only one hop from the seed "e" rewrites.
- After (mutated to `leftToRightIterative`): `eeae` → no parse; `eeee` → `ok::ROOT3|eeee` instead —
  scanning left to right, each rewrite updates the string before the next position to its right is
  checked, so the "e" cascades across the whole word in one pass.

`eeee` and the raw underlying `eaaa` are kept as negative controls in the checked-in `words.yaml`
(both `expect_fail`), matching the existing fixture's own house style for `ROOT1`/`ROOT2`.

---

## Challenged proof: `dtd:enum/VariableFeature/name/%CE%B1` (label-symmetry)

**THE PROOF IS WRONG AS A UNIVERSAL CLAIM.** `semantic-coverage-proofs.tsv` claims: "Greek letters
name a phonological variable and carry no order; permuting the name produces identical output, so
the 24 values are one mechanism under 24 labels." That is true of every fixture the automated sweep
actually measures — none of them declares more than one `VariableFeature` reachable in a single rule
application (`edge-cases/feature-system-breadth`'s `prAlpha` and `languages/suffixing-vowel-harmony`'s
`prAlphaHighHarmony` each declare exactly one) — but it does not follow from anything about the
mechanism itself, and a grammar that puts two `VariableFeature`s in play together shows why: the
engine's variable-binding mechanism, `VariableBindings` (`SIL.Machine/FeatureModel/
VariableBindings.cs`), is a **single dictionary keyed by the name string for the whole match**,
shared across every `VariableFeature` a rule declares — not scoped per declared feature. Confirmed
by reading `SimpleFeatureValue.IsUnifiableImpl` (`SimpleFeatureValue.cs` lines 52–102): a bound
value is looked up and reused purely by `VariableName`, with no check that the stored value's
underlying `SymbolicFeature` matches the feature currently being compared.

Built `conformance/edge-cases/alpha-variable-name-collision/` to test this directly: one
`PhonologicalRule` with two `VariableFeature`s (`varBack`/`featBack`, `varRound`/`featRound`), each
independently self-flipping its own position (backness at position 1, roundness at position 2) — a
realistic shape (two independent vowel-harmony dimensions feeding one rule), not a contrived one.
Renamed `varBack`'s name from `"α"` to `"β"` — exactly the mutation the counterfactual sweep
performs on this surface, and exactly what "permuting the name" into a sibling value does when that
sibling is already in use elsewhere in the same rule's scope.

- Before (distinct names `α`/`β`): word `au` (the raw underlying shape) → no parse, since the rule
  fires obligatorily; word `ia` → `ok::AU|ia` (the correctly flipped surface: "a" → "i" via
  backness, "u" → "a" via roundness, independently).
- After (`varBack` renamed to `β`, colliding with `varRound`): **completely reversed** — `au` →
  `ok::AU|au` (the rule no longer transforms it correctly) and `ia` → no parse. Confirmed
  deterministic across repeated runs, not a scheduling fluke.

One further finding, itself part of the counterexample: this collision's visibility depends on
which specific symbol values occupy which ordinal position within each feature's own symbol list.
`SymbolicFeatureValue`'s internal representation (`UlongSymbolicFeatureValueFlags`) assigns each
value's bit by position within its *own* feature's declared `Symbols` list, so if `featBack` and
`featRound` declare their two symbols in the *same* relative order (`+` first both times, say), the
collision's cross-feature bit comparison can accidentally still line up and the bug stays invisible
— this fixture deliberately declares `featRound`'s symbols in the *opposite* order from `featBack`'s
to surface it (documented in `grammar.xml`'s own header; verified the same-order variant produces no
observable difference before settling on the opposite-order design). That does not weaken the
finding — the proof under test makes a claim about *every* value, unconditionally, and a
counterexample that requires a specific (realistic, DTD-legal) grammar shape to become visible is
still a counterexample, exactly as the assignment's own framing anticipated ("when two variables are
bound in one rule and the letters distinguish them").

**Recommendation:** the `label-symmetry` proof kind's own definition ("Valid only for genuinely
symmetric alphabets, never for ordered or numeric ranges") should be tightened to also exclude any
alphabet whose members can co-occur as *distinct, simultaneously-bound* variables in one scope — or
the specific claim on `dtd:enum/VariableFeature/name/%CE%B1` should be retracted and the surface
re-classified as gap rather than proven-impossible. Not applied here since editing
`semantic-coverage-proofs.tsv` was explicitly out of scope for this task.
