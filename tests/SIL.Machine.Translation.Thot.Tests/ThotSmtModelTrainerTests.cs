using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotSmtModelTrainerTests
	{
		[Test]
		public void Train_NonEmptyCorpus_GeneratesModels()
		{
			using (var tempDir = new TempDirectory("ThotSmtModelTrainerTests"))
			{
				var sourceCorpus = new DictionaryTextCorpus(new[]
				{
					new MemoryText("text1", new[]
					{
						Segment(1, "¿ le importaría darnos las llaves de la habitación , por favor ?"),
						Segment(2,
							"he hecho la reserva de una habitación tranquila doble con ||| teléfono ||| y televisión a nombre de rosario cabedo ."),
						Segment(3, "¿ le importaría cambiarme a otra habitación más tranquila ?"),
						Segment(4, "por favor , tengo reservada una habitación ."),
						Segment(5, "me parece que existe un problema .")
					})
				});

				var targetCorpus = new DictionaryTextCorpus(new[]
				{
					new MemoryText("text1", new[]
					{
						Segment(1, "would you mind giving us the keys to the room , please ?"),
						Segment(2,
							"i have made a reservation for a quiet , double room with a ||| telephone ||| and a tv for rosario cabedo ."),
						Segment(3, "would you mind moving me to a quieter room ?"),
						Segment(4, "i have booked a room ."),
						Segment(5, "i think that there is a problem .")
					})
				});

				var alignmentCorpus = new DictionaryAlignmentCorpus(new[]
				{
					new MemoryAlignmentCollection("text1", new[]
					{
						Alignment(1, new AlignedWordPair(8, 9)),
						Alignment(2, new AlignedWordPair(6, 10)),
						Alignment(3, new AlignedWordPair(6, 8)),
						Alignment(4, new AlignedWordPair(6, 4)),
						Alignment(5)
					})
				});

				var corpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);

				var parameters = new ThotSmtParameters
				{
					TranslationModelFileNamePrefix = Path.Combine(tempDir.Path, "tm", "src_trg"),
					LanguageModelFileNamePrefix = Path.Combine(tempDir.Path, "lm", "trg.lm")
				};

				using (var trainer = new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, parameters))
				{
					trainer.Train();
					trainer.Save();
				}

				Assert.That(File.Exists(Path.Combine(tempDir.Path, "lm", "trg.lm")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg_swm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg_invswm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg.ttable")), Is.True);
				// TODO: test for more than just existence of files
			}
		}

		[Test]
		public void TrainModels_EmptyCorpus_GeneratesModels()
		{
			using (var tempDir = new TempDirectory("ThotSmtModelTrainerTests"))
			{
				var sourceCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
				var targetCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
				var alignmentCorpus = new DictionaryAlignmentCorpus(
					Enumerable.Empty<MemoryAlignmentCollection>());

				var corpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);

				var parameters = new ThotSmtParameters
				{
					TranslationModelFileNamePrefix = Path.Combine(tempDir.Path, "tm", "src_trg"),
					LanguageModelFileNamePrefix = Path.Combine(tempDir.Path, "lm", "trg.lm")
				};

				using (var trainer = new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, parameters))
				{
					trainer.Train();
					trainer.Save();
				}

				Assert.That(File.Exists(Path.Combine(tempDir.Path, "lm", "trg.lm")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg_swm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg_invswm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg.ttable")), Is.True);
				// TODO: test for more than just existence of files
			}
		}

		private static TextRow Segment(int key, string text)
		{
			return new TextRow(new RowRef(key))
			{
				Segment = text.Split(),
				IsEmpty = text.Length == 0
			};
		}

		private static AlignmentRow Alignment(int key, params AlignedWordPair[] pairs)
		{
			return new AlignmentRow(new RowRef(key))
			{
				AlignedWordPairs = pairs
			};
		}
	}
}
