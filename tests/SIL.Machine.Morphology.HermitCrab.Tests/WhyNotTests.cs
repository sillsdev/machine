using System.Text.Json;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.WhyNot;

namespace SIL.Machine.Morphology.HermitCrab;

public class WhyNotTests
{
    private sealed class Grammar
    {
        public Language Language = default!;
        public Stratum Stratum = default!;
        public CharacterDefinitionTable Table = default!;
        public SyntacticFeatureSystem Syntax = default!;
        public ComplexFeature Head = default!;

        public FeatureStruct Verb(string? person = null)
        {
            var builder = FeatureStruct.New(Syntax).Symbol("V");
            return person == null
                ? builder.Value
                : builder.Feature(Head).EqualTo(head => head.Feature("pers").EqualTo(person)).Value;
        }

        public LexEntry AddEntry(string id, FeatureStruct fs, params string[] forms)
        {
            var entry = new LexEntry
            {
                Id = id,
                Gloss = id,
                SyntacticFeatureStruct = fs,
            };
            foreach (string form in forms)
                entry.Allomorphs.Add(new RootAllomorph(new Segments(Table, form, true)));
            Stratum.Entries.Add(entry);
            return entry;
        }

        public AffixProcessRule AddSuffix(string id, string text)
        {
            var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
            var rule = new AffixProcessRule
            {
                Id = id,
                Name = id,
                Gloss = id,
                RequiredSyntacticFeatureStruct = Verb(),
            };
            rule.Allomorphs.Add(
                new AffixProcessAllomorph
                {
                    Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                    Rhs = { new CopyFromInput("1"), new InsertSegments(Table, "+" + text) },
                }
            );
            Stratum.MorphologicalRules.Add(rule);
            return rule;
        }
    }

    private static Grammar NewGrammar()
    {
        var phonology = new FeatureSystem
        {
            new SymbolicFeature("cons", new FeatureSymbol("cons+", "+"), new FeatureSymbol("cons-", "-")),
        };
        phonology.Freeze();
        var syntax = new SyntacticFeatureSystem
        {
            new SymbolicFeature("pers", new FeatureSymbol("1"), new FeatureSymbol("2"), new FeatureSymbol("3")),
        };
        syntax.AddPartsOfSpeech(new FeatureSymbol("N", "Noun"), new FeatureSymbol("V", "Verb"));
        ComplexFeature head = syntax.AddHeadFeature();
        syntax.AddFootFeature();
        syntax.Freeze();

        var table = new CharacterDefinitionTable { Name = "table" };
        foreach (string consonant in new[] { "k", "t", "s" })
        {
            var fs = new FeatureStruct();
            fs.AddValue(phonology.GetSymbol("cons+").Feature, new SymbolicFeatureValue(phonology.GetSymbol("cons+")));
            table.AddSegment(consonant, fs);
        }
        var vowel = new FeatureStruct();
        vowel.AddValue(phonology.GetSymbol("cons-").Feature, new SymbolicFeatureValue(phonology.GetSymbol("cons-")));
        table.AddSegment("a", vowel);
        table.AddBoundary("+");

        var stratum = new Stratum(table)
        {
            Name = "Surface",
            MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered,
        };
        return new Grammar
        {
            Stratum = stratum,
            Table = table,
            Syntax = syntax,
            Head = head,
            Language = new Language
            {
                Name = "WhyNot",
                PhonologicalFeatureSystem = phonology,
                SyntacticFeatureSystem = syntax,
                Strata = { stratum },
            },
        };
    }

    private static WhyNotResult Why(Grammar g, string word, params ExpectedMorph[] expected) =>
        WhyNotAnalyzer.Analyze(g.Language, word, expected);

    [Test]
    public void MorphemeCoOccurrence_ReportsAdhocProhibitionWithBothMorphs()
    {
        Grammar g = NewGrammar();
        LexEntry kat = g.AddEntry("kat", g.Verb(), "kat");
        AffixProcessRule plural = g.AddSuffix("PL", "s");
        kat.MorphemeCoOccurrenceRules.Add(
            new MorphemeCoOccurrenceRule(
                ConstraintType.Exclude,
                new Morpheme[] { plural },
                MorphCoOccurrenceAdjacency.Anywhere
            )
        );

        WhyNotResult result = Why(g, "kats", new ExpectedMorph("kat", null), new ExpectedMorph("PL", null));

        Assert.That(result.Outcome, Is.EqualTo(WhyNotOutcome.Rejected));
        Failure failure = result.Results[0].Failure;
        Assert.That(failure.Kind, Is.EqualTo("adhoc_prohibition"));
        Assert.That(failure.HcReason, Is.EqualTo("MorphemeCoOccurrenceRules"));
        Assert.That(failure.Provenance, Is.EqualTo("hc_reported"));
        Assert.That(failure.Morphs.Select(m => m.MorphemeId), Is.EqualTo(new[] { "kat", "PL" }));
        Assert.That(failure.Position, Is.EqualTo(0));
    }

    [Test]
    public void AllomorphCoOccurrence_ReportsTheRequiredPartnerThatIsMissing()
    {
        Grammar g = NewGrammar();
        LexEntry kat = g.AddEntry("kat", g.Verb(), "kat");
        AffixProcessRule plural = g.AddSuffix("PL", "s");
        AffixProcessRule other = g.AddSuffix("OTHER", "t");
        kat.Allomorphs[0]
            .AllomorphCoOccurrenceRules.Add(
                new AllomorphCoOccurrenceRule(
                    ConstraintType.Require,
                    new[] { other.Allomorphs[0] },
                    MorphCoOccurrenceAdjacency.Anywhere
                )
            );

        WhyNotResult result = Why(g, "kats", new ExpectedMorph("kat", 0), new ExpectedMorph("PL", 0));

        Failure failure = result.Results[0].Failure;
        Assert.That(failure.Kind, Is.EqualTo("adhoc_prohibition"));
        Assert.That(failure.HcReason, Is.EqualTo("AllomorphCoOccurrenceRules"));
        Assert.That(failure.Morphs.Select(m => m.MorphemeId), Is.EqualTo(new[] { "kat", "OTHER" }));
        Assert.That(plural.Allomorphs.Count, Is.EqualTo(1));
    }

    [Test]
    public void RequiredStemName_ReportsTheFeatureDiff()
    {
        Grammar g = NewGrammar();
        LexEntry entry = g.AddEntry("sak", g.Verb("2"), "sak", "sat");
        entry.Allomorphs[1].StemName = new StemName(g.Verb("1")) { Name = "first" };

        WhyNotResult result = Why(g, "sat", new ExpectedMorph("sak", 1));

        Assert.That(result.Outcome, Is.EqualTo(WhyNotOutcome.Rejected));
        Failure failure = result.Results[0].Failure;
        Assert.That(failure.Kind, Is.EqualTo("stem_name"));
        Assert.That(failure.HcReason, Is.EqualTo("RequiredStemName"));
        Assert.That(failure.StemName!.Name, Is.EqualTo("first"));
        Assert.That(
            failure.Features.Select(f => (f.Path, f.Required, f.Actual)),
            Is.EqualTo(new[] { ("head/pers", "1", "2") })
        );
    }

    [Test]
    public void ExcludedStemName_NamesTheOtherAllomorphsStemName()
    {
        Grammar g = NewGrammar();
        LexEntry entry = g.AddEntry("sak", g.Verb("1"), "sak", "sat");
        entry.Allomorphs[1].StemName = new StemName(g.Verb("1")) { Name = "first" };

        WhyNotResult result = Why(g, "sak", new ExpectedMorph("sak", 0));

        Failure failure = result.Results[0].Failure;
        Assert.That(failure.Kind, Is.EqualTo("stem_name"));
        Assert.That(failure.HcReason, Is.EqualTo("ExcludedStemName"));
        Assert.That(failure.StemName!.Name, Is.EqualTo("first"));
        Assert.That(failure.Morphs.Single().AllomorphIndex, Is.EqualTo(0));
    }

    [Test]
    public void BoundRoot_ReportsStemNameKind()
    {
        Grammar g = NewGrammar();
        LexEntry entry = g.AddEntry("sak", g.Verb());
        entry.Allomorphs.Add(new RootAllomorph(new Segments(g.Table, "sak", true)) { IsBound = true });

        WhyNotResult result = Why(g, "sak", new ExpectedMorph("sak", null));

        Failure failure = result.Results[0].Failure;
        Assert.That(failure.Kind, Is.EqualTo("stem_name"));
        Assert.That(failure.HcReason, Is.EqualTo("BoundRoot"));
    }

    [Test]
    public void ValidAnalysis_IsAccepted()
    {
        Grammar g = NewGrammar();
        g.AddEntry("kat", g.Verb(), "kat");
        g.AddSuffix("PL", "s");

        WhyNotResult result = Why(g, "kats", new ExpectedMorph("kat", null), new ExpectedMorph("PL", null));

        Assert.That(result.Outcome, Is.EqualTo(WhyNotOutcome.Accepted));
        Assert.That(result.Analysis!.Select(m => m.MorphemeId), Is.EqualTo(new[] { "kat", "PL" }));
    }

    [Test]
    public void UnrelatedExpectation_IsUnknownNotRejected()
    {
        Grammar g = NewGrammar();
        g.AddEntry("kat", g.Verb(), "kat");

        WhyNotResult result = Why(g, "kat", new ExpectedMorph("nope", null));

        Assert.That(result.Outcome, Is.EqualTo(WhyNotOutcome.Unknown));
        Assert.That(result.StopReason, Is.EqualTo("no_derivation_reached_validation"));
    }

    private static readonly string AdhocGrammar = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "TestData",
        "WhyNotAdhocProhibition.xml"
    );

    private static GrammarIds IdsFromXml(Language language, string xml)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"whynot-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, xml);
        return GrammarIds.Load(path, language);
    }

    private const string KatXml =
        @"<Language><Stratum>
  <LexicalEntry id='e1'><MorphemeId>kat</MorphemeId>
    <Allomorphs><Allomorph id='a-gone' isActive='no'/><Allomorph id='a-kat'/><Allomorph id='a-kas'/></Allomorphs>
  </LexicalEntry>
  <LexicalEntry id='e2'><MorphemeId>other</MorphemeId>
    <Allomorphs><Allomorph id='o1'/><Allomorph id='o2'/></Allomorphs>
  </LexicalEntry>
  <StemName id='sn1' partsOfSpeech='V'><Name>first</Name></StemName>
  <StemName id='sn2' partsOfSpeech='V'><Name>dup</Name></StemName>
  <StemName id='sn3' partsOfSpeech='V'><Name>dup</Name></StemName>
</Stratum></Language>";

    [Test]
    public void GrammarIds_MapsOnlyMorphemesWhoseAllomorphCountsAgreeWithTheLoader()
    {
        Grammar g = NewGrammar();
        g.AddEntry("kat", g.Verb(), "kat", "kas");
        g.AddEntry("other", g.Verb(), "kat");

        GrammarIds ids = IdsFromXml(g.Language, KatXml);

        Assert.That(ids.AllomorphId("kat", 1), Is.EqualTo("a-kas"));
        Assert.That(ids.AllomorphIndex("kat", "a-kat"), Is.EqualTo(0));
        Assert.That(ids.AllomorphIndex("kat", "nope"), Is.Null);
        Assert.That(ids.AllomorphId("other", 0), Is.Null, "two active allomorph elements, one loaded: unmapped");
        Assert.That(ids.StemNameId(new StemName(FeatureStruct.New().Value) { Name = "first" }), Is.EqualTo("sn1"));
        Assert.That(ids.StemNameId(new StemName(FeatureStruct.New().Value) { Name = "dup" }), Is.Null);
    }

    [Test]
    public void Json_WritesGrammarIdsAndNeverGuids()
    {
        Grammar g = NewGrammar();
        LexEntry kat = g.AddEntry("kat", g.Verb(), "kat", "kas");
        AffixProcessRule plural = g.AddSuffix("PL", "s");
        kat.MorphemeCoOccurrenceRules.Add(
            new MorphemeCoOccurrenceRule(
                ConstraintType.Exclude,
                new Morpheme[] { plural },
                MorphCoOccurrenceAdjacency.Anywhere
            )
        );
        GrammarIds ids = IdsFromXml(g.Language, KatXml);

        string json = WhyNotJson.Serialize(
            Why(g, "kats", new ExpectedMorph("kat", 0), new ExpectedMorph("PL", null)),
            ids
        );

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        Assert.That(root.GetProperty("schema").GetString(), Is.EqualTo("whynot.v1"));
        Assert.That(root.GetProperty("result").GetString(), Is.EqualTo("rejected"));
        JsonElement failure = root.GetProperty("failure");
        Assert.That(failure.GetProperty("kind").GetString(), Is.EqualTo("adhoc_prohibition"));
        JsonElement morphs = failure.GetProperty("morphs");
        Assert.That(morphs[0].GetProperty("allomorph").GetString(), Is.EqualTo("a-kat"));
        Assert.That(morphs[1].GetProperty("morpheme").GetString(), Is.EqualTo("PL"));
        Assert.That(morphs[1].TryGetProperty("allomorph", out _), Is.False, "PL has no mapped id in the snippet");
        Assert.That(root.GetProperty("notes").GetArrayLength(), Is.GreaterThan(0));
        Assert.That(json, Does.Not.Contain("guid").IgnoreCase);
    }

    [Test]
    public void Environment_ReportsTheAllomorphWithoutAnIndex()
    {
        Grammar g = NewGrammar();
        LexEntry kat = g.AddEntry("kat", g.Verb(), "kat", "kas");
        var vowel = FeatureStruct.New(g.Language.PhonologicalFeatureSystem).Symbol("cons-").Value;
        kat.Allomorphs[0]
            .Environments.Add(
                new AllomorphEnvironment(
                    ConstraintType.Require,
                    null,
                    Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
                )
            );

        WhyNotResult result = Why(g, "kat", new ExpectedMorph("kat", 0));

        Assert.That(result.Results[0].Failure.Kind, Is.EqualTo("environment_unmet"));
        Assert.That(result.Results[0].Failure.HcReason, Is.EqualTo("Environments"));
        string json = WhyNotJson.Serialize(result, IdsFromXml(g.Language, KatXml));
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement environment = doc.RootElement.GetProperty("failure").GetProperty("environment");
        Assert.That(environment.GetProperty("allomorph_id").GetString(), Is.EqualTo("a-kat"));
        Assert.That(environment.TryGetProperty("index", out _), Is.False);
    }

    [Test]
    public void FailureWhoseRequiredFieldHasNoId_BecomesAnUnknownRow()
    {
        Grammar g = NewGrammar();
        LexEntry kat = g.AddEntry("kat", g.Verb(), "kat", "kas");
        var vowel = FeatureStruct.New(g.Language.PhonologicalFeatureSystem).Symbol("cons-").Value;
        kat.Allomorphs[0]
            .Environments.Add(
                new AllomorphEnvironment(
                    ConstraintType.Require,
                    null,
                    Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
                )
            );
        WhyNotResult result = Why(g, "kat", new ExpectedMorph("kat", 0));
        Assert.That(result.Results[0].Failure.Kind, Is.EqualTo("environment_unmet"));

        using JsonDocument doc = JsonDocument.Parse(WhyNotJson.Serialize(result, GrammarIds.Empty));

        JsonElement root = doc.RootElement;
        Assert.That(root.GetProperty("result").GetString(), Is.EqualTo("unknown"));
        Assert.That(root.TryGetProperty("failure", out _), Is.False);
        Assert.That(root.GetProperty("stop").GetProperty("reason").GetString(), Is.EqualTo("failure_not_expressible"));
        Assert.That(root.GetProperty("notes").GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public void RequireWithAdjacencyFailingOnTheWrongSide_DoesNotThrow()
    {
        Grammar g = NewGrammar();
        LexEntry kat = g.AddEntry("kat", g.Verb(), "kat");
        AffixProcessRule plural = g.AddSuffix("PL", "s");
        kat.Allomorphs[0]
            .AllomorphCoOccurrenceRules.Add(
                new AllomorphCoOccurrenceRule(
                    ConstraintType.Require,
                    new[] { plural.Allomorphs[0] },
                    MorphCoOccurrenceAdjacency.SomewhereToLeft
                )
            );

        WhyNotResult result = Why(g, "kats", new ExpectedMorph("kat", 0), new ExpectedMorph("PL", 0));

        Failure failure = result.Results[0].Failure;
        Assert.That(failure.Kind, Is.EqualTo("adhoc_prohibition"));
        Assert.That(failure.Morphs.Select(m => m.MorphemeId), Is.EqualTo(new[] { "kat", "PL" }));
    }

    [Test]
    public void AllomorphRequiredSyntacticFeatures_ReportFeatureClashWithTheDiff()
    {
        Grammar g = NewGrammar();
        g.AddEntry("kat", g.Verb(), "kat");
        AffixProcessRule plural = g.AddSuffix("PL", "s");
        plural.Allomorphs[0].RequiredSyntacticFeatureStruct = FeatureStruct.New(g.Syntax).Symbol("N").Value;

        WhyNotResult result = Why(g, "kats", new ExpectedMorph("kat", 0), new ExpectedMorph("PL", 0));

        Assert.That(result.Outcome, Is.EqualTo(WhyNotOutcome.Rejected));
        Failure failure = result.Results[0].Failure;
        Assert.That(failure.Kind, Is.EqualTo("feature_clash"));
        Assert.That(failure.HcReason, Is.EqualTo("RequiredSyntacticFeatureStruct"));
        Assert.That(failure.Features, Is.Not.Empty);
        Assert.That(failure.Morphs.Single().MorphemeId, Is.EqualTo("PL"));
    }

    [Test]
    public void Json_UnspecifiedActualValueIsMarked()
    {
        var failure = new Failure(
            "feature_clash",
            "x",
            null,
            "ObligatorySyntacticFeatures",
            "hc_reported",
            Array.Empty<MorphRef>(),
            null,
            null,
            new[] { new FeatureMismatch("head/tense", "specified", null) }
        );
        var result = new WhyNotResult(
            "w",
            new[] { new ExpectedMorph("a", null) },
            WhyNotOutcome.Rejected,
            null,
            new[] { new DerivationResult(Array.Empty<MorphRef>(), failure, 0) },
            null
        );

        using JsonDocument doc = JsonDocument.Parse(WhyNotJson.Serialize(result, GrammarIds.Empty));

        JsonElement feature = doc.RootElement.GetProperty("failure").GetProperty("features")[0];
        Assert.That(feature.GetProperty("actual").GetString(), Is.EqualTo("(unspecified)"));
    }

    [Test]
    public void Cli_BatchAnswersEveryQueryAndTurnsBadOnesIntoUnknownRows()
    {
        string dir = TestContext.CurrentContext.WorkDirectory;
        string queries = Path.Combine(dir, $"whynot-q-{Guid.NewGuid():N}.jsonl");
        string output = Path.Combine(dir, $"whynot-o-{Guid.NewGuid():N}.jsonl");
        File.WriteAllLines(
            queries,
            new[]
            {
                @"{""word"":""tada"",""expect"":[{""morpheme"":""TA1"",""allomorph"":""subTa1a""},{""morpheme"":""DA"",""allomorph"":""aDa""}]}",
                @"{""word"":""da"",""expect"":[{""morpheme"":""DA"",""allomorph"":""aDa""}]}",
                @"{""word"":""da"",""expect"":[{""morpheme"":""DA"",""allomorph"":""nope""}]}",
                @"{""word"":""da""}",
                "not json",
                @"{""word"":""ba"",""expect"":[{""morpheme"":""BA"",""allomorph"":""aBa""}]}",
            }
        );

        int exit = WhyNotCli.Run(new[] { "why-not", AdhocGrammar, queries, output });

        Assert.That(exit, Is.EqualTo(0));
        JsonElement[] rows = File.ReadAllLines(output).Select(l => JsonDocument.Parse(l).RootElement).ToArray();
        Assert.That(rows, Has.Length.EqualTo(6));
        Assert.That(rows[0].GetProperty("result").GetString(), Is.EqualTo("rejected"));
        JsonElement failure = rows[0].GetProperty("failure");
        Assert.That(failure.GetProperty("kind").GetString(), Is.EqualTo("adhoc_prohibition"));
        Assert.That(failure.GetProperty("hc_reason").GetString(), Is.EqualTo("AllomorphCoOccurrenceRules"));
        Assert.That(
            failure.GetProperty("morphs").EnumerateArray().Select(m => m.GetProperty("allomorph").GetString()),
            Is.EqualTo(new[] { "subTa1a", "aDa" })
        );
        Assert.That(rows[1].GetProperty("result").GetString(), Is.EqualTo("accepted"));
        Assert.That(rows[2].GetProperty("result").GetString(), Is.EqualTo("unknown"));
        Assert.That(rows[2].GetProperty("stop").GetProperty("reason").GetString(), Is.EqualTo("unknown_allomorph_id"));
        Assert.That(rows[2].GetProperty("expect")[0].GetProperty("allomorph").GetString(), Is.EqualTo("nope"));
        Assert.That(rows[3].GetProperty("stop").GetProperty("reason").GetString(), Does.StartWith("error:"));
        Assert.That(rows[4].GetProperty("stop").GetProperty("reason").GetString(), Does.StartWith("error:"));
    }

    [Test]
    public void Cli_BadArgumentsAndMissingGrammarHaveDistinctExitCodes()
    {
        Assert.That(WhyNotCli.Run(new[] { "why-not" }), Is.EqualTo(2));
        Assert.That(WhyNotCli.Run(new[] { "why-not", "missing.xml", "q", "o" }), Is.EqualTo(1));
    }
}
