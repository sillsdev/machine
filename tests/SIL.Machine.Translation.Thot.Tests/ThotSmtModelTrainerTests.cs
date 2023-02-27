using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    [TestFixture]
    public class ThotSmtModelTrainerTests
    {
        [Test]
        public async Task Train_NonEmptyCorpus()
        {
            using var tempDir = new TempDirectory("ThotSmtModelTrainerTests");
            var sourceCorpus = new DictionaryTextCorpus(
                new[]
                {
                    new MemoryText(
                        "text1",
                        new[]
                        {
                            Row(1, "¿ le importaría darnos las llaves de la habitación , por favor ?"),
                            Row(
                                2,
                                "he hecho la reserva de una habitación tranquila doble con ||| teléfono ||| y "
                                    + "televisión a nombre de rosario cabedo ."
                            ),
                            Row(3, "¿ le importaría cambiarme a otra habitación más tranquila ?"),
                            Row(4, "por favor , tengo reservada una habitación ."),
                            Row(5, "me parece que existe un problema ."),
                            Row(6, "¿ tiene habitaciones libres con televisión , aire acondicionado y caja fuerte ?"),
                            Row(7, "¿ le importaría mostrarnos una habitación con televisión ?"),
                            Row(8, "¿ tiene teléfono ?"),
                            Row(9, "voy a marcharme el dos a las ocho de la noche ."),
                            Row(10, "¿ cuánto cuesta una habitación individual por semana ?")
                        }
                    )
                }
            );

            var targetCorpus = new DictionaryTextCorpus(
                new[]
                {
                    new MemoryText(
                        "text1",
                        new[]
                        {
                            Row(1, "would you mind giving us the keys to the room , please ?"),
                            Row(
                                2,
                                "i have made a reservation for a quiet , double room with a ||| telephone ||| and a tv "
                                    + "for rosario cabedo ."
                            ),
                            Row(3, "would you mind moving me to a quieter room ?"),
                            Row(4, "i have booked a room ."),
                            Row(5, "i think that there is a problem ."),
                            Row(6, "do you have any rooms with a tv , air conditioning and a safe available ?"),
                            Row(7, "would you mind showing us a room with a tv ?"),
                            Row(8, "does it have a telephone ?"),
                            Row(9, "i am leaving on the second at eight in the evening ."),
                            Row(10, "how much does a single room cost per week ?")
                        }
                    )
                }
            );

            var alignmentCorpus = new DictionaryAlignmentCorpus(
                new[]
                {
                    new MemoryAlignmentCollection(
                        "text1",
                        new[]
                        {
                            Alignment(1, new AlignedWordPair(8, 9)),
                            Alignment(2, new AlignedWordPair(6, 10)),
                            Alignment(3, new AlignedWordPair(6, 8)),
                            Alignment(4, new AlignedWordPair(6, 4)),
                            Alignment(5),
                            Alignment(6, new AlignedWordPair(2, 4)),
                            Alignment(7, new AlignedWordPair(5, 6)),
                            Alignment(8),
                            Alignment(9),
                            Alignment(10, new AlignedWordPair(4, 5))
                        }
                    )
                }
            );

            IParallelTextCorpus corpus = sourceCorpus.AlignRows(targetCorpus, alignmentCorpus);

            var parameters = new ThotSmtParameters
            {
                TranslationModelFileNamePrefix = Path.Combine(tempDir.Path, "tm", "src_trg"),
                LanguageModelFileNamePrefix = Path.Combine(tempDir.Path, "lm", "trg.lm")
            };

            using (var trainer = new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, parameters))
            {
                await trainer.TrainAsync();
                await trainer.SaveAsync();
                parameters = trainer.Parameters;
            }

            using var model = new ThotSmtModel(ThotWordAlignmentModelType.Hmm, parameters);
            TranslationResult result = await model.TranslateAsync("una habitación individual por semana".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("a single room cost per week".Split()));
        }

        [Test]
        public async Task Train_EmptyCorpus()
        {
            using var tempDir = new TempDirectory("ThotSmtModelTrainerTests");
            var sourceCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
            var targetCorpus = new DictionaryTextCorpus(Enumerable.Empty<MemoryText>());
            var alignmentCorpus = new DictionaryAlignmentCorpus(Enumerable.Empty<MemoryAlignmentCollection>());

            IParallelTextCorpus corpus = sourceCorpus.AlignRows(targetCorpus, alignmentCorpus);

            var parameters = new ThotSmtParameters
            {
                TranslationModelFileNamePrefix = Path.Combine(tempDir.Path, "tm", "src_trg"),
                LanguageModelFileNamePrefix = Path.Combine(tempDir.Path, "lm", "trg.lm")
            };

            using (var trainer = new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, parameters))
            {
                await trainer.TrainAsync();
                await trainer.SaveAsync();
                parameters = trainer.Parameters;
            }

            using var model = new ThotSmtModel(ThotWordAlignmentModelType.Hmm, parameters);
            TranslationResult result = await model.TranslateAsync("una habitación individual por semana".Split());
            Assert.That(result.TargetSegment, Is.EqualTo("una habitación individual por semana".Split()));
        }

        private static TextRow Row(int rowRef, string text)
        {
            return new TextRow("text1", rowRef) { Segment = text.Split() };
        }

        private static AlignmentRow Alignment(int rowRef, params AlignedWordPair[] pairs)
        {
            return new AlignmentRow("text1", rowRef) { AlignedWordPairs = pairs };
        }
    }
}
