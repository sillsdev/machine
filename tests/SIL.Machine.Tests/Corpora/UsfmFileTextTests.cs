using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class UsfmFileTextTests
	{
		[Test]
		public void Segments_NonEmptyText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus.GetText("MAT");
			TextSegment[] segments = text.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(10));

			Assert.That(segments[0].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));
			Assert.That(segments[0].Segment, Is.EqualTo("Chapter one , verse one .".Split()));

			Assert.That(segments[1].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:2", corpus.Versification)));
			Assert.That(segments[1].Segment, Is.EqualTo("Chapter one , verse two .".Split()));

			Assert.That(segments[5].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:1", corpus.Versification)));
			Assert.That(segments[5].Segment, Is.EqualTo("Chapter two , verse one .".Split()));

			Assert.That(segments[9].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:5", corpus.Versification)));
			Assert.That(segments[9].Segment, Is.EqualTo("Chapter two , verse five .".Split()));
		}

		[Test]
		public void Segments_SentenceStart()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus.GetText("MAT");
			TextSegment[] segments = text.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(10));

			Assert.That(segments[3].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:4", corpus.Versification)));
			Assert.That(segments[3].Segment, Is.EqualTo("Chapter one , verse four ,".Split()));
			Assert.That(segments[3].SentenceStart, Is.True);

			Assert.That(segments[4].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:5", corpus.Versification)));
			Assert.That(segments[4].Segment, Is.EqualTo("chapter one , verse five .".Split()));
			Assert.That(segments[4].SentenceStart, Is.False);
		}

		[Test]
		public void Segments_EmptyText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus.GetText("MRK");
			TextSegment[] segments = text.Segments.ToArray();
			Assert.That(segments, Is.Empty);
		}
	}
}
