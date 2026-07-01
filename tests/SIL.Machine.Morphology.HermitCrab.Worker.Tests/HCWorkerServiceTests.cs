using System.IO;
using System.Linq;
using System.ServiceModel;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.Worker;

/// <summary>
/// Validates the out-of-process worker (RUSTIFY-fieldworks-worker-design.md) along its two risk
/// axes:
/// 1. Grammar-transfer fidelity: XmlLanguageWriter.Save -> XmlLanguageLoader.Load preserves
///    FieldWorks' ad hoc Properties["ID"]/["ID2"]/["InflTypeID"] tags (see HCWorkerConstants),
///    which HCParser.GetMorphs needs and which the DTO carries back across the process boundary -
///    plus a real net.pipe ServiceHost/ChannelFactory round trip (catches DataContract mistakes an
///    in-proc call would miss). Uses a single-morph grammar built from scratch: HermitCrabTestBase
///    is in-memory-only (its "1".."4"/"cons+" style ids are never DTD-id-validated because nothing
///    there ever calls Save) and Pattern.Annotation(featureStruct)-built LHS patterns (the
///    AffixProcessAllomorph shape MorpherTests uses) have no CharacterDefinition/SimpleContext Tag
///    for XmlLanguageWriter to serialize (WritePatternNodes: `if (constraint.Tag == null) yield
///    break;`) - both pre-existing gaps orthogonal to this design, not exercised until now because
///    nothing previously round-tripped a Pattern-bearing Language through Save/Load.
/// 2. DTO-extraction correctness (HCWorkerService.ToWordAnalysisDto) on a real multi-morph Word,
///    including the MorphemeIndex grouping that stands in for GetMorphs' Dictionary&lt;Morpheme,
///    MorphInfo&gt; reference-identity lookup - built the same way MorpherTests does (in-memory
///    only, no serialization needed since ToWordAnalysisDto is exercised directly).
/// </summary>
[TestFixture]
public class HCWorkerServiceTests
{
    private const int RootMsaId = 42;
    private const int RootFormId = 555;

    private Language _language = default!;
    private string _grammarXml = default!;

    [SetUp]
    public void SetUp()
    {
        // Symbol/feature ids double as an XML ID (NCName) once written via XmlLanguageWriter, so
        // unlike HermitCrabTestBase's in-memory-only fixture, these must be NCName-safe - matching
        // how HCLoader.cs actually generates ids in production (e.g. "pos" + msa.Hvo, never a bare
        // symbol character like "+").
        var phoneticFeatSys = new FeatureSystem
        {
            new SymbolicFeature("cons", new FeatureSymbol("consPos", "+"), new FeatureSymbol("consNeg", "-")),
            new SymbolicFeature("voc", new FeatureSymbol("vocPos", "+"), new FeatureSymbol("vocNeg", "-")),
            new SymbolicFeature("place", new FeatureSymbol("alveolar"), new FeatureSymbol("velar")),
        };
        phoneticFeatSys.Freeze();

        // "s" and "g" are both consonants (consPos/vocNeg); without a distinguishing "place"
        // feature CharacterDefinitionTable.GetMatchingStrReps can't tell them apart and returns
        // whichever was added first for both - a fixture bug, not a worker bug (caught by
        // ParseWord_AfterGrammarRoundTrip_CarriesFieldWorksIds asserting the exact FormStr).
        var table = new CharacterDefinitionTable { Name = "table" };
        AddSeg(table, phoneticFeatSys, "s", "consPos", "vocNeg", "alveolar");
        AddSeg(table, phoneticFeatSys, "a", "consNeg", "vocPos");
        AddSeg(table, phoneticFeatSys, "g", "consPos", "vocNeg", "velar");

        var syntacticFeatSys = new SyntacticFeatureSystem();
        syntacticFeatSys.AddPartsOfSpeech(new FeatureSymbol("V", "Verb"));
        syntacticFeatSys.Freeze();

        var stratum = new Stratum(table) { Name = "Test", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };

        var root = new LexEntry
        {
            Id = "root",
            Gloss = "sag",
            SyntacticFeatureStruct = FeatureStruct.New(syntacticFeatSys).Symbol("V").Value,
        };
        root.Allomorphs.Add(new RootAllomorph(new Segments(table, "sag", true)));
        // Simulate what FieldWorks' HCLoader.cs does when it builds the grammar from LCM: tag the
        // entry's morpheme/allomorph with the ids HCParser.GetMorphs needs to map back to live LCM
        // objects (Src\LexText\ParserCore\HCLoader.cs lines ~714/809-813; HCParser.cs's
        // FormID/MsaID/InflTypeID constants are "ID"/"ID2"/"InflTypeID"). HCWorkerConstants in the
        // worker project uses the identical key strings.
        root.Properties[HCWorkerConstants.MsaId] = RootMsaId;
        root.PrimaryAllomorph.Properties[HCWorkerConstants.FormId] = RootFormId;
        stratum.Entries.Add(root);

        _language = new Language
        {
            Name = "WorkerTest",
            PhonologicalFeatureSystem = phoneticFeatSys,
            SyntacticFeatureSystem = syntacticFeatSys,
            Strata = { stratum },
        };
        _language.CharacterDefinitionTables.Add(table);

        string tempPath = Path.Combine(Path.GetTempPath(), $"hcworker-test-{TestContext.CurrentContext.Test.ID}.xml");
        XmlLanguageWriter.Save(_language, tempPath);
        _grammarXml = File.ReadAllText(tempPath);
        File.Delete(tempPath);
    }

    private static void AddSeg(
        CharacterDefinitionTable table,
        FeatureSystem phoneticFeatSys,
        string strRep,
        params string[] symbols
    )
    {
        var fs = new FeatureStruct();
        foreach (string symbolId in symbols)
        {
            FeatureSymbol symbol = phoneticFeatSys.GetSymbol(symbolId);
            fs.AddValue(symbol.Feature, new SymbolicFeatureValue(symbol));
        }
        table.AddSegment(strRep, fs);
    }

    private HCGrammarDto MakeGrammarDto() =>
        new()
        {
            CompiledGrammarXml = _grammarXml,
            DeletionReapplications = 0,
            MaxStemCount = 2,
            MergeEquivalentAnalyses = false,
        };

    [Test]
    public void ParseWord_AfterGrammarRoundTrip_CarriesFieldWorksIds()
    {
        var service = new HCWorkerService();
        service.UpdateGrammar(MakeGrammarDto());
        WordAnalysisDto[] actual = service.ParseWord("sag", false);

        Assert.That(actual, Has.Length.EqualTo(1));
        Assert.That(actual[0].Morphs, Has.Length.EqualTo(1));

        MorphDto root = actual[0].Morphs[0];
        Assert.That(root.FormId, Is.EqualTo(RootFormId));
        Assert.That(root.MsaId, Is.EqualTo(RootMsaId));
        Assert.That(root.FormStr, Is.EqualTo("sag"));
        Assert.That(root.IsAffixProcessAllomorph, Is.False);
    }

    [Test]
    public void ParseWordsBatch_ReturnsOneEntryPerWord()
    {
        var service = new HCWorkerService();
        service.UpdateGrammar(MakeGrammarDto());

        var result = service.ParseWordsBatch(new[] { "sag", "nonword" }, false);

        Assert.That(result.Keys, Is.EquivalentTo(new[] { "sag", "nonword" }));
        Assert.That(result["sag"], Has.Length.EqualTo(1));
        Assert.That(result["nonword"], Is.Empty);
    }

    [Test]
    public void ParseWord_BeforeUpdateGrammar_Throws()
    {
        var service = new HCWorkerService();
        Assert.Throws<System.InvalidOperationException>(() => service.ParseWord("sag", false));
    }

    [Test]
    public void OverWcfNamedPipe_RoundTripsCorrectly()
    {
        string pipeName = "hcworker-test-" + TestContext.CurrentContext.Test.ID;
        NetNamedPipeBinding pipeBinding = PipeBindingFactory.Create();

        using var host = new ServiceHost(new HCWorkerService());
        host.AddServiceEndpoint(typeof(IHCWorkerService), pipeBinding, "net.pipe://localhost/" + pipeName);
        host.Open();
        try
        {
            var factory = new ChannelFactory<IHCWorkerService>(
                pipeBinding,
                new EndpointAddress("net.pipe://localhost/" + pipeName)
            );
            IHCWorkerService client = factory.CreateChannel();

            client.UpdateGrammar(MakeGrammarDto());
            WordAnalysisDto[] result = client.ParseWord("sag", false);

            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].Morphs, Has.Length.EqualTo(1));
            Assert.That(result[0].Morphs[0].FormId, Is.EqualTo(RootFormId));

            var batch = client.ParseWordsBatch(new[] { "sag" }, false);
            Assert.That(batch["sag"], Has.Length.EqualTo(1));

            ((IClientChannel)client).Close();
            factory.Close();
        }
        finally
        {
            host.Close();
        }
    }

    /// <summary>
    /// Exercises ToWordAnalysisDto directly on a real multi-morph Word (root + suffix rule, built
    /// the same way MorpherTests.AnalyzeWord_CanAnalyze_ReturnsCorrectAnalysis does) - no XML
    /// round trip involved, so Pattern.Annotation's writer limitation (see class doc comment)
    /// doesn't apply here. This is the scenario GetMorphs' circumfix/second-occurrence dictionary
    /// lookup exists for; MorphemeIndex is this DTO's wire-safe replacement for it.
    /// </summary>
    [Test]
    public void ToWordAnalysisDto_MultiMorphWord_GroupsByMorphemeAndFlagsAffixProcessAllomorph()
    {
        var phonologicalFeatSys = new FeatureSystem
        {
            new SymbolicFeature("cons", new FeatureSymbol("cons+", "+"), new FeatureSymbol("cons-", "-")),
            new SymbolicFeature("voc", new FeatureSymbol("voc+", "+"), new FeatureSymbol("voc-", "-")),
        };
        phonologicalFeatSys.Freeze();
        var syntacticFeatSys = new SyntacticFeatureSystem();
        syntacticFeatSys.AddPartsOfSpeech(new FeatureSymbol("V", "Verb"));
        syntacticFeatSys.Freeze();

        var table = new CharacterDefinitionTable { Name = "table" };
        AddSeg(table, phonologicalFeatSys, "s", "cons+", "voc-");
        AddSeg(table, phonologicalFeatSys, "a", "cons-", "voc+");
        AddSeg(table, phonologicalFeatSys, "g", "cons+", "voc-");
        AddSeg(table, phonologicalFeatSys, "d", "cons+", "voc-");
        table.AddBoundary("+");

        var stratum = new Stratum(table) { Name = "Test", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };

        var root = new LexEntry
        {
            Id = "root",
            Gloss = "sag",
            SyntacticFeatureStruct = FeatureStruct.New(syntacticFeatSys).Symbol("V").Value,
        };
        root.Allomorphs.Add(new RootAllomorph(new Segments(table, "sag", true)));
        root.Properties[HCWorkerConstants.MsaId] = RootMsaId;
        root.PrimaryAllomorph.Properties[HCWorkerConstants.FormId] = RootFormId;
        stratum.Entries.Add(root);

        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var pastSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(syntacticFeatSys).Symbol("V").Value,
        };
        var suffixAllomorph = new AffixProcessAllomorph
        {
            Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
            Rhs = { new CopyFromInput("1"), new InsertSegments(table, "+d") },
        };
        const int suffixFormId = 777;
        const int suffixMsaId = 99;
        suffixAllomorph.Properties[HCWorkerConstants.FormId] = suffixFormId;
        pastSuffix.Properties[HCWorkerConstants.MsaId] = suffixMsaId;
        pastSuffix.Allomorphs.Add(suffixAllomorph);
        stratum.MorphologicalRules.Add(pastSuffix);

        var language = new Language
        {
            Name = "WorkerTest",
            PhonologicalFeatureSystem = phonologicalFeatSys,
            SyntacticFeatureSystem = syntacticFeatSys,
            Strata = { stratum },
        };

        var morpher = new Morpher(new TraceManager(), language);
        Word word = morpher.ParseWord("sagd", out _, false).Single();

        WordAnalysisDto dto = HCWorkerService.ToWordAnalysisDto(word);

        Assert.That(dto.Morphs, Has.Length.EqualTo(2));
        MorphDto rootMorph = dto.Morphs[0];
        MorphDto suffixMorph = dto.Morphs[1];

        Assert.That(rootMorph.FormId, Is.EqualTo(RootFormId));
        Assert.That(rootMorph.MsaId, Is.EqualTo(RootMsaId));
        Assert.That(rootMorph.IsAffixProcessAllomorph, Is.False);

        Assert.That(suffixMorph.FormId, Is.EqualTo(suffixFormId));
        Assert.That(suffixMorph.MsaId, Is.EqualTo(suffixMsaId));
        Assert.That(suffixMorph.IsAffixProcessAllomorph, Is.True);

        Assert.That(suffixMorph.MorphemeIndex, Is.Not.EqualTo(rootMorph.MorphemeIndex));
    }
}
