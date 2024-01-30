using System.Linq;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    [TestFixture]
    public class UsxZipTextTests
    {
        [Test]
        public void GetRows_NonEmptyText()
        {
            using (var env = new DblBundleTestEnvironment())
            {
                IText text = env.Corpus["MAT"];
                TextRow[] segments = text.GetRows().ToArray();
                Assert.That(segments.Length, Is.EqualTo(14));

                Assert.That(segments[0].Ref, Is.EqualTo(new VerseRef("MAT 1:1", env.Corpus.Versification)));
                Assert.That(segments[0].Text, Is.EqualTo("Chapter one, verse one."));

                Assert.That(segments[1].Ref, Is.EqualTo(new VerseRef("MAT 1:2", env.Corpus.Versification)));
                Assert.That(segments[1].Text, Is.EqualTo("Chapter one, verse two."));

                Assert.That(segments[4].Ref, Is.EqualTo(new VerseRef("MAT 1:5", env.Corpus.Versification)));
                Assert.That(segments[4].Text, Is.EqualTo("Chapter one, verse five."));

                Assert.That(segments[5].Ref, Is.EqualTo(new VerseRef("MAT 2:1", env.Corpus.Versification)));
                Assert.That(segments[5].Text, Is.EqualTo("Chapter two, verse one."));

                Assert.That(segments[6].Ref, Is.EqualTo(new VerseRef("MAT 2:2", env.Corpus.Versification)));
                Assert.That(segments[6].Text, Is.EqualTo("Chapter two, verse two. Chapter two, verse three."));
                Assert.That(segments[6].IsInRange, Is.True);

                Assert.That(segments[7].Ref, Is.EqualTo(new VerseRef("MAT 2:3", env.Corpus.Versification)));
                Assert.That(segments[7].Text, Is.Empty);
                Assert.That(segments[7].IsInRange, Is.True);

                Assert.That(segments[8].Ref, Is.EqualTo(new VerseRef("MAT 2:4a", env.Corpus.Versification)));
                Assert.That(segments[8].Text, Is.Empty);
                Assert.That(segments[8].IsInRange, Is.True);

                Assert.That(segments[9].Ref, Is.EqualTo(new VerseRef("MAT 2:4b", env.Corpus.Versification)));
                Assert.That(segments[9].Text, Is.EqualTo("Chapter two, verse four."));

                Assert.That(segments[10].Ref, Is.EqualTo(new VerseRef("MAT 2:5", env.Corpus.Versification)));
                Assert.That(segments[10].Text, Is.EqualTo("Chapter two, verse five."));

                Assert.That(segments[11].Ref, Is.EqualTo(new VerseRef("MAT 2:6", env.Corpus.Versification)));
                Assert.That(segments[11].Text, Is.EqualTo("Chapter two, verse six."));
            }
        }

        [Test]
        public void GetRows_SentenceStart()
        {
            using (var env = new DblBundleTestEnvironment())
            {
                IText text = env.Corpus["MAT"];
                TextRow[] segments = text.GetRows().ToArray();
                Assert.That(segments.Length, Is.EqualTo(14));

                Assert.That(segments[3].Ref, Is.EqualTo(new VerseRef("MAT 1:4", env.Corpus.Versification)));
                Assert.That(segments[3].Text, Is.EqualTo("Chapter one, verse four,"));
                Assert.That(segments[3].IsSentenceStart, Is.True);

                Assert.That(segments[4].Ref, Is.EqualTo(new VerseRef("MAT 1:5", env.Corpus.Versification)));
                Assert.That(segments[4].Text, Is.EqualTo("Chapter one, verse five."));
                Assert.That(segments[4].IsSentenceStart, Is.False);
            }
        }

        [Test]
        public void GetRows_EmptyText()
        {
            using (var env = new DblBundleTestEnvironment())
            {
                IText text = env.Corpus["MRK"];
                TextRow[] segments = text.GetRows().ToArray();
                Assert.That(segments, Is.Empty);
            }
        }
    }
}
