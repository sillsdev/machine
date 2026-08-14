# HermitCrab affix templates: optionality and complexity

Part of the [HermitCrab-for-LLMs reference](README.md). Repo: `sillsdev/machine`.

## What this covers

HermitCrab models a word's inflectional morphology (e.g. a noun's number and case) with an
**affix template**: an ordered sequence of "slots," each slot holding the morphological
rule(s) that can fill that position. This doc explains the actual traversal algorithm,
shows why a common grammar-design choice causes exponential parse-time blowup, and gives
the linear-cost alternative.

This matters most for languages where many inflectional cells are realized with a **null
(zero-segment) affix** — the slot is filled, semantically, but nothing is inserted into the
surface string. If you naively model each such position as its own optional slot, the
parser cannot tell "slot fired with a null affix" apart from "slot didn't fire," and ends up
exploring both possibilities at every position, independently.

## The data model

```csharp
// src/SIL.Machine.Morphology.HermitCrab/AffixTemplateSlot.cs
public class AffixTemplateSlot
{
    // a slot holds a LIST of rules — these are tried as alternatives, not composed
    public ReadOnlyCollection<MorphemicMorphologicalRule> Rules { get; }

    // if true, the slot may be skipped entirely (no rule in it fires)
    public bool Optional { get; set; }
}
```

An `AffixTemplate` (`src/SIL.Machine.Morphology.HermitCrab/AffixTemplate.cs`) is just an
ordered list of these slots, applied to a stem in sequence.

## The traversal algorithm (synthesis direction; analysis is structurally the same shape)

```csharp
// src/SIL.Machine.Morphology.HermitCrab/SynthesisAffixTemplateRule.cs
_rules = template.Slots.Select(slot => new RuleBatch<Word, ShapeNode>(
    slot.Rules.Select(mr => mr.CompileSynthesisRule(morpher)),
    false, // disjunctive = false: try ALL rules in the slot, union their outputs
    FreezableEqualityComparer<Word>.Default
));

private void ApplySlots(Word input, int index, HashSet<Word> output)
{
    for (int i = index; i < _rules.Count; i++)
    {
        foreach (Word outWord in _rules[i].Apply(input))   // batch = all rules in slot i
            ApplySlots(outWord, i + 1, output);             // recurse per surviving output

        if (!_template.Slots[i].Optional)
            return;   // mandatory slot: must have produced output above, stop here either way

        // slot IS optional: loop continues to slot i+1 using the ORIGINAL `input`,
        // i.e. "skip this slot" is tried as a separate path in addition to "apply it"
    }
    output.Add(input);
}
```

And the batch itself (`src/SIL.Machine/Rules/RuleBatch.cs`):

```csharp
public virtual IEnumerable<TData> Apply(TData input)
{
    var output = new HashSet<TData>(_comparer);
    foreach (var rule in _rules)
    {
        output.UnionWith(rule.Apply(input));
        if (_disjunctive && output.Count > 0)
            return output;   // (not our case — slots use disjunctive: false)
    }
    return output;   // unions ALL matching rules' outputs
}
```

Two facts fall out of this that drive everything below:

1. **Across slots**, the recursion is per-slot, not per-rule: `ApplySlots` recurses once for
   each slot's *combined* batch output, not once per individual rule inside a slot.
2. **Within a slot**, every rule in the slot is tried and *all* that structurally unify
   contribute an output (union, not first-match). So a slot with `k` mutually-exclusive
   rules costs `O(k)` — not `O(2^k)` — *provided* the rules' required feature structures are
   actually mutually exclusive, so normally only one (or a few, for genuine ambiguity)
   unifies per analysis.

## Pattern A: one slot per affix, each optional — exponential

This is how a grammar with 20 independent prefix positions (say, 10 grammatical cases ×
singular/plural, many realized as null) is often modeled at first: 20 slots, each
`optional="true"`, each holding one rule.

Every optional slot independently contributes an "applied" branch and a "skipped" branch,
because the recursion has no way to know that a null-realized rule firing looks identical,
on the surface, to the slot never having fired. Cost for a single stem: **O(2ⁿ)** — for
n = 20, roughly 10⁶ candidate paths, before any other rule interactions.

This is not hypothetical — the conformance suite in this repo has a fixture that
demonstrates exactly this pathology on purpose:
`conformance/edge-cases/deep-optional-affix-nesting/grammar.xml`. It defines 12 independent,
all-optional prefix-template slots, each rule inserting the same literal character. The
fixture's own comment states the consequence directly:

> A surface word with k x's before the root therefore has C(12,k) DISTINCT valid analyses
> ... even though every analysis produces byte-identical surface text ... 2^12 slot-subset
> space since every slot is optional and each individual rule always matches.

That fixture is a deliberate pathological probe (a "complexity driver" test), not a
recommended pattern — it exists so the engine's behavior under this exact anti-pattern is
pinned and monitored, not so grammars should be written this way.

## Pattern B: one slot, many mutually-exclusive rules, non-optional — linear

The fix is to recognize that the 20 cells are not 20 independent binary toggles — they are
**one paradigmatic position** (e.g. "number+case prefix") with 20 alternative realizations,
exactly one of which applies to any given noun. Model that as:

- **One** `AffixTemplateSlot`, containing all 20 `MorphologicalRule`s.
- Each rule gated by a required inflectional feature structure (e.g. `num=sg, case=erg` vs
  `num=pl, case=abs`, ...) so the rules' domains are pairwise disjoint.
- The slot marked **non-optional** (`optional="false"`) — the noun always has *some*
  number+case value; it's never truly absent, it's just sometimes spelled with zero
  segments. Marking it optional adds a spurious extra "skip" branch for a cell that
  linguistically can't actually be skipped.

Toy example (invented `casA`/`casB`/`casC` × `sg`/`pl`, not any real language) — six
mutually-exclusive rules in one slot instead of six optional slots:

```xml
<AffixTemplate requiredPartsOfSpeech="posN">
  <Name>numberCasePrefix</Name>
  <Slot optional="false" morphologicalRules="mrSgA mrPlA mrSgB mrPlB mrSgC mrPlC">
    <Name>numCase</Name>
  </Slot>
</AffixTemplate>
```

where e.g. `mrPlB` requires `[num:pl case:casB]` and its subrule's `MorphologicalOutput` is
simply `<CopyFromInput .../>` with no `<InsertSegments>` (the null realization) — the rule
still "fires" and tags the word with `[num:pl case:casB]`, it just doesn't change the string.

With rules' feature requirements pairwise disjoint, at most one of the six unifies per
analysis → **O(6)** for that slot, not `O(2^6)`.

## If some null cells are genuinely homophonous

Suppose 3 of the 20 cells really do share an identical null exponent and are truly
indistinguishable on the surface (not just "we were lazy about features" — actually
ambiguous). Analysis of such a word correctly yields 3 candidate parses, each tagged with a
different `num`/`case` feature bundle, all with identical surface text. That is **O(3)**
residual ambiguity at that node — real linguistic ambiguity, not a performance bug. It should
be resolved (if at all) by agreement elsewhere in the clause (verb/adjective concord,
syntax-level unification) — not by trying to make the morphological parser guess.

## Complexity summary

| Grammar design | Branching per word | Why |
|---|---|---|
| n independently-optional slots, 1 rule each | `O(2ⁿ)` | every slot forks apply/skip independently |
| n mutually-exclusive rules in 1 non-optional slot | `O(n)` | one alternative fires per position, feature-disjoint |
| ...with k cells sharing one identical null exponent | `O(k)` residual ambiguity at that node | genuine ambiguity, correctly surfaced, not exponential |

## Practical takeaway

If a HermitCrab grammar has many optional slots that are individually cheap but multiply
together into unusable parse times, look first at whether those slots are actually
independent affixes (compounding is genuinely multiplicative) or are alternative fillers of
one paradigmatic position that got split into separate slots by mistake. Collapsing the
latter into one non-optional, multi-rule slot is the standard fix and changes the asymptotic
behavior, not just the constant factor.
