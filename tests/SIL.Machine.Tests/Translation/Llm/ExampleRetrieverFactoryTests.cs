using NUnit.Framework;

namespace SIL.Machine.Translation.Llm;

[TestFixture]
public class ExampleRetrieverFactoryTests
{
    [Test]
    public void CreateExampleRetriever_Tfidf()
    {
        var examples = new[] { new Example("a", "0"), new Example("b", "1") };
        var retriever = ExampleRetrieverFactory.CreateExampleRetriever(ExampleRetrievalMethod.Tfidf, examples);
        Assert.That(retriever, Is.TypeOf<TfidfExampleRetriever>());
        Assert.That(retriever.Examples, Is.EqualTo(examples));
    }

    [Test]
    public void CreateExampleRetriever_Coverage()
    {
        var examples = new[] { new Example("a", "0"), new Example("b", "1") };
        var retriever = ExampleRetrieverFactory.CreateExampleRetriever(ExampleRetrievalMethod.Coverage, examples);
        Assert.That(retriever, Is.TypeOf<CoverageExampleRetriever>());
        Assert.That(retriever.Examples, Is.EqualTo(examples));
    }

    [Test]
    public void CreateExampleRetriever_UnknownMethod()
    {
        Assert.Throws<ArgumentException>(() =>
            ExampleRetrieverFactory.CreateExampleRetriever((ExampleRetrievalMethod)99, Array.Empty<Example>())
        );
    }
}
