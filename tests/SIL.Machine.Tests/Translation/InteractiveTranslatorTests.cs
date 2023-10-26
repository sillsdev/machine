using System.Text;
using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
    [TestFixture]
    public class InteractiveTranslatorTests
    {
        [Test]
        public async Task GetCurrentResults_EmptyPrefix()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("In the beginning the Word already existía ."));
        }

        [Test]
        public async Task GetCurrentResults_AppendCompleteWord()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();
            translator.AppendToPrefix("In ");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("In the beginning the Word already existía ."));
        }

        [Test]
        public async Task GetCurrentResults_AppendPartialWord()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();
            translator.AppendToPrefix("In ");
            translator.AppendToPrefix("t");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("In the beginning the Word already existía ."));
        }

        [Test]
        public async Task GetCurrentResults_RemoveWord()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();
            translator.AppendToPrefix("In the beginning ");
            translator.SetPrefix("In the ");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("In the beginning the Word already existía ."));
        }

        [Test]
        public async Task GetCurrentResults_RemoveAllWords()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();
            translator.AppendToPrefix("In the beginning ");
            translator.SetPrefix("");

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(result.Translation, Is.EqualTo("In the beginning the Word already existía ."));
        }

        [Test]
        public async Task IsSourceSegmentValid_Valid()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();

            Assert.That(translator.IsSegmentValid, Is.True);
        }

        [Test]
        public async Task IsSourceSegmentValid_Invalid()
        {
            var env = new TestEnvironment();
            var sb = new StringBuilder();
            for (int i = 0; i < TranslationConstants.MaxSegmentLength; i++)
                sb.Append("word ");
            sb.Append('.');
            string sourceSegment = sb.ToString();
            env.Engine
                .GetWordGraphAsync(sourceSegment)
                .Returns(
                    Task.FromResult(new WordGraph(WhitespaceTokenizer.Instance.Tokenize(sourceSegment).ToArray()))
                );
            InteractiveTranslator translator = await env.CreateTranslatorAsync(sourceSegment);

            Assert.That(translator.IsSegmentValid, Is.False);
        }

        [Test]
        public async Task Approve_AlignedOnly()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();
            translator.AppendToPrefix("In the beginning ");
            await translator.ApproveAsync(alignedOnly: true);

            await env.Engine.Received().TrainSegmentAsync("En el principio", "In the beginning");

            translator.AppendToPrefix("the Word already existed .");
            await translator.ApproveAsync(alignedOnly: true);

            await env.Engine
                .Received()
                .TrainSegmentAsync(
                    "En el principio la Palabra ya existía .",
                    "In the beginning the Word already existed ."
                );
        }

        [Test]
        public async Task Approve_WholeSourceSegment()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();
            translator.AppendToPrefix("In the beginning ");
            await translator.ApproveAsync(alignedOnly: false);

            await env.Engine
                .Received()
                .TrainSegmentAsync("En el principio la Palabra ya existía .", "In the beginning");
        }

        [Test]
        public async Task GetCurrentResults_MultipleSuggestionsEmptyPrefix()
        {
            var env = new TestEnvironment();
            env.UseSimpleWordGraph();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();

            TranslationResult[] results = translator.GetCurrentResults().Take(2).ToArray();
            Assert.That(results[0].Translation, Is.EqualTo("In the beginning the Word already existía ."));
            Assert.That(results[1].Translation, Is.EqualTo("In the start the Word already existía ."));
        }

        [Test]
        public async Task GetCurrentResults_MultipleSuggestionsNonemptyPrefix()
        {
            var env = new TestEnvironment();
            env.UseSimpleWordGraph();
            InteractiveTranslator translator = await env.CreateTranslatorAsync();
            translator.AppendToPrefix("In the ");

            TranslationResult[] results = translator.GetCurrentResults().Take(2).ToArray();
            Assert.That(results[0].Translation, Is.EqualTo("In the beginning the Word already existía ."));
            Assert.That(results[1].Translation, Is.EqualTo("In the start the Word already existía ."));

            translator.AppendToPrefix("beginning");

            results = translator.GetCurrentResults().Take(2).ToArray();
            Assert.That(results[0].Translation, Is.EqualTo("In the beginning the Word already existía ."));
            Assert.That(results[1].Translation, Is.EqualTo("In the beginning his Word already existía ."));
        }

        private class TestEnvironment
        {
            public static readonly string SourceSegment = "En el principio la Palabra ya existía .";

            private readonly InteractiveTranslatorFactory _factory;

            public TestEnvironment()
            {
                Engine = Substitute.For<IInteractiveTranslationEngine>();

                var wordGraph = new WordGraph(
                    WhitespaceTokenizer.Instance.Tokenize(SourceSegment),
                    new[]
                    {
                        new WordGraphArc(
                            0,
                            1,
                            -22.4162,
                            new[] { "now", "It" },
                            new WordAlignmentMatrix(2, 2) { [0, 1] = true, [1, 0] = true },
                            Range<int>.Create(0, 2),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.00006755903, 0.0116618536 }
                        ),
                        new WordGraphArc(
                            0,
                            2,
                            -23.5761,
                            new[] { "In", "your" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(0, 2),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.355293363, 0.0000941652761 }
                        ),
                        new WordGraphArc(
                            0,
                            3,
                            -11.1167,
                            new[] { "In", "the" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(0, 2),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.355293363, 0.5004668 }
                        ),
                        new WordGraphArc(
                            0,
                            4,
                            -13.7804,
                            new[] { "In" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(0, 1),
                            new[] { TranslationSources.Smt },
                            new[] { 0.355293363 }
                        ),
                        new WordGraphArc(
                            3,
                            5,
                            -12.9695,
                            new[] { "beginning" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(2, 3),
                            new[] { TranslationSources.Smt },
                            new[] { 0.348795831 }
                        ),
                        new WordGraphArc(
                            4,
                            5,
                            -7.68319,
                            new[] { "the", "beginning" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(1, 3),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.5004668, 0.348795831 }
                        ),
                        new WordGraphArc(
                            4,
                            3,
                            -14.4373,
                            new[] { "the" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(1, 2),
                            new[] { TranslationSources.Smt },
                            new[] { 0.5004668 }
                        ),
                        new WordGraphArc(
                            5,
                            6,
                            -19.3042,
                            new[] { "his", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.00347203249, 0.477621228 }
                        ),
                        new WordGraphArc(
                            5,
                            7,
                            -8.49148,
                            new[] { "the", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.346071422, 0.477621228 }
                        ),
                        new WordGraphArc(
                            1,
                            8,
                            -15.2926,
                            new[] { "beginning" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(2, 3),
                            new[] { TranslationSources.Smt },
                            new[] { 0.348795831 }
                        ),
                        new WordGraphArc(
                            2,
                            9,
                            -15.2926,
                            new[] { "beginning" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(2, 3),
                            new[] { TranslationSources.Smt },
                            new[] { 0.348795831 }
                        ),
                        new WordGraphArc(
                            7,
                            10,
                            -14.3453,
                            new[] { "already" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(5, 6),
                            new[] { TranslationSources.Smt },
                            new[] { 0.2259867 }
                        ),
                        new WordGraphArc(
                            8,
                            6,
                            -19.3042,
                            new[] { "his", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.00347203249, 0.477621228 }
                        ),
                        new WordGraphArc(
                            8,
                            7,
                            -8.49148,
                            new[] { "the", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.346071422, 0.477621228 }
                        ),
                        new WordGraphArc(
                            9,
                            6,
                            -19.3042,
                            new[] { "his", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.00347203249, 0.477621228 }
                        ),
                        new WordGraphArc(
                            9,
                            7,
                            -8.49148,
                            new[] { "the", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.346071422, 0.477621228 }
                        ),
                        new WordGraphArc(
                            6,
                            10,
                            -14.0526,
                            new[] { "already" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(5, 6),
                            new[] { TranslationSources.Smt },
                            new[] { 0.2259867 }
                        ),
                        new WordGraphArc(
                            10,
                            11,
                            51.1117,
                            new[] { "existía" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(6, 7),
                            new[] { TranslationSources.None },
                            new[] { 0.0 }
                        ),
                        new WordGraphArc(
                            11,
                            12,
                            -29.0049,
                            new[] { "you", "." },
                            new WordAlignmentMatrix(1, 2) { [0, 1] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.005803475, 0.317073762 }
                        ),
                        new WordGraphArc(
                            11,
                            13,
                            -27.7143,
                            new[] { "to" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt },
                            new[] { 0.038961038 }
                        ),
                        new WordGraphArc(
                            11,
                            14,
                            -30.0868,
                            new[] { ".", "‘" },
                            new WordAlignmentMatrix(1, 2) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.317073762, 0.06190489 }
                        ),
                        new WordGraphArc(
                            11,
                            15,
                            -30.1586,
                            new[] { ".", "he" },
                            new WordAlignmentMatrix(1, 2) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.317073762, 0.06702433 }
                        ),
                        new WordGraphArc(
                            11,
                            16,
                            -28.2444,
                            new[] { ".", "the" },
                            new WordAlignmentMatrix(1, 2) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.317073762, 0.115540564 }
                        ),
                        new WordGraphArc(
                            11,
                            17,
                            -23.8056,
                            new[] { "and" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt },
                            new[] { 0.08047272 }
                        ),
                        new WordGraphArc(
                            11,
                            18,
                            -23.5842,
                            new[] { "the" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt },
                            new[] { 0.09361572 }
                        ),
                        new WordGraphArc(
                            11,
                            19,
                            -18.8988,
                            new[] { "," },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt },
                            new[] { 0.1428188 }
                        ),
                        new WordGraphArc(
                            11,
                            20,
                            -11.9218,
                            new[] { ".", "’" },
                            new WordAlignmentMatrix(1, 2) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.317073762, 0.018057242 }
                        ),
                        new WordGraphArc(
                            11,
                            21,
                            -3.51852,
                            new[] { "." },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt },
                            new[] { 0.317073762 }
                        ),
                    },
                    new[] { 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 },
                    -191.0998
                );

                Engine.GetWordGraphAsync(SourceSegment).Returns(Task.FromResult(wordGraph));

                _factory = new InteractiveTranslatorFactory(Engine);
            }

            public IInteractiveTranslationEngine Engine { get; }

            public void UseSimpleWordGraph()
            {
                var wordGraph = new WordGraph(
                    WhitespaceTokenizer.Instance.Tokenize(SourceSegment).ToArray(),
                    new[]
                    {
                        new WordGraphArc(
                            0,
                            1,
                            -10,
                            new[] { "In", "the", "beginning" },
                            new WordAlignmentMatrix(3, 3)
                            {
                                [0, 0] = true,
                                [1, 1] = true,
                                [2, 2] = true
                            },
                            Range<int>.Create(0, 3),
                            new[] { TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.5, 0.5, 0.5 }
                        ),
                        new WordGraphArc(
                            0,
                            1,
                            -11,
                            new[] { "In", "the", "start" },
                            new WordAlignmentMatrix(3, 3)
                            {
                                [0, 0] = true,
                                [1, 1] = true,
                                [2, 2] = true
                            },
                            Range<int>.Create(0, 3),
                            new[] { TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.5, 0.5, 0.4 }
                        ),
                        new WordGraphArc(
                            1,
                            2,
                            -10,
                            new[] { "the", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.5, 0.5 }
                        ),
                        new WordGraphArc(
                            1,
                            2,
                            -11,
                            new[] { "his", "Word" },
                            new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                            Range<int>.Create(3, 5),
                            new[] { TranslationSources.Smt, TranslationSources.Smt },
                            new[] { 0.4, 0.5 }
                        ),
                        new WordGraphArc(
                            2,
                            3,
                            -10,
                            new[] { "already" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(5, 6),
                            new[] { TranslationSources.Smt },
                            new[] { 0.5 }
                        ),
                        new WordGraphArc(
                            3,
                            4,
                            50,
                            new[] { "existía" },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(6, 7),
                            new[] { TranslationSources.None },
                            new[] { 0.0 }
                        ),
                        new WordGraphArc(
                            4,
                            5,
                            -10,
                            new[] { "." },
                            new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                            Range<int>.Create(7, 8),
                            new[] { TranslationSources.Smt },
                            new[] { 0.5 }
                        ),
                    },
                    new[] { 5 }
                );

                Engine.GetWordGraphAsync(SourceSegment).Returns(Task.FromResult(wordGraph));
            }

            public Task<InteractiveTranslator> CreateTranslatorAsync(string? segment = null)
            {
                if (segment is null)
                    segment = SourceSegment;

                return _factory.CreateAsync(segment);
            }
        }
    }
}
