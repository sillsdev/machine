using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
    [TestFixture]
    public class ThotSmtModelTests
    {
        [Test]
        public async Task TranslateAsync_TargetSegment_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            TranslationResult result = await smtModel.TranslateAsync("voy a marcharme hoy por la tarde .".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
        }

        [Test]
        public async Task TranslateAsync_NBestLessThanN_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(
                3,
                "voy a marcharme hoy por la tarde .".Split()
            );
            Assert.That(
                results.Select(tr => tr.TargetSegment),
                Is.EqualTo(new[] { "i am leaving today in the afternoon .".Split() })
            );
        }

        [Test]
        public async Task TranslateAsync_NBest_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(
                2,
                "hablé hasta cinco en punto .".Split()
            );
            Assert.That(
                results.Select(tr => tr.TargetSegment),
                Is.EqualTo(new[] { "hablé until five o ' clock .".Split(), "hablé until five o ' clock for".Split() })
            );
        }

        [Test]
        public async Task TrainSegmentAsync_Segment_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            TranslationResult result = await smtModel.TranslateAsync("esto es una prueba .".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("esto is a prueba .".Split()));
            await smtModel.TrainSegmentAsync("esto es una prueba .".Split(), "this is a test .".Split());
            result = await smtModel.TranslateAsync("esto es una prueba .".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
        }

        [Test]
        public async Task GetBestPhraseAlignmentAsync_SegmentPair_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            TranslationResult result = await smtModel.GetBestPhraseAlignmentAsync(
                "esto es una prueba .".Split(),
                "this is a test .".Split()
            );
            Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
        }

        [Test]
        public async Task GetWordGraphAsync_EmptySegment_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            WordGraph wordGraph = await smtModel.GetWordGraphAsync(new string[0]);
            Assert.That(wordGraph.IsEmpty, Is.True);
        }

        [Test]
        public async Task TranslateBatchAsync_Batch_Hmm()
        {
            string[] batch =
            {
                "por favor , desearía reservar una habitación hasta mañana .",
                "por favor , despiértenos mañana a las siete y cuarto .",
                "voy a marcharme hoy por la tarde .",
                "por favor , ¿ les importaría bajar nuestro equipaje a la habitación número cero trece ?",
                "¿ me podrían dar la llave de la habitación dos cuatro cuatro , por favor ?"
            };

            using ThotSmtModel smtModel = CreateHmmModel();
            IReadOnlyList<TranslationResult> results = await smtModel.TranslateBatchAsync(
                batch.Select(s => s.Split()).ToArray()
            );
            Assert.That(
                results.Select(tr => string.Join(" ", tr.TargetSegment)),
                Is.EqualTo(
                    new[]
                    {
                        "please i would like to book a room until tomorrow .",
                        "please wake us up tomorrow at a quarter past seven .",
                        "i am leaving today in the afternoon .",
                        "please would you mind sending down our luggage to room number oh thirteenth ?",
                        "could you give me the key to room number two four four , please ?"
                    }
                )
            );
        }

        [Test]
        public void TranslateBatch_Batch_Hmm()
        {
            string[] batch =
            {
                "por favor , desearía reservar una habitación hasta mañana .",
                "por favor , despiértenos mañana a las siete y cuarto .",
                "voy a marcharme hoy por la tarde .",
                "por favor , ¿ les importaría bajar nuestro equipaje a la habitación número cero trece ?",
                "¿ me podrían dar la llave de la habitación dos cuatro cuatro , por favor ?"
            };

            using ThotSmtModel smtModel = CreateHmmModel();
            IReadOnlyList<TranslationResult> results = smtModel.TranslateBatch(batch.Select(s => s.Split()).ToArray());
            Assert.That(
                results.Select(tr => string.Join(" ", tr.TargetSegment)),
                Is.EqualTo(
                    new[]
                    {
                        "please i would like to book a room until tomorrow .",
                        "please wake us up tomorrow at a quarter past seven .",
                        "i am leaving today in the afternoon .",
                        "please would you mind sending down our luggage to room number oh thirteenth ?",
                        "could you give me the key to room number two four four , please ?"
                    }
                )
            );
        }

        [Test]
        public async Task TranslateAsync_TargetSegment_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            TranslationResult result = await smtModel.TranslateAsync("voy a marcharme hoy por la tarde .".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
        }

        [Test]
        public async Task TranslateAsync_NBestLessThanN_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(
                3,
                "voy a marcharme hoy por la tarde .".Split()
            );
            Assert.That(
                results.Select(tr => tr.TargetSegment),
                Is.EqualTo(new[] { "i am leaving today in the afternoon .".Split() })
            );
        }

        [Test]
        public async Task TranslateAsync_NBest_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(
                2,
                "hablé hasta cinco en punto .".Split()
            );
            Assert.That(
                results.Select(tr => tr.TargetSegment),
                Is.EqualTo(
                    new[] { "hablé until five o ' clock .".Split(), "hablé until five o ' clock , please .".Split() }
                )
            );
        }

        [Test]
        public async Task TrainSegmentAsync_Segment_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            TranslationResult result = await smtModel.TranslateAsync("esto es una prueba .".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("esto is a prueba .".Split()));
            await smtModel.TrainSegmentAsync("esto es una prueba .".Split(), "this is a test .".Split());
            result = await smtModel.TranslateAsync("esto es una prueba .".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
        }

        [Test]
        public async Task GetBestPhraseAlignmentAsync_SegmentPair_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            TranslationResult result = await smtModel.GetBestPhraseAlignmentAsync(
                "esto es una prueba .".Split(),
                "this is a test .".Split()
            );
            Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
        }

        [Test]
        public async Task GetWordGraphAsync_EmptySegment_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            WordGraph wordGraph = await smtModel.GetWordGraphAsync(new string[0]);
            Assert.That(wordGraph.IsEmpty, Is.True);
        }

        private static ThotSmtModel CreateHmmModel()
        {
            return new ThotSmtModel(ThotWordAlignmentModelType.Hmm, TestHelpers.ToyCorpusHmmConfigFileName);
        }

        private static ThotSmtModel CreateFastAlignModel()
        {
            return new ThotSmtModel(ThotWordAlignmentModelType.FastAlign, TestHelpers.ToyCorpusFastAlignConfigFileName);
        }
    }
}
