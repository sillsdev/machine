using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.IO;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot.Tests
{
	[TestFixture]
	public class ThotBatchTrainerTests
	{
		[Test]
		public void Train_NonEmptyCorpus_GeneratesModels()
		{
			using (var tempDir = new TempDirectory("ThotSmtEngineTests"))
			{
				var sourceCorpus = new DictionaryTextCorpus(new[]
					{
						new MemoryText("text1", new[]
							{
								new TextSegment(new TextSegmentRef(1, 1), "¿ le importaría darnos las llaves de la habitación , por favor ?".Split()),
								new TextSegment(new TextSegmentRef(1, 2), "he hecho la reserva de una habitación tranquila doble con teléfono y televisión a nombre de rosario cabedo .".Split()),
								new TextSegment(new TextSegmentRef(1, 3), "¿ le importaría cambiarme a otra habitación más tranquila ?".Split()),
								new TextSegment(new TextSegmentRef(1, 4), "por favor , tengo reservada una habitación .".Split()),
								new TextSegment(new TextSegmentRef(1, 5), "me parece que existe un problema .".Split())
							})
					});

				var targetCorpus = new DictionaryTextCorpus(new[]
					{
						new MemoryText("text1", new[]
							{
								new TextSegment(new TextSegmentRef(1, 1), "would you mind giving us the keys to the room , please ?".Split()),
								new TextSegment(new TextSegmentRef(1, 2), "i have made a reservation for a quiet , double room with a telephone and a tv for rosario cabedo .".Split()),
								new TextSegment(new TextSegmentRef(1, 3), "would you mind moving me to a quieter room ?".Split()),
								new TextSegment(new TextSegmentRef(1, 4), "i have booked a room .".Split()),
								new TextSegment(new TextSegmentRef(1, 5), "i think that there is a problem .".Split())
							})
					});

				var alignmentCorpus = new DictionaryTextAlignmentCorpus(new[]
					{
						new MemoryTextAlignmentCollection("text1", new[]
							{
								new TextAlignment(new TextSegmentRef(1, 1), new WordAlignmentMatrix(13, 13, AlignmentType.Unknown) {[8, 9] = AlignmentType.Aligned}),
								new TextAlignment(new TextSegmentRef(1, 2), new WordAlignmentMatrix(19, 21, AlignmentType.Unknown) {[6, 10] = AlignmentType.Aligned}),
								new TextAlignment(new TextSegmentRef(1, 3), new WordAlignmentMatrix(10, 10, AlignmentType.Unknown) {[6, 8] = AlignmentType.Aligned}),
								new TextAlignment(new TextSegmentRef(1, 4), new WordAlignmentMatrix(8, 6, AlignmentType.Unknown) {[6, 4] = AlignmentType.Aligned}),
								new TextAlignment(new TextSegmentRef(1, 5), new WordAlignmentMatrix(7, 8, AlignmentType.Unknown))     
							})
					});

				var trainer = new ThotBatchTrainer(Path.Combine(tempDir.Path, "tm", "src_trg"), Path.Combine(tempDir.Path, "lm", "trg.lm"), new ThotSmtParameters(),
					s => s, sourceCorpus, s => s, targetCorpus, alignmentCorpus);
				trainer.Train();

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
			using (var tempDir = new TempDirectory("ThotSmtEngineTests"))
			{
				var sourceCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
				var targetCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
				var alignmentCorpus = new DictionaryTextAlignmentCorpus(Enumerable.Empty<MemoryTextAlignmentCollection>());

				var trainer = new ThotBatchTrainer(Path.Combine(tempDir.Path, "tm", "src_trg"), Path.Combine(tempDir.Path, "lm", "trg.lm"), new ThotSmtParameters(),
					s => s, sourceCorpus, s => s, targetCorpus, alignmentCorpus);
				trainer.Train();

				Assert.That(File.Exists(Path.Combine(tempDir.Path, "lm", "trg.lm")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg_swm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg_invswm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir.Path, "tm", "src_trg.ttable")), Is.True);
				// TODO: test for more than just existence of files
			}
		}
	}
}
