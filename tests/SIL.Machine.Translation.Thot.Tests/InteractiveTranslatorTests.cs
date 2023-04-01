using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
    [TestFixture]
    public class InteractiveTranslatorTests
    {
        [Test]
        public async Task TargetSegment_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("me marcho hoy por la tarde .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
        }

        [Test]
        public async Task SetPrefix_AddWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("me marcho hoy por la tarde .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
            translator.SetPrefix("i am ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i am leave today in the afternoon ."));
            translator.SetPrefix("i am leaving ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i am leaving today in the afternoon ."));
        }

        [Test]
        public async Task SetPrefix_MissingWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("caminé a mi habitación .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("caminé to my room ."));
            translator.SetPrefix("i walked ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i walked to my room ."));
        }

        [Test]
        public async Task SetPrefix_RemoveWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("me marcho hoy por la tarde .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
            translator.SetPrefix("i am ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i am leave today in the afternoon ."));
            translator.SetPrefix("i ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
        }

        [Test]
        public async Task ApproveAsync_TwoSegmentsUnknownWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("hablé con recepción .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("hablé with reception ."));
            translator.SetPrefix("i talked ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i talked with reception ."));
            translator.SetPrefix("i talked with reception .");
            await translator.ApproveAsync(false);

            translator = await factory.CreateAsync("hablé hasta cinco en punto .");

            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i talked until five o ' clock ."));
        }

        [Test]
        public async Task TargetSegment_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("me marcho hoy por la tarde .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
        }

        [Test]
        public async Task SetPrefix_AddWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("me marcho hoy por la tarde .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
            translator.SetPrefix("i am ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i am leave today in the afternoon ."));
            translator.SetPrefix("i am leaving ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i am leaving today in the afternoon ."));
        }

        [Test]
        public async Task SetPrefix_MissingWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("caminé a mi habitación .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("caminé to my room ."));
            translator.SetPrefix("i walked ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i walked to my room ."));
        }

        [Test]
        public async Task SetPrefix_RemoveWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("me marcho hoy por la tarde .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
            translator.SetPrefix("i am ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i am leave today in the afternoon ."));
            translator.SetPrefix("i ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
        }

        [Test]
        public async Task ApproveAsync_TwoSegmentsUnknownWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var factory = new InteractiveTranslatorFactory(smtModel);
            InteractiveTranslator translator = await factory.CreateAsync("hablé con recepción .");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("hablé with reception ."));
            translator.SetPrefix("i talked ");
            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i talked with reception ."));
            translator.SetPrefix("i talked with reception .");
            await translator.ApproveAsync(false);

            translator = await factory.CreateAsync("hablé hasta cinco en punto .");

            result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("i talked until five o ' clock ."));
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
