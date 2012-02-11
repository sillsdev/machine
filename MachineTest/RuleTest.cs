using System.Linq;
using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.Machine.Test
{
	public class RuleTest : PhoneticTestBase
	{
		[Test]
		public void Apply()
		{
			var pattern = Pattern<StringData, int>.New()
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value))
				.Group("target", target => target
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons-")
						.Symbol("low+").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			var ruleSpec = new DefaultPatternRuleSpec<StringData, int>(pattern, (PatternRule<StringData, int> r, Match<StringData, int> match, out StringData output) =>
			                                  	{
													GroupCapture<int> target = match["target"];
			                                  		foreach (Annotation<int> ann in match.Input.Annotations.GetNodes(target.Span))
			                                  			ann.FeatureStruct.PriorityUnion(FeatureStruct.New(PhoneticFeatSys).Symbol("low-").Value);
			                                  		output = match.Input;
			                                  		return target.Span.End;
			                                  	});

			var rule = new PatternRule<StringData, int>(SpanFactory, ruleSpec);
			StringData inputWord = CreateStringData("fazk");
			Assert.IsTrue(rule.Apply(inputWord).Any());
		}

		[Test]
		public void Batch()
		{
			var pattern = Pattern<StringData, int>.New()
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value))
				.Group("target", target => target
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons-")
						.Symbol("low+").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			var ruleSpec1 = new DefaultPatternRuleSpec<StringData, int>(pattern, (PatternRule<StringData, int> r, Match<StringData, int> match, out StringData output) =>
												{
													GroupCapture<int> target = match["target"];
													foreach (Annotation<int> ann in match.Input.Annotations.GetNodes(target.Span))
														ann.FeatureStruct.PriorityUnion(FeatureStruct.New(PhoneticFeatSys)
															.Symbol("low-")
															.Symbol("mid-").Value);
													output = match.Input;
													return target.Span.End;
												},
												input => input.Annotations.Single(ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word)
													.FeatureStruct.IsUnifiable(FeatureStruct.New(WordFeatSys).Symbol("verb").Value));

			var ruleSpec2 = new DefaultPatternRuleSpec<StringData, int>(pattern, (PatternRule<StringData, int> r, Match<StringData, int> match, out StringData output) =>
												{
													GroupCapture<int> target = match["target"];
													foreach (Annotation<int> ann in match.Input.Annotations.GetNodes(target.Span))
														ann.FeatureStruct.PriorityUnion(FeatureStruct.New(PhoneticFeatSys)
															.Symbol("low-")
															.Symbol("mid+").Value);
													output = match.Input;
													return target.Span.End;
												});

			var batchSpec = new BatchPatternRuleSpec<StringData, int>(new[] {ruleSpec1, ruleSpec2});
			var rule = new PatternRule<StringData, int>(SpanFactory, batchSpec);
			StringData inputWord = CreateStringData("fazk");
			inputWord.Annotations.Add(inputWord.Span, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			Assert.IsTrue(rule.Apply(inputWord).Any());
		}
	}
}
