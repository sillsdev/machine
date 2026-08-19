using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class MorpherTests : HermitCrabTestBase
{
    [Test]
    public void AnalyzeWord_CanAnalyze_ReturnsCorrectAnalysis()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        Assert.That(
            morpher.AnalyzeWord("sagd"),
            Is.EquivalentTo(new[] { new WordAnalysis(new IMorpheme[] { Entries["32"], edSuffix }, 0, "V") })
        );
    }

    [Test]
    public void AnalyzeWord_MaxAlternatives()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);
        var gSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "g_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        gSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+g") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(gSuffix);

        var morpher = new Morpher(TraceManager, Language);
        morpher.MaxAlternatives = 1;

        Assert.Throws<MaxAlternativesExceededException>(() => morpher.AnalyzeWord("sagd"));
    }

    [Test]
    public void AnalyzeWord_CanAnalyzeLinear_ReturnsCorrectAnalysis()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        // Adding rules shouldn't block sagd analysis when Linear.
        var tSuffix = new AffixProcessRule
        {
            Id = "PLURAL",
            Name = "t_suffix",
            Gloss = "PLURAL",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        tSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+t") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(tSuffix);

        // Add a phonological rule so that "sagd" becomes "sag[dt]" during unapplication.
        // This is to verify that unapplication works correctly.
        var rule1 = new RewriteRule
        {
            Name = "rule1",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
        };
        rule1.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "d")).Value }
        );
        Morphophonemic.PhonologicalRules.Add(rule1);

        SetRuleOrder(MorphologicalRuleOrder.Linear);
        var morpher = new Morpher(TraceManager, Language);

        Assert.That(
            morpher.AnalyzeWord("sagd"),
            Is.EquivalentTo(new[] { new WordAnalysis(new IMorpheme[] { Entries["32"], edSuffix }, 0, "V") })
        );
    }

    [Test]
    public void ParseWord_UnblockedRealizationalRuleMatchesOwnOutput_DoesNotHang()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        LexEntry entry = AddEntry(
            "realtest",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Morphophonemic,
            "zag"
        );
        entry.MprFeatures.Add(Latinate);

        var realRule = new RealizationalAffixProcessRule { Name = "real_rule", Gloss = "REAL" };
        realRule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "d") },
            }
        );
        realRule.Allomorphs[0].RequiredMprFeatures.Add(Latinate);
        Morphophonemic.MorphologicalRules.Add(realRule);

        SetRuleOrder(MorphologicalRuleOrder.Linear);
        var morpher = new Morpher(TraceManager, Language);

        Word[]? output = null;
        bool completed = Task.Run(() => output = morpher.ParseWord("zag").ToArray()).Wait(TimeSpan.FromSeconds(10));

        Assert.That(
            completed,
            Is.True,
            "ParseWord did not return: the realizational rule reapplied to its own output without bound."
        );
        AssertMorphsEqual(output!, "realtest");
    }

    [Test]
    public void AnalyzeWord_CannotAnalyze_ReturnsEmptyEnumerable()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        Assert.That(morpher.AnalyzeWord("sagt"), Is.Empty);
    }

    [Test]
    public void AnalyzeWord_CannotAnalyzeDueToAllomorphCooccurenceFailure_ReturnsEmptyEnumerable()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        // Create co-occurence rule blocking sag +d
        // get sag root
        var sag = Language.Strata[0].Entries.ElementAt(6);
        // create +ed suffix
        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var edAllo = edSuffix.GetAllomorph(0);

        // create co-occurence rule which blocks the analysis
        var other1 = new List<Allomorph> { edAllo };
        var rule1 = new AllomorphCoOccurrenceRule(ConstraintType.Exclude, other1, MorphCoOccurrenceAdjacency.Anywhere);
        var sagAllo = sag.GetAllomorph(0);
        sagAllo.AllomorphCoOccurrenceRules.Add(rule1);
        var morpher = new Morpher(TraceManager, Language);

        Assert.That(morpher.AnalyzeWord("sagd"), Is.Empty);

        // In FLEx, clitics occur as both a stem and an affix.
        // LT-22156 notes that they can be ignored when they occur in a co-occurrence rule
        // FLEx produces two rules for the key morpheme, one with the "other" using an affix and one using a stem
        // Now create co-occurence rule blocking sag =d
        var syntacticFeatSys = new SyntacticFeatureSystem();
        syntacticFeatSys.AddPartsOfSpeech(
            new FeatureSymbol("N", "Noun"),
            new FeatureSymbol("V", "Verb"),
            new FeatureSymbol("TV", "Transitive Verb"),
            new FeatureSymbol("IV", "Intransitive Verb"),
            new FeatureSymbol("A", "Adjective")
        );
        syntacticFeatSys.Freeze();
        AddEntry("dEnclitic", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "d");
        var edEnclitic = Language.Strata[0].Entries.ElementAt(42);
        var edEncliticAllo = edEnclitic.GetAllomorph(0);
        var other2 = new List<Allomorph> { edEncliticAllo };
        var rule2 = new AllomorphCoOccurrenceRule(ConstraintType.Exclude, other2, MorphCoOccurrenceAdjacency.Anywhere);
        sagAllo.AllomorphCoOccurrenceRules.Add(rule2);

        Assert.That(morpher.AnalyzeWord("sagd"), Is.Empty);
    }

    [Test]
    public void AnalyzeWord_CannotAnalyzeDueToMorphemeCooccurenceFailure_ReturnsEmptyEnumerable()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        // Create co-occurence rule blocking sag +d
        // get sag root
        var sag = Language.Strata[0].Entries.ElementAt(6);
        // create +ed suffix
        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        // create co-occurence rule which blocks the analysis
        var other1 = new List<Morpheme> { edSuffix };
        var rule1 = new MorphemeCoOccurrenceRule(ConstraintType.Exclude, other1, MorphCoOccurrenceAdjacency.Anywhere);
        sag.MorphemeCoOccurrenceRules.Add(rule1);
        var morpher = new Morpher(TraceManager, Language);

        Assert.That(morpher.AnalyzeWord("sagd"), Is.Empty);

        // In FLEx, clitics occur as both a stem and an affix.
        // LT-22156 notes that they can be ignored when they occur in a co-occurrence rule
        // FLEx produces two rules for the key morpheme, one with the "other" using an affix and one using a stem
        // Now create co-occurence rule blocking sag =d
        var syntacticFeatSys = new SyntacticFeatureSystem();
        syntacticFeatSys.AddPartsOfSpeech(
            new FeatureSymbol("N", "Noun"),
            new FeatureSymbol("V", "Verb"),
            new FeatureSymbol("TV", "Transitive Verb"),
            new FeatureSymbol("IV", "Intransitive Verb"),
            new FeatureSymbol("A", "Adjective")
        );
        syntacticFeatSys.Freeze();
        AddEntry("dEnclitic", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "d");
        var edEnclitic = Language.Strata[0].Entries.ElementAt(42);
        var other2 = new List<Morpheme> { edEnclitic };
        var rule2 = new MorphemeCoOccurrenceRule(ConstraintType.Exclude, other2, MorphCoOccurrenceAdjacency.Anywhere);
        sag.MorphemeCoOccurrenceRules.Add(rule2);

        Assert.That(morpher.AnalyzeWord("sagd"), Is.Empty);
    }

    [Test]
    public void AnalyzeWord_CanGuess_ReturnsCorrectAnalysis()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var naturalClass = new NaturalClass(new FeatureStruct()) { Name = "Any" };
        Morphophonemic.CharacterDefinitionTable.AddNaturalClass(naturalClass);
        AddEntry("pattern", new FeatureStruct(), Morphophonemic, "[Any]*");

        var morpher = new Morpher(TraceManager, Language);

        Assert.That(morpher.AnalyzeWord("gag"), Is.Empty);
        Assert.That(morpher.AnalyzeWord("gagd"), Is.Empty);
        var analyses = morpher.AnalyzeWord("gag", true).ToList();
        Assert.That(analyses[0].ToString(), Is.EquivalentTo("[*gag]"));
        var analyses2 = morpher.AnalyzeWord("gagd", true).ToList();
        Assert.That(analyses2[0].ToString(), Is.EquivalentTo("[*gag ed_suffix]"));
    }

    [Test]
    public void GenerateWords_CanGenerate_ReturnsCorrectWord()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var siPrefix = new AffixProcessRule
        {
            Id = "3SG",
            Name = "si_prefix",
            Gloss = "3SG",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        siPrefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "si+"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(siPrefix);

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+ɯd") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        var analysis = new WordAnalysis(new IMorpheme[] { siPrefix, Entries["33"], edSuffix }, 1, "V");

        string[] words = morpher.GenerateWords(analysis).ToArray();
        Assert.That(words, Is.EquivalentTo(new[] { "sisasɯd" }));
    }

    [Test]
    public void GenerateWords_CannotGenerate_ReturnsEmptyEnumerable()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PL",
            Name = "ed_suffix",
            Gloss = "PL",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+ɯd") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        var analysis = new WordAnalysis(new IMorpheme[] { Entries["32"], edSuffix }, 0, "V");
        Assert.That(morpher.GenerateWords(analysis), Is.Empty);
    }

    [Test]
    public void TestMatchNodesWithPattern()
    {
        Morpher morpher = new Morpher(TraceManager, Language);
        Feature feat1 = new StringFeature("1");
        Feature feat2 = new StringFeature("2");
        FeatureValue valueA = new StringFeatureValue("A");
        FeatureValue valueB = new StringFeatureValue("B");
        FeatureStruct fs1A = new FeatureStruct();
        FeatureStruct fs2B = new FeatureStruct();
        fs1A.AddValue(feat1, valueA);
        fs2B.AddValue(feat2, valueB);

        // Test feature matching.
        List<ShapeNode> nodesfs1A = new List<ShapeNode> { new ShapeNode(fs1A) };
        List<ShapeNode> nodesfs2B = new List<ShapeNode> { new ShapeNode(fs2B) };
        var fs1A2B = morpher.MatchNodesWithPattern(nodesfs1A, nodesfs2B);
        Assert.That(
            fs1A2B.ToList()[0][0].Annotation.FeatureStruct.GetValue(feat1).ToString(),
            Is.EqualTo(valueA.ToString())
        );
        Assert.That(
            fs1A2B.ToList()[0][0].Annotation.FeatureStruct.GetValue(feat2).ToString(),
            Is.EqualTo(valueB.ToString())
        );

        IList<ShapeNode> noNodes = GetNodes("");
        IList<ShapeNode> oneNode = GetNodes("a");
        IList<ShapeNode> twoNodes = GetNodes("aa");
        IList<ShapeNode> threeNodes = GetNodes("aaa");
        IList<ShapeNode> fourNodes = GetNodes("aaaa");
        var naturalClass = new NaturalClass(new FeatureStruct()) { Name = "Any" };
        Table2.AddNaturalClass(naturalClass);

        // Test sequences.
        Assert.That(morpher.MatchNodesWithPattern(oneNode, GetNodes("i")), Is.Empty);
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, oneNode),
            Is.EqualTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(twoNodes, twoNodes),
            Is.EquivalentTo(new List<IList<ShapeNode>> { twoNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(threeNodes, threeNodes),
            Is.EquivalentTo(new List<IList<ShapeNode>> { threeNodes })
        );

        // Test optionality.
        IList<ShapeNode> optionalPattern = GetNodes("([Any])");
        Assert.That(
            morpher.MatchNodesWithPattern(noNodes, optionalPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { noNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, optionalPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, optionalPattern), Is.Empty);

        // Test ambiguity.
        // (It is up to the caller to eliminate duplicates.)
        IList<ShapeNode> optionalPattern2 = GetNodes("([Any])([Any])");
        Assert.That(
            morpher.MatchNodesWithPattern(noNodes, optionalPattern2),
            Is.EquivalentTo(new List<IList<ShapeNode>> { noNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, optionalPattern2),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode, oneNode })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(twoNodes, optionalPattern2),
            Is.EquivalentTo(new List<IList<ShapeNode>> { twoNodes })
        );
        Assert.That(morpher.MatchNodesWithPattern(threeNodes, optionalPattern2), Is.Empty);

        // Test Kleene star.
        IList<ShapeNode> starPattern = GetNodes("[Any]*");
        Assert.That(
            morpher.MatchNodesWithPattern(noNodes, starPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { noNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, starPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(twoNodes, starPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { twoNodes })
        );

        // Test Kleene plus look alike ("+" is a boundary marker).
        IList<ShapeNode> plusPattern = GetNodes("[Any]+");
        Assert.That(morpher.MatchNodesWithPattern(noNodes, plusPattern), Is.Empty);
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, plusPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, plusPattern), Is.Empty);
    }

    IList<ShapeNode> GetNodes(string pattern)
    {
        // Use Table2 because it has boundaries defined.
        Shape shape = new Segments(Table2, pattern, true).Shape;
        return shape.GetNodes(shape.Range).ToList();
    }

    // A compounding rule and a commuting PAST prefix as peers in one Unordered cascade, so an equal
    // AnalysisStateKey can be re-arrived at by different unapplication orders.
    private void AddCompoundingAndPrefixRules()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var crule = new CompoundingRule { Name = "rule1" };
        Allophonic.MorphologicalRules.Add(crule);
        crule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
            }
        );

        var prefix = new AffixProcessRule
        {
            Id = "PREFIX",
            Name = "prefix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Feature(Head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value,
        };
        Allophonic.MorphologicalRules.Insert(0, prefix);
        prefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "di+"), new CopyFromInput("1") },
            }
        );
    }

    [Test]
    public void ParseWord_SingleThreaded_MatchesParallel_WithCompounding()
    {
        // MaxDegreeOfParallelism must be a pure no-op on results, independent of the memo it gates.
        AddCompoundingAndPrefixRules();

        var parallel = new Morpher(TraceManager, Language);
        var singleThreaded = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        foreach (string word in new[] { "pʰutdidat", "pʰutdat" })
        {
            List<Word> singleResult = singleThreaded.ParseWord(word).ToList();
            List<Word> parallelResult = parallel.ParseWord(word).ToList();
            Assert.That(
                singleResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal),
                Is.EqualTo(parallelResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal)),
                $"single-threaded parse of '{word}' must match the parallel parse"
            );
        }
    }

    [Test]
    public void ParseWord_MemoOnMatchesMemoOff_HitCounterGuarded_WithCompounding()
    {
        // The standing acceptance gate: analysis-set equality between the memoized sequential cascade and
        // the unmemoized parallel default, kept non-vacuous by the hit-counter assertion at the end.
        AddCompoundingAndPrefixRules();

        var memoOff = new Morpher(TraceManager, Language);
        var memoOn = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        foreach (string word in new[] { "pʰutdidat", "pʰutdat" })
        {
            List<Word> onResult = memoOn.ParseWord(word).ToList();
            List<Word> offResult = memoOff.ParseWord(word).ToList();
            Assert.That(
                onResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal),
                Is.EqualTo(offResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal)),
                $"memo-on parse of '{word}' must be analysis-set identical to memo-off"
            );
        }
        TestContext.Out.WriteLine($"positive hits: {memoOn.MemoHits}, nogood hits: {memoOn.NogoodHits}");
        Assert.That(
            memoOn.MemoHits + memoOn.NogoodHits,
            Is.GreaterThan(0),
            "the memo must actually have hit (positive or nogood) at least once on this grammar -- "
                + "otherwise this test cannot distinguish a working memo from a no-op one"
        );
    }

    [Test]
    public void ParseWord_MemoOnMatchesMemoOff_ForSelfOpaquingSimultaneousEpenthesis()
    {
        // Guards the memo against a Simultaneous-mode epenthesis rule, which AnalysisRewriteRule compiles
        // as ReapplyType.SelfOpaquing -- a repeat-until-fixpoint loop, and the one rule shape whose
        // interaction with the nogood cache has a suspected (never reproduced) bug elsewhere. Known gap:
        // no available fixture drives the loop past a single iteration, so two or more remains untested.
        var highVowel = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons-")
            .Symbol("voc+")
            .Symbol("high+")
            .Value;
        var highFrontUnrndVowel = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons-")
            .Symbol("voc+")
            .Symbol("high+")
            .Symbol("back-")
            .Symbol("round-")
            .Value;

        var rule4 = new RewriteRule { Name = "rule4", ApplicationMode = RewriteApplicationMode.Simultaneous };
        Allophonic.PhonologicalRules.Add(rule4);
        rule4.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value,
            }
        );

        var memoOff = new Morpher(TraceManager, Language);
        var memoOn = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        foreach (string word in new[] { "buibui", "bubu", "bibu" })
        {
            List<Word> onResult = memoOn.ParseWord(word).ToList();
            List<Word> offResult = memoOff.ParseWord(word).ToList();
            Assert.That(
                onResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal),
                Is.EqualTo(offResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal)),
                $"memo-on parse of '{word}' must be analysis-set identical to memo-off"
            );
        }
        // Pinned as an absolute value, not just on-vs-off, so a bug affecting both sides identically
        // (both wrongly returning empty, say) is still caught.
        Assert.That(memoOn.ParseWord("buibui").Count(), Is.EqualTo(1));
    }

    [Test]
    public void ParseWord_MemoOnMatchesMemoOff_HitCounterGuarded_WithAffixTemplate()
    {
        // Two commuting prefixes, not one: a single rule unapplies only once, so no key would ever be
        // re-arrived at and the template memo would never fire. Unapplying di-then-gu or gu-then-di
        // reaches the same key by a different trail order, which is what makes the second one replay.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "TPAST",
            Name = "template_ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        var verbTemplate = new AffixTemplate
        {
            Name = "verb_template",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(edSuffix) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);

        var diPrefix = new AffixProcessRule
        {
            Id = "TDI",
            Name = "template_di_prefix",
            Gloss = "DI",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        diPrefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "di+"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(diPrefix);

        var guPrefix = new AffixProcessRule
        {
            Id = "TGU",
            Name = "template_gu_prefix",
            Gloss = "GU",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        guPrefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "gu+"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(guPrefix);

        var memoOff = new Morpher(TraceManager, Language);
        var memoOn = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        foreach (string word in new[] { "digusagd", "disagd", "gusagd", "sagd", "sag" })
        {
            List<Word> onResult = memoOn.ParseWord(word).ToList();
            List<Word> offResult = memoOff.ParseWord(word).ToList();
            Assert.That(
                onResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal),
                Is.EqualTo(offResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal)),
                $"memo-on parse of '{word}' must be analysis-set identical to memo-off"
            );
        }
        TestContext.Out.WriteLine(
            $"template positive hits: {memoOn.TemplateMemoHits}, "
                + $"template nogood hits: {memoOn.TemplateNogoodHits}"
        );
        // The graft's effect on final signatures is invisible through synthesis, which re-derives rule
        // orderings anyway, so this counter -- not the equality assertions above -- is what proves the
        // memoized path was exercised at all.
        Assert.That(
            memoOn.TemplateMemoHits + memoOn.TemplateNogoodHits,
            Is.GreaterThan(0),
            "the template memo must actually have hit (positive or nogood) at least once on this "
                + "grammar -- otherwise this test cannot distinguish a working memo from a no-op one"
        );
    }

    [Test]
    public void ParseWord_MemoOnMatchesMemoOff_OnLinearStratumWithAffixTemplate()
    {
        // Every other memo test runs on Unordered strata, leaving Linear -- MorphologicalRuleOrder's
        // default -- with no end-to-end equivalence gate. AnalysisStratumRuleTests covers the exclusion
        // that keeps the memo off this path; this covers the results it produces.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "TPAST",
            Name = "template_ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        var verbTemplate = new AffixTemplate
        {
            Name = "verb_template",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(edSuffix) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);

        var diPrefix = new AffixProcessRule
        {
            Id = "TDI",
            Name = "template_di_prefix",
            Gloss = "DI",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        diPrefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "di+"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(diPrefix);

        var guPrefix = new AffixProcessRule
        {
            Id = "TGU",
            Name = "template_gu_prefix",
            Gloss = "GU",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        guPrefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "gu+"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(guPrefix);

        SetRuleOrder(MorphologicalRuleOrder.Linear);
        var memoOff = new Morpher(TraceManager, Language);
        var memoOn = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        foreach (string word in new[] { "digusagd", "disagd", "gusagd", "sagd", "sag" })
        {
            List<Word> onResult = memoOn.ParseWord(word).ToList();
            List<Word> offResult = memoOff.ParseWord(word).ToList();
            Assert.That(
                onResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal),
                Is.EqualTo(offResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal)),
                $"Linear-stratum parse of '{word}' must be analysis-set identical with and without the memo"
            );
        }
    }

    [Test]
    public void ParseWord_HonorsMaxDegreeOfParallelismAsACap_WithoutChangingResults()
    {
        // The value is a resource knob, never a semantic one, so an intermediate cap must not shift results.
        AddCompoundingAndPrefixRules();

        var unbounded = new Morpher(TraceManager, Language);
        var capped = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 2);

        foreach (string word in new[] { "pʰutdidat", "pʰutdat" })
        {
            List<Word> cappedResult = capped.ParseWord(word).ToList();
            List<Word> unboundedResult = unbounded.ParseWord(word).ToList();
            Assert.That(
                cappedResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal),
                Is.EqualTo(unboundedResult.Select(WordAnalysisSignature).OrderBy(s => s, StringComparer.Ordinal)),
                $"a parallelism cap must not change the analysis set for '{word}'"
            );
        }
    }

    [Test]
    public void CreateParallelOptions_MapsTheCapOntoEveryParallelCallSite()
    {
        // The test above shows the cap does not change results, not that it reaches the Parallel.ForEach
        // call sites at all. Observing real thread counts would be flaky, so pin the mapping instead.
        Assert.Multiple(() =>
        {
            Assert.That(
                new Morpher(TraceManager, Language, maxDegreeOfParallelism: 2)
                    .CreateParallelOptions()
                    .MaxDegreeOfParallelism,
                Is.EqualTo(2),
                "a configured cap must reach the call sites, not just gate the sequential path"
            );
            Assert.That(
                new Morpher(TraceManager, Language).CreateParallelOptions().MaxDegreeOfParallelism,
                Is.EqualTo(-1),
                "the default of 0 means unbounded, which is TPL's -1"
            );
            // Synthesize's loop had ProcessorCount as its default before the cap existed; keep it.
            Assert.That(
                new Morpher(TraceManager, Language)
                    .CreateParallelOptions(Environment.ProcessorCount)
                    .MaxDegreeOfParallelism,
                Is.EqualTo(Environment.ProcessorCount)
            );
            Assert.That(
                new Morpher(TraceManager, Language, maxDegreeOfParallelism: 2)
                    .CreateParallelOptions(Environment.ProcessorCount)
                    .MaxDegreeOfParallelism,
                Is.EqualTo(2),
                "a configured cap must win over a call site's own uncapped default"
            );
        });
    }

    // What the memo gates compare, instead of object equality: a replayed Word is not field-for-field
    // identical to a freshly-computed one. MorphemesInApplicationOrder is the load-bearing part, since it
    // walks the trail and non-heads that ReplayOnto rewrites; AllomorphsInMorphOrder alone would miss a
    // broken graft, walking only Shape annotations that ReplayOnto never touches. The root distinguishes
    // analyses that share a morpheme sequence but not a lexical entry.
    internal static string WordAnalysisSignature(Word word)
    {
        return string.Join("+", word.AllomorphsInMorphOrder.Select(a => a.Morpheme.Id))
            + "|"
            + string.Join("+", word.MorphemesInApplicationOrder.Select(m => m.Id))
            + "|root="
            + word.RootAllomorph.Morpheme.Id;
    }
}
