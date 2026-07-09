using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class XmlLanguageSerializationTests
{
    private static string TestXmlFileName =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "XmlLanguageSerializationTests.xml");

    private static string TempXmlFileName => Path.Combine(Path.GetTempPath(), "XmlLanguageSerializationTests.xml");

    [Test]
    public void RoundTripXml()
    {
        try
        {
            Language lang = XmlLanguageLoader.Load(TestXmlFileName);
            XmlLanguageWriter.Save(lang, TempXmlFileName);
            string testXml = File.ReadAllText(TestXmlFileName).Replace("\r\n", "\n");
            string tempXml = File.ReadAllText(TempXmlFileName).Replace("\r\n", "\n");
            Assert.That(tempXml, Is.EqualTo(testXml));
        }
        finally
        {
            if (File.Exists(TempXmlFileName))
                File.Delete(TempXmlFileName);
        }
    }

    // A Language built programmatically (e.g. by FieldWorks' HCLoader) can have
    // MorphemeCoOccurrenceRules/AllomorphCoOccurrenceRules that reference a morpheme/allomorph never written
    // under any Stratum -- e.g. an ad-hoc prohibition still targets an affix in a slot whose owning
    // inflectional-affix template was disabled. XmlLanguageWriter must skip such rules instead of throwing.
    [Test]
    public void Save_MorphemeCoOccurrenceRuleReferencesUnwrittenMorpheme_DoesNotThrowAndOmitsRule()
    {
        Language language = BuildCoOccurrenceRuleTestLanguage(out LexEntry writtenEntry, out LexEntry danglingEntry);
        language.MorphemeCoOccurrenceRules.Add(
            (
                danglingEntry,
                new MorphemeCoOccurrenceRule(
                    ConstraintType.Exclude,
                    new Morpheme[] { writtenEntry },
                    MorphCoOccurrenceAdjacency.Anywhere
                )
            )
        );

        string xml = SaveCoOccurrenceRuleTestLanguageToTempFile(language);
        Assert.That(xml, Does.Not.Contain("MorphemeCoOccurrenceRules"));
    }

    [Test]
    public void Save_MorphemeCoOccurrenceRuleReferencesUnwrittenOtherMorpheme_DoesNotThrowAndOmitsRule()
    {
        Language language = BuildCoOccurrenceRuleTestLanguage(out LexEntry writtenEntry, out LexEntry danglingEntry);
        language.MorphemeCoOccurrenceRules.Add(
            (
                writtenEntry,
                new MorphemeCoOccurrenceRule(
                    ConstraintType.Exclude,
                    new Morpheme[] { danglingEntry },
                    MorphCoOccurrenceAdjacency.Anywhere
                )
            )
        );

        string xml = SaveCoOccurrenceRuleTestLanguageToTempFile(language);
        Assert.That(xml, Does.Not.Contain("MorphemeCoOccurrenceRules"));
    }

    [Test]
    public void Save_AllomorphCoOccurrenceRuleReferencesUnwrittenAllomorph_DoesNotThrowAndOmitsRule()
    {
        Language language = BuildCoOccurrenceRuleTestLanguage(out LexEntry writtenEntry, out LexEntry danglingEntry);
        language.AllomorphCoOccurrenceRules.Add(
            (
                danglingEntry.Allomorphs[0],
                new AllomorphCoOccurrenceRule(
                    ConstraintType.Exclude,
                    new Allomorph[] { writtenEntry.Allomorphs[0] },
                    MorphCoOccurrenceAdjacency.Anywhere
                )
            )
        );

        string xml = SaveCoOccurrenceRuleTestLanguageToTempFile(language);
        Assert.That(xml, Does.Not.Contain("AllomorphCoOccurrenceRules"));
    }

    [Test]
    public void Save_MorphemeCoOccurrenceRuleReferencesOnlyWrittenMorphemes_IsWritten()
    {
        Language language = BuildCoOccurrenceRuleTestLanguage(out LexEntry writtenEntry, out _);
        var otherEntry = new LexEntry { Id = "other", Gloss = "other" };
        otherEntry.Allomorphs.Add(new RootAllomorph(new Segments(language.Strata[0].CharacterDefinitionTable, "a")));
        language.Strata[0].Entries.Add(otherEntry);
        language.MorphemeCoOccurrenceRules.Add(
            (
                writtenEntry,
                new MorphemeCoOccurrenceRule(
                    ConstraintType.Exclude,
                    new Morpheme[] { otherEntry },
                    MorphCoOccurrenceAdjacency.Anywhere
                )
            )
        );

        string xml = SaveCoOccurrenceRuleTestLanguageToTempFile(language);
        Assert.That(xml, Does.Contain("MorphemeCoOccurrenceRules"));
    }

    private static Language BuildCoOccurrenceRuleTestLanguage(out LexEntry writtenEntry, out LexEntry danglingEntry)
    {
        var syntacticFeatSys = new SyntacticFeatureSystem();
        syntacticFeatSys.AddPartsOfSpeech(new FeatureSymbol("N", "Noun"));
        syntacticFeatSys.Freeze();

        var table = new CharacterDefinitionTable { Name = "table1" };
        table.AddSegment("a");

        var stratum = new Stratum(table) { Name = "Stratum1" };

        writtenEntry = new LexEntry { Id = "written", Gloss = "written" };
        writtenEntry.Allomorphs.Add(new RootAllomorph(new Segments(table, "a")));
        stratum.Entries.Add(writtenEntry);

        // Never added to any stratum, so XmlLanguageWriter never assigns it an id.
        danglingEntry = new LexEntry { Id = "dangling", Gloss = "dangling" };
        danglingEntry.Allomorphs.Add(new RootAllomorph(new Segments(table, "a")));

        return new Language
        {
            Name = "Test",
            SyntacticFeatureSystem = syntacticFeatSys,
            CharacterDefinitionTables = { table },
            Strata = { stratum },
        };
    }

    private static string SaveCoOccurrenceRuleTestLanguageToTempFile(Language language)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Assert.DoesNotThrow(() => XmlLanguageWriter.Save(language, path));
            return File.ReadAllText(path);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
