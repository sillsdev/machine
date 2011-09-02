using NUnit.Framework;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.APRE.Test
{
	[TestFixture]
	public class RuleTest
	{
		[Test]
		public void Apply()
		{
			var spanFactory = new IntegerSpanFactory();
			FeatureSystem featSys = CreateFeatureSystem();

			Pattern<int> pattern = Pattern<int>.With(spanFactory)
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation(FeatureStruct.With(featSys)
						.Feature("type").EqualTo("Seg")
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value))
				.Group("lhs", lhs => lhs
					.Annotation(FeatureStruct.With(featSys)
						.Feature("type").EqualTo("Seg")
						.Symbol("cons-")
						.Symbol("low+").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(FeatureStruct.With(featSys)
						.Feature("type").EqualTo("Seg")
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			var rule = new PatternRule<int>(pattern, (lhs, input, match) =>
			                                  	{
			                                  		IBidirList<Annotation<int>> target = input.GetView(match["lhs"]);
			                                  		foreach (Annotation<int> ann in target)
			                                  			ann.FeatureStruct.AddValues(FeatureStruct.With(featSys).Symbol("low-").Value);
			                                  		return target.GetLast(lhs.Direction);
			                                  	});

			AnnotationList<int> word = CreateFeatShapeAnnotations("fazk", spanFactory, featSys);
			Assert.IsTrue(rule.Apply(word));
		}

		[Test]
		public void Batch()
		{
			var spanFactory = new IntegerSpanFactory();
			FeatureSystem featSys = CreateFeatureSystem();

			Pattern<int> pattern = Pattern<int>.With(spanFactory)
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation(FeatureStruct.With(featSys)
						.Feature("type").EqualTo("Seg")
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value))
				.Group("lhs", lhs => lhs
					.Annotation(FeatureStruct.With(featSys)
						.Feature("type").EqualTo("Seg")
						.Symbol("cons-")
						.Symbol("low+").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(FeatureStruct.With(featSys)
						.Feature("type").EqualTo("Seg")
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			var rule1 = new PatternRule<int>(pattern, (lhs, input, match) =>
												{
													IBidirListView<Annotation<int>> target = input.GetView(match["lhs"]);
													foreach (Annotation<int> ann in target)
														ann.FeatureStruct.AddValues(FeatureStruct.With(featSys)
															.Symbol("low-")
															.Symbol("mid-").Value);
													return target.GetLast(lhs.Direction);
												},
												input => input.First.FeatureStruct.IsUnifiable(FeatureStruct.With(featSys).Symbol("verb").Value));

			var rule2 = new PatternRule<int>(pattern, (lhs, input, match) =>
												{
													IBidirListView<Annotation<int>> target = input.GetView(match["lhs"]);
													foreach (Annotation<int> ann in target)
														ann.FeatureStruct.AddValues(FeatureStruct.With(featSys)
															.Symbol("low-")
															.Symbol("mid+").Value);
													return target.GetLast(lhs.Direction);
												});

			var batch = new PatternRuleBatch<int>(new[] {rule1, rule2});
			batch.Compile();
			AnnotationList<int> word = CreateFeatShapeAnnotations("fazk", spanFactory, featSys);
			Assert.IsTrue(batch.Apply(word));
		}

		private static AnnotationList<int> CreateFeatShapeAnnotations(string str, SpanFactory<int> spanFactory, FeatureSystem featSys)
		{
			var annList = new AnnotationList<int>();
			for (int i = 0; i < str.Length; i++)
			{
				FeatureStruct fs = null;
				switch (str[i])
				{
					case 'f':
						fs = FeatureStruct.With(featSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab+")
							.Symbol("low-")
							.Symbol("mid-")
							.Feature("type").EqualTo("Seg").Value;
						break;
					case 'k':
						fs = FeatureStruct.With(featSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-")
							.Feature("type").EqualTo("Seg").Value;
						break;
					case 'z':
						fs = FeatureStruct.With(featSys)
							.Symbol("cons+")
							.Symbol("voice+")
							.Symbol("sib+")
							.Symbol("cor+")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-")
							.Feature("type").EqualTo("Seg").Value;
						break;
					case 's':
						fs = FeatureStruct.With(featSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib+")
							.Symbol("cor+")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-")
							.Feature("type").EqualTo("Seg").Value;
						break;
					case 'a':
						fs = FeatureStruct.With(featSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low+")
							.Symbol("mid-")
							.Feature("type").EqualTo("Seg").Value;
						break;
					case 'i':
						fs = FeatureStruct.With(featSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-")
							.Feature("type").EqualTo("Seg").Value;
						break;
					case 'e':
						fs = FeatureStruct.With(featSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid+")
							.Feature("type").EqualTo("Seg").Value;
						break;
					case '+':
						fs = FeatureStruct.With(featSys)
							.Feature("str").EqualTo("+")
							.Feature("type").EqualTo("Bdry").Value;
						break;
				}
				annList.Add(new Annotation<int>(spanFactory.Create(i, i + 1), fs));
			}
			annList.Add(new Annotation<int>(spanFactory.Create(0, str.Length), FeatureStruct.With(featSys).Feature("type").EqualTo("Shape").Symbol("noun").Value));
			return annList;
		}

		private static FeatureSystem CreateFeatureSystem()
		{
			return FeatureSystem.With
				.SymbolicFeature("cons", cons => cons
					.Symbol("cons+", "+")
					.Symbol("cons-", "-"))
				.SymbolicFeature("voice", voice => voice
					.Symbol("voice+", "+")
					.Symbol("voice-", "-"))
				.SymbolicFeature("sib", sib => sib
					.Symbol("sib+", "+")
					.Symbol("sib-", "-"))
				.SymbolicFeature("cor", cor => cor
					.Symbol("cor+", "+")
					.Symbol("cor-", "-"))
				.SymbolicFeature("lab", lab => lab
					.Symbol("lab+", "+")
					.Symbol("lab-", "-"))
				.SymbolicFeature("low", low => low
					.Symbol("low+", "+")
					.Symbol("low-", "-"))
				.SymbolicFeature("mid", mid => mid
					.Symbol("mid+", "+")
					.Symbol("mid-", "-"))
				.StringFeature("type")
				.SymbolicFeature("pos", pos => pos
					.Symbol("noun")
					.Symbol("verb")).Value;
		}
	}
}
