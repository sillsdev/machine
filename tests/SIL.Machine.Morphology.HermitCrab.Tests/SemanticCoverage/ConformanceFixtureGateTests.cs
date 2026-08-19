using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Runs every conformance fixture inside the ordinary test suite. Before this existed the fixtures
/// were reachable only through the standalone hc-conformance CLI, which no CI step invoked, so a
/// fixture could regress without any build failing. The coverage ledger is only meaningful if the
/// grammars it measures are actually executed.
/// </summary>
[TestFixture]
public sealed class ConformanceFixtureGateTests
{
    /// <summary>Guards against the suite silently shrinking to nothing and still reporting green.</summary>
    private const int MinimumFixtures = 25;

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

    private static List<Fixture> Discover() => Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance"));

    [Test]
    public void EveryConformanceFixturePassesSelfCheck()
    {
        List<Fixture> fixtures = Discover();
        Assert.That(fixtures, Has.Count.GreaterThanOrEqualTo(MinimumFixtures));

        var engine = new SelfCheckEngine(null);
        RunReport report = Runner.RunSelfCheck(fixtures, includePathological: false, engine.Capabilities, propose: false, TextWriter.Null);

        string[] failures = report
            .Results.Where(result => result.Outcome == FixtureOutcome.Failed)
            .Select(result => $"{result.FixtureId}: {result.Reason}")
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        Assert.That(failures, Is.Empty, $"conformance fixtures failed:\n  {string.Join("\n  ", failures)}");
        Assert.That(report.Passed, Is.GreaterThanOrEqualTo(MinimumFixtures), "the run must actually execute fixtures, not skip them all");
    }

    // A fixture whose grammar the coverage gate reads but which never runs would let the ledger
    // credit surfaces nothing executes.
    [Test]
    public void EveryGrammarTheCoverageGateReadsBelongsToADiscoveredFixture()
    {
        string root = RepositoryRoot();
        var discovered = Discover().Select(fixture => Path.GetFullPath(fixture.GrammarPath)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] unrun = GrammarCoverageGate
            .DiscoverGrammars(root)
            .Select(item => Path.GetFullPath(item.GrammarPath))
            .Where(path => !discovered.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.That(unrun, Is.Empty, $"these grammars feed coverage but are not run:\n  {string.Join("\n  ", unrun)}");
    }

    // coverage.csv and rules.csv are only written by an explicit --coverage-report run, and
    // parity-check.py gates on them. Adding a construct to constructs.txt without regenerating them
    // left that gate red for two commits.
    [Test]
    public void CheckedInCoverageTablesAreUpToDate()
    {
        string root = RepositoryRoot();
        string temp = Path.Combine(Path.GetTempPath(), $"hc-coverage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            string coverage = Path.Combine(temp, "coverage.csv");
            string rules = Path.Combine(temp, "rules.csv");
            CoverageReport.WriteCsvs(Discover(), coverage, rules);

            Assert.Multiple(() =>
            {
                Assert.That(
                    File.ReadAllText(coverage).ReplaceLineEndings("\n"),
                    Is.EqualTo(File.ReadAllText(Path.Combine(root, "conformance", "coverage.csv")).ReplaceLineEndings("\n")),
                    "regenerate with: hc-conformance --fixtures conformance --coverage-report"
                );
                Assert.That(
                    File.ReadAllText(rules).ReplaceLineEndings("\n"),
                    Is.EqualTo(File.ReadAllText(Path.Combine(root, "conformance", "rules.csv")).ReplaceLineEndings("\n")),
                    "regenerate with: hc-conformance --fixtures conformance --coverage-report"
                );
            });
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    // Every construct a fixture claims must be a real line in the checklist, and every checklist line
    // must be claimed. parity-check.py enforces the second half; nothing enforced the first.
    [Test]
    public void EveryExercisedConstructIsDeclaredInTheChecklist()
    {
        string root = RepositoryRoot();
        var declared = CoverageReport
            .LoadConstructChecklist(Path.Combine(root, "conformance", "constructs.txt"))
            .ToHashSet(StringComparer.Ordinal);

        string[] unknown = Discover()
            .SelectMany(fixture => fixture.Words.Words)
            .SelectMany(word => word.Exercises.Concat(word.Parses.SelectMany(parse => parse.Exercises)))
            .Distinct(StringComparer.Ordinal)
            .Where(construct => !declared.Contains(construct))
            .OrderBy(construct => construct, StringComparer.Ordinal)
            .ToArray();

        Assert.That(unknown, Is.Empty, $"add these to constructs.txt or fix the tag:\n  {string.Join("\n  ", unknown)}");
    }

    // WordsYamlLoader already requires at least one word and a parse unless the word is
    // expect_fail/expect_skip, so asserting that is a tautology. What it does NOT guarantee is that any
    // word attributes its parse to a rule, which is the only thing trace evidence can be built from.
    [Test]
    public void FixturesWithRulesAttributeAtLeastOneParseToThem()
    {
        var silent = new List<string>();
        foreach (Fixture fixture in Discover())
        {
            bool declaresRules = File.ReadAllText(fixture.GrammarPath).Contains("MorphologicalRule id=", StringComparison.Ordinal);
            bool attributesAny = fixture.Words.Words.Any(word => word.Parses.Any(parse => parse.Rules.Count > 0));
            if (declaresRules && !attributesAny)
                silent.Add(fixture.Id);
        }

        Assert.That(
            silent,
            Is.Empty,
            "these declare morphological rules but no parse names one, so nothing they contain can be trace-verified"
        );
    }
}
