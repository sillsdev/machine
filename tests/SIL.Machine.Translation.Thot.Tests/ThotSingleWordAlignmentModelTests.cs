using System.IO;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot.Tests
{
	[TestFixture]
	public class ThotSingleWordAlignmentModelTests
	{
		[Test]
		public void GetBestAlignment_ReturnsCorrectAlignment()
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(Path.Combine(TestHelpers.ToyCorpusFolderName, "tm", "src_trg_invswm")))
			{
				string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split(' ');
				string[] targetSegment = "could we see another room , please ?".Split(' ');
				WordAlignmentMatrix waMatrix = swAlignModel.GetBestAlignment(sourceSegment, targetSegment);
				Assert.That(waMatrix.ToGizaFormat(sourceSegment, targetSegment), Is.EqualTo("could we see another room , please ?\n"
					+ "NULL ({ }) por ({ 6 }) favor ({ 7 }) , ({ }) ¿ ({ 8 }) podríamos ({ 1 2 }) ver ({ 3 }) otra ({ 4 }) habitación ({ 5 }) ? ({ })\n"));
			}
		}

		[Test]
		public void GetTranslationProbability_ReturnsCorrectProbability()
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(Path.Combine(TestHelpers.ToyCorpusFolderName, "tm", "src_trg_invswm")))
			{
				Assert.That(swAlignModel.GetTranslationProbability("esto", "this"), Is.EqualTo(0.0).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("es", "is"), Is.EqualTo(0.65).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("una", "a"), Is.EqualTo(0.70).Within(0.01));
				Assert.That(swAlignModel.GetTranslationProbability("prueba", "test"), Is.EqualTo(0.0).Within(0.01));
			}
		}
	}
}
