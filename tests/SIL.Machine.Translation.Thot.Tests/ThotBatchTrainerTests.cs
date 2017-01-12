using System;
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
	public class ThotBatchTrainerTests
	{
		[Test]
		public void Train_NonEmptyCorpus_GeneratesModels()
		{
			string tempDir = CreateTempDirectory("ThotSmtEngineTests");
			try
			{
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

				var trainer = new ThotBatchTrainer(Path.Combine(tempDir, "tm", "src_trg"), Path.Combine(tempDir, "lm", "trg.lm"), new ThotSmtParameters(),
					s => s, tokenizer, sourceCorpus, s => s, tokenizer, targetCorpus);
				trainer.Train();

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
				var spanFactory = new IntegerSpanFactory();
				var tokenizer = new WhitespaceTokenizer(spanFactory);
				var sourceCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
				var targetCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());

				var trainer = new ThotBatchTrainer(Path.Combine(tempDir, "tm", "src_trg"), Path.Combine(tempDir, "lm", "trg.lm"), new ThotSmtParameters(),
					s => s, tokenizer, sourceCorpus, s => s, tokenizer, targetCorpus);
				trainer.Train();

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
