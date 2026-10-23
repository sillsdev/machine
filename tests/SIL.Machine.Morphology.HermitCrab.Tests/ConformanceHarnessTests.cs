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

    // A copy of the slow fixture with a 1 ms budget, so the self-check watchdog fires after its 1 s margin while
    // the fast first word has already finished and the 924-parse second word has not.
    private static FixtureResult RunSlowFixtureOverBudget(string extraFrontMatter)
    {
        string root = Path.Combine(Path.GetTempPath(), $"slow-fixture-{Guid.NewGuid():N}");
        string source = Path.Combine(RepositoryRoot(), "conformance", "edge-cases", "deep-optional-affix-nesting");
        string target = Path.Combine(root, "edge-cases", "deep-optional-affix-nesting");
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        string wordsPath = Path.Combine(target, "words.yaml");
        string words = File.ReadAllText(wordsPath);
        Assert.That(words, Does.Contain("budget_ms: 15000"));
        File.WriteAllText(wordsPath, words.Replace("budget_ms: 15000", "budget_ms: 1" + extraFrontMatter));
        try
        {
            RunReport report = Runner.RunSelfCheck(
                Fixture.DiscoverAll(root),
                includePathological: true,
                engineCapabilities: new HashSet<string>(StringComparer.Ordinal),
                propose: false,
                proposeOutput: TextWriter.Null
            );
            return report.Results.Single();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void CompareRuleTraceMultisets_SameSignatureWithDistinctTraces_PassesRegardlessOfOrder()
    {
        const string Signature = "DAK+IMO|dakimo";
        var expected = new (string Signature, IEnumerable<string> RuleIds)[]
        {
            (Signature, new[] { "crLeftHead", "crShared" }),
            (Signature, new[] { "crRightHead" }),
        };
        var actual = new (string Signature, IEnumerable<string> RuleIds)[]
        {
            (Signature, new[] { "crRightHead" }),
            (Signature, new[] { "crShared", "crLeftHead" }),
        };

        Assert.That(Runner.CompareRuleTraceMultisets(expected, actual), Is.Empty);
    }

    [Test]
    public void CompareRuleTraceMultisets_WrongTraceForSameSignature_Fails()
    {
        const string Signature = "DAK+IMO|dakimo";
        var expected = new (string Signature, IEnumerable<string> RuleIds)[]
        {
            (Signature, new[] { "crLeftHead" }),
        };
        var actual = new (string Signature, IEnumerable<string> RuleIds)[]
        {
            (Signature, new[] { "crWrongHead" }),
        };

        List<string> details = Runner.CompareRuleTraceMultisets(expected, actual);

        Assert.That(details, Has.Count.EqualTo(2));
        Assert.That(details, Has.Some.Contains("declared rules [crLeftHead] count 1 != traced count 0"));
        Assert.That(details, Has.Some.Contains("declared rules [crWrongHead] count 0 != traced count 1"));
    }

    [Test]
    public void CompareRuleTraceMultisets_DuplicateTraceMultiplicityMustMatch()
    {
        const string Signature = "DAK+IMO|dakimo";
        var expected = new (string Signature, IEnumerable<string> RuleIds)[]
        {
            (Signature, new[] { "crLeftHead" }),
            (Signature, new[] { "crLeftHead" }),
        };
        var actual = new (string Signature, IEnumerable<string> RuleIds)[]
        {
            (Signature, new[] { "crLeftHead" }),
            (Signature, new[] { "crRightHead" }),
        };

        List<string> details = Runner.CompareRuleTraceMultisets(expected, actual);

        Assert.That(details, Has.Count.EqualTo(2));
        Assert.That(details, Has.Some.Contains("declared rules [crLeftHead] count 2 != traced count 1"));
        Assert.That(details, Has.Some.Contains("declared rules [crRightHead] count 0 != traced count 1"));
    }
    [Test]
    public void RunSelfCheck_ExpectCrashFixtureThatTimesOut_Fails()
    {
        FixtureResult result = RunSlowFixtureOverBudget("\nexpect_crash: true");

        Assert.That(result.Outcome, Is.EqualTo(FixtureOutcome.Failed), result.Reason);
    }

    [Test]
    public void RunSelfCheck_TimedOutFixture_ReportsNoWordResults()
    {
        FixtureResult result = RunSlowFixtureOverBudget("");

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(FixtureOutcome.Failed), result.Reason);
            Assert.That(result.WordResults, Is.Empty);
        });
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
