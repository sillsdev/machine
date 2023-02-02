using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    [TestFixture]
    public class UsfmFileTextCorpusTests
    {
        [Test]
        public void Texts()
        {
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

            Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "MAT", "MRK" }));
        }

        [Test]
        public void TryGetText()
        {
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

            Assert.That(corpus.TryGetText("MAT", out IText mat), Is.True);
            Assert.That(mat.GetRows(), Is.Not.Empty);
            Assert.That(corpus.TryGetText("LUK", out _), Is.False);
        }

        [Test]
        public void ExtractScripture()
        {
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

            var lines = corpus.ExtractScripture().ToList();
            Assert.That(lines.Count, Is.EqualTo(41899));

            (string text, VerseRef origRef, VerseRef? corpusRef) = lines[0];
            Assert.That(text, Is.EqualTo(""));
            Assert.That(origRef, Is.EqualTo(new VerseRef("GEN 1:1", ScrVers.Original)));
            Assert.That(corpusRef.HasValue, Is.False);

            (text, origRef, corpusRef) = lines[23213];
            Assert.That(text, Is.EqualTo("Chapter one, verse one."));
            Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 1:1", ScrVers.Original)));
            Assert.That(corpusRef.HasValue, Is.True);
            Assert.That(corpusRef.Value, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));

            (text, origRef, corpusRef) = lines[23240];
            Assert.That(text, Is.EqualTo("<range>"));
            Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:3", ScrVers.Original)));
            Assert.That(corpusRef.HasValue, Is.True);
            Assert.That(corpusRef.Value, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));
        }
    }
}
