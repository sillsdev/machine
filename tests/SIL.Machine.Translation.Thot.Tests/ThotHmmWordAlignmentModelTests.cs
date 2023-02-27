using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    [TestFixture]
    public class ThotHmmWordAlignmentModelTests
    {
        private string DirectModelPath => Path.Combine(TestHelpers.ToyCorpusHmmFolderName, "tm", "src_trg_invswm");
        private string InverseModelPath => Path.Combine(TestHelpers.ToyCorpusHmmFolderName, "tm", "src_trg_swm");

        [Test]
        public void Align()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split();
            string[] targetSegment = "could we see another room , please ?".Split();
            WordAlignmentMatrix alignment = model.Align(sourceSegment, targetSegment);
            Assert.That(alignment.ToString(), Is.EqualTo("0-5 1-6 3-7 4-0 4-1 5-2 6-3 7-4"));
        }

        [Test]
        public void AlignBatch()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            (string, string)[] batch =
            {
                (
                    "por favor , desearía reservar una habitación hasta mañana .",
                    "i would like to book a room until tomorrow , please ."
                ),
                (
                    "por favor , despiértenos mañana a las siete y cuarto .",
                    "please wake us up tomorrow at a quarter past seven ."
                ),
                ("voy a marcharme hoy por la tarde .", "i am leaving today in the afternoon ."),
                (
                    "por favor , ¿ les importaría bajar nuestro equipaje a la habitación número cero trece ?",
                    "would you mind sending down our luggage to room number oh one three , please ?"
                ),
                (
                    "¿ me podrían dar la llave de la habitación dos cuatro cuatro , por favor ?",
                    "could you give me the key to room number two four four , please ?"
                )
            };
            IReadOnlyList<WordAlignmentMatrix> alignments = model.AlignBatch(
                batch
                    .Select(p => ((IReadOnlyList<string>)p.Item1.Split(), (IReadOnlyList<string>)p.Item2.Split()))
                    .ToArray()
            );
            Assert.That(
                alignments.Select(m => m.ToString()),
                Is.EqualTo(
                    new[]
                    {
                        "0-9 1-10 3-1 3-2 4-3 4-4 5-5 6-6 7-7 8-8 9-11",
                        "2-0 3-1 3-2 3-3 4-4 6-5 7-9 8-6 9-7 9-8 10-10",
                        "0-0 0-1 0-2 3-3 5-5 6-4 6-6 7-7",
                        "0-13 0-14 3-15 4-0 5-1 5-2 5-3 6-4 7-5 8-6 9-7 11-8 12-9 13-10 14-11 14-12",
                        "2-0 2-1 3-2 3-3 4-4 5-5 6-6 8-7 9-8 9-9 10-10 11-11 13-12 14-13 15-14"
                    }
                )
            );
        }

        [Test]
        public void GetAvgTranslationScore()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split();
            string[] targetSegment = "could we see another room , please ?".Split();
            WordAlignmentMatrix alignment = model.Align(sourceSegment, targetSegment);
            double score = model.GetAvgTranslationScore(sourceSegment, targetSegment, alignment);
            Assert.That(score, Is.EqualTo(0.40).Within(0.01));
        }

        [Test]
        public void GetBestAlignedWordPairs()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            string[] sourceSegment = "hablé hasta cinco en punto .".Split();
            string[] targetSegment = "i am staying until five o ' clock .".Split();
            AlignedWordPair[] pairs = model.GetBestAlignedWordPairs(sourceSegment, targetSegment).ToArray();
            Assert.That(pairs.Length, Is.EqualTo(8));

            Assert.That(pairs[0].SourceIndex, Is.EqualTo(1));
            Assert.That(pairs[0].TargetIndex, Is.EqualTo(3));
            Assert.That(pairs[0].TranslationScore, Is.EqualTo(0.78).Within(0.01));
            Assert.That(pairs[0].AlignmentScore, Is.EqualTo(0.18).Within(0.01));
        }

        [Test]
        public void ComputeAlignedWordPairScores()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            string[] sourceSegment = "hablé hasta cinco en punto .".Split();
            string[] targetSegment = "i am staying until five o ' clock .".Split();
            WordAlignmentMatrix waMatrix = model.Align(sourceSegment, targetSegment);
            AlignedWordPair[] pairs = waMatrix.ToAlignedWordPairs(includeNull: true).ToArray();
            model.ComputeAlignedWordPairScores(sourceSegment, targetSegment, pairs);
            Assert.That(pairs.Length, Is.EqualTo(11));

            Assert.That(pairs[0].SourceIndex, Is.EqualTo(-1));
            Assert.That(pairs[0].TargetIndex, Is.EqualTo(0));
            Assert.That(pairs[0].TranslationScore, Is.EqualTo(0.34).Within(0.01));
            Assert.That(pairs[0].AlignmentScore, Is.EqualTo(0.08).Within(0.01));

            Assert.That(pairs[1].SourceIndex, Is.EqualTo(0));
            Assert.That(pairs[1].TargetIndex, Is.EqualTo(-1));
            Assert.That(pairs[1].TranslationScore, Is.EqualTo(0));
            Assert.That(pairs[1].AlignmentScore, Is.EqualTo(0));

            Assert.That(pairs[2].SourceIndex, Is.EqualTo(1));
            Assert.That(pairs[2].TargetIndex, Is.EqualTo(3));
            Assert.That(pairs[2].TranslationScore, Is.EqualTo(0.78).Within(0.01));
            Assert.That(pairs[2].AlignmentScore, Is.EqualTo(0.18).Within(0.01));
        }

        [Test]
        public void GetTranslationProbability()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            Assert.That(model.GetTranslationProbability("esto", "this"), Is.EqualTo(0.0).Within(0.01));
            Assert.That(model.GetTranslationProbability("es", "is"), Is.EqualTo(0.65).Within(0.01));
            Assert.That(model.GetTranslationProbability("una", "a"), Is.EqualTo(0.70).Within(0.01));
            Assert.That(model.GetTranslationProbability("prueba", "test"), Is.EqualTo(0.0).Within(0.01));
        }

        [Test]
        public void SourceWords_Enumerate()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            Assert.That(model.SourceWords.Count(), Is.EqualTo(513));
        }

        [Test]
        public void SourceWords_IndexAccessor()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            Assert.That(model.SourceWords[0], Is.EqualTo("NULL"));
            Assert.That(model.SourceWords[512], Is.EqualTo("pagar"));
        }

        [Test]
        public void SourceWords_Count()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            Assert.That(model.SourceWords.Count, Is.EqualTo(513));
        }

        [Test]
        public void TargetWords_Enumerate()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            Assert.That(model.TargetWords.Count(), Is.EqualTo(363));
        }

        [Test]
        public void TargetWords_IndexAccessor()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            Assert.That(model.TargetWords[0], Is.EqualTo("NULL"));
            Assert.That(model.TargetWords[362], Is.EqualTo("pay"));
        }

        [Test]
        public void TargetWords_Count()
        {
            using var model = new ThotHmmWordAlignmentModel(DirectModelPath);
            Assert.That(model.TargetWords.Count, Is.EqualTo(363));
        }

        [Test]
        public void GetTranslationTable_SymmetrizedNoThreshold()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotHmmWordAlignmentModel(DirectModelPath),
                new ThotHmmWordAlignmentModel(InverseModelPath)
            );
            Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable();
            Assert.That(table.Count, Is.EqualTo(513));
            Assert.That(table["es"].Count, Is.EqualTo(23));
        }

        [Test]
        public void GetTranslationTable_SymmetrizedThreshold()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotHmmWordAlignmentModel(DirectModelPath),
                new ThotHmmWordAlignmentModel(InverseModelPath)
            );
            Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable(0.2);
            Assert.That(table.Count, Is.EqualTo(513));
            Assert.That(table["es"].Count, Is.EqualTo(9));
        }

        [Test]
        public void GetAvgTranslationScore_Symmetrized()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotHmmWordAlignmentModel(DirectModelPath),
                new ThotHmmWordAlignmentModel(InverseModelPath)
            );
            string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split(' ');
            string[] targetSegment = "could we see another room , please ?".Split(' ');
            WordAlignmentMatrix waMatrix = model.Align(sourceSegment, targetSegment);
            double score = model.GetAvgTranslationScore(sourceSegment, targetSegment, waMatrix);
            Assert.That(score, Is.EqualTo(0.46).Within(0.01));
        }

        [Test]
        public void GetBestAlignedWordPairs_Symmetrized()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotHmmWordAlignmentModel(DirectModelPath),
                new ThotHmmWordAlignmentModel(InverseModelPath)
            );
            string[] sourceSegment = "hablé hasta cinco en punto .".Split(' ');
            string[] targetSegment = "i am staying until five o ' clock .".Split(' ');
            AlignedWordPair[] pairs = model.GetBestAlignedWordPairs(sourceSegment, targetSegment).ToArray();
            Assert.That(pairs.Length, Is.EqualTo(8));

            Assert.That(pairs[0].SourceIndex, Is.EqualTo(0));
            Assert.That(pairs[0].TargetIndex, Is.EqualTo(1));
            Assert.That(pairs[0].TranslationScore, Is.EqualTo(0.01).Within(0.01));
            Assert.That(pairs[0].AlignmentScore, Is.EqualTo(0.26).Within(0.01));
        }

        [Test]
        public void ComputeAlignedWordPairScores_Symmetrized()
        {
            using var model = new SymmetrizedWordAlignmentModel(
                new ThotHmmWordAlignmentModel(DirectModelPath),
                new ThotHmmWordAlignmentModel(InverseModelPath)
            );
            string[] sourceSegment = "hablé hasta cinco en punto .".Split(' ');
            string[] targetSegment = "i am staying until five o ' clock .".Split(' ');
            WordAlignmentMatrix waMatrix = model.Align(sourceSegment, targetSegment);
            AlignedWordPair[] pairs = waMatrix.ToAlignedWordPairs(includeNull: true).ToArray();
            model.ComputeAlignedWordPairScores(sourceSegment, targetSegment, pairs);
            Assert.That(pairs.Length, Is.EqualTo(10));

            Assert.That(pairs[0].SourceIndex, Is.EqualTo(-1));
            Assert.That(pairs[0].TargetIndex, Is.EqualTo(0));
            Assert.That(pairs[0].TranslationScore, Is.EqualTo(0.34).Within(0.01));
            Assert.That(pairs[0].AlignmentScore, Is.EqualTo(0.08).Within(0.01));

            Assert.That(pairs[1].SourceIndex, Is.EqualTo(-1));
            Assert.That(pairs[1].TargetIndex, Is.EqualTo(2));
            Assert.That(pairs[1].TranslationScore, Is.EqualTo(0.01).Within(0.01));
            Assert.That(pairs[1].AlignmentScore, Is.EqualTo(0.11).Within(0.01));

            Assert.That(pairs[2].SourceIndex, Is.EqualTo(0));
            Assert.That(pairs[2].TargetIndex, Is.EqualTo(1));
            Assert.That(pairs[2].TranslationScore, Is.EqualTo(0.01).Within(0.01));
            Assert.That(pairs[2].AlignmentScore, Is.EqualTo(0.26).Within(0.01));
        }

        [Test]
        public async Task CreateTrainer()
        {
            using var model = new ThotHmmWordAlignmentModel
            {
                Parameters = new ThotWordAlignmentParameters
                {
                    Ibm1IterationCount = 2,
                    HmmIterationCount = 2,
                    HmmP0 = 0.1
                }
            };
            ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
            await trainer.TrainAsync();
            await trainer.SaveAsync();

            WordAlignmentMatrix matrix = model.Align(
                "isthay isyay ayay esttay-N .".Split(),
                "this is a test N .".Split()
            );
            var expected = new WordAlignmentMatrix(
                5,
                6,
                new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (3, 3), (3, 4), (4, 5) }
            );
            Assert.That(matrix.ValueEquals(expected), Is.True);

            matrix = model.Align("isthay isyay otnay ayay esttay-N .".Split(), "this is not a test N .".Split());
            expected = new WordAlignmentMatrix(
                6,
                7,
                new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (3, 3), (4, 4), (4, 5), (5, 6) }
            );
            Assert.That(matrix.ValueEquals(expected), Is.True);

            matrix = model.Align("isthay isyay ayay esttay-N ardhay .".Split(), "this is a hard test N .".Split());
            expected = new WordAlignmentMatrix(
                6,
                7,
                new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (4, 3), (3, 4), (3, 5), (3, 6) }
            );
            Assert.That(matrix.ValueEquals(expected), Is.True);
        }
    }
}
