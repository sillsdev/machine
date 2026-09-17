using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceMorpherFactoryTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    [Test]
    public void DefaultMorpherIsMemoizationEligible()
    {
        Fixture fixture = Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance")).First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);

        Morpher morpher = ConformanceMorpherFactory.Create(language);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.MaxDegreeOfParallelism, Is.EqualTo(1));
            Assert.That(morpher.TraceManager.IsTracing, Is.False);
        });
    }

    [Test]
    public void DiagnosticMorpherCanDisableMemoization()
    {
        Fixture fixture = Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance")).First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);

        Morpher morpher = ConformanceMorpherFactory.Create(language, useMemoization: false);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.MaxDegreeOfParallelism, Is.EqualTo(0));
            Assert.That(morpher.TraceManager.IsTracing, Is.False);
        });
    }

    [Test]
    public void DiagnosticSelfCheckNamesDisabledMemoization()
    {
        var engine = new SelfCheckEngine(useMemoization: false);

        Assert.That(engine.Name, Is.EqualTo("self-check (memoization disabled)"));
    }

    [Test]
    public void TracingMorpherCanBeCreatedSequentially()
    {
        Fixture fixture = Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance")).First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);

        Morpher morpher = ConformanceMorpherFactory.CreateTracing(language);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.MaxDegreeOfParallelism, Is.EqualTo(1));
            Assert.That(morpher.TraceManager.IsTracing, Is.True);
        });
    }
}
