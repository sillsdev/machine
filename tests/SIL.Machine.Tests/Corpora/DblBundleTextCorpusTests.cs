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
				IText text;
				Assert.That(env.Corpus.TryGetText("MAT", out text), Is.True);
				Assert.That(text.Id, Is.EqualTo("MAT"));
				Assert.That(env.Corpus.TryGetText("LUK", out text), Is.False);
			}
		}
	}
}
