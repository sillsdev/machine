# Interaction patterns: schema and stress test

`docs/coverage-levels.md` names level 3 — interactions between surfaces — and states why pairs
cannot represent it: the MPR-overwrite hazard is a conjunction of four ingredients (an
`overwrite` feature group, an `unordered` stratum, two rules writing that group, a downstream
rule gated on the evicted member) where every ingredient is individually `Evidenced` and no pair
of them is itself an interaction. That document also states what is not built: "the enumerator,
the reachability analysis, and the interaction ledger." This document proposes a schema for the
first of those three — what an enumerable *pattern* looks like — and then spends it against an
independently-derived set of eleven real HermitCrab hazards to find out whether the schema is
actually adequate, before any of it gets built.

The eleven hazards live at `sillsdev/machine`'s `docs/hc/gotchas/` — a reference written by
someone reading the HermitCrab engine source with no knowledge of this catalog or this schema.
That independence is the point of using them as a test: a schema that only explains the example
it was designed around proves nothing.

## The schema

An interaction pattern is a hyperedge over grammar declarations, not a set:

- **`roles`** — named participants. Each role is a DTD element plus attribute predicates on it
  (e.g. "a `Stratum` with `morphologicalRuleOrder="unordered"`").
- **`joins`** — structural bindings between roles: an IDREF on one role's attribute resolving to
  another role's `id`, or containment (one role's element appearing inside another's subtree).
  This is what makes the pattern a hyperedge instead of a bag of independently-true facts — the
  MPR hazard's four ingredients are each `Evidenced` in isolation precisely because nothing joins
  them.
- **`distinct`** — non-identity constraints between roles bound to the same element type, so a
  matcher does not count a rule as interacting with itself when a pattern needs two different
  rules.
- **`surfaces`** — the level-2 surface IDs the pattern touches, split into the ones it makes
  grammar-observable (`observable`) and the ones it merely depends on structurally to exist
  (`supporting`) — e.g. the MPR hazard's gate rule needs *some* `PartOfSpeech` to be well-formed,
  but the hazard is not about parts of speech.
- **`decision`** — how an instantiation is confirmed, not merely located. Two modes, and the
  stress test below shows they are not always cleanly separable:
  - `structural` — answerable by walking the grammar XML and its IDREF graph: attribute values,
    element containment, ID resolution. Deterministic, cheap, and — per `coverage-levels.md`'s own
    admission — not sufficient by itself for reachability.
  - `language` — requires intersecting one rule's output language against another's trigger
    context, an automaton construction over the compiled grammar, not a scan of the XML.
    `coverage-levels.md` states this directly for "can interact"; several patterns below need it
    for a *different* question — not whether two things can co-occur, but whether a computed
    property (a feature-structure union, a self-satisfying environment) crosses a threshold.
- **`discriminator`** — the observable that falsifies the pattern: a per-phase trace delta
  (`PhaseTraceRecorder`, `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/
  PhaseTraceRecorder.cs`) showing two orders/configurations produce different `FinalParse` events,
  or produce the same events but a different `AnalyzeWord`/`SynthesisConfirmation` count.
  `PhaseTraceRecorder` matters here specifically because HermitCrab unapplies rules in reverse —
  an interaction witnessed only in synthesis is untested in the direction a proposer actually runs.
- **`witness`** — a fixture path, a word, and the counterfactual row that shows the delta, in
  exactly the shape `conformance/semantic-coverage-counterfactuals.tsv` already uses (surface ID,
  verdict, fixture, neutralization, delta, word, before-parse, after-parse) — or `null` when the
  pattern is known to instantiate in some grammar but no fixture demonstrates it yet.

**Stored:** the pattern catalog (human-sized — dozens of patterns, not thousands) and the witness
ledger (one row per witnessed instantiation, same shape as the existing level-2 ledger).
**Computed, never stored:** instantiations — concrete bindings of roles to declarations in a
specific grammar that satisfy every join and every distinct constraint. "How many level-N
interactions does this grammar have" means "how many instantiations does it produce, summed over
patterns of arity N" — a query answered fresh against each grammar, not a fixed count carried in
the catalog. This mirrors the level-2 design: `conformance/semantic-catalog.yaml` stores the
surface vocabulary once; `conformance/semantic-coverage-counterfactuals.tsv` is regenerated per
run.

## Worked example: the MPR overwrite/order hazard

### A plausible first draft, and why it is wrong

A first pass at this pattern, written from `coverage-levels.md`'s prose alone without checking the
DTD, would reach for the natural-sounding roles: an MPR feature group living on the `Stratum` that
uses it, and `requiredMPRFeatures`/the output-MPR attribute living on `MorphologicalRule` itself,
since that is the element a grammar author thinks of as "the rule." Checking this against
`conformance/HermitCrabInput.dtd` and the real fixture at
`conformance/edge-cases/morphotactic-attribute-breadth/grammar.xml` shows three corrections:

| Draft assumption | What the DTD actually says | Where |
|---|---|---|
| The feature group is declared on the `Stratum` | `MorphologicalPhonologicalRuleFeatureGroup` is a child of `Language/MorphologicalPhonologicalRuleFeatures`, a sibling of `Strata`, not nested under any stratum | DTD lines 75–87 |
| `requiredMPRFeatures`/output MPR features are attributes of `MorphologicalRule` | The gate is `MorphologicalInput/@requiredMPRFeatures` (or `@excludedMPRFeatures`); the write is `MorphologicalOutput/@MPRFeatures`. Both are two containment levels below `MorphologicalRule`, inside its `MorphologicalSubrules/MorphologicalSubrule` | DTD lines 402–425 |
| "Same stratum" means "same `<Stratum>` element as parent" | A `MorphologicalRule` declared inside a `Stratum`'s `MorphologicalRuleDefinitions` only actually *participates* in that stratum's rule cascade if it is also named in the `Stratum`'s own `morphologicalRules` IDREFS attribute, or reachable through one of that stratum's `AffixTemplate`/`Slot.morphologicalRules` | DTD lines 239–247, 263–271; confirmed by `conformance/edge-cases/morphotactic-attribute-breadth/grammar.xml`'s own header comment: "multipleApplication has NO effect on a rule reached through an AffixTemplate slot... Every repeatable rule here is therefore listed on the Stratum's own morphologicalRules attribute instead" |

The third correction is the one that would actually break a matcher: containment nesting is not
the join. `conformance/edge-cases/mpr-overwrite-order-dependence/grammar.xml` (an
already-authored, currently unstaged fixture in this worktree that turns out to be exactly this
pattern's witness — see below) lists every participating rule explicitly:
`<Stratum characterDefinitionTable="tbl" morphologicalRuleOrder="unordered"
morphologicalRules="mrSetX mrSetY mrGateX mrSetP mrSetQ mrGatePQ">`. A matcher that used "same
parent `<Stratum>` element" instead of "named in `morphologicalRules`" would happen to get the
right answer on this one fixture only because it declares no `AffixTemplate` — the general case
needs the IDREF, not the containment edge.

There is also a hop the draft skips over entirely: the group's `features` attribute is itself
IDREFS to individual `MorphologicalPhonologicalRuleFeature` declarations (`mprX`, `mprY`, DTD line
86), and both the write side (`MorphologicalOutput/@MPRFeatures`) and the gate side
(`MorphologicalInput/@requiredMPRFeatures`) resolve to those same per-feature IDs, not to the
group directly. "Rule writes a member of the group" and "rule gates on a member of the group" are
each a two-hop IDREF resolution (rule → feature id → group.features), not a one-hop check against
the group.

### The corrected pattern

```
pattern MprOverwriteOrderDependence:
  roles:
    Group:    MorphologicalPhonologicalRuleFeatureGroup[outputType = "overwrite"]
    Stratum:  Stratum[morphologicalRuleOrder = "unordered"]
    WriteA:   MorphologicalOutput  (nested in some MorphologicalRule under Stratum)
    WriteB:   MorphologicalOutput  (nested in some other MorphologicalRule under Stratum)
    Gate:     MorphologicalInput | PhonologicalSubrule | HeadMorphologicalInput
              (any of the three elements the DTD gives requiredMPRFeatures/excludedMPRFeatures to)
  joins:
    f1 in Group.features, f2 in Group.features                       # both IDREFS into
                                                                       # MorphologicalPhonologicalRuleFeature
    f1 in WriteA.MPRFeatures
    f2 in WriteB.MPRFeatures
    f1 in Gate.requiredMPRFeatures
    containing-rule(WriteA) in Stratum.morphologicalRules-closure    # rule OR reachable via a
    containing-rule(WriteB) in Stratum.morphologicalRules-closure    # Slot.morphologicalRules
    containing-rule(Gate)   in Stratum.morphologicalRules-closure    # of an AffixTemplate on Stratum
  distinct:
    f1 != f2
    WriteA != WriteB
  surfaces:
    observable: [dtd:enum/MorphologicalPhonologicalRuleFeatureGroup/outputType/overwrite,
                 dtd:enum/Stratum/morphologicalRuleOrder/unordered]
    supporting: [dtd:attribute/MorphologicalOutput/MPRFeatures,
                 dtd:attribute/MorphologicalInput/requiredMPRFeatures]
  decision:
    structural: locate all (Group, Stratum, WriteA, WriteB, Gate) bindings satisfying the joins —
                pure IDREF resolution, no automaton needed
    language:   confirm reachability — that some single word's derivation can realize WriteA-then-
                WriteB and a different word's derivation can realize WriteB-then-WriteA, both
                inside the same unordered stratum's search. This is exactly the step
                coverage-levels.md says a pairwise/structural scan cannot do.
  discriminator: FinalParse digest differs between the two orders for words that are otherwise
                 identical except for which of WriteA/WriteB's rule fired last
  witness: conformance/edge-cases/mpr-overwrite-order-dependence — root "dabo", rules
           mrSetX/mrSetY/mrGateX, group "overwriteGroup" (mprX, mprY)
```

Arity is 5 (Group, Stratum, WriteA, WriteB, Gate) — matching `coverage-levels.md`'s "irreducibly
~5-ary" claim exactly, not by construction of this document but because that is what the DTD
graph actually requires to reach the hazard.

### The witness already exists

`conformance/edge-cases/mpr-overwrite-order-dependence/grammar.xml` (untracked in this worktree at
the time of writing, so not yet part of the default suite) instantiates this pattern directly: an
`overwriteGroup` over `mprX`/`mprY`, an `unordered` `Main` stratum, `mrSetX`/`mrSetY` each writing
one member, `mrGateX` gated on `requiredMPRFeatures="mprX"`. Its own header comment states the
predicted delta: applying X then Y leaves only `mprY` (`MprFeatureSet.AddOutput` evicts absent
group members before unioning in the new output), so `mrGateX` succeeds only through the
Y-then-X derivation. It also carries a same-shape `appendGroup`/`mrGatePQ` control that succeeds
under both orders — the counter-example that attributes the effect to `outputType` specifically,
not to some other difference between the two probes.

Independently, every one of the four ingredients is already `Evidenced` in
`conformance/semantic-coverage-counterfactuals.tsv`, each through a *different* fixture — which is
the concrete demonstration of `coverage-levels.md`'s claim that individual evidence does not add
up to conjunction coverage:

| Surface | Verdict | Fixture |
|---|---|---|
| `dtd:enum/MorphologicalPhonologicalRuleFeatureGroup/outputType/overwrite` | Evidenced | `languages/fusional-realizational-morphology` |
| `dtd:enum/MorphologicalPhonologicalRuleFeatureGroup/outputType/append` | Evidenced | `edge-cases/morphotactic-attribute-breadth` |
| `dtd:enum/Stratum/morphologicalRuleOrder/unordered` | Evidenced | `edge-cases/morphotactic-attribute-breadth` |
| `dtd:enum/Stratum/morphologicalRuleOrder/linear` | Evidenced | `edge-cases/feature-gating-breadth` |

None of those four fixtures is the fixture that actually instantiates the 5-ary pattern. The
`mpr-overwrite-order-dependence` fixture is new precisely because no existing fixture combined
an unordered stratum with two rules writing one overwrite group — confirmed by its own header
comment: "morphotactic-attribute-breadth's own unordered stratum, Second, holds a single MPR-inert
rule."

## What a matcher must do

1. **Resolve the IDREF graph once per grammar.** Every join above is an IDREF or containment
   lookup; none require running the engine. This is cheap and the same shape as the existing
   level-2 coverage pipeline already does for surface presence.
2. **Enumerate candidate bindings** for each pattern's roles against that graph — a constraint
   search bounded by the grammar's actual declarations, not the 264-surface cross product
   `coverage-levels.md` rules out (34,716 pairs, ~3.03M triples, ~198M 4-tuples over all surfaces
   generically; the point of "the unit must be structural, not combinatorial" is that this search
   is instead bounded by what actually co-occurs — one stratum's rule list, one group's member
   count — which is small).
3. **Split by decision mode.** Structural bindings that need no automaton are instantiations
   outright. Bindings whose pattern declares a `language` step are only *candidates* until that
   step runs; per `coverage-levels.md`'s three states, a candidate is `cannot-interact`,
   `can-interact-unwitnessed`, or `can-interact-witnessed` only after this step, never before.
4. **Attach or request a witness.** A candidate with a matching row in the counterfactual ledger
   is witnessed. One without a witness but a positive reachability decision is the actionable
   output this whole design exists to produce — `coverage-levels.md`'s "reachable and unwitnessed
   is a conformance gap worth a fixture."

## Stress test: eleven independently-derived hazards

Each of `sillsdev/machine`'s `docs/hc/gotchas/*.md` was attempted against the schema above.
Roles/arity/decision reflect the tightest pattern that captures the hazard's actual mechanism, not
a loosened version chosen to make it fit.

| # | Gotcha | Roles (sketch) | Arity | Decision | Verdict |
|---|---|---|---|---|---|
| 1 | Affix-template optional slots (`O(2ⁿ)`) | `Slot[optional=true]` × 2, same parent `AffixTemplate` | 2 (per pair; severity is instantiation *count* over the template, not one instantiation) | structural (shape) + language (are the two rules' domains actually distinguishable, i.e. is the "skip" path surface-identical to a null-realized "apply") | Expressible, with a caveat |
| 2 | Unordered/linear strata: `linear` is `O(n)` to generate, still `O(2ⁿ)`-ish to parse | `Stratum[morphologicalRuleOrder=linear]` | 1, plus a **phase** axis (`AnalysisCandidate` vs `SynthesisConfirmation`) that is not a grammar declaration at all | n/a — the same attribute, same grammar, has two different cost profiles depending on which of `LinearRuleCascade` vs `PermutationRuleCascade` the direction picks | **Needs a new concept**: a role that is a run-time phase, not a DTD element |
| 3 | Kitchen-sink natural classes widen environments | `SegmentNaturalClass` (k member `Segment`s) + consuming `SimpleContext`/`Environment` | 2 (plus internal k-way feature union) | structural, but the "structural" step is simulating the loader's feature-union algorithm (does the union converge to `ANY`), not a plain attribute-equality query | Expressible, decision heavier than the two-bucket model suggests |
| 4 | Environment-conditioned allomorphs deferred to final validity | ≥2 `Allomorph[RequiredEnvironments]` on one entry/subrule set, ordered, none last/free-fluctuating | 2+ (variable) | language (need environment-vs-surface satisfiability) | **Does not fit**: no discriminator exists — the final parse is identical either way; only the number of live candidates differs, and the schema's discriminator is defined as an output/trace delta between two *orders*, not a magnitude |
| 5 | MPR features / co-occurrence rules filter late, not early | `MorphCoOccurrenceRule` or `requiredMPRFeatures` gate, alone | 1, no distinguishing co-occurrence condition | n/a | **Does not fit at all**: true of every grammar using the construct, by construction of the engine — not an interaction between two things that "can co-occur," and correctness is unaffected either way, so there is nothing to falsify |
| 6 | MPR overwrite groups are order-dependent | see worked example above | 5 | structural + language | Cleanly expressible (this is the design's own basis) |
| 7 | Iterative rules rescan mutated output | `PhonologicalRule[multipleApplicationOrder]`'s own inserted output checked against its own `Environment` | 1 (self-referential) | language (does the inserted material's feature bundle re-satisfy the environment pattern) | **Needs a new concept**: the pattern needs a role checked against *itself*, not `distinct` (which enforces non-identity) but its opposite |
| 8 | Epenthesis 256-node crash; metathesis has no cap | (a) same self-join shape as #7; (b) "one crashing word aborts the rest of the batch" | (a) 1, self-referential; (b) needs a **word-list/batch-order** role | (a) language; (b) n/a — not a grammar property at all | **Needs a new concept** on both halves — this is the second and third distinct non-DTD role category found (after #2's phase) |
| 9 | Compounding split-point enumeration | `CompoundingRule` head/non-head pattern breadth + `Morpher.MaxStemCount` | 2, but one role is a **host-application setting**, `grammar_visible: partially` per the gotcha's own header — never appears in the grammar XML at all | structural for the pattern breadth; the stem cap isn't in the document to query | **Needs a new concept**: a role that is external runtime configuration, not a DTD element and not derivable from one |
| 10 | Pattern-shaped root allomorphs bypass the trie | `RootAllomorph` whose shape uses `OptionalSegmentSequence` (iterative/optional, not literal `Segment`s) | 1 — cost scales with the *count* of such allomorphs in a stratum's lexicon, not a conjunction of distinct co-occurring surfaces | structural | Expressible, but degenerate: no real join, better served by a census than a hyperedge matcher |
| 11 | Stem-name-restricted allomorph needs the feature explicitly assigned | `StemName[Region requires FeatureValue f=v]`, `RootAllomorph[stemName=...]` | 2–3 (plus whichever rule, if any, assigns `f` on the reachable path) | language (is `f` set in the accumulated feature structure along some derivation reaching this allomorph) | Cleanly expressible — a bare-root word with zero valid parses is a real, witnessable discriminator in this suite's existing vocabulary |

### Classification summary

- **Cleanly expressible: 2 of 11** (#6, #11).
- **Expressible with a caveat the schema already anticipates** (instantiation *count* as the real
  signal, or a heavier structural decision step): **2 of 11** (#1, #3).
- **Does not fit the schema at all**, because the hazard has no correctness discriminator — output
  is identical, only internal cost differs: **2 of 11** (#4, #5).
- **Needs a role that is not a DTD element**, in three distinct flavors — a runtime *phase*
  (#2), a *host-application setting* outside the grammar file (#9), and *batch/word-list order*
  (#8b) — plus two cases needing a *reflexive/self-join* the `roles`/`distinct` vocabulary does not
  currently support (#7, #8a): **4 of 11** (#2, #7, #8, #9).
- **Expressible but degenerate** — arity 1, no real join, a plain census would serve better than a
  hyperedge matcher: **1 of 11** (#10).

Arity distribution actually found: one 5-ary pattern (#6, matching the design's own worked
example), a handful of 2-ary and 2-plus-variable patterns (#1, #3, #4, #9, #11), several arity-1
patterns that are not really interactions at all (#2's single surface, #5, #7's self-join, #10),
and one hazard (#8) that splits into an arity-1 self-join plus a non-grammar batch property. There
is no cluster at 3 or 4; the distribution is bimodal between "1 (not really an interaction)" and
"2 or more (genuinely a hyperedge)," with the design's own arity-5 example as an outlier on the
high end, not a typical case.

## Open problems

1. **The schema conflates "interaction between surfaces" with "correctness witnessing," and most
   real hazards are cost hazards, not correctness hazards.** Of the eleven, seven are
   fundamentally about asymptotic cost (`O(2ⁿ)`, `O(n!)`, multiplicative stacking, linear-scan vs.
   trie) with the final parse result unaffected — only four (#6, #7, #8's crash, #11) have an
   actual behavioral delta a `discriminator` can express as written. A discriminator defined as
   "does the `FinalParse` digest differ" cannot witness a hazard where the final parse is identical
   and only the number of candidates explored differs.

2. **A possible partial fix, not yet validated:** `PhaseTraceRecorder`'s per-phase event count
   (already used elsewhere in this repo's own optimization work as an admissible "deterministic
   counter delta," per the standing rule that wall-clock timing is inadmissible evidence) could
   serve as a cost-shaped discriminator — "did the number of `AnalyzeWord`/rule-application events
   for this word increase" — for hazards like #1 and #4. This does not fully close the gap: an
   asymptotic claim (`O(2ⁿ)`) is a statement about a *family* of grammars at increasing n, and the
   schema's `witness` is a single fixture+word+counterfactual row, not a scaling curve. Witnessing
   "this pattern instantiates" and witnessing "this pattern's cost is exponential in n" are
   different claims requiring different evidence shapes.

3. **Some hazards are universal architectural facts about a single construct, not
   grammar-dependent interactions.** #5 (MPR/co-occurrence checks always run late) is true of
   every grammar using the construct, with no distinguishing structural condition and no
   correctness delta. It does not belong in an interaction-pattern catalog at all; it belongs in a
   different document (an engine-mechanics reference, which is exactly what the `gotchas/` files
   already are). Forcing it into the schema as a degenerate arity-1, no-join, no-discriminator
   "pattern" would silently invent false structure.

4. **The schema has no vocabulary for a role bound to something other than a DTD declaration.**
   Three distinct non-grammar role categories turned up independently: a *run-time phase*
   (analysis vs. synthesis, #2), a *host-application configuration value* not present in the
   grammar file at all (`Morpher.MaxStemCount`, #9), and *batch/word-list ordering* (#8's
   crash-aborts-the-rest-of-the-run behavior). These are not variations on one gap; they are three
   separate places a hazard's real cause lives outside the document the schema queries.

5. **The schema has no vocabulary for a role checked against itself.** #7 and #8a both hinge on a
   single rule's own output re-satisfying its own trigger — not two distinct participants joined
   together, but one participant's substructure checked against another substructure of the same
   declaration. `distinct` enforces the opposite property (non-identity between roles); nothing in
   the sketch currently supports the reflexive case.

6. **`decision` is not a clean two-way split in practice.** Every pattern that needed a `language`
   step in the stress test also needed a cheap `structural` pre-filter first (#1, #3, #4, #6, #11)
   — the two are sequential phases of one decision, not alternative tags on a pattern. Separately,
   #3's "structural" step (simulating a feature-structure union to see if it degenerates to `ANY`)
   is a heavier computation than the MPR pattern's plain IDREF resolution, while still requiring no
   automaton — suggesting the real taxonomy has at least three tiers (attribute/IDREF lookup;
   deterministic feature-algebra simulation; automaton-based language intersection), not two.

7. **Arity is sometimes a family parameter, not a fixed number.** #1's severity is the *count* of
   pairwise instantiations across a template's slot set (scaling with `n`), and #10's is the count
   of pattern-shaped allomorphs in a stratum's lexicon — in both cases the interesting quantity is
   an aggregate over many arity-2 (or arity-1) instantiations sharing a common parent, not the
   existence of a single higher-arity instantiation. The schema's "count instantiations of a
   pattern" framing already anticipates this for #1, but #10 shows the same shape occurring for a
   pattern that is not really a multi-role interaction at all (see finding 3) — the aggregate-count
   signal and the hyperedge-arity signal are two different things that happen to look similar.

## Bottom line

The schema is precise and correct for the one hazard it was built around: the MPR
overwrite/order-dependence pattern is a real 5-ary hyperedge, its joins are IDREF chains that
exist in the DTD exactly as described once corrected against it (Part 3 above), and a witness for
it already exists as an authored fixture in this worktree. Against that single case, the design
holds up completely.

Against the eleven independently-derived hazards, it does not generalize. Two fit cleanly, two fit
with anticipated caveats, and seven either fail outright (no discriminator exists for a pure cost
hazard) or need a concept — a non-grammar role, a reflexive join, a multi-tier decision, a
scaling-family witness — that is not in the current sketch. The dominant failure mode is not a
flaw in the hyperedge mechanism itself, which correctly located candidate role-bindings for every
one of the eleven; it is that the schema was designed exclusively around *correctness*
interactions (does order change the final parse), while most of what a HermitCrab grammar author
actually needs warned about is a *cost* interaction (does this combination make parsing
explode) — a different claim, needing a different kind of evidence, that this schema does not yet
know how to hold.

**This means the schema as drafted is not adequate as a general level-3 interaction-pattern
catalog.** It is adequate for a narrower, still-useful category: multi-declaration correctness
hazards decidable from the grammar plus a trace-level before/after witness. Extending it to cover
the cost-hazard majority, the non-grammar-role cases, and the reflexive-join cases is not a matter
of relaxing constraints on the existing fields — each of those requires a genuinely new mechanism
(a count-based discriminator with a defined evidence shape for scaling claims; a role type for
run-time and host-configuration values; reflexive joins), and building the enumerator against only
the MPR-shaped subset would misrepresent how much of the real hazard space it actually reaches.
