using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot.Tests
{
	[TestFixture]
	public class ThotSmtEngineTests
	{
		[Test]
		public void Translate_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.Translate("voy a marcharme hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Translate_NBestLessThanN_TranslationsCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				IEnumerable<TranslationResult> results = engine.Translate(3, "voy a marcharme hoy por la tarde .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment), Is.EqualTo(new[] {"i am leaving today in the afternoon .".Split()}));
			}
		}

		[Test]
		public void Translate_NBest_TranslationsCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				IEnumerable<TranslationResult> results = engine.Translate(2, "hablé hasta cinco en punto .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment), Is.EqualTo(new[] {"hablé until five o ' clock .".Split(), "hablé until five o ' clock for".Split()}));
			}
		}

		[Test]
		public void TrainSegment_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.Translate("esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("esto is a prueba .".Split()));
				engine.TrainSegment("esto es una prueba .".Split(), "this is a test .".Split());
				result = engine.Translate("esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public void TrainSegment_AlignmentSpecified_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.Translate("maria no dio una bofetada a la bruja verde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("maria no dio a bofetada to bruja verde .".Split()));

				var matrix = new WordAlignmentMatrix(10, 7, AlignmentType.Unknown);
				SetAligned(matrix, 1, 1);
				SetAligned(matrix, 2, 2);
				SetAligned(matrix, 3, 2);
				SetAligned(matrix, 4, 2);
				SetSourceNotAligned(matrix, 5);
				SetAligned(matrix, 8, 4);
				engine.TrainSegment("maria no dio una bofetada a la bruja verde .".Split(), "mary didn't slap the green witch .".Split(), matrix);
				result = engine.Translate("maria es una bruja .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("mary is a witch .".Split()));
			}
		}

		[Test]
		public void GetBestPhraseAlignment_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.GetBestPhraseAlignment("esto es una prueba .".Split(), "this is a test .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		private static void SetAligned(WordAlignmentMatrix matrix, int i, int j)
		{
			matrix[i, j] = AlignmentType.Aligned;

			for (int ti = 0; ti < matrix.RowCount; ti++)
			{
				if (matrix[ti, j] == AlignmentType.Unknown)
					matrix[ti, j] = AlignmentType.NotAligned;
			}

			for (int tj = 0; tj < matrix.ColumnCount; tj++)
			{
				if (matrix[i, tj] == AlignmentType.Unknown)
					matrix[i, tj] = AlignmentType.NotAligned;
			}
		}

		private static void SetSourceNotAligned(WordAlignmentMatrix matrix, int i)
		{
			for (int j = 0; j < matrix.ColumnCount; j++)
			{
				if (matrix[i, j] == AlignmentType.Unknown)
					matrix[i, j] = AlignmentType.NotAligned;
			}
		}
	}
}
