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
			using (IInteractiveTranslationSession session = engine.TranslateInteractively("me marcho hoy por la tarde .".Split()))
			{
				TranslationResult result = session.CurrentResult;
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_AddWord_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			using (IInteractiveTranslationSession session = engine.TranslateInteractively("me marcho hoy por la tarde .".Split()))
			{
				TranslationResult result = session.CurrentResult;
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.SetPrefix("i am".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = session.SetPrefix("i am leaving".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_MissingWord_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			using (IInteractiveTranslationSession session = engine.TranslateInteractively("caminé a mi habitación .".Split()))
			{
				TranslationResult result = session.CurrentResult;
				Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
				result = session.SetPrefix("i walked".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void SetPrefix_RemoveWord_TranslationCorrect()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			using (IInteractiveTranslationSession session = engine.TranslateInteractively("me marcho hoy por la tarde .".Split()))
			{
				TranslationResult result = session.CurrentResult;
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.SetPrefix("i am".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = session.SetPrefix("i".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Approve_TwoSegmentsUnknownWord_LearnsUnknownWord()
		{
			using (var smtModel = new ThotSmtModel(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveSmtEngine engine = smtModel.CreateInteractiveEngine())
			{
				using (IInteractiveTranslationSession session = engine.TranslateInteractively("hablé con recepción .".Split()))
				{
					TranslationResult result = session.CurrentResult;
					Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
					result = session.SetPrefix("i talked".Split(), true);
					Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
					session.SetPrefix("i talked with reception .".Split(), true);
					session.Approve();
				}

				using (IInteractiveTranslationSession session = engine.TranslateInteractively("hablé hasta cinco en punto .".Split()))
				{
					TranslationResult result = session.CurrentResult;
					Assert.That(result.TargetSegment, Is.EqualTo("talked until five o ' clock .".Split()));
				}
			}
		}
	}
}
