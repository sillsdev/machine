using NUnit.Framework;
using SIL.Machine.Translation;

namespace SIL.Machine.Tests.Translation
{
	[TestFixture]
	public class ErrorCorrectingModelTests
	{
		private readonly ErrorCorrectingModel _ecm = new ErrorCorrectingModel();

		[Test]
		public void CorrectPrefix_EmptyUncorrectedPrefix_AppendsPrefix()
		{
			TranslationInfo ti = CreateTranslationInfo(string.Empty);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Target.Count, prefix, true), Is.EqualTo(4));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Target, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(0));
		}

		[Test]
		public void CorrectPrefix_NewEndWord_InsertsWordAtEnd()
		{
			TranslationInfo ti = CreateTranslationInfo("this is a", 1, 2);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Target.Count, prefix, true), Is.EqualTo(1));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Target, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(1));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
		}

		[Test]
		public void CorrectPrefix_SubstringUncorrectedPrefixNewEndWord_InsertsWordAtEnd()
		{
			TranslationInfo ti = CreateTranslationInfo("this is a and only a test", 1, 2, 4, 6);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, 3, prefix, true), Is.EqualTo(0));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(8));
			Assert.That(ti.Target, Is.EqualTo("this is a test and only a test".Split()));
			Assert.That(ti.Phrases.Count, Is.EqualTo(4));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(1));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
			Assert.That(ti.Phrases[2].TargetCut, Is.EqualTo(5));
			Assert.That(ti.Phrases[2].Alignment.ColumnCount, Is.EqualTo(3));
			Assert.That(ti.Phrases[3].TargetCut, Is.EqualTo(7));
			Assert.That(ti.Phrases[3].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_NewMiddleWord_InsertsWord()
		{
			TranslationInfo ti = CreateTranslationInfo("this is a test", 1, 3);

			string[] prefix = "this is , a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Target.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Target, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(1));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(4));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(3));
		}

		[Test]
		public void CorrectPrefix_NewStartWord_InsertsWordAtBeginning()
		{
			TranslationInfo ti = CreateTranslationInfo("this is a test", 1, 3);

			string[] prefix = "yes this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Target.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Target, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(4));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_MissingEndWord_DeletesWordAtEnd()
		{
			TranslationInfo ti = CreateTranslationInfo("this is a test", 1, 3);

			string[] prefix = "this is a".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Target.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Target, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(1));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
		}

		[Test]
		public void CorrectPrefix_SubstringUncorrectedPrefixMissingEndWord_DeletesWordAtEnd()
		{
			TranslationInfo ti = CreateTranslationInfo("this is a test and only a test", 1, 3, 5, 7);

			string[] prefix = "this is a".Split();
			Assert.That(_ecm.CorrectPrefix(ti, 4, prefix, true), Is.EqualTo(0));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(7));
			Assert.That(ti.Target, Is.EqualTo("this is a and only a test".Split()));
			Assert.That(ti.Phrases.Count, Is.EqualTo(4));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(1));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(1));
			Assert.That(ti.Phrases[2].TargetCut, Is.EqualTo(4));
			Assert.That(ti.Phrases[2].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[3].TargetCut, Is.EqualTo(6));
			Assert.That(ti.Phrases[3].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_MissingMiddleWord_DeletesWord()
		{
			TranslationInfo ti = CreateTranslationInfo("this is a test", 1, 3);

			string[] prefix = "this a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Target.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Target, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(0));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(1));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(2));
		}

		[Test]
		public void CorrectPrefix_MissingStartWord_DeletesWordAtBeginning()
		{
			TranslationInfo ti = CreateTranslationInfo("yes this is a test", 2, 4);

			string[] prefix = "this is a test".Split();
			Assert.That(_ecm.CorrectPrefix(ti, ti.Target.Count, prefix, true), Is.EqualTo(0));
			Assert.That(ti.TargetConfidences.Count, Is.EqualTo(prefix.Length));
			Assert.That(ti.Target, Is.EqualTo(prefix));
			Assert.That(ti.Phrases.Count, Is.EqualTo(2));
			Assert.That(ti.Phrases[0].TargetCut, Is.EqualTo(1));
			Assert.That(ti.Phrases[0].Alignment.ColumnCount, Is.EqualTo(2));
			Assert.That(ti.Phrases[1].TargetCut, Is.EqualTo(3));
			Assert.That(ti.Phrases[1].Alignment.ColumnCount, Is.EqualTo(2));
		}

		private static TranslationInfo CreateTranslationInfo(string target, params int[] cuts)
		{
			var ti = new TranslationInfo();
			if (!string.IsNullOrEmpty(target))
			{
				foreach (string word in target.Split())
				{
					ti.Target.Add(word);
					ti.TargetConfidences.Add(1);
				}
			}

			int startIndex = 0;
			foreach (int cut in cuts)
			{
				int len = cut - startIndex + 1;
				var phrase = new PhraseInfo
				{
					SourceStartIndex = startIndex,
					SourceEndIndex = cut,
					TargetCut = cut,
					Alignment = new WordAlignmentMatrix(len, len)
				};
				ti.Phrases.Add(phrase);
				startIndex = cut + 1;
			}
			return ti;
		}
	}
}
