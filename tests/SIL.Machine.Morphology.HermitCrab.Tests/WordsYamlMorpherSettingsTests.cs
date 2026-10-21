using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class WordsYamlMorpherSettingsTests
{
    private const string Words =
        "words:\n  - word: kat\n    note: n\n    parses:\n      - signature: \"KAT|kat\"\n        rules: []\n";

    private static WordsYaml Load(string frontMatter)
    {
        string path = Path.Combine(Path.GetTempPath(), "words-" + Guid.NewGuid().ToString("N") + ".yaml");
        File.WriteAllText(path, "language: T\n" + frontMatter + Words);
        try
        {
            return WordsYamlLoader.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void AbsentBlockMeansTheDefaults()
    {
        WordsYaml words = Load("");
        Assert.That(words.Morpher.IsEmpty, Is.True);
    }

    [Test]
    public void EveryAcceptedKeyIsRead()
    {
        WordsYaml words = Load(
            "morpher:\n  DelReapps: 1\n  MaxRoots: 3\n  MergeAnalyses: false\n  MakePartialsFinal: true\n"
        );
        Assert.Multiple(() =>
        {
            Assert.That(words.Morpher.DelReapps, Is.EqualTo(1));
            Assert.That(words.Morpher.MaxRoots, Is.EqualTo(3));
            Assert.That(words.Morpher.MergeAnalyses, Is.False);
            Assert.That(words.Morpher.MakePartialsFinal, Is.True);
        });
    }

    [TestCase("morpher:\n  GuessRoots: true\n", "unknown key 'GuessRoots'")]
    [TestCase("morpher:\n  MaxAlternatives: 5\n", "unknown key 'MaxAlternatives'")]
    [TestCase("morpher:\n  DelReapps: one\n", "DelReapps")]
    [TestCase("morpher:\n  DelReapps: -1\n", "DelReapps")]
    [TestCase("morpher:\n  MaxRoots: 0\n", "MaxRoots")]
    [TestCase("morpher:\n  MergeAnalyses: yes\n", "expected 'true' or 'false'")]
    [TestCase("morpher: {}\n", "empty block")]
    [TestCase("morpher: 1\n", "expected a mapping")]
    public void AnUnappliableBlockIsAnError(string frontMatter, string expected)
    {
        var ex = Assert.Throws<WordsYamlException>(() => Load(frontMatter));
        Assert.That(ex!.Message, Does.Contain(expected));
    }

    [Test]
    public void TheFactoryAppliesEveryDeclaredValueAndOnlyThose()
    {
        Fixture fixture = Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance")).First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);
        var settings = new MorpherSettings { DelReapps = 2, MakePartialsFinal = true };

        Morpher morpher = ConformanceMorpherFactory.Create(language, settings);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.DeletionReapplications, Is.EqualTo(2));
            Assert.That(morpher.AlwaysEnforceFinalTemplates, Is.True);
            Assert.That(morpher.MaxStemCount, Is.EqualTo(2), "undeclared: constructor default");
            Assert.That(morpher.MergeEquivalentAnalyses, Is.True, "undeclared: constructor default");
        });
    }

    [Test]
    public void ABlockIsACapabilityRequirement()
    {
        Fixture fixture = Fixture
            .DiscoverAll(Path.Combine(RepositoryRoot(), "conformance"))
            .Single(f => f.Id == "edge-cases/deletion-reapplications");

        Assert.That(
            RequiresDerivation.Derive(fixture.GrammarPath, fixture.Words.Morpher),
            Is.EqualTo(new[] { "phonology", MorpherSettings.Capability })
        );
        Assert.That(
            RequiresDerivation.Derive(fixture.GrammarPath, MorpherSettings.Defaults),
            Is.EqualTo(new[] { "phonology" })
        );
    }

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
}
