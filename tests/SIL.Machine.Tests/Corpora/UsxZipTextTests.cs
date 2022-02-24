using NUnit.Framework;
using SIL.Scripture;
using System.Linq;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class UsxZipTextTests
	{
		[Test]
		public void GetSegments_NonEmptyText()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				IText text = env.Corpus.GetText("MAT");
				TextSegment[] segments = text.GetSegments().ToArray();
				Assert.That(segments.Length, Is.EqualTo(14));

				Assert.That(segments[0].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:1", env.Corpus.Versification)));
				Assert.That(segments[0].Segment[0], Is.EqualTo("Chapter one, verse one."));

				Assert.That(segments[1].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:2", env.Corpus.Versification)));
				Assert.That(segments[1].Segment[0], Is.EqualTo("Chapter one, verse two."));

				Assert.That(segments[4].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:5", env.Corpus.Versification)));
				Assert.That(segments[4].Segment[0], Is.EqualTo("Chapter one, verse five."));

				Assert.That(segments[5].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:1", env.Corpus.Versification)));
				Assert.That(segments[5].Segment[0], Is.EqualTo("Chapter two, verse one."));

				Assert.That(segments[6].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:2", env.Corpus.Versification)));
				Assert.That(segments[6].Segment[0], Is.EqualTo("Chapter two, verse two. Chapter two, verse three."));
				Assert.That(segments[6].IsInRange, Is.True);

				Assert.That(segments[7].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:3", env.Corpus.Versification)));
				Assert.That(segments[7].Segment, Is.Empty);
				Assert.That(segments[7].IsInRange, Is.True);

				Assert.That(segments[8].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:4a", env.Corpus.Versification)));
				Assert.That(segments[8].Segment, Is.Empty);
				Assert.That(segments[8].IsInRange, Is.True);

				Assert.That(segments[9].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:4b", env.Corpus.Versification)));
				Assert.That(segments[9].Segment[0], Is.EqualTo("Chapter two, verse four."));

				Assert.That(segments[10].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:5", env.Corpus.Versification)));
				Assert.That(segments[10].Segment[0], Is.EqualTo("Chapter two, verse five."));

				Assert.That(segments[11].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:6", env.Corpus.Versification)));
				Assert.That(segments[11].Segment[0], Is.EqualTo("Chapter two, verse six."));
			}
		}

		[Test]
		public void GetSegments_SentenceStart()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				IText text = env.Corpus.GetText("MAT");
				TextSegment[] segments = text.GetSegments().ToArray();
				Assert.That(segments.Length, Is.EqualTo(14));

				Assert.That(segments[3].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:4", env.Corpus.Versification)));
				Assert.That(segments[3].Segment[0], Is.EqualTo("Chapter one, verse four,"));
				Assert.That(segments[3].IsSentenceStart, Is.True);

				Assert.That(segments[4].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:5", env.Corpus.Versification)));
				Assert.That(segments[4].Segment[0], Is.EqualTo("Chapter one, verse five."));
				Assert.That(segments[4].IsSentenceStart, Is.False);
			}
		}

		[Test]
		public void GetSegments_EmptyText()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				IText text = env.Corpus.GetText("MRK");
				TextSegment[] segments = text.GetSegments().ToArray();
				Assert.That(segments, Is.Empty);
			}
		}
	}
}
