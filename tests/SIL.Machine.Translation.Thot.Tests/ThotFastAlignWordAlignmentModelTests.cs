using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
    [TestFixture]
    public class ThotFastAlignWordAlignmentModelTests
    {
        private string DirectModelPath =>
            Path.Combine(TestHelpers.ToyCorpusFastAlignFolderName, "tm", "src_trg_invswm");
        private string InverseModelPath => Path.Combine(TestHelpers.ToyCorpusFastAlignFolderName, "tm", "src_trg_swm");

        [Test]
        public void GetBestAlignment()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split(' ');
            string[] targetSegment = "could we see another room , please ?".Split(' ');
            WordAlignmentMatrix waMatrix = model.GetBestAlignment(sourceSegment, targetSegment);
            var expected = new WordAlignmentMatrix(
                9,
                8,
                new[] { (0, 0), (4, 1), (5, 2), (6, 3), (7, 4), (8, 6), (8, 7) }
            );
            Assert.That(waMatrix.ValueEquals(expected), Is.True);
        }

        [Test]
        public void GetAvgTranslationScore()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split(' ');
            string[] targetSegment = "could we see another room , please ?".Split(' ');
            WordAlignmentMatrix waMatrix = model.GetBestAlignment(sourceSegment, targetSegment);
            double score = model.GetAvgTranslationScore(sourceSegment, targetSegment, waMatrix);
            Assert.That(score, Is.EqualTo(0.34).Within(0.01));
        }

        [Test]
        public void GetTranslationProbability()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            Assert.That(model.GetTranslationProbability("esto", "this"), Is.EqualTo(0.0).Within(0.01));
            Assert.That(model.GetTranslationProbability("es", "is"), Is.EqualTo(0.90).Within(0.01));
            Assert.That(model.GetTranslationProbability("una", "a"), Is.EqualTo(0.83).Within(0.01));
            Assert.That(model.GetTranslationProbability("prueba", "test"), Is.EqualTo(0.0).Within(0.01));
        }

        [Test]
        public void SourceWords_Enumerate()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            Assert.That(model.SourceWords.Count(), Is.EqualTo(500));
        }

        [Test]
        public void SourceWords_IndexAccessor()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            Assert.That(model.SourceWords[0], Is.EqualTo("NULL"));
            Assert.That(model.SourceWords[499], Is.EqualTo("pagar"));
        }

        [Test]
        public void SourceWords_Count()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            Assert.That(model.SourceWords.Count, Is.EqualTo(500));
        }

        [Test]
        public void TargetWords_Enumerate()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            Assert.That(model.TargetWords.Count(), Is.EqualTo(352));
        }

        [Test]
        public void TargetWords_IndexAccessor()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            Assert.That(model.TargetWords[0], Is.EqualTo("NULL"));
            Assert.That(model.TargetWords[351], Is.EqualTo("pay"));
        }

        [Test]
        public void TargetWords_Count()
        {
            using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
            Assert.That(model.TargetWords.Count, Is.EqualTo(352));
        }

        [Test]
        public void GetTranslationTable_SymmetrizedNoThreshold()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotFastAlignWordAlignmentModel(DirectModelPath),
                new ThotFastAlignWordAlignmentModel(InverseModelPath)
            );
            Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable();
            Assert.That(table.Count, Is.EqualTo(500));
            Assert.That(table["es"].Count, Is.EqualTo(21));
        }

        [Test]
        public void GetTranslationTable_SymmetrizedThreshold()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotFastAlignWordAlignmentModel(DirectModelPath),
                new ThotFastAlignWordAlignmentModel(InverseModelPath)
            );
            Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable(0.2);
            Assert.That(table.Count, Is.EqualTo(500));
            Assert.That(table["es"].Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAvgTranslationScore_Symmetrized()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotFastAlignWordAlignmentModel(DirectModelPath),
                new ThotFastAlignWordAlignmentModel(InverseModelPath)
            );
            string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split(' ');
            string[] targetSegment = "could we see another room , please ?".Split(' ');
            WordAlignmentMatrix waMatrix = model.GetBestAlignment(sourceSegment, targetSegment);
            double score = model.GetAvgTranslationScore(sourceSegment, targetSegment, waMatrix);
            Assert.That(score, Is.EqualTo(0.36).Within(0.01));
        }
    }
}
