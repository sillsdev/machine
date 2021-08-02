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
			var tokenizer = new NullTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus.GetText("MAT");
			TextSegment[] segments = text.GetSegments().ToArray();
			Assert.That(segments.Length, Is.EqualTo(12));

			Assert.That(segments[0].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));
			Assert.That(segments[0].Segment[0], Is.EqualTo("Chapter one, verse one."));

			Assert.That(segments[1].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:2", corpus.Versification)));
			Assert.That(segments[1].Segment[0], Is.EqualTo("Chapter one, verse two."));

			Assert.That(segments[4].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:5", corpus.Versification)));
			Assert.That(segments[4].Segment[0], Is.EqualTo("Chapter one, verse five."));

			Assert.That(segments[5].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:1", corpus.Versification)));
			Assert.That(segments[5].Segment[0], Is.EqualTo("Chapter two, verse one."));

			Assert.That(segments[6].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:2", corpus.Versification)));
			Assert.That(segments[6].Segment[0],
				Is.EqualTo("Chapter two, verse two. Chapter two, verse three."));
			Assert.That(segments[6].IsInRange, Is.True);

			Assert.That(segments[7].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));
			Assert.That(segments[7].Segment, Is.Empty);
			Assert.That(segments[7].IsInRange, Is.True);

			Assert.That(segments[8].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:4a", corpus.Versification)));
			Assert.That(segments[8].Segment, Is.Empty);
			Assert.That(segments[8].IsInRange, Is.True);

			Assert.That(segments[9].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:4b", corpus.Versification)));
			Assert.That(segments[9].Segment[0], Is.EqualTo("Chapter two, verse four."));

			Assert.That(segments[10].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:5", corpus.Versification)));
			Assert.That(segments[10].Segment[0], Is.EqualTo("Chapter two, verse five."));

			Assert.That(segments[11].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:6", corpus.Versification)));
			Assert.That(segments[11].Segment[0], Is.EqualTo("Chapter two, verse six."));
		}

		[Test]
		public void Segments_SentenceStart()
		{
			var tokenizer = new NullTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus.GetText("MAT");
			TextSegment[] segments = text.GetSegments().ToArray();
			Assert.That(segments.Length, Is.EqualTo(12));

			Assert.That(segments[3].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:4", corpus.Versification)));
			Assert.That(segments[3].Segment[0], Is.EqualTo("Chapter one, verse four,"));
			Assert.That(segments[3].SentenceStart, Is.True);

			Assert.That(segments[4].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:5", corpus.Versification)));
			Assert.That(segments[4].Segment[0], Is.EqualTo("Chapter one, verse five."));
			Assert.That(segments[4].SentenceStart, Is.False);
		}

		[Test]
		public void Segments_EmptyText()
		{
			var tokenizer = new LatinWordTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus.GetText("MRK");
			TextSegment[] segments = text.GetSegments().ToArray();
			Assert.That(segments, Is.Empty);
		}

		[Test]
		public void Segments_IncludeMarkers()
		{
			var tokenizer = new NullTokenizer();
			var corpus = new UsfmFileTextCorpus(tokenizer, CorporaTestHelpers.UsfmStylesheetPath,
				Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath, includeMarkers: true);

			IText text = corpus.GetText("MAT");
			TextSegment[] segments = text.GetSegments().ToArray();
			Assert.That(segments.Length, Is.EqualTo(12));

			Assert.That(segments[0].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));
			Assert.That(segments[0].Segment[0], Is.EqualTo("Chapter one, verse one.\\f + \\fr 1:1: \\ft This is a footnote.\\f*"));

			Assert.That(segments[1].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:2", corpus.Versification)));
			Assert.That(segments[1].Segment[0], Is.EqualTo("Chapter one, \\li2 verse two."));

			Assert.That(segments[4].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:5", corpus.Versification)));
			Assert.That(segments[4].Segment[0],
				Is.EqualTo("Chapter one, \\li2 verse \\fig Figure 1|src=\"image1.png\" size=\"col\" ref=\"1:5\"\\fig* five."));

			Assert.That(segments[5].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:1", corpus.Versification)));
			Assert.That(segments[5].Segment[0], Is.EqualTo(
				"Chapter \\add two\\add*, verse\\f + \\fr 2:1: \\ft This is a footnote.\\f* one."));

			Assert.That(segments[6].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:2", corpus.Versification)));
			Assert.That(segments[6].Segment[0],
				Is.EqualTo("Chapter two, verse two. Chapter two, verse three."));
			Assert.That(segments[6].IsInRange, Is.True);

			Assert.That(segments[7].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));
			Assert.That(segments[7].Segment, Is.Empty);
			Assert.That(segments[7].IsInRange, Is.True);

			Assert.That(segments[8].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:4a", corpus.Versification)));
			Assert.That(segments[8].Segment, Is.Empty);
			Assert.That(segments[8].IsInRange, Is.True);

			Assert.That(segments[9].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:4b", corpus.Versification)));
			Assert.That(segments[9].Segment[0], Is.EqualTo("Chapter two, verse four."));

			Assert.That(segments[10].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:5", corpus.Versification)));
			Assert.That(segments[10].Segment[0], Is.EqualTo("Chapter two, verse five."));

			Assert.That(segments[11].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:6", corpus.Versification)));
			Assert.That(segments[11].Segment[0], Is.EqualTo("Chapter two, verse \\w six|strong=\"12345\" \\w*."));
		}
	}
}
