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

    // Keeps compound and affix unapplication on the same unordered cascade.
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
        // Both rule types exercise the sequential and parallel cascade paths.
        AddCompoundingAndPrefixRules();

        string[] words = { "pʰutdidat", "pʰutdat" };
        AssertSingleThreadedMatchesParallel(words);
        // Absolute, so a bug that empties both sides identically is still caught. pʰutdidat has no parse here,
        // so only pʰutdat (two compound analyses) is compared non-trivially.
        Assert.That(new Morpher(TraceManager, Language).ParseWord("pʰutdat"), Is.Not.Empty);
    }

    [Test]
    public void ParseWord_SingleThreaded_MatchesParallel_ForSelfOpaquingSimultaneousEpenthesis()
    {
        // AnalysisRewriteRule compiles a Simultaneous-mode epenthesis rule as ReapplyType.SelfOpaquing, a
        // repeat-until-fixpoint loop. No fixture here drives the loop past one iteration.
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

        AssertSingleThreadedMatchesParallel("buibui", "bubu", "bibu");
        // Absolute, so a bug that empties both sides identically is still caught.
        Assert.That(new Morpher(TraceManager, Language).ParseWord("buibui").Count(), Is.EqualTo(1));
    }

    [Test]
    public void ParseWord_SingleThreaded_MatchesParallel_WithAffixTemplate()
    {
        AddAffixTemplateAndCommutingPrefixes();

        AssertSingleThreadedMatchesParallel("digusagd", "disagd", "gusagd", "sagd", "sag");
        Assert.That(new Morpher(TraceManager, Language).ParseWord("digusagd"), Is.Not.Empty);
    }

    [Test]
    public void ParseWord_SingleThreaded_MatchesParallel_OnLinearStratumWithAffixTemplate()
    {
        AddAffixTemplateAndCommutingPrefixes();
        SetRuleOrder(MorphologicalRuleOrder.Linear);

        AssertSingleThreadedMatchesParallel("digusagd", "gudisagd", "disagd", "gusagd", "sagd", "sag");
        // With the Linear order the di and gu prefixes apply only in rule order, so gudisagd parses and
        // digusagd has no parse. The parity check on digusagd is only an empty-equals-empty comparison.
        var parallel = new Morpher(TraceManager, Language);
        Assert.That(parallel.ParseWord("digusagd"), Is.Empty);
        foreach (string word in new[] { "gudisagd", "disagd", "gusagd", "sagd", "sag" })
            Assert.That(parallel.ParseWord(word), Is.Not.Empty, $"'{word}' must parse");
    }

    [TestCase(MorphologicalRuleOrder.Unordered, 0)]
    [TestCase(MorphologicalRuleOrder.Unordered, 100)]
    [TestCase(MorphologicalRuleOrder.Linear, 0)]
    [TestCase(MorphologicalRuleOrder.Linear, 100)]
    public void ParseWords_PreservesInputOrderAndSingleWordAnalysisSets(MorphologicalRuleOrder ruleOrder, int limit)
    {
        AddAffixTemplateAndCommutingPrefixes();
        SetRuleOrder(ruleOrder);
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1) { MaxAlternatives = limit };
        string[] words = { "gudisagd", "sag", "digusagd", "sagt", "disagd", "sag" };
        string[][] expected = words
            .Select(w => morpher.ParseWord(w).Select(BatchAnalysisSignature).ToArray())
            .ToArray();

        foreach (int concurrency in new[] { 1, 4 })
        {
            IReadOnlyList<WordParseResult> results = morpher.ParseWords(words, concurrency);

            Assert.That(results.Select(r => r.SurfaceForm), Is.EqualTo(words));
            for (int i = 0; i < words.Length; i++)
            {
                Assert.That(results[i].Error, Is.Null);
                Assert.That(results[i].Trace, Is.Null);
                Assert.That(results[i].Analyses.Select(BatchAnalysisSignature), Is.EquivalentTo(expected[i]));
            }
            Assert.That(results[1].Analyses, Is.Not.Empty);
        }
    }

    [TestCase(MorphologicalRuleOrder.Unordered, 0)]
    [TestCase(MorphologicalRuleOrder.Unordered, 1)]
    [TestCase(MorphologicalRuleOrder.Unordered, 2)]
    [TestCase(MorphologicalRuleOrder.Linear, 0)]
    [TestCase(MorphologicalRuleOrder.Linear, 1)]
    [TestCase(MorphologicalRuleOrder.Linear, 2)]
    public void ParseWord_MaxAlternativesCanChangeBetweenCalls(MorphologicalRuleOrder ruleOrder, int degree)
    {
        AddAffixTemplateAndCommutingPrefixes();
        SetRuleOrder(ruleOrder);
        var morpher = new Morpher(TraceManager, Language, degree);
        string[] expected = morpher.ParseWord("gudisagd").Select(BatchAnalysisSignature).ToArray();
        Assert.That(expected, Is.Not.Empty);

        foreach (int limit in new[] { 1, 100, 1, 0 })
        {
            morpher.MaxAlternatives = limit;
            if (limit == 1)
                Assert.Throws<MaxAlternativesExceededException>(() => morpher.ParseWord("gudisagd"));
            else
                Assert.That(morpher.ParseWord("gudisagd").Select(BatchAnalysisSignature), Is.EquivalentTo(expected));
        }
    }

    [Test]
    public void CompileAnalysisRule_DirectStratumInvocationRemainsUncapped()
    {
        AddAffixTemplateAndCommutingPrefixes();
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1) { MaxAlternatives = 1 };
        IRule<Word, ShapeNode> rule = Morphophonemic.CompileAnalysisRule(morpher);
        var input = new Word(Morphophonemic, Table3.Segment("gudisagd"));
        input.Freeze();

        Assert.That(rule.Apply(input), Is.Not.Empty);
        Assert.Throws<MaxAlternativesExceededException>(() => morpher.ParseWord("gudisagd"));
    }

    [Test]
    public void ParseWords_MatchesSingleWordAnalysisSetsWithCompounding()
    {
        AddCompoundingAndPrefixRules();
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        string[] words = { "pʰutdidat", "pʰutdat", "pʰutdat" };

        foreach (int concurrency in new[] { 1, 4 })
        {
            IReadOnlyList<WordParseResult> results = morpher.ParseWords(words, concurrency);
            Assert.That(results.Select(r => r.SurfaceForm), Is.EqualTo(words));
            for (int i = 0; i < words.Length; i++)
            {
                Assert.That(results[i].Error, Is.Null);
                Assert.That(
                    results[i].Analyses.Select(BatchAnalysisSignature),
                    Is.EquivalentTo(morpher.ParseWord(words[i]).Select(BatchAnalysisSignature))
                );
            }
            Assert.That(results[1].Analyses, Has.Count.EqualTo(2));
        }
    }

    [TestCase(2)]
    [TestCase(4)]
    public void ParseWords_UsesBoundedWordConcurrency(int concurrency)
    {
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        using var entered = new CountdownEvent(concurrency);
        int calls = 0;
        int active = 0;
        int peak = 0;
        object sync = new();
        morpher.LexEntrySelector = _ =>
        {
            int current = Interlocked.Increment(ref active);
            try
            {
                lock (sync)
                    peak = Math.Max(peak, current);
                if (Interlocked.Increment(ref calls) <= concurrency)
                    entered.Signal();
                if (!entered.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("The requested word workers did not enter lexical lookup.");
                return true;
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        };
        string[] words = Enumerable.Repeat("sag", 8).ToArray();

        IReadOnlyList<WordParseResult> results =
            concurrency == 4 ? morpher.ParseWords(words) : morpher.ParseWords(words, concurrency);

        Assert.That(results.Select(r => r.Error), Is.All.Null);
        Assert.That(results.Select(r => r.Analyses.Count), Is.All.GreaterThan(0));
        Assert.That(peak, Is.EqualTo(concurrency));
    }

    [TestCase(1)]
    [TestCase(4)]
    public void ParseWords_CapturesInvalidShapesWithoutLosingOtherResults(int concurrency)
    {
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        string[] words = { "sag", "sag$", "sagt", "sag" };

        IReadOnlyList<WordParseResult> results = morpher.ParseWords(words, concurrency);

        Assert.That(results.Select(r => r.SurfaceForm), Is.EqualTo(words));
        Assert.That(results[1].Error, Is.TypeOf<InvalidShapeException>());
        Assert.That(results[1].Analyses, Is.Empty);
        Assert.That(results[1].Trace, Is.Null);
        Assert.That(results[2].Error, Is.Null);
        Assert.That(results[2].Analyses, Is.Empty);
        foreach (int index in new[] { 0, 3 })
        {
            Assert.That(results[index].Error, Is.Null);
            Assert.That(results[index].Analyses, Is.Not.Empty);
        }
    }

    [TestCase(1)]
    [TestCase(4)]
    public void ParseWords_CapturesMaxAlternativesPerWord(int concurrency)
    {
        AddAffixTemplateAndCommutingPrefixes();
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1) { MaxAlternatives = 1 };

        IReadOnlyList<WordParseResult> results = morpher.ParseWords(
            new[] { "gudisagd", "sag", "digusagd", "sag" },
            concurrency
        );

        foreach (int index in new[] { 0, 2 })
        {
            Assert.That(results[index].Error, Is.TypeOf<MaxAlternativesExceededException>());
            Assert.That(results[index].Analyses, Is.Empty);
        }
        foreach (int index in new[] { 1, 3 })
        {
            Assert.That(results[index].Error, Is.Null);
            Assert.That(results[index].Analyses, Is.Not.Empty);
        }

        morpher.MaxAlternatives = 0;
        Assert.That(morpher.ParseWords(new[] { "gudisagd" }, concurrency)[0].Analyses, Is.Not.Empty);
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(2)]
    public void ParseWords_RequiresSequentialMorpher(int degree)
    {
        var morpher = new Morpher(TraceManager, Language, degree);

        var error = Assert.Throws<InvalidOperationException>(() => morpher.ParseWords(new[] { "sag" }));

        Assert.That(error!.Message, Does.Contain("maxDegreeOfParallelism: 1"));
        Assert.That(morpher.ParseWord("sag"), Is.Not.Empty);
    }

    [Test]
    public void ParseWords_ValidatesBatchArguments()
    {
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        Assert.Throws<ArgumentNullException>(() => morpher.ParseWords(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => morpher.ParseWords(new[] { "sag" }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => morpher.ParseWords(new[] { "sag" }, -1));
        Assert.That(morpher.ParseWords(Array.Empty<string>()), Is.Empty);
    }

    [Test]
    public void ParseWords_EnumeratesInputOnceAndPropagatesEnumerationErrors()
    {
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        int enumerations = 0;
        IEnumerable<string> Inputs()
        {
            enumerations++;
            yield return "sag";
            yield return "sag";
        }
        var error = new InvalidOperationException("Input enumeration failed.");
        IEnumerable<string> BrokenInputs()
        {
            yield return "sag";
            throw error;
        }

        Assert.That(morpher.ParseWords(Inputs()), Has.Count.EqualTo(2));
        Assert.That(enumerations, Is.EqualTo(1));
        Assert.That(
            Assert.Throws<InvalidOperationException>(() => morpher.ParseWords(BrokenInputs())),
            Is.SameAs(error)
        );
    }

    [Test]
    public void ParseWords_GuessesRootsWhenRequested()
    {
        var naturalClass = new NaturalClass(new FeatureStruct()) { Name = "BatchAny" };
        Morphophonemic.CharacterDefinitionTable.AddNaturalClass(naturalClass);
        LexEntry pattern = AddEntry("batch_pattern", new FeatureStruct(), Morphophonemic, "[BatchAny]*");
        try
        {
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
            string[] words = { "gag", "sag", "gug" };
            Assert.That(morpher.ParseWords(words)[0].Analyses, Is.Empty);

            foreach (int concurrency in new[] { 1, 4 })
            {
                IReadOnlyList<WordParseResult> results = morpher.ParseWords(words, concurrency, guessRoot: true);
                for (int i = 0; i < words.Length; i++)
                {
                    Assert.That(results[i].Error, Is.Null);
                    Assert.That(results[i].Analyses, Is.Not.Empty);
                    Assert.That(
                        results[i].Analyses.Select(BatchAnalysisSignature),
                        Is.EquivalentTo(morpher.ParseWord(words[i], out _, true).Select(BatchAnalysisSignature))
                    );
                }
                Assert.That(results[0].Analyses.All(w => w.RootAllomorph.Guessed), Is.True);
            }
        }
        finally
        {
            Morphophonemic.Entries.Remove(pattern);
            Entries.Remove("batch_pattern");
        }
    }

    [Test]
    public void ParseWords_TracingRunsOnCallingThreadAndRetainsPerWordTraces()
    {
        AddAffixTemplateAndCommutingPrefixes();
        TraceManager.IsTracing = true;
        try
        {
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
            var error = new InvalidOperationException("First parse failed.");
            int callingThread = Environment.CurrentManagedThreadId;
            int offThreadCalls = 0;
            int calls = 0;
            morpher.RuleSelector = _ =>
            {
                if (Environment.CurrentManagedThreadId != callingThread)
                    Interlocked.Increment(ref offThreadCalls);
                if (Interlocked.Increment(ref calls) == 1)
                    throw error;
                return true;
            };

            IReadOnlyList<WordParseResult> results = morpher.ParseWords(new[] { "sag", "digusagd", "sag" }, 4);

            Assert.That(offThreadCalls, Is.Zero);
            Assert.That(results[0].Error, Is.SameAs(error));
            Assert.That(results[0].Analyses, Is.Empty);
            Assert.That(results.Select(r => r.Trace), Is.All.InstanceOf<Trace>());
            Assert.That(results.Select(r => r.Trace).Distinct().Count(), Is.EqualTo(3));
            foreach (int index in new[] { 1, 2 })
            {
                Assert.That(results[index].Error, Is.Null);
                Assert.That(results[index].Analyses, Is.Not.Empty);
                Assert.That(
                    results[index].Analyses.Select(BatchAnalysisSignature),
                    Is.EquivalentTo(morpher.ParseWord(results[index].SurfaceForm).Select(BatchAnalysisSignature))
                );
            }
        }
        finally
        {
            TraceManager.IsTracing = false;
        }
    }

    private string BatchAnalysisSignature(Word word)
    {
        static string AllomorphKey(Allomorph allomorph) => $"{allomorph.Morpheme.Id}:{allomorph.Index}";
        return string.Join(
            "|",
            new[]
            {
                AllomorphKey(word.RootAllomorph),
                string.Join(",", word.MorphologicalRules.Select(r => r.Name)),
                string.Join(",", word.Allomorphs.Select(AllomorphKey).OrderBy(s => s, StringComparer.Ordinal)),
                string.Join(",", word.AllomorphsInMorphOrder.Select(AllomorphKey)),
                word.Shape.ToString(Language.SurfaceStratum.CharacterDefinitionTable, false),
                word.SyntacticFeatureStruct.ToString(),
                word.RealizationalFeatureStruct.ToString(),
            }
        );
    }

    private void AssertSingleThreadedMatchesParallel(params string[] words)
    {
        var parallel = new Morpher(TraceManager, Language);
        var singleThreaded = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        foreach (string word in words)
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

    // Two prefixes that commute on an Unordered stratum, so one word is reached by more than one unapplication
    // order. On a Linear stratum they apply only in rule order, so only gudisagd parses.
    private void AddAffixTemplateAndCommutingPrefixes()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var verb = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "TPAST",
            Name = "template_ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = verb,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        var verbTemplate = new AffixTemplate { Name = "verb_template", RequiredSyntacticFeatureStruct = verb };
        verbTemplate.Slots.Add(new AffixTemplateSlot(edSuffix) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);

        foreach (
            (string id, string name, string gloss, string prefix) in new[]
            {
                ("TDI", "template_di_prefix", "DI", "di+"),
                ("TGU", "template_gu_prefix", "GU", "gu+"),
            }
        )
        {
            var prefixRule = new AffixProcessRule
            {
                Id = id,
                Name = name,
                Gloss = gloss,
                RequiredSyntacticFeatureStruct = verb,
            };
            prefixRule.Allomorphs.Add(
                new AffixProcessAllomorph
                {
                    Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                    Rhs = { new InsertSegments(Table3, prefix), new CopyFromInput("1") },
                }
            );
            Morphophonemic.MorphologicalRules.Add(prefixRule);
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

        // Absolute, so a bug that empties both capped and unbounded results identically is still caught.
        // pʰutdidat has no parse here, so only pʰutdat (two analyses) is compared non-trivially.
        Assert.That(unbounded.ParseWord("pʰutdat"), Is.Not.Empty);
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

    // Include allomorph identity, application order, and root identity in result comparisons.
    internal static string WordAnalysisSignature(Word word)
    {
        return string.Join("+", word.AllomorphsInMorphOrder.Select(a => a.Morpheme.Id))
            + "|"
            + string.Join("+", word.MorphemesInApplicationOrder.Select(m => m.Id))
            + "|root="
            + word.RootAllomorph.Morpheme.Id;
    }
}
