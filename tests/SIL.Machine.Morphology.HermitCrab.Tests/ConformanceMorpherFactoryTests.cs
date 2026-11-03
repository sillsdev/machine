using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceMorpherFactoryTests
{
    [Test]
    public void DefaultMorpherIsSequentialAndUntraced()
    {
        Fixture fixture = Fixture.DiscoverAll(ConformanceSuite.Root()).First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);

        Morpher morpher = ConformanceMorpherFactory.Create(language, fixture.Words.Morpher);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.MaxDegreeOfParallelism, Is.EqualTo(1));
            Assert.That(morpher.TraceManager.IsTracing, Is.False);
        });
    }

    [Test]
    public void TracingMorpherCanBeCreatedSequentially()
    {
        Fixture fixture = Fixture.DiscoverAll(ConformanceSuite.Root()).First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);

        Morpher morpher = ConformanceMorpherFactory.CreateTracing(language, fixture.Words.Morpher);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.MaxDegreeOfParallelism, Is.EqualTo(1));
            Assert.That(morpher.TraceManager.IsTracing, Is.True);
        });
    }
}
