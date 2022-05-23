using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Tokenization.SentencePiece
{
    [TestFixture]
    public class SentencePieceTokenizerTests
    {
        private static string TestFilename =>
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "test.txt");

        private string _tempDir;

        private string ModelFilename => Path.Combine(_tempDir, "sp.model");

        [OneTimeSetUp]
        public void CreateModel()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
            Directory.CreateDirectory(_tempDir);
            var trainer = new SentencePieceTrainer { VocabSize = 100 };
            trainer.Train(TestFilename, Path.Combine(_tempDir, "sp"));
        }

        [OneTimeTearDown]
        public void DeleteModel()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public void Tokenize()
        {
            using (var processor = new SentencePieceTokenizer(ModelFilename))
            {
                string[] tokens = processor.Tokenize("Other travelling salesmen live a life of luxury.").ToArray();
                Assert.That(tokens.Length, Is.EqualTo(30));
            }
        }

        [Test]
        public void Tokenize_Empty()
        {
            using (var processor = new SentencePieceTokenizer(ModelFilename))
            {
                string[] tokens = processor.Tokenize("").ToArray();
                Assert.That(tokens.Length, Is.EqualTo(0));
            }
        }
    }
}
