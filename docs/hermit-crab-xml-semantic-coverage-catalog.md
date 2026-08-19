# Canonical HermitCrab XML semantic coverage catalog

Status: design and initial source-derived inventory<br>
Catalog profile: `sil.machine.hc-feature/v1`<br>
Combination profile: `sil.machine.hc-combination/v1`

## 1. Purpose and claim boundary

Machine owns the semantic denominator for HermitCrab XML conformance. The denominator is the set of
meaningful grammar shapes that the XML format can express and the C# HermitCrab loader and engine can
execute, reject, or fail to execute. PanGloss and other consumers run their implementations against
that Machine-owned denominator; they do not define it.

This catalog is intentionally above compiler lowering. Two XML features remain distinct when they
reach the same low-level operation but enter it through different grammar semantics. A prefix inserted
by an ordinary affix rule, an epenthetic rewrite, and a realizational suffix may all eventually create
segments, but they differ in anchoring, gating, scheduling, inversion, and analysis identity. Each has
its own coverage obligation.

“Full coverage” here means complete coverage of a finite, named partition of the unbounded XML space:

1. every XML construct, enum value, meaningful default, and loader-derived semantic branch is
   classified;
2. every admitted feature leaf has positive and negative or contrastive evidence;
3. every compatible rule-category pair and connected triple, including repetition, is required or
   explicitly retired;
4. every applicable scheduling topology is required or explicitly impossible;
5. a required obligation earns coverage only from discriminating C# HermitCrab evidence; and
6. every consumer backend either matches the C# result or returns an approved explicit refusal.

This is not every XML document. IDs, strings, feature values, rule counts, pattern bounds, and nesting
depths are unbounded. Those values are partitioned by semantics, with boundary-value and seeded fuzz
tests layered above the categorical obligations.

The v1 oracle contract is the final analysis set returned by the complete C# `Morpher.ParseWord`
pipeline. That pipeline is deliberately bidirectional: analysis/unapplication generates candidates,
synthesis reapplies the grammar to confirm them, and final validity checks remove candidates. The
catalog therefore records three effects for every semantic leaf: `analysisCandidateEffect`,
`synthesisConfirmationEffect`, and `finalParseEffect`. An effect can be `none`, but it cannot be
omitted. Standalone C# word generation is a separate runtime capability and ledger; it must not be
inferred from parse conformance.

## 2. Sources and fail-closed completeness

The catalog is derived from all of the following, not from the DTD alone:

- `src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd`: syntactically expressible XML;
- `XmlLanguageLoader.cs`: what is loaded, defaulted, derived, ignored, or rejected;
- the HermitCrab model and compiled analysis/synthesis rules: actual execution semantics;
- `conformance/constructs.txt`: the existing coarse human checklist;
- conformance `grammar.xml` and `words.yaml`: existing evidence, not the definition of the space.

The eventual machine-readable catalog must have a completeness gate against the frozen XML surface.
Adding a DTD element/attribute/enum, a loader switch branch, a public grammar-model rule kind, or a
derived execution branch must fail CI until it is mapped to one of these dispositions:

| Disposition | Meaning | Required evidence |
|---|---|---|
| `semantic` | Loaded and can affect an analysis set | C# positive and discriminating negative/control cases |
| `loader` | Affects acceptance or construction but not analysis semantics | Focused loader success/failure case |
| `metadata` | Preserved or exposed but intentionally ignored by semantic equality | Round-trip/API test; no analysis credit |
| `ignored` | DTD-valid input is currently ignored by the C# loader | Explicit limitation case; never “supported” |
| `ignored-reference` | A reference is valid but its active runtime object is unavailable and the loader omits it | Loader diagnostic plus constructed-model assertion |
| `partial-load` | An error handler records a construction failure and loading continues with a reduced grammar | Typed diagnostic, constructed-model assertion, and no semantic credit for the omitted object |
| `invalid` | Rejected by DTD, loader, or model validation | Typed rejection case |
| `nonconvergent` | Valid grammar and word do not converge | Controlled `nonconvergent` outcome |

An unclassified surface is a build failure. A C# crash is never a catalog disposition; it is a red
Machine defect.

## 3. Canonical identities

### 3.1 Feature IDs

A feature ID names one XML-level semantic partition:

```text
sil.machine.hc-feature/v1/morph/affix/output/insert-segments/prefix
sil.machine.hc-feature/v1/morph/affix/output/copy/reduplication/partial-prefix
sil.machine.hc-feature/v1/phon/rewrite/iterative/rtl/epenthesis
sil.machine.hc-feature/v1/gate/mpr/group/output/overwrite
sil.machine.hc-feature/v1/container/template/slot/required
```

IDs describe semantics, not fixture filenames, authored XML IDs, C# class names, or PanGloss backend
mechanisms. Renaming a fixture or rule does not change a feature ID. Changing the semantic partition
requires a new profile version or a new leaf.

### 3.2 Combination IDs

A combination is a canonical structured value containing:

- typed feature occurrences;
- occurrence-local profiles;
- a schedule graph;
- a connected semantic-interaction graph; and
- the carrier that establishes each schedule edge.

Example:

```yaml
profile: sil.machine.hc-combination/v1
occurrences:
  - feature: sil.machine.hc-feature/v1/morph/affix/output/insert-segments/suffix
  - feature: sil.machine.hc-feature/v1/morph/compound/head-before-nonhead
  - feature: sil.machine.hc-feature/v1/phon/rewrite/iterative/ltr/feature-change
schedule:
  - {left: 0, relation: unordered-union, right: 1, carrier: morph-stratum}
  - {left: 0, relation: precedes, right: 2, carrier: stratum-phase}
  - {left: 1, relation: precedes, right: 2, carrier: stratum-phase}
interactions:
  - {left: 0, relation: feeds, right: 1, channel: morpheme-sequence}
  - {left: 1, relation: feeds, right: 2, channel: segment-sequence}
```

Canonicalization uses RFC 8785 JSON and SHA-256 over its UTF-8 bytes. Occurrences initially receive
fixture-local indices only so the schedule and interaction edges can refer to them. Machine enumerates
every label-preserving permutation of the occurrence nodes, jointly remaps both edge sets, sorts edges
by `(left, relation, right, carrier-or-channel)`, serializes each complete object, and chooses the
lexicographically least byte sequence. Repeated equal-profile occurrences remain separate nodes, but
their authored IDs and original indices do not survive canonicalization. Schedule DAGs are stored as
their unique transitive reduction; cyclic or non-DAG schedule policies use an explicitly named policy.

The authoritative ID is `sha256:<64-lowercase-hex-digits>`. A readable slug is diagnostic only; a
slug collision appends the first twelve digest digits. The implementation must ship vectors for
unique labels, `AAB`, `AAA`, disconnected candidate graphs, mixed partial orders, symmetric edges,
and jointly permuted schedule/interaction edges. Fixture IDs remain evidence references outside the
hashed semantic object.

## 4. Complete grammar-feature inventory

This section enumerates the semantic partitions that the v1 machine-readable catalog must contain.
The listed stems omit the common `sil.machine.hc-feature/v1/` prefix.

Every row expands to a phase-effect record. Unless a row says otherwise, “requires”, “excludes”,
“blocks”, or “outputs” on a morphological or phonological subrule is not presumed symmetric. Current
C# analysis commonly unapplies a structural transform permissively; synthesis then enforces
POS/feature/MPR/environment gates, applies output state, and performs blocking. The final parse effect
is the intersection of generated candidates with successful synthesis confirmation and final
validity. Fixtures must distinguish these phases in traces or counterfactual results; a final result
alone may prove the oracle result but cannot certify a claimed internal phase effect.

The machine-readable record minimally contains:

```yaml
analysisCandidateEffect: {reads: [], writes: [], behavior: none|propose|filter|transform}
synthesisConfirmationEffect: {reads: [], writes: [], behavior: none|confirm|reject|transform}
finalParseEffect: {behavior: preserve|remove|add-analysis|typed-outcome}
```

### 4.1 Document selection and activation

| ID stem | XML shape and semantic partitions |
|---|---|
| `document/language/active-single` | Exactly one active `Language`; the loader calls `Single(IsActive)` |
| `document/language/active-none` | No active language; loader rejection |
| `document/language/active-multiple` | More than one active language; loader rejection |
| `document/phon-feature-system/active-single` | Exactly one active phonological feature system |
| `document/phon-feature-system/active-none` | Accepted featureless mode: loader freezes an empty system and segment definitions ignore authored phonological FeatureValues |
| `document/phon-feature-system/active-multiple` | More than one active system reaches `SingleOrDefault`; typed loader rejection |
| `document/activation/active` | An `isActive`-bearing definition participates |
| `document/activation/inactive` | Inactive feature systems, features, tables, definitions, classes, families, rules, subrules, templates, slots, entries, allomorphs, and co-occurrence rules are excluded |
| `document/reference/dtd-unresolved` | A true unresolved IDREF under validating .NET XML loading is an `invalid` document |
| `document/reference/inactive-or-failed-object` | An IDREF resolves in XML, but the target was inactive or failed construction; stratum rule lists and template slots may silently omit it as `ignored-reference` |
| `document/load/error-handler-continues` | A subrule/allomorph construction error is reported and loading continues; `partial-load`, never semantic credit |
| `document/load/platform-validation` | .NET validating-load and Mono/non-validating behavior are distinct harness profiles; the oracle profile requires validating .NET behavior |
| `document/dtd/defaulted-attribute` | DTD default is omitted and the loader observes the intended default |
| `document/dtd/explicit-default` | The same default is written explicitly; semantic result equals the omitted form |

Activation must be tested at each loader collection boundary. One inactive rule does not certify an
inactive feature, character definition, subrule, slot, or allomorph.

### 4.2 Feature systems and feature structures

| ID stem | Partitions |
|---|---|
| `feature/pos` | One/multiple parts of speech; required, assigned, and output POS; absent POS |
| `feature/phon/symbolic` | Symbolic phonological feature with one/multiple symbols |
| `feature/phon/default-symbol/present` | `defaultSymbol` present and used in matching |
| `feature/phon/default-symbol/absent` | No default symbol |
| `feature/syntax/head/symbolic` | Symbolic head feature |
| `feature/syntax/head/complex` | Nested complex head feature |
| `feature/syntax/foot/symbolic` | Symbolic foot feature |
| `feature/syntax/foot/complex` | Nested complex foot feature |
| `feature/value/single` | One symbolic value |
| `feature/value/disjunctive` | Multiple `symbolValues` in one feature value |
| `feature/value/complex` | Recursive `FeatureValue` for a complex feature |
| `feature/value/empty-complex` | Complex feature with no child values |
| `feature/value/symbolic-without-symbol-values` | Symbolic feature used without `symbolValues` reaches the invalid complex-feature cast branch |
| `feature/value/complex-with-symbol-values` | Complex feature used with `symbolValues` reaches the invalid symbolic-feature cast branch |
| `feature/value/symbolic-values-with-children` | Symbolic feature has `symbolValues` and recursive children; loader uses the symbols and ignores the children |
| `feature/value/complex-values-with-children` | Complex feature has both `symbolValues` and children; invalid symbolic-feature cast occurs before children can load |
| `feature/value/inactive` | Inactive `FeatureValue` is omitted |
| `feature/agreement/required-head` | Rule or allomorph requires head features |
| `feature/agreement/required-foot` | Rule or allomorph requires foot features |
| `feature/agreement/assigned-head` | Lexical entry/stem-name region supplies head features |
| `feature/agreement/assigned-foot` | Lexical entry/stem-name region supplies foot features |
| `feature/agreement/output-head` | Morphological rule writes head features |
| `feature/agreement/output-foot` | Morphological rule writes foot features |
| `feature/agreement/obligatory` | Output feature completeness requirement, both satisfied and unsatisfied |
| `feature/realizational/required` | Realizational feature structure selects a realizational rule |

Required/assigned/output are distinct leaves even when backed by the same `FeatureStruct` operation.

### 4.3 MPR state

| ID stem | Partitions |
|---|---|
| `mpr/feature` | Individual MPR feature present/absent |
| `mpr/group/match/any` | Any group member satisfies a requirement |
| `mpr/group/match/all` | All group members are required |
| `mpr/group/output/append` | Output accumulates group members |
| `mpr/group/output/overwrite` | Output replaces prior members of the group |
| `mpr/gate/required` | Required MPR on an ordinary morphological input, compound head input, or phonological subrule |
| `mpr/gate/excluded` | Excluded MPR at those same supported sites |
| `mpr/gate/required-and-excluded` | Same or overlapping set exercises conflict semantics |
| `mpr/output/morphological` | Morphological output adds MPR state |
| `mpr/compound/head-production` | Head production restriction |
| `mpr/compound/nonhead-production` | Non-head production restriction |
| `mpr/compound/output-production` | Output production restriction |

The DTD and loader do not provide symmetric non-head-input required/excluded MPR attributes for a
`CompoundingSubrule`; that absence is a model limitation, not a missing test leaf.

### 4.4 Character definitions and representations

| ID stem | Partitions |
|---|---|
| `representation/table/single` | One character-definition table |
| `representation/table/multiple-disjoint` | Multiple tables with disjoint representations |
| `representation/table/multiple-overlap` | Multiple tables share a surface representation with different identities/features |
| `representation/table/stratum-transition` | Word crosses strata that use different tables |
| `representation/table/explicit-pattern-override` | `Segments@characterDefinitionTable` overrides the carrier table |
| `representation/table/explicit-output-override` | `InsertSegments@characterDefinitionTable` overrides the carrier table |
| `representation/segment/featureless` | Segment definition without phonological features |
| `representation/segment/feature-bearing` | Fully or partially specified segment feature structure |
| `representation/segment/one-spelling` | One representation |
| `representation/segment/multiple-spellings` | Multiple representations for one segment identity |
| `representation/segment/prefix-overlap` | Distinct spellings overlap by prefix; segmentation deterministically chooses the longest match |
| `representation/segment/normalized-duplicate-across-definitions` | A normalized spelling already owned by an earlier definition; table construction rejects the later definition |
| `representation/segment/normalized-duplicate-within-definition` | Two spellings in one new definition normalize identically; construction accepts them and the lookup collapses to one key |
| `representation/segment/cross-table-overlap` | Separate tables may assign the same spelling to different identities; the active carrier table disambiguates |
| `representation/output/multiple-spellings` | One segment identity has multiple output spellings; enumerate the C# surface alternatives separately from input segmentation |
| `representation/segment/diacritic` | Combining or precomposed representation identity |
| `representation/boundary/defined` | Boundary definition and representation |
| `representation/boundary/in-pattern` | Boundary marker participates in a pattern |
| `representation/boundary/inserted` | Inserted segment string contains a boundary |
| `representation/root/boundary-only` | Root parses to boundaries only; loader rejects it |
| `representation/invalid-word` | Input cannot be represented by the selected table; `invalidInput` |
| `representation/root-pattern/literal` | Root `PhoneticShape` longest-match segmentation with pattern syntax enabled |
| `representation/root-pattern/class` | Root `[Class]` expands one natural-class member |
| `representation/root-pattern/optional-class` | Root `([Class])` permits one optional class; compound parenthesized patterns are invalid |
| `representation/root-pattern/repeated-class` | Root `[Class]*` expands zero or more members; `+` is a boundary marker, not a Kleene operator |
| `representation/root-pattern/malformed` | Unclosed/unknown class and malformed metacharacter syntax produce typed loader failure |
| `representation/normalization/nfd` | Root and word representations normalize to NFD before segmentation |
| `representation/normalization/collision` | Canonical-equivalence collision is partitioned by within-definition acceptance versus across-definition rejection |

### 4.5 Natural classes, patterns, and environments

| ID stem | Partitions |
|---|---|
| `pattern/atom/segment` | Literal `Segment` identity |
| `pattern/atom/segments` | Literal `Segments` string expanded under a table |
| `pattern/atom/natural-class/segment-list` | `SegmentNaturalClass` |
| `pattern/atom/natural-class/feature` | `FeatureNaturalClass` |
| `pattern/atom/natural-class/empty` | Empty class, if loader/model accepts it, with no-match evidence |
| `pattern/atom/boundary` | `BoundaryMarker` |
| `pattern/sequence/empty` | Empty phonetic sequence |
| `pattern/sequence/multiple-atoms` | Ordered multi-atom sequence |
| `pattern/quantifier/default-star` | Missing `min`/`max`: `0..unbounded` |
| `pattern/quantifier/optional` | `0..1` |
| `pattern/quantifier/bounded` | Other finite `min..max`, including exact repetition |
| `pattern/quantifier/unbounded-positive` | Positive minimum and unbounded maximum |
| `pattern/quantifier/invalid-bounds` | Negative/ill-ordered/non-numeric bounds; typed loader/model rejection |
| `pattern/capture/named` | Named input part used by output action or metathesis switch |
| `pattern/capture/uncaptured` | Pattern material intentionally omitted from output |
| `environment/left` | Left environment only |
| `environment/right` | Right environment only |
| `environment/bilateral` | Both environments |
| `environment/empty` | Explicit environment with no effective side |
| `environment/anchor/initial` | Initial boundary condition |
| `environment/anchor/final` | Final boundary condition |
| `environment/anchor/both` | Both anchors |
| `environment/alpha/plus` | Positive alpha-variable agreement |
| `environment/alpha/minus` | Negative alpha-variable disagreement |
| `environment/alpha/multiple` | Multiple variables/features in one rule |
| `environment/alpha/reused` | One variable reused across target/output/environment |

Pattern leaves must be exercised in each semantically distinct carrier that supports them: root
allomorph shape, morphological LHS, rewrite LHS/RHS/environment, metathesis structural description,
and allomorph environment. A quantifier in one carrier does not certify another carrier's execution.
Alpha variables in an allomorph environment are currently a `blocked-csharp-defect`: the loader
passes an empty variable map and indexes it when the alpha context is loaded. They cannot earn
semantic carrier credit until C# is fixed.

### 4.6 Lexicon, allomorphy, and blocking

| ID stem | Partitions |
|---|---|
| `lexicon/entry/single-allomorph` | One root allomorph |
| `lexicon/entry/multiple-allomorph/free` | Constraint-equivalent alternatives free-fluctuate |
| `lexicon/entry/multiple-allomorph/prioritized` | Constraint-distinct alternatives trigger disjunctive re-check/order |
| `lexicon/entry/partial` | Partial lexical entry participates in continuation/template semantics |
| `lexicon/allomorph/bound` | Bound root cannot stand alone |
| `lexicon/allomorph/required-environment` | Required left/right/bilateral environment |
| `lexicon/allomorph/excluded-environment` | Excluded left/right/bilateral environment |
| `lexicon/allomorph/required-and-excluded` | Interacting positive and negative environments |
| `lexicon/allomorph/stem-name` | Stem-name region selects the root allomorph |
| `lexicon/stem-name/one-region` | One POS/head/foot region |
| `lexicon/stem-name/multiple-regions` | Disjunctive regions |
| `lexicon/family/blocking` | Irregular family member blocks a regular derivation |
| `lexicon/property/metadata` | Arbitrary properties are loaded but do not by themselves earn semantic coverage |
| `lexicon/morpheme-id-gloss/metadata` | Identity/presentation fields, separate from rule behavior |

### 4.7 Ordinary and realizational affix-process rules

| ID stem | Partitions |
|---|---|
| `morph/affix/rule` | Ordinary `MorphologicalRule` |
| `morph/realizational/rule` | `RealizationalRule` with empty/non-empty realizational features |
| `morph/affix/subrule/single` | One allomorph/subrule |
| `morph/affix/subrule/multiple` | Competing or gated allomorphs; declaration priority/free fluctuation |
| `morph/affix/input/one-part` | One captured input part |
| `morph/affix/input/multiple-parts` | Discontinuous/circumfix-capable input |
| `morph/affix/input/unnamed-referenced-part` | `PhoneticSequence@id` omitted but Copy/Modify references its index; typed loader failure |
| `morph/affix/output/copy/once` | `CopyFromInput` once |
| `morph/affix/output/copy/reordered` | Captured parts copied in a different order: movement/permutation |
| `morph/affix/output/copy/repeated` | Same part copied more than once: reduplication |
| `morph/affix/output/omit/leading` | Leading captured material omitted: subtraction/truncation |
| `morph/affix/output/omit/trailing` | Trailing captured material omitted |
| `morph/affix/output/omit/internal` | Internal captured part omitted |
| `morph/affix/output/empty` | No RHS actions: complete deletion of matched material where valid |
| `morph/affix/output/modify` | `ModifyFromInput` feature mutation |
| `morph/affix/output/insert-class` | `InsertSimpleContext` creates a natural-class/feature-defined segment |
| `morph/affix/output/insert-segments` | `InsertSegments` creates literal material |
| `morph/affix/output/mixed` | Multiple action kinds in one ordered RHS |
| `morph/affix/shape/prefix` | Derived action topology inserts/copies before the stem |
| `morph/affix/shape/suffix` | Derived action topology inserts/copies after the stem |
| `morph/affix/shape/infix` | Material inserted between copied portions |
| `morph/affix/shape/circumfix` | Material inserted on both sides |
| `morph/affix/shape/simulfix` | Modification is the sole or principal exponent |
| `morph/affix/redup-hint/prefix` | `redupMorphType=prefix` |
| `morph/affix/redup-hint/suffix` | `redupMorphType=suffix` |
| `morph/affix/redup-hint/implicit` | Default implicit hint, with true-copy and non-copy controls |
| `morph/affix/max-application/zero` | Rule cannot apply |
| `morph/affix/max-application/one` | Default single application |
| `morph/affix/max-application/multiple` | Bounded repetition, including upper DTD boundary 9 |
| `morph/affix/blockable/true` | Synthesis confirmation checks family blocking; analysis candidate generation does not |
| `morph/affix/blockable/false` | Synthesis confirmation bypasses blocking |
| `morph/affix/partial/true` | Partial-rule continuation behavior |
| `morph/affix/partial/false` | Ordinary complete rule |
| `morph/affix/gate/rule` | Required POS/head/foot/stem-name on the rule |
| `morph/affix/gate/subrule` | Required head/foot, MPR, and environments on the allomorph |
| `morph/affix/output/syntax` | Output POS/head/foot/obligatory features |
| `morph/realizational/blocking` | Synthesis-confirmation blocking and competition with ordinary rules; analysis only proposes candidates |

Output topology is derived from the ordered LHS captures and RHS actions. It is not safe to infer
prefix/suffix/circumfix solely from `redupMorphType` or from the first inserted string.

### 4.8 Compounding

| ID stem | Partitions |
|---|---|
| `morph/compound/rule` | `CompoundingRule` |
| `morph/compound/subrule/single` | One compound subrule |
| `morph/compound/subrule/multiple` | Alternative compound shapes |
| `morph/compound/head-before-nonhead` | RHS places head material before non-head material |
| `morph/compound/nonhead-before-head` | RHS reverses that order |
| `morph/compound/interleaved-modified` | Mixed insertion/modification/reordering across head and non-head captures |
| `morph/compound/gate/head-pos` | Head POS restriction |
| `morph/compound/gate/nonhead-pos` | Non-head POS restriction |
| `morph/compound/gate/head-features` | Head head/foot feature restriction |
| `morph/compound/gate/nonhead-features` | Non-head head/foot feature restriction |
| `morph/compound/gate/head-mpr` | Head input required/excluded MPR |
| `morph/compound/production-mpr/nonhead-analysis-gate` | Analysis candidate generation checks non-head production restrictions |
| `morph/compound/production-mpr/head-synthesis-gate` | Synthesis confirmation checks head production restrictions |
| `morph/compound/production-mpr/output-synthesis` | Synthesis writes output production restrictions |
| `morph/compound/variable-binding/head-nonhead-not-unified` | Current C# synthesis does not unify head and non-head alpha-variable bindings; explicit implementation limitation |
| `morph/compound/output/syntax` | Output POS/head/foot/obligatory features |
| `morph/compound/max-application/zero` | Cannot apply |
| `morph/compound/max-application/one` | Default one application |
| `morph/compound/max-application/multiple` | Bounded recursive compounding, including upper boundary 9 |
| `morph/compound/blockable/true` | Synthesis confirmation checks family blocking; analysis candidate generation does not |
| `morph/compound/blockable/false` | Synthesis confirmation bypasses blocking |

### 4.9 Templates and morphology scheduling

| ID stem | Partitions |
|---|---|
| `container/morph-stratum/linear-synthesis` | Synthesis uses the authored total `LinearRuleCascade` |
| `container/morph-stratum/linear-analysis` | Analysis uses optional unapplication through a `PermutationRuleCascade`; the topology is ordered subsequences, not one reversed total order |
| `container/morph-stratum/unordered-union` | HC explores admissible loose-rule combinations/orders and unions results |
| `container/template/single` | One applicable template |
| `container/template/multiple-alternatives` | Multiple applicable templates are alternatives |
| `container/template/gate-pos` | Template required POS |
| `container/template/final/true` | Final template terminates loose/template continuation |
| `container/template/final/false` | Non-final template requires valid continuation |
| `container/template/slot/required` | Required slot |
| `container/template/slot/optional` | Optional slot |
| `container/template/slot/alternatives` | Multiple rules in one slot are alternatives, not a cascade |
| `container/template/slot/sequence` | Earlier and later slots establish order |
| `container/template/loose-rule/linear` | Template/loose-rule interaction in a linear stratum |
| `container/template/loose-rule/unordered` | Template and loose rules can interleave in an unordered stratum |
| `container/template/loose-rule/synthesis-two-root-paths` | Synthesis unions morphology-first and template-first roots; each branch may recursively enter the other |
| `container/template/loose-rule/analysis-two-root-paths` | Analysis unions template-first and morphology-first roots, reversing root enumeration while recursively crossing branches |
| `container/template/loose-rule/single-family-output` | Template-only and loose-morphology-only successful outputs remain admitted beside cross-branch outputs |
| `container/strata/sequence` | Synthesis proceeds by increasing stratum depth; analysis reverses it |
| `container/strata/same-table` | Ordered strata share representation |
| `container/strata/table-transition` | Ordered strata change representation table |

### 4.10 Rewrite rules

| ID stem | Partitions |
|---|---|
| `phon/rewrite/rule` | `PhonologicalRule`/`RewriteRule` |
| `phon/rewrite/subrule/single` | One subrule |
| `phon/rewrite/subrule/disjunctive-synthesis` | Synthesis selects the first applicable subrule in declaration order |
| `phon/rewrite/subrule/disjunctive-analysis` | Analysis visits every subrule against mutable candidate state; not first-match priority |
| `phon/rewrite/application/iterative-ltr` | Default left-to-right iterative application |
| `phon/rewrite/application/iterative-rtl` | Right-to-left iterative application |
| `phon/rewrite/application/simultaneous` | Simultaneous application; XML supplies no RTL-simultaneous variant |
| `phon/rewrite/application/analysis-forced-simultaneous` | Analysis forces deletion and expansion inverses to simultaneous regardless of the authored XML mode |
| `phon/rewrite/application/simultaneous-grouped-rhs-defect` | A simultaneous equal-child-count inverse with `Segments`/optional grouped RHS can cast Group/Quantifier to Constraint and crash; `blocked-csharp-defect` |
| `phon/rewrite/shape/cardinality-syntax-nodes` | Current analysis dispatch compares top-level pattern-child counts, not effective segment cardinality; grouped/quantified cross-products require explicit evidence |
| `phon/rewrite/shape/feature-change` | LHS and RHS cardinalities equal; feature/substitution reversal path |
| `phon/rewrite/shape/deletion` | LHS cardinality greater than RHS |
| `phon/rewrite/shape/epenthesis` | Empty LHS and non-empty RHS |
| `phon/rewrite/shape/expansion` | Non-empty LHS shorter than RHS |
| `phon/rewrite/shape/merge` | Multi-node LHS maps to fewer RHS nodes |
| `phon/rewrite/shape/empty-output` | Complete deletion |
| `phon/rewrite/shape/multi-output` | Multi-node output |
| `phon/rewrite/gate/pos` | Synthesis confirmation enforces required POS; analysis subrules propose without this gate |
| `phon/rewrite/gate/mpr-required` | Synthesis confirmation enforces required MPR; analysis proposes without it |
| `phon/rewrite/gate/mpr-excluded` | Synthesis confirmation enforces excluded MPR; analysis proposes without it |
| `phon/rewrite/boundary/analysis-filtered` | Analysis target matching excludes boundary nodes and strips boundaries from environments |
| `phon/rewrite/boundary/synthesis-retained` | Synthesis target matching and environments retain boundary nodes |
| `phon/rewrite/environment` | Left/right/bilateral/anchored/quantified/alpha-variable profiles from §4.5 |
| `phon/rewrite/analysis/reapply-normal` | Equal-width inverse reapplication |
| `phon/rewrite/analysis/reapply-deletion` | Deletion/expansion inverse uses bounded `DeletionReapplications` behavior |
| `phon/rewrite/analysis/reapply-self-opaquing` | Simultaneous inverse reapplication until self-opaque |
| `phon/rewrite/self-feeding/convergent` | Reapplication reaches a fixed point |
| `phon/rewrite/self-feeding/nonconvergent` | Controlled nonconvergent outcome |

### 4.11 Metathesis

| ID stem | Partitions |
|---|---|
| `phon/metathesis/rule` | `MetathesisRule` |
| `phon/metathesis/application/ltr` | Left-to-right iterative scan |
| `phon/metathesis/application/rtl` | Right-to-left iterative scan |
| `phon/metathesis/shape/adjacent` | Switch captures adjacent |
| `phon/metathesis/shape/nonadjacent` | Intervening material is retained |
| `phon/metathesis/shape/contextual` | Additional left/right material constrains the switch |
| `phon/metathesis/shape/anchored` | Initial/final/both boundary condition |
| `phon/metathesis/shape/quantified` | Optional/repeated intervening material |
| `phon/metathesis/shape/literal-switch` | Switches identify literal segments |
| `phon/metathesis/shape/class-switch` | Switches identify natural-class contexts |
| `phon/metathesis/switch/same-id` | Equal `leftSwitch` and `rightSwitch` IDREFs collide in the loader dictionary; typed invalid/blocked outcome |
| `phon/metathesis/switch/missing-capture` | A switch ID resolves in XML but is absent from the structural description capture set; typed loader/model outcome |
| `phon/metathesis/boundary/analysis-filtered` | Analysis structural matching excludes boundary nodes |
| `phon/metathesis/boundary/synthesis-retained` | Synthesis structural matching retains boundary nodes |
| `phon/metathesis/effect/analysis-feature-swap` | Analysis swaps feature structures in place, preserving node identity |
| `phon/metathesis/effect/synthesis-node-move` | Synthesis physically moves nodes and rebuilds morph annotations |
| `phon/metathesis/self-feeding/convergent` | Iteration stabilizes |
| `phon/metathesis/self-feeding/nonconvergent` | Controlled nonconvergent outcome if constructible |

### 4.12 Co-occurrence and selection constraints

Each of the following applies independently to morpheme and allomorph co-occurrence rules:

| ID stem | Partitions |
|---|---|
| `constraint/cooccurrence/require` | Primary requires one of the named others |
| `constraint/cooccurrence/exclude` | Primary excludes the named others |
| `constraint/cooccurrence/anywhere` | No direction/distance restriction |
| `constraint/cooccurrence/somewhere-left` | Other occurs somewhere to the left |
| `constraint/cooccurrence/somewhere-right` | Other occurs somewhere to the right |
| `constraint/cooccurrence/adjacent-left` | Other immediately precedes primary |
| `constraint/cooccurrence/adjacent-right` | Other immediately follows primary |
| `constraint/cooccurrence/one-other` | One target |
| `constraint/cooccurrence/multiple-others` | Disjunctive target set |
| `constraint/cooccurrence/chain` | Multiple constraints form a dependency chain |
| `constraint/cooccurrence/cycle` | Constraints form a cycle or contradiction |

### 4.13 Runtime dimensions adjacent to, but not defined by, the grammar

These require conformance coverage but do not enter the XML feature-combination denominator:

- lexical guessing on/off;
- tracing on/off and trace fidelity;
- merge-equivalent-analyses on/off;
- rule and lexical-entry selectors;
- C# analysis reapplication limits and maximum unapplications/stem count;
- engine-managed deterministic node/arc/traversal/application budgets; and
- batch, adapter, and structured-result behavior.

They receive separate runtime-integration IDs so a runtime option cannot masquerade as coverage of an
XML feature.

## 5. Interaction taxonomy

Every semantic feature occurrence declares its three phase effects and which state domains each effect
reads, writes, adds, removes, selects, or bounds. Domains include segment sequence, segment features,
boundaries, morpheme sequence, allomorph choice, POS/category, head/foot/obligatory syntactic features,
realizational-feature state, MPR state, stem name, character table, template partial/final state,
deleted/modified node flags, current compound non-head, stratum phase, rule application/unapplication
counters, and other application history.

The catalog uses these interaction relations:

| Relation | Definition | Typical witness |
|---|---|---|
| `feeds` | A writes something B positively reads | Affix creates the segment targeted by rewrite |
| `bleeds` | A removes or changes something B positively reads | Rewrite destroys a later rule's environment |
| `counterfeeds` | Authored order prevents B from seeing material A would otherwise create | Reversing two ordered rules adds an analysis |
| `counterbleeds` | Authored order preserves a match that the reverse order would remove | Reversing two ordered rules removes an analysis |
| `competes` | A and B write overlapping state | Two rewrites target the same segment |
| `gates` | A's state controls whether B is eligible | MPR-producing rule enables a gated rule |
| `blocks` | A supplies an exception that suppresses B | Family/allomorph blocks regular morphology |
| `selects` | A changes the allomorph, subrule, template, or lexical alternative chosen | Output features select realizational allomorph |
| `accumulates` | A and B independently add state retained in the result | MPR append or independent feature outputs |
| `overwrites` | B replaces earlier state from A | MPR group overwrite |
| `copies-from` | B duplicates material/state produced or selected by A | Reduplication after stem modification |
| `bounds` | A limits B's repetition/search | Max-application and obligatory/final constraints |
| `recurs` | An occurrence reads its own output | Iterative rewrite, repeated morphology, recursive compound |
| `alternates` | Several rules/allomorphs/slots are alternatives rather than a cascade | Multiple template rules in one slot |
| `unions-orders` | HC admits several derivation orders and unions their results | Unordered morphological stratum |
| `crosses-phase` | State moves through morphology/phonology, template, or stratum boundary | Morphological output feeds later phonology |
| `threads-representation` | Identity crosses a character-table/boundary representation seam | Multi-table strata |
| `confirms` | A later validity check removes a candidate produced earlier | Co-occurrence or allomorph validity check |
| `orthogonal` | Proven non-interacting for the declared domains and placement | Disjoint rules; reported but not interaction credit |

An interaction word is load-bearing only if removing or neutralizing each occurrence changes the C#
analysis set or typed outcome. Merely firing two rules is insufficient. For a connected triple, the
declared interaction graph must connect all three occurrences, and every declared edge needs a
counterfactual witness.

## 6. Ordering and unordered semantics

“Ordered” is not one flag.

### 6.1 Rule-local policy

- rewrite application: iterative or simultaneous;
- rewrite/metathesis scan: left-to-right or right-to-left;
- morphological/compound application: zero, one, or bounded multiple application; and
- self-application: convergent or nonconvergent.

These are node profiles, not edges between rules.

### 6.2 Container schedule

- loose morphological rules in synthesis: authored linear cascade or unordered combination/union;
- loose morphological rules in analysis: optional unapplication, including ordered subsequences for
  a linear stratum and admissible permutations for an unordered stratum;
- phonological rules: authored linear cascade in synthesis, reversed in analysis;
- template slots: linear sequence;
- rules within a template slot: alternatives;
- multiple applicable templates: alternatives;
- templates versus loose rules: carrier-dependent interleaving; and
- strata: ordered phase sequence, reversed by analysis.

### 6.3 Pairwise schedule relations

Every pair in a combination receives exactly one applicable relation:

- `precedes` with a named carrier;
- `unordered-union` with a named carrier;
- `alternative` with a named carrier;
- `same-rule-local` for two profiles of one rule occurrence; or
- `not-co-schedulable`, which retires the candidate with a reason.

Absence of an edge is not shorthand for unordered.

### 6.4 Evidence rules

For an ordered pair, reversing the relevant order must change the C# analysis set to earn
order-sensitive coverage. If it does not, the case earns `orthogonal` or order-invariant coverage,
not ordering coverage.

For an ordered triple, every admitted sequence of category labels is a distinct type-level
obligation. A connected chain `A > B > C` needs load-bearing evidence for the declared edges, not
only a comparison of the complete order with its full reversal.

For an unordered pair or triple, “unordered” means the union of admissible derivations, not that each
derivation has the same output. Evidence must establish both:

1. the final C# result contains the required derivation-order contributions; and
2. every concrete XML declaration permutation produces the same final union.

At arity three Machine runs all six permutations of the concrete rule occurrences, including `AAA`
and `AAB`, where same-category occurrences remain distinct through fixture-local rule IDs.

Mixed partial orders are first-class. For example, two unordered morphological rules may both precede
a phonological rewrite. A tuple-level `ordered: true|false` cannot represent that grammar.

## 7. Obligation enumeration

The machine-readable catalog generates four related ledgers.

### 7.1 Atomic ledger

Every feature leaf in §4 receives at least:

- a positive or accepted witness where meaningful;
- a negative, excluded, absent, or contrastive control;
- a C# outcome and structured analysis set when semantic; and
- a fixture and word/case reference.

### 7.2 Within-rule configuration ledger

For each rule kind, enumerate every compatible pair of local feature partitions: output action,
pattern form, gate, environment, state output, repetition, and rule-local application mode. Connected
triples are required when three settings jointly choose a distinct C# branch or have a known
three-way dependency. Mutually exclusive values of one enum are separate atomic variants, not a
nonsensical same-rule combination.

### 7.3 Cross-rule interaction ledger

Generate all compatible pairs and connected triples, with repetition, over the admitted high-level
rule profiles:

- ordinary affix-process profiles;
- realizational affix-process profiles;
- compounding profiles;
- rewrite profiles;
- metathesis profiles; and
- independently schedulable constraint/selection participants where the C# model exposes them as
  derivational participants.

Do not collapse an affixation, truncation, reduplication, and mutation profile merely because all are
instances of `AffixProcessRule`. Each profile participates in the tuple denominator. Conversely,
templates, strata, environments, and feature structures enter as carriers/channels/profiles rather
than pretending to be schedulable rules.

The generator first emits candidates from schema placement and read/write compatibility. Each
candidate must then be one of:

- `required`: a Machine grammar and discriminating word must exist;
- `retired-invalid`: XML/model placement cannot express it;
- `retired-orthogonal`: a cited structural argument proves no interaction;
- `retired-duplicate`: canonicalization proves it is the same obligation; or
- `blocked-csharp-defect`: C# should support it but currently crashes or behaves incorrectly; red.

No candidate silently disappears.

### 7.4 Schedule ledger

For every required combination, enumerate every scheduling topology admitted by its legal carriers.
Total orders, unordered unions, alternatives, and mixed partial orders are distinct obligations. A
carrier limitation is an explicit retirement, not an absent row.

## 8. Evidence and ownership

Machine owns:

- the feature catalog and schema;
- canonicalization and combination generation;
- required and retired combination rows;
- the conformance grammars containing the interacting rules;
- `words.yaml` cases naming the obligations they witness;
- C# expected structured analyses and typed outcomes; and
- completeness reports for atomic, interaction, ordering, and C# evidence.

PanGloss imports the catalog and evidence, maps each obligation to its backends, and runs every backend
that claims to represent it. PanGloss may add local diagnostic/unit tests, but those do not satisfy a
Machine semantic obligation.

A fixture earns semantic credit only from `complete` C# results. `invalidInput` and `nonconvergent`
earn their respective safety/validation obligations but no positive semantic-analysis credit. Missing
C#, uncontrolled crash, malformed result, budget exhaustion, or harness failure is red.

The adapter outcome is one of `complete`, `invalidInput`, `invalidGrammar`, `partialLoad`,
`nonconvergent`, or `harnessFailure`. Diagnostics are structured and may accompany
`invalidGrammar` or `partialLoad`. Today C# `AnalyzeWord` can collapse an
`InvalidShapeException` to an empty result, epenthesis can throw `InfiniteLoopException`, and a
self-opaquing inverse can run without a logical bound. Therefore these typed outcomes are a required
harness/engine change, not a description of already-controlled behavior. The runner must prevalidate
or intercept those paths, translate only known failures, and report every unknown exception, process
exit, or logical-budget exhaustion as red. Wall-clock limits remain engine management and are not
part of the semantic contract.

## 9. DTD features currently accepted but not implemented by the C# loader

Source inspection at Machine commit `73599a8` found these explicit gaps:

| XML surface | Current C# behavior | Catalog treatment |
|---|---|---|
| `Stratum@cyclicity` | Loader never reads it | `ignored`; cannot claim cyclic-stratum support |
| `Stratum@phonologicalRuleOrder` (`linear|simultaneous`) | Loader never reads it; phonological rules always form a linear cascade | `ignored`; simultaneous cascade is not representable by current C# semantics |
| `PreviousWord` / `NextWord` / `Null` on a phonological subrule | Loader does not load them | `ignored`; cross-word phonological context unsupported through this loader |
| `SyntacticRules` / `SyntacticRule` | Loader does not load them | `ignored` |
| `AffixTemplate@requiredSubcategorizedRules` | Loader does not read it | `ignored` |
| `MorphologicalRule@requiredSubcategorizedRules` | Loader does not read it | `ignored` |
| `MorphologicalRule@outputSubcategorization` and `OutputSubcategorizationOverrides` | Loader does not read them | `ignored` |
| `LexicalEntry@morphologicalRules`, `subcategorizations`, `obligatoryHeadFeatures`, `obligatoryFootFeatures` | Loader does not read them | `ignored` |
| `CompoundingRule@headSubcategorizedRules`, `nonHeadSubcategorizedRules`, `outputSubcategorization` | Loader does not read them | `ignored` |
| simultaneous right-to-left rewrite | DTD has only `simultaneous`; loader maps its direction to left-to-right | `not-expressible` as a distinct variant |
| metathesis alpha variables | Metathesis loader supplies no variable definitions | `not-expressible` unless C# is extended |
| allomorph-environment alpha variables | Loader supplies an empty variable map, then indexes it for an alpha context | `blocked-csharp-defect`; valid XML must produce a controlled red outcome until fixed |
| compound non-head input MPR gate | DTD/model expose head-input gate only | `not-expressible` unless the format/model is extended |
| compound head/non-head alpha bindings | Synthesis contains an explicit TODO and does not unify the two binding sets | `blocked-csharp-defect` when an interaction requires shared binding |
| inactive or failed-loaded IDREF target | Some stratum rule lists and template slots omit the missing runtime object | `ignored-reference`; require diagnostic/model evidence |
| subrule or allomorph construction error | Some loader paths report through the error handler and continue | `partial-load`; never infer whole-grammar success |
| unrepresentable analysis word | `AnalyzeWord` can return an empty set after catching `InvalidShapeException` | runner must distinguish `invalidInput` from a valid no-analysis result |
| epenthesis or self-opaquing divergence | Current paths can throw or run without an explicit logical bound | `blocked-csharp-defect` until exposed as controlled `nonconvergent` |
| simultaneous rewrite with grouped/quantified RHS | Equal top-level child counts select a path that casts RHS children to `Constraint` | `blocked-csharp-defect`; cardinality must be classified by authored node shape as well as effective width |
| metathesis identical/missing switch captures | Identical switch IDs collide during dictionary construction; a resolved ID can still miss the structural-description capture | typed invalid or `blocked-csharp-defect`, determined by the corrected C# contract |

These rows answer “the XML contains it” separately from “C# HC does it.” If a declared feature is
supposed to work, the correct resolution is to fix Machine and then promote the row to `semantic`, not
to make another engine imitate the ignored behavior invisibly.

### 9.1 Frozen XML-surface accountability map

The prose taxonomy is backed by a generated, exact manifest of DTD declarations and loader branches.
The manifest—not this grouped table—is the completeness input. This table states where every surface
family must land and prevents a whole XML family from escaping the semantic review:

| XML/loader surface family | Required catalog destination |
|---|---|
| document root, languages, activation, DTD defaults, ID/IDREF resolution | §4.1 plus loader outcome records |
| parts of speech; phonological, head, and foot feature systems; symbolic/complex feature values | §4.2 |
| MPR features, groups, match type, and output type | §4.3 |
| character-definition tables, segment/boundary definitions, representations, normalization | §4.4 |
| natural classes, alpha variables, phonetic sequences, simple contexts, quantifiers | §4.5 |
| left/right environments, anchors, previous/next-word and null contexts | §4.5 or explicit §9 limitation |
| lexicon, lexical entries, root allomorphs, stem names/regions, families, properties | §4.6 |
| morphological and realizational rules, subrules, inputs, output actions, gates, output state | §4.7 |
| compounding rules, head/non-head inputs, production restrictions, output state | §4.8 |
| affix templates, slots, required/final flags, loose rules, strata and table assignment | §4.9 |
| rewrite rules/subrules, mode, direction, structural descriptions/changes, reapplication | §4.10 |
| metathesis rules, direction, switch captures, context and movement behavior | §4.11 |
| morpheme/allomorph co-occurrence, polarity, adjacency and target collections | §4.12 |
| syntactic rules and subcategorization surfaces | §9 until implemented |
| runtime Morpher options, selectors, tracing, logical budgets and adapter outcomes | §4.13, outside the XML tuple denominator |

For each DTD declaration the generated manifest records element/attribute path, type or enum members,
default, cardinality, containing carriers, loader read sites, model destination, execution read sites,
disposition, and feature IDs. For each loader/model branch it records the source location and at least
one feature or typed-outcome ID. CI fails on an unmapped declaration, enum member, default, carrier,
loader branch, or execution branch. Reflection over public model types is supplemental only: private
derived branches and swallowed-error paths require source-maintained branch markers or an analyzer.

## 10. Known gaps in the existing conformance vocabulary and corpus

`conformance/constructs.txt` is useful but insufficient as the canonical catalog because its rows mix
rule types, modes, containers, operands, representation, runtime options, and tracing. It does not
enumerate enum/default partitions, carriers, derived rule shapes, or interactions.

Source and current-fixture inspection identifies at least these gaps or unproven partitions:

- MPR group `append` is distinct from `overwrite`; current graduated grammars visibly emphasize
  overwrite and cannot transfer that evidence to append.
- Negative alpha-variable polarity and multiple distinct alpha variables need distinct evidence.
  Reuse of one positive variable already exists in the suffixing-vowel-harmony fixture and must not be
  reported as absent.
- Template `final=false`, partial entries/rules, and bound roots need explicit semantic witnesses.
- Morphological `multipleApplication=0`, values greater than one, and the upper DTD boundary need
  discriminating evidence rather than comments about default one.
- Compounding multiple application and recursive compound-of-compound behavior remain incompletely
  attributed in the existing suite.
- Ordinary and realizational `blockable=false/true` need direct contrasts, not only compound blocking.
- Empty, leading, trailing, and internal omission on morphological RHS must be separately covered;
  “subtraction/truncation” is too coarse.
- Reordered captured parts (movement) must be distinguished from repeated capture (copy/reduplication).
- Every rewrite cardinality family—feature change, deletion, epenthesis, expansion, merge, and empty
  output—needs both iterative/simultaneous combinations where expressible and direction combinations
  where expressible.
- Rewrite subrule priority/overlap, allomorph priority/free fluctuation, template alternatives, and
  unordered-rule union are four different choice mechanisms and need separate evidence.
- Metathesis needs right-to-left, anchored, quantified, adjacent, and non-adjacent profiles rather
  than one generic construct row.
- Pattern atoms and quantifiers need carrier-specific coverage; loader execution in a root shape does
  not prove rewrite/environment/metathesis behavior.
- Featureless phonology, mixed symbolic/complex `FeatureValue` shapes, allomorph-environment alpha
  variables, and grouped/quantified simultaneous rewrite RHSs need explicit accepted/ignored/blocked
  evidence rather than inheriting a nearby feature-structure or pattern result.
- Multi-table tests must cover disjoint, overlapping, explicit-override, and cross-stratum threading
  shapes.
- Required/excluded environments need left, right, bilateral, overlapping, and contradictory cases on
  roots and rule allomorphs.
- Morpheme and allomorph co-occurrence each need both require/exclude, all five adjacency values,
  multiple targets, chains, and contradictory/cyclic configurations.
- No current completeness mechanism proves every compatible high-level pair, connected triple,
  ordering, unordered declaration permutation, or mixed partial order has a discriminating word.
- Existing trace-based `rules:` coverage proves participation, not that the interaction or order is
  load-bearing. Counterfactual C# results are required.
- Loader-ignored DTD fields in §9 are currently liable to look covered merely because the grammar
  validates. They need explicit limitation tests and reporting.

This list is the initial audit, not a waiver for anything not named. The generated completeness gate,
not this prose list, is the permanent defense against omissions.

## 11. Required implementation artifacts

The design resolves into these Machine-owned artifacts:

1. a standalone JSON Schema for feature, combination, evidence, and retirement records;
2. a versioned semantic catalog mapping every DTD/loader/model surface to a disposition and effect
   signature;
3. an exhaustive candidate generator and canonical ID implementation;
4. a ledger containing every required, retired, or blocked obligation;
5. conformance grammar/word references from each required obligation;
6. a C# runner that records structured semantic identity or typed outcomes per word;
7. declaration-permutation generation for unordered cases and counterfactual variants for ordered
   cases; and
8. CI gates for catalog completeness, fixture completeness, C# evidence completeness, and stale
   checked-in results when a grammar or word changes.

Until those artifacts exist, this document is the source-derived design inventory, not a claim that
the current corpus already provides the coverage it specifies.
