using NUnit.Framework;
using SIL.APRE.FeatureModel;
using SIL.APRE.Patterns;
using SIL.APRE.Rules;

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

			Pattern<int> pattern = Pattern<int>.Build(spanFactory).Expression(expr => expr
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation(featSys, fs => fs
						.String("type", "Seg")
						.Symbol("cons+")
						.Variable("voice", "a")))
				.Group("lhs", lhs => lhs
					.Annotation(featSys, fs => fs
						.String("type", "Seg")
						.Symbol("cons-")
						.Symbol("low+")))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(featSys, fs => fs
						.String("type", "Seg")
						.Symbol("cons+")
						.Not().Variable("voice", "a"))));

			var rule = new Rule<int>(pattern, (lhs, input, match) =>
			                                  	{
			                                  		IBidirList<Annotation<int>> target = input.GetView(match["lhs"]);
			                                  		foreach (Annotation<int> ann in target)
			                                  			ann.FeatureStructure.AddValues(featSys.BuildFS().Symbol("low-"));
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

			Pattern<int> pattern = Pattern<int>.Build(spanFactory).Expression(expr => expr
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation(featSys, fs => fs
						.String("type", "Seg")
						.Symbol("cons+")
						.Variable("voice", "a")))
				.Group("lhs", lhs => lhs
					.Annotation(featSys, fs => fs
						.String("type", "Seg")
						.Symbol("cons-")
						.Symbol("low+")))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(featSys, fs => fs
						.String("type", "Seg")
						.Symbol("cons+")
						.Not().Variable("voice", "a"))));

			var rule1 = new Rule<int>(pattern, (lhs, input, match) =>
												{
													IBidirList<Annotation<int>> target = input.GetView(match["lhs"]);
													foreach (Annotation<int> ann in target)
														ann.FeatureStructure.AddValues(featSys.BuildFS().Symbol("low-"));
													return target.GetLast(lhs.Direction);
												});

			var rule2 = new Rule<int>(pattern, (lhs, input, match) =>
												{
													IBidirList<Annotation<int>> target = input.GetView(match["lhs"]);
													foreach (Annotation<int> ann in target)
														ann.FeatureStructure.AddValues(featSys.BuildFS().Symbol("low+"));
													return target.GetLast(lhs.Direction);
												});

			var batch = new RuleBatch<int>(new[] {rule1, rule2});
			batch.Compile();
			AnnotationList<int> word = CreateFeatShapeAnnotations("fazk", spanFactory, featSys);
			Assert.IsTrue(batch.Apply(word));
		}

		private static AnnotationList<int> CreateFeatShapeAnnotations(string str, SpanFactory<int> spanFactory, FeatureSystem featSys)
		{
			var annList = new AnnotationList<int>();
			for (int i = 0; i < str.Length; i++)
			{
				FeatureStructure fs = null;
				switch (str[i])
				{
					case 'f':
						fs = FeatureStructure.Build(featSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab+")
							.Symbol("low-")
							.String("type", "Seg");
						break;
					case 'k':
						fs = FeatureStructure.Build(featSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.String("type", "Seg");
						break;
					case 'z':
						fs = FeatureStructure.Build(featSys)
							.Symbol("cons+")
							.Symbol("voice+")
							.Symbol("sib+")
							.Symbol("cor+")
							.Symbol("lab-")
							.Symbol("low-")
							.String("type", "Seg");
						break;
					case 's':
						fs = FeatureStructure.Build(featSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib+")
							.Symbol("cor+")
							.Symbol("lab-")
							.Symbol("low-")
							.String("type", "Seg");
						break;
					case 'a':
						fs = FeatureStructure.Build(featSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low+")
							.String("type", "Seg");
						break;
					case 'ɨ':
						fs = FeatureStructure.Build(featSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.String("type", "Seg");
						break;
					case '+':
						fs = FeatureStructure.Build(featSys)
							.String("str", "+")
							.String("type", "Bdry");
						break;
				}
				annList.Add(new Annotation<int>(spanFactory.Create(i, i + 1), fs));
			}
			return annList;
		}

		private static FeatureSystem CreateFeatureSystem()
		{
			return FeatureSystem.Build()
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
				.StringFeature("type");
		}
	}
}
