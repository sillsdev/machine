using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Matching.Fluent;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

public class MetathesisRuleTests : HermitCrabTestBase
{
    [Test]
    public void SimpleRule()
    {
        var rule1 = new MetathesisRule
        {
            Name = "rule1",
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "i")))
                .Group("2", group => group.Annotation(Character(Table3, "u")))
                .Value,
            LeftSwitchName = "2",
            RightSwitchName = "1",
        };
        Morphophonemic.PhonologicalRules.Add(rule1);

        var morpher = new Morpher(TraceManager, Language);
        AssertMorphsEqual(morpher.ParseWord("mui"), "51");
    }

    // Differs from SimpleRule only in the switch-name order, and must give the same result.
    [Test]
    public void SimpleRule_LeftSwitchNamesEarlierGroup()
    {
        var rule1 = new MetathesisRule
        {
            Name = "rule1",
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "i")))
                .Group("2", group => group.Annotation(Character(Table3, "u")))
                .Value,
            LeftSwitchName = "1",
            RightSwitchName = "2",
        };
        Morphophonemic.PhonologicalRules.Add(rule1);

        var morpher = new Morpher(TraceManager, Language);
        AssertMorphsEqual(morpher.ParseWord("mui"), "51");
    }

    // Differs from SimpleRule only in the switch-name order, plus a right-to-left direction.
    [Test]
    public void SimpleRule_LeftSwitchNamesEarlierGroup_RightToLeft()
    {
        var rule1 = new MetathesisRule
        {
            Name = "rule1",
            Direction = Direction.RightToLeft,
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "i")))
                .Group("2", group => group.Annotation(Character(Table3, "u")))
                .Value,
            LeftSwitchName = "1",
            RightSwitchName = "2",
        };
        Morphophonemic.PhonologicalRules.Add(rule1);

        var morpher = new Morpher(TraceManager, Language);
        AssertMorphsEqual(morpher.ParseWord("mui"), "51");
    }

    [Test]
    public void ComplexRule()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var rule1 = new MetathesisRule
        {
            Name = "rule1",
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "i")))
                .Group("middle", group => group.Annotation(Character(Table3, "+")))
                .Group("2", group => group.Annotation(Character(Table3, "u")))
                .Group("rightEnv", group => group.Annotation(HCFeatureSystem.RightSideAnchor))
                .Value,
            LeftSwitchName = "2",
            RightSwitchName = "1",
        };
        Morphophonemic.PhonologicalRules.Add(rule1);

        var uSuffix = new AffixProcessRule { Name = "u_suffix", Gloss = "3SG" };
        Morphophonemic.MorphologicalRules.Add(uSuffix);
        uSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+u") },
            }
        );

        var morpher = new Morpher(TraceManager, Language);
        AssertMorphsEqual(morpher.ParseWord("mui"), "53 3SG");
    }

    // ComplexRule with the switch names reversed: a group sits between the two switches.
    [Test]
    public void ComplexRule_LeftSwitchNamesEarlierGroup()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var rule1 = new MetathesisRule
        {
            Name = "rule1",
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "i")))
                .Group("middle", group => group.Annotation(Character(Table3, "+")))
                .Group("2", group => group.Annotation(Character(Table3, "u")))
                .Group("rightEnv", group => group.Annotation(HCFeatureSystem.RightSideAnchor))
                .Value,
            LeftSwitchName = "1",
            RightSwitchName = "2",
        };
        Morphophonemic.PhonologicalRules.Add(rule1);

        var uSuffix = new AffixProcessRule { Name = "u_suffix", Gloss = "3SG" };
        Morphophonemic.MorphologicalRules.Add(uSuffix);
        uSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+u") },
            }
        );

        var morpher = new Morpher(TraceManager, Language);
        AssertMorphsEqual(morpher.ParseWord("mui"), "53 3SG");
    }

    [Test]
    public void SimpleRuleNotUnapplied()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var prule = new MetathesisRule
        {
            Name = "rule1",
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "i")))
                .Group("2", group => group.Annotation(Character(Table3, "u")))
                .Value,
            LeftSwitchName = "2",
            RightSwitchName = "1",
        };
        Morphophonemic.PhonologicalRules.Add(prule);

        var iSuffix = new AffixProcessRule { Name = "i_suffix", Gloss = "3SG" };
        Morphophonemic.MorphologicalRules.Add(iSuffix);
        iSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "i") },
            }
        );

        var morpher = new Morpher(TraceManager, Language);
        AssertMorphsEqual(morpher.ParseWord("pui"), "52 3SG");
    }

    // The rule t-i-k to k-i-t keeps the middle segment i in place, so analysis must keep it too (machine #619).
    [Test]
    public void MiddleSegment_AnalysisUndoesSynthesis()
    {
        LexEntry entry = AddMiddleContextRule(group => group.Annotation(Character(Table3, "i")), "tika");

        var morpher = new Morpher(TraceManager, Language);
        Assert.That(
            morpher.GenerateWords(entry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "kita" })
        );
        AssertMorphsEqual(morpher.ParseWord("kita"), "tika");
        foreach (string neighbor in new[] { "tika", "kuta", "kta", "kitta" })
            AssertMorphsEqual(morpher.ParseWord(neighbor));
    }

    [Test]
    public void MiddleSegmentOutsideAnyGroup_AnalysisUndoesSynthesis()
    {
        LexEntry entry = AddMiddleContextRule(null, "tika");

        var morpher = new Morpher(TraceManager, Language);
        Assert.That(
            morpher.GenerateWords(entry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "kita" })
        );
        AssertMorphsEqual(morpher.ParseWord("kita"), "tika");
    }

    [Test]
    public void MiddleNaturalClass_AnalysisUndoesSynthesis()
    {
        var vowels = new SegmentNaturalClass(new[] { Table3["i"], Table3["a"] });
        AddMiddleContextRule(group => group.Annotation(vowels.FeatureStruct), "tika");
        AddEntry("taka", FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value, Morphophonemic, "taka");

        var morpher = new Morpher(TraceManager, Language);
        AssertMorphsEqual(morpher.ParseWord("kita"), "tika");
        AssertMorphsEqual(morpher.ParseWord("kata"), "taka");
        // A consonant between the switches does not satisfy the middle class.
        AssertMorphsEqual(morpher.ParseWord("kpta"));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void RequiredQuantifiedBoundaryBetweenSwitches_AnalysisUndoesSynthesis(int maxDegreeOfParallelism)
    {
        Morphophonemic.PhonologicalRules.Add(
            new MetathesisRule
            {
                Pattern = Pattern<Word, ShapeNode>
                    .New()
                    .Group("1", group => group.Annotation(Character(Table3, "t")))
                    .Annotation(Character(Table3, "+"))
                    .Range(1, 1)
                    .Group("2", group => group.Annotation(Character(Table3, "k")))
                    .Value,
                LeftSwitchName = "1",
                RightSwitchName = "2",
            }
        );
        LexEntry entry = AddEntry(
            "root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Morphophonemic,
            "at+ka"
        );

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism);
        Assert.That(
            morpher.GenerateWords(entry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "akta" })
        );
        AssertMorphsEqual(morpher.ParseWord("akta"), "root");
    }

    [TestCase(1, 0)]
    [TestCase(1, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 0)]
    [TestCase(2, 1)]
    [TestCase(2, 2)]
    public void BoundaryAlternative_AnalysisUndoesSynthesis(int maxDegreeOfParallelism, int nesting)
    {
        var pattern = new Pattern<Word, ShapeNode>();
        pattern.Children.Add(new Group<Word, ShapeNode>("1", new Constraint<Word, ShapeNode>(Character(Table3, "t"))));
        PatternNode<Word, ShapeNode> boundary = new Constraint<Word, ShapeNode>(Character(Table3, "+"));
        if (nesting == 1)
            boundary = new Quantifier<Word, ShapeNode>(0, 1, boundary);
        else if (nesting == 2)
            boundary = new Group<Word, ShapeNode>(boundary);
        var alternatives = new Alternation<Word, ShapeNode>(
            new PatternNode<Word, ShapeNode>[] { boundary, new Constraint<Word, ShapeNode>(Character(Table3, "i")) }
        );
        pattern.Children.Add(new Quantifier<Word, ShapeNode>(1, 1, alternatives));
        pattern.Children.Add(new Group<Word, ShapeNode>("2", new Constraint<Word, ShapeNode>(Character(Table3, "k"))));
        Morphophonemic.PhonologicalRules.Add(
            new MetathesisRule
            {
                Pattern = pattern,
                LeftSwitchName = "1",
                RightSwitchName = "2",
            }
        );
        LexEntry entry = AddEntry(
            "root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Morphophonemic,
            "at+ka"
        );
        LexEntry segmentEntry = AddEntry(
            "segment",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Morphophonemic,
            "atika"
        );
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism);
        Assert.That(
            morpher.GenerateWords(entry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "akta" })
        );
        AssertMorphsEqual(morpher.ParseWord("akta"), "root");
        Assert.That(
            morpher.GenerateWords(segmentEntry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "akita" })
        );
        AssertMorphsEqual(morpher.ParseWord("akita"), "segment");
    }

    [Test]
    public void NonGreedyMiddleQuantifier_PreservesGreediness()
    {
        Pattern<Word, ShapeNode> pattern = Pattern<Word, ShapeNode>
            .New()
            .Group("1", group => group.Annotation(Character(Table3, "t")))
            .Annotation(Character(Table3, "i"))
            .LazyRange(1, 2)
            .Group("2", group => group.Annotation(Character(Table3, "k")))
            .Value;
        var spec = new AnalysisMetathesisRuleSpec(pattern, "1", "2");
        Quantifier<Word, ShapeNode> quantifier = spec.Pattern.Children.OfType<Quantifier<Word, ShapeNode>>().Single();
        Assert.That(quantifier.IsGreedy, Is.False);
    }

    [Test]
    public void QuantifiedMiddleSequence_RetainsSegmentAndSkipsBoundary()
    {
        Morphophonemic.PhonologicalRules.Add(
            new MetathesisRule
            {
                Pattern = Pattern<Word, ShapeNode>
                    .New()
                    .Group("1", group => group.Annotation(Character(Table3, "t")))
                    .Group(
                        "sequence",
                        group => group.Annotation(Character(Table3, "+")).Annotation(Character(Table3, "i"))
                    )
                    .Range(1, 1)
                    .Group("2", group => group.Annotation(Character(Table3, "k")))
                    .Value,
                LeftSwitchName = "1",
                RightSwitchName = "2",
            }
        );
        LexEntry entry = AddEntry(
            "root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Morphophonemic,
            "at+ika"
        );
        var morpher = new Morpher(TraceManager, Language);
        Assert.That(
            morpher.GenerateWords(entry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "akita" })
        );
        AssertMorphsEqual(morpher.ParseWord("akita"), "root");
        AssertMorphsEqual(morpher.ParseWord("akuta"));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void BoundaryBetweenSwitches_AnalysisUndoesSynthesis(int maxDegreeOfParallelism)
    {
        Morphophonemic.PhonologicalRules.Add(
            new MetathesisRule
            {
                Pattern = Pattern<Word, ShapeNode>
                    .New()
                    .Group("1", group => group.Annotation(Character(Table3, "t")))
                    .Annotation(Character(Table3, "+"))
                    .Group("2", group => group.Annotation(Character(Table3, "k")))
                    .Value,
                LeftSwitchName = "1",
                RightSwitchName = "2",
            }
        );
        LexEntry entry = AddEntry(
            "root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Morphophonemic,
            "at+ka"
        );
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism);
        Assert.That(
            morpher.GenerateWords(entry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "akta" })
        );
        AssertMorphsEqual(morpher.ParseWord("akta"), "root");
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void UntypedMiddleConstraint_AnalysisUndoesSynthesis(bool grouped, bool nonempty)
    {
        FeatureStruct middle = nonempty ? FeatureStruct.New().Symbol(HCFeatureSystem.Clean).Value : new FeatureStruct();
        Pattern<Word, ShapeNode> pattern = grouped
            ? Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "t")))
                .Group("middle", group => group.Annotation(middle))
                .Group("2", group => group.Annotation(Character(Table3, "k")))
                .Value
            : Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Character(Table3, "t")))
                .Annotation(middle)
                .Group("2", group => group.Annotation(Character(Table3, "k")))
                .Value;
        Morphophonemic.PhonologicalRules.Add(
            new MetathesisRule
            {
                Pattern = pattern,
                LeftSwitchName = "1",
                RightSwitchName = "2",
            }
        );
        LexEntry entry = AddEntry(
            "root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Morphophonemic,
            "tika"
        );
        var morpher = new Morpher(TraceManager, Language);
        Assert.That(
            morpher.GenerateWords(entry, System.Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "kita" })
        );
        AssertMorphsEqual(morpher.ParseWord("kita"), "root", "tika");
    }

    private LexEntry AddMiddleContextRule(
        System.Func<IGroupSyntax<Word, ShapeNode>, IGroupSyntax<Word, ShapeNode>>? middle,
        string root
    )
    {
        Pattern<Word, ShapeNode> pattern = (
            middle is null
                ? Pattern<Word, ShapeNode>
                    .New()
                    .Group("1", group => group.Annotation(Character(Table3, "t")))
                    .Annotation(Character(Table3, "i"))
                    .Group("2", group => group.Annotation(Character(Table3, "k")))
                    .Value
                : Pattern<Word, ShapeNode>
                    .New()
                    .Group("1", group => group.Annotation(Character(Table3, "t")))
                    .Group("middle", middle)
                    .Group("2", group => group.Annotation(Character(Table3, "k")))
                    .Value
        );
        Morphophonemic.PhonologicalRules.Add(
            new MetathesisRule
            {
                Name = "rule1",
                Pattern = pattern,
                LeftSwitchName = "2",
                RightSwitchName = "1",
            }
        );
        return AddEntry(
            root,
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Morphophonemic,
            root
        );
    }
}
