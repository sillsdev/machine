using System.Linq;
using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Test
{
	public class MatcherTest : PhoneticTestBase
	{

		[Test]
		public void SimplePattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 1), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(10, 11), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(17, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 1), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(13, 14), matches[7].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[16].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(13, matches.Length);
			Assert.AreEqual(SpanFactory.Create(10, 11), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(17, 18), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[12].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(17, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 1), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(13, 14), matches[7].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[16].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(13, matches.Length);
			Assert.AreEqual(SpanFactory.Create(10, 11), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(17, 18), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[12].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
			sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
			sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(1, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(15, 17), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[3].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(20, 22), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[2].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[3].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(20, 22), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[2].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void AlternationPattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Or
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("son+").Symbol(Seg).Symbol("syl-").Value).Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(1, 2), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(8, 9), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(16, matches.Length);
			Assert.AreEqual(SpanFactory.Create(1, 2), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(13, 14), matches[6].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[15].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(13, matches.Length);
			Assert.AreEqual(SpanFactory.Create(8, 9), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 16), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[12].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(16, matches.Length);
			Assert.AreEqual(SpanFactory.Create(1, 2), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(13, 14), matches[6].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[15].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(13, matches.Length);
			Assert.AreEqual(SpanFactory.Create(8, 9), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 16), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[12].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
			sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
			sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Or
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
				.Or
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Or
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
				.Or
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 10));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 7), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 18), match.Span);
			match = matcher.Match(sentence, 10);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 7), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(9, 18), matches[1].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 18), matches[0].Span);
			matches = matcher.Matches(sentence, 10).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 7), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(9, 18), matches[1].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 18), matches[0].Span);
			matches = matcher.AllMatches(sentence, 10).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void OptionalPattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).Optional
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value).Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(15, 17), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 22), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[3].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 22), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[2].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(20, 22), matches[4].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(20, 22), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[3].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
			sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
			sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Group(g => g
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(",").Value).Optional
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)).Optional
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsFalse(matcher.IsMatch(sentence, 7));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 14), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 14), matches[0].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 14), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(0, 7), matches[1].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(0, matches.Length);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Group(g => g
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(",").Value).Optional
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)).LazyOptional
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsFalse(matcher.IsMatch(sentence, 7));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 7), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 7), matches[0].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 7), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(0, 14), matches[1].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void ZeroOrMorePattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore.Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 14), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[5].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 14), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[3].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(30, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(2, 3), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(4, 6), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(4, 5), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(16, 17), matches[14].Span);
			Assert.AreEqual(SpanFactory.Create(26, 27), matches[29].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(24, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 14), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(9, 13), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(9, 12), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(19, 22), matches[11].Span);
			Assert.AreEqual(SpanFactory.Create(26, 27), matches[23].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
			sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
			sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).LazyZeroOrMore.Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 10), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(4, 5), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[5].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 10), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 27), matches[3].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(30, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(2, 3), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(4, 5), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(4, 6), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(26, 29), matches[29].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(24, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 10), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(9, 11), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(9, 12), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[11].Span);
			Assert.AreEqual(SpanFactory.Create(26, 29), matches[23].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void OneOrMorePattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).OneOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).OneOrMore.Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match.Span);
			match = matcher.Match(sentence, 16);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[2].Span);
			matches = matcher.Matches(sentence, 16).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[1].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(7, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(20, 24), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(20, 23), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(25, 28), matches[6].Span);
			matches = matcher.AllMatches(sentence, 16).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(20, 24), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(20, 23), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(25, 28), matches[5].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
			sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
			sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).OneOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).LazyOneOrMore.Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match.Span);
			match = matcher.Match(sentence, 16);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(19, 23), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(25, 28), matches[2].Span);
			matches = matcher.Matches(sentence, 16).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(25, 28), matches[1].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(7, matches.Length);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(20, 23), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(20, 24), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(25, 28), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[6].Span);
			matches = matcher.AllMatches(sentence, 16).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(20, 23), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(20, 24), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(25, 28), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[5].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void RangePattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value).Range(0, 2).Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 12), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(9, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[8].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(7, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 12), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 22), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[6].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(51, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(0, 2), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(0, 1), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(1, 2), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(2, 3), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(9, 10), matches[14].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[50].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(39, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 12), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(9, 11), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(9, 10), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(13, 14), matches[11].Span);
			Assert.AreEqual(SpanFactory.Create(28, 29), matches[38].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
			sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
			sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value).Range(1, 3).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 13), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[5].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 13), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[3].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(33, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(0, 2), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(4, 6), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(12, 14), matches[14].Span);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[32].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(27, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 13), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(9, 12), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(9, 11), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(16, 18), matches[11].Span);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[26].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value).LazyRange(1, 3).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 2), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 11), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(9, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 2), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(15, 17), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[8].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(7, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 11), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(19, 21), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[6].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(33, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 2), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(4, 6), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(12, 14), matches[14].Span);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[32].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(27, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 11), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(9, 12), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(9, 13), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(16, 18), matches[11].Span);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[26].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void CapturingGroupPattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Group("onset", onset => onset
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Group("coda", coda => coda
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore).Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			Assert.IsTrue(match["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 2), match["onset"].Span);
			Assert.IsFalse(match["coda"].Success);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 14), match.Span);
			Assert.IsFalse(match["onset"].Success);
			Assert.IsTrue(match["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 14), match["coda"].Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[1].Span);
			Assert.IsFalse(matches[1]["onset"].Success);
			Assert.IsTrue(matches[1]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(5, 7), matches[1]["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[5].Span);
			Assert.IsTrue(matches[5]["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(25, 26), matches[5]["onset"].Span);
			Assert.IsTrue(matches[5]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[5]["coda"].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 14), matches[0].Span);
			Assert.IsFalse(matches[0]["onset"].Success);
			Assert.IsTrue(matches[0]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 14), matches[0]["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[1].Span);
			Assert.IsTrue(matches[1]["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 16), matches[1]["onset"].Span);
			Assert.IsTrue(matches[1]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(17, 18), matches[1]["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[3].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(30, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.IsTrue(matches[0]["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 2), matches[0]["onset"].Span);
			Assert.IsFalse(matches[0]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[1].Span);
			Assert.IsTrue(matches[1]["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(1, 2), matches[1]["onset"].Span);
			Assert.IsFalse(matches[1]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(2, 3), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(4, 6), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(4, 5), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(16, 17), matches[14].Span);
			Assert.AreEqual(SpanFactory.Create(26, 27), matches[29].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(24, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 14), matches[0].Span);
			Assert.IsFalse(matches[0]["onset"].Success);
			Assert.IsTrue(matches[0]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 14), matches[0]["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(9, 13), matches[1].Span);
			Assert.IsFalse(matches[1]["onset"].Success);
			Assert.IsTrue(matches[1]["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 13), matches[1]["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(9, 12), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(19, 22), matches[11].Span);
			Assert.AreEqual(SpanFactory.Create(26, 27), matches[23].Span);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);

			pattern = Pattern<StringData, int>.New()
				.Group("NP", np => np
					.Group(det => det
						.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
						.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore).Optional
					.Group(adj => adj
						.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
						.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore).ZeroOrMore
					.Group("headNoun", headNoun => headNoun.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)))
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore
				.Group("VP", vp => vp
					.Group("headVerb", headVerb => headVerb.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value))
					.Group(adv => adv
						.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore
						.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value)).ZeroOrMore).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 16));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 29), match.Span);
			Assert.IsTrue(match["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), match["NP"].Span);
			Assert.IsTrue(match["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match["headNoun"].Span);
			Assert.IsTrue(match["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match["VP"].Span);
			Assert.IsTrue(match["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match["headVerb"].Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 29), match.Span);
			Assert.IsTrue(match["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(9, 18), match["NP"].Span);
			Assert.IsTrue(match["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match["headNoun"].Span);
			Assert.IsTrue(match["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match["VP"].Span);
			Assert.IsTrue(match["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match["headVerb"].Span);
			match = matcher.Match(sentence, 16);
			Assert.IsFalse(match.Success);

			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(1, matches.Length);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(1, matches.Length);
			matches = matcher.Matches(sentence, 16).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(8, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 24), matches[1].Span);
			Assert.IsTrue(matches[1]["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), matches[1]["NP"].Span);
			Assert.IsTrue(matches[1]["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[1]["headNoun"].Span);
			Assert.IsTrue(matches[1]["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1]["VP"].Span);
			Assert.IsTrue(matches[1]["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1]["headVerb"].Span);
			Assert.AreEqual(SpanFactory.Create(15, 24), matches[7].Span);
			Assert.IsTrue(matches[7]["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[7]["NP"].Span);
			Assert.IsTrue(matches[7]["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[7]["headNoun"].Span);
			Assert.IsTrue(matches[7]["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[7]["VP"].Span);
			Assert.IsTrue(matches[7]["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[7]["headVerb"].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			matches = matcher.AllMatches(sentence, 16).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void Subpattern()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Subpattern("unvoiceInitial", unvoiceInitial => unvoiceInitial
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("voice-").Symbol("son-").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value))
				.Subpattern("word", word => word
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore).Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 19));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			Assert.AreEqual("unvoiceInitial", match.PatternPath[0]);
			match = matcher.Match(sentence, 19);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(19, 22), match.Span);
			Assert.AreEqual("unvoiceInitial", match.PatternPath[0]);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual("unvoiceInitial", matches[0].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[1].Span);
			Assert.AreEqual("word", matches[1].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[5].Span);
			Assert.AreEqual("word", matches[5].PatternPath[0]);
			matches = matcher.Matches(sentence, 19).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(SpanFactory.Create(19, 22), matches[0].Span);
			Assert.AreEqual("unvoiceInitial", matches[0].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[1].Span);
			Assert.AreEqual("word", matches[1].PatternPath[0]);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(32, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual("unvoiceInitial", matches[0].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[1].Span);
			Assert.AreEqual("word", matches[1].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[2].Span);
			Assert.AreEqual("word", matches[2].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(16, 17), matches[15].Span);
			Assert.AreEqual("word", matches[15].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(26, 27), matches[31].Span);
			Assert.AreEqual("word", matches[31].PatternPath[0]);
			matches = matcher.AllMatches(sentence, 19).ToArray();
			Assert.AreEqual(16, matches.Length);
			Assert.AreEqual(SpanFactory.Create(19, 22), matches[0].Span);
			Assert.AreEqual("unvoiceInitial", matches[0].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1].Span);
			Assert.AreEqual("word", matches[1].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(19, 23), matches[2].Span);
			Assert.AreEqual("word", matches[2].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(21, 24), matches[7].Span);
			Assert.AreEqual("word", matches[7].PatternPath[0]);
			Assert.AreEqual(SpanFactory.Create(26, 27), matches[15].Span);
			Assert.AreEqual("word", matches[15].PatternPath[0]);
			matches = matcher.AllMatches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void MatcherSettings()
		{
			StringData sentence = CreateStringData("the old, angry man slept well.");
			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);

			Pattern<StringData, int> pattern = Pattern<StringData, int>.New()
				.Group("NP", np => np
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value).Optional
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value).ZeroOrMore
					.Group("headNoun", headNoun => headNoun.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)))
				.Group("VP", vp => vp
					.Group("headVerb", headVerb => headVerb.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value))
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value).ZeroOrMore).Value;

			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Assert.IsFalse(matcher.IsMatch(sentence));

			matcher = new Matcher<StringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word});
			Match<StringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 29), match.Span);
			Assert.IsTrue(match["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), match["NP"].Span);
			Assert.IsTrue(match["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match["headNoun"].Span);
			Assert.IsTrue(match["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match["VP"].Span);
			Assert.IsTrue(match["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match["headVerb"].Span);

			pattern = Pattern<StringData, int>.New()
				.Group("NP", np => np
					.Group(det => det
						.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
						.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore).Optional
					.Group(adj => adj
						.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
						.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore).ZeroOrMore
					.Group("headNoun", headNoun => headNoun.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)))
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore
				.Group("VP", vp => vp
					.Group("headVerb", headVerb => headVerb.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value))
					.Group(adv => adv
						.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value).OneOrMore
						.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value)).ZeroOrMore).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern, new MatcherSettings<int> {Quasideterministic = true});
			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 29), match.Span);
			Assert.IsTrue(match["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), match["NP"].Span);
			Assert.IsTrue(match["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match["headNoun"].Span);
			Assert.IsTrue(match["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match["VP"].Span);
			Assert.IsTrue(match["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match["headVerb"].Span);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Or
				.Group(g => g
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value)
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value)).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word});
			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);

			matcher = new Matcher<StringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word, Direction = Direction.RightToLeft});
			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 29), match.Span);

			pattern = Pattern<StringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("nas+").Value).Value;

			matcher = new Matcher<StringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) != Word});
			Match<StringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(4, matches.Length);

			matcher = new Matcher<StringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) != Word, UseDefaultsForMatching = true});
			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(1, matches.Length);
		}

		[Test]
		public void VariablePattern()
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
						.Symbol("son+")
						.Symbol("syl+")
						.Symbol("cons-")
						.Symbol("high-")
						.Symbol("back-")
						.Symbol("front+")
						.Symbol("low+")
						.Symbol("rnd-").Value))
				.Group("rightEnv", rightEnv => rightEnv
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons+")
						.Feature("voice").Not.EqualToVariable("a").Value)).Value;

			StringData word = CreateStringData("fazk");
			var matcher = new Matcher<StringData, int>(SpanFactory, pattern);
			Match<StringData, int> match = matcher.Match(word);
			Assert.IsTrue(match.Success);
			Assert.AreEqual("voice-", ((FeatureSymbol) match.VariableBindings["a"]).ID);

			word = CreateStringData("dazk");
			match = matcher.Match(word);
			Assert.IsFalse(match.Success);
		}
	}
}
