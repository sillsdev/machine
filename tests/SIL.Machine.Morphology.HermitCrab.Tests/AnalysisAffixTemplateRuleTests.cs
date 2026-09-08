using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

// Exercises AnalysisAffixTemplateRule.Apply directly (bypassing AnalysisStratumRule/Morpher.ParseWord), since
// the bug this guards against -- mutating a frozen, possibly-shared input word -- is only observable at the
// boundary of this one method: everywhere else the input has already been re-wrapped or discarded.
[TestFixture]
public class AnalysisAffixTemplateRuleTests : HermitCrabTestBase
{
    // The only slot's rule can never apply (its OutSyntacticFeatureStruct -- symbol "N" -- can never unify
    // with our "V" input), so the DFS falls straight through every optional slot to the terminal
    // "nothing applied" case, which is the one place the pre-fix code emitted the input object itself.
    [TestCase(0)]
    [TestCase(1)]
    public void Apply_LeavesFrozenInputUnmutated_AndEmitsDistinctOutput(int maxDegreeOfParallelism)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var neverApplies = new AffixProcessRule
        {
            Name = "never_applies",
            Gloss = "NEVER",
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        neverApplies.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "z") },
            }
        );

        var template = new AffixTemplate
        {
            Name = "skip_all_template",
            RequiredSyntacticFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Symbol("V")
                .Feature(Head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value,
        };
        template.Slots.Add(new AffixTemplateSlot(neverApplies) { Optional = true });

        Morpher morpher =
            maxDegreeOfParallelism == 0
                ? new Morpher(TraceManager, Language)
                : new Morpher(TraceManager, Language, maxDegreeOfParallelism: maxDegreeOfParallelism);
        var rule = new AnalysisAffixTemplateRule(morpher, template);

        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment("sag"));
        input.SyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;
        input.Freeze();
        FeatureStruct originalInputFs = input.SyntacticFeatureStruct.Clone();

        Word[] output = rule.Apply(input).ToArray();

        Assert.That(
            output,
            Has.Length.EqualTo(1),
            "the only slot is optional and never applies, so exactly one (the all-skipped) output is expected"
        );
        Assert.That(input.IsFrozen, Is.True, "Apply must not unfreeze its input");
        Assert.That(
            input.SyntacticFeatureStruct,
            Is.EqualTo(originalInputFs).Using(FreezableEqualityComparer<FeatureStruct>.Default),
            "Apply must not mutate the shared input's syntactic feature structure"
        );

        Word onlyOutput = output[0];
        Assert.That(
            onlyOutput,
            Is.Not.SameAs(input),
            "the emitted word must be a distinct object from the frozen input"
        );
        Assert.That(
            onlyOutput.ValueEquals(input),
            Is.True,
            "aside from the merged syntactic features, the output must be value-equal to the input"
        );
        Assert.That(
            onlyOutput.SyntacticFeatureStruct,
            Is.EqualTo(
                    FeatureStruct
                        .New(Language.SyntacticFeatureSystem)
                        .Symbol("V")
                        .Feature(Head)
                        .EqualTo(head => head.Feature("tense").EqualTo("past"))
                        .Value
                )
                .Using(FreezableEqualityComparer<FeatureStruct>.Default),
            "the output must carry the template's required syntactic features merged in"
        );
    }

    // The traced path is untouched by the optimization: it must keep cloning eagerly, so parsing under a
    // tracing TraceManager must succeed exactly as before and still produce template trace nodes.
    [Test]
    public void Apply_TracedPath_StillParsesAndProducesTemplateTraceNodes()
    {
        TraceManager.IsTracing = true;
        try
        {
            var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
            var alvStop = FeatureStruct
                .New(Language.PhonologicalFeatureSystem)
                .Symbol(HCFeatureSystem.Segment)
                .Symbol("cons+")
                .Symbol("strident-")
                .Symbol("del_rel-")
                .Symbol("alveolar")
                .Value;
            var voicelessCons = FeatureStruct
                .New(Language.PhonologicalFeatureSystem)
                .Symbol(HCFeatureSystem.Segment)
                .Symbol("cons+")
                .Symbol("vd-")
                .Value;

            var edSuffix = new AffixProcessRule { Name = "ed_suffix", Gloss = "PAST" };
            edSuffix.Allomorphs.Add(
                new AffixProcessAllomorph
                {
                    Lhs =
                    {
                        Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
                        Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value,
                    },
                    Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertSegments(Table3, "ɯd") },
                }
            );
            edSuffix.Allomorphs.Add(
                new AffixProcessAllomorph
                {
                    Lhs =
                    {
                        Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value,
                    },
                    Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "t") },
                }
            );
            edSuffix.Allomorphs.Add(
                new AffixProcessAllomorph
                {
                    Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                    Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "d") },
                }
            );

            var verbTemplate = new AffixTemplate
            {
                Name = "verb",
                RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            };
            verbTemplate.Slots.Add(new AffixTemplateSlot(edSuffix));
            Morphophonemic.AffixTemplates.Add(verbTemplate);

            var morpher = new Morpher(TraceManager, Language);
            Word[] output = morpher.ParseWord("sagd", out object trace).ToArray();

            AssertMorphsEqual(output, "32 PAST");
            var nodes = Flatten((Trace)trace).ToList();
            Assert.That(
                nodes.Any(t => t.Type == TraceType.TemplateAnalysisInput),
                "a template-analysis-input trace node must be recorded while tracing"
            );
            Assert.That(
                nodes.Any(t => t.Type == TraceType.TemplateAnalysisOutput),
                "a template-analysis-output trace node must be recorded while tracing"
            );
        }
        finally
        {
            TraceManager.IsTracing = false;
        }
    }

    private static System.Collections.Generic.IEnumerable<Trace> Flatten(Trace root)
    {
        yield return root;
        foreach (Trace child in root.Children)
        {
            foreach (Trace descendant in Flatten(child))
                yield return descendant;
        }
    }
}
