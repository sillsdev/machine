using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotSymmetrizedWordAlignmentModelTests
	{
		private string DirectModelPath => Path.Combine(TestHelpers.ToyCorpusFolderName, "tm", "src_trg_invswm");
		private string InverseModelPath => Path.Combine(TestHelpers.ToyCorpusFolderName, "tm", "src_trg_swm");

		[Test]
		public void GetTranslationTable_NoThreshold()
		{
			using (var model = new ThotSymmetrizedWordAlignmentModel(DirectModelPath, InverseModelPath))
			{
				IDictionary<string, IDictionary<string, double>> table = model.GetTranslationTable();
				Assert.That(table.Count, Is.EqualTo(513));
				Assert.That(table["es"].Count, Is.EqualTo(363));
			}
		}

		[Test]
		public void GetTranslationTable_Threshold()
		{
			using (var model = new ThotSymmetrizedWordAlignmentModel(DirectModelPath, InverseModelPath))
			{
				IDictionary<string, IDictionary<string, double>> table = model.GetTranslationTable(0.2);
				Assert.That(table.Count, Is.EqualTo(513));
				Assert.That(table["es"].Count, Is.EqualTo(9));
			}
		}
	}
}
