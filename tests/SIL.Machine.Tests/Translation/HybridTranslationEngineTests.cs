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
				sourceAnalyzer.AddAnalyses("mi", new WordAnalysis(new[]
				{
					new TestMorpheme("s3", "adj", "my", MorphemeType.Stem),
				}, 0, "adj"));
				var targetGenerator = Substitute.For<IMorphologicalGenerator>();
				var targetMorphemes = new ReadOnlyObservableList<IMorpheme>(new ObservableList<IMorpheme>
				{
					new TestMorpheme("e1", "v", "walk", MorphemeType.Stem),
					new TestMorpheme("e2", "v", "pst", MorphemeType.Affix),
					new TestMorpheme("e3", "adj", "my", MorphemeType.Stem)
				});
				targetGenerator.Morphemes.Returns(targetMorphemes);
				targetGenerator.AddGeneratedWords(
					new WordAnalysis(new[] { targetMorphemes[0], targetMorphemes[1] }, 0, "v"), "walked");
				targetGenerator.AddGeneratedWords(
					new WordAnalysis(new[] { targetMorphemes[2] }, 0, "adj"), "my");
				var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
				ITranslationEngine transferEngine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
				var smtEngine = Substitute.For<IInteractiveTranslationEngine>();

				AddTranslation(smtEngine, "caminé a mi habitación .", "caminé to mi room .",
					new[] { 0, 0.5, 0, 0.5, 0.5 });
				AddTranslation(smtEngine, "hablé con recepción .", "hablé with reception .",
					new[] { 0, 0.5, 0.5, 0.5 });

				Ecm = new ErrorCorrectionModel();
				Engine = new HybridTranslationEngine(smtEngine, transferEngine);
			}

			private static void AddTranslation(IInteractiveTranslationEngine engine, string sourceSegment,
				string targetSegment, double[] confidences)
			{
				string[] sourceSegmentArray = sourceSegment.Split();
				string[] targetSegmentArray = targetSegment.Split();
				TranslationSources[] sources = new TranslationSources[confidences.Length];
				for (int j = 0; j < sources.Length; j++)
					sources[j] = confidences[j] <= 0 ? TranslationSources.None : TranslationSources.Smt;

				var arcs = new List<WordGraphArc>();
				for (int i = 0; i < sourceSegmentArray.Length; i++)
				{
					arcs.Add(new WordGraphArc(i, i + 1, 100, new string[] { targetSegmentArray[i] },
						new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(i, i + 1),
						new TranslationSources[] { sources[i] }, new double[] { confidences[i] }));
				}

				engine.GetWordGraph(Arg.Is<IReadOnlyList<string>>(ss => ss.SequenceEqual(sourceSegmentArray)))
					.Returns(new WordGraph(arcs, new int[] { sourceSegmentArray.Length }));
			}

			public HybridTranslationEngine Engine { get; }
			public ErrorCorrectionModel Ecm { get; }

			public InteractiveTranslator CreateTranslator(string segment)
			{
				return InteractiveTranslator.Create(Ecm, Engine, 1, segment.Split());
			}

			protected override void DisposeManagedResources()
			{
				Engine.Dispose();
			}
		}

		[Test]
		public void InteractiveTranslator_TransferredWord()
		{
			using (var env = new TestEnvironment())
			{
				InteractiveTranslator translator = env.CreateTranslator("caminé a mi habitación .");
				TranslationResult result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("walked to my room .".Split()));
				Assert.That(result.WordSources[0], Is.EqualTo(TranslationSources.Transfer));
				Assert.That(result.WordSources[2], Is.EqualTo(TranslationSources.Transfer));
			}
		}

		[Test]
		public void InteractiveTranslator_UnknownWord()
		{
			using (var env = new TestEnvironment())
			{
				InteractiveTranslator translator = env.CreateTranslator("hablé con recepción .");
				TranslationResult result = translator.CurrentResults[0];
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
				Assert.That(result.WordSources[0], Is.EqualTo(TranslationSources.None));
			}
		}
	}
}
