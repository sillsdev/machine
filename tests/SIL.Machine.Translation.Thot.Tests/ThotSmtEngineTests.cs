using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation.Thot.Tests
{
	[TestFixture]
	public class ThotSmtEngineTests
	{
		[Test]
		public void TrainModels_NonEmptyCorpus_GeneratesModels()
		{
			string tempDir = CreateTempDirectory("ThotSmtEngineTests");
			try
			{
				string cfgFileName = Path.Combine(tempDir, "test.cfg");
				File.WriteAllText(cfgFileName, "-tm tm/src_trg\n-lm lm/trg.lm\n");

				var spanFactory = new IntegerSpanFactory();
				var tokenizer = new WhitespaceTokenizer(spanFactory);
				var sourceCorpus = new DictionaryTextCorpus(new[]
					{
						new MemoryText("text1", new[]
							{
								new TextSegment(new TextSegmentRef(1, 1), "¿ le importaría darnos las llaves de la habitación , por favor ?"),
								new TextSegment(new TextSegmentRef(1, 2), "he hecho la reserva de una habitación tranquila doble con teléfono y televisión a nombre de rosario cabedo ."),
								new TextSegment(new TextSegmentRef(1, 3), "¿ le importaría cambiarme a otra habitación más tranquila ?"),
								new TextSegment(new TextSegmentRef(1, 4), "por favor , tengo reservada una habitación ."),
								new TextSegment(new TextSegmentRef(1, 5), "me parece que existe un problema .")
							})
					});

				var targetCorpus = new DictionaryTextCorpus(new[]
					{
						new MemoryText("text1", new[]
							{
								new TextSegment(new TextSegmentRef(1, 1), "would you mind giving us the keys to the room , please ?"),
								new TextSegment(new TextSegmentRef(1, 2), "i have made a reservation for a quiet , double room with a telephone and a tv for rosario cabedo ."),
								new TextSegment(new TextSegmentRef(1, 3), "would you mind moving me to a quieter room ?"),
								new TextSegment(new TextSegmentRef(1, 4), "i have booked a room ."),
								new TextSegment(new TextSegmentRef(1, 5), "i think that there is a problem .")
							})
					});

				ThotSmtEngine.TrainModels(cfgFileName, s => s, tokenizer, sourceCorpus, s => s, tokenizer, targetCorpus);

				Assert.That(File.Exists(Path.Combine(tempDir, "lm", "trg.lm")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg_swm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg_invswm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg.ttable")), Is.True);
				// TODO: test for more than just existence of files
			}
			finally
			{
				DeleteFolderThatMayBeInUse(tempDir);
			}
		}

		[Test]
		public void TrainModels_EmptyCorpus_GeneratesModels()
		{
			string tempDir = CreateTempDirectory("ThotSmtEngineTests");
			try
			{
				string cfgFileName = Path.Combine(tempDir, "test.cfg");
				File.WriteAllText(cfgFileName, "-tm tm/src_trg\n-lm lm/trg.lm\n");

				var spanFactory = new IntegerSpanFactory();
				var tokenizer = new WhitespaceTokenizer(spanFactory);
				var sourceCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
				var targetCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());

				ThotSmtEngine.TrainModels(cfgFileName, s => s, tokenizer, sourceCorpus, s => s, tokenizer, targetCorpus);
				Assert.That(File.Exists(Path.Combine(tempDir, "lm", "trg.lm")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg_swm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg_invswm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg.ttable")), Is.True);
				// TODO: test for more than just existence of files
			}
			finally
			{
				DeleteFolderThatMayBeInUse(tempDir);
			}
		}

		[Test]
		public void Translate_TranslationCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			{
				TranslationResult result = engine.Translate("voy a marcharme hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Translate_NBestLessThanN_TranslationsCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			{
				IEnumerable<TranslationResult> results = engine.Translate(3, "voy a marcharme hoy por la tarde .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment), Is.EqualTo(new[] {"i am leaving today in the afternoon .".Split()}));
			}
		}

		[Test]
		public void Translate_NBest_TranslationsCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			{
				IEnumerable<TranslationResult> results = engine.Translate(2, "hablé hasta cinco en punto .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment), Is.EqualTo(new[] {"hablé until five o ' clock .".Split(), "hablé until five o ' clock for".Split()}));
			}
		}

		[Test]
		public void TrainSegment_TranslationCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
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
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
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
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			{
				TranslationResult result = engine.GetBestPhraseAlignment("esto es una prueba .".Split(), "this is a test .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		private static void SetAligned(WordAlignmentMatrix matrix, int i, int j)
		{
			matrix[i, j] = AlignmentType.Aligned;

			for (int ti = 0; ti < matrix.I; ti++)
			{
				if (matrix[ti, j] == AlignmentType.Unknown)
					matrix[ti, j] = AlignmentType.NotAligned;
			}

			for (int tj = 0; tj < matrix.J; tj++)
			{
				if (matrix[i, tj] == AlignmentType.Unknown)
					matrix[i, tj] = AlignmentType.NotAligned;
			}
		}

		private static void SetSourceNotAligned(WordAlignmentMatrix matrix, int i)
		{
			for (int j = 0; j < matrix.J; j++)
			{
				if (matrix[i, j] == AlignmentType.Unknown)
					matrix[i, j] = AlignmentType.NotAligned;
			}
		}

		private static string CreateTempDirectory(string name)
		{
			string path = Path.Combine(Path.GetTempPath(), name);
			DeleteFolderThatMayBeInUse(path);
			Directory.CreateDirectory(path);
			return path;
		}

		private static void DeleteFolderThatMayBeInUse(string folder)
		{
			if (Directory.Exists(folder))
			{
				try
				{
					Directory.Delete(folder, true);
				}
				catch (Exception)
				{
					try
					{
						//maybe we can at least clear it out a bit
						string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
						foreach (string s in files)
						{
							File.Delete(s);
						}
						//sleep and try again (seems to work)
						Thread.Sleep(1000);
						Directory.Delete(folder, true);
					}
					catch (Exception)
					{
					}
				}
			}
		}
	}
}
