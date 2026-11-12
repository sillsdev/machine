using NUnit.Framework;

namespace SIL.Machine.Translation.Llm;

[TestFixture]
public class CoverageExampleRetrieverTests
{
    private static CoverageExampleRetriever Create(params string[] sources) =>
        new CoverageExampleRetriever(sources.Select(s => new Example(s, "")));

    [Test]
    public void Rank_PrefersAnUncoveredClauseToANearDuplicate()
    {
        string[] sources =
        {
            "the shepherd counted his sheep in the green field",
            "the shepherd counted his sheep in the green field again",
            "the shepherd counted all his sheep in the green field",
            "fishermen mended nets",
            "a woman drew water from the well",
        };
        string query = "the shepherd counted his sheep in the green field while fishermen mended nets";

        Assert.That(Create(sources).Rank(query, 2).Select(e => e.Source), Is.EqualTo(new[] { sources[0], sources[3] }));
    }

    [Test]
    public void Rank_KeepsTheClosestExample()
    {
        string[] sources = { "the shepherd counted his sheep in the green field", "fishermen mended nets" };
        string query = "the shepherd counted his sheep in the green field while fishermen mended nets";

        Assert.That(Create(sources).Rank(query, 1).Select(e => e.Source), Is.EqualTo(new[] { sources[0] }));
    }

    [Test]
    public void Rank_SelectsAnExactDuplicatePairOnlyOnce()
    {
        var examples = new[]
        {
            new Example("apple pie", "tarte"),
            new Example("apple pie", "tarte"),
            new Example("banana split", "banane"),
        };
        var retriever = new CoverageExampleRetriever(examples);

        var selected = retriever.Rank("apple pie", 2);
        Assert.That(selected, Does.Contain(examples[2]));
        Assert.That(selected.Count(e => e.Equals(examples[0])), Is.EqualTo(1));

        selected = retriever.Rank("apple pie", 3);
        Assert.That(selected.Count(e => e.Equals(examples[0])), Is.EqualTo(2));
    }

    [Test]
    public void Rank_KeepsRepeatedSourcesWithDifferentTargets()
    {
        var examples = new[]
        {
            new Example("apple pie", "tarte"),
            new Example("apple pie", "tourte"),
            new Example("banana split", "banane"),
        };

        Assert.That(new CoverageExampleRetriever(examples).Rank("apple pie", 2), Is.EquivalentTo(examples.Take(2)));
    }

    [Test]
    public void Rank_KeepsACopyAtADifferentIndex()
    {
        var examples = new[]
        {
            new Example("apple pie", "tarte"),
            new Example("apple pie", "tarte"),
            new Example("banana split", "banane"),
        };

        Assert.That(
            new CoverageExampleRetriever(examples).Rank("apple pie", 1, excludeIndex: 0),
            Is.EqualTo(new[] { examples[1] })
        );
    }

    [Test]
    public void Rank_ReturnsACopyWhenTheFirstPositionIsExcluded()
    {
        var examples = new[] { new Example("apple pie", "tarte"), new Example("apple pie", "tarte") };

        Assert.That(
            new CoverageExampleRetriever(examples).Rank("apple pie", 1, excludeIndex: 0),
            Is.EqualTo(new[] { examples[1] })
        );
    }

    [Test]
    public void Rank_MatchesAScriptWithoutSpaces()
    {
        string[] sources = { "他们在家里吃饭", "我们去学校", "明天下雨" };

        Assert.That(Create(sources).Rank("我们明天去学校吗", 1)[0].Source, Is.EqualTo(sources[1]));
    }

    [Test]
    public void Rank_FillsKWhenTheQueryHasNoKnownNgrams()
    {
        var retriever = Create("apple pie", "banana split", "cherry tart");
        Assert.That(retriever.Rank("zzzz", 2).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void Rank_CorpusWithNoText()
    {
        Assert.That(Create("", "  ").Rank("anything", 1), Is.Empty);
    }
}
