using NUnit.Framework;
using SIL.Scripture;
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

				Assert.That(segments[0].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:1", env.Corpus.Versification)));
				Assert.That(segments[0].Segment[0],
					Is.EqualTo("This is the record of the ancestors of Jesus the Messiah, the descendant of King David and of Abraham, from whom all we Jews have descended."));

				Assert.That(segments[1].SegmentRef, Is.EqualTo(new VerseRef("MAT 1:2", env.Corpus.Versification)));
				Assert.That(segments[1].Segment[0],
					Is.EqualTo("Abraham was the father of Isaac. Isaac was the father of Jacob. Jacob was the father of Judah and Judah's older and younger brothers."));

				Assert.That(segments[25].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:1", env.Corpus.Versification)));
				Assert.That(segments[25].Segment[0],
					Is.EqualTo("Jesus was born in Bethlehem town in Judea province during the time [MTY] that King Herod the Great ruled there. Some time after Jesus was born, some men who studied the stars and who lived in a country east of Judea came to Jerusalem city."));

				Assert.That(segments[36].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:12", env.Corpus.Versification)));
				Assert.That(segments[36].Segment[0],
					Is.EqualTo("Because God knew that King Herod planned to kill Jesus, in a dream the men who studied the stars were warned {he warned the men who studied the stars} that they should not return to King Herod. So they returned to their country, but instead of traveling back on the same road, they went on a different road."));

				Assert.That(segments[39].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:15", env.Corpus.Versification)));
				Assert.That(segments[39].Segment[0],
					Is.EqualTo("They stayed there until King Herod died, and then they left Egypt. By doing that, it was {they} fulfilled what the prophet Hosea wrote, which had been said by the Lord {which the Lord had said}, I have told my son to come out of Egypt."));
			}
		}

		[Test]
		public void Segments_SentenceStart()
		{
			using (var env = new DblBundleTestEnvironment())
			{
				IText text = env.Corpus.GetText("MAT");
				TextSegment[] segments = text.Segments.ToArray();
				Assert.That(segments.Length, Is.EqualTo(48));

				Assert.That(segments[38].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:14", env.Corpus.Versification)));
				Assert.That(segments[38].Segment[0],
					Is.EqualTo("So Joseph got up, he took the child and his mother that night, and they fled to Egypt."));
				Assert.That(segments[38].SentenceStart, Is.True);

				Assert.That(segments[46].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:22", env.Corpus.Versification)));
				Assert.That(segments[46].Segment[0],
					Is.EqualTo("When Joseph heard that Archaelaus now ruled in Judea district instead of his father, King Herod the Great, he was afraid to go there. Because he was warned {God warned Joseph} in a dream that it was still dangerous for them to live in Judea, he and Mary and Jesus went to Galilee District"));
				Assert.That(segments[46].SentenceStart, Is.True);

				Assert.That(segments[47].SegmentRef, Is.EqualTo(new VerseRef("MAT 2:23", env.Corpus.Versification)));
				Assert.That(segments[47].Segment[0],
					Is.EqualTo("to the town called Nazareth to live there. The result was that what had been said by the ancient prophets {what the ancient prophets had said} about the Messiah, that he would be called {people would call him} a Nazareth-man, was fulfilled {came true}."));
				Assert.That(segments[47].SentenceStart, Is.False);
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
