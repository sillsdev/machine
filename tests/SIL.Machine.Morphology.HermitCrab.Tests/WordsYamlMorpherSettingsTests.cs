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
        if (
            !frontMatter.Contains("requires:", StringComparison.Ordinal)
            && (
                frontMatter.Contains("morpher:", StringComparison.Ordinal)
                || frontMatter.Contains("variants:", StringComparison.Ordinal)
            )
        )
        {
            frontMatter = "requires: [morpher-settings]\n" + frontMatter;
        }
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
            "morpher:\n  DelReapps: 1\n  MaxRoots: 3\n  MergeAnalyses: false\n  MakePartialsFinal: true\n  GuessRoots: false\n"
        );
        Assert.Multiple(() =>
        {
            Assert.That(words.Morpher.DelReapps, Is.EqualTo(1));
            Assert.That(words.Morpher.MaxRoots, Is.EqualTo(3));
            Assert.That(words.Morpher.MergeAnalyses, Is.False);
            Assert.That(words.Morpher.MakePartialsFinal, Is.True);
            Assert.That(words.Morpher.GuessRoots, Is.False);
            Assert.That(words.Morpher.GuessRoots, Is.EqualTo(false));
        });
    }

    [TestCase("morpher:\n  MaxAlternatives: 5\n", "unknown key 'MaxAlternatives'")]
    [TestCase("morpher:\n  DelReapps: one\n", "DelReapps")]
    [TestCase("morpher:\n  DelReapps: -1\n", "DelReapps")]
    [TestCase("morpher:\n  DelReapps: 2147483648\n", "DelReapps")]
    [TestCase("morpher:\n  MaxRoots: 0\n", "MaxRoots")]
    [TestCase("morpher:\n  MaxRoots: 2147483648\n", "MaxRoots")]
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
        Fixture fixture = Fixture.DiscoverAll(ConformanceSuite.Root()).First();
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
    public void VariantsAreFullRunsAndRequireTheSameOrderedWords()
    {
        WordsYaml words = Load(
            "morpher:\n  DelReapps: 1\nvariants:\n  - name: roots-two\n    morpher:\n      MaxRoots: 2\n    words:\n      - word: kat\n        note: n\n        parses:\n          - signature: KAT|kat\n            rules: []\n"
        );
        Assert.Multiple(() =>
        {
            Assert.That(words.Runs.Select(run => run.Name), Is.EqualTo(new[] { "default", "roots-two" }));
            Assert.That(words.Variants.Single().Morpher.MaxRoots, Is.EqualTo(2));
            Assert.That(
                words.Variants.Single().Morpher.DelReapps,
                Is.Null,
                "variant settings start from engine defaults"
            );
            Assert.That(words.Variants.Single().Words.Single().Note, Is.EqualTo("n"));
        });
    }

    [TestCase("default", "default is reserved")]
    [TestCase("BadName", "must match")]
    public void InvalidVariantNamesAreRejected(string name, string expected)
    {
        string yaml =
            $"requires: [morpher-settings]\nvariants:\n  - name: {name}\n    morpher:\n      MaxRoots: 2\n    words:\n      - word: kat\n        note: n\n        parses:\n          - signature: KAT|kat\n            rules: []\n";
        var ex = Assert.Throws<WordsYamlException>(() => Load(yaml));
        Assert.That(ex!.Message, Does.Contain(expected));
    }

    [Test]
    public void DuplicateVariantNamesAreRejected()
    {
        const string VariantBody =
            "    morpher:\n      MaxRoots: 2\n    words:\n      - word: kat\n        note: n\n        parses:\n          - signature: KAT|kat\n            rules: []\n";
        string yaml =
            "requires: [morpher-settings]\nvariants:\n  - name: same\n"
            + VariantBody
            + "  - name: same\n"
            + VariantBody;
        var ex = Assert.Throws<WordsYamlException>(() => Load(yaml));
        Assert.That(ex!.Message, Does.Contain("duplicate variant name"));
    }

    [Test]
    public void VariantWordOrderMustMatchDefault()
    {
        string yaml =
            "requires: [morpher-settings]\nvariants:\n  - name: changed\n    morpher:\n      MaxRoots: 2\n    words:\n      - word: other\n        note: n\n        parses:\n          - signature: OTHER|other\n            rules: []\n";
        var ex = Assert.Throws<WordsYamlException>(() => Load(yaml));
        Assert.That(ex!.Message, Does.Contain("same order"));
    }

    [Test]
    public void SelfCheckRunsVariantsAndFailsAWrongVariantExpectationByName()
    {
        Fixture fixture = Fixture
            .DiscoverAll(ConformanceSuite.Root())
            .Single(f => f.Id == "edge-cases/deletion-reapplications");
        WordRun variant = fixture.Words.Variants.Single(v => v.Name == "del-reapps-two");
        variant.Words[0].Parses[0].Signature = "deliberately-wrong";

        RunReport report = Runner.RunSelfCheck(
            new List<Fixture> { fixture },
            includePathological: true,
            new HashSet<string> { "phonology", MorpherSettings.Capability },
            propose: false,
            TextWriter.Null
        );

        FixtureResult failed = report.Results.Single(r =>
            r.FixtureId.Contains("variant del-reapps-two", StringComparison.Ordinal)
        );
        Assert.That(failed.Outcome, Is.EqualTo(FixtureOutcome.Failed));
        Assert.That(failed.Reason, Does.Contain("mismatched"));
    }

    [Test]
    public void ABlockIsACapabilityRequirement()
    {
        Fixture fixture = Fixture
            .DiscoverAll(ConformanceSuite.Root())
            .Single(f => f.Id == "edge-cases/deletion-reapplications");

        Assert.That(
            RequiresDerivation.Derive(fixture.GrammarPath, fixture.Words.Morpher),
            Is.EqualTo(new[] { "phonology", MorpherSettings.Capability })
        );
        Assert.That(
            RequiresDerivation.Derive(fixture.GrammarPath, MorpherSettings.Defaults),
            Is.EqualTo(new[] { "phonology" })
        );
        Assert.That(
            RequiresDerivation.Derive(
                fixture.GrammarPath,
                new[]
                {
                    MorpherSettings.Defaults,
                    new MorpherSettings { MaxRoots = 2 },
                }
            ),
            Is.EqualTo(new[] { "phonology", MorpherSettings.Capability })
        );
    }
}
