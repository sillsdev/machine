using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// Drives AnalysisStratumRule against a scope the test owns, which is the only way to observe whether the
// template battery memoized anything: through Morpher the scope is created and discarded inside
// ParseWord, and a Linear stratum invokes the battery at most once per distinct state, so it never
// replays there and the hit counters cannot distinguish a memoized run from an excluded one.
[TestFixture]
public class AnalysisStratumRuleTests : HermitCrabTestBase
{
    [Test]
    public void Apply_MemoizesTemplateBattery_OnUnorderedStratum()
    {
        AddVerbTemplate();
        SetRuleOrder(MorphologicalRuleOrder.Unordered);

        AnalysisScope scope = ApplyStratumRule("sagd");

        Assert.That(
            scope.TemplateMemo,
            Is.Not.Empty,
            "an Unordered stratum must memoize the template battery -- otherwise the negative case below "
                + "proves nothing"
        );
    }

    [Test]
    public void Apply_DoesNotMemoizeTemplateBattery_OnLinearStratum()
    {
        AddVerbTemplate();
        SetRuleOrder(MorphologicalRuleOrder.Linear);

        AnalysisScope scope = ApplyStratumRule("sagd");

        Assert.That(
            scope.TemplateMemo,
            Is.Empty,
            "a Linear stratum must not memoize the template battery: AnalysisStateKey's key-completeness "
                + "audit covers only the Unordered cascade"
        );
        Assert.That(
            scope.Memo,
            Is.Empty,
            "nor may the mrule table be written on a Linear stratum, which runs PermutationRuleCascade"
        );
    }

    // Runs one stratum rule over `word` with a scope attached, and hands the scope back for inspection.
    private AnalysisScope ApplyStratumRule(string word)
    {
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        var stratumRule = new AnalysisStratumRule(morpher, Morphophonemic);

        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment(word));
        var scope = new AnalysisScope();
        input.AnalysisScope = scope;
        input.Freeze();

        // Apply builds its result set eagerly, so this forces the battery for every state it reaches.
        _ = stratumRule.Apply(input).ToList();
        return scope;
    }

    private void AddVerbTemplate()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var dSuffix = new AffixProcessRule
        {
            Id = "TPAST",
            Name = "template_d_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        dSuffix.Allomorphs.Add(
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
        verbTemplate.Slots.Add(new AffixTemplateSlot(dSuffix) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);
    }
}
