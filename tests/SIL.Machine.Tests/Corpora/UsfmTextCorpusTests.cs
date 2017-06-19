using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class UsfmTextCorpusTests
	{
		[Test]
		public void Texts()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmTextCorpus(CorporaTestHelpers.UsfmStylesheetPath, Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath, tokenizer);

			Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] {"41MAT", "42MRK"}));
		}

		[Test]
		public void TryGetText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmTextCorpus(CorporaTestHelpers.UsfmStylesheetPath, Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath, tokenizer);

			IText text;
			Assert.That(corpus.TryGetText("41MAT", out text), Is.True);
			Assert.That(text.Id, Is.EqualTo("41MAT"));
			Assert.That(corpus.TryGetText("43LUK", out text), Is.False);
		}
	}
}
