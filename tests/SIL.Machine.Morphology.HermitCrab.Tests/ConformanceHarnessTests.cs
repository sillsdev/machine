using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceHarnessTests
{
    private sealed class AlwaysCrashingEngine(IEnumerable<string> capabilities) : IEngine
    {
        public string Name => "always-crashing";
        public IReadOnlySet<string> Capabilities { get; } = new HashSet<string>(capabilities, StringComparer.Ordinal);

        public List<TsvRow> Run(MaterializedFixture fixture) =>
            throw new EngineCrashException($"stub crash for '{fixture.Id}'");
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

    [Test]
    public void RunAdapter_ExpectCrashFixture_FailsWhenTheEngineAlsoCrashesOnAnOrdinaryFixture()
    {
        List<Fixture> all = Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance"));
        Fixture crash = all.First(fixture => fixture.Words.ExpectCrash);
        Fixture ordinary = all.First(fixture => !fixture.Words.ExpectCrash && fixture.Words.BudgetMs is null);
        var engine = new AlwaysCrashingEngine(crash.Words.Requires.Concat(ordinary.Words.Requires));

        RunReport report = Runner.RunAdapter(new List<Fixture> { crash, ordinary }, engine, includePathological: true);

        FixtureResult crashResult = report.Results.Single(result => result.FixtureId == crash.Id);
        Assert.That(crashResult.Outcome, Is.EqualTo(FixtureOutcome.Failed), crashResult.Reason);
        Assert.That(crashResult.Reason, Does.Contain(ordinary.Id));
    }

    [Test]
    public void WordsYamlLoader_NonNumericBudget_ThrowsWordsYamlException()
    {
        string path = Path.Combine(Path.GetTempPath(), $"words-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, "language: Probe\nbudget_ms: 5s\nwords: []\n");
        try
        {
            WordsYamlException ex = Assert.Throws<WordsYamlException>(() => WordsYamlLoader.Load(path))!;
            Assert.That(ex.Message, Does.Contain("budget_ms"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void GrammarRuleIndex_TwoRulesWithTheSameName_Throws()
    {
        string path = Path.Combine(Path.GetTempPath(), $"grammar-{Guid.NewGuid():N}.xml");
        File.WriteAllText(
            path,
            """
            <HermitCrabInput>
              <PhonologicalRule id="pr1"><Name>Epenthesis</Name></PhonologicalRule>
              <PhonologicalRule id="pr2"><Name>Epenthesis</Name></PhonologicalRule>
            </HermitCrabInput>
            """
        );
        try
        {
            InvalidDataException ex = Assert.Throws<InvalidDataException>(() => GrammarRuleIndex.Load(path))!;
            Assert.That(ex.Message, Does.Contain("pr1").And.Contain("pr2"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
