using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

// A template's required syntactic features gate unapplication but are never added to the output words.
// Word.ValueEquals omits SyntacticFeatureStruct because analysis derives it from the rule trail alone,
// and adding a template's features would break that: a template can be passed through with every slot
// left empty, leaving the shape and the trail untouched.
// AffixTemplateTests.SameRuleUsedInMultipleTemplates covers the end-to-end effect.
[TestFixture]
public class AnalysisAffixTemplateRuleTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    [Test]
    public void Apply_SlotLeftEmpty_DoesNotAddTheTemplateFeaturesToTheOutput()
    {
        FeatureStruct v = Pos("V");
        AffixTemplate template = VerbTemplate(v);

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        IRule<Word, ShapeNode> templateRule = template.CompileAnalysisRule(morpher);

        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment("sags"));
        input.Freeze();

        Word outWord = templateRule.Apply(input).Single();

        Assert.That(
            outWord.SyntacticFeatureStruct.ValueEquals(input.SyntacticFeatureStruct),
            Is.True,
            "a pass through the template with its only slot left empty must leave SyntacticFeatureStruct alone"
        );
    }

    // The same suffix unapplied with and without a template in front of it reaches Word.ValueEquals-equal
    // words, so differing feature structures would drop one of them wherever Words are deduplicated.
    [Test]
    public void Apply_SlotLeftEmpty_LeavesTheSameStateAsNotEnteringTheTemplate()
    {
        FeatureStruct v = Pos("V");
        AffixTemplate template = VerbTemplate(v);
        AffixProcessRule sSuffix = Suffix("s_suffix", FeatureStruct.New().Value, v, Table3, "+s");

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        IRule<Word, ShapeNode> templateRule = template.CompileAnalysisRule(morpher);
        var sRule = new AnalysisAffixProcessRule(morpher, sSuffix);

        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment("sags"));
        input.Freeze();

        Word viaTemplate = sRule.Apply(templateRule.Apply(input).Single()).Single();
        Word direct = sRule.Apply(input).Single();

        Assert.Multiple(() =>
        {
            Assert.That(FreezableEqualityComparer<Word>.Default.Equals(viaTemplate, direct), Is.True);
            Assert.That(
                viaTemplate.SyntacticFeatureStruct.ValueEquals(direct.SyntacticFeatureStruct),
                Is.True,
                "Word.ValueEquals-equal analyses must agree on SyntacticFeatureStruct"
            );
        });
    }

    private AffixTemplate VerbTemplate(FeatureStruct required)
    {
        var template = new AffixTemplate { Name = "verb_template", RequiredSyntacticFeatureStruct = required };
        template.Slots.Add(
            new AffixTemplateSlot(Suffix("d_suffix", required, required, Table3, "+d")) { Optional = true }
        );
        return template;
    }

    private static AffixProcessRule Suffix(
        string name,
        FeatureStruct required,
        FeatureStruct outFs,
        CharacterDefinitionTable table,
        string insert
    )
    {
        var rule = new AffixProcessRule
        {
            Id = name,
            Name = name,
            Gloss = name,
            RequiredSyntacticFeatureStruct = required,
            OutSyntacticFeatureStruct = outFs,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(table, insert) },
            }
        );
        return rule;
    }

    private FeatureStruct Pos(string symbol) => FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol(symbol).Value;
}
