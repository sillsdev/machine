using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Corpora
{
    [TestFixture]
    public class TextFileTextCorpusTests
    {
        [Test]
        public void DoesNotExist()
        {
            Assert.Throws<FileNotFoundException>(() => new TextFileTextCorpus("does-not-exist.txt"));
        }

        [Test]
        public void Folder()
        {
            var corpus = new TextFileTextCorpus(CorporaTestHelpers.TextTestProjectPath);

            Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "Test1", "Test2", "Test3" }));
        }

        [Test]
        public void SingleFile()
        {
            var corpus = new TextFileTextCorpus(Path.Combine(CorporaTestHelpers.TextTestProjectPath, "Test1.txt"));

            Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "*all*" }));
        }

        [Test]
        public void PatternStar()
        {
            var corpus = new TextFileTextCorpus(Path.Combine(CorporaTestHelpers.TextTestProjectPath, "*.txt"));

            Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "Test1", "Test2", "Test3" }));
        }

        [Test]
        public void PatternQuestionMark()
        {
            var corpus = new TextFileTextCorpus(Path.Combine(CorporaTestHelpers.TextTestProjectPath, "Test?.txt"));

            Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "1", "2", "3" }));
        }
    }
}
