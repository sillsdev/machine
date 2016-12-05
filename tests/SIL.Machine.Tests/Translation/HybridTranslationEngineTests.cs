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
	public class HybridTranslationEngineTests
	{
		private class TestEnvironment : DisposableBase
		{
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

				var alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
				AddWordPair(alignment, 0, 0, TranslationSources.None);
				AddWordPair(alignment, 1, 1, TranslationSources.Smt);
				AddWordPair(alignment, 2, 2, TranslationSources.Smt);
				AddWordPair(alignment, 3, 3, TranslationSources.Smt);
				AddWordPair(alignment, 4, 4, TranslationSources.Smt);
				AddTranslation(smtEngine, "caminé a mi habitación .", "caminé to my room .", new[] {0, 0.5, 0.5, 0.5, 0.5}, alignment);

				alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
				AddWordPair(alignment, 0, 0, TranslationSources.None);
				AddWordPair(alignment, 1, 1, TranslationSources.Smt);
				AddWordPair(alignment, 2, 2, TranslationSources.Smt);
				AddWordPair(alignment, 3, 3, TranslationSources.Smt);
				AddTranslation(smtEngine, "hablé con recepción .", "hablé with reception .", new[] {0, 0.5, 0.5, 0.5}, alignment);

				Engine = new HybridTranslationEngine(smtEngine, transferEngine);
			}

			private static void AddTranslation(IInteractiveSmtEngine engine, string sourceSegment, string targetSegment, double[] confidences,
				Dictionary<Tuple<int, int>, AlignedWordPair> alignment)
			{
				string[] sourceSegmentArray = sourceSegment.Split();
				string[] targetSegmentArray = targetSegment.Split();
				AlignedWordPair[,] alignmentMatrix = new AlignedWordPair[sourceSegmentArray.Length, targetSegmentArray.Length];
				foreach (KeyValuePair<Tuple<int, int>, AlignedWordPair> kvp in alignment)
					alignmentMatrix[kvp.Key.Item1, kvp.Key.Item2] = kvp.Value;

				var smtSession = Substitute.For<IInteractiveTranslationSession>();
				smtSession.SourceSegment.Returns(sourceSegmentArray);
				smtSession.CurrentTranslationResult.Returns(new TranslationResult(sourceSegmentArray, targetSegmentArray,
					confidences, alignmentMatrix));

				engine.TranslateInteractively(Arg.Is<IEnumerable<string>>(ss => ss.SequenceEqual(sourceSegmentArray))).Returns(smtSession);
			}

			private static void AddWordPair(Dictionary<Tuple<int, int>, AlignedWordPair> alignment, int i, int j, TranslationSources sources)
			{
				alignment[Tuple.Create(i, j)] = new AlignedWordPair(i, j, sources);
			}

			public HybridTranslationEngine Engine { get; }

			protected override void DisposeManagedResources()
			{
				Engine.Dispose();
			}
		}

		[Test]
		public void TranslateInteractively_TransferredWord_CorrectTranslation()
		{
			using (var env = new TestEnvironment())
			using (IInteractiveTranslationSession session = env.Engine.TranslateInteractively("caminé a mi habitación .".Split()))
			{
				TranslationResult result = session.CurrentTranslationResult;
				Assert.That(result.TargetSegment, Is.EqualTo("walked to my room .".Split()));
			}
		}

		[Test]
		public void TranslateInteractively_UnknownWord_PartialTranslation()
		{
			using (var env = new TestEnvironment())
			using (IInteractiveTranslationSession session = env.Engine.TranslateInteractively("hablé con recepción .".Split()))
			{
				TranslationResult result = session.CurrentTranslationResult;
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
			}
		}
	}
}
