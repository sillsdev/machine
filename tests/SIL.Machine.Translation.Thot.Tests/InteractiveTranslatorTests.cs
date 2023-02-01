using System.Linq;
using System.Threading.Tasks;
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
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(
                ecm,
                smtModel,
                "me marcho hoy por la tarde .".Split()
            );

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
        }

        [Test]
        public async Task SetPrefix_AddWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(
                ecm,
                smtModel,
                "me marcho hoy por la tarde .".Split()
            );

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
            translator.SetPrefix("i am".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
            translator.SetPrefix("i am leaving".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
        }

        [Test]
        public async Task SetPrefix_MissingWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(ecm, smtModel, "caminé a mi habitación .".Split());

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
            translator.SetPrefix("i walked".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
        }

        [Test]
        public async Task SetPrefix_RemoveWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(
                ecm,
                smtModel,
                "me marcho hoy por la tarde .".Split()
            );

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
            translator.SetPrefix("i am".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
            translator.SetPrefix("i".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
        }

        [Test]
        public async Task ApproveAsync_TwoSegmentsUnknownWord_Hmm()
        {
            using ThotSmtModel smtModel = CreateHmmModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(ecm, smtModel, "hablé con recepción .".Split());

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
            translator.SetPrefix("i talked".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
            translator.SetPrefix("i talked with reception .".Split(), true);
            await translator.ApproveAsync(false);

            translator = await InteractiveTranslator.CreateAsync(ecm, smtModel, "hablé hasta cinco en punto .".Split());

            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i talked until five o ' clock .".Split()));
        }

        [Test]
        public async Task TargetSegment_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(
                ecm,
                smtModel,
                "me marcho hoy por la tarde .".Split()
            );

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
        }

        [Test]
        public async Task SetPrefix_AddWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(
                ecm,
                smtModel,
                "me marcho hoy por la tarde .".Split()
            );

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
            translator.SetPrefix("i am".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
            translator.SetPrefix("i am leaving".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
        }

        [Test]
        public async Task SetPrefix_MissingWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(ecm, smtModel, "caminé a mi habitación .".Split());

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
            translator.SetPrefix("i walked".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
        }

        [Test]
        public async Task SetPrefix_RemoveWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(
                ecm,
                smtModel,
                "me marcho hoy por la tarde .".Split()
            );

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
            translator.SetPrefix("i am".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
            translator.SetPrefix("i".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
        }

        [Test]
        public async Task ApproveAsync_TwoSegmentsUnknownWord_FastAlign()
        {
            using ThotSmtModel smtModel = CreateFastAlignModel();
            var ecm = new ErrorCorrectionModel();
            var translator = await InteractiveTranslator.CreateAsync(ecm, smtModel, "hablé con recepción .".Split());

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
            translator.SetPrefix("i talked".Split(), true);
            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
            translator.SetPrefix("i talked with reception .".Split(), true);
            await translator.ApproveAsync(false);

            translator = await InteractiveTranslator.CreateAsync(ecm, smtModel, "hablé hasta cinco en punto .".Split());

            result = translator.GetCurrentResults().First();
            Assert.That(result.TargetSegment, Is.EqualTo("i talked until five o ' clock .".Split()));
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
