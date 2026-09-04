using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

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
        var shape = new Shape(begin => new ShapeNode(
            begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor
        ));
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

    [Test]
    public void Check_PartialLexicalEntry_ReportsActionableWarning()
    {
        var table = new CharacterDefinitionTable { Name = "table1" };
        var stratum = new Stratum(table) { Name = "Surface" };
        var entry = new LexEntry { Id = "entry1", IsPartial = true };
        stratum.Entries.Add(entry);
        var language = new Language();
        language.Strata.Add(stratum);

        GrammarHealthFinding finding = GrammarHealthChecker.Check(language).Single();

        Assert.That(finding.Code, Is.EqualTo(GrammarHealthCodes.PartialMorpheme));
        Assert.That(finding.Severity, Is.EqualTo(GrammarHealthSeverity.Warning));
        Assert.That(finding.Message, Does.Contain("entry1"));
        Assert.That(finding.Message, Does.Contain("partially analyzed"));
        Assert.That(finding.Message, Does.Contain("final-template pruning"));
        Assert.That(finding.Subjects, Is.EqualTo(new object[] { entry }));
    }

    [Test]
    public void Check_PartialOrdinaryRule_ReportsRule()
    {
        var table = new CharacterDefinitionTable { Name = "table1" };
        var stratum = new Stratum(table) { Name = "Surface" };
        var rule = new AffixProcessRule { Name = "plural", IsPartial = true };
        stratum.MorphologicalRules.Add(rule);
        var language = new Language();
        language.Strata.Add(stratum);

        GrammarHealthFinding finding = GrammarHealthChecker.Check(language).Single();

        Assert.That(finding.Code, Is.EqualTo(GrammarHealthCodes.PartialMorpheme));
        Assert.That(finding.Message, Does.Contain("plural"));
        Assert.That(finding.Subjects, Is.EqualTo(new object[] { rule }));
    }

    [Test]
    public void Check_PartialTemplateRuleReferencedTwice_ReportsOnce()
    {
        var table = new CharacterDefinitionTable { Name = "table1" };
        var stratum = new Stratum(table) { Name = "Surface" };
        var rule = new AffixProcessRule { Name = "subject", IsPartial = true };
        var template = new AffixTemplate { Name = "verb" };
        template.Slots.Add(new AffixTemplateSlot(rule));
        template.Slots.Add(new AffixTemplateSlot(rule));
        stratum.AffixTemplates.Add(template);
        var language = new Language();
        language.Strata.Add(stratum);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Code, Is.EqualTo(GrammarHealthCodes.PartialMorpheme));
        Assert.That(findings[0].Subjects, Is.EqualTo(new object[] { rule }));
    }

    [Test]
    public void Check_PartialMorphemeAndExistingProblem_ReportsBoth()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);
        table.AddSegment("b", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);
        var stratum = new Stratum(table) { Name = "Surface" };
        stratum.Entries.Add(new LexEntry { Id = "entry1", IsPartial = true });
        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(table);
        language.Strata.Add(stratum);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(
            findings.Select(finding => finding.Code),
            Is.EquivalentTo(new[] { GrammarHealthCodes.DuplicateFeatureBundle, GrammarHealthCodes.PartialMorpheme })
        );
    }
}
