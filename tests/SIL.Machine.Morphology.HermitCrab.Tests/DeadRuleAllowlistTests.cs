using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class DeadRuleAllowlistTests
{
    private static CoverageReport.DeadRule Dead(string fixtureId, string ruleId) =>
        new() { FixtureId = fixtureId, RuleId = ruleId };

    private static string WriteAllowlist(params string[] lines)
    {
        string path = Path.GetTempFileName();
        File.WriteAllLines(path, lines);
        return path;
    }

    [Test]
    public void AllowedDeadRulesPass()
    {
        var allowed = new Dictionary<(string, string), string> { [("edge-cases/a", "r1")] = "decoy" };

        var (unexpected, stale) = CoverageReport.CheckDeadRules(new[] { Dead("edge-cases/a", "r1") }, allowed);

        Assert.That(unexpected, Is.Empty);
        Assert.That(stale, Is.Empty);
    }

    [Test]
    public void DeadRuleMissingFromAllowlistIsUnexpected()
    {
        var allowed = new Dictionary<(string, string), string> { [("edge-cases/a", "r1")] = "decoy" };

        var (unexpected, _) = CoverageReport.CheckDeadRules(
            new[] { Dead("edge-cases/a", "r1"), Dead("edge-cases/a", "r2") },
            allowed
        );

        Assert.That(unexpected.Select(d => d.RuleId), Is.EqualTo(new[] { "r2" }));
    }

    [Test]
    public void AllowedRuleThatIsNoLongerDeadIsStale()
    {
        var allowed = new Dictionary<(string, string), string>
        {
            [("edge-cases/a", "r1")] = "decoy",
            [("edge-cases/b", "r1")] = "decoy",
        };

        var (_, stale) = CoverageReport.CheckDeadRules(new[] { Dead("edge-cases/a", "r1") }, allowed);

        Assert.That(stale, Is.EqualTo(new[] { ("edge-cases/b", "r1") }));
    }

    [Test]
    public void RuleIdsMatchPerFixtureAndOrdinally()
    {
        var allowed = new Dictionary<(string, string), string> { [("edge-cases/a", "rule")] = "decoy" };

        var (unexpected, stale) = CoverageReport.CheckDeadRules(
            new[] { Dead("edge-cases/b", "rule"), Dead("edge-cases/a", "Rule") },
            allowed
        );

        Assert.That(unexpected, Has.Count.EqualTo(2));
        Assert.That(stale, Has.Count.EqualTo(1));
    }

    [Test]
    public void LoaderSkipsCommentsAndBlankLines()
    {
        string path = WriteAllowlist("# header", "", "edge-cases/a\tr1\tdecoy");
        try
        {
            Dictionary<(string FixtureId, string RuleId), string> allowed = CoverageReport.LoadDeadRuleAllowlist(path);

            Assert.That(allowed[("edge-cases/a", "r1")], Is.EqualTo("decoy"));
            Assert.That(allowed, Has.Count.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase("edge-cases/a\tr1")]
    [TestCase("edge-cases/a\tr1\t ")]
    [TestCase("edge-cases/a\tr1\tdecoy\textra")]
    public void LoaderRejectsRowsWithoutAReason(string row)
    {
        string path = WriteAllowlist(row);
        try
        {
            Assert.Throws<FormatException>(() => CoverageReport.LoadDeadRuleAllowlist(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void LoaderRejectsDuplicateEntries()
    {
        string path = WriteAllowlist("edge-cases/a\tr1\tdecoy", "edge-cases/a\tr1\tagain");
        try
        {
            Assert.Throws<FormatException>(() => CoverageReport.LoadDeadRuleAllowlist(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static WordsYaml WordsBlockedBy(params string[] labels)
    {
        var words = new WordsYaml { Language = "L" };
        var word = new WordEntry { Word = "w", ExpectFail = true };
        word.BlockedBy.AddRange(labels);
        words.Words.Add(word);
        return words;
    }

    [Test]
    public void BlockedByLabelThatIsAGrammarRuleIdPasses()
    {
        var unknown = CoverageReport.FindUnknownBlockedBy("edge-cases/a", WordsBlockedBy("r1"), new[] { "r1", "r2" });

        Assert.That(unknown, Is.Empty);
    }

    [Test]
    public void BlockedByLabelThatIsNotAGrammarRuleIdFails()
    {
        var unknown = CoverageReport.FindUnknownBlockedBy(
            "edge-cases/a",
            WordsBlockedBy("r1", "bogus"),
            new[] { "r1", "r2" }
        );

        Assert.That(unknown.Select(u => u.Label), Is.EqualTo(new[] { "bogus" }));
        Assert.That(unknown[0].FixtureId, Is.EqualTo("edge-cases/a"));
        Assert.That(unknown[0].Word, Is.EqualTo("w"));
    }

    [Test]
    public void BlockedByLabelsAreComparedOrdinally()
    {
        var unknown = CoverageReport.FindUnknownBlockedBy("edge-cases/a", WordsBlockedBy("R1"), new[] { "r1" });

        Assert.That(unknown, Has.Count.EqualTo(1));
    }
}
