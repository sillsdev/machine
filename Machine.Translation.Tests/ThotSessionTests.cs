using System;
using System.IO;
using NUnit.Framework;

namespace SIL.Machine.Translation.Tests
{
	[TestFixture]
	public class ThotSessionTests
	{
		private static string ConfigFileName
		{
			get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "data", "toy_corpus", "toy_corpus.cfg"); }
		}

		[Test]
		public void Translate_TranslationCorrect()
		{
			using (var decoder = new ThotDecoder(ConfigFileName))
			using (ThotSession session = decoder.StartSession())
			{
				ThotResult result = session.Translate("voy a marcharme hoy por la tarde .");
				Assert.That(result.Translation, Is.EqualTo("i am leaving today in the afternoon ."));
			}
		}

		[Test]
		public void TranslateInteractively_TranslationCorrect()
		{
			using (var decoder = new ThotDecoder(ConfigFileName))
			using (ThotSession session = decoder.StartSession())
			{
				ThotResult result = session.TranslateInteractively("me marcho hoy por la tarde .");
				Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
			}
		}

		[Test]
		public void AddStringToPrefix_TranslationCorrect()
		{
			using (var decoder = new ThotDecoder(ConfigFileName))
			using (ThotSession session = decoder.StartSession())
			{
				ThotResult result = session.TranslateInteractively("me marcho hoy por la tarde .");
				Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
				result = session.AddStringToPrefix("I am ");
				Assert.That(result.Translation, Is.EqualTo("I am leave today in the afternoon ."));
				result = session.AddStringToPrefix("leaving ");
				Assert.That(result.Translation, Is.EqualTo("I am leaving today in the afternoon ."));
			}
		}

		[Test]
		public void SetPrefix_TranslationCorrect()
		{
			using (var decoder = new ThotDecoder(ConfigFileName))
			using (ThotSession session = decoder.StartSession())
			{
				ThotResult result = session.TranslateInteractively("me marcho hoy por la tarde .");
				Assert.That(result.Translation, Is.EqualTo("i leave today in the afternoon ."));
				result = session.AddStringToPrefix("I am ");
				Assert.That(result.Translation, Is.EqualTo("I am leave today in the afternoon ."));
				result = session.SetPrefix("I ");
				Assert.That(result.Translation, Is.EqualTo("I leave today in the afternoon ."));
			}
		}

		[Test]
		public void Train_TranslationCorrect()
		{
			using (var decoder = new ThotDecoder(ConfigFileName))
			using (ThotSession session = decoder.StartSession())
			{
				ThotResult result = session.Translate("esto es una prueba .");
				Assert.That(result.Translation, Is.EqualTo("esto is a prueba ."));
				session.Train("esto es una prueba .", "this is a test .");
				result = session.Translate("esto es una prueba .");
				Assert.That(result.Translation, Is.EqualTo("this is a test ."));
			}
		}

		[Test]
		public void Translate_WordConfidencesCorrect()
		{
			using (var decoder = new ThotDecoder(ConfigFileName))
			using (ThotSession session = decoder.StartSession())
			{
				ThotResult result = session.Translate("esto es una prueba .");
				Assert.That(result.WordConfidences[0], Is.EqualTo(0.0).Within(0.01));
				Assert.That(result.WordConfidences[1], Is.EqualTo(0.65).Within(0.01));
				Assert.That(result.WordConfidences[2], Is.EqualTo(0.70).Within(0.01));
				Assert.That(result.WordConfidences[3], Is.EqualTo(0.0).Within(0.01));
			}
		}
	}
}
