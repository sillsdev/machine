using NUnit.Framework;
using System.Linq;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class DblBundleTextTests
	{
		[Test]
		public void Segments_NonEmptyText()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				IText text = env.Corpus.GetText("MAT");
				TextSegment[] segments = text.Segments.ToArray();
				Assert.That(segments.Length, Is.EqualTo(48));
				Assert.That(segments[0].SegmentRef, Is.EqualTo(new TextSegmentRef(1, 1)));
				Assert.That(segments[0].Segment,
					Is.EqualTo("This is the record of the ancestors of Jesus the Messiah , the descendant of King David and of Abraham , from whom all we Jews have descended .".Split()));
				Assert.That(segments[1].SegmentRef, Is.EqualTo(new TextSegmentRef(1, 2)));
				Assert.That(segments[1].Segment,
					Is.EqualTo("Abraham was the father of Isaac . Isaac was the father of Jacob . Jacob was the father of Judah and Judah's older and younger brothers .".Split()));
				Assert.That(segments[25].SegmentRef, Is.EqualTo(new TextSegmentRef(2, 1)));
				Assert.That(segments[25].Segment,
					Is.EqualTo("Jesus was born in Bethlehem town in Judea province during the time [ MTY ] that King Herod the Great ruled there . Some time after Jesus was born , some men who studied the stars and who lived in a country east of Judea came to Jerusalem city .".Split()));
				Assert.That(segments[36].SegmentRef, Is.EqualTo(new TextSegmentRef(2, 12)));
				Assert.That(segments[36].Segment,
					Is.EqualTo("Because God knew that King Herod planned to kill Jesus , in a dream the men who studied the stars were warned { he warned the men who studied the stars } that they should not return to King Herod . So they returned to their country , but instead of traveling back on the same road , they went on a different road .".Split()));
			}
		}

		[Test]
		public void Segments_EmptyText()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				IText text = env.Corpus.GetText("MRK");
				TextSegment[] segments = text.Segments.ToArray();
				Assert.That(segments, Is.Empty);
			}
		}
	}
}
