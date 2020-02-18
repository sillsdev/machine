using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class UsfmFileTextCorpusTests
	{
		[Test]
		public void Texts()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "MAT", "MRK" }));
		}

		[Test]
		public void TryGetText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text;
			Assert.That(corpus.TryGetText("MAT", out text), Is.True);
			Assert.That(text.Id, Is.EqualTo("MAT"));
			Assert.That(corpus.TryGetText("LUK", out _), Is.False);
		}
	}
}
