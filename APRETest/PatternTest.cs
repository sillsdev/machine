using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

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
				annList.Add(new Annotation<int>(spanFactory.Create(i, i + 1), FeatureStruct.With(featSys)
					.Feature("str").EqualTo(str[i].ToString())
					.Feature("type").EqualTo(annType).Value));
			}
			return annList;
		}

		[Test]
		public void PatternMatch()
		{
			var spanFactory = new IntegerSpanFactory();
			FeatureSystem featSys = FeatureSystem.With
				.StringFeature("str")
				.StringFeature("type").Value;

			FeatureStruct types = FeatureStruct.With(featSys).Feature("type").EqualTo("Noun", "Verb", "Det", "Adj", "Adv").Value;
			Pattern<int> ltorPattern = Pattern<int>.With(spanFactory).AnnotationsAllowableWhere(ann => ann.FeatureStruct.IsUnifiable(types))
				.Annotation(FeatureStruct.With(featSys).Feature("type").EqualTo("Det").Value)
				.Or
				.Group(g => g
					.Group("adj", adj => adj.Annotation(FeatureStruct.With(featSys).Feature("type").EqualTo("Adj").Value))
					.Group("noun", noun => noun.Annotation(FeatureStruct.With(featSys).Feature("type").EqualTo("Noun").Value))
					.Group("verb", verb => verb.Annotation(FeatureStruct.With(featSys).Feature("type").EqualTo("Verb").Value))
					.Group("range", range => range
						.Group("adv", adv => adv.Annotation(FeatureStruct.With(featSys).Feature("type").EqualTo("Adv").Value)).LazyOptional)).Value;
			ltorPattern.Compile();

			AnnotationList<int> annList = CreateShapeAnnotations("the old, angry man slept well", spanFactory, featSys);
			annList.Add(new Annotation<int>(spanFactory.Create(0, 2), FeatureStruct.With(featSys).Feature("type").EqualTo("Det").Value) { Optional = true });
			annList.Add(new Annotation<int>(spanFactory.Create(4, 6), FeatureStruct.With(featSys).Feature("type").EqualTo("Adj").Value));
			annList.Add(new Annotation<int>(spanFactory.Create(9, 13), FeatureStruct.With(featSys).Feature("type").EqualTo("Adj").Value) { Optional = true });
			annList.Add(new Annotation<int>(spanFactory.Create(15, 17), FeatureStruct.With(featSys).Feature("type").EqualTo("Noun").Value));
			annList.Add(new Annotation<int>(spanFactory.Create(19, 23), FeatureStruct.With(featSys).Feature("type").EqualTo("Verb").Value));
			annList.Add(new Annotation<int>(spanFactory.Create(25, 28), FeatureStruct.With(featSys).Feature("type").EqualTo("Adv").Value));
			annList.Add(new Annotation<int>(spanFactory.Create(0, 17), FeatureStruct.With(featSys).Feature("type").EqualTo("NP").Value));
			annList.Add(new Annotation<int>(spanFactory.Create(19, 28), FeatureStruct.With(featSys).Feature("type").EqualTo("VP").Value));

			IEnumerable<PatternMatch<int>> matches;
			Assert.True(ltorPattern.IsMatch(annList, out matches));
			Assert.AreEqual(4, matches.Count());
			Assert.AreEqual(0, matches.First().Start);
			Assert.AreEqual(23, matches.First().End);
			Assert.AreEqual(4, matches.First()["adj"].Start);
			Assert.AreEqual(13, matches.First()["adj"].End);
			Assert.AreEqual(9, matches.Last().Start);
			Assert.AreEqual(23, matches.Last().End);

			Pattern<int> rtolPattern = ltorPattern.Reverse();
			rtolPattern.Compile();
			Assert.True(rtolPattern.IsMatch(annList, out matches));
			Assert.AreEqual(7, matches.Count());
			Assert.AreEqual(0, matches.First().Start);
			Assert.AreEqual(28, matches.First().End);
			Assert.AreEqual(0, matches.First()["adj"].Start);
			Assert.AreEqual(6, matches.First()["adj"].End);
			Assert.AreEqual(0, matches.Last().Start);
			Assert.AreEqual(2, matches.Last().End);
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
							.Feature("str").EqualTo("f")
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
							.Feature("str").EqualTo("k")
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
							.Feature("str").EqualTo("z")
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
							.Feature("str").EqualTo("s")
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
							.Feature("str").EqualTo("a")
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
							.Feature("str").EqualTo("i")
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
				.StringFeature("str")
				.StringFeature("type").Value;
		}

		[Test]
		public void Variables()
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
						.Feature("str").EqualTo("a").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(FeatureStruct.With(featSys)
						.Feature("type").EqualTo("Seg")
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			pattern.Compile();

			AnnotationList<int> word = CreateFeatShapeAnnotations("fazk", spanFactory, featSys);
			Assert.IsTrue(pattern.IsMatch(word));
		}
	}
}
