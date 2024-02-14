using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;

namespace SIL.Machine.Translation;

[TestFixture]
public class HybridTranslationEngineTests
{
    [Test]
    public async Task InteractiveTranslator_TransferredWord()
    {
        using var env = new TestEnvironment();
        InteractiveTranslator translator = await env.CreateTranslatorAsync("caminé a mi habitación .");
        TranslationResult result = translator.GetCurrentResults().First();
        Assert.That(result.Translation, Is.EqualTo("walked to my room ."));
        Assert.That(result.Sources[0], Is.EqualTo(TranslationSources.Transfer));
        Assert.That(result.Sources[2], Is.EqualTo(TranslationSources.Transfer));
    }

    [Test]
    public async Task InteractiveTranslator_UnknownWord()
    {
        using var env = new TestEnvironment();
        InteractiveTranslator translator = await env.CreateTranslatorAsync("hablé con recepción .");
        TranslationResult result = translator.GetCurrentResults().First();
        Assert.That(result.Translation, Is.EqualTo("hablé with reception ."));
        Assert.That(result.Sources[0], Is.EqualTo(TranslationSources.None));
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly InteractiveTranslatorFactory _factory;

        public TestEnvironment()
        {
            var sourceAnalyzer = Substitute.For<IMorphologicalAnalyzer>();
            sourceAnalyzer.AddAnalyses(
                "caminé",
                new WordAnalysis(
                    new[]
                    {
                        new TestMorpheme("s1", "v", "walk", MorphemeType.Stem),
                        new TestMorpheme("s2", "v", "pst", MorphemeType.Affix)
                    },
                    0,
                    "v"
                )
            );
            sourceAnalyzer.AddAnalyses(
                "mi",
                new WordAnalysis(new[] { new TestMorpheme("s3", "adj", "my", MorphemeType.Stem), }, 0, "adj")
            );
            var targetGenerator = Substitute.For<IMorphologicalGenerator>();
            var targetMorphemes = new ReadOnlyObservableList<IMorpheme>(
                new ObservableList<IMorpheme>
                {
                    new TestMorpheme("e1", "v", "walk", MorphemeType.Stem),
                    new TestMorpheme("e2", "v", "pst", MorphemeType.Affix),
                    new TestMorpheme("e3", "adj", "my", MorphemeType.Stem)
                }
            );
            targetGenerator.Morphemes.Returns(targetMorphemes);
            targetGenerator.AddGeneratedWords(
                new WordAnalysis(new[] { targetMorphemes[0], targetMorphemes[1] }, 0, "v"),
                "walked"
            );
            targetGenerator.AddGeneratedWords(new WordAnalysis(new[] { targetMorphemes[2] }, 0, "adj"), "my");
            var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
            ITranslationEngine transferEngine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
            var smtEngine = Substitute.For<IInteractiveTranslationEngine>();

            AddTranslation(smtEngine, "caminé a mi habitación .", "caminé to mi room .", new[] { 0, 0.5, 0, 0.5, 0.5 });
            AddTranslation(smtEngine, "hablé con recepción .", "hablé with reception .", new[] { 0, 0.5, 0.5, 0.5 });

            Engine = new HybridTranslationEngine(smtEngine, transferEngine);
            _factory = new InteractiveTranslatorFactory(Engine);
        }

        private static void AddTranslation(
            IInteractiveTranslationEngine engine,
            string sourceSegment,
            string targetSegment,
            double[] confidences
        )
        {
            string[] sourceTokens = WhitespaceTokenizer.Instance.Tokenize(sourceSegment).ToArray();
            string[] targetSegmentArray = WhitespaceTokenizer.Instance.Tokenize(targetSegment).ToArray();
            TranslationSources[] sources = new TranslationSources[confidences.Length];
            for (int j = 0; j < sources.Length; j++)
                sources[j] = confidences[j] <= 0 ? TranslationSources.None : TranslationSources.Smt;

            var arcs = new List<WordGraphArc>();
            for (int i = 0; i < sourceTokens.Length; i++)
            {
                arcs.Add(
                    new WordGraphArc(
                        i,
                        i + 1,
                        100,
                        new string[] { targetSegmentArray[i] },
                        new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                        Range<int>.Create(i, i + 1),
                        new TranslationSources[] { sources[i] },
                        new double[] { confidences[i] }
                    )
                );
            }

            engine
                .GetWordGraphAsync(sourceSegment)
                .Returns(Task.FromResult(new WordGraph(sourceTokens, arcs, new int[] { sourceTokens.Length })));
        }

        public HybridTranslationEngine Engine { get; }

        public Task<InteractiveTranslator> CreateTranslatorAsync(string segment)
        {
            return _factory.CreateAsync(segment);
        }

        protected override void DisposeManagedResources()
        {
            Engine.Dispose();
        }
    }
}
