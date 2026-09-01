using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

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

    // Built by hand, bypassing CharacterDefinitionTable.Segment's validation; a host building the
    // model directly is not required to call it.
    private static Segments UndeclaredSegments(CharacterDefinitionTable table, FeatureSystem featSys)
    {
        FeatureStruct undeclaredFs = FeatureStruct.NewMutable(featSys).Symbol("voc-").Value;
        undeclaredFs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
        undeclaredFs.Freeze();
        var shape = new Shape(begin => new ShapeNode(
            begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor
        ));
        shape.Add(undeclaredFs);
        return new Segments(table, "z", shape);
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
        Assert.That(finding.Message, Does.Contain(": a, b."));
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

        Segments segments = UndeclaredSegments(table, featSys);

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
    public void Check_TemplateOnlyRuleInsertsUndeclaredSegment_ReportsFinding()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);

        var stratum = new Stratum(table) { Name = "Surface" };
        var rule = new AffixProcessRule { Name = "plural" };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph { Rhs = { new InsertSegments(UndeclaredSegments(table, featSys)) } }
        );
        var template = new AffixTemplate { Name = "verb" };
        template.Slots.Add(new AffixTemplateSlot(rule));
        stratum.AffixTemplates.Add(template);
        // rule is deliberately absent from stratum.MorphologicalRules -- reached only via the template slot.

        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(table);
        language.Strata.Add(stratum);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Code, Is.EqualTo(GrammarHealthCodes.UndeclaredSegment));
        Assert.That(findings[0].Severity, Is.EqualTo(GrammarHealthSeverity.Error));
        Assert.That(findings[0].Message, Does.Contain("plural"));
    }

    [Test]
    public void Check_RealizationalRuleInsertsUndeclaredSegment_ReportsFinding()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);

        var stratum = new Stratum(table) { Name = "Surface" };
        var rule = new RealizationalAffixProcessRule { Name = "past_suffix" };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph { Rhs = { new InsertSegments(UndeclaredSegments(table, featSys)) } }
        );
        stratum.MorphologicalRules.Add(rule);

        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(table);
        language.Strata.Add(stratum);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Code, Is.EqualTo(GrammarHealthCodes.UndeclaredSegment));
        Assert.That(findings[0].Severity, Is.EqualTo(GrammarHealthSeverity.Error));
        Assert.That(findings[0].Message, Does.Contain("past_suffix"));
    }

    [Test]
    public void Check_NaturalClassReferencesUndeclaredSegment_ReportsFinding()
    {
        FeatureSystem featSys = VocFeatureSystem();
        var declaredTable = new CharacterDefinitionTable { Name = "table1" };
        declaredTable.AddSegment("a", FeatureStruct.NewMutable(featSys).Symbol("voc+").Value);

        var undeclaredTable = new CharacterDefinitionTable { Name = "table2" };
        CharacterDefinition undeclaredSegment = undeclaredTable.AddSegment(
            "z",
            FeatureStruct.NewMutable(featSys).Symbol("voc-").Value
        );
        // undeclaredTable is deliberately never added to language.CharacterDefinitionTables.

        var naturalClass = new SegmentNaturalClass(new[] { undeclaredSegment }) { Name = "Vowel" };

        var language = new Language { PhonologicalFeatureSystem = featSys };
        language.CharacterDefinitionTables.Add(declaredTable);
        language.NaturalClasses.Add(naturalClass);

        IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Code, Is.EqualTo(GrammarHealthCodes.UndeclaredSegment));
        Assert.That(findings[0].Severity, Is.EqualTo(GrammarHealthSeverity.Error));
        Assert.That(findings[0].Message, Does.Contain("Vowel"));
        Assert.That(findings[0].Message, Does.Contain("z"));
        Assert.That(findings[0].Subjects, Is.EqualTo(new object[] { naturalClass, undeclaredSegment }));
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
}
