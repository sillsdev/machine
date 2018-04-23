using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
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
				targetGenerator.AddGeneratedWords(
					new WordAnalysis(new[] { targetMorphemes[0], targetMorphemes[1] }, 0, "v"), "walked");
				var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
				ITranslationEngine transferEngine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
				var smtEngine = Substitute.For<IInteractiveSmtEngine>();

				var alignment = new WordAlignmentMatrix(5, 5)
				{
					[0, 0] = AlignmentType.Aligned,
					[1, 1] = AlignmentType.Aligned,
					[2, 2] = AlignmentType.Aligned,
					[3, 3] = AlignmentType.Aligned,
					[4, 4] = AlignmentType.Aligned
				};
				AddTranslation(smtEngine, "caminé a mi habitación .", "caminé to my room .",
					new[] { 0, 0.5, 0.5, 0.5, 0.5 }, alignment);

				alignment = new WordAlignmentMatrix(4, 4)
				{
					[0, 0] = AlignmentType.Aligned,
					[1, 1] = AlignmentType.Aligned,
					[2, 2] = AlignmentType.Aligned,
					[3, 3] = AlignmentType.Aligned
				};
				AddTranslation(smtEngine, "hablé con recepción .", "hablé with reception .", new[] { 0, 0.5, 0.5, 0.5 },
					alignment);

				Engine = new HybridTranslationEngine(smtEngine, transferEngine);
			}

			private static void AddTranslation(IInteractiveSmtEngine engine, string sourceSegment, string targetSegment,
				double[] confidences, WordAlignmentMatrix alignment)
			{
				string[] sourceSegmentArray = sourceSegment.Split();
				string[] targetSegmentArray = targetSegment.Split();
				TranslationSources[] sources = new TranslationSources[confidences.Length];
				for (int j = 0; j < sources.Length; j++)
					sources[j] = confidences[j] <= 0 ? TranslationSources.None : TranslationSources.Smt;
				var smtSession = Substitute.For<IInteractiveTranslationSession>();
				smtSession.SourceSegment.Returns(sourceSegmentArray);
				smtSession.CurrentResults.Returns(new[] { new TranslationResult(sourceSegmentArray, targetSegmentArray,
					confidences, sources, alignment,
					new[] { new Phrase(Range<int>.Create(0, sourceSegmentArray.Length), targetSegmentArray.Length,
						confidences.Min()) }) });

				engine.TranslateInteractively(1,
					Arg.Is<IReadOnlyList<string>>(ss => ss.SequenceEqual(sourceSegmentArray))).Returns(smtSession);
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
			using (IInteractiveTranslationSession session = env.Engine.TranslateInteractively(1,
				"caminé a mi habitación .".Split()))
			{
				TranslationResult result = session.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("walked to my room .".Split()));
			}
		}

		[Test]
		public void TranslateInteractively_UnknownWord_PartialTranslation()
		{
			using (var env = new TestEnvironment())
			using (IInteractiveTranslationSession session = env.Engine.TranslateInteractively(1,
				"hablé con recepción .".Split()))
			{
				TranslationResult result = session.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
			}
		}
	}
}
