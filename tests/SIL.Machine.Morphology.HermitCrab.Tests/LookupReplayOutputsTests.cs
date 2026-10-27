using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// Realizational rules fire anywhere and a compounding rule on the trail adds outputs; a required check allows both.
[TestFixture]
public class LookupReplayOutputsTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    private FeatureStruct Pos(string symbol) => FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol(symbol).Value;

    private AffixProcessAllomorph Allomorph(string insert) =>
        new AffixProcessAllomorph
        {
            Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
            Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, insert) },
        };

    private AffixProcessRule RequiringLatinate()
    {
        var rule = new AffixProcessRule
        {
            Id = "need",
            Name = "need",
            Gloss = "NEED",
            RequiredSyntacticFeatureStruct = Pos("N"),
            OutSyntacticFeatureStruct = Pos("N"),
        };
        AffixProcessAllomorph allo = Allomorph("d");
        allo.RequiredMprFeatures.Add(Latinate);
        rule.Allomorphs.Add(allo);
        Morphophonemic.MorphologicalRules.Add(rule);
        return rule;
    }

    private bool IsFeasible(AffixProcessRule rule, IMorphologicalRule? earlier = null)
    {
        LexEntry entry = AddEntry("root", Pos("N"), Morphophonemic, "zud");
        var word = new Word(entry.PrimaryAllomorph, FeatureStruct.New().Value);
        word.MorphologicalRuleUnapplied(rule, null);
        if (earlier != null)
            word.MorphologicalRuleUnapplied(earlier, null);
        return new LookupReplay(Language).IsFeasible(word);
    }

    [Test]
    public void RequiredFeature_NoRuleCanSupplyIt_IsPruned()
    {
        Assert.That(IsFeasible(RequiringLatinate()), Is.False);
    }

    [Test]
    public void RequiredFeature_RealizationalRuleOutputsIt_IsNotPruned()
    {
        AffixProcessRule rule = RequiringLatinate();
        var real = new RealizationalAffixProcessRule
        {
            Name = "real",
            Gloss = "REAL",
            RealizationalFeatureStruct = Pos("N"),
        };
        AffixProcessAllomorph realAllo = Allomorph("t");
        realAllo.OutMprFeatures.Add(Latinate);
        real.Allomorphs.Add(realAllo);
        Morphophonemic.MorphologicalRules.Add(real);

        Assert.That(IsFeasible(rule), Is.True);
    }

    [Test]
    public void RequiredFeature_CompoundingRuleOnTheTrailOutputsIt_IsNotPruned()
    {
        AffixProcessRule rule = RequiringLatinate();
        var compound = new CompoundingRule { Name = "compound" };
        var subrule = new CompoundingSubrule
        {
            HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(Any).OneOrMore.Value },
            NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(Any).OneOrMore.Value },
            Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
        };
        subrule.OutMprFeatures.Add(Latinate);
        compound.Subrules.Add(subrule);
        Morphophonemic.MorphologicalRules.Add(compound);

        Assert.That(IsFeasible(rule, compound), Is.True);
    }
}
