using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
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
                .Group("1", group => group.Annotation(Char(_table3, "i")))
                .Group("2", group => group.Annotation(Char(_table3, "u")))
                .Value,
            LeftSwitchName = "2",
            RightSwitchName = "1"
        };
        _morphophonemic.PhonologicalRules.Add(rule1);

        var morpher = new Morpher(_ttraceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("mui"), "51");
    }

    [Test]
    public void ComplexRule()
    {
        FeatureStruct any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var rule1 = new MetathesisRule
        {
            Name = "rule1",
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Char(_table3, "i")))
                .Group("middle", group => group.Annotation(Char(_table3, "+")))
                .Group("2", group => group.Annotation(Char(_table3, "u")))
                .Group("rightEnv", group => group.Annotation(HCFeatureSystem.RightSideAnchor))
                .Value,
            LeftSwitchName = "2",
            RightSwitchName = "1"
        };
        _morphophonemic.PhonologicalRules.Add(rule1);

        var uSuffix = new AffixProcessRule { Name = "u_suffix", Gloss = "3SG" };
        _morphophonemic.MorphologicalRules.Add(uSuffix);
        uSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+u") }
            }
        );

        var morpher = new Morpher(_ttraceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("mui"), "53 3SG");
    }

    [Test]
    public void SimpleRuleNotUnapplied()
    {
        FeatureStruct any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var prule = new MetathesisRule
        {
            Name = "rule1",
            Pattern = Pattern<Word, ShapeNode>
                .New()
                .Group("1", group => group.Annotation(Char(_table3, "i")))
                .Group("2", group => group.Annotation(Char(_table3, "u")))
                .Value,
            LeftSwitchName = "2",
            RightSwitchName = "1"
        };
        _morphophonemic.PhonologicalRules.Add(prule);

        var iSuffix = new AffixProcessRule { Name = "i_suffix", Gloss = "3SG" };
        _morphophonemic.MorphologicalRules.Add(iSuffix);
        iSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "i") }
            }
        );

        var morpher = new Morpher(_ttraceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("pui"), "52 3SG");
    }
}
