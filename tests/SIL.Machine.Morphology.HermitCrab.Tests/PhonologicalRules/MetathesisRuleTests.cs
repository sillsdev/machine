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
