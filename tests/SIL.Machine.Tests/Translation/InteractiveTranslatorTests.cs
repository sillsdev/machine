using System.Collections.Generic;
using NUnit.Framework;
using NSubstitute;
using SIL.Machine.Annotations;
using System.Linq;

namespace SIL.Machine.Translation
{
    [TestFixture]
    public class InteractiveTranslatorTests
    {
        [Test]
        public void GetCurrentResults_EmptyPrefix()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(
                string.Join(" ", result.TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
        }

        [Test]
        public void GetCurrentResults_AppendCompleteWord()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();
            translator.AppendToPrefix("In", true);

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(
                string.Join(" ", result.TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
        }

        [Test]
        public void GetCurrentResults_AppendPartialWord()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();
            translator.AppendToPrefix("In", true);
            translator.AppendToPrefix("t", false);

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(
                string.Join(" ", result.TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
        }

        [Test]
        public void GetCurrentResults_RemoveWord()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();
            translator.AppendToPrefix("In", "the", "beginning");
            translator.SetPrefix(new[] { "In", "the" }, true);

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(
                string.Join(" ", result.TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
        }

        [Test]
        public void GetCurrentResults_RemoveAllWords()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();
            translator.AppendToPrefix("In", "the", "beginning");
            translator.SetPrefix(new string[0], true);

            TranslationResult result = translator.GetCurrentResults().First();
            Assert.That(
                string.Join(" ", result.TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
        }

        [Test]
        public void IsSourceSegmentValid_Valid()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();

            Assert.That(translator.IsSourceSegmentValid, Is.True);
        }

        [Test]
        public void IsSourceSegmentValid_Invalid()
        {
            var env = new TestEnvironment();
            string[] sourceSegment = Enumerable
                .Repeat("word", TranslationConstants.MaxSegmentLength)
                .Concat(new[] { "." })
                .ToArray();
            env.Engine.GetWordGraph(SegmentEqual(sourceSegment)).Returns(new WordGraph());
            InteractiveTranslator translator = env.CreateTranslator(sourceSegment);

            Assert.That(translator.IsSourceSegmentValid, Is.False);
        }

        [Test]
        public void Approve_AlignedOnly()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();
            translator.AppendToPrefix("In", "the", "beginning");
            translator.Approve(alignedOnly: true);

            env.Engine
                .Received()
                .TrainSegment(SegmentEqual("En", "el", "principio"), SegmentEqual("In", "the", "beginning"));

            translator.AppendToPrefix("the", "Word", "already", "existed", ".");
            translator.Approve(alignedOnly: true);

            env.Engine
                .Received()
                .TrainSegment(
                    SegmentEqual("En", "el", "principio", "la", "Palabra", "ya", "existía", "."),
                    SegmentEqual("In", "the", "beginning", "the", "Word", "already", "existed", ".")
                );
        }

        [Test]
        public void Approve_WholeSourceSegment()
        {
            var env = new TestEnvironment();
            InteractiveTranslator translator = env.CreateTranslator();
            translator.AppendToPrefix("In", "the", "beginning");
            translator.Approve(alignedOnly: false);

            env.Engine
                .Received()
                .TrainSegment(
                    SegmentEqual("En", "el", "principio", "la", "Palabra", "ya", "existía", "."),
                    SegmentEqual("In", "the", "beginning")
                );
        }

        [Test]
        public void GetCurrentResults_MultipleSuggestionsEmptyPrefix()
        {
            var env = new TestEnvironment();
            env.UseSimpleWordGraph();
            InteractiveTranslator translator = env.CreateTranslator();

            TranslationResult[] results = translator.GetCurrentResults().Take(2).ToArray();
            Assert.That(
                string.Join(" ", results[0].TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
            Assert.That(
                string.Join(" ", results[1].TargetSegment),
                Is.EqualTo("In the start the Word already existía .")
            );
        }

        [Test]
        public void GetCurrentResults_MultipleSuggestionsNonemptyPrefix()
        {
            var env = new TestEnvironment();
            env.UseSimpleWordGraph();
            InteractiveTranslator translator = env.CreateTranslator();
            translator.AppendToPrefix("In", "the");

            TranslationResult[] results = translator.GetCurrentResults().Take(2).ToArray();
            Assert.That(
                string.Join(" ", results[0].TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
            Assert.That(
                string.Join(" ", results[1].TargetSegment),
                Is.EqualTo("In the start the Word already existía .")
            );

            translator.AppendToPrefix("beginning");

            results = translator.GetCurrentResults().Take(2).ToArray();
            Assert.That(
                string.Join(" ", results[0].TargetSegment),
                Is.EqualTo("In the beginning the Word already existía .")
            );
            Assert.That(
                string.Join(" ", results[1].TargetSegment),
                Is.EqualTo("In the beginning his Word already existía .")
            );
        }

        private static IReadOnlyList<string> SegmentEqual(params string[] segment)
        {
            return Arg.Is<IReadOnlyList<string>>(s => s.SequenceEqual(segment));
        }

        private class TestEnvironment
        {
            public static readonly string[] SourceSegment =
            {
                "En",
                "el",
                "principio",
                "la",
                "Palabra",
                "ya",
                "existía",
                "."
            };

            private readonly ErrorCorrectionModel _ecm;

            public TestEnvironment()
            {
                Engine = Substitute.For<IInteractiveTranslationEngine>();
                _ecm = new ErrorCorrectionModel();

                var wordGraph = new WordGraph(
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

                Engine.GetWordGraph(SegmentEqual(SourceSegment)).Returns(wordGraph);
            }

            public IInteractiveTranslationEngine Engine { get; }

            public void UseSimpleWordGraph()
            {
                var wordGraph = new WordGraph(
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

                Engine.GetWordGraph(SegmentEqual(SourceSegment)).Returns(wordGraph);
            }

            public InteractiveTranslator CreateTranslator(IReadOnlyList<string> segment = null)
            {
                if (segment == null)
                    segment = SourceSegment;

                return InteractiveTranslator.Create(_ecm, Engine, segment);
            }
        }
    }
}
