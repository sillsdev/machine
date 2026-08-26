using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Static classification of what un-applying a morphological rule does to word length
/// (hermitcrab-forest-memo-plan.md Stage 1). The direction is the thing these tests pin down: a rule
/// that INSERTS on synthesis REMOVES on analysis, and a rule that DELETES on synthesis RESTORES on
/// analysis. Only the first kind can be dropped from <see cref="AnalysisStateKey"/>.
/// </summary>
public class RuleLengthClassifierTests : HermitCrabTestBase
{
    private static FeatureStruct Any => FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    private static AffixProcessRule Rule(string name, params AffixProcessAllomorph[] allomorphs)
    {
        var rule = new AffixProcessRule { Name = name, Gloss = name };
        foreach (AffixProcessAllomorph allomorph in allomorphs)
            rule.Allomorphs.Add(allomorph);
        return rule;
    }

    [Test]
    public void OrdinarySuffix_Shrinks()
    {
        AffixProcessRule rule = Rule(
            "s_suffix",
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.Shrinking));
    }

    [Test]
    public void ZeroMorpheme_DoesNotShrink()
    {
        // The correspondent's own N -> V -> N cycle case: nothing is inserted, so nothing is removed on
        // un-application and the shape length measure gives no progress. Must stay in the key.
        AffixProcessRule rule = Rule(
            "zero",
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.NonShrinking));
    }

    [Test]
    public void Infix_Shrinks()
    {
        // Material inserted BETWEEN two copied parts is still material inserted. This is the case most
        // likely to be got backwards, so it is asserted explicitly.
        AffixProcessRule rule = Rule(
            "infix",
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(Any).Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(Any).OneOrMore.Value,
                },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s"), new CopyFromInput("2") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.Shrinking));
    }

    [Test]
    public void Truncation_DoesNotShrink()
    {
        // An Lhs part with no Rhs copy is deleted on synthesis, so AnalysisMorphologicalTransform
        // untruncates it on analysis -- the un-applied word GROWS even though the rule also inserts.
        AffixProcessRule rule = Rule(
            "truncating",
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(Any).Value,
                },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.NonShrinking));
    }

    [Test]
    public void Reduplication_IsUnknown()
    {
        AffixProcessRule rule = Rule(
            "redup",
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.Unknown));
    }

    [Test]
    public void Simulfix_DoesNotShrink()
    {
        // ModifyFromInput captures its part (so nothing is untruncated) but inserts nothing, so the
        // un-applied word is exactly as long as the input: length-preserving, and therefore retained.
        var voiced = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("vd+")
            .Value;
        AffixProcessRule rule = Rule(
            "simulfix",
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(Any).Value,
                },
                Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced) },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.NonShrinking));
    }

    [Test]
    public void SimulfixPlusInsertion_Shrinks()
    {
        var voiced = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("vd+")
            .Value;
        AffixProcessRule rule = Rule(
            "simulfix_z",
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(Any).Value,
                },
                Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced), new InsertSegments(Table3, "z") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.Shrinking));
    }

    [Test]
    public void BoundaryOnlyInsertion_DoesNotShrink()
    {
        // "+" is a boundary, not a segment. InsertSegments.GenerateAnalysisLhs deliberately omits boundary
        // nodes from the analysis pattern, so the classifier does not count them as removable material.
        // Conservative, and therefore safe.
        AffixProcessRule rule = Rule(
            "boundary_only",
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.NonShrinking));
    }

    [Test]
    public void WeakestAllomorphGoverns()
    {
        // One allomorph inserts, the other is a zero. The rule as a whole cannot be relied on to shorten.
        AffixProcessRule rule = Rule(
            "mixed",
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            },
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.NonShrinking));
    }

    [Test]
    public void RealizationalRule_IsClassifiedLikeAnAffixRule()
    {
        var rule = new RealizationalAffixProcessRule { Name = "real", Gloss = "REAL" };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.Shrinking));
    }

    [Test]
    public void CompoundingRule_IsUnknown()
    {
        var rule = new CompoundingRule { Name = "compound" };
        rule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(Any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new CopyFromInput("nonHead") },
            }
        );

        Assert.That(RuleLengthClassifier.Classify(rule), Is.EqualTo(UnapplicationLengthEffect.Unknown));
    }

    [Test]
    public void RetainInKeyMap_DropsOnlyShrinkingRules()
    {
        AffixProcessRule suffix = Rule(
            "s_suffix",
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );
        AffixProcessRule zero = Rule(
            "zero",
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(suffix);
        Morphophonemic.MorphologicalRules.Add(zero);

        IReadOnlyDictionary<IMorphologicalRule, bool> map = RuleLengthClassifier.BuildRetainInKeyMap(Language);

        Assert.That(map[suffix], Is.False, "a strictly shrinking rule need not be counted in the key");
        Assert.That(map[zero], Is.True, "a zero morpheme can cycle and must stay in the key");
    }
}
