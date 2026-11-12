using NUnit.Framework;

namespace SIL.Machine.Translation.Llm;

[TestFixture]
public class ExampleRetrieverTests
{
    [Test]
    public void Rank_KGreaterThanN()
    {
        var examples = new[] { new Example("a", "1"), new Example("b", "2") };
        var retriever = new MockExampleRetriever(examples);
        Assert.That(retriever.Rank("query", 3), Is.EqualTo(examples));
    }

    [Test]
    public void Rank_NoSuchExclude()
    {
        var examples = new[] { new Example("a", "1"), new Example("b", "2") };
        var retriever = new MockExampleRetriever(examples);
        Assert.That(retriever.Rank("query", 2, excludeIndex: 10), Is.EqualTo(examples));
    }

    [Test]
    public void Rank_ExcludesOnlyTheSpecifiedIndex()
    {
        var duplicate = new Example("a", "1");
        var examples = new[] { duplicate, new Example("b", "2"), duplicate };
        var retriever = new MockExampleRetriever(examples);
        Assert.That(retriever.Rank("query", 3, excludeIndex: 0), Is.EqualTo(examples.Skip(1)));
    }

    [Test]
    public void Rank_NonPositiveK()
    {
        var retriever = new MockExampleRetriever(new[] { new Example("a", "1") });
        Assert.That(retriever.Rank("query", 0), Is.Empty);
    }

    [Test]
    public void Examples_ReturnsACopy()
    {
        var examples = new[] { new Example("a", "1"), new Example("b", "2") };
        var retriever = new MockExampleRetriever(examples);

        ((List<Example>)retriever.Examples).Clear();

        Assert.That(retriever.Examples, Is.EqualTo(examples));
    }
}
