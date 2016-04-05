using NUnit.Framework;

namespace SIL.Machine.Translation.Tests
{
	[TestFixture]
	public class ThotSmtSessionTests
	{
		[Test]
		public void Translate_TranslationCorrect()
		{
			using (var decoder = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtSession session = decoder.StartSession())
			{
				SmtResult result = session.Translate("voy a marcharme hoy por la tarde .".Split());
				Assert.That(result.Translation, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void TranslateInteractively_TranslationCorrect()
		{
			using (var decoder = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtSession session = decoder.StartSession())
			{
				SmtResult result = session.TranslateInteractively("me marcho hoy por la tarde .".Split());
				Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void AddStringToPrefix_TranslationCorrect()
		{
			using (var decoder = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtSession session = decoder.StartSession())
			{
				SmtResult result = session.TranslateInteractively("me marcho hoy por la tarde .".Split());
				Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.AddToPrefix("I am".Split(), false);
				Assert.That(result.Translation, Is.EqualTo("I am leave today in the afternoon .".Split()));
				result = session.AddToPrefix("leaving".Split(), false);
				Assert.That(result.Translation, Is.EqualTo("I am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void SetPrefix_TranslationCorrect()
		{
			using (var decoder = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtSession session = decoder.StartSession())
			{
				SmtResult result = session.TranslateInteractively("me marcho hoy por la tarde .".Split());
				Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.AddToPrefix("I am".Split(), false);
				Assert.That(result.Translation, Is.EqualTo("I am leave today in the afternoon .".Split()));
				result = session.SetPrefix("I".Split(), false);
				Assert.That(result.Translation, Is.EqualTo("I leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Train_TranslationCorrect()
		{
			using (var decoder = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtSession session = decoder.StartSession())
			{
				SmtResult result = session.Translate("esto es una prueba .".Split());
				Assert.That(result.Translation, Is.EqualTo("esto is a prueba .".Split()));
				session.Train("esto es una prueba .".Split(), "this is a test .".Split());
				result = session.Translate("esto es una prueba .".Split());
				Assert.That(result.Translation, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public void Translate_WordConfidencesCorrect()
		{
			using (var decoder = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (ThotSmtSession session = decoder.StartSession())
			{
				SmtResult result = session.Translate("esto es una prueba .".Split());
				Assert.That(result.WordConfidences[0], Is.EqualTo(0.0).Within(0.01));
				Assert.That(result.WordConfidences[1], Is.EqualTo(0.65).Within(0.01));
				Assert.That(result.WordConfidences[2], Is.EqualTo(0.70).Within(0.01));
				Assert.That(result.WordConfidences[3], Is.EqualTo(0.0).Within(0.01));
			}
		}
	}
}
