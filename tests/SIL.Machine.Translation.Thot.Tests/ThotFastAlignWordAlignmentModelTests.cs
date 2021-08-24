using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotFastAlignWordAlignmentModelTests
	{
		private string DirectModelPath => Path.Combine(TestHelpers.ToyCorpusFastAlignFolderName, "tm",
			"src_trg_invswm");
		private string InverseModelPath => Path.Combine(TestHelpers.ToyCorpusFastAlignFolderName, "tm",
			"src_trg_swm");

		[Test]
		public void GetBestAlignment()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split(' ');
				string[] targetSegment = "could we see another room , please ?".Split(' ');
				WordAlignmentMatrix waMatrix = swAlignModel.GetBestAlignment(sourceSegment, targetSegment);
				Assert.That(waMatrix.ToGizaFormat(sourceSegment, targetSegment), Is.EqualTo("could we see another room , please ?\n"
					+ "NULL ({ 6 }) por ({ 1 }) favor ({ }) , ({ }) ¿ ({ }) podríamos ({ 2 }) ver ({ 3 }) otra ({ 4 }) habitación ({ 5 }) ? ({ 7 8 })\n"));
			}
		}

		[Test]
		public void GetTranslationProbability()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.GetTranslationProbability("esto", "this"), Is.EqualTo(0.0).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("es", "is"), Is.EqualTo(0.90).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("una", "a"), Is.EqualTo(0.83).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("prueba", "test"), Is.EqualTo(0.0).Within(0.01));
			}
		}

		[Test]
		public void SourceWords_Enumerate()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.SourceWords.Count(), Is.EqualTo(500));
			}
		}

		[Test]
		public void SourceWords_IndexAccessor()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.SourceWords[0], Is.EqualTo("NULL"));
				Assert.That(swAlignModel.SourceWords[499], Is.EqualTo("pagar"));
			}
		}

		[Test]
		public void SourceWords_Count()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.SourceWords.Count, Is.EqualTo(500));
			}
		}

		[Test]
		public void TargetWords_Enumerate()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.TargetWords.Count(), Is.EqualTo(352));
			}
		}

		[Test]
		public void TargetWords_IndexAccessor()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.TargetWords[0], Is.EqualTo("NULL"));
				Assert.That(swAlignModel.TargetWords[351], Is.EqualTo("pay"));
			}
		}

		[Test]
		public void TargetWords_Count()
		{
			using (var swAlignModel = new ThotFastAlignWordAlignmentModel(DirectModelPath))
			{
				Assert.That(swAlignModel.TargetWords.Count, Is.EqualTo(352));
			}
		}

		[Test]
		public void GetTranslationTable_SymmetrizedNoThreshold()
		{
			using (var model = new SymmetrizedWordAlignmentModel(new ThotFastAlignWordAlignmentModel(DirectModelPath),
				new ThotFastAlignWordAlignmentModel(InverseModelPath)))
			{
				Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable();
				Assert.That(table.Count, Is.EqualTo(500));
				Assert.That(table["es"].Count, Is.EqualTo(21));
			}
		}

		[Test]
		public void GetTranslationTable_SymmetrizedThreshold()
		{
			using (var model = new SymmetrizedWordAlignmentModel(new ThotFastAlignWordAlignmentModel(DirectModelPath),
				new ThotFastAlignWordAlignmentModel(InverseModelPath)))
			{
				Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable(0.2);
				Assert.That(table.Count, Is.EqualTo(500));
				Assert.That(table["es"].Count, Is.EqualTo(2));
			}
		}
	}
}
