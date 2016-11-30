using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Morphology;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.Tests.Translation
{
	[TestFixture]
	public class HybridTranslationSessionTests
	{
		private class TestEnvironment : DisposableBase
		{
			private readonly HybridTranslationEngine _engine;
			private readonly IInteractiveTranslationSession _session;

			public TestEnvironment()
			{
				var sourceAnalyzer = Substitute.For<IMorphologicalAnalyzer>();
				sourceAnalyzer.AddAnalyses("caminé", new WordAnalysis(new[]
					{
						new TestMorpheme("s1", "v", "walk", MorphemeType.Stem),
						new TestMorpheme("s2", "v", "pst", MorphemeType.Affix)
					}, 0, "v"));
				var targetGenerator = Substitute.For<IMorphologicalGenerator>();
				var targetMorphemes = new ReadOnlyObservableList<IMorpheme>(new ObservableList<IMorpheme>
					{
						new TestMorpheme("e1", "v", "walk", MorphemeType.Stem),
						new TestMorpheme("e2", "v", "pst", MorphemeType.Affix)
					});
				targetGenerator.Morphemes.Returns(targetMorphemes);
				targetGenerator.AddGeneratedWords(new WordAnalysis(new[] {targetMorphemes[0], targetMorphemes[1]}, 0, "v"), "walked");
				var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
				var transferEngine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
				var smtEngine = Substitute.For<IInteractiveSmtEngine>();
				var smtSession = Substitute.For<IInteractiveTranslationSession>();

				var alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
				AddWordPair(alignment, 0, 0, TranslationSources.None);
				AddWordPair(alignment, 1, 1, TranslationSources.Smt);
				AddWordPair(alignment, 2, 2, TranslationSources.Smt);
				AddWordPair(alignment, 3, 3, TranslationSources.Smt);
				AddWordPair(alignment, 4, 4, TranslationSources.Smt);
				AddTranslation(smtSession, "caminé a mi habitación .", "caminé to my room .", new[] {0, 0.5, 0.5, 0.5, 0.5}, alignment);

				alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
				AddWordPair(alignment, 0, 0, TranslationSources.None);
				AddWordPair(alignment, 1, 1, TranslationSources.Smt);
				AddWordPair(alignment, 2, 2, TranslationSources.Smt);
				AddWordPair(alignment, 3, 3, TranslationSources.Smt);
				AddTranslation(smtSession, "hablé con recepción .", "hablé with reception .", new[] {0, 0.5, 0.5, 0.5}, alignment);

				smtEngine.StartSession().Returns(smtSession);
				_engine = new HybridTranslationEngine(smtEngine, transferEngine);
				_session = _engine.StartSession();
			}

			private static void AddTranslation(IInteractiveTranslationSession session, string sourceSegment, string targetSegment, double[] confidences,
				Dictionary<Tuple<int, int>, AlignedWordPair> alignment)
			{
				string[] sourceSegmentArray = sourceSegment.Split();
				string[] targetSegmentArray = targetSegment.Split();
				AlignedWordPair[,] alignmentMatrix = new AlignedWordPair[sourceSegmentArray.Length, targetSegmentArray.Length];
				foreach (KeyValuePair<Tuple<int, int>, AlignedWordPair> kvp in alignment)
					alignmentMatrix[kvp.Key.Item1, kvp.Key.Item2] = kvp.Value;
				session.TranslateInteractively(Arg.Is<IEnumerable<string>>(ss => ss.SequenceEqual(sourceSegmentArray))).Returns(new TranslationResult(sourceSegmentArray, targetSegmentArray,
					confidences, alignmentMatrix));
			}

			private static void AddWordPair(Dictionary<Tuple<int, int>, AlignedWordPair> alignment, int i, int j, TranslationSources sources)
			{
				alignment[Tuple.Create(i, j)] = new AlignedWordPair(i, j, sources);
			}

			public IInteractiveTranslationSession Session
			{
				get { return _session; }
			}

			protected override void DisposeManagedResources()
			{
				_session.Dispose();
				_engine.Dispose();
			}
		}

		[Test]
		public void TranslateInteractively_TransferredWord_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			{
				TranslationResult result = env.Session.TranslateInteractively("caminé a mi habitación .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("walked to my room .".Split()));
			}
		}

		[Test]
		public void TranslateInteractively_UnknownWord_PartialTranslation()
		{
			using (var env = new TestEnvironment())
			{
				TranslationResult result = env.Session.TranslateInteractively("hablé con recepción .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
			}
		}
	}
}
