using NSubstitute;
using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Tests
{
	[TestFixture]
	public class SegmentTranslatorTests
	{
		private class TestEnvironment : DisposableBase
		{
			private readonly TranslationEngine _engine;

			public TestEnvironment()
			{
				var sourceAnalyzer = Substitute.For<ISourceAnalyzer>();
				sourceAnalyzer.AddAnalyses("caminé", new WordAnalysis(new[]
				{
					new MorphemeInfo("s1", "v", "walk", MorphemeType.Stem),
					new MorphemeInfo("s2", "v", "pst", MorphemeType.Affix)
				}, 0, "v"));
				var targetGenerator = Substitute.For<ITargetGenerator>();
				var targetMorphemes = new ReadOnlyObservableList<MorphemeInfo>(new ObservableList<MorphemeInfo>
				{
					new MorphemeInfo("e1", "v", "walk", MorphemeType.Stem),
					new MorphemeInfo("e2", "v", "pst", MorphemeType.Affix)
				});
				targetGenerator.Morphemes.Returns(targetMorphemes);
				targetGenerator.AddGeneratedWords(new WordAnalysis(new[] {targetMorphemes[0], targetMorphemes[1]}, 0, "v"), "walked");
				var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
				_engine = new TranslationEngine(TestHelpers.ToyCorpusConfigFileName, sourceAnalyzer, transferer, targetGenerator);
			}

			public TranslationEngine Engine
			{
				get { return _engine; }
			}

			protected override void DisposeManagedResources()
			{
				_engine.Dispose();
			}
		}

		[Test]
		public void CurrentTranslation_EmptyPrefixUsesTransferEngine_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			{
				SegmentTranslator translator = env.Engine.StartSegmentTranslation("caminé a mi habitación .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("walked to my room .".Split()));
			}
		}

		[Test]
		public void CurrentTranslation_PrefixUpdatedUsesTransferEngine_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			{
				SegmentTranslator translator = env.Engine.StartSegmentTranslation("caminé a mi habitación .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("walked to my room .".Split()));
				translator.Prefix.Add("i");
				Assert.That(translator.CurrentTranslation, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void CurrentTranslation_TwoSegmentsUsesTransferEngineMissingWord_LearnsMissingWord()
		{
			using (var env = new TestEnvironment())
			{
				SegmentTranslator translator = env.Engine.StartSegmentTranslation("caminé a mi habitación .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("walked to my room .".Split()));
				translator.Prefix.Add("i");
				Assert.That(translator.CurrentTranslation, Is.EqualTo("i walked to my room .".Split()));
				translator.Approve();

				translator = env.Engine.StartSegmentTranslation("caminé a la montaña .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("i walked to the mountain .".Split()));
			}
		}

		[Test]
		public void CurrentTranslation_UnknownWordEmptyPrefix_PartialTranslation()
		{
			using (var env = new TestEnvironment())
			{
				SegmentTranslator translator = env.Engine.StartSegmentTranslation("hablé con recepción .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("hablé with reception .".Split()));
			}
		}

		[Test]
		public void CurrentTranslation_UnknownWordPrefixUpdated_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			{
				SegmentTranslator translator = env.Engine.StartSegmentTranslation("hablé con recepción .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("hablé with reception .".Split()));
				translator.Prefix.Add("i");
				translator.Prefix.Add("talked");
				Assert.That(translator.CurrentTranslation, Is.EqualTo("i talked with reception .".Split()));
			}
		}

		[Test]
		public void CurrentTranslation_TwoSegmentsUnknownWord_LearnsNewWord()
		{
			using (var env = new TestEnvironment())
			{
				SegmentTranslator translator = env.Engine.StartSegmentTranslation("hablé con recepción .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("hablé with reception .".Split()));
				translator.Prefix.Add("i");
				translator.Prefix.Add("talked");
				Assert.That(translator.CurrentTranslation, Is.EqualTo("i talked with reception .".Split()));
				translator.Approve();

				translator = env.Engine.StartSegmentTranslation("hablé hasta cinco en punto .".Split());
				Assert.That(translator.CurrentTranslation, Is.EqualTo("i talked until five o ' clock .".Split()));
			}
		}
	}
}
