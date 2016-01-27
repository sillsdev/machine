using System;
using System.Collections.Generic;
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

		private static IEnumerable<string> Split(string segment)
		{
			return segment.Split(' ');
		}

		[Test]
		public void Translate_TranslationCorrect()
		{
			using (var decoder = new SmtEngine(ConfigFileName))
			using (SmtSession session = decoder.StartSession())
			{
				SmtResult result = session.Translate(Split("voy a marcharme hoy por la tarde ."));
				Assert.That(result.Translation, Is.EqualTo(Split("i am leaving today in the afternoon .")));
			}
		}

		[Test]
		public void TranslateInteractively_TranslationCorrect()
		{
			using (var decoder = new SmtEngine(ConfigFileName))
			using (SmtSession session = decoder.StartSession())
			{
				SmtResult result = session.TranslateInteractively(Split("me marcho hoy por la tarde ."));
				Assert.That(result.Translation, Is.EqualTo(Split("i leave today in the afternoon .")));
			}
		}

		[Test]
		public void AddStringToPrefix_TranslationCorrect()
		{
			using (var decoder = new SmtEngine(ConfigFileName))
			using (SmtSession session = decoder.StartSession())
			{
				SmtResult result = session.TranslateInteractively(Split("me marcho hoy por la tarde ."));
				Assert.That(result.Translation, Is.EqualTo(Split("i leave today in the afternoon .")));
				result = session.AddStringToPrefix(Split("I am"));
				Assert.That(result.Translation, Is.EqualTo(Split("I am leave today in the afternoon .")));
				result = session.AddStringToPrefix(Split("leaving"));
				Assert.That(result.Translation, Is.EqualTo(Split("I am leaving today in the afternoon .")));
			}
		}

		[Test]
		public void SetPrefix_TranslationCorrect()
		{
			using (var decoder = new SmtEngine(ConfigFileName))
			using (SmtSession session = decoder.StartSession())
			{
				SmtResult result = session.TranslateInteractively(Split("me marcho hoy por la tarde ."));
				Assert.That(result.Translation, Is.EqualTo(Split("i leave today in the afternoon .")));
				result = session.AddStringToPrefix(Split("I am"));
				Assert.That(result.Translation, Is.EqualTo(Split("I am leave today in the afternoon .")));
				result = session.SetPrefix(Split("I"));
				Assert.That(result.Translation, Is.EqualTo(Split("I leave today in the afternoon .")));
			}
		}

		[Test]
		public void Train_TranslationCorrect()
		{
			using (var decoder = new SmtEngine(ConfigFileName))
			using (SmtSession session = decoder.StartSession())
			{
				SmtResult result = session.Translate(Split("esto es una prueba ."));
				Assert.That(result.Translation, Is.EqualTo(Split("esto is a prueba .")));
				session.Train(Split("esto es una prueba ."), Split("this is a test ."));
				result = session.Translate(Split("esto es una prueba ."));
				Assert.That(result.Translation, Is.EqualTo(Split("this is a test .")));
			}
		}

		[Test]
		public void Translate_WordConfidencesCorrect()
		{
			using (var decoder = new SmtEngine(ConfigFileName))
			using (SmtSession session = decoder.StartSession())
			{
				SmtResult result = session.Translate(Split("esto es una prueba ."));
				Assert.That(result.WordConfidences[0], Is.EqualTo(0.0).Within(0.01));
				Assert.That(result.WordConfidences[1], Is.EqualTo(0.65).Within(0.01));
				Assert.That(result.WordConfidences[2], Is.EqualTo(0.70).Within(0.01));
				Assert.That(result.WordConfidences[3], Is.EqualTo(0.0).Within(0.01));
			}
		}
	}
}
