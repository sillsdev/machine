using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Corpora
{
    [TestFixture]
    public class TextFileTextTests
    {
        [Test]
        public void GetRows_NonEmptyText_Refs()
        {
            var corpus = new TextFileTextCorpus(CorporaTestHelpers.TextTestProjectPath);
            IText text = corpus["Test1"];
            TextRow[] rows = text.GetRows().ToArray();
            Assert.That(rows.Length, Is.EqualTo(5));

            Assert.That(rows[0].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 1, 1)));
            Assert.That(rows[0].Text, Is.EqualTo("Section one, sentence one."));
            Assert.That(rows[0].Flags, Is.EqualTo(TextRowFlags.SentenceStart));

            Assert.That(rows[1].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 1, 2)));
            Assert.That(rows[1].Text, Is.EqualTo("Section one, sentence two. sentence three."));
            Assert.That(
                rows[1].Flags,
                Is.EqualTo(TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart)
            );

            Assert.That(rows[2].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 1, 3)));
            Assert.That(rows[2].Text, Is.Empty);
            Assert.That(rows[2].Flags, Is.EqualTo(TextRowFlags.InRange));

            Assert.That(rows[3].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 2, 1)));
            Assert.That(rows[3].Text, Is.EqualTo("Section two, sentence one."));
            Assert.That(rows[3].Flags, Is.EqualTo(TextRowFlags.SentenceStart));

            Assert.That(rows[4].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 2, 2)));
            Assert.That(rows[4].Text, Is.EqualTo("Section two, sentence two."));
            Assert.That(rows[4].Flags, Is.EqualTo(TextRowFlags.None));
        }

        [Test]
        public void GetRows_NonEmptyText_NoRefs()
        {
            var corpus = new TextFileTextCorpus(CorporaTestHelpers.TextTestProjectPath);
            IText text = corpus["Test3"];
            TextRow[] rows = text.GetRows().ToArray();
            Assert.That(rows.Length, Is.EqualTo(3));

            Assert.That(rows[0].Ref, Is.EqualTo(new MultiKeyRef("Test3", 1)));
            Assert.That(rows[0].Text, Is.EqualTo("Line one."));

            Assert.That(rows[1].Ref, Is.EqualTo(new MultiKeyRef("Test3", 2)));
            Assert.That(rows[1].Text, Is.EqualTo("Line two."));

            Assert.That(rows[2].Ref, Is.EqualTo(new MultiKeyRef("Test3", 3)));
            Assert.That(rows[2].Text, Is.EqualTo("Line three."));
        }

        [Test]
        public void GetRows_EmptyText()
        {
            var corpus = new TextFileTextCorpus(CorporaTestHelpers.TextTestProjectPath);
            IText text = corpus["Test2"];
            TextRow[] rows = text.GetRows().ToArray();
            Assert.That(rows, Is.Empty);
        }
    }
}
