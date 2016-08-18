using System.Linq;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Tests.Matching
{
	public class MatcherTests : PhoneticTestsBase
	{
		[Test]
		public void SimplePattern()
		{
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 1), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(10, 11), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
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

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Or
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("son+").Symbol(Seg).Symbol("syl-").Value).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(1, 2), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(8, 9), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
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

			pattern = Pattern<AnnotatedStringData, int>.New()
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

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).Optional
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(15, 17), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
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

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Group(g => g
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(",").Value).Optional
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)).Optional
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Group(g => g
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(",").Value).Optional
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)).LazyOptional
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore.Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 14), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
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

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).LazyZeroOrMore.Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).OneOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).OneOrMore.Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match.Span);
			match = matcher.Match(sentence, 16);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
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

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).OneOrMore
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).LazyOneOrMore.Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value).Range(0, 2).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 12), match.Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
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

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value).Range(1, 3).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value).LazyRange(1, 3).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
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
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Group("onset", onset => onset
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Group("coda", coda => coda
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			Assert.IsTrue(match.GroupCaptures["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 2), match.GroupCaptures["onset"].Span);
			Assert.IsFalse(match.GroupCaptures["coda"].Success);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 14), match.Span);
			Assert.IsFalse(match.GroupCaptures["onset"].Success);
			Assert.IsTrue(match.GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 14), match.GroupCaptures["coda"].Span);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(6, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[1].Span);
			Assert.IsFalse(matches[1].GroupCaptures["onset"].Success);
			Assert.IsTrue(matches[1].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(5, 7), matches[1].GroupCaptures["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[5].Span);
			Assert.IsTrue(matches[5].GroupCaptures["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(25, 26), matches[5].GroupCaptures["onset"].Span);
			Assert.IsTrue(matches[5].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(27, 29), matches[5].GroupCaptures["coda"].Span);
			matches = matcher.Matches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 14), matches[0].Span);
			Assert.IsFalse(matches[0].GroupCaptures["onset"].Success);
			Assert.IsTrue(matches[0].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 14), matches[0].GroupCaptures["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[1].Span);
			Assert.IsTrue(matches[1].GroupCaptures["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 16), matches[1].GroupCaptures["onset"].Span);
			Assert.IsTrue(matches[1].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(17, 18), matches[1].GroupCaptures["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(25, 29), matches[3].Span);
			matches = matcher.Matches(sentence, 29).ToArray();
			Assert.AreEqual(0, matches.Length);

			matches = matcher.AllMatches(sentence).ToArray();
			Assert.AreEqual(30, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].Span);
			Assert.IsTrue(matches[0].GroupCaptures["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 2), matches[0].GroupCaptures["onset"].Span);
			Assert.IsFalse(matches[0].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(1, 3), matches[1].Span);
			Assert.IsTrue(matches[1].GroupCaptures["onset"].Success);
			Assert.AreEqual(SpanFactory.Create(1, 2), matches[1].GroupCaptures["onset"].Span);
			Assert.IsFalse(matches[1].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(2, 3), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(4, 7), matches[3].Span);
			Assert.AreEqual(SpanFactory.Create(4, 6), matches[4].Span);
			Assert.AreEqual(SpanFactory.Create(4, 5), matches[5].Span);
			Assert.AreEqual(SpanFactory.Create(16, 17), matches[14].Span);
			Assert.AreEqual(SpanFactory.Create(26, 27), matches[29].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(24, matches.Length);
			Assert.AreEqual(SpanFactory.Create(9, 14), matches[0].Span);
			Assert.IsFalse(matches[0].GroupCaptures["onset"].Success);
			Assert.IsTrue(matches[0].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 14), matches[0].GroupCaptures["coda"].Span);
			Assert.AreEqual(SpanFactory.Create(9, 13), matches[1].Span);
			Assert.IsFalse(matches[1].GroupCaptures["onset"].Success);
			Assert.IsTrue(matches[1].GroupCaptures["coda"].Success);
			Assert.AreEqual(SpanFactory.Create(10, 13), matches[1].GroupCaptures["coda"].Span);
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

			pattern = Pattern<AnnotatedStringData, int>.New()
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

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 7));
			Assert.IsFalse(matcher.IsMatch(sentence, 16));

			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 29), match.Span);
			Assert.IsTrue(match.GroupCaptures["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), match.GroupCaptures["NP"].Span);
			Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match.GroupCaptures["headNoun"].Span);
			Assert.IsTrue(match.GroupCaptures["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match.GroupCaptures["VP"].Span);
			Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match.GroupCaptures["headVerb"].Span);
			match = matcher.Match(sentence, 7);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 29), match.Span);
			Assert.IsTrue(match.GroupCaptures["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(9, 18), match.GroupCaptures["NP"].Span);
			Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match.GroupCaptures["headNoun"].Span);
			Assert.IsTrue(match.GroupCaptures["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match.GroupCaptures["VP"].Span);
			Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match.GroupCaptures["headVerb"].Span);
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
			Assert.IsTrue(matches[1].GroupCaptures["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), matches[1].GroupCaptures["NP"].Span);
			Assert.IsTrue(matches[1].GroupCaptures["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[1].GroupCaptures["headNoun"].Span);
			Assert.IsTrue(matches[1].GroupCaptures["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1].GroupCaptures["VP"].Span);
			Assert.IsTrue(matches[1].GroupCaptures["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[1].GroupCaptures["headVerb"].Span);
			Assert.AreEqual(SpanFactory.Create(15, 24), matches[7].Span);
			Assert.IsTrue(matches[7].GroupCaptures["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[7].GroupCaptures["NP"].Span);
			Assert.IsTrue(matches[7].GroupCaptures["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), matches[7].GroupCaptures["headNoun"].Span);
			Assert.IsTrue(matches[7].GroupCaptures["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[7].GroupCaptures["VP"].Span);
			Assert.IsTrue(matches[7].GroupCaptures["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), matches[7].GroupCaptures["headVerb"].Span);
			matches = matcher.AllMatches(sentence, 7).ToArray();
			Assert.AreEqual(4, matches.Length);
			matches = matcher.AllMatches(sentence, 16).ToArray();
			Assert.AreEqual(0, matches.Length);
		}

		[Test]
		public void Subpattern()
		{
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Subpattern("unvoiceInitial", unvoiceInitial => unvoiceInitial
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("voice-").Symbol("son-").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value))
				.Subpattern("word", word => word
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsTrue(matcher.IsMatch(sentence));
			Assert.IsTrue(matcher.IsMatch(sentence, 19));
			Assert.IsFalse(matcher.IsMatch(sentence, 29));

			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);
			Assert.AreEqual("unvoiceInitial", match.PatternPath[0]);
			match = matcher.Match(sentence, 19);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(19, 22), match.Span);
			Assert.AreEqual("unvoiceInitial", match.PatternPath[0]);
			match = matcher.Match(sentence, 29);
			Assert.IsFalse(match.Success);

			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
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
			AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");
			sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
			sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
			sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
			sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
			sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);

			Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>.New()
				.Group("NP", np => np
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value).Optional
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value).ZeroOrMore
					.Group("headNoun", headNoun => headNoun.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)))
				.Group("VP", vp => vp
					.Group("headVerb", headVerb => headVerb.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value))
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value).ZeroOrMore).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Assert.IsFalse(matcher.IsMatch(sentence));

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word});
			Match<AnnotatedStringData, int> match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 29), match.Span);
			Assert.IsTrue(match.GroupCaptures["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), match.GroupCaptures["NP"].Span);
			Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match.GroupCaptures["headNoun"].Span);
			Assert.IsTrue(match.GroupCaptures["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match.GroupCaptures["VP"].Span);
			Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match.GroupCaptures["headVerb"].Span);

			pattern = Pattern<AnnotatedStringData, int>.New()
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

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern, new MatcherSettings<int> { Nondeterministic = true });
			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 29), match.Span);
			Assert.IsTrue(match.GroupCaptures["NP"].Success);
			Assert.AreEqual(SpanFactory.Create(0, 18), match.GroupCaptures["NP"].Span);
			Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
			Assert.AreEqual(SpanFactory.Create(15, 18), match.GroupCaptures["headNoun"].Span);
			Assert.IsTrue(match.GroupCaptures["VP"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 29), match.GroupCaptures["VP"].Span);
			Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
			Assert.AreEqual(SpanFactory.Create(19, 24), match.GroupCaptures["headVerb"].Span);

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
				.Or
				.Group(g => g
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value)
					.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value)).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word});
			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(0, 3), match.Span);

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) == Word, Direction = Direction.RightToLeft});
			match = matcher.Match(sentence);
			Assert.IsTrue(match.Success);
			Assert.AreEqual(SpanFactory.Create(9, 29), match.Span);

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("nas+").Value).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) != Word, MatchingMethod = MatchingMethod.Unification});
			Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(4, matches.Length);

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int> {Filter = ann => ((FeatureSymbol) ann.FeatureStruct.GetValue(Type)) != Word, UseDefaults = true, MatchingMethod = MatchingMethod.Unification});
			matches = matcher.Matches(sentence).ToArray();
			Assert.AreEqual(1, matches.Length);
		}

		[Test]
		public void VariablePattern()
		{
			var pattern = Pattern<AnnotatedStringData, int>.New()
				.Group("leftEnv", leftEnv => leftEnv
					.Annotation(FeatureStruct.New(PhoneticFeatSys)
						.Symbol(Seg)
						.Symbol("cons+")
						.Feature("voice").EqualToVariable("a").Value).OneOrMore)
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
						.Feature("voice").Not.EqualToVariable("a").Value).OneOrMore).Value;

			AnnotatedStringData word = CreateStringData("fazk");
			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern);
			Match<AnnotatedStringData, int> match = matcher.Match(word);
			Assert.IsTrue(match.Success);
			Assert.AreEqual("voice-", ((FeatureSymbol) match.VariableBindings["a"]).ID);

			word = CreateStringData("dazk");
			match = matcher.Match(word);
			Assert.IsFalse(match.Success);
		}

		[Test]
		public void NondeterministicPattern()
		{
			var any = FeatureStruct.New().Value;

			var pattern = Pattern<AnnotatedStringData, int>.New()
				.Group("first", first => first.Annotation(any).OneOrMore)
				.Group("second", second => second.Annotation(any).OneOrMore).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int>
				{
					AnchoredToStart = true,
					AnchoredToEnd = true,
					AllSubmatches = true
				});
			var word = new AnnotatedStringData(SpanFactory, "test");
			word.Annotations.Add(0, 1, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("t").Value);
			word.Annotations.Add(1, 2, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("e").Value);
			word.Annotations.Add(2, 3, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("s").Value);
			word.Annotations.Add(3, 4, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("t").Value);

			Match<AnnotatedStringData, int>[] matches = matcher.AllMatches(word).ToArray();
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(SpanFactory.Create(0, 4), matches[0].Span);
			Assert.AreEqual(SpanFactory.Create(0, 3), matches[0].GroupCaptures["first"].Span);
			Assert.AreEqual(SpanFactory.Create(3, 4), matches[0].GroupCaptures["second"].Span);

			Assert.AreEqual(SpanFactory.Create(0, 4), matches[1].Span);
			Assert.AreEqual(SpanFactory.Create(0, 2), matches[1].GroupCaptures["first"].Span);
			Assert.AreEqual(SpanFactory.Create(2, 4), matches[1].GroupCaptures["second"].Span);

			Assert.AreEqual(SpanFactory.Create(0, 4), matches[2].Span);
			Assert.AreEqual(SpanFactory.Create(0, 1), matches[2].GroupCaptures["first"].Span);
			Assert.AreEqual(SpanFactory.Create(1, 4), matches[2].GroupCaptures["second"].Span);

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Group("first", g1 => g1.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value))
				.Group("second", g2 => g2.Group("third", g3 => g3.Annotation(FeatureStruct.New().Symbol(Seg).Value).Optional).ZeroOrMore).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int>
				{
					AnchoredToStart = true,
					AnchoredToEnd = true,
					AllSubmatches = true
				});

			word = CreateStringData("etested");
			matches = matcher.AllMatches(word).ToArray();
			Assert.That(matches.Length, Is.EqualTo(1));
			Assert.That(matches[0].Success, Is.True);
			Assert.That(matches[0].Span, Is.EqualTo(SpanFactory.Create(0, 7)));
			Assert.That(matches[0].GroupCaptures["first"].Span, Is.EqualTo(SpanFactory.Create(0, 1)));
			Assert.That(matches[0].GroupCaptures["second"].Span, Is.EqualTo(SpanFactory.Create(1, 7)));
			Assert.That(matches[0].GroupCaptures["third"].Span, Is.EqualTo(SpanFactory.Create(6, 7)));

			word = CreateStringData("e");
			matches = matcher.AllMatches(word).ToArray();
			Assert.That(matches.Length, Is.EqualTo(1));
			Assert.That(matches[0].Success, Is.True);
			Assert.That(matches[0].Span, Is.EqualTo(SpanFactory.Create(0, 1)));
			Assert.That(matches[0].GroupCaptures["first"].Span, Is.EqualTo(SpanFactory.Create(0, 1)));
			Assert.That(matches[0].GroupCaptures["second"].Success, Is.False);
			Assert.That(matches[0].GroupCaptures["third"].Success, Is.False);

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
				.Group("first", g => g.Annotation(FeatureStruct.New().Symbol(Seg).Value).ZeroOrMore).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int>
				{
					AnchoredToStart = true,
					AnchoredToEnd = true,
					AllSubmatches = true
				});

			word = CreateStringData("etested");
			matches = matcher.AllMatches(word).ToArray();
			Assert.That(matches.Length, Is.EqualTo(1));
			Assert.That(matches[0].Success, Is.True);
			Assert.That(matches[0].Span, Is.EqualTo(SpanFactory.Create(0, 7)));
			Assert.That(matches[0].GroupCaptures["first"].Span, Is.EqualTo(SpanFactory.Create(1, 7)));

			word = CreateStringData("e");
			matches = matcher.AllMatches(word).ToArray();
			Assert.That(matches.Length, Is.EqualTo(1));
			Assert.That(matches[0].Success, Is.True);
			Assert.That(matches[0].Span, Is.EqualTo(SpanFactory.Create(0, 1)));
			Assert.That(matches[0].GroupCaptures["first"].Success, Is.False);
		}

		[Test]
		public void DiscontiguousAnnotation()
		{
			var pattern = Pattern<AnnotatedStringData, int>.New()
				.Group("first", first => first
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
					.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value))
				.Group("second", second => second.Annotation(FeatureStruct.New().Symbol(Seg).Value).OneOrMore).Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int>
				{
					AnchoredToStart = true,
					AnchoredToEnd = true
				});
			AnnotatedStringData word = CreateStringData("ketested");
			Annotation<int> allo1 = new Annotation<int>(word.Span, FeatureStruct.New().Symbol(Allo).Value);
			allo1.Children.AddRange(word.Annotations.GetNodes(0, 2).ToArray());
			allo1.Children.AddRange(word.Annotations.GetNodes(6, 8).ToArray());
			word.Annotations.Add(allo1, false);
			Annotation<int> allo2 = new Annotation<int>(SpanFactory.Create(2, 6), FeatureStruct.New().Symbol(Allo).Value);
			allo2.Children.AddRange(word.Annotations.GetNodes(2, 6).ToArray());
			word.Annotations.Add(allo2, false);
			Match<AnnotatedStringData, int> match = matcher.Match(word);
			Assert.That(match.Success, Is.True);
			Assert.That(match.Span, Is.EqualTo(SpanFactory.Create(0, 8)));
			Assert.That(match.GroupCaptures["first"].Span, Is.EqualTo(SpanFactory.Create(0, 2)));
			Assert.That(match.GroupCaptures["second"].Span, Is.EqualTo(SpanFactory.Create(2, 8)));
		}

		[Test]
		public void OptionalAnnotation()
		{
			var pattern = Pattern<AnnotatedStringData, int>.New()
				.Annotation(FeatureStruct.New().Symbol(Seg).Value).OneOrMore.Value;

			var matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int>
				{
					AnchoredToStart = true,
					AnchoredToEnd = true
				});
			AnnotatedStringData word = CreateStringData("k");
			word.Annotations.First.Optional = true;
			Match<AnnotatedStringData, int> match = matcher.Match(word);
			Assert.That(match.Success, Is.True);

			pattern = Pattern<AnnotatedStringData, int>.New()
				.Group("first", g1 => g1.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value))
				.Group("second", g2 => g2.Annotation(FeatureStruct.New().Symbol(Seg).Value).OneOrMore).Value;

			matcher = new Matcher<AnnotatedStringData, int>(SpanFactory, pattern,
				new MatcherSettings<int>
				{
					AnchoredToStart = true,
					AnchoredToEnd = true
				});
			word = CreateStringData("+e+tested+");
			word.Annotations.First.Optional = true;
			word.Annotations.ElementAt(2).Optional = true;
			word.Annotations.Last.Optional = true;
			match = matcher.Match(word);
			Assert.That(match.Success, Is.True);
			Assert.That(match.Span, Is.EqualTo(SpanFactory.Create(0, 10)));
			Assert.That(match.GroupCaptures["first"].Span, Is.EqualTo(SpanFactory.Create(1, 3)));
			Assert.That(match.GroupCaptures["second"].Span, Is.EqualTo(SpanFactory.Create(3, 10)));
		}
	}
}
