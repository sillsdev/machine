using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules;

public class RuleTests : PhoneticTestsBase
{
    [Test]
    public void Apply()
    {
        var pattern = Pattern<AnnotatedStringData, int>
            .New()
            .Group(
                "leftEnv",
                leftEnv =>
                    leftEnv.Annotation(
                        FeatureStruct
                            .New(PhoneticFeatSys)
                            .Symbol(Seg)
                            .Symbol("cons+")
                            .Feature("voice")
                            .EqualToVariable("a")
                            .Value
                    )
            )
            .Group(
                "target",
                target =>
                    target.Annotation(
                        FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("cons-").Symbol("low+").Value
                    )
            )
            .Group(
                "rightEnv",
                rightEnv =>
                    rightEnv.Annotation(
                        FeatureStruct
                            .New(PhoneticFeatSys)
                            .Symbol(Seg)
                            .Symbol("cons+")
                            .Feature("voice")
                            .Not.EqualToVariable("a")
                            .Value
                    )
            )
            .Value;

        var ruleSpec = new DefaultPatternRuleSpec<AnnotatedStringData, int>(
            pattern,
            (r, match) =>
            {
                GroupCapture<int> target = match.GroupCaptures["target"];
                foreach (Annotation<int> ann in match.Input.Annotations.GetNodes(target.Range))
                    ann.FeatureStruct.PriorityUnion(FeatureStruct.New(PhoneticFeatSys).Symbol("low-").Value);
                return match.Input;
            }
        );

        var rule = new PatternRule<AnnotatedStringData, int>(ruleSpec);
        AnnotatedStringData inputWord = CreateStringData("fazk");
        Assert.IsTrue(rule.Apply(inputWord).Any());
    }

    [Test]
    public void Batch()
    {
        var pattern = Pattern<AnnotatedStringData, int>
            .New()
            .Group(
                "leftEnv",
                leftEnv =>
                    leftEnv.Annotation(
                        FeatureStruct
                            .New(PhoneticFeatSys)
                            .Symbol(Seg)
                            .Symbol("cons+")
                            .Feature("voice")
                            .EqualToVariable("a")
                            .Value
                    )
            )
            .Group(
                "target",
                target =>
                    target.Annotation(
                        FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("cons-").Symbol("low+").Value
                    )
            )
            .Group(
                "rightEnv",
                rightEnv =>
                    rightEnv.Annotation(
                        FeatureStruct
                            .New(PhoneticFeatSys)
                            .Symbol(Seg)
                            .Symbol("cons+")
                            .Feature("voice")
                            .Not.EqualToVariable("a")
                            .Value
                    )
            )
            .Value;

        var ruleSpec1 = new DefaultPatternRuleSpec<AnnotatedStringData, int>(
            pattern,
            (r, match) =>
            {
                GroupCapture<int> target = match.GroupCaptures["target"];
                foreach (Annotation<int> ann in match.Input.Annotations.GetNodes(target.Range))
                {
                    ann.FeatureStruct.PriorityUnion(
                        FeatureStruct.New(PhoneticFeatSys).Symbol("low-").Symbol("mid-").Value
                    );
                }
                return match.Input;
            },
            input =>
                input
                    .Annotations.Single(ann => ((FeatureSymbol)ann.FeatureStruct.GetValue(Type)) == Word)
                    .FeatureStruct.IsUnifiable(FeatureStruct.New(WordFeatSys).Symbol("verb").Value)
        );

        var ruleSpec2 = new DefaultPatternRuleSpec<AnnotatedStringData, int>(
            pattern,
            (r, match) =>
            {
                GroupCapture<int> target = match.GroupCaptures["target"];
                foreach (Annotation<int> ann in match.Input.Annotations.GetNodes(target.Range))
                {
                    ann.FeatureStruct.PriorityUnion(
                        FeatureStruct.New(PhoneticFeatSys).Symbol("low-").Symbol("mid+").Value
                    );
                }
                return match.Input;
            }
        );

        var batchSpec = new BatchPatternRuleSpec<AnnotatedStringData, int>(new[] { ruleSpec1, ruleSpec2 });
        var rule = new PatternRule<AnnotatedStringData, int>(batchSpec);
        AnnotatedStringData inputWord = CreateStringData("fazk");
        inputWord.Annotations.Add(inputWord.Range, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
        Assert.IsTrue(rule.Apply(inputWord).Any());
    }
}
