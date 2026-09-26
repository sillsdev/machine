using System.Security.Cryptography;
using System.Text.RegularExpressions;
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
        RunReport report = Runner.RunSelfCheck(
            fixtures,
            includePathological: false,
            engine.Capabilities,
            propose: false,
            TextWriter.Null
        );

        string[] failures = report
            .Results.Where(result => result.Outcome == FixtureOutcome.Failed)
            .Select(result => $"{result.FixtureId}: {result.Reason}")
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        Assert.That(failures, Is.Empty, $"conformance fixtures failed:\n  {string.Join("\n  ", failures)}");
        Assert.That(
            report.Passed,
            Is.GreaterThanOrEqualTo(MinimumFixtures),
            "the run must actually execute fixtures, not skip them all"
        );
    }

    // A fixture whose grammar the coverage gate reads but which never runs would let the ledger
    // credit surfaces nothing executes.
    [Test]
    public void EveryGrammarTheCoverageGateReadsBelongsToADiscoveredFixture()
    {
        string root = RepositoryRoot();
        var discovered = Discover()
            .Select(fixture => Path.GetFullPath(fixture.GrammarPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] unrun = GrammarCoverageGate
            .DiscoverGrammars(root)
            .Select(item => Path.GetFullPath(item.GrammarPath))
            .Where(path => !discovered.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.That(unrun, Is.Empty, $"these grammars feed coverage but are not run:\n  {string.Join("\n  ", unrun)}");
    }

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
                    Is.EqualTo(
                        File.ReadAllText(Path.Combine(root, "conformance", "coverage.csv")).ReplaceLineEndings("\n")
                    ),
                    "regenerate with: hc-conformance --fixtures conformance --coverage-report"
                );
                Assert.That(
                    File.ReadAllText(rules).ReplaceLineEndings("\n"),
                    Is.EqualTo(
                        File.ReadAllText(Path.Combine(root, "conformance", "rules.csv")).ReplaceLineEndings("\n")
                    ),
                    "regenerate with: hc-conformance --fixtures conformance --coverage-report"
                );
            });
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public void EveryFixtureContributesAtLeastOneCoverageRow()
    {
        var noExerciseByDesign = new HashSet<string>(StringComparer.Ordinal)
        {
            "edge-cases/simultaneous-epenthesis-cascade",
        };

        List<Fixture> fixtures = Discover();
        Assert.That(
            fixtures,
            Has.Count.GreaterThanOrEqualTo(MinimumFixtures),
            "the discovery walk must find fixtures, not silently see zero"
        );

        HashSet<string> discoveredIds = fixtures.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        string[] staleAllowlistEntries = noExerciseByDesign
            .Where(id => !discoveredIds.Contains(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        Assert.That(
            staleAllowlistEntries,
            Is.Empty,
            "these allowlist entries name no discovered fixture (renamed or typo'd?):\n  "
                + string.Join("\n  ", staleAllowlistEntries)
        );

        string temp = Path.Combine(Path.GetTempPath(), $"hc-exercise-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            string coverage = Path.Combine(temp, "coverage.csv");
            string rules = Path.Combine(temp, "rules.csv");
            CoverageReport.WriteCsvs(fixtures, coverage, rules);

            string[] lines = File.ReadAllLines(coverage);
            Assert.That(
                lines,
                Has.Length.GreaterThan(1),
                "coverage.csv must have a header plus at least one data row -- an empty table proves nothing"
            );

            HashSet<string> languagesWithRows = lines
                .Skip(1)
                .Select(line => line[..line.IndexOf(',')])
                .ToHashSet(StringComparer.Ordinal);

            string[] silent = fixtures
                .Where(f => !languagesWithRows.Contains(f.Words.Language) && !noExerciseByDesign.Contains(f.Id))
                .Select(f => f.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            Assert.That(
                silent,
                Is.Empty,
                "these fixtures contribute ZERO rows to coverage.csv (no 'exercises:' tag anywhere) and "
                    + $"are not in the named allowlist above:\n  {string.Join("\n  ", silent)}"
            );
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

        Assert.That(
            unknown,
            Is.Empty,
            $"add these to constructs.txt or fix the tag:\n  {string.Join("\n  ", unknown)}"
        );
    }

    [Test]
    public void FixturesWithRulesAttributeAtLeastOneParseToThem()
    {
        var silent = new List<string>();
        foreach (Fixture fixture in Discover())
        {
            bool declaresRules = File.ReadAllText(fixture.GrammarPath)
                .Contains("MorphologicalRule id=", StringComparison.Ordinal);
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

    [Test]
    public void FieldWorksWitnessDirectoriesAreShapedCorrectly()
    {
        var allowedManifestKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "version",
            "base_sha256",
            "cases",
            "id",
            "operations",
            "expect",
            "remove_phoneme",
            "remove_all_phonemes",
            "guid",
            "assert_representations",
            "require_unreferenced",
            "xample_projection",
            "hc_analyses",
            "inferred_segments",
        };

        var errors = new List<string>();
        foreach (Fixture fixture in Discover())
        {
            string fieldworksDir = Path.Combine(fixture.Directory, "fieldworks");
            if (!Directory.Exists(fieldworksDir))
                continue;

            if (fixture.Words.FieldworksProducible != true)
            {
                errors.Add($"{fixture.Id}: has a fieldworks/ directory but is not 'fieldworks_producible: true'");
                continue;
            }

            var expectedEntries = new HashSet<string>(StringComparer.Ordinal)
            {
                "project.fwdata",
                "phonology-mutations.yaml",
                "WritingSystemStore",
            };
            string[] actualEntries = Directory.GetFileSystemEntries(fieldworksDir).Select(Path.GetFileName).ToArray()!;
            foreach (string extra in actualEntries.Where(e => !expectedEntries.Contains(e!)))
                errors.Add($"{fixture.Id}: fieldworks/ contains an unexpected entry '{extra}'");
            foreach (string missing in expectedEntries.Where(e => !actualEntries.Contains(e)))
                errors.Add($"{fixture.Id}: fieldworks/ is missing '{missing}'");

            string writingSystemDir = Path.Combine(fieldworksDir, "WritingSystemStore");
            if (Directory.Exists(writingSystemDir))
            {
                string[] nonLdml = Directory
                    .GetFiles(writingSystemDir)
                    .Select(Path.GetFileName)
                    .Where(name => !name!.EndsWith(".ldml", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray()!;
                if (nonLdml.Length > 0)
                {
                    errors.Add(
                        $"{fixture.Id}: WritingSystemStore/ contains non-.ldml entries: {string.Join(", ", nonLdml)}"
                    );
                }
            }

            string projectPath = Path.Combine(fieldworksDir, "project.fwdata");
            string manifestPath = Path.Combine(fieldworksDir, "phonology-mutations.yaml");
            if (!File.Exists(projectPath) || !File.Exists(manifestPath))
                continue; // already recorded as missing above

            string manifestText = File.ReadAllText(manifestPath);

            Match versionMatch = Regex.Match(manifestText, @"^version:\s*(\d+)\s*$", RegexOptions.Multiline);
            if (!versionMatch.Success || versionMatch.Groups[1].Value != "1")
                errors.Add($"{fixture.Id}: phonology-mutations.yaml has no 'version: 1'");

            Match shaMatch = Regex.Match(manifestText, @"^base_sha256:\s*([0-9a-f]{64})\s*$", RegexOptions.Multiline);
            if (!shaMatch.Success)
            {
                errors.Add($"{fixture.Id}: phonology-mutations.yaml has no 64-character lowercase-hex 'base_sha256'");
            }
            else
            {
                using var sha256 = SHA256.Create();
                using FileStream stream = File.OpenRead(projectPath);
                string actual = Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
                if (!string.Equals(actual, shaMatch.Groups[1].Value, StringComparison.Ordinal))
                {
                    errors.Add(
                        $"{fixture.Id}: phonology-mutations.yaml base_sha256 {shaMatch.Groups[1].Value} "
                            + $"does not match project.fwdata's actual sha256 {actual}"
                    );
                }
            }

            foreach (
                Match keyMatch in Regex.Matches(
                    manifestText,
                    @"^\s*-?\s*([A-Za-z_][A-Za-z0-9_]*):",
                    RegexOptions.Multiline
                )
            )
            {
                string key = keyMatch.Groups[1].Value;
                if (!allowedManifestKeys.Contains(key))
                    errors.Add($"{fixture.Id}: phonology-mutations.yaml uses a key outside the v1 vocabulary: '{key}'");
            }

            foreach (
                Match valueMatch in Regex.Matches(
                    manifestText,
                    @"(xample_projection|hc_analyses):\s*(\S+)\s*$",
                    RegexOptions.Multiline
                )
            )
            {
                if (valueMatch.Groups[2].Value != "same_as_base")
                {
                    errors.Add(
                        $"{fixture.Id}: phonology-mutations.yaml has {valueMatch.Groups[1].Value}: "
                            + $"{valueMatch.Groups[2].Value}, but only 'same_as_base' is defined"
                    );
                }
            }
        }

        Assert.That(errors, Is.Empty, string.Join("\n  ", errors));
    }
}
