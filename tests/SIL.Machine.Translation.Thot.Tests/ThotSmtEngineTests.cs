using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotSmtEngineTests
	{
		[Test]
		public void Translate_Segment()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.Translate("voy a marcharme hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Translate_NBestLessThanN()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				IEnumerable<TranslationResult> results = engine.Translate(3,
					"voy a marcharme hoy por la tarde .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment),
					Is.EqualTo(new[] {"i am leaving today in the afternoon .".Split()}));
			}
		}

		[Test]
		public void Translate_NBest()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				IEnumerable<TranslationResult> results = engine.Translate(2, "hablé hasta cinco en punto .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment),
					Is.EqualTo(new[]
						{
							"hablé until five o ' clock .".Split(),
							"hablé until five o ' clock for".Split()
						}));
			}
		}

		[Test]
		public void TrainSegment_Segment()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.Translate("esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("esto is a prueba .".Split()));
				engine.TrainSegment("esto es una prueba .".Split(), "this is a test .".Split());
				result = engine.Translate("esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public void GetBestPhraseAlignment_SegmentPair()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.GetBestPhraseAlignment("esto es una prueba .".Split(),
					"this is a test .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public void GetWordGraph_EmptySegment()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				WordGraph wordGraph = engine.GetWordGraph(new string[0]);
				Assert.That(wordGraph.IsEmpty, Is.True);
			}
		}

		[Test]
		public void InteractiveTranslator_Segment()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, 1, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void InteractiveTranslator_SetPrefix_AddWord()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, 1, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = translator.SetPrefix("i am".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = translator.SetPrefix("i am leaving".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void InteractiveTranslator_SetPrefix_MissingWord()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateInteractiveEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, 1, "caminé a mi habitación .".Split());

				TranslationResult result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
				result = translator.SetPrefix("i walked".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void InteractiveTranslator_SetPrefix_RemoveWord()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, 1, "me marcho hoy por la tarde .".Split());

				TranslationResult result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = translator.SetPrefix("i am".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = translator.SetPrefix("i".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void InteractiveTranslator_Approve_TwoSegmentsUnknownWord()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				var ecm = new ErrorCorrectionModel();
				var translator = InteractiveTranslator.Create(ecm, engine, 1, "hablé con recepción .".Split());

				TranslationResult result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
				result = translator.SetPrefix("i talked".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
				translator.SetPrefix("i talked with reception .".Split(), true);
				translator.Approve(false);

				translator = InteractiveTranslator.Create(ecm, engine, 1, "hablé hasta cinco en punto .".Split());

				result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("talked until five o ' clock .".Split()));
			}
		}
	}
}
