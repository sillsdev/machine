using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
	[TestFixture]
	public class ErrorCorrectionModelTests
	{
		private readonly ErrorCorrectionModel _ecm = new ErrorCorrectionModel();

		[Test]
		public void CorrectPrefix_EmptyUncorrectedPrefix_AppendsPrefix()
		{
			TranslationResultBuilder builder = CreateResultBuilder(string.Empty);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(builder, builder.Words.Count, prefix, true), Is.EqualTo(4));
			Assert.That(builder.Confidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(builder.Words, Is.EqualTo(prefix));
			Assert.That(builder.Phrases.Count, Is.EqualTo(0));
		}

		[Test]
		public void CorrectPrefix_NewEndWord_InsertsWordAtEnd()
		{
			TranslationResultBuilder ti = CreateResultBuilder("this is a", 2, 3);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Words.Count, prefix, true), Is.EqualTo(1));
			Assert.That(ti.Confidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Words, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
		}

		[Test]
		public void CorrectPrefix_SubstringUncorrectedPrefixNewEndWord_InsertsWordAtEnd()
		{
			TranslationResultBuilder ti = CreateResultBuilder("this is a and only a test", 2, 3, 5, 7);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, 3, prefix, true), Is.EqualTo(0));
			Assert.That(ti.Confidences.Count, Is.EqualTo(8));
			Assert.That(ti.Words, Is.EqualTo("this is a test and only a test".Split()));
			Assert.That(ti.Phrases.Count, Is.EqualTo(4));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
			Assert.That(ti.Phrases[2].TargetCut, Is.EqualTo(6));
			Assert.That(ti.Phrases[2].Alignment.ColumnCount, Is.EqualTo(3));
			Assert.That(ti.Phrases[3].TargetCut, Is.EqualTo(8));
			Assert.That(ti.Phrases[3].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_NewMiddleWord_InsertsWord()
		{
			TranslationResultBuilder ti = CreateResultBuilder("this is a test", 2, 4);

			string[] prefix = "this is , a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Words.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.Confidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Words, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(5));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(3));
		}

		[Test]
		public void CorrectPrefix_NewStartWord_InsertsWordAtBeginning()
		{
			TranslationResultBuilder ti = CreateResultBuilder("this is a test", 2, 4);

			string[] prefix = "yes this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Words.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.Confidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Words, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(3));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(5));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_MissingEndWord_DeletesWordAtEnd()
		{
			TranslationResultBuilder ti = CreateResultBuilder("this is a test", 2, 4);

			string[] prefix = "this is a".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Words.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.Confidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Words, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
		}

		[Test]
		public void CorrectPrefix_SubstringUncorrectedPrefixMissingEndWord_DeletesWordAtEnd()
		{
			TranslationResultBuilder ti = CreateResultBuilder("this is a test and only a test", 2, 4, 6, 8);

			string[] prefix = "this is a".Split();
			Assert.That(_ecm.CorrectPrefix(ti, 4, prefix, true), Is.EqualTo(0));
			Assert.That(ti.Confidences.Count, Is.EqualTo(7));
			Assert.That(ti.Words, Is.EqualTo("this is a and only a test".Split()));
			Assert.That(ti.Phrases.Count, Is.EqualTo(4));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
			Assert.That(ti.Phrases[2].TargetCut, Is.EqualTo(5));
			Assert.That(ti.Phrases[2].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[3].TargetCut, Is.EqualTo(7));
			Assert.That(ti.Phrases[3].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_MissingMiddleWord_DeletesWord()
		{
			TranslationResultBuilder ti = CreateResultBuilder("this is a test", 2, 4);

			string[] prefix = "this a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Words.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.Confidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Words, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(1));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(1));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_MissingStartWord_DeletesWordAtBeginning()
		{
			TranslationResultBuilder ti = CreateResultBuilder("yes this is a test", 3, 5);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Words.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.Confidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Words, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(4));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(2));
		}

		private static TranslationResultBuilder CreateResultBuilder(string target, params int[] cuts)
		{
			var builder = new TranslationResultBuilder();
			if (!string.IsNullOrEmpty(target))
			{
				int i = 0;
				int k = 0;
				string[] words = target.Split();
				for (int j = 0; j < words.Length; j++)
				{
					builder.AppendWord(words[j], TranslationSources.Smt, 1);
					int cut = j + 1;
					if (k < cuts.Length && cuts[k] == cut)
					{
						int len = cut - i;
						builder.MarkPhrase(Range<int>.Create(i, cut), new WordAlignmentMatrix(len, len));
						k++;
						i = cut;
					}
				}
			}
			return builder;
		}
	}
}
