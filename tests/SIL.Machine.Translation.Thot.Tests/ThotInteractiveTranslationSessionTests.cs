using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotInteractiveTranslationSessionTests
	{
		[Test]
		public void TranslateInteractively_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			using (IInteractiveTranslationSession session = engine.TranslateInteractively(1,
				"me marcho hoy por la tarde .".Split()))
			{
				TranslationResult result = session.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_AddWord_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			using (IInteractiveTranslationSession session = engine.TranslateInteractively(1,
				"me marcho hoy por la tarde .".Split()))
			{
				TranslationResult result = session.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.SetPrefix("i am".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = session.SetPrefix("i am leaving".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_MissingWord_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			using (IInteractiveTranslationSession session = engine.TranslateInteractively(1,
				"caminé a mi habitación .".Split()))
			{
				TranslationResult result = session.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
				result = session.SetPrefix("i walked".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void SetPrefix_RemoveWord_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			using (IInteractiveTranslationSession session = engine.TranslateInteractively(1,
				"me marcho hoy por la tarde .".Split()))
			{
				TranslationResult result = session.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.SetPrefix("i am".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = session.SetPrefix("i".Split(), true)[0];
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Approve_TwoSegmentsUnknownWord_LearnsUnknownWord()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			{
				using (IInteractiveTranslationSession session = engine.TranslateInteractively(1,
					"hablé con recepción .".Split()))
				{
					TranslationResult result = session.CurrentResults[0];
					Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
					result = session.SetPrefix("i talked".Split(), true)[0];
					Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
					session.SetPrefix("i talked with reception .".Split(), true);
					session.Approve(false);
				}

				using (IInteractiveTranslationSession session = engine.TranslateInteractively(1,
					"hablé hasta cinco en punto .".Split()))
				{
					TranslationResult result = session.CurrentResults[0];
					Assert.That(result.TargetSegment, Is.EqualTo("talked until five o ' clock .".Split()));
				}
			}
		}
	}
}
