using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.Tests.Translation
{
	[TestFixture]
	public class TranslationExtensionsTests
	{
		[Test]
		public void GetSuggestedWordIndices_Punctuation_EndsAtPunctuation()
		{
			IInteractiveTranslationSession session = Substitute.For<IInteractiveTranslationSession>();
			session.Prefix.Returns(new ReadOnlyList<string>(new string[0]));
			session.IsLastWordComplete.Returns(true);
			session.CurrentResult.Returns(CreateResult(5, 0, "this is a test .", 0.5, 0.5, 0.5, 0.5, 0.5));

			Assert.That(session.GetSuggestedWordIndices(0.2), Is.EqualTo(new[] {0, 1, 2, 3}));
		}

		[Test]
		public void GetSuggestedWordIndices_UntranslatedWord_EndsAtUntranslatedWord()
		{
			IInteractiveTranslationSession session = Substitute.For<IInteractiveTranslationSession>();
			session.Prefix.Returns(new ReadOnlyList<string>(new string[0]));
			session.IsLastWordComplete.Returns(true);
			session.CurrentResult.Returns(CreateResult(5, 0, "this is a test .", 0.5, 0.5, 0, 0.5, 0.5));

			Assert.That(session.GetSuggestedWordIndices(0.2), Is.EqualTo(new[] {0, 1}));
		}

		[Test]
		public void GetSuggestedWordIndices_PrefixCompletedWord_IncludesCompletedWord()
		{
			IInteractiveTranslationSession session = Substitute.For<IInteractiveTranslationSession>();
			session.Prefix.Returns(new ReadOnlyList<string>("th".Split()));
			session.IsLastWordComplete.Returns(false);
			session.CurrentResult.Returns(CreateResult(5, 1, "this is a test .", 0.5, 0.5, 0.5, 0.5, 0.5));

			Assert.That(session.GetSuggestedWordIndices(0.2), Is.EqualTo(new[] {0, 1, 2, 3}));
		}

		[Test]
		public void GetSuggestedWordIndices_PrefixPartialWord_NoSuggestions()
		{
			IInteractiveTranslationSession session = Substitute.For<IInteractiveTranslationSession>();
			session.Prefix.Returns(new ReadOnlyList<string>("te".Split()));
			session.IsLastWordComplete.Returns(false);
			session.CurrentResult.Returns(CreateResult(5, 1, "te this is a test .", -1, 0.5, 0.5, 0.5, 0.5, 0.5));

			Assert.That(session.GetSuggestedWordIndices(0.2), Is.Empty);
		}

		[Test]
		public void GetSuggestedWordIndices_InsertedWord_SkipsInsertedWord()
		{
			IInteractiveTranslationSession session = Substitute.For<IInteractiveTranslationSession>();
			session.Prefix.Returns(new ReadOnlyList<string>(new string[0]));
			session.IsLastWordComplete.Returns(true);
			session.CurrentResult.Returns(CreateResult(4, 0, "this is a test .", -1, 0.5, 0.5, 0.5, 0.5));

			Assert.That(session.GetSuggestedWordIndices(0.2), Is.EqualTo(new[] {1, 2, 3}));
		}

		[Test]
		public void GetSuggestedWordIndices_DeletedWord_IgnoresDeletedWord()
		{
			IInteractiveTranslationSession session = Substitute.For<IInteractiveTranslationSession>();
			session.Prefix.Returns(new ReadOnlyList<string>(new string[0]));
			session.IsLastWordComplete.Returns(true);
			session.CurrentResult.Returns(CreateResult(6, 0, "this is a test .", -1, 0.5, 0.5, 0.5, 0.5, 0.5));

			Assert.That(session.GetSuggestedWordIndices(0.2), Is.EqualTo(new[] {0, 1, 2, 3}));
		}

		private static TranslationResult CreateResult(int sourceLen, int prefixLen, string target, params double[] confidences)
		{
			string[] targetArray = target.Split();
			var targetConfidences = new double[targetArray.Length];
			var targetSources = new TranslationSources[targetArray.Length];
			var alignment = new WordAlignmentMatrix(sourceLen, targetArray.Length);
			int i = 0, j = 0;
			foreach (double confidence in confidences)
			{
				if (j < prefixLen)
					targetSources[j] = TranslationSources.Prefix;

				if (confidence >= 0)
				{
					alignment[i, j] = AlignmentType.Aligned;
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
			return new TranslationResult(Enumerable.Range(0, sourceLen).Select(index => index.ToString()), targetArray, targetConfidences, targetSources, alignment);
		}
	}
}
