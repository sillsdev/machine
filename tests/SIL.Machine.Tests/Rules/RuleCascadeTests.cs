using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules;

[TestFixture]
public class RuleCascadeTests
{
    private static readonly IEqualityComparer<AnnotatedStringData> Comparer = new StringDataComparer();

    // Both rules rewrite any other string to "b", so the cascade sees "b" twice but has one distinct result.
    private static IRule<AnnotatedStringData, int>[] DuplicateProducingRules() => [new RewriteToB(), new RewriteToB()];

    [Test]
    public void ParallelCombination_MaxAlternatives_CountsDistinctResults()
    {
        var sequential = new CombinationRuleCascade<AnnotatedStringData, int>(DuplicateProducingRules(), Comparer)
        {
            MaxAlternatives = 1,
        };
        var parallel = new ParallelCombinationRuleCascade<AnnotatedStringData, int>(DuplicateProducingRules(), Comparer)
        {
            MaxAlternatives = 1,
        };

        AssertSameResults(sequential, parallel);
    }

    [Test]
    public void ParallelPermutation_MaxAlternatives_CountsDistinctResults()
    {
        var sequential = new PermutationRuleCascade<AnnotatedStringData, int>(DuplicateProducingRules(), Comparer)
        {
            MaxAlternatives = 1,
        };
        var parallel = new ParallelPermutationRuleCascade<AnnotatedStringData, int>(DuplicateProducingRules(), Comparer)
        {
            MaxAlternatives = 1,
        };

        AssertSameResults(sequential, parallel);
    }

    private static void AssertSameResults(
        RuleCascade<AnnotatedStringData, int> sequential,
        RuleCascade<AnnotatedStringData, int> parallel
    )
    {
        string[] expected = sequential.Apply(new AnnotatedStringData("a")).Select(d => d.String).ToArray();
        Assert.That(expected, Is.EqualTo(new[] { "b" }));
        Assert.That(
            parallel.Apply(new AnnotatedStringData("a")).Select(d => d.String).ToArray(),
            Is.EquivalentTo(expected)
        );
    }

    private sealed class RewriteToB : IRule<AnnotatedStringData, int>
    {
        public IEnumerable<AnnotatedStringData> Apply(AnnotatedStringData input)
        {
            if (string.Equals(input.String, "b", StringComparison.Ordinal))
                return [];
            return [new AnnotatedStringData("b")];
        }
    }

    private sealed class StringDataComparer : IEqualityComparer<AnnotatedStringData>
    {
        public bool Equals(AnnotatedStringData? x, AnnotatedStringData? y) =>
            string.Equals(x?.String, y?.String, StringComparison.Ordinal);

        public int GetHashCode(AnnotatedStringData obj) => StringComparer.Ordinal.GetHashCode(obj.String);
    }
}
