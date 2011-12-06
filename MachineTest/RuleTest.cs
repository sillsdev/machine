using System.Collections.Generic;
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
			var pattern = Pattern<StringData, int>.New(SpanFactory)
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value))
				.Group("target", target => target
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons-")
						.Symbol("low+").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			var rule = new ActionPatternRule<StringData, int>(pattern, (PatternRule<StringData, int> r, StringData input, PatternMatch<int> match, out StringData output) =>
			                                  	{
													Span<int> target = match["target"];
			                                  		foreach (Annotation<int> ann in input.Annotations.GetNodes(target))
			                                  			ann.FeatureStruct.PriorityUnion(FeatureStruct.New(PhoneticFeatSys).Symbol("low-").Value);
			                                  		output = input;
			                                  		Annotation<int> resumeAnn;
													input.Annotations.Find(target.GetEnd(pattern.Direction), pattern.Direction, out resumeAnn);
			                                  		return resumeAnn;
			                                  	});

			StringData inputWord = CreateStringData("fazk");
			IEnumerable<StringData> outputWords;
			Assert.IsTrue(rule.Apply(inputWord, out outputWords));
		}

		[Test]
		public void Batch()
		{
			var pattern = Pattern<StringData, int>.New(SpanFactory)
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value))
				.Group("target", target => target
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons-")
						.Symbol("low+").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			var rule1 = new ActionPatternRule<StringData, int>(pattern, (PatternRule<StringData, int> r, StringData input, PatternMatch<int> match, out StringData output) =>
												{
													Span<int> target = match["target"];
													foreach (Annotation<int> ann in input.Annotations.GetNodes(target))
														ann.FeatureStruct.PriorityUnion(FeatureStruct.New(PhoneticFeatSys)
															.Symbol("low-")
															.Symbol("mid-").Value);
													output = input;
													Annotation<int> resumeAnn;
													input.Annotations.Find(target.GetEnd(pattern.Direction), pattern.Direction, out resumeAnn);
													return resumeAnn;
												},
												input => input.Annotations.GetNodes("Word").Single().FeatureStruct.IsUnifiable(FeatureStruct.New(WordFeatSys).Symbol("verb").Value));

			var rule2 = new ActionPatternRule<StringData, int>(pattern, (PatternRule<StringData, int> r, StringData input, PatternMatch<int> match, out StringData output) =>
												{
													Span<int> target = match["target"];
													foreach (Annotation<int> ann in input.Annotations.GetNodes(target))
														ann.FeatureStruct.PriorityUnion(FeatureStruct.New(PhoneticFeatSys)
															.Symbol("low-")
															.Symbol("mid+").Value);
													output = input;
													Annotation<int> resumeAnn;
													input.Annotations.Find(target.GetEnd(pattern.Direction), pattern.Direction, out resumeAnn);
													return resumeAnn;
												});

			var batch = new ActionPatternRuleBatch<StringData, int>(SpanFactory);
			batch.AddRule(rule1);
			batch.AddRule(rule2);
			batch.Lhs.Compile();
			StringData inputWord = CreateStringData("fazk");
			inputWord.Annotations.Add("Word", inputWord.Span, FeatureStruct.New(WordFeatSys).Symbol("noun").Value);
			IEnumerable<StringData> outputWords;
			Assert.IsTrue(batch.Apply(inputWord, out outputWords));
		}
	}
}
