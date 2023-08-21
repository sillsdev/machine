using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;

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
                            Row(1, "¿ Le importaría darnos las llaves de la habitación , por favor ?"),
                            Row(
                                2,
                                "He hecho la reserva de una habitación tranquila doble con ||| teléfono ||| y "
                                    + "televisión a nombre de Rosario Cabedo ."
                            ),
                            Row(3, "¿ Le importaría cambiarme a otra habitación más tranquila ?"),
                            Row(4, "Por favor , tengo reservada una habitación ."),
                            Row(5, "Me parece que existe un problema ."),
                            Row(6, "¿ Tiene habitaciones libres con televisión , aire acondicionado y caja fuerte ?"),
                            Row(7, "¿ Le importaría mostrarnos una habitación con televisión ?"),
                            Row(8, "¿ Tiene teléfono ?"),
                            Row(9, "Voy a marcharme el dos a las ocho de la noche ."),
                            Row(10, "¿ Cuánto cuesta una habitación individual por semana ?")
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
                            Row(1, "Would you mind giving us the keys to the room , please ?"),
                            Row(
                                2,
                                "I have made a reservation for a quiet , double room with a ||| telephone ||| and a tv "
                                    + "for Rosario Cabedo ."
                            ),
                            Row(3, "Would you mind moving me to a quieter room ?"),
                            Row(4, "I have booked a room ."),
                            Row(5, "I think that there is a problem ."),
                            Row(6, "Do you have any rooms with a tv , air conditioning and a safe available ?"),
                            Row(7, "Would you mind showing us a room with a tv ?"),
                            Row(8, "Does it have a telephone ?"),
                            Row(9, "I am leaving on the second at eight in the evening ."),
                            Row(10, "How much does a single room cost per week ?")
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

            using (
                var trainer = new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, parameters)
                {
                    LowercaseSource = true,
                    LowercaseTarget = true
                }
            )
            {
                await trainer.TrainAsync();
                await trainer.SaveAsync();
                parameters = trainer.Parameters;
            }

            using var model = new ThotSmtModel(ThotWordAlignmentModelType.Hmm, parameters)
            {
                LowercaseSource = true,
                LowercaseTarget = true
            };
            TranslationResult result = await model.TranslateAsync("Una habitación individual por semana");
            Assert.That(result.Translation, Is.EqualTo("a single room cost per week"));
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

            using (
                var trainer = new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, parameters)
                {
                    LowercaseSource = true,
                    LowercaseTarget = true
                }
            )
            {
                await trainer.TrainAsync();
                await trainer.SaveAsync();
                parameters = trainer.Parameters;
            }

            using var model = new ThotSmtModel(ThotWordAlignmentModelType.Hmm, parameters)
            {
                LowercaseSource = true,
                LowercaseTarget = true
            };
            TranslationResult result = await model.TranslateAsync("Una habitación individual por semana");
            Assert.That(result.Translation, Is.EqualTo("una habitación individual por semana"));
        }

        private static TextRow Row(int rowRef, string text)
        {
            return new TextRow("text1", rowRef) { Segment = new[] { text } };
        }

        private static AlignmentRow Alignment(int rowRef, params AlignedWordPair[] pairs)
        {
            return new AlignmentRow("text1", rowRef) { AlignedWordPairs = pairs };
        }
    }
}
