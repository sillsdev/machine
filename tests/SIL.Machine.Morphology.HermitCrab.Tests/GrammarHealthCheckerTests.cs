using System.Text;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class GrammarHealthCheckerTests
{
    private static FeatureSystem VocFeatureSystem()
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("voc", new FeatureSymbol("voc+", "+"), new FeatureSymbol("voc-", "-")),
        };
        featSys.Freeze();
        return featSys;
    }

    [Test]
    public void Check_TwoSegmentsShareFeatureBundle_ReportsBothByName()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);
        table.AddSegment("b", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);

        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(table);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(findings, Has.Count.EqualTo(1));
        GrammarHealthFinding finding = findings[0];
        Assert.That(finding.Code, Is.EqualTo(GrammarHealthCodes.DuplicateFeatureBundle));
        Assert.That(finding.Message, Does.Contain("a"));
        Assert.That(finding.Message, Does.Contain("b"));
        Assert.That(finding.Subjects, Contains.Item(table));
    }

    [Test]
    public void Check_EverySegmentHasDistinctFeatureBundle_NoFindings()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);
        table.AddSegment("b", FeatureStruct.NewMutable(featSys).Symbol("voc-").Value);

        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(table);

        Assert.That(GrammarHealthChecker.Check(language), Is.Empty);
    }

    [Test]
    public void Check_NoPhonologicalFeatureSystem_DoesNotFlagTriviallyIdenticalBundles()
    {
        // No PhonologicalFeatureSystem at all (the strrep-identity shape): every segment's bundle is
        // the same empty struct by construction, so this must not be reported as a duplicate.
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a");
        table.AddSegment("b");
        table.AddSegment("c");

        var language = new Language();
        language.CharacterDefinitionTables.Add(table);

        Assert.That(GrammarHealthChecker.Check(language), Is.Empty);
    }

    [Test]
    public void Check_LexicalEntryUsesSegmentNoTableDeclares_ReportsFinding()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);

        var stratum = new Stratum(table) { Name = "Surface" };

        // A Segments object built by hand rather than through CharacterDefinitionTable.Segment, which
        // is the only place that validates a representation's characters against the table -- a host
        // building the object model directly (not via XmlLanguageLoader) is not required to go through it.
        FeatureStruct undeclaredFs = FeatureStruct.NewMutable(featSys).Symbol("voc-").Value;
        undeclaredFs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
        undeclaredFs.Freeze();
        var shape = new Shape(begin => new ShapeNode(begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
        shape.Add(undeclaredFs);
        var segments = new Segments(table, "z", shape);

        var entry = new LexEntry { Id = "e1" };
        entry.Allomorphs.Add(new RootAllomorph(segments));
        stratum.Entries.Add(entry);

        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(table);
        language.Strata.Add(stratum);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Code, Is.EqualTo(GrammarHealthCodes.UndeclaredSegment));
        Assert.That(findings[0].Severity, Is.EqualTo(GrammarHealthSeverity.Error));
        Assert.That(findings[0].Message, Does.Contain("e1"));
    }

    [Test]
    public void Check_CleanGrammar_NoFindingsAtAll()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);
        table.AddSegment("b", FeatureStruct.NewMutable(featSys).Symbol("voc-").Value);

        var stratum = new Stratum(table) { Name = "Surface" };
        var entry = new LexEntry { Id = "e1" };
        entry.Allomorphs.Add(new RootAllomorph(new Segments(table, "ab")));
        stratum.Entries.Add(entry);

        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(table);
        language.Strata.Add(stratum);

        Assert.That(GrammarHealthChecker.Check(language), Is.Empty);
    }

    private static string FindRepositoryRoot()
    {
        for (string? dir = AppContext.BaseDirectory; dir != null; dir = Directory.GetParent(dir)?.FullName)
        {
            if (File.Exists(Path.Combine(dir, "conformance", "constructs.txt")))
                return dir;
        }
        throw new InvalidOperationException("could not find the repository root from the test output directory");
    }

    [Test]
    public void Check_StrRepIdentityFixture_HasNoPhonologicalFeatureSystem_ReportsNoDuplicateBundleFinding()
    {
        string grammarPath = Path.Combine(
            FindRepositoryRoot(),
            "conformance",
            "edge-cases",
            "strrep-identity",
            "grammar.xml"
        );
        Language language = XmlLanguageLoader.Load(grammarPath);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(findings.Where(f => f.Code == GrammarHealthCodes.DuplicateFeatureBundle), Is.Empty);
    }

    // Not a correctness assertion on the fixture set (which is free to grow or change) -- this is the
    // "run it over the real fixtures and report what it finds" deliverable the task asked for.
    [Test]
    public void Check_AllRealConformanceFixtures_ReportsFindings()
    {
        string conformanceRoot = Path.Combine(FindRepositoryRoot(), "conformance");
        List<Fixture> fixtures = Fixture.DiscoverAll(conformanceRoot);
        Assert.That(fixtures, Is.Not.Empty, "expected to discover conformance/languages and conformance/edge-cases fixtures");

        var report = new StringBuilder();
        report.AppendLine($"{fixtures.Count} fixtures checked.");
        int fixturesWithFindings = 0;
        int totalFindings = 0;
        foreach (Fixture fixture in fixtures.OrderBy(f => f.Id, StringComparer.Ordinal))
        {
            Language language = XmlLanguageLoader.Load(fixture.GrammarPath);
            IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);
            if (findings.Count == 0)
                continue;

            fixturesWithFindings++;
            totalFindings += findings.Count;
            report.AppendLine($"{fixture.Id}:");
            foreach (GrammarHealthFinding finding in findings)
                report.AppendLine($"  [{finding.Severity}] {finding.Code}: {finding.Message}");
        }

        report.AppendLine($"{fixturesWithFindings} of {fixtures.Count} fixtures had findings ({totalFindings} total).");
        TestContext.Out.WriteLine(report.ToString());
    }
}
