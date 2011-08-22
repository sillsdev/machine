using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.APRE.FeatureModel;
using SIL.APRE.Patterns;

namespace SIL.APRE.Test
{
	[TestFixture]
	public class PatternTest
	{
		static AnnotationList<int> CreateShapeAnnotations(string str, SpanFactory<int> spanFactory, FeatureSystem featSys)
		{
			var annList = new AnnotationList<int>();
			for (int i = 0; i < str.Length; i++)
			{
				string annType = "Seg";
				switch (str[i])
				{
					case ',':
					case ' ':
					case '.':
						annType = "Bdry";
						break;
				}
				annList.Add(new Annotation<int>(spanFactory.Create(i, i + 1), featSys.BuildFS()
					.String("str", str[i].ToString())
					.String("type", annType)));
			}
			return annList;
		}

		[Test]
		public void PatternMatch()
		{
			var spanFactory = new IntegerSpanFactory();
			FeatureSystem featSys = FeatureSystem.Build()
				.StringFeature("str")
				.StringFeature("type");

			FeatureStructure types = featSys.BuildFS().String("type", "Noun", "Verb", "Det", "Adj", "Adv");
			Pattern<int> ltorPattern = Pattern<int>.Build(spanFactory).Filter(ann => ann.FeatureStructure.IsUnifiable(types, false, true))
				.Expression(expr => expr
					.Or(or => or
						.Annotation(featSys, fs => fs.String("type", "Det"))
						.Group(g => g
							.Group("adj", adj => adj.Annotation(featSys, fs => fs.String("type", "Adj")))
							.Group("noun", noun => noun.Annotation(featSys, fs => fs.String("type", "Noun")))
							.Group("verb", verb => verb.Annotation(featSys, fs => fs.String("type", "Verb")))
							.Group("range", range => range
								.Group("adv", adv => adv.Annotation(featSys, fs => fs.String("type", "Adv"))).Optional()))));

			AnnotationList<int> annList = CreateShapeAnnotations("the old, angry man slept well", spanFactory, featSys);
			annList.Add(new Annotation<int>(spanFactory.Create(0, 2), featSys.BuildFS().String("type", "Det")) { IsOptional = true });
			annList.Add(new Annotation<int>(spanFactory.Create(4, 6), featSys.BuildFS().String("type", "Adj")));
			annList.Add(new Annotation<int>(spanFactory.Create(9, 13), featSys.BuildFS().String("type", "Adj")) { IsOptional = true });
			annList.Add(new Annotation<int>(spanFactory.Create(15, 17), featSys.BuildFS().String("type", "Noun")));
			annList.Add(new Annotation<int>(spanFactory.Create(19, 23), featSys.BuildFS().String("type", "Verb")));
			annList.Add(new Annotation<int>(spanFactory.Create(25, 28), featSys.BuildFS().String("type", "Adv")));
			annList.Add(new Annotation<int>(spanFactory.Create(0, 17), featSys.BuildFS().String("type", "NP")));
			annList.Add(new Annotation<int>(spanFactory.Create(19, 28), featSys.BuildFS().String("type", "VP")));

			IEnumerable<PatternMatch<int>> matches;
			Assert.True(ltorPattern.IsMatch(annList, out matches));
			Assert.AreEqual(7, matches.Count());
			Assert.AreEqual(0, matches.First().Start);
			Assert.AreEqual(28, matches.First().End);
			Assert.AreEqual(4, matches.First()["adj"].Start);
			Assert.AreEqual(6, matches.First()["adj"].End);
			Assert.AreEqual(9, matches.Last().Start);
			Assert.AreEqual(23, matches.Last().End);

			Pattern<int> rtolPattern = ltorPattern.Reverse();
			Assert.True(rtolPattern.IsMatch(annList, out matches));
			Assert.AreEqual(7, matches.Count());
			Assert.AreEqual(0, matches.First().Start);
			Assert.AreEqual(28, matches.First().End);
			Assert.AreEqual(4, matches.First()["adj"].Start);
			Assert.AreEqual(6, matches.First()["adj"].End);
			Assert.AreEqual(0, matches.Last().Start);
			Assert.AreEqual(2, matches.Last().End);
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
							.String("str", "f")
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
							.String("str", "k")
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
							.String("str", "z")
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
							.String("str", "s")
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
							.String("str", "a")
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
							.String("str", "ɨ")
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
				.StringFeature("str")
				.StringFeature("type");
		}

		[Test]
		public void Variables()
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
						.String("str", "a")))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(featSys, fs => fs
						.String("type", "Seg")
						.Symbol("cons+")
						.Not().Variable("voice", "a"))));

			pattern.Compile();

			AnnotationList<int> word = CreateFeatShapeAnnotations("fazk", spanFactory, featSys);
			Assert.IsTrue(pattern.IsMatch(word));
		}
	}
}
