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
				Assert.That(waMatrix.ToGizaFormat(sourceSegment, targetSegment), Is.EqualTo("could we see another room , please ?\n"
					+ "NULL ({ }) por ({ 6 }) favor ({ 7 }) , ({ }) ¿ ({ 8 }) podríamos ({ 1 2 }) ver ({ 3 }) otra ({ 4 }) habitación ({ 5 }) ? ({ })\n"));
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
	}
}
