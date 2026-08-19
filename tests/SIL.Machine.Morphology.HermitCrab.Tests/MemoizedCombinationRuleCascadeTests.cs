using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

// The cascade exercised directly, bypassing Morpher, so a commuting-order re-arrival at a PRODUCTIVE
// state can be forced. That matters because the end-to-end grammars in MorpherTests are small enough
// that they only ever reach the nogood table, leaving the positive replay path untested.
[TestFixture]
public class MemoizedCombinationRuleCascadeTests : HermitCrabTestBase
{
    [Test]
    public void Apply_ReplaysPositiveHit_WhenTwoOrdersReachTheSameKey()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };
        var ruleB = new AffixProcessRule { Name = "ruleB" };
        var ruleC = new AffixProcessRule { Name = "ruleC" };

        // Each rule unapplies at most once, so A-then-B and B-then-A reach the same key (multiset
        // {ruleA:1, ruleB:1}) by different routes. ruleC can still apply from that shared state, making
        // its subtree positive rather than a nogood.
        var cascade = new MemoizedCombinationRuleCascade(
            new IRule<Word, ShapeNode>[]
            {
                new SingleUseUnapplyRule(ruleA),
                new SingleUseUnapplyRule(ruleB),
                new SingleUseUnapplyRule(ruleC),
            },
            FreezableEqualityComparer<Word>.Default
        );

        Word initial = NewTestWord();
        var scope = new AnalysisScope();
        initial.AnalysisScope = scope;
        initial.Freeze();

        List<Word> results = new List<Word>(cascade.Apply(initial));

        Assert.That(
            results,
            Has.Some.Matches<Word>(w =>
                w.GetUnapplicationCount(ruleA) == 1
                && w.GetUnapplicationCount(ruleB) == 1
                && w.GetUnapplicationCount(ruleC) == 1
            )
        );
        Assert.That(
            scope.MemoHits,
            Is.GreaterThan(0),
            "this test's whole point is to force a positive replay -- it must not go vacuous"
        );
    }

    [Test]
    public void Apply_PositiveReplayMatchesUnmemoizedResultSet_IncludingTrailOrder()
    {
        // Compares MorphemesInApplicationOrder rather than rule counts: counts are order-invariant, so
        // they would pass even if the graft collapsed [ruleB,ruleA,ruleC] into a duplicate of
        // [ruleA,ruleB,ruleC], whereas the trail is exactly what ReplayOnto rewrites.
        var ruleA = new AffixProcessRule { Id = "RULE_A", Name = "ruleA" };
        var ruleB = new AffixProcessRule { Id = "RULE_B", Name = "ruleB" };
        var ruleC = new AffixProcessRule { Id = "RULE_C", Name = "ruleC" };
        IRule<Word, ShapeNode>[] rules =
        {
            new SingleUseUnapplyRule(ruleA),
            new SingleUseUnapplyRule(ruleB),
            new SingleUseUnapplyRule(ruleC),
        };

        Word memoized = NewTestWord();
        var scope = new AnalysisScope();
        memoized.AnalysisScope = scope;
        memoized.Freeze();

        // No AnalysisScope: takes the unmemoized fallback, the same path a tracing parse takes.
        Word unmemoized = NewTestWord();
        unmemoized.Freeze();

        var memoizedCascade = new MemoizedCombinationRuleCascade(rules, FreezableEqualityComparer<Word>.Default);
        var unmemoizedCascade = new MemoizedCombinationRuleCascade(rules, FreezableEqualityComparer<Word>.Default);

        List<string> memoizedSignatures = memoizedCascade
            .Apply(memoized)
            .Select(TrailSignature)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        List<string> unmemoizedSignatures = unmemoizedCascade
            .Apply(unmemoized)
            .Select(TrailSignature)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Assert.That(
            memoizedSignatures,
            Is.EqualTo(unmemoizedSignatures),
            "a positive replay must reproduce exactly the unmemoized result set, INCLUDING trail order"
        );
        Assert.That(
            scope.MemoHits,
            Is.GreaterThan(0),
            "this test's whole point is to compare a real replay against the unmemoized result -- it "
                + "must not go vacuous"
        );
    }

    [Test]
    public void Apply_FallsBackToUnmemoizedExpansion_WhenKeyIsAlreadyInProgress()
    {
        // The in-flight state is simulated by pre-populating InProgress, because single-use rules make the
        // key monotonic in application count, so a genuine cyclic re-arrival cannot be forced here.
        var ruleA = new AffixProcessRule { Id = "RULE_A", Name = "ruleA" };
        var cascade = new MemoizedCombinationRuleCascade(
            new IRule<Word, ShapeNode>[] { new SingleUseUnapplyRule(ruleA) },
            FreezableEqualityComparer<Word>.Default
        );

        Word initial = NewTestWord();
        var scope = new AnalysisScope();
        initial.AnalysisScope = scope;
        initial.Freeze();

        var key = AnalysisStateKey.PinAndKey(initial);
        scope.InProgress.Add(key);

        List<Word> results = new List<Word>(cascade.Apply(initial));

        Assert.That(results, Has.Some.Matches<Word>(w => w.GetUnapplicationCount(ruleA) == 1));
        Assert.That(
            scope.MemoHits,
            Is.Zero,
            "the in-flight fallback must not read/count a memo hit -- it never consults Memo at all"
        );
        Assert.That(
            scope.Memo.ContainsKey(key),
            Is.False,
            "the in-flight arrival's OWN key must never be written to Memo (deeper recursive calls for "
                + "OTHER keys, reached via ApplyRulesRaw's normal recursion, may still memoize themselves)"
        );
    }

    [Test]
    public void Apply_EnforcesMaxAlternatives_ForRawAndReplayPaths()
    {
        var ruleA = new AffixProcessRule { Id = "RULE_A", Name = "ruleA" };
        var ruleB = new AffixProcessRule { Id = "RULE_B", Name = "ruleB" };
        IRule<Word, ShapeNode>[] rules = { new SingleUseUnapplyRule(ruleA), new SingleUseUnapplyRule(ruleB) };

        var rawCascade = new MemoizedCombinationRuleCascade(rules, FreezableEqualityComparer<Word>.Default)
        {
            MaxAlternatives = 1,
        };
        Word rawInitial = NewTestWord();
        rawInitial.AnalysisScope = new AnalysisScope();
        rawInitial.Freeze();

        Assert.Throws<MaxAlternativesExceededException>(() => new List<Word>(rawCascade.Apply(rawInitial)));

        var replayCascade = new MemoizedCombinationRuleCascade(rules, FreezableEqualityComparer<Word>.Default);
        Word replayInitial = NewTestWord();
        replayInitial.AnalysisScope = new AnalysisScope();
        replayInitial.Freeze();
        _ = new List<Word>(replayCascade.Apply(replayInitial));

        replayCascade.MaxAlternatives = 1;
        Assert.Throws<MaxAlternativesExceededException>(() => new List<Word>(replayCascade.Apply(replayInitial)));
    }

    private static string TrailSignature(Word word) =>
        string.Join("+", word.MorphemesInApplicationOrder.Select(m => m.Id));

    private Word NewTestWord()
    {
        return new Word(Entries["32"].PrimaryAllomorph, FeatureStruct.New().Value) { Stratum = Morphophonemic };
    }

    // Stand-in for a compiled analysis rule: unapplies once per input, with no Shape/FeatureStruct
    // matching, so commuting orders can be exercised without a real FST-backed rule.
    private sealed class SingleUseUnapplyRule(IMorphologicalRule rule) : IRule<Word, ShapeNode>
    {
        private readonly IMorphologicalRule _rule = rule;

        public IEnumerable<Word> Apply(Word input)
        {
            if (input.GetUnapplicationCount(_rule) > 0)
                yield break;

            Word result = input.Clone();
            result.MorphologicalRuleUnapplied(_rule);
            result.Freeze();
            yield return result;
        }
    }
}
