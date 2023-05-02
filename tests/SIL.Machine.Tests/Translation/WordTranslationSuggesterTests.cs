using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
    [TestFixture]
    public class WordTranslationSuggesterTests
    {
        [Test]
        public void GetSuggestion_Punctuation_EndsAtPunctuation()
        {
            TranslationResult result = CreateResult(
                sourceLen: 5,
                prefixLen: 0,
                "this is a test .",
                0.5,
                0.5,
                0.5,
                0.5,
                0.5
            );
            var suggester = new WordTranslationSuggester() { ConfidenceThreshold = 0.2 };
            Assert.That(suggester.GetSuggestion(0, true, result).TargetWordIndices, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void GetSuggestion_UntranslatedWord_EndsAtUntranslatedWord()
        {
            TranslationResult result = CreateResult(
                sourceLen: 5,
                prefixLen: 0,
                "this is a test .",
                0.5,
                0.5,
                0,
                0.5,
                0.5
            );
            var suggester = new WordTranslationSuggester() { ConfidenceThreshold = 0.2 };
            Assert.That(suggester.GetSuggestion(0, true, result).TargetWordIndices, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void GetSuggestion_PrefixCompletedWord_IncludesCompletedWord()
        {
            TranslationResult result = CreateResult(
                sourceLen: 5,
                prefixLen: 1,
                "this is a test .",
                0.5,
                0.5,
                0.5,
                0.5,
                0.5
            );
            var suggester = new WordTranslationSuggester() { ConfidenceThreshold = 0.2 };
            Assert.That(suggester.GetSuggestion(1, false, result).TargetWordIndices, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void GetSuggestion_PrefixPartialWord_NoSuggestions()
        {
            TranslationResult result = CreateResult(
                sourceLen: 5,
                prefixLen: 1,
                "te this is a test .",
                -1,
                0.5,
                0.5,
                0.5,
                0.5,
                0.5
            );
            var suggester = new WordTranslationSuggester() { ConfidenceThreshold = 0.2 };
            Assert.That(suggester.GetSuggestion(1, false, result).TargetWordIndices, Is.Empty);
        }

        [Test]
        public void GetSuggestion_InsertedWord_SkipsInsertedWord()
        {
            TranslationResult result = CreateResult(
                sourceLen: 4,
                prefixLen: 0,
                "this is a test .",
                -1,
                0.5,
                0.5,
                0.5,
                0.5
            );
            var suggester = new WordTranslationSuggester() { ConfidenceThreshold = 0.2 };
            Assert.That(suggester.GetSuggestion(0, true, result).TargetWordIndices, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void GetSuggestion_DeletedWord_IgnoresDeletedWord()
        {
            TranslationResult result = CreateResult(
                sourceLen: 6,
                prefixLen: 0,
                "this is a test .",
                -1,
                0.5,
                0.5,
                0.5,
                0.5,
                0.5
            );
            var suggester = new WordTranslationSuggester() { ConfidenceThreshold = 0.2 };
            Assert.That(suggester.GetSuggestion(0, true, result).TargetWordIndices, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        private static TranslationResult CreateResult(
            int sourceLen,
            int prefixLen,
            string target,
            params double[] confidences
        )
        {
            string[] targetArray = target.Split();
            var targetConfidences = new double[targetArray.Length];
            var targetSources = new TranslationSources[targetArray.Length];
            var alignment = new WordAlignmentMatrix(sourceLen, targetArray.Length);
            int i = 0,
                j = 0;
            foreach (double confidence in confidences)
            {
                if (j < prefixLen)
                    targetSources[j] = TranslationSources.Prefix;

                if (confidence >= 0)
                {
                    alignment[i, j] = true;
                    targetConfidences[j] = confidence;
                    if (confidence > 0)
                        targetSources[j] |= TranslationSources.Smt;
                    i++;
                    j++;
                }
                else if (targetArray.Length > sourceLen)
                {
                    targetConfidences[j] = confidence;
                    j++;
                }
                else if (targetArray.Length < sourceLen)
                {
                    i++;
                }
                else
                {
                    throw new ArgumentException("A confidence was incorrectly set below 0.", nameof(confidences));
                }
            }
            return new TranslationResult(
                target,
                Enumerable.Repeat("word", sourceLen),
                targetArray,
                targetConfidences,
                targetSources,
                alignment,
                new[] { new Phrase(Range<int>.Create(0, sourceLen), targetArray.Length) }
            );
        }
    }
}
