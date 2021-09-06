using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotHmmWordAlignmentModelTests
	{
		private string DirectModelPath => Path.Combine(TestHelpers.ToyCorpusHmmFolderName, "tm", "src_trg_invswm");
		private string InverseModelPath => Path.Combine(TestHelpers.ToyCorpusHmmFolderName, "tm", "src_trg_swm");

		[Test]
		public void GetBestAlignment()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split(' ');
				string[] targetSegment = "could we see another room , please ?".Split(' ');
				WordAlignmentMatrix waMatrix = swAlignModel.GetBestAlignment(sourceSegment, targetSegment);
				var expected = new WordAlignmentMatrix(9, 8,
					new HashSet<(int, int)> { (0, 5), (1, 6), (3, 7), (4, 0), (4, 1), (5, 2), (6, 3), (7, 4) });
				Assert.That(waMatrix.ValueEquals(expected), Is.True);
			}
		}

		[Test]
		public void GetTranslationProbability()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.GetTranslationProbability("esto", "this"), Is.EqualTo(0.0).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("es", "is"), Is.EqualTo(0.65).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("una", "a"), Is.EqualTo(0.70).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("prueba", "test"), Is.EqualTo(0.0).Within(0.01));
			}
		}

		[Test]
		public void SourceWords_Enumerate()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.SourceWords.Count(), Is.EqualTo(513));
			}
		}

		[Test]
		public void SourceWords_IndexAccessor()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.SourceWords[0], Is.EqualTo("NULL"));
				Assert.That(swAlignModel.SourceWords[512], Is.EqualTo("pagar"));
			}
		}

		[Test]
		public void SourceWords_Count()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.SourceWords.Count, Is.EqualTo(513));
			}
		}

		[Test]
		public void TargetWords_Enumerate()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.TargetWords.Count(), Is.EqualTo(363));
			}
		}

		[Test]
		public void TargetWords_IndexAccessor()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.TargetWords[0], Is.EqualTo("NULL"));
				Assert.That(swAlignModel.TargetWords[362], Is.EqualTo("pay"));
			}
		}

		[Test]
		public void TargetWords_Count()
		{
			using (var swAlignModel = new ThotHmmWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.TargetWords.Count, Is.EqualTo(363));
			}
		}

		[Test]
		public void GetTranslationTable_SymmetrizedNoThreshold()
		{
			using (var model = new SymmetrizedWordAlignmentModel(new ThotHmmWordAlignmentModel(DirectModelPath),
				new ThotHmmWordAlignmentModel(InverseModelPath)))
			{
				Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable();
				Assert.That(table.Count, Is.EqualTo(513));
				Assert.That(table["es"].Count, Is.EqualTo(23));
			}
		}

		[Test]
		public void GetTranslationTable_SymmetrizedThreshold()
		{
			using (var model = new SymmetrizedWordAlignmentModel(new ThotHmmWordAlignmentModel(DirectModelPath),
				new ThotHmmWordAlignmentModel(InverseModelPath)))
			{
				Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable(0.2);
				Assert.That(table.Count, Is.EqualTo(513));
				Assert.That(table["es"].Count, Is.EqualTo(9));
			}
		}

		[Test]
		public void CreateTrainer()
		{
			using (var model = new ThotHmmWordAlignmentModel
			{
				Parameters = new ThotWordAlignmentModelParameters
				{
					Ibm1IterationCount = 2,
					HmmIterationCount = 2,
					HmmP0 = 0.1
				}
			})
			{

				ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
				trainer.Train();
				trainer.Save();

				WordAlignmentMatrix matrix = model.GetBestAlignment("isthay isyay ayay esttay-N .".Split(),
					"this is a test N .".Split());
				var expected = new WordAlignmentMatrix(5, 6,
					new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (3, 3), (3, 4), (4, 5) });
				Assert.That(matrix.ValueEquals(expected), Is.True);

				matrix = model.GetBestAlignment("isthay isyay otnay ayay esttay-N .".Split(),
					"this is not a test N .".Split());
				expected = new WordAlignmentMatrix(6, 7,
					new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (3, 3), (4, 4), (4, 5), (5, 6) });
				Assert.That(matrix.ValueEquals(expected), Is.True);

				matrix = model.GetBestAlignment("isthay isyay ayay esttay-N ardhay .".Split(),
					"this is a hard test N .".Split());
				expected = new WordAlignmentMatrix(6, 7,
					new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (4, 3), (3, 4), (3, 5), (3, 6) });
				Assert.That(matrix.ValueEquals(expected), Is.True);
			}
		}
	}
}
