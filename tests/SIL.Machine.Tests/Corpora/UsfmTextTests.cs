using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Tests.Corpora
{
	[TestFixture]
	public class UsfmTextTests
	{
		[Test]
		public void Segments_NonEmptyText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmTextCorpus(CorporaTestHelpers.UsfmStylesheetPath, Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath, tokenizer);

			IText text = corpus.GetText("41MAT");
			TextSegment[] segments = text.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(10));
			Assert.That(segments[0].SegmentRef, Is.EqualTo(new TextSegmentRef(1, 1)));
			Assert.That(segments[0].Segment, Is.EqualTo("Chapter one , verse one .".Split()));
			Assert.That(segments[1].SegmentRef, Is.EqualTo(new TextSegmentRef(1, 2)));
			Assert.That(segments[1].Segment, Is.EqualTo("Chapter one , verse two .".Split()));
			Assert.That(segments[5].SegmentRef, Is.EqualTo(new TextSegmentRef(2, 1)));
			Assert.That(segments[5].Segment, Is.EqualTo("Chapter two , verse one .".Split()));
			Assert.That(segments[9].SegmentRef, Is.EqualTo(new TextSegmentRef(2, 5)));
			Assert.That(segments[9].Segment, Is.EqualTo("Chapter two , verse five .".Split()));
		}

		[Test]
		public void Segments_EmptyText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmTextCorpus(CorporaTestHelpers.UsfmStylesheetPath, Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath, tokenizer);

			IText text = corpus.GetText("42MRK");
			TextSegment[] segments = text.Segments.ToArray();
			Assert.That(segments, Is.Empty);
		}
	}
}
