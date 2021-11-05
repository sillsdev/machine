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
			var corpus = new UsfmFileTextCorpus(tokenizer, "usfm.sty", Encoding.UTF8,
				CorporaTestHelpers.UsfmTestProjectPath);

			Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "MAT", "MRK" }));
		}

		[Test]
		public void GetText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, "usfm.sty", Encoding.UTF8,
				CorporaTestHelpers.UsfmTestProjectPath);

			Assert.That(corpus.GetText("MAT").GetSegments(), Is.Not.Empty);
			Assert.That(corpus.GetText("LUK").GetSegments(), Is.Empty);
		}
	}
}
