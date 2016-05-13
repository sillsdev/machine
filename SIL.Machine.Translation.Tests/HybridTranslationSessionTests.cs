using System.Linq;
using NSubstitute;
using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Tests
{
	[TestFixture]
	public class HybridTranslationSessionTests
	{
		private class TestEnvironment : DisposableBase
		{
			private readonly HybridTranslationEngine _engine;

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
				var transferEngine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
				var smtEngine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName);
				_engine = new HybridTranslationEngine(smtEngine, transferEngine);
			}

			public HybridTranslationEngine Engine
			{
				get { return _engine; }
			}

			protected override void DisposeManagedResources()
			{
				_engine.Dispose();
			}
		}

		[Test]
		public void TranslateInteractively_EmptyPrefixUsesTransferEngine_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			using (IImtSession session = env.Engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("caminé a mi habitación .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("walked to my room .".Split()));
			}
		}

		[Test]
		public void AddToPrefix_PrefixUpdatedUsesTransferEngine_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			using (IImtSession session = env.Engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("caminé a mi habitación .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("walked to my room .".Split()));
				result = session.AddToPrefix("i", false);
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void Approve_TwoSegmentsUsesTransferEngineMissingWord_LearnsMissingWord()
		{
			using (var env = new TestEnvironment())
			using (IImtSession session = env.Engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("caminé a mi habitación .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("walked to my room .".Split()));
				Assert.That(result.GetTargetWordPairs(0).First().Sources, Is.EqualTo(TranslationSources.Transfer));
				result = session.AddToPrefix("i", false);
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
				session.AddToPrefix(new[] {"walked", "to", "my", "room", "."}, false);
				session.Approve();

				result = session.TranslateInteractively("caminé a la montaña .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("walked to the mountain .".Split()));
				Assert.That(result.GetTargetWordPairs(0).First().Sources, Is.EqualTo(TranslationSources.Smt | TranslationSources.Transfer));
			}
		}

		[Test]
		public void TranslateInteractively_UnknownWordEmptyPrefix_PartialTranslation()
		{
			using (var env = new TestEnvironment())
			using (IImtSession session = env.Engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("hablé con recepción .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
			}
		}

		[Test]
		public void AddToPrefix_UnknownWordPrefixUpdated_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			using (IImtSession session = env.Engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("hablé con recepción .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
				result = session.AddToPrefix(new[] {"i", "talked"}, false);
				Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
			}
		}

		[Test]
		public void Approve_TwoSegmentsUnknownWord_LearnsUnknownWord()
		{
			using (var env = new TestEnvironment())
			using (IImtSession session = env.Engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("hablé con recepción .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
				result = session.AddToPrefix(new[] {"i", "talked"}, false);
				Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
				session.AddToPrefix(new[] {"with", "reception", "."}, false);
				session.Approve();

				result = session.TranslateInteractively("hablé hasta cinco en punto .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("talked until five o ' clock .".Split()));
			}
		}
	}
}
