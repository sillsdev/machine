using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

// Stage 3 of memoization.md: MemoizedCombinationRuleCascade exercised directly, bypassing
// Morpher/AnalysisStratumRule entirely, so the test can force the exact commuting-order redundancy the
// memo targets without depending on whether a particular morphology grammar's search happens to revisit
// a PRODUCTIVE (not just nogood) state -- MorpherTests' end-to-end compounding grammar, checked via the
// real Morpher pipeline, only ever hits the nogood table on this small a grammar (see its own hit-count
// diagnostic output), so this is the test that pins the POSITIVE replay path non-vacuously.
[TestFixture]
public class MemoizedCombinationRuleCascadeTests : HermitCrabTestBase
{
    [Test]
    public void Apply_ReplaysPositiveHit_WhenTwoOrdersReachTheSameKey()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };
        var ruleB = new AffixProcessRule { Name = "ruleB" };
        var ruleC = new AffixProcessRule { Name = "ruleC" };

        // Each fake rule unapplies its own IMorphologicalRule at most once, so from the initial word,
        // trying ruleA-then-ruleB-then-ruleC and ruleB-then-ruleA-then-ruleC reach the SAME
        // AnalysisStateKey after the first two steps (multiset {ruleA:1, ruleB:1}) via different orders
        // -- exactly the redundancy AnalysisStateKey collapses. Only from that shared state can ruleC
        // still apply, so the shared node's own subtree is POSITIVE (one result), not a nogood.
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
        initial.AnalysisScope = new AnalysisScope();
        initial.Freeze();

        long hitsBefore = MemoizedCombinationRuleCascade.DiagMemoHits;
        List<Word> results = new List<Word>(cascade.Apply(initial));

        // Every leaf where all 3 rules have been unapplied, in whichever of the two orders explored the
        // shared {ruleA,ruleB} state first vs via replay, must appear -- and the replay path must have
        // actually fired (memoization.md's non-vacuousness requirement).
        Assert.That(
            results,
            Has.Some.Matches<Word>(w =>
                w.GetUnapplicationCount(ruleA) == 1
                && w.GetUnapplicationCount(ruleB) == 1
                && w.GetUnapplicationCount(ruleC) == 1
            )
        );
        Assert.That(
            MemoizedCombinationRuleCascade.DiagMemoHits,
            Is.GreaterThan(hitsBefore),
            "this test's whole point is to force a positive replay -- it must not go vacuous"
        );
    }

    [Test]
    public void Apply_PositiveReplayMatchesUnmemoizedResultSet_IncludingTrailOrder()
    {
        // The previous test proves a replay FIRES; this one proves it produces the RIGHT result. A count
        // assertion (3 rules applied) would pass even if ReplayOnto grafted the wrong prefix, since counts
        // are order-invariant -- comparing MorphemesInApplicationOrder (which walks the trail ReplayOnto
        // rewrites, see WordAnalysisSignature's own doc comment in MorpherTests.cs) catches a wrong-prefix
        // graft that silently collapses [ruleB,ruleA,ruleC] into a duplicate of [ruleA,ruleB,ruleC].
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
        memoized.AnalysisScope = new AnalysisScope();
        memoized.Freeze();

        Word unmemoized = NewTestWord();
        // AnalysisScope left null -- exercises MemoizedCombinationRuleCascade's own unmemoized fallback
        // (ApplyRulesRaw), the same path a tracing parse takes.
        unmemoized.Freeze();

        var memoizedCascade = new MemoizedCombinationRuleCascade(rules, FreezableEqualityComparer<Word>.Default);
        var unmemoizedCascade = new MemoizedCombinationRuleCascade(rules, FreezableEqualityComparer<Word>.Default);

        long hitsBefore = MemoizedCombinationRuleCascade.DiagMemoHits;
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
            MemoizedCombinationRuleCascade.DiagMemoHits,
            Is.GreaterThan(hitsBefore),
            "this test's whole point is to compare a real replay against the unmemoized result -- it "
                + "must not go vacuous"
        );
    }

    [Test]
    public void Apply_FallsBackToUnmemoizedExpansion_WhenKeyIsAlreadyInProgress()
    {
        // Pins the in-flight re-entry guard (memoization.md's design rule 3 / the InProgress table):
        // a multiApp cascade can reach the same AnalysisStateKey again while its own first expansion is
        // still on the call stack (e.g. via a self-loop elsewhere in a real grammar's rule graph).
        // Rather than construct that reentrancy organically -- the key is monotonic in rule-application
        // count for straightforward single-use rules, so a real cyclic re-arrival is hard to force here
        // -- this simulates the in-flight state directly: pre-populate InProgress with the exact key
        // `initial` will compute, then call Apply and confirm it falls through to a correct, unmemoized
        // expansion (ApplyRulesRaw) instead of reading a nonexistent Memo entry or hanging.
        var ruleA = new AffixProcessRule { Id = "RULE_A", Name = "ruleA" };
        var cascade = new MemoizedCombinationRuleCascade(
            new IRule<Word, ShapeNode>[] { new SingleUseUnapplyRule(ruleA) },
            FreezableEqualityComparer<Word>.Default
        );

        Word initial = NewTestWord();
        var scope = new AnalysisScope();
        initial.AnalysisScope = scope;
        initial.Freeze();

        var key = new AnalysisStateKey(initial);
        scope.InProgress.TryAdd(key, 0);

        long hitsBefore = MemoizedCombinationRuleCascade.DiagMemoHits;
        List<Word> results = new List<Word>(cascade.Apply(initial));

        Assert.That(results, Has.Some.Matches<Word>(w => w.GetUnapplicationCount(ruleA) == 1));
        Assert.That(
            MemoizedCombinationRuleCascade.DiagMemoHits,
            Is.EqualTo(hitsBefore),
            "the in-flight fallback must not read/count a memo hit -- it never consults Memo at all"
        );
        Assert.That(
            scope.Memo.ContainsKey(key),
            Is.False,
            "the in-flight arrival's OWN key must never be written to Memo (deeper recursive calls for "
                + "OTHER keys, reached via ApplyRulesRaw's normal recursion, may still memoize themselves)"
        );
    }

    private static string TrailSignature(Word word) =>
        string.Join("+", word.MorphemesInApplicationOrder.Select(m => m.Id));

    private Word NewTestWord()
    {
        return new Word(Entries["32"].PrimaryAllomorph, FeatureStruct.New().Value) { Stratum = Morphophonemic };
    }

    // Minimal stand-in for a compiled analysis morphological rule: unapplies `_rule` against the input
    // exactly once (per input), independent of Shape/FeatureStruct pattern matching, so the cascade's
    // own commuting-order redundancy can be exercised without compiling a real FST-backed rule.
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
