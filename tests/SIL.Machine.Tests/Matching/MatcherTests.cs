using System.Linq;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching
{
    public class MatcherTests : PhoneticTestsBase
    {
        [Test]
        public void SimplePattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 1)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(10, 11)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(17));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[7].Range, Is.EqualTo(Range<int>.Create(13, 14)));
            Assert.That(matches[16].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(13));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(10, 11)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(17, 18)));
            Assert.That(matches[12].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(17));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[7].Range, Is.EqualTo(Range<int>.Create(13, 14)));
            Assert.That(matches[16].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(13));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(10, 11)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(17, 18)));
            Assert.That(matches[12].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
            sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
            sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
            sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
            sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
            sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(1, 3)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(15, 17)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(3));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(20, 22)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(3));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(20, 22)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
        }

        [Test]
        public void AlternationPattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
                .Or.Annotation(
                    FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("son+").Symbol(Seg).Symbol("syl-").Value
                )
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(1, 2)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(8, 9)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(16));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(1, 2)));
            Assert.That(matches[6].Range, Is.EqualTo(Range<int>.Create(13, 14)));
            Assert.That(matches[15].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(13));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(8, 9)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(15, 16)));
            Assert.That(matches[12].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(16));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(1, 2)));
            Assert.That(matches[6].Range, Is.EqualTo(Range<int>.Create(13, 14)));
            Assert.That(matches[15].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(13));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(8, 9)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(15, 16)));
            Assert.That(matches[12].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
            sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
            sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
            sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
            sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
            sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                .Or.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                .Or.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
                .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                .Or.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                .Or.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 10));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 7)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 18)));
            match = matcher.Match(sentence, 10);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 7)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 18)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 18)));
            matches = matcher.Matches(sentence, 10).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 7)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 18)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 18)));
            matches = matcher.AllMatches(sentence, 10).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
        }

        [Test]
        public void OptionalPattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .Optional.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(15, 17)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(3));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(20, 22)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(20, 22)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
            sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
            sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
            sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
            sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
            sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
                .Group(
                    g =>
                        g.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                            .Annotation(
                                FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(",").Value
                            )
                            .Optional.Annotation(
                                FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value
                            )
                )
                .Optional.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsFalse(matcher.IsMatch(sentence, 7));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 14)));
            match = matcher.Match(sentence, 7);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 14)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 14)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 7)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
                .Group(
                    g =>
                        g.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                            .Annotation(
                                FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(",").Value
                            )
                            .Optional.Annotation(
                                FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value
                            )
                )
                .LazyOptional.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsFalse(matcher.IsMatch(sentence, 7));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 7)));
            match = matcher.Match(sentence, 7);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 7)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 7)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 14)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
        }

        [Test]
        public void ZeroOrMorePattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .ZeroOrMore.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .ZeroOrMore.Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 14)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 14)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(30));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(2, 3)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(4, 6)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(4, 5)));
            Assert.That(matches[14].Range, Is.EqualTo(Range<int>.Create(16, 17)));
            Assert.That(matches[29].Range, Is.EqualTo(Range<int>.Create(26, 27)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(24));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 14)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 13)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(9, 12)));
            Assert.That(matches[11].Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(matches[23].Range, Is.EqualTo(Range<int>.Create(26, 27)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
            sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
            sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
            sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
            sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
            sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .ZeroOrMore.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .LazyZeroOrMore.Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 10)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(4, 5)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 10)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 27)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(30));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(2, 3)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(4, 5)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(4, 6)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.That(matches[29].Range, Is.EqualTo(Range<int>.Create(26, 29)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(24));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 10)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 11)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(9, 12)));
            Assert.That(matches[11].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[23].Range, Is.EqualTo(Range<int>.Create(26, 29)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
        }

        [Test]
        public void OneOrMorePattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .OneOrMore.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .OneOrMore.Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(15, 18)));
            match = matcher.Match(sentence, 16);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(19, 24)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(3));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.Matches(sentence, 16).ToArray();
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(7));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(20, 24)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(20, 23)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            Assert.That(matches[6].Range, Is.EqualTo(Range<int>.Create(25, 28)));
            matches = matcher.AllMatches(sentence, 16).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(20, 24)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(20, 23)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 28)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
            sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
            sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
            sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
            sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
            sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .OneOrMore.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                .LazyOneOrMore.Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(15, 18)));
            match = matcher.Match(sentence, 16);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(19, 23)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(3));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(25, 28)));
            matches = matcher.Matches(sentence, 16).ToArray();
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(25, 28)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(7));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(20, 23)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(20, 24)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 28)));
            Assert.That(matches[6].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.AllMatches(sentence, 16).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(20, 23)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(20, 24)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(25, 28)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
        }

        [Test]
        public void RangePattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
                .Range(0, 2)
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 12)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(9));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.That(matches[8].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(7));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 12)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(matches[6].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(51));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(1, 2)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(2, 3)));
            Assert.That(matches[14].Range, Is.EqualTo(Range<int>.Create(9, 10)));
            Assert.That(matches[50].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(39));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 12)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 11)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(9, 10)));
            Assert.That(matches[11].Range, Is.EqualTo(Range<int>.Create(13, 14)));
            Assert.That(matches[38].Range, Is.EqualTo(Range<int>.Create(28, 29)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
            sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
            sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
            sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);
            sentence.Annotations.Add(0, 18, FeatureStruct.New().Symbol(NP).Value);
            sentence.Annotations.Add(19, 29, FeatureStruct.New().Symbol(VP).Value);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
                .Range(1, 3)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 13)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 13)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(33));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(4, 6)));
            Assert.That(matches[14].Range, Is.EqualTo(Range<int>.Create(12, 14)));
            Assert.That(matches[32].Range, Is.EqualTo(Range<int>.Create(27, 29)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(27));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 13)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 12)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(9, 11)));
            Assert.That(matches[11].Range, Is.EqualTo(Range<int>.Create(16, 18)));
            Assert.That(matches[26].Range, Is.EqualTo(Range<int>.Create(27, 29)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Value)
                .LazyRange(1, 3)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 2)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 11)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(9));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(15, 17)));
            Assert.That(matches[8].Range, Is.EqualTo(Range<int>.Create(27, 29)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(7));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 11)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(19, 21)));
            Assert.That(matches[6].Range, Is.EqualTo(Range<int>.Create(27, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(33));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(4, 6)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.That(matches[14].Range, Is.EqualTo(Range<int>.Create(12, 14)));
            Assert.That(matches[32].Range, Is.EqualTo(Range<int>.Create(27, 29)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(27));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 11)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 12)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(9, 13)));
            Assert.That(matches[11].Range, Is.EqualTo(Range<int>.Create(16, 18)));
            Assert.That(matches[26].Range, Is.EqualTo(Range<int>.Create(27, 29)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
        }

        [Test]
        public void CapturingGroupPattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "onset",
                    onset =>
                        onset.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
                )
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Group(
                    "coda",
                    coda =>
                        coda.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value).ZeroOrMore
                )
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.IsTrue(match.GroupCaptures["onset"].Success);
            Assert.That(match.GroupCaptures["onset"].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.IsFalse(match.GroupCaptures["coda"].Success);
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 14)));
            Assert.IsFalse(match.GroupCaptures["onset"].Success);
            Assert.IsTrue(match.GroupCaptures["coda"].Success);
            Assert.That(match.GroupCaptures["coda"].Range, Is.EqualTo(Range<int>.Create(10, 14)));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.IsFalse(matches[1].GroupCaptures["onset"].Success);
            Assert.IsTrue(matches[1].GroupCaptures["coda"].Success);
            Assert.That(matches[1].GroupCaptures["coda"].Range, Is.EqualTo(Range<int>.Create(5, 7)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            Assert.IsTrue(matches[5].GroupCaptures["onset"].Success);
            Assert.That(matches[5].GroupCaptures["onset"].Range, Is.EqualTo(Range<int>.Create(25, 26)));
            Assert.IsTrue(matches[5].GroupCaptures["coda"].Success);
            Assert.That(matches[5].GroupCaptures["coda"].Range, Is.EqualTo(Range<int>.Create(27, 29)));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 14)));
            Assert.IsFalse(matches[0].GroupCaptures["onset"].Success);
            Assert.IsTrue(matches[0].GroupCaptures["coda"].Success);
            Assert.That(matches[0].GroupCaptures["coda"].Range, Is.EqualTo(Range<int>.Create(10, 14)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(matches[1].GroupCaptures["onset"].Success);
            Assert.That(matches[1].GroupCaptures["onset"].Range, Is.EqualTo(Range<int>.Create(15, 16)));
            Assert.IsTrue(matches[1].GroupCaptures["coda"].Success);
            Assert.That(matches[1].GroupCaptures["coda"].Range, Is.EqualTo(Range<int>.Create(17, 18)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(30));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.IsTrue(matches[0].GroupCaptures["onset"].Success);
            Assert.That(matches[0].GroupCaptures["onset"].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.IsFalse(matches[0].GroupCaptures["coda"].Success);
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.IsTrue(matches[1].GroupCaptures["onset"].Success);
            Assert.That(matches[1].GroupCaptures["onset"].Range, Is.EqualTo(Range<int>.Create(1, 2)));
            Assert.IsFalse(matches[1].GroupCaptures["coda"].Success);
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(2, 3)));
            Assert.That(matches[3].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.That(matches[4].Range, Is.EqualTo(Range<int>.Create(4, 6)));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(4, 5)));
            Assert.That(matches[14].Range, Is.EqualTo(Range<int>.Create(16, 17)));
            Assert.That(matches[29].Range, Is.EqualTo(Range<int>.Create(26, 27)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(24));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(9, 14)));
            Assert.IsFalse(matches[0].GroupCaptures["onset"].Success);
            Assert.IsTrue(matches[0].GroupCaptures["coda"].Success);
            Assert.That(matches[0].GroupCaptures["coda"].Range, Is.EqualTo(Range<int>.Create(10, 14)));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(9, 13)));
            Assert.IsFalse(matches[1].GroupCaptures["onset"].Success);
            Assert.IsTrue(matches[1].GroupCaptures["coda"].Success);
            Assert.That(matches[1].GroupCaptures["coda"].Range, Is.EqualTo(Range<int>.Create(10, 13)));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(9, 12)));
            Assert.That(matches[11].Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(matches[23].Range, Is.EqualTo(Range<int>.Create(26, 27)));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            sentence.Annotations.Add(0, 3, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value);
            sentence.Annotations.Add(4, 7, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(9, 14, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value);
            sentence.Annotations.Add(15, 18, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value);
            sentence.Annotations.Add(19, 24, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value);
            sentence.Annotations.Add(25, 29, FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "NP",
                    np =>
                        np.Group(
                            det =>
                                det.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                                    .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                                    .OneOrMore
                        )
                            .Optional.Group(
                                adj =>
                                    adj.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                                        .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                                        .OneOrMore
                            )
                            .ZeroOrMore.Group(
                                "headNoun",
                                headNoun =>
                                    headNoun.Annotation(
                                        FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value
                                    )
                            )
                )
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                .OneOrMore.Group(
                    "VP",
                    vp =>
                        vp.Group(
                                "headVerb",
                                headVerb =>
                                    headVerb.Annotation(
                                        FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value
                                    )
                            )
                            .Group(
                                adv =>
                                    adv.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                                        .OneOrMore.Annotation(
                                            FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value
                                        )
                            )
                            .ZeroOrMore
                )
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 7));
            Assert.IsFalse(matcher.IsMatch(sentence, 16));

            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 29)));
            Assert.IsTrue(match.GroupCaptures["NP"].Success);
            Assert.That(match.GroupCaptures["NP"].Range, Is.EqualTo(Range<int>.Create(0, 18)));
            Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
            Assert.That(match.GroupCaptures["headNoun"].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(match.GroupCaptures["VP"].Success);
            Assert.That(match.GroupCaptures["VP"].Range, Is.EqualTo(Range<int>.Create(19, 29)));
            Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
            Assert.That(match.GroupCaptures["headVerb"].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            match = matcher.Match(sentence, 7);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 29)));
            Assert.IsTrue(match.GroupCaptures["NP"].Success);
            Assert.That(match.GroupCaptures["NP"].Range, Is.EqualTo(Range<int>.Create(9, 18)));
            Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
            Assert.That(match.GroupCaptures["headNoun"].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(match.GroupCaptures["VP"].Success);
            Assert.That(match.GroupCaptures["VP"].Range, Is.EqualTo(Range<int>.Create(19, 29)));
            Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
            Assert.That(match.GroupCaptures["headVerb"].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            match = matcher.Match(sentence, 16);
            Assert.IsFalse(match.Success);

            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            matches = matcher.Matches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            matches = matcher.Matches(sentence, 16).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(8));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 24)));
            Assert.IsTrue(matches[1].GroupCaptures["NP"].Success);
            Assert.That(matches[1].GroupCaptures["NP"].Range, Is.EqualTo(Range<int>.Create(0, 18)));
            Assert.IsTrue(matches[1].GroupCaptures["headNoun"].Success);
            Assert.That(matches[1].GroupCaptures["headNoun"].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(matches[1].GroupCaptures["VP"].Success);
            Assert.That(matches[1].GroupCaptures["VP"].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.IsTrue(matches[1].GroupCaptures["headVerb"].Success);
            Assert.That(matches[1].GroupCaptures["headVerb"].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[7].Range, Is.EqualTo(Range<int>.Create(15, 24)));
            Assert.IsTrue(matches[7].GroupCaptures["NP"].Success);
            Assert.That(matches[7].GroupCaptures["NP"].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(matches[7].GroupCaptures["headNoun"].Success);
            Assert.That(matches[7].GroupCaptures["headNoun"].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(matches[7].GroupCaptures["VP"].Success);
            Assert.That(matches[7].GroupCaptures["VP"].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.IsTrue(matches[7].GroupCaptures["headVerb"].Success);
            Assert.That(matches[7].GroupCaptures["headVerb"].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            matches = matcher.AllMatches(sentence, 7).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));
            matches = matcher.AllMatches(sentence, 16).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
        }

        [Test]
        public void Subpattern()
        {
            AnnotatedStringData sentence = CreateStringData("the old, angry man slept well.");

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Subpattern(
                    "unvoiceInitial",
                    unvoiceInitial =>
                        unvoiceInitial
                            .Annotation(
                                FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("voice-").Symbol("son-").Value
                            )
                            .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                            .ZeroOrMore.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                )
                .Subpattern(
                    "word",
                    word =>
                        word.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                            .ZeroOrMore.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                            .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                            .ZeroOrMore
                )
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsTrue(matcher.IsMatch(sentence));
            Assert.IsTrue(matcher.IsMatch(sentence, 19));
            Assert.IsFalse(matcher.IsMatch(sentence, 29));

            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(match.PatternPath[0], Is.EqualTo("unvoiceInitial"));
            match = matcher.Match(sentence, 19);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(match.PatternPath[0], Is.EqualTo("unvoiceInitial"));
            match = matcher.Match(sentence, 29);
            Assert.IsFalse(match.Success);

            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(6));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[0].PatternPath[0], Is.EqualTo("unvoiceInitial"));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(4, 7)));
            Assert.That(matches[1].PatternPath[0], Is.EqualTo("word"));
            Assert.That(matches[5].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            Assert.That(matches[5].PatternPath[0], Is.EqualTo("word"));
            matches = matcher.Matches(sentence, 19).ToArray();
            Assert.That(matches.Length, Is.EqualTo(2));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(matches[0].PatternPath[0], Is.EqualTo("unvoiceInitial"));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(25, 29)));
            Assert.That(matches[1].PatternPath[0], Is.EqualTo("word"));
            matches = matcher.Matches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));

            matches = matcher.AllMatches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(32));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[0].PatternPath[0], Is.EqualTo("unvoiceInitial"));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[1].PatternPath[0], Is.EqualTo("word"));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(matches[2].PatternPath[0], Is.EqualTo("word"));
            Assert.That(matches[15].Range, Is.EqualTo(Range<int>.Create(16, 17)));
            Assert.That(matches[15].PatternPath[0], Is.EqualTo("word"));
            Assert.That(matches[31].Range, Is.EqualTo(Range<int>.Create(26, 27)));
            Assert.That(matches[31].PatternPath[0], Is.EqualTo("word"));
            matches = matcher.AllMatches(sentence, 19).ToArray();
            Assert.That(matches.Length, Is.EqualTo(16));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(19, 22)));
            Assert.That(matches[0].PatternPath[0], Is.EqualTo("unvoiceInitial"));
            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(19, 24)));
            Assert.That(matches[1].PatternPath[0], Is.EqualTo("word"));
            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(19, 23)));
            Assert.That(matches[2].PatternPath[0], Is.EqualTo("word"));
            Assert.That(matches[7].Range, Is.EqualTo(Range<int>.Create(21, 24)));
            Assert.That(matches[7].PatternPath[0], Is.EqualTo("word"));
            Assert.That(matches[15].Range, Is.EqualTo(Range<int>.Create(26, 27)));
            Assert.That(matches[15].PatternPath[0], Is.EqualTo("word"));
            matches = matcher.AllMatches(sentence, 29).ToArray();
            Assert.That(matches.Length, Is.EqualTo(0));
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

            Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "NP",
                    np =>
                        np.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                            .Optional.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                            .ZeroOrMore.Group(
                                "headNoun",
                                headNoun =>
                                    headNoun.Annotation(
                                        FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value
                                    )
                            )
                )
                .Group(
                    "VP",
                    vp =>
                        vp.Group(
                                "headVerb",
                                headVerb =>
                                    headVerb.Annotation(
                                        FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value
                                    )
                            )
                            .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value)
                            .ZeroOrMore
                )
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Assert.IsFalse(matcher.IsMatch(sentence));

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int> { Filter = ann => ((FeatureSymbol)ann.FeatureStruct.GetValue(Type)) == Word }
            );
            Match<AnnotatedStringData, int> match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 29)));
            Assert.IsTrue(match.GroupCaptures["NP"].Success);
            Assert.That(match.GroupCaptures["NP"].Range, Is.EqualTo(Range<int>.Create(0, 18)));
            Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
            Assert.That(match.GroupCaptures["headNoun"].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(match.GroupCaptures["VP"].Success);
            Assert.That(match.GroupCaptures["VP"].Range, Is.EqualTo(Range<int>.Create(19, 29)));
            Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
            Assert.That(match.GroupCaptures["headVerb"].Range, Is.EqualTo(Range<int>.Create(19, 24)));

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "NP",
                    np =>
                        np.Group(
                            det =>
                                det.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                                    .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                                    .OneOrMore
                        )
                            .Optional.Group(
                                adj =>
                                    adj.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                                        .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                                        .OneOrMore
                            )
                            .ZeroOrMore.Group(
                                "headNoun",
                                headNoun =>
                                    headNoun.Annotation(
                                        FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value
                                    )
                            )
                )
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                .OneOrMore.Group(
                    "VP",
                    vp =>
                        vp.Group(
                                "headVerb",
                                headVerb =>
                                    headVerb.Annotation(
                                        FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value
                                    )
                            )
                            .Group(
                                adv =>
                                    adv.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Value)
                                        .OneOrMore.Annotation(
                                            FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value
                                        )
                            )
                            .ZeroOrMore
                )
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int> { Nondeterministic = true }
            );
            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 29)));
            Assert.IsTrue(match.GroupCaptures["NP"].Success);
            Assert.That(match.GroupCaptures["NP"].Range, Is.EqualTo(Range<int>.Create(0, 18)));
            Assert.IsTrue(match.GroupCaptures["headNoun"].Success);
            Assert.That(match.GroupCaptures["headNoun"].Range, Is.EqualTo(Range<int>.Create(15, 18)));
            Assert.IsTrue(match.GroupCaptures["VP"].Success);
            Assert.That(match.GroupCaptures["VP"].Range, Is.EqualTo(Range<int>.Create(19, 29)));
            Assert.IsTrue(match.GroupCaptures["headVerb"].Success);
            Assert.That(match.GroupCaptures["headVerb"].Range, Is.EqualTo(Range<int>.Create(19, 24)));

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("det").Value)
                .Or.Group(
                    g =>
                        g.Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adj").Value)
                            .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("noun").Value)
                            .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("verb").Value)
                            .Annotation(FeatureStruct.New(WordFeatSys).Symbol(Word).Symbol("adv").Value)
                )
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int> { Filter = ann => ((FeatureSymbol)ann.FeatureStruct.GetValue(Type)) == Word }
            );
            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 3)));

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int>
                {
                    Filter = ann => ((FeatureSymbol)ann.FeatureStruct.GetValue(Type)) == Word,
                    Direction = Direction.RightToLeft
                }
            );
            match = matcher.Match(sentence);
            Assert.IsTrue(match.Success);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(9, 29)));

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value)
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("nas+").Value)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int>
                {
                    Filter = ann => ((FeatureSymbol)ann.FeatureStruct.GetValue(Type)) != Word,
                    MatchingMethod = MatchingMethod.Unification
                }
            );
            Match<AnnotatedStringData, int>[] matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(4));

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int>
                {
                    Filter = ann => ((FeatureSymbol)ann.FeatureStruct.GetValue(Type)) != Word,
                    UseDefaults = true,
                    MatchingMethod = MatchingMethod.Unification
                }
            );
            matches = matcher.Matches(sentence).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
        }

        [Test]
        public void VariablePattern()
        {
            var pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "leftEnv",
                    leftEnv =>
                        leftEnv
                            .Annotation(
                                FeatureStruct
                                    .New(PhoneticFeatSys)
                                    .Symbol(Seg)
                                    .Symbol("cons+")
                                    .Feature("voice")
                                    .EqualToVariable("a")
                                    .Value
                            )
                            .OneOrMore
                )
                .Group(
                    "target",
                    target =>
                        target.Annotation(
                            FeatureStruct
                                .New(PhoneticFeatSys)
                                .Symbol(Seg)
                                .Symbol("son+")
                                .Symbol("syl+")
                                .Symbol("cons-")
                                .Symbol("high-")
                                .Symbol("back-")
                                .Symbol("front+")
                                .Symbol("low+")
                                .Symbol("rnd-")
                                .Value
                        )
                )
                .Group(
                    "rightEnv",
                    rightEnv =>
                        rightEnv
                            .Annotation(
                                FeatureStruct
                                    .New(PhoneticFeatSys)
                                    .Symbol(Seg)
                                    .Symbol("cons+")
                                    .Feature("voice")
                                    .Not.EqualToVariable("a")
                                    .Value
                            )
                            .OneOrMore
                )
                .Value;

            AnnotatedStringData word = CreateStringData("fazk");
            var matcher = new Matcher<AnnotatedStringData, int>(pattern);
            Match<AnnotatedStringData, int> match = matcher.Match(word);
            Assert.IsTrue(match.Success);
            Assert.That(((FeatureSymbol)match.VariableBindings["a"]).ID, Is.EqualTo("voice-"));

            word = CreateStringData("dazk");
            match = matcher.Match(word);
            Assert.IsFalse(match.Success);
        }

        [Test]
        public void NondeterministicPattern()
        {
            var any = FeatureStruct.New().Value;

            var pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group("first", first => first.Annotation(any).OneOrMore)
                .Group("second", second => second.Annotation(any).OneOrMore)
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int>
                {
                    AnchoredToStart = true,
                    AnchoredToEnd = true,
                    AllSubmatches = true
                }
            );
            var word = new AnnotatedStringData("test");
            word.Annotations.Add(0, 1, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("t").Value);
            word.Annotations.Add(1, 2, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("e").Value);
            word.Annotations.Add(2, 3, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("s").Value);
            word.Annotations.Add(3, 4, FeatureStruct.New(PhoneticFeatSys).Feature("strRep").EqualTo("t").Value);

            Match<AnnotatedStringData, int>[] matches = matcher.AllMatches(word).ToArray();
            Assert.That(matches.Length, Is.EqualTo(3));
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 4)));
            Assert.That(matches[0].GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(0, 3)));
            Assert.That(matches[0].GroupCaptures["second"].Range, Is.EqualTo(Range<int>.Create(3, 4)));

            Assert.That(matches[1].Range, Is.EqualTo(Range<int>.Create(0, 4)));
            Assert.That(matches[1].GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.That(matches[1].GroupCaptures["second"].Range, Is.EqualTo(Range<int>.Create(2, 4)));

            Assert.That(matches[2].Range, Is.EqualTo(Range<int>.Create(0, 4)));
            Assert.That(matches[2].GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[2].GroupCaptures["second"].Range, Is.EqualTo(Range<int>.Create(1, 4)));

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "first",
                    g1 => g1.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                )
                .Group(
                    "second",
                    g2 =>
                        g2.Group(
                            "third",
                            g3 => g3.Annotation(FeatureStruct.New().Symbol(Seg).Value).Optional
                        ).ZeroOrMore
                )
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int>
                {
                    AnchoredToStart = true,
                    AnchoredToEnd = true,
                    AllSubmatches = true
                }
            );

            word = CreateStringData("etested");
            matches = matcher.AllMatches(word).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Success, Is.True);
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 7)));
            Assert.That(matches[0].GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[0].GroupCaptures["second"].Range, Is.EqualTo(Range<int>.Create(1, 7)));
            Assert.That(matches[0].GroupCaptures["third"].Range, Is.EqualTo(Range<int>.Create(6, 7)));

            word = CreateStringData("e");
            matches = matcher.AllMatches(word).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Success, Is.True);
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[0].GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[0].GroupCaptures["second"].Success, Is.False);
            Assert.That(matches[0].GroupCaptures["third"].Success, Is.False);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                .Group("first", g => g.Annotation(FeatureStruct.New().Symbol(Seg).Value).ZeroOrMore)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int>
                {
                    AnchoredToStart = true,
                    AnchoredToEnd = true,
                    AllSubmatches = true
                }
            );

            word = CreateStringData("etested");
            matches = matcher.AllMatches(word).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Success, Is.True);
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 7)));
            Assert.That(matches[0].GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(1, 7)));

            word = CreateStringData("e");
            matches = matcher.AllMatches(word).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Success, Is.True);
            Assert.That(matches[0].Range, Is.EqualTo(Range<int>.Create(0, 1)));
            Assert.That(matches[0].GroupCaptures["first"].Success, Is.False);
        }

        [Test]
        public void DiscontiguousAnnotation()
        {
            var pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "first",
                    first =>
                        first
                            .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl-").Value)
                            .Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                )
                .Group("second", second => second.Annotation(FeatureStruct.New().Symbol(Seg).Value).OneOrMore)
                .Value;

            var matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int> { AnchoredToStart = true, AnchoredToEnd = true }
            );
            AnnotatedStringData word = CreateStringData("ketested");
            Annotation<int> allo1 = new Annotation<int>(word.Range, FeatureStruct.New().Symbol(Allo).Value);
            allo1.Children.AddRange(word.Annotations.GetNodes(0, 2).ToArray());
            allo1.Children.AddRange(word.Annotations.GetNodes(6, 8).ToArray());
            word.Annotations.Add(allo1, false);
            Annotation<int> allo2 = new Annotation<int>(
                Range<int>.Create(2, 6),
                FeatureStruct.New().Symbol(Allo).Value
            );
            allo2.Children.AddRange(word.Annotations.GetNodes(2, 6).ToArray());
            word.Annotations.Add(allo2, false);
            Match<AnnotatedStringData, int> match = matcher.Match(word);
            Assert.That(match.Success, Is.True);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 8)));
            Assert.That(match.GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(0, 2)));
            Assert.That(match.GroupCaptures["second"].Range, Is.EqualTo(Range<int>.Create(2, 8)));
        }

        [Test]
        public void OptionalAnnotation()
        {
            var pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Annotation(FeatureStruct.New().Symbol(Seg).Value)
                .OneOrMore.Value;

            var matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int> { AnchoredToStart = true, AnchoredToEnd = true }
            );
            AnnotatedStringData word = CreateStringData("k");
            word.Annotations.First.Optional = true;
            Match<AnnotatedStringData, int> match = matcher.Match(word);
            Assert.That(match.Success, Is.True);

            pattern = Pattern<AnnotatedStringData, int>
                .New()
                .Group(
                    "first",
                    g1 => g1.Annotation(FeatureStruct.New(PhoneticFeatSys).Symbol(Seg).Symbol("syl+").Value)
                )
                .Group("second", g2 => g2.Annotation(FeatureStruct.New().Symbol(Seg).Value).OneOrMore)
                .Value;

            matcher = new Matcher<AnnotatedStringData, int>(
                pattern,
                new MatcherSettings<int> { AnchoredToStart = true, AnchoredToEnd = true }
            );
            word = CreateStringData("+e+tested+");
            word.Annotations.First.Optional = true;
            word.Annotations.ElementAt(2).Optional = true;
            word.Annotations.Last.Optional = true;
            match = matcher.Match(word);
            Assert.That(match.Success, Is.True);
            Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 10)));
            Assert.That(match.GroupCaptures["first"].Range, Is.EqualTo(Range<int>.Create(1, 3)));
            Assert.That(match.GroupCaptures["second"].Range, Is.EqualTo(Range<int>.Create(3, 10)));
        }
    }
}
