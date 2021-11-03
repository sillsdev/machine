using NUnit.Framework;
using System.Linq;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class DblBundleTextCorpusTests
	{
		[Test]
		public void Texts()
		{
			using (var env = new DblBundleTestEnvironment())
				Assert.That(env.Corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "MAT", "MRK" }));
		}

		[Test]
		public void GetText()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				Assert.That(env.Corpus.GetText("MAT").GetSegments(), Is.Not.Empty);
				Assert.That(env.Corpus.GetText("LUK").GetSegments(), Is.Empty);
			}
		}
	}
}
