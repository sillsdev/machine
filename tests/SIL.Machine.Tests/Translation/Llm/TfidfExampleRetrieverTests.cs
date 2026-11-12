using NUnit.Framework;

namespace SIL.Machine.Translation.Llm;

[TestFixture]
public class TfidfExampleRetrieverTests
{
    [Test]
    public void Rank_MostSimilarSourceFirst()
    {
        var examples = new[]
        {
            new Example("the cat sat on the mat", "0"),
            new Example("completely unrelated sentence", "1"),
            new Example("a cat sat here", "2"),
        };

        Assert.That(
            new TfidfExampleRetriever(examples).Rank("the cat sat", 2),
            Is.EqualTo(new[] { examples[0], examples[2] })
        );
    }

    [Test]
    public void Rank_RespectsK()
    {
        var examples = Enumerable.Range(0, 3).Select(i => new Example("apple pie", i.ToString())).ToArray();
        Assert.That(new TfidfExampleRetriever(examples).Rank("apple pie", 2), Has.Count.EqualTo(2));
    }

    [Test]
    public void Rank_ExcludesItsOwnPosition()
    {
        var examples = new[]
        {
            new Example("the cat sat", "0"),
            new Example("the cat sat", "1"),
            new Example("totally different words", "2"),
        };

        Assert.That(
            new TfidfExampleRetriever(examples).Rank("the cat sat", 2, excludeIndex: 0),
            Is.EqualTo(new[] { examples[1], examples[2] })
        );
    }

    [Test]
    public void Rank_KeepsDuplicateCopiesAtOtherIndices()
    {
        var excluded = new Example("apple pie", "tarte");
        var retriever = new TfidfExampleRetriever(
            new[] { excluded, excluded, new Example("banana split", "banane"), new Example("cherry tart", "cerise") }
        );

        Assert.That(retriever.Rank("apple pie", 1, excludeIndex: 0), Is.EqualTo(new[] { excluded }));
    }

    [Test]
    public void Rank_KLargerThanTheCorpusReturnsEveryExample()
    {
        var examples = new[] { new Example("apple pie", "0"), new Example("banana split", "1") };
        Assert.That(new TfidfExampleRetriever(examples).Rank("apple pie", 10), Is.EqualTo(examples));
    }

    [Test]
    public void Rank_EmptyCorpus()
    {
        Assert.That(new TfidfExampleRetriever(Array.Empty<Example>()).Rank("anything", 5), Is.Empty);
    }

    [Test]
    public void Rank_CorpusWithNoWordTokens()
    {
        var examples = new[] { new Example("!!!", "0"), new Example("...", "1") };

        Assert.That(new TfidfExampleRetriever(examples).Rank("anything", 1), Is.EqualTo(new[] { examples[0] }));
        Assert.That(new TfidfExampleRetriever(examples).Rank("anything", 4), Is.EqualTo(examples));
    }

    [Test]
    public void Rank_OnlySomeSourcesHaveWords()
    {
        var examples = new[] { new Example("!!!", "0"), new Example("let there be light", "1") };
        Assert.That(new TfidfExampleRetriever(examples).Rank("light", 1), Is.EqualTo(new[] { examples[1] }));
    }

    [Test]
    public void Rank_PunctuationCarriesNoSignal()
    {
        var examples = new[]
        {
            new Example("the cat sat", "0"),
            new Example("!!! !!! !!! ???", "1"),
            new Example("something else entirely", "2"),
        };

        Assert.That(new TfidfExampleRetriever(examples).Rank("!!! !!! !!! cat", 1), Is.EqualTo(new[] { examples[0] }));
    }
}
