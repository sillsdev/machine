using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora
{
    [TestFixture]
    public class ParatextBackupTextCorpusTests
    {
        [Test]
        public void Texts()
        {
            using var env = new TestEnvironment();
            Assert.That(env.Corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "MAT", "MRK" }));
        }

        [Test]
        public void TryGetText()
        {
            using var env = new TestEnvironment();
            Assert.That(env.Corpus.TryGetText("MAT", out IText mat), Is.True);
            Assert.That(mat.GetRows(), Is.Not.Empty);
            Assert.That(env.Corpus.TryGetText("LUK", out _), Is.False);
        }

        private class TestEnvironment : DisposableBase
        {
            private readonly string _backupPath;

            public TestEnvironment()
            {
                _backupPath = CorporaTestHelpers.CreateTestParatextBackup();
                Corpus = new ParatextBackupTextCorpus(_backupPath);
            }

            public ParatextBackupTextCorpus Corpus { get; }

            protected override void DisposeManagedResources()
            {
                if (File.Exists(_backupPath))
                    File.Delete(_backupPath);
            }
        }
    }
}
