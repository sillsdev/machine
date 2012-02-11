using System.Linq;
using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Test
{
	public class PatternTest : PhoneticTestBase
	{
		[Test]
		public void PatternMatch()
		{
			var pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Or
				.Group(g => g
					.Group("adj", adj => adj.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value))
					.Group("noun", noun => noun.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value))
					.Group("verb", verb => verb.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value))
					.Group("range", range => range
						.Group("adv", adv => adv.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value)).LazyOptional)).Value;

			StringData sentence = CreateStringData("the old, angry man slept well.");
			sentence.Annotations.Add(0, 2, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value, true);
			sentence.Annotations.Add(4, 6, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 13, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value, true);
			sentence.Annotations.Add(15, 17, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 23, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 28, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
			sentence.Annotations.Add(0, 17, FeatureStruct.New().Symbol(NP).Value);
			sentence.Annotations.Add(19, 28, FeatureStruct.New().Symbol(VP).Value);

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern, new MatcherSettings<int>
			                                                                 	{
			                                                                 		Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word
			                                                                 	});
			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(0, matches[0].Span.Start);
			Assert.AreEqual(23, matches[0].Span.End);
			Assert.AreEqual(4, matches[0]["adj"].Span.Start);
			Assert.AreEqual(13, matches[0]["adj"].Span.End);

			matcher = new Matcher<StringData, int>(SpanFactory, pattern, new MatcherSettings<int>
			                                                             	{
			                                                             		Direction = Direction.RightToLeft,
																				Filter = ann => ((FeatureSymbol)ann.FeatureStruct.GetValue(Type)) == Word
			                                                             	});
			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(4, matches[0].Span.Start);
			Assert.AreEqual(28, matches[0].Span.End);
			Assert.AreEqual(4, matches[0]["adj"].Span.Start);
			Assert.AreEqual(6, matches[0]["adj"].Span.End);
			Assert.AreEqual(0, matches[1].Span.Start);
			Assert.AreEqual(2, matches[1].Span.End);
		}

		[Test]
		public void Variables()
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
						.Feature("str").EqualTo("a").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			StringData word = CreateStringData("fazk");
			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(word));
		}
	}
}
