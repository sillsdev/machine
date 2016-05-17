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
			private readonly IImtSession _session;

			public TestEnvironment()
			{
				var sourceAnalyzer = Substitute.For<IMorphologicalAnalyzer>();
				sourceAnalyzer.AddAnalyses("caminé", new WordAnalysis(new[]
				{
					new Morpheme("s1", "v", "walk", MorphemeType.Stem),
					new Morpheme("s2", "v", "pst", MorphemeType.Affix)
				}, 0, "v"));
				var targetGenerator = Substitute.For<IMorphologicalGenerator>();
				var targetMorphemes = new ReadOnlyObservableList<Morpheme>(new ObservableList<Morpheme>
				{
					new Morpheme("e1", "v", "walk", MorphemeType.Stem),
					new Morpheme("e2", "v", "pst", MorphemeType.Affix)
				});
				targetGenerator.Morphemes.Returns(targetMorphemes);
				targetGenerator.AddGeneratedWords(new WordAnalysis(new[] {targetMorphemes[0], targetMorphemes[1]}, 0, "v"), "walked");
				var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
				var transferEngine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
				var smtEngine = Substitute.For<ISmtEngine>();
				var smtSession = Substitute.For<ISmtSession>();

				var alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
				AddWordPair(alignment, 0, 0, 0, TranslationSources.None);
				AddWordPair(alignment, 1, 1, 0.5, TranslationSources.Smt);
				AddWordPair(alignment, 2, 2, 0.5, TranslationSources.Smt);
				AddWordPair(alignment, 3, 3, 0.5, TranslationSources.Smt);
				AddWordPair(alignment, 4, 4, 0.5, TranslationSources.Smt);
				AddTranslation(smtSession, "caminé a mi habitación .", "caminé to my room .", alignment);

				alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
				AddWordPair(alignment, 0, 0, 0, TranslationSources.None);
				AddWordPair(alignment, 1, 1, 0.5, TranslationSources.Smt);
				AddWordPair(alignment, 2, 2, 0.5, TranslationSources.Smt);
				AddWordPair(alignment, 3, 3, 0.5, TranslationSources.Smt);
				AddTranslation(smtSession, "hablé con recepción .", "hablé with reception .", alignment);

				smtEngine.StartSession().Returns(smtSession);
				_engine = new HybridTranslationEngine(smtEngine, transferEngine);
				_session = _engine.StartSession();
			}

			private static void AddTranslation(ISmtSession session, string sourceSegment, string targetSegment, Dictionary<Tuple<int, int>, AlignedWordPair> alignment)
			{
				string[] sourceSegmentArray = sourceSegment.Split();
				string[] targetSegmentArray = targetSegment.Split();
				AlignedWordPair[,] alignmentMatrix = new AlignedWordPair[sourceSegmentArray.Length, targetSegmentArray.Length];
				var confidences = new double[targetSegmentArray.Length];
				for (int i = 0; i < confidences.Length; i++)
					confidences[i] = -1;
				foreach (KeyValuePair<Tuple<int, int>, AlignedWordPair> kvp in alignment)
				{
					alignmentMatrix[kvp.Key.Item1, kvp.Key.Item2] = kvp.Value;
					confidences[kvp.Key.Item2] = confidences[kvp.Key.Item2] < 0 ? kvp.Value.Confidence : (confidences[kvp.Key.Item2] + kvp.Value.Confidence) / 2;
				}
				session.TranslateInteractively(Arg.Is<IEnumerable<string>>(ss => ss.SequenceEqual(sourceSegmentArray))).Returns(new TranslationResult(sourceSegmentArray, targetSegmentArray,
					confidences, alignmentMatrix));
			}

			private static void AddWordPair(Dictionary<Tuple<int, int>, AlignedWordPair> alignment, int i, int j, double confidence, TranslationSources sources)
			{
				alignment[Tuple.Create(i, j)] = new AlignedWordPair(i, j, confidence, sources);
			}

			public IImtSession Session
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
