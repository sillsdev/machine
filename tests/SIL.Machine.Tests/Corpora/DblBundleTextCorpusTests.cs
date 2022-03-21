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
		public void TryGetText()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				Assert.That(env.Corpus.TryGetText("MAT", out IText mat), Is.True);
				Assert.That(mat.GetRows(), Is.Not.Empty);
				Assert.That(env.Corpus.TryGetText("LUK", out _), Is.False);
			}
		}
	}
}
