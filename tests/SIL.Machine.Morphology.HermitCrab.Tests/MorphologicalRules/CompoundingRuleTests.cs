using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

public class CompoundingRuleTests : HermitCrabTestBase
{
    [Test]
    public void SimpleRules()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule1 = new CompoundingRule { Name = "rule1" };
        Allophonic.MorphologicalRules.Add(rule1);
        rule1.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") }
            }
        );

        var morpher = new Morpher(TraceManager, Language);
        List<Word> output = morpher.ParseWord("pʰutdat").ToList();
        AssertMorphsEqual(output, "5 8", "5 9");
        AssertRootAllomorphsEquals(output, "5");
        Assert.That(morpher.ParseWord("pʰutdas"), Is.Empty);
        Assert.That(morpher.ParseWord("pʰusdat"), Is.Empty);

        rule1.Subrules.Clear();
        rule1.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("nonHead"), new InsertSegments(Table3, "+"), new CopyFromInput("head") }
            }
        );

        morpher = new Morpher(TraceManager, Language);
        output = morpher.ParseWord("pʰutdat").ToList();
        AssertMorphsEqual(output, "5 8", "5 9");
        AssertRootAllomorphsEquals(output, "8", "9");
        Assert.That(morpher.ParseWord("pʰutdas"), Is.Empty);
        Assert.That(morpher.ParseWord("pʰusdat"), Is.Empty);

        var prefix = new AffixProcessRule
        {
            Name = "prefix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Feature(Head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value
        };
        Allophonic.MorphologicalRules.Insert(0, prefix);
        prefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "di+"), new CopyFromInput("1") }
            }
        );

        morpher = new Morpher(TraceManager, Language);
        output = morpher.ParseWord("pʰutdidat").ToList();
        AssertMorphsEqual(output, "5 PAST 9");
        AssertRootAllomorphsEquals(output, "9");

        Allophonic.MorphologicalRules.RemoveAt(0);

        rule1.MaxApplicationCount = 2;
        rule1.Subrules.Clear();
        rule1.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") }
            }
        );

        morpher = new Morpher(TraceManager, Language) { MaxStemCount = 3 };
        output = morpher.ParseWord("pʰutdatpip").ToList();
        AssertMorphsEqual(output, "5 8 41", "5 9 41");
        AssertRootAllomorphsEquals(output, "5");

        rule1.MaxApplicationCount = 1;

        var rule2 = new CompoundingRule { Name = "rule2" };
        Allophonic.MorphologicalRules.Add(rule2);
        rule2.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("nonHead"), new InsertSegments(Table3, "+"), new CopyFromInput("head") }
            }
        );

        morpher = new Morpher(TraceManager, Language) { MaxStemCount = 3 };
        output = morpher.ParseWord("pʰutdatpip").ToList();
        AssertMorphsEqual(output, "5 8 41", "5 9 41");
        AssertRootAllomorphsEquals(output, "8", "9");
    }

    [Test]
    public void MorphosyntacticRules()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule1 = new CompoundingRule
        {
            Name = "rule1",
            NonHeadRequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
        };
        Allophonic.MorphologicalRules.Add(rule1);
        rule1.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") }
            }
        );

        var morpher = new Morpher(TraceManager, Language);
        List<Word> output = morpher.ParseWord("pʰutdat").ToList();
        AssertMorphsEqual(output, "5 9");
        AssertRootAllomorphsEquals(output, "5");
        AssertSyntacticFeatureStructsEqual(
            output,
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value
        );
        Assert.That(morpher.ParseWord("pʰutbupu"), Is.Empty);

        Assert.That(
            morpher.GenerateWords(Entries["5"], new Morpheme[] { Entries["9"] }, new FeatureStruct()),
            Is.EquivalentTo(new[] { "pʰutdat" })
        );

        rule1.OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;

        morpher = new Morpher(TraceManager, Language);
        output = morpher.ParseWord("pʰutdat").ToList();
        AssertMorphsEqual(output, "5 9");
        AssertRootAllomorphsEquals(output, "5");
        AssertSyntacticFeatureStructsEqual(
            output,
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
        );

        Allophonic.MorphologicalRules.Clear();
        Morphophonemic.MorphologicalRules.Add(rule1);
        rule1.HeadRequiredSyntacticFeatureStruct = FeatureStruct
            .New(Language.SyntacticFeatureSystem)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("pers").EqualTo("2"))
            .Value;
        rule1.NonHeadRequiredSyntacticFeatureStruct = FeatureStruct.New().Value;

        morpher = new Morpher(TraceManager, Language);
        output = morpher.ParseWord("ssagabba").ToList();
        AssertMorphsEqual(output, "Perc0 39", "Perc0 40", "Perc3 39", "Perc3 40");
        AssertRootAllomorphsEquals(output, "Perc0", "Perc3");
    }

    [Test]
    public void ProdRestrictRule()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule1 = new CompoundingRule { Name = "rule1" };
        Allophonic.MorphologicalRules.Add(rule1);
        rule1.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") }
            }
        );

        var morpher = new Morpher(TraceManager, Language);
        List<Word> output = morpher.ParseWord("pʰutdat").ToList();
        AssertMorphsEqual(output, "5 8", "5 9");
        AssertRootAllomorphsEquals(output, "5");

        // Create an exception "feature"
        var excFeat = new MprFeature();
        excFeat.Name = "Allows compounding";
        Language.MprFeatures.Add(excFeat);
        // Add the exception "feature" to the head of the rule
        rule1.HeadProdRestrictionsMprFeatures.Add(excFeat);
        // The word should no longer parse
        Assert.That(morpher.ParseWord("pʰutdat"), Is.Empty);

        // Add the exception "feature" to the head root
        var head = Allophonic.Entries.ElementAt(2);
        head.MprFeatures.Add(excFeat);
        // It should now parse
        output = morpher.ParseWord("pʰutdat").ToList();
        AssertMorphsEqual(output, "5 8", "5 9");
        AssertRootAllomorphsEquals(output, "5");

        // Remove the exception "feature" from the head of the rule
        // and add it to the nonhead
        rule1.HeadProdRestrictionsMprFeatures.Remove(excFeat);
        rule1.NonHeadProdRestrictionsMprFeatures.Add(excFeat);
        // The word should no longer parse
        Assert.That(morpher.ParseWord("pʰutdat"), Is.Empty);

        // Removee the exception "feature" from the head root
        head.MprFeatures.Remove(excFeat);
        // Add the exception "feature" to the nonhead root
        var nonhead = Allophonic.Entries.ElementAt(5);
        nonhead.MprFeatures.Add(excFeat);
        // It should now parse
        output = morpher.ParseWord("pʰutdat").ToList();
        AssertMorphsEqual(output, "5 8");
        AssertRootAllomorphsEquals(output, "5");
    }

    private static void AssertRootAllomorphsEquals(IEnumerable<Word> words, params string[] expected)
    {
        Assert.That(words.Select(w => w.RootAllomorph.Morpheme.Gloss).Distinct(), Is.EquivalentTo(expected));
    }
}
