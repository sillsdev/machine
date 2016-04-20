using NUnit.Framework;

namespace SIL.Machine.Translation.Tests
{
	[TestFixture]
	public class ThotSmtEngineTests
	{
		[Test]
		public void GetBestAlignment_ReturnsCorrectAlignment()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			{
				int[] alignment = engine.GetBestAlignment("esto es una prueba .".Split(), "this is a test .".Split());
				Assert.That(alignment, Is.EqualTo(new[] {0, 1, 2, 3, 4}));
			}
		}

		[Test]
		public void GetTranslationProbability_ReturnsCorrectProbability()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			{
				Assert.That(engine.GetTranslationProbability("esto", "this"), Is.EqualTo(0.0).Within(0.01));
				Assert.That(engine.GetTranslationProbability("es", "is"), Is.EqualTo(0.65).Within(0.01));
				Assert.That(engine.GetTranslationProbability("una", "a"), Is.EqualTo(0.70).Within(0.01));
				Assert.That(engine.GetTranslationProbability("prueba", "test"), Is.EqualTo(0.0).Within(0.01));
			}
		}
	}
}
