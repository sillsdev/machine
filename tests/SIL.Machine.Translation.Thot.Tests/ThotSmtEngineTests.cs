using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotSmtEngineTests
	{
		[Test]
		public void Translate_TargetSegment_Hmm()
		{
			using (ThotSmtModel smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.Translate("voy a marcharme hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Translate_NBestLessThanN_Hmm()
		{
			using (ThotSmtModel smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				IEnumerable<TranslationResult> results = engine.Translate(3,
					"voy a marcharme hoy por la tarde .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment),
					Is.EqualTo(new[] { "i am leaving today in the afternoon .".Split() }));
			}
		}

		[Test]
		public void Translate_NBest_Hmm()
		{
			using (ThotSmtModel smtModel = CreateHmmModel())
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
		public void TrainSegment_Segment_Hmm()
		{
			using (ThotSmtModel smtModel = CreateHmmModel())
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
		public void GetBestPhraseAlignment_SegmentPair_Hmm()
		{
			using (ThotSmtModel smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.GetBestPhraseAlignment("esto es una prueba .".Split(),
					"this is a test .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public void GetWordGraph_EmptySegment_Hmm()
		{
			using (ThotSmtModel smtModel = CreateHmmModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				WordGraph wordGraph = engine.GetWordGraph(new string[0]);
				Assert.That(wordGraph.IsEmpty, Is.True);
			}
		}

		[Test]
		public void Translate_TargetSegment_FastAlign()
		{
			using (ThotSmtModel smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.Translate("voy a marcharme hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Translate_NBestLessThanN_FastAlign()
		{
			using (ThotSmtModel smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				IEnumerable<TranslationResult> results = engine.Translate(3,
					"voy a marcharme hoy por la tarde .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment),
					Is.EqualTo(new[] { "i am leaving today in the afternoon .".Split() }));
			}
		}

		[Test]
		public void Translate_NBest_FastAlign()
		{
			using (ThotSmtModel smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				IEnumerable<TranslationResult> results = engine.Translate(2, "hablé hasta cinco en punto .".Split());
				Assert.That(results.Select(tr => tr.TargetSegment),
					Is.EqualTo(new[]
						{
							"hablé until five o ' clock .".Split(),
							"hablé until five o ' clock , please .".Split()
						}));
			}
		}

		[Test]
		public void TrainSegment_Segment_FastAlign()
		{
			using (ThotSmtModel smtModel = CreateFastAlignModel())
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
		public void GetBestPhraseAlignment_SegmentPair_FastAlign()
		{
			using (ThotSmtModel smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				TranslationResult result = engine.GetBestPhraseAlignment("esto es una prueba .".Split(),
					"this is a test .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public void GetWordGraph_EmptySegment_FastAlign()
		{
			using (ThotSmtModel smtModel = CreateFastAlignModel())
			using (ThotSmtEngine engine = smtModel.CreateEngine())
			{
				WordGraph wordGraph = engine.GetWordGraph(new string[0]);
				Assert.That(wordGraph.IsEmpty, Is.True);
			}
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
