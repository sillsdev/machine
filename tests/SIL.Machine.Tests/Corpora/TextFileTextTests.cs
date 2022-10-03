using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Corpora
{
    [TestFixture]
    public class TextFileTextTests
    {
        [Test]
        public void GetRows_NonEmptyTextMultiKeyRef()
        {
            var corpus = new TextFileTextCorpus(CorporaTestHelpers.TextTestProjectPath);
            IText text = corpus["Test1"];
            TextRow[] segments = text.GetRows().ToArray();
            Assert.That(segments.Length, Is.EqualTo(4));

            Assert.That(segments[0].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 1, 1)));
            Assert.That(segments[0].Text, Is.EqualTo("Section one, line one."));

            Assert.That(segments[1].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 1, 2)));
            Assert.That(segments[1].Text, Is.EqualTo("Section one, line two."));

            Assert.That(segments[2].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 2, 1)));
            Assert.That(segments[2].Text, Is.EqualTo("Section two, line one."));

            Assert.That(segments[3].Ref, Is.EqualTo(new MultiKeyRef("Test1", "s", 2, 2)));
            Assert.That(segments[3].Text, Is.EqualTo("Section two, line two."));
        }

        [Test]
        public void GetRows_NonEmptyTextTextFileRef()
        {
            var corpus = new TextFileTextCorpus(CorporaTestHelpers.TextTestProjectPath);
            IText text = corpus["Test3"];
            TextRow[] segments = text.GetRows().ToArray();
            Assert.That(segments.Length, Is.EqualTo(3));

            Assert.That(segments[0].Ref, Is.EqualTo(new MultiKeyRef("Test3", 1)));
            Assert.That(segments[0].Text, Is.EqualTo("Line one."));

            Assert.That(segments[1].Ref, Is.EqualTo(new MultiKeyRef("Test3", 2)));
            Assert.That(segments[1].Text, Is.EqualTo("Line two."));

            Assert.That(segments[2].Ref, Is.EqualTo(new MultiKeyRef("Test3", 3)));
            Assert.That(segments[2].Text, Is.EqualTo("Line three."));
        }

        [Test]
        public void GetRows_EmptyText()
        {
            var corpus = new TextFileTextCorpus(CorporaTestHelpers.TextTestProjectPath);
            IText text = corpus["Test2"];
            TextRow[] segments = text.GetRows().ToArray();
            Assert.That(segments, Is.Empty);
        }
    }
}
