using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.APRE.Test
{
	public class PatternTest : PhoneticTestBase
	{
		[Test]
		public void PatternMatch()
		{
			var ltorPattern = Pattern<StringData, int>.New(SpanFactory).AnnotationsAllowableWhere(ann => ann.Type == "Word")
				.Annotation("Word", FeatureStruct.New(WordFeatSys).Symbol("det").Value)
				.Or
				.Group(g => g
					.Group("adj", adj => adj.Annotation("Word", FeatureStruct.New(WordFeatSys).Symbol("adj").Value))
					.Group("noun", noun => noun.Annotation("Word", FeatureStruct.New(WordFeatSys).Symbol("noun").Value))
					.Group("verb", verb => verb.Annotation("Word", FeatureStruct.New(WordFeatSys).Symbol("verb").Value))
					.Group("range", range => range
						.Group("adv", adv => adv.Annotation("Word", FeatureStruct.New(WordFeatSys).Symbol("adv").Value)).LazyOptional)).Value;
			ltorPattern.Compile();

			StringData sentence = CreateStringData("the old, angry man slept well.");
			sentence.Annotations.Add("Word", 0, 2, FeatureStruct.New(WordFeatSys).Symbol("det").Value, true);
			sentence.Annotations.Add("Word", 4, 6, FeatureStruct.New(WordFeatSys).Symbol("adj").Value);
			sentence.Annotations.Add("Word", 9, 13, FeatureStruct.New(WordFeatSys).Symbol("adj").Value, true);
			sentence.Annotations.Add("Word", 15, 17, FeatureStruct.New(WordFeatSys).Symbol("noun").Value);
			sentence.Annotations.Add("Word", 19, 23, FeatureStruct.New(WordFeatSys).Symbol("verb").Value);
			sentence.Annotations.Add("Word", 25, 28, FeatureStruct.New(WordFeatSys).Symbol("adv").Value);
			sentence.Annotations.Add("NP", 0, 17, new FeatureStruct());
			sentence.Annotations.Add("VP", 19, 28, new FeatureStruct());

			IEnumerable<PatternMatch<int>> matches;
			Assert.True(ltorPattern.IsMatch(sentence, out matches));
			Assert.AreEqual(3, matches.Count());
			Assert.AreEqual(0, matches.First().Start);
			Assert.AreEqual(23, matches.First().End);
			Assert.AreEqual(4, matches.First()["adj"].Start);
			Assert.AreEqual(13, matches.First()["adj"].End);
			Assert.AreEqual(9, matches.Last().Start);
			Assert.AreEqual(23, matches.Last().End);

			Pattern<StringData, int> rtolPattern = ltorPattern.Reverse();
			rtolPattern.Compile();
			Assert.True(rtolPattern.IsMatch(sentence, out matches));
			Assert.AreEqual(5, matches.Count());
			Assert.AreEqual(4, matches.First().Start);
			Assert.AreEqual(28, matches.First().End);
			Assert.AreEqual(4, matches.First()["adj"].Start);
			Assert.AreEqual(6, matches.First()["adj"].End);
			Assert.AreEqual(0, matches.Last().Start);
			Assert.AreEqual(2, matches.Last().End);
		}

		[Test]
		public void Variables()
		{
			var pattern = Pattern<StringData, int>.New(SpanFactory)
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value))
				.Group("target", target => target
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Feature("str").EqualTo("a").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation("Seg", FeatureStruct.New(PhoneticFeatSys)
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			pattern.Compile();

			StringData word = CreateStringData("fazk");
			Assert.IsTrue(pattern.IsMatch(word));
		}
	}
}
