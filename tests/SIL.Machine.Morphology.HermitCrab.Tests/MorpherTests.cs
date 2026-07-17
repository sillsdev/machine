using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+t") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(tSuffix);

        // Add a phonological rule so that "sagd" becomes "sag[dt]" during unapplication.
        // This is to verify that unapplication works correctly.
        var rule1 = new RewriteRule
        {
            Name = "rule1",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        rule1.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value }
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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

    [Test]
    public void ParseWord_SingleThreaded_MatchesParallel_WithCompounding()
    {
        // Stage 1 of memoization.md: pins the new runtime MaxDegreeOfParallelism toggle to be a pure
        // no-op on results before any memoization is wired up. Compounding specifically (not just plain
        // affixes) because it's the mechanism the eventual analysis-cascade memo cares about: an affix
        // rule that commutes with a compounding rule -- both peers in the same Unordered
        // MorphologicalRules cascade -- can revisit an equal AnalysisStateKey via different arrival
        // orders, so this grammar is the one later commits' equivalence tests build on.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var crule = new CompoundingRule { Name = "rule1" };
        Allophonic.MorphologicalRules.Add(crule);
        crule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, int>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, int>.New("nonHead").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "di+"), new CopyFromInput("1") },
            }
        );

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
        // Stage 3 of memoization.md: the mrule-cascade memo is now actually wired in for
        // maxDegreeOfParallelism: 1. This is the standing acceptance gate (memoization.md §5) --
        // analysis-set (signature) equality between the memoized single-threaded cascade and the
        // unmemoized parallel default -- made non-vacuous by asserting the memo's hit counter actually
        // moved, so a memo that silently stopped firing couldn't pass this test by accident.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var crule = new CompoundingRule { Name = "rule1" };
        Allophonic.MorphologicalRules.Add(crule);
        crule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, int>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, int>.New("nonHead").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "di+"), new CopyFromInput("1") },
            }
        );

        var memoOff = new Morpher(TraceManager, Language);
        var memoOn = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        long hitsBefore = MemoizedCombinationRuleCascade.DiagMemoHits;
        long nogoodHitsBefore = MemoizedCombinationRuleCascade.DiagNogoodHits;
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
        TestContext.Out.WriteLine(
            $"positive hits: {MemoizedCombinationRuleCascade.DiagMemoHits - hitsBefore}, "
                + $"nogood hits: {MemoizedCombinationRuleCascade.DiagNogoodHits - nogoodHitsBefore}"
        );
        Assert.That(
            MemoizedCombinationRuleCascade.DiagMemoHits + MemoizedCombinationRuleCascade.DiagNogoodHits,
            Is.GreaterThan(hitsBefore + nogoodHitsBefore),
            "the memo must actually have hit (positive or nogood) at least once on this grammar -- "
                + "otherwise this test cannot distinguish a working memo from a no-op one"
        );
    }

    [Test]
    public void ParseWord_MemoOnMatchesMemoOff_ForSelfOpaquingSimultaneousEpenthesis()
    {
        // Diagnostic pulled forward per memoization.md §5(a): PanGloss's rust conformance suite documents
        // a confirmed C#-oracle nogood-cache bug on this exact fixture shape (a Simultaneous-mode
        // epenthesis rule, which AnalysisRewriteRule compiles with ReapplyType.SelfOpaquing -- a
        // repeat-until-fixpoint loop -- against root 19's boundary-bearing "b+ubu"). A direct
        // reconstruction attempt against this prototype's own AnalysisScope (parse-optimization-archive)
        // did not reproduce a divergence for "buibui" under any tracing/order combination tried, and the
        // real minimal fixture (rust conformance/rewrite/simultaneous-epenthesis) is not present in this
        // checkout to test against directly. Wiring this in as a standing regression case is the
        // practical mitigation: the general memo-on/off equality gate (this test) covers it going
        // forward, and the ≥2-self-opaquing-iteration case remains an open, documented gap (PanGloss's
        // own conclusion too -- no available fixture drives the loop past one iteration).
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
                Rhs = Pattern<Word, int>.New().Annotation(highFrontUnrndVowel).Value,
                LeftEnvironment = Pattern<Word, int>.New().Annotation(highVowel).Value,
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
        // The traced/correct oracle value for "buibui" (PanGloss's rust conformance fixture): root 19,
        // one epenthesized result. Pinned directly, not just compared on-vs-off, so a bug that happened
        // to affect both sides identically (e.g. both wrongly returning empty) would still be caught.
        Assert.That(memoOn.ParseWord("buibui").Count(), Is.EqualTo(1));
    }

    [Test]
    public void ParseWord_MemoOnMatchesMemoOff_HitCounterGuarded_WithAffixTemplate()
    {
        // Stage 4 of memoization.md: exercises the template-battery memo
        // (AnalysisStratumRule.ApplyTemplateBattery) specifically -- TWO free prefix rules that commute
        // with each other and with a template slot suffix. Unapplying di-then-gu vs gu-then-di reaches
        // the same AnalysisStateKey (same shape, same rule MULTISET) via a different trail ORDER, so the
        // second arrival replays the first arrival's stored template outputs with its own trail prefix
        // grafted on (Word.ReplayOnto). One commuting prefix would NOT be enough -- a single rule can
        // only unapply once, so no key would ever be re-arrived at and the memo would never fire.
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "gu+"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(guPrefix);

        var memoOff = new Morpher(TraceManager, Language);
        var memoOn = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        long templateHitsBefore = AnalysisStratumRule.DiagTemplateMemoHits;
        long templateNogoodHitsBefore = AnalysisStratumRule.DiagTemplateNogoodHits;
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
            $"template positive hits: {AnalysisStratumRule.DiagTemplateMemoHits - templateHitsBefore}, "
                + $"template nogood hits: {AnalysisStratumRule.DiagTemplateNogoodHits - templateNogoodHitsBefore}"
        );
        // Guards against this test going vacuous: the template memo's replay path must actually fire
        // for this grammar. (As with the mrule-cascade memo, the ReplayOnto graft's effect on final
        // signatures is unobservable through ParseWord's synthesis round-trip -- see memoization.md §5(b)
        // -- so this counter, not the equivalence assertions above, is what proves the memoized path is
        // actually exercised here, not silently bypassed.)
        Assert.That(
            AnalysisStratumRule.DiagTemplateMemoHits + AnalysisStratumRule.DiagTemplateNogoodHits,
            Is.GreaterThan(templateHitsBefore + templateNogoodHitsBefore),
            "the template memo must actually have hit (positive or nogood) at least once on this "
                + "grammar -- otherwise this test cannot distinguish a working memo from a no-op one"
        );
    }

    // Canonical analysis-set signature (memoization.md §5: gates compare this, never byte/object
    // equality -- a memo-replayed Word is not guaranteed field-for-field identical to a freshly-computed
    // one). AllomorphsInMorphOrder alone would not catch a broken trail/non-head graft (it walks Shape
    // annotations, which Word.ReplayOnto never touches) -- MorphemesInApplicationOrder walks
    // _mruleApps/_nonHeadApps directly, which is exactly what ReplayOnto rewrites. Root index is included
    // so two analyses with the same morpheme sequence but different lexical roots can't collide.
    internal static string WordAnalysisSignature(Word word)
    {
        return string.Join("+", word.AllomorphsInMorphOrder.Select(a => a.Morpheme.Id))
            + "|"
            + string.Join("+", word.MorphemesInApplicationOrder.Select(m => m.Id))
            + "|root="
            + word.RootAllomorph.Morpheme.Id;
    }

    [Test]
    public void AnalyzeWord_SingleThreaded_MatchesParallel()
    {
        // Build a small Unordered grammar (the order FieldWorks uses, which exercises the
        // parallel analysis cascade and parallel affix-template unapplication).
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var parallel = new Morpher(TraceManager, Language); // default: Environment.ProcessorCount
        var singleThreaded = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);

        Assert.That(singleThreaded.MaxDegreeOfParallelism, Is.EqualTo(1));

        // The single-threaded cascade (MaxDegreeOfParallelism == 1) must produce the same analyses
        // as the parallel cascade.
        IEnumerable<WordAnalysis> singleResult = singleThreaded.AnalyzeWord("sagd").ToList();
        IEnumerable<WordAnalysis> parallelResult = parallel.AnalyzeWord("sagd").ToList();
        Assert.That(
            singleResult,
            Is.EquivalentTo(parallelResult),
            "single-threaded analysis must match the parallel analysis"
        );
    }

    [Test]
    public void AnalyzeWord_ConcurrentRepeatedParsing_IsDeterministic()
    {
        // Concurrency safety net for the copy-on-write refactors (Plans A & B): many threads
        // parse against one shared frozen grammar whose FeatureStructs become shared into
        // per-parse clones. A COW race would show up as a nondeterministic analysis. Unordered
        // order exercises the parallel cascade + affix-template paths.
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);
        var words = new[] { "sagd", "sag", "tag", "tagd", "gag", "xyzzy" };
        Dictionary<string, string> baseline = words.ToDictionary(w => w, w => AnalysisSignature(morpher, w));

        for (int iter = 0; iter < 50; iter++)
        {
            var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            System.Threading.Tasks.Parallel.ForEach(
                Enumerable.Range(0, 250),
                i =>
                {
                    string w = words[i % words.Length];
                    results[w] = AnalysisSignature(morpher, w);
                }
            );
            foreach (string w in words)
            {
                Assert.That(
                    results[w],
                    Is.EqualTo(baseline[w]),
                    $"nondeterministic analysis for '{w}' on iteration {iter}"
                );
            }
        }
    }

    private static string AnalysisSignature(Morpher morpher, string word)
    {
        return string.Join(
            "|",
            morpher
                .AnalyzeWord(word)
                .Select(a => string.Join("+", a.Morphemes.Select(m => m.Id)) + ":" + a.RootMorphemeIndex)
                .OrderBy(s => s, System.StringComparer.Ordinal)
        );
    }
}
