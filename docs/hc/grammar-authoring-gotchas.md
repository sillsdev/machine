# HermitCrab grammar authoring: constructs and performance gotchas

Part of the [HermitCrab-for-LLMs reference](README.md). Repo: `sillsdev/machine`.

## What this covers

This file documents the constructs a linguist actually writes when authoring a HermitCrab
XML grammar (strata, morphological/phonological rules, natural classes, compounding, lexical
entries) — the mechanics behind each one, and the specific ways grammar-authoring choices blow
up parse/generation time or produce surprising results. It's grounded entirely in the engine
source under `src/SIL.Machine.Morphology.HermitCrab/` and `src/SIL.Machine/`, plus the
synthetic conformance fixtures under `conformance/` (toy grammars built to pin exact engine
behavior, not real languages). If you have a real FieldWorks grammar and a slowness question,
describe its structure abstractly against the patterns below rather than pasting the real
rules — see the privacy note in [`README.md`](README.md).

A companion doc, [`affix-templates-and-optionality.md`](affix-templates-and-optionality.md),
already covers affix templates and slot optionality in depth (the `O(2ⁿ)` blowup from
independently-optional slots). This file summarizes that topic in one section and does not
duplicate it — see that doc for the full traversal-algorithm walkthrough.

## Quick reference

| Construct | Primary file(s):line | One-line description |
|---|---|---|
| Stratum / `morphologicalRuleOrder` | `Stratum.cs:9-13,126`; `SynthesisStratumRule.cs:25-41`; `AnalysisStratumRule.cs:36-64` | Per-stratum choice between a fixed rule pipeline (`linear`) and a full combinatorial search over rule orderings (`unordered`) — and even `linear` is combinatorial on the *analysis* side. |
| Affix templates / slots | `AffixTemplateSlot.cs`; `AffixTemplate.cs`; `SynthesisAffixTemplateRule.cs`; `SynthesisAffixTemplatesRule.cs:33-55` | See the companion doc; independently-optional slots are `O(2ⁿ)`, one non-optional multi-rule slot is `O(n)`. |
| Natural classes / feature structs | `FeatureStruct.cs:375-414,1010-1068,1224-1227`; `SymbolicFeatureValue.cs`; `SegmentNaturalClass.cs`; `SimpleContext.cs` | Disjunctive symbolic feature values are cheap bitset ops; the real gotcha is that a natural class spanning dissimilar segments *loses* constraining features, widening which string positions a rule's environment matches. |
| Morphological rules & subrules (allomorphs) | `MorphemicMorphologicalRule.cs`; `MorphologicalRules/AffixProcessRule.cs`; `MorphologicalRules/SynthesisAffixProcessRule.cs:120-233` | `k` allomorphs on a rule cost `O(k)` to try, but environment-conditioned allomorphs can multiplicatively fan out because HC can't enforce their disjointness until a surface form exists. |
| MPR features & co-occurrence rules | `MprFeature.cs`; `MprFeatureGroup.cs`; `MprFeatureSet.cs`; `MorphCoOccurrenceRule.cs`; `ConstraintType.cs`; `Allomorph.cs:105-204`; `Morpher.cs:563-589` | Boolean tags/co-occurrence constraints are cheap per check, but are validated only *after* a full candidate word already exists — they filter the final candidate set, they don't prune the search early. |
| Phonological rewrite rules: simultaneous vs iterative | `RewriteRule.cs:9-13,34`; `SimultaneousPhonologicalPatternRule.cs:22-36`; `IterativePhonologicalPatternRule.cs:17-48` | Simultaneous = one pass over the untouched input; iterative = rescans the *mutated* word after every match, so it can feed a rule's own output back into its trigger environment. |
| Epenthesis / metathesis loop guards | `HCFeatureSystem.cs:14-16,48-55`; `EpenthesisSynthesisRewriteSubruleSpec.cs:23-43`; `InfiniteLoopException.cs`; `SynthesisMetathesisRuleSpec.cs` | A `Clean`/`Dirty` feature marks nodes a rule just produced so the *same* rule can't immediately rematch them; epenthesis additionally has a hard 256-shape-node cap as a backstop. |
| Compounding rules | `MorphologicalRules/CompoundingRule.cs`; `MorphologicalRules/AnalysisCompoundingRule.cs:44-60`; `MorphologicalRules/SynthesisCompoundingRule.cs`; `Morpher.cs:57` | Combines a head + non-head stem; analysis deliberately restricts the non-head to a bare-root lookup and caps total stems via `MaxStemCount` (default 2) specifically to bound the combinatorics. |
| Lexical entries, allomorphs, environments, stem names | `LexEntry.cs`; `RootAllomorph.cs`; `RootAllomorphTrie.cs`; `AllomorphEnvironment.cs`; `StemName.cs`; `Morpher.cs:30,40-54,351-354,390` | Literal-shape root allomorphs are indexed in an `O(word-length)` trie; any allomorph with an optional/iterative shape ("pattern") falls out of the trie into a linear-scanned fallback list. |

---

## 1. Strata and `morphologicalRuleOrder`

**Overview.** A `Stratum` groups a character-definition table, a lexicon (`Entries`), phonological
rules, morphological rules, and affix templates that all apply together
(`Stratum.cs:19-137`). Strata form an ordered chain — `Language` assigns each stratum a `Depth`
by list position, and synthesis only lets a root enter a stratum at or beyond its own lexical
stratum (`SynthesisStratumRule.cs:51`: `input.RootAllomorph.Morpheme.Stratum.Depth > _stratum.Depth`
short-circuits). That chaining is a plain sequential pipeline — no combinatorics there. The
per-stratum choice that *does* matter is `MorphologicalRuleOrder`, an XML attribute
(`morphologicalRuleOrder="linear"` or `"unordered"`, default `linear` —
`XmlLanguageLoader.cs:53-65`) controlling how the stratum's `MorphologicalRules` list is applied
relative to each other.

**Mechanics.** `SynthesisStratumRule`'s constructor picks a rule-cascade implementation from the
enum (`SynthesisStratumRule.cs:25-41`):

```csharp
case MorphologicalRuleOrder.Linear:
    _mrulesRule = new LinearRuleCascade<Word, ShapeNode>(mrules, true, ...);
case MorphologicalRuleOrder.Unordered:
    _mrulesRule = new CombinationRuleCascade<Word, ShapeNode>(mrules, true, ...);
```

`LinearRuleCascade.ApplyRules` (`LinearRuleCascade.cs:32-57`) walks the rule list in fixed index
order; at the first index `i` where a rule actually produces output, it recurses on that output
starting at `i+1` and then **stops trying any other rule at this level** (`if (applied) return
false;`, line 54). That is a genuine single fixed pipeline: `O(n)` rule-application attempts per
derivation, not `2ⁿ` or `n!`.

`CombinationRuleCascade.ApplyRules` (`CombinationRuleCascade.cs:32-55`) instead loops over
**every** rule not yet used on the current derivation path (a per-path `rulesApplied` set) and
recurses into each one — i.e. it explores every ordering of every subset of the stratum's
morphological rules that structurally apply. Worst case that's `O(n!)` rule-application
attempts for `n` mutually-applicable rules, bounded in practice only by
`Morpher.MaxAlternatives` (checked via `RuleCascade.CheckMaxAlternatives`,
`RuleCascade.cs:57-61`, which throws `MaxAlternativesExceededException` once the alternative
count exceeds the configured cap — 0/unbounded by default). `AnalysisStratumRule` uses
`ParallelCombinationRuleCascade` for the same case (`AnalysisStratumRule.cs:56-62`), which is the
same search parallelized across threads, not a cheaper algorithm.

**The gotcha most authors miss:** switching a slow stratum from `unordered` to `linear` only
fixes the *generation* (synthesis) side. On the **analysis** (parsing) side, `linear` does not
compile to `LinearRuleCascade` at all — `AnalysisStratumRule.cs:36-48` uses
`PermutationRuleCascade` instead, with this comment (`AnalysisStratumRule.cs:39-42`):

> Use `PermutationRuleCascade` instead of `LinearRuleCascade` because morphological rules should
> be considered optional during unapplication (they are obligatory during application, but we
> don't know they have been applied during unapplication).

`PermutationRuleCascade.ApplyRules` (`PermutationRuleCascade.cs:32-45`) loops over every rule
index from the current position onward and recurses into **each one independently against the
same input**, with no "stop after first success" — i.e., during parsing, HC must consider every
subset of a stratum's rules as a candidate explanation for the surface string, because a rule
that wasn't applied looks identical (from the surface form alone) to one that doesn't fire.
Because recursion only ever moves the index forward (`i+1`, unless the rule allows multiple
application), this restricts the search to *subsets taken in listed order* rather than full
`n!` permutations — cheaper than `CombinationRuleCascade`'s any-order-any-subset search, but
still combinatorial (`O(2ⁿ)`-ish rather than `O(n)`).

**Practical implication:** for a stratum with more than a handful of morphological rules that can
structurally co-apply, `unordered` is expensive to parse *and* generate; `linear` is cheap to
generate but still combinatorial to parse, because parsing must guess which rules applied.
There is no `PhonologicalRuleOrder` — phonological rules are always compiled into a plain
`LinearRuleCascade` regardless of stratum settings (`SynthesisStratumRule.cs:42-44`); only
morphological-rule ordering is a grammar-author choice with this cost profile. If a stratum's
rules genuinely have no fixed relative order (rare, and typically small `n`), `unordered` is
correct; for anything larger, give the rules a real order and use `linear` — it won't make
parsing `O(n)`, but it keeps generation linear and keeps the parse-side search to ordered
subsets instead of full permutations.

## 2. Affix templates and slots (summary — see companion doc)

An `AffixTemplate` applies an ordered list of `AffixTemplateSlot`s to a stem
(`AffixTemplate.cs`, `AffixTemplateSlot.cs`); within one stratum, multiple templates are tried
in a union, each gated by `RequiredSyntacticFeatureStruct` being unifiable with the stem's
features (`SynthesisAffixTemplatesRule.cs:33-55`). The full mechanics — why `n` independently
`optional="true"` slots (one rule each) cost `O(2ⁿ)`, and why collapsing them into one
non-optional slot with `n` mutually-exclusive rules costs `O(n)` — are worked out in detail,
with a live pathological fixture (`conformance/edge-cases/deep-optional-affix-nesting/grammar.xml`),
in [`affix-templates-and-optionality.md`](affix-templates-and-optionality.md). That analysis is
not repeated here.

## 3. Natural classes, feature structures, and unification cost

**Overview.** A `NaturalClass` (e.g. "vowels", "voiceless obstruents") is a named `FeatureStruct`
used as a matching constraint in rule environments (`NaturalClass.cs`). `SegmentNaturalClass`
builds its feature struct by unioning every member segment's own feature struct
(`SegmentNaturalClass.cs:12-27`); `FeatureNaturalClass` (used for classes defined directly by
feature values rather than by listing segments) just states the feature values directly.
`SimpleContext` (`SimpleContext.cs:8-22`) wraps a natural class's feature struct (plus any
pattern variables) as the actual constraint object tested against each string position during
phonological pattern matching.

**Where the cost is, and where it isn't.** Unification (`FeatureStruct.NondestructiveUnify`,
around `FeatureStruct.cs:1010-1068`) recurses over each struct's feature dictionary, following
nested complex features and re-entrancies through a `copies`/visited map that also acts as a
cycle guard — cost is proportional to the number of distinct features involved (typically tens),
not exponential in disjunction size. Within one feature, a symbolic value's disjunctive
value-set is backed by a bitset (`UlongSymbolicFeatureValueFlags` for ≤64 possible symbols,
`BitArraySymbolicFeatureValueFlags` above that — `SymbolicFeatureValue.cs:22-27`), and
`IntersectWith`/`UnionWith`/`Overlaps` are bitwise ops on that flag set
(`SymbolicFeatureValue.cs:143-159`). **Unifying two symbolic values, even with large disjunctive
sets, is cheap** — this is not the mechanism that makes broad natural classes slow.

The actual gotcha is upstream, in *how a natural class is built*. `FeatureStruct.Union`
(`FeatureStruct.cs:375-414`) only keeps a feature key that's present in **both** operands
(`_definite.RemoveAll(kvp => !otherFS._definite.ContainsKey(kvp.Key))`, line 410), narrowing the
surviving value via bitset union where a key is kept. So a natural class spanning segments that
don't share many features (e.g. "everything that isn't a vowel," lumping bilabial stops in with
sibilants and laterals) converges toward an **emptier** feature struct as more dissimilar
segments are added — and an empty feature struct renders (and behaves) as unconstrained: HC's
own `FeatureStruct.ToString` literally prints an empty struct as `"ANY"`
(`FeatureStruct.cs:1226-1227,1273`). A `SimpleContext` built from that natural class then matches
at essentially every string position, because the pattern constraint it wraps has lost the
features that would have restricted it.

**Gotcha, concretely:** defining a kitchen-sink natural class (e.g. lumping every consonant in
the inventory into one `AnyC` class instead of the specific place/manner classes a rule's
environment actually needs) does not make *unification* slow — it makes the *rule* less
selective, so its environment matches far more positions than intended, and every one of those
spurious matches is a candidate the rest of the engine (rule ordering, template slots, other
phonological rules downstream) now has to process. The blowup shows up as more candidate
match/rewrite sites explored, not as expensive feature-struct comparisons.

**Fix:** define natural classes at the granularity the rule's environment actually needs (a
`FeatureNaturalClass` stated directly in terms of the relevant features is usually more precise
and more legible than a `SegmentNaturalClass` built by listing many segments and hoping their
union keeps the right features) and check what feature struct a segment-listed natural class
actually reduces to when new segments are added to it.

## 4. Morphological rules, subrules (allomorphs), and rule-order interaction

**Overview.** A morphological rule (e.g. `MorphologicalRules/AffixProcessRule.cs`) is a named
`MorphemicMorphologicalRule` with one or more **subrules** — in the loaded model these become
`AffixProcessAllomorph`s (`AffixProcessAllomorph.cs`), each with its own `Lhs`/`Rhs` pattern,
`RequiredSyntacticFeatureStruct`, and MPR-feature gates. A rule carries
`RequiredSyntacticFeatureStruct`/`OutSyntacticFeatureStruct` at the rule level too
(`AffixProcessRule.cs:30-31`).

**Rule order is semantically load-bearing, not just cosmetic.** In synthesis
(`SynthesisAffixProcessRule.cs:121-135,181-182`):

```csharp
if (!_rule.RequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
    return Enumerable.Empty<Word>();   // rule doesn't fire
...
outWord.SyntacticFeatureStruct = syntacticFS;
outWord.SyntacticFeatureStruct.PriorityUnion(_rule.OutSyntacticFeatureStruct);
```

Each rule's required features must unify against whatever the *previously applied* rules left in
the accumulated feature struct, and its own output features feed forward for the next rule to
require against. So within a `linear` stratum, a rule that requires a feature another rule sets
must be listed after it — this is a correctness dependency, and `ObligatorySyntacticFeatures`
(`AffixProcessRule.cs:68-71`, checked at `Morpher.cs:575-587`) is HC's mechanism for catching a
word where a promised feature was never actually set by the end of the word.

**Subrule (allomorph) fan-out.** `SynthesisAffixProcessRule` builds one pattern rule per
`AffixProcessAllomorph` and loops over them in listed order (`SynthesisAffixProcessRule.cs:139`)
— structurally `O(k)` for `k` allomorphs, same shape as an `AffixTemplateSlot`'s rule batch. But
there's a documented twist (`SynthesisAffixProcessRule.cs:213-227`):

> return all word syntheses that match subrules that are constrained by environments, HC
> violates the disjunctive property of allomorphs here because it cannot check the
> environmental constraints until it has a surface form, we will enforce the disjunctive
> property of allomorphs at that time

Concretely: as soon as an allomorph in the list has an `Environment` (a phonological condition
checked only once a surface string exists), matching it does **not** stop the loop — every
subsequent allomorph is tried too, and every one whose pattern matches produces its own output
`Word`, all propagated forward independently. The loop only short-circuits early
(`SynthesisAffixProcessRule.cs:220-227`) once it reaches an allomorph with no `Environment`, an
empty `RequiredSyntacticFeatureStruct`, and no free-fluctuation relation to the current one — a
true elsewhere-case default.

**Gotcha:** a rule with several phonologically-conditioned allomorphs (e.g. "insert `-i` after a
consonant, `-ni` after a vowel, elsewhere `-mi`") looks disjunctive to a grammar author (exactly
one allomorph *should* apply to any given stem) but is not treated as disjunctive internally
until each candidate surface form is checked against its environment later. If several such
rules stack across a derivation, the independently-propagated candidates multiply. This is a
real, if usually small, multiplicative cost — it grows with the number of *environment-gated*
allomorphs per rule, not the total allomorph count, and disappears once allomorphs are made
feature-disjoint (via `RequiredMprFeatures`/`RequiredSyntacticFeatureStruct`, see §5) so the loop
can short-circuit after the first match. MPR-feature gates on an allomorph are checked first and
`continue` past non-matching ones cheaply (`SynthesisAffixProcessRule.cs:143-176`), so adding MPR
gates to environment-conditioned allomorphs is the fix, not a cost — it makes the `O(k)` loop
skip allomorphs it would otherwise have to pattern-match.

## 5. MPR features and morpheme/allomorph co-occurrence rules

**Overview.** `MprFeature`s (`MprFeature.cs`) are boolean tags — e.g. a noun-class or
conjugation-class agreement tag — attached to a `LexEntry` (`LexEntry.MprFeatures`,
`LexEntry.cs:80`) and referenced from rule/allomorph gates
(`RequiredMprFeatures`/`ExcludedMprFeatures` on `AffixProcessAllomorph`,
`AffixProcessAllomorph.cs:32-34,58-66`, and on `CompoundingSubrule`,
`CompoundingSubrule.cs:43-51`). `MprFeatureGroup` groups related tags for mutual-exclusion
semantics: `MprFeatureGroupMatchType.Any` (satisfied if *any* feature in the group is present) vs
`.All` (`MprFeatureGroup.cs:10-21`), and `MprFeatureGroupOutput.Overwrite` vs `.Append`
(`MprFeatureGroup.cs:26-37`) controls whether a rule's output MPR features replace or add to a
group's existing tags on the word (`MprFeatureSet.AddOutput`, `MprFeatureSet.cs:29-44`). These
checks (`MprFeatureSet.IsMatchRequired`/`IsMatchExcluded`, `MprFeatureSet.cs:46-96`) are
`HashSet.Contains`-style lookups grouped by `MprFeatureGroup` — `O(features on the allomorph)`,
essentially free.

**Morpheme/allomorph co-occurrence rules** are a separate mechanism for "morpheme X requires (or
excludes) morpheme(s) Y elsewhere in the word." `ConstraintType`
(`ConstraintType.cs:3-7`) is the obligatory-vs-excluded knob — `Require` / `Exclude` — loaded from
the XML `type="require"`/`type="exclude"` attribute (default `exclude`,
`XmlLanguageLoader.cs:159-170`). Positional constraint is `MorphCoOccurrenceAdjacency`
(`MorphCoOccurrenceRule.cs:11-37`) — **this is the actual "adjacency" concept the engine has; it
is not called `AdjacencyType`.** Its five values (loaded from an `adjacency` XML attribute,
`XmlLanguageLoader.cs:137-156`) are `anywhere` (default), `somewhereToLeft`,
`somewhereToRight`, `adjacentToLeft`, `adjacentToRight`. A rule attaches to either a specific
`Morpheme` (`MorphemeCoOccurrenceRule`, XML `<MorphemeCoOccurrenceRule
primaryMorpheme="..." type="require|exclude" adjacency="..." otherMorphemes="id1 id2 ..."/>`) or a
specific `Allomorph` (`AllomorphCoOccurrenceRule`, same shape with `primaryAllomorph`/
`otherAllomorphs`, `XmlLanguageLoader.cs:556-588`).

**Cost mechanics.** `MorphCoOccurrenceRule<T>.CoOccurs` (`MorphCoOccurrenceRule.cs:92-170`) takes
`word.AllomorphsInMorphOrder.ToList()` and does one linear scan of that list per rule check —
`O(m)` where `m` is the number of morphemes already in the word, **not** the size of the
grammar. `Allomorph.CheckAllomorphConstraints` (`Allomorph.cs:158-204`) loops over the
allomorph's own `AllomorphCoOccurrenceRules` and the morpheme's `MorphemeCoOccurrenceRules`, so
total co-occurrence cost for a candidate word is `O(rules-on-its-allomorphs × word-length)` —
cheap in isolation.

**The real gotcha: co-occurrence rules are a late filter, not an early prune.** They are invoked
from `Allomorph.IsWordValid` (`Allomorph.cs:105-156`), which in turn is only called from
`Morpher.IsWordValid` (`Morpher.cs:563-589`):

```csharp
return word.Allomorphs.All(allo => allo.IsWordValid(this, word));
```

— and every call site of `Morpher.IsWordValid` applies it as a final `.Where(IsWordValid)` filter
*after* the entire synthesis rule cascade has already produced a complete candidate word
(`Morpher.cs:150,218,292,329`). So a grammar author who adds obligatory co-occurrence rules
expecting them to *cut down* the combinatorial fan-out from templates/strata/allomorphs (§1, §2,
§4) will not see that benefit — the full candidate set is generated first, at whatever cost the
upstream combinatorics already impose, and co-occurrence rules only reject invalid members of
that already-built set. If a grammar is slow because of combinatorial rule/template/allomorph
interaction, adding co-occurrence constraints will fix *correctness* (spurious analyses
disappearing) but will not by itself fix *performance* — the fix for performance has to happen at
the source of the combinatorics (§1/§2/§4), same as `AllomorphEnvironment` checks
(`AllomorphEnvironment.cs:81-97`), which are similarly applied inside the same post-hoc
`IsWordValid` walk.

**Toy example** (adapted from the repo's own synthetic fixture
`conformance/edge-cases/mpr-gated-exception/grammar.xml`, a fictional toy language, not a real
one): a suffix rule excludes lexical entries tagged with an MPR feature:

```xml
<MorphologicalPhonologicalRuleFeatures>
  <MorphologicalPhonologicalRuleFeature id="mprException">Exception</MorphologicalPhonologicalRuleFeature>
</MorphologicalPhonologicalRuleFeatures>
...
<MorphologicalSubrule id="subSuf">
  <MorphologicalInput excludedMPRFeatures="mprException">...</MorphologicalInput>
  <MorphologicalOutput>...<InsertSegments><PhoneticShape>an</PhoneticShape></InsertSegments></MorphologicalOutput>
</MorphologicalSubrule>
...
<LexicalEntry id="eVokad" partOfSpeech="posMPR" ruleFeatures="mprException">
  <Allomorphs><Allomorph id="aVokad"><PhoneticShape>vokad</PhoneticShape></Allomorph></Allomorphs>
  ...
</LexicalEntry>
```

`eVokad`'s `ruleFeatures="mprException"` tag means the `-an` subrule's `excludedMPRFeatures`
check fails for it — this root simply doesn't take that suffix, an irregular-exception pattern,
cheaply gated (`O(1)` tag lookup), independent of the rest of the grammar's size.

## 6. Phonological rewrite rules: simultaneous vs. iterative application

**Overview.** `RewriteRule` (`PhonologicalRules/RewriteRule.cs:9-13,34`) has an
`ApplicationMode` — `Simultaneous` or `Iterative` — loaded from a `multipleApplicationOrder`-style
XML attribute (`"simultaneous"`, or `"rightToLeftIterative"`/`"leftToRightIterative"`, default
iterative — `XmlLanguageLoader.cs:67-80`; the left-to-right/right-to-left choice also sets
`Direction`). A rewrite rule can have multiple `RewriteSubrule`s, each with its own left/right
environment; like affix template slots, all subrules are tried and each one that matches
contributes an output — `O(k)` for `k` subrules, not exponential, *provided* the subrules'
environments are close to mutually exclusive.

**The algorithmic difference that matters:**

- `SimultaneousPhonologicalPatternRule.Apply` (`SimultaneousPhonologicalPatternRule.cs:22-36`)
  collects **all** matches from the matcher against the original, unmodified word first, then
  applies every matched subrule's output afterward in a second pass. No match ever sees the
  effect of any other match from the same `Apply` call — one pass, `O(#matches)`, no rescanning.
- `IterativePhonologicalPatternRule.Apply` (`IterativePhonologicalPatternRule.cs:17-48`) is a
  `while` loop: match, apply (or advance past a non-match), then **re-match starting just past
  the just-consumed match** on the *already-mutated* word. Content the rule just inserted or
  modified is still ahead of the scan position and can be matched again on the next loop
  iteration.

**Gotcha:** an iterative rule whose own output re-satisfies its own trigger environment can
reapply into content it just produced. HermitCrab's guard against this is a feature, not a
counter: `HCFeatureSystem.Modified` is a symbolic feature with values `Dirty`/`Clean`, default
`Clean` (`HCFeatureSystem.cs:14-16,48-55`). A rule's own LHS pattern is compiled with an added
requirement that the target be `Clean` (e.g. `EpenthesisAnalysisRewriteRuleSpec.cs:24`), and
every node a rule inserts/modifies is marked `Dirty` immediately
(`EpenthesisSynthesisRewriteSubruleSpec.cs:39-40`; similarly in metathesis, see below) — so the
*same rule* cannot immediately rematch a node it just touched, within one `Apply()` call. The
`Dirty` marks persist for the whole scan and are only reset once, at the very end
(`IterativePhonologicalPatternRule.cs:44`, `input.ResetDirty()`), so this protection covers one
full `Apply()` invocation, not across separate stratum/rule applications.

**Fix / practical takeaway:** if a phonological pattern is meant to look at the *original*
string only (a static assimilation table, for instance) rather than a genuinely cascading
process, prefer `simultaneous` — it's a strictly one-pass operation and cannot loop by
construction. Reserve `iterative` for rules that are genuinely meant to rescan (e.g. iterative
stress assignment across a whole word); be aware that its safety against self-reapplication
depends on the Clean/Dirty tagging described above, not on the grammar author doing anything
explicit.

## 7. Epenthesis and metathesis: loop guards

**Epenthesis** (segment insertion with nothing consumed on the input side) is handled by
`EpenthesisSynthesisRewriteSubruleSpec.ApplyRhs`
(`EpenthesisSynthesisRewriteSubruleSpec.cs:23-43`), which inserts the new segment(s) directly into
the word's shape. Beyond the Clean/Dirty guard from §6, there is one additional hard backstop —
the **only** throw site of `InfiniteLoopException` in the entire HermitCrab source
(confirmed by a full-source grep): a cap of 256 total shape nodes on the word
(`EpenthesisSynthesisRewriteSubruleSpec.cs:32-33`, roughly `if (targetMatch.Input.Shape.Count ==
256) throw new InfiniteLoopException(...)`). This fires if repeated epenthesis (across
iterations of the enclosing iterative loop, or via chains where each epenthesized segment
satisfies a *different* subrule/rule than the one that just inserted it, sidestepping the
per-rule Clean/Dirty check) keeps growing the word indefinitely. There is no configurable
override for this cap.

**The repo's own toy fixture demonstrates the boundary case directly** —
`conformance/edge-cases/simultaneous-epenthesis-cascade/grammar.xml` inserts a high-front-unrounded
vowel after *any* high vowel, tagged `Simultaneous`, with the fixture's own comment noting this
exact pattern would cascade under `Iterative`:

```xml
<!-- insert an HFU vowel ("i") after any high vowel, tagged simultaneous, no
     RightEnvironment. C#'s own unit test proves "buibui" <- "bubu" under Simultaneous. -->
```

Under `Simultaneous`, `bubu` → `buibui` in one pass and stops (every match is computed against
the *original* string). The same rule under `Iterative` risks each newly-inserted `i` re-creating
a new "after a high vowel" context for the *next* pass — exactly the case §6's Clean/Dirty guard
and this section's 256-node cap exist for.

**Metathesis** (swapping two matched spans) has no `ApplicationMode` of its own —
`SynthesisMetathesisRule` always wraps its rule spec in the iterative pattern-rule class
(`SynthesisMetathesisRule.cs:32`), so metathesis is always scanned iterative-style.
`SynthesisMetathesisRuleSpec` swaps the two captured spans and marks every moved segment `Dirty`
(`SynthesisMetathesisRuleSpec.cs`, around lines 100-131 for the swap, line 127 for the dirty
mark), and its switch-group LHS constraints are likewise cloned with a `Clean` requirement — so a
rule that swaps segments A and B cannot immediately re-match the now-`Dirty` result to swap them
back within the same `Apply()` call. **Unlike epenthesis, metathesis has no numeric backstop** —
no `InfiniteLoopException` throw exists anywhere in `MetathesisRule.cs`,
`SynthesisMetathesisRule.cs`, or `SynthesisMetathesisRuleSpec.cs`. Oscillation prevention for
metathesis relies entirely on the Clean/Dirty mechanism; there is no cap analogous to
epenthesis's 256-node ceiling to fall back on if a grammar somehow produces a metathesis
environment the Dirty tagging doesn't cover.

**Practical takeaway:** if a metathesis rule appears to hang rather than throw
`InfiniteLoopException`, that specific error is not the applicable diagnostic (it's
epenthesis-only) — look instead at whether the rule's environment can be satisfied again by the
result of its own swap, since the only protection there is the one-call-scoped `Dirty` tag.

## 8. Compounding rules

**Overview.** `CompoundingRule` (`MorphologicalRules/CompoundingRule.cs`) combines a **head** word
with a **non-head** word into one compound. It carries separate required-feature-structures for
each side (`HeadRequiredSyntacticFeatureStruct`/`NonHeadRequiredSyntacticFeatureStruct`,
`CompoundingRule.cs:21-22,42,44`), separate MPR-feature productivity restrictions per side
(`HeadProdRestrictionsMprFeatures`/`NonHeadProdRestrictionsMprFeatures`, lines 23-24), a
`MaxApplicationCount` defaulting to **1** (line 19 — a compounding rule fires at most once per
word by default), and `Blockable = true` by default (line 20, so a more specific compound can
block a more general one, same `CheckBlocking` mechanism used by ordinary affix rules).

**How the two combinatorics-limiting choices show up in the analysis-side code.** Splitting an
unknown surface string into head+non-head has no a-priori split point, so
`AnalysisCompoundingRule` compiles each subrule's combined head+non-head pattern with
`AllSubmatches = true` and both `AnchoredToStart`/`AnchoredToEnd`
(`AnalysisCompoundingRule.cs:24-34`) — the underlying FST-based matcher enumerates every split
point consistent with the pattern, not just one. Two explicit guards in
`AnalysisCompoundingRule.Apply` (`AnalysisCompoundingRule.cs:44-51`) bound how far this can grow:

```csharp
if (
    input.NonHeadCount + 1 >= _morpher.MaxStemCount
    || input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
    || !_rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct)
)
{
    return Enumerable.Empty<Word>();
}
```

`Morpher.MaxStemCount` defaults to **2** (`Morpher.cs:57`) — by default a word may contain at
most a head and one non-head stem; deeper nominal/verbal compounding (three or more stems)
requires explicitly raising this, which re-opens combinatorial cost the default exists to cap.

**The more important, and less obvious, limit:** `AnalysisCompoundingRule.Apply` restricts each
candidate non-head to a **bare lexical root** — it looks up root allomorphs directly via
`_morpher.SearchRootAllomorphs(_rule.Stratum, outWord.CurrentNonHead.Shape)`
(`AnalysisCompoundingRule.cs:62`), not a fully re-derived (affixed) sub-word. The code's own
comment states this is a deliberate complexity decision (`AnalysisCompoundingRule.cs:59-60`):

> for computational complexity reasons, we ensure that the non-head is a root, otherwise we
> assume it is not a valid analysis and throw it away

This means compounding's combinatorics are **not** symmetric — it is not
`parses(stem1) × parses(stem2)` recursively for both sides. The **head** genuinely re-enters the
normal stratum/affix-template pipeline (it's the same word continuing its ordinary derivation,
so it can carry its own inflectional morphology), but the **non-head** is capped to whatever root
allomorphs literally match its shape via the trie (§9) — no recursive re-affixing of the
non-head is attempted at all. `AnalysisCompoundingRule.Apply` also explicitly deduplicates
same-shape/same-allomorph candidates before continuing (`AnalysisCompoundingRule.cs:97-115`,
comment: "this is not strictly necessary, but it helps to reduce the search space").

**Gotcha and fix:** grammars that need genuinely recursive compounding (a compound whose
non-head is itself a fully affixed compound or derived stem) run up against this root-only
restriction on the analysis side by design — raising `MaxStemCount` widens how many *flat*
stems a word can contain, it does not enable recursive non-head derivation. If deep recursive
compounding is linguistically required, expect that HC's analysis-side compounding is
structured to avoid it, and budget for handling deeply nested compounds outside the compounding
mechanism (e.g. as separate lexicalized entries) rather than assuming the grammar will discover
arbitrary-depth nesting on its own.

## 9. Lexical entries, allomorphs, environments, and stem names

**Overview.** A `LexEntry` (`LexEntry.cs`) holds one or more `RootAllomorph`s
(`LexEntry.Allomorphs`, `LexEntry.cs:71-74`) — suppletive or phonologically-conditioned surface
forms of the same morpheme. Each `RootAllomorph` may carry `Environments`
(`AllomorphEnvironment`s — a left/right pattern constraint, `AllomorphEnvironment.cs:12-62`) and
a `StemName` restricting which paradigm cell(s) it's valid for (`RootAllomorph.cs:39`).

**Trie-indexed lookup, and what defeats it.** `Morpher`'s constructor builds one
`RootAllomorphTrie` per stratum from every root allomorph in that stratum's lexicon
(`Morpher.cs:36,40-49`). `RootAllomorphTrie` is a segment-by-segment FST built by chaining each
allomorph's literal shape into shared states keyed on exact feature-struct equality per node
(`RootAllomorphTrie.cs:37-68`); `Search` transduces the input shape against it
(`RootAllomorphTrie.cs:70-79`), giving lookup cost proportional to the word's length, not the
number of lexical entries. But **not every allomorph goes into the trie**: `RootAllomorph`'s
constructor sets `IsPattern = true` if any node in its shape `IsIterative()` or is an optional,
non-boundary annotation (`RootAllomorph.cs:16-29`) — i.e. any root declared with a wildcard-like
shape (a segment sequence using an unbounded/optional quantifier rather than literal segments).
`Morpher`'s constructor routes every `IsPattern` allomorph into a separate flat list,
`_lexicalPatterns`, instead of the trie (`Morpher.cs:44-47`). Lexical lookup then searches
`_lexicalPatterns` with an explicit linear loop over every entry in that list, once per analysis
attempt (`Morpher.cs:390` onward), in addition to (not instead of) the O(word-length) trie
lookup.

**Gotcha:** an allomorph whose own `PhoneticShape` is written with a broad, underspecified
segment sequence (e.g. matching "any string of segments" to model a maximally general root
shape, as opposed to the root's actual literal string) does not get the trie's near-free lookup
— every such allomorph is checked against every analysis input via a separate linear scan. A
grammar with many such "pattern" roots (as opposed to a handful of genuinely templatic ones,
e.g. reduplication bases) pays that linear cost on every word analyzed, on top of the normal trie
lookup for its literal entries.

**Fix:** reserve pattern-shaped root allomorphs for cases that are genuinely templatic (true
reduplication/prosodic templates); give ordinary roots their literal phonemic shape so they land
in the trie.

**Environments** are checked, like co-occurrence rules (§5), from `Allomorph.IsWordValid`
(`Allomorph.cs:110-125`), i.e. as a post-hoc filter on a complete candidate word rather than a
pre-filter during generation/parsing — the same "late filter, not early prune" caveat from §5
applies: broad/underspecified environments don't slow down unification per se, they just mean
more candidates survive to be checked (and, if the environment is *too* narrow or wrong,
legitimate words silently fail to parse/generate with no error beyond a trace-manager
`FailureReason.Environments`, `AllomorphEnvironment.cs:81-86`, `Allomorph.cs:112-125`).

**Stem names** (`StemName.cs`) restrict a root allomorph to only be valid when the word's
accumulated `SyntacticFeatureStruct` falls inside one of the stem name's declared `Regions`
(`StemName.IsRequiredMatch`, `StemName.cs:31-34`); any *other* allomorph of the same lex entry
that has no stem name (or a different one) is only valid **outside** that region
(`StemName.IsExcludedMatch`, `StemName.cs:36-44`, invoked from
`RootAllomorph.CheckAllomorphConstraints`, `RootAllomorph.cs:72-91`). This is the mechanism
behind "principal parts" — a root with an irregular form for one paradigm cell and a regular
form everywhere else.

**Gotcha:** because `IsRequiredMatch` tests the word's *current* feature struct, a bare stem
(before any rule has assigned the relevant feature) does not automatically satisfy a stem name's
region — even if the stem name's region only mentions one feature, a form with that feature
totally unassigned is outside the region, not inside it. The repo's own toy fixture
(`conformance/edge-cases/stem-name-restricted-root-allomorph/grammar.xml`) pins this precisely: a
root `tam`/`kap` where `kap` is `stemName`-restricted to `featPers=1`. The bare root `kap` alone
(no person feature assigned at all) has **zero** valid parses — the stem-name-restricted
allomorph requires `featPers=1` to be explicitly present, and an unmarked form doesn't count,
even trivially:

```xml
<StemNames>
  <StemName id="snP1" partsOfSpeech="posV">
    <Regions><Region><AssignedHeadFeatures><FeatureValue feature="featPers" symbolValues="symP1" /></AssignedHeadFeatures></Region></Regions>
  </StemName>
</StemNames>
...
<LexicalEntry id="eRoot" partOfSpeech="posV">
  <Allomorphs>
    <Allomorph id="aDefault"><PhoneticShape>tam</PhoneticShape></Allomorph>
    <Allomorph id="aRestricted" stemName="snP1"><PhoneticShape>kap</PhoneticShape></Allomorph>
  </Allomorphs>
</LexicalEntry>
```

Only `mrPers1` (which assigns `featPers=1`) makes `kap`-derived forms valid; the bare root, and
any form built with `mrPers2` (`featPers=2`), can only surface as `tam`. A grammar author who
expects a stem-name-restricted allomorph to be usable "whenever nothing else says otherwise"
will see silent, correct-per-the-code parse failures instead — the fix is to make sure every
morphological rule that's supposed to license a restricted stem name actually assigns the
feature value the region requires, not just a compatible one.
