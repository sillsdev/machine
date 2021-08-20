using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class InteractiveTranslatorTests
	{
		[Test]
		public void TargetSegment_Hmm()
		{
			using (ThotSmtModel<ThotHmmWordAlignmentModel> smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_AddWord_Hmm()
		{
			using (ThotSmtModel<ThotHmmWordAlignmentModel> smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				translator.SetPrefix("i am".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				translator.SetPrefix("i am leaving".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_MissingWord_Hmm()
		{
			using (ThotSmtModel<ThotHmmWordAlignmentModel> smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateInteractiveEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "caminé a mi habitación .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
				translator.SetPrefix("i walked".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void SetPrefix_RemoveWord_Hmm()
		{
			using (ThotSmtModel<ThotHmmWordAlignmentModel> smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				translator.SetPrefix("i am".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				translator.SetPrefix("i".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Approve_TwoSegmentsUnknownWord_Hmm()
		{
			using (ThotSmtModel<ThotHmmWordAlignmentModel> smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "hablé con recepción .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
				translator.SetPrefix("i talked".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
				translator.SetPrefix("i talked with reception .".Split(), true);
				translator.Approve(false);

				translator = InteractiveTranslator.Create(ecm, engine, "hablé hasta cinco en punto .".Split());

				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("talked until five o ' clock .".Split()));
			}
		}

		[Test]
		public void TargetSegment_FastAlign()
		{
			using (ThotSmtModel<ThotFastAlignWordAlignmentModel> smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_AddWord_FastAlign()
		{
			using (ThotSmtModel<ThotFastAlignWordAlignmentModel> smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				translator.SetPrefix("i am".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				translator.SetPrefix("i am leaving".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_MissingWord_FastAlign()
		{
			using (ThotSmtModel<ThotFastAlignWordAlignmentModel> smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateInteractiveEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "caminé a mi habitación .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
				translator.SetPrefix("i walked".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void SetPrefix_RemoveWord_FastAlign()
		{
			using (ThotSmtModel<ThotFastAlignWordAlignmentModel> smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				translator.SetPrefix("i am".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				translator.SetPrefix("i".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Approve_TwoSegmentsUnknownWord_FastAlign()
		{
			using (ThotSmtModel<ThotFastAlignWordAlignmentModel> smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, "hablé con recepción .".Split());

				TranslationResult result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
				translator.SetPrefix("i talked".Split(), true);
				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
				translator.SetPrefix("i talked with reception .".Split(), true);
				translator.Approve(false);

				translator = InteractiveTranslator.Create(ecm, engine, "hablé hasta cinco en punto .".Split());

				result = translator.GetCurrentResults().First();
				Assert.That(result.TargetSegment, Is.EqualTo("i talked until five o ' clock .".Split()));
			}
		}

		private static ThotSmtModel<ThotHmmWordAlignmentModel> CreateHmmModel()
		{
			return new ThotSmtModel<ThotHmmWordAlignmentModel>(TestHelpers.ToyCorpusHmmConfigFileName);
		}

		private static ThotSmtModel<ThotFastAlignWordAlignmentModel> CreateFastAlignModel()
		{
			return new ThotSmtModel<ThotFastAlignWordAlignmentModel>(TestHelpers.ToyCorpusFastAlignConfigFileName);
		}
	}
}
