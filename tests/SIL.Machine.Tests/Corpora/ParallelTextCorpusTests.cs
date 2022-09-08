using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    [TestFixture]
    public class ParallelTextCorpusTests
    {
        [Test]
        public void GetRows_NoRows()
        {
            var sourceCorpus = new DictionaryTextCorpus();
            var targetCorpus = new DictionaryTextCorpus();
            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            Assert.That(parallelCorpus, Is.Empty);
        }

        [Test]
        public void GetRows_NoMissingRows()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 .", isSentenceStart: false),
                        TextRow("text1", 2, "source segment 2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 .", isSentenceStart: false)
                    }
                )
            );
            var alignments = new DictionaryAlignmentCorpus(
                new MemoryAlignmentCollection(
                    "text1",
                    new[]
                    {
                        AlignmentRow("text1", 1, new AlignedWordPair(0, 0)),
                        AlignmentRow("text1", 2, new AlignedWordPair(1, 1)),
                        AlignmentRow("text1", 3, new AlignedWordPair(2, 2))
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
            Assert.That(rows[0].IsSourceSentenceStart, Is.False);
            Assert.That(rows[0].IsTargetSentenceStart, Is.True);
            Assert.That(rows[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
            Assert.That(rows[2].IsSourceSentenceStart, Is.True);
            Assert.That(rows[2].IsTargetSentenceStart, Is.False);
            Assert.That(rows[2].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
        }

        [Test]
        public void GetRows_MissingMiddleTargetRow()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 1, "target segment 1 ."), TextRow("text1", 3, "target segment 3 .") }
                )
            );
            var alignments = new DictionaryAlignmentCorpus(
                new MemoryAlignmentCollection(
                    "text1",
                    new[]
                    {
                        AlignmentRow("text1", 1, new AlignedWordPair(0, 0)),
                        AlignmentRow("text1", 3, new AlignedWordPair(2, 2))
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
            Assert.That(rows[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
            Assert.That(rows[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
        }

        [Test]
        public void GetRows_MissingMiddleSourceRow()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 1, "source segment 1 ."), TextRow("text1", 3, "source segment 3 .") }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );
            var alignments = new DictionaryAlignmentCorpus(
                new MemoryAlignmentCollection(
                    "text1",
                    new[]
                    {
                        AlignmentRow("text1", 1, new AlignedWordPair(0, 0)),
                        AlignmentRow("text1", 3, new AlignedWordPair(2, 2))
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
            Assert.That(rows[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
            Assert.That(rows[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
        }

        [Test]
        public void GetRows_MissingLastTargetRow()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 1, "target segment 1 ."), TextRow("text1", 2, "target segment 2 .") }
                )
            );
            var alignments = new DictionaryAlignmentCorpus(
                new MemoryAlignmentCollection(
                    "text1",
                    new[]
                    {
                        AlignmentRow("text1", 1, new AlignedWordPair(0, 0)),
                        AlignmentRow("text1", 2, new AlignedWordPair(1, 1))
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
            Assert.That(rows[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
            Assert.That(rows[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
        }

        [Test]
        public void GetRows_MissingLastSourceRow()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 1, "source segment 1 ."), TextRow("text1", 2, "source segment 2 .") }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );
            var alignments = new DictionaryAlignmentCorpus(
                new MemoryAlignmentCollection(
                    "text1",
                    new[]
                    {
                        AlignmentRow("text1", 1, new AlignedWordPair(0, 0)),
                        AlignmentRow("text1", 2, new AlignedWordPair(1, 1))
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
            Assert.That(rows[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
            Assert.That(rows[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
        }

        [Test]
        public void GetRows_MissingFirstTargetRow()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 2, "target segment 2 ."), TextRow("text1", 3, "target segment 3 .") }
                )
            );
            var alignments = new DictionaryAlignmentCorpus(
                new MemoryAlignmentCollection(
                    "text1",
                    new[]
                    {
                        AlignmentRow("text1", 2, new AlignedWordPair(1, 1)),
                        AlignmentRow("text1", 3, new AlignedWordPair(2, 2))
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
            Assert.That(rows[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
            Assert.That(rows[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
        }

        [Test]
        public void GetRows_MissingFirstSourceRow()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 2, "source segment 2 ."), TextRow("text1", 3, "source segment 3 .") }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );
            var alignments = new DictionaryAlignmentCorpus(
                new MemoryAlignmentCollection(
                    "text1",
                    new[]
                    {
                        AlignmentRow("text1", 2, new AlignedWordPair(1, 1)),
                        AlignmentRow("text1", 3, new AlignedWordPair(2, 2))
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
            Assert.That(rows[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
            Assert.That(rows[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
        }

        [Test]
        public void GetRows_Range()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow(
                            "text1",
                            2,
                            "source segment 2 . source segment 3 .",
                            isSentenceStart: false,
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 3, isInRange: true),
                        TextRow("text1", 4, "source segment 4 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 ."),
                        TextRow("text1", 4, "target segment 4 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2 . source segment 3 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2 . target segment 3 .".Split()));
            Assert.That(rows[1].IsSourceSentenceStart, Is.False);
            Assert.That(rows[1].IsTargetSentenceStart, Is.True);
        }

        [Test]
        public void GetRows_OverlappingRanges()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow(
                            "text1",
                            2,
                            "source segment 2 . source segment 3 .",
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 3, isInRange: true)
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow(
                            "text1",
                            1,
                            "target segment 1 . target segment 2 .",
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 2, isInRange: true),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(1));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(
                rows[0].SourceSegment,
                Is.EqualTo("source segment 1 . source segment 2 . source segment 3 .".Split())
            );
            Assert.That(
                rows[0].TargetSegment,
                Is.EqualTo("target segment 1 . target segment 2 . target segment 3 .".Split())
            );
            Assert.That(rows[0].IsSourceSentenceStart, Is.True);
            Assert.That(rows[0].IsTargetSentenceStart, Is.True);
        }

        [Test]
        public void GetRows_AdjacentRangesSameText()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow(
                            "text1",
                            1,
                            "source segment 1 . source segment 2 .",
                            isSentenceStart: false,
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 2, isInRange: true),
                        TextRow(
                            "text1",
                            3,
                            "source segment 3 . source segment 4 .",
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 4, isInRange: true)
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 .", isSentenceStart: false),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 ."),
                        TextRow("text1", 4, "target segment 4 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 . source segment 2 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 . target segment 2 .".Split()));
            Assert.That(rows[0].IsSourceSentenceStart, Is.False);
            Assert.That(rows[0].IsTargetSentenceStart, Is.False);
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 . source segment 4 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 3 . target segment 4 .".Split()));
            Assert.That(rows[1].IsSourceSentenceStart, Is.True);
            Assert.That(rows[1].IsTargetSentenceStart, Is.True);
        }

        [Test]
        public void GetRows_AdjacentRangesDifferentTexts()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow(
                            "text1",
                            1,
                            "source segment 1 . source segment 2 .",
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 2, isInRange: true),
                        TextRow("text1", 3, "source segment 3 ."),
                        TextRow("text1", 4, "source segment 4 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow(
                            "text1",
                            3,
                            "target segment 3 . target segment 4 .",
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 4, isInRange: true)
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 . source segment 2 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 . target segment 2 .".Split()));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 . source segment 4 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 3 . target segment 4 .".Split()));
        }

        [Test]
        public void GetRows_AllSourceRows()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2 ."),
                        TextRow("text1", 3, "source segment 3 ."),
                        TextRow("text1", 4, "source segment 4 .")
                    }
                ),
                new MemoryText("text2", new[] { TextRow("text2", 5, "source segment 5 .") }),
                new MemoryText(
                    "text3",
                    new[] { TextRow("text3", 6, "source segment 6 ."), TextRow("text3", 7, "source segment 7 ."), }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 3, "target segment 3 ."),
                        TextRow("text1", 4, "target segment 4 .")
                    }
                ),
                new MemoryText(
                    "text3",
                    new[] { TextRow("text3", 6, "target segment 6 ."), TextRow("text3", 7, "target segment 7 ."), }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus) { AllSourceRows = true };
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(7));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.Empty);
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.Empty);

            Assert.That(rows[4].SourceRefs, Is.EqualTo(new[] { 5 }));
            Assert.That(rows[4].TargetRefs, Is.Empty);
            Assert.That(rows[4].SourceSegment, Is.EqualTo("source segment 5 .".Split()));
            Assert.That(rows[4].TargetSegment, Is.Empty);
        }

        [Test]
        public void GetRows_MissingText()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText("text1", new[] { TextRow("text1", 1, "source segment 1 .") }),
                new MemoryText("text2", new[] { TextRow("text2", 2, "source segment 2 .") }),
                new MemoryText("text3", new[] { TextRow("text3", 3, "source segment 3 .") })
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText("text1", new[] { TextRow("text1", 1, "target segment 1 .") }),
                new MemoryText("text3", new[] { TextRow("text3", 3, "target segment 3 .") })
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(2));
            Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1 }));
            Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
            Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));

            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
        }

        [Test]
        public void GetRows_RangeAllTargetRows()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow(
                            "text1",
                            2,
                            "source segment 2 . source segment 3 .",
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("text1", 3, isInRange: true),
                        TextRow("text1", 4, "source segment 4 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 ."),
                        TextRow("text1", 4, "target segment 4 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus) { AllTargetRows = true };
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(4));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2 . source segment 3 .".Split()));
            Assert.That(rows[1].IsSourceInRange, Is.True);
            Assert.That(rows[1].IsSourceRangeStart, Is.True);
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { 3 }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo(Enumerable.Empty<string>()));
            Assert.That(rows[2].IsSourceInRange, Is.True);
            Assert.That(rows[2].IsSourceRangeStart, Is.False);
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
        }

        [Test]
        public void GetRows_SameRefMiddleManyToMany()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2-1 ."),
                        TextRow("text1", 2, "source segment 2-2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2-1 ."),
                        TextRow("text1", 2, "target segment 2-2 ."),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(6));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
            Assert.That(rows[3].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[3].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[3].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
            Assert.That(rows[3].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
            Assert.That(rows[4].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[4].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[4].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
            Assert.That(rows[4].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
        }

        [Test]
        public void GetGetRows_SameRefMiddleOneToMany()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2-1 ."),
                        TextRow("text1", 2, "target segment 2-2 ."),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus) { AllTargetRows = true };
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(4));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
        }

        [Test]
        public void GetGetRows_SameRefMiddleManyToOne()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2-1 ."),
                        TextRow("text1", 2, "source segment 2-2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2 ."),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus) { AllSourceRows = true };
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(4));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
        }

        [Test]
        public void GetGetRows_SameRefLastOneToMany()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 1, "source segment 1 ."), TextRow("text1", 2, "source segment 2 ."), }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "target segment 1 ."),
                        TextRow("text1", 2, "target segment 2-1 ."),
                        TextRow("text1", 2, "target segment 2-2 ."),
                        TextRow("text1", 3, "target segment 3 .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus) { AllTargetRows = true };
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(4));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
        }

        [Test]
        public void GetGetRows_SameRefLastManyToOne()
        {
            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        TextRow("text1", 1, "source segment 1 ."),
                        TextRow("text1", 2, "source segment 2-1 ."),
                        TextRow("text1", 2, "source segment 2-2 ."),
                        TextRow("text1", 3, "source segment 3 .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[] { TextRow("text1", 1, "target segment 1 ."), TextRow("text1", 2, "target segment 2 ."), }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus) { AllSourceRows = true };
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(4));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { 2 }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
        }

        [Test]
        public void GetGetRows_SameVerseRefOneToMany()
        {
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
            string src = "&MAT 1:2-3 = MAT 1:2\nMAT 1:4 = MAT 1:3\n";
            ScrVers versification;
            using (var reader = new StringReader(src))
            {
                versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            }

            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "MAT",
                    new[]
                    {
                        TextRow("MAT", new VerseRef("MAT 1:1", ScrVers.Original), "source chapter one, verse one ."),
                        TextRow("MAT", new VerseRef("MAT 1:2", ScrVers.Original), "source chapter one, verse two ."),
                        TextRow("MAT", new VerseRef("MAT 1:3", ScrVers.Original), "source chapter one, verse three .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "MAT",
                    new[]
                    {
                        TextRow("MAT", new VerseRef("MAT 1:1", versification), "target chapter one, verse one ."),
                        TextRow(
                            "MAT",
                            new VerseRef("MAT 1:2", versification),
                            "target chapter one, verse two . target chapter one, verse three .",
                            isInRange: true,
                            isRangeStart: true
                        ),
                        TextRow("MAT", new VerseRef("MAT 1:3", versification), isInRange: true),
                        TextRow("MAT", new VerseRef("MAT 1:4", versification), "target chapter one, verse four .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { new VerseRef("MAT 1:2", ScrVers.Original) }));
            Assert.That(
                rows[1].TargetRefs,
                Is.EqualTo(new[] { new VerseRef("MAT 1:2", versification), new VerseRef("MAT 1:3", versification) })
            );
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source chapter one, verse two .".Split()));
            Assert.That(
                rows[1].TargetSegment,
                Is.EqualTo("target chapter one, verse two . target chapter one, verse three .".Split())
            );
        }

        [Test]
        public void GetGetRows_VerseRefOutOfOrder()
        {
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
            string src = "&MAT 1:4-5 = MAT 1:4\nMAT 1:2 = MAT 1:3\nMAT 1:3 = MAT 1:2\n";
            ScrVers versification;
            using (var reader = new StringReader(src))
            {
                versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            }

            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "MAT",
                    new[]
                    {
                        TextRow("MAT", new VerseRef("MAT 1:1", ScrVers.Original), "source chapter one, verse one ."),
                        TextRow("MAT", new VerseRef("MAT 1:2", ScrVers.Original), "source chapter one, verse two ."),
                        TextRow("MAT", new VerseRef("MAT 1:3", ScrVers.Original), "source chapter one, verse three ."),
                        TextRow("MAT", new VerseRef("MAT 1:4", ScrVers.Original), "source chapter one, verse four .")
                    }
                )
            );
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "MAT",
                    new[]
                    {
                        TextRow("MAT", new VerseRef("MAT 1:1", versification), "target chapter one, verse one ."),
                        TextRow("MAT", new VerseRef("MAT 1:2", versification), "target chapter one, verse two ."),
                        TextRow("MAT", new VerseRef("MAT 1:3", versification), "target chapter one, verse three ."),
                        TextRow("MAT", new VerseRef("MAT 1:4", versification), "target chapter one, verse four ."),
                        TextRow("MAT", new VerseRef("MAT 1:5", versification), "target chapter one, verse five .")
                    }
                )
            );

            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
            ParallelTextRow[] rows = parallelCorpus.ToArray();
            Assert.That(rows.Length, Is.EqualTo(4));

            Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { new VerseRef("MAT 1:2", ScrVers.Original) }));
            Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { new VerseRef("MAT 1:3", versification) }));
            Assert.That(rows[1].SourceSegment, Is.EqualTo("source chapter one, verse two .".Split()));
            Assert.That(rows[1].TargetSegment, Is.EqualTo("target chapter one, verse three .".Split()));

            Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { new VerseRef("MAT 1:3", ScrVers.Original) }));
            Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { new VerseRef("MAT 1:2", versification) }));
            Assert.That(rows[2].SourceSegment, Is.EqualTo("source chapter one, verse three .".Split()));
            Assert.That(rows[2].TargetSegment, Is.EqualTo("target chapter one, verse two .".Split()));

            Assert.That(rows[3].SourceRefs, Is.EqualTo(new[] { new VerseRef("MAT 1:4", ScrVers.Original) }));
            Assert.That(
                rows[3].TargetRefs,
                Is.EqualTo(new[] { new VerseRef("MAT 1:4", versification), new VerseRef("MAT 1:5", versification) })
            );
            Assert.That(rows[3].SourceSegment, Is.EqualTo("source chapter one, verse four .".Split()));
            Assert.That(
                rows[3].TargetSegment,
                Is.EqualTo("target chapter one, verse four . target chapter one, verse five .".Split())
            );
        }

        private static TextRow TextRow(
            string textId,
            object rowRef,
            string text = "",
            bool isSentenceStart = true,
            bool isInRange = false,
            bool isRangeStart = false
        )
        {
            return new TextRow(textId, rowRef)
            {
                Segment = text.Length == 0 ? Array.Empty<string>() : text.Split(),
                IsSentenceStart = isSentenceStart,
                IsInRange = isInRange,
                IsRangeStart = isRangeStart
            };
        }

        private static AlignmentRow AlignmentRow(string textId, object rowRef, params AlignedWordPair[] pairs)
        {
            return new AlignmentRow(textId, rowRef) { AlignedWordPairs = new List<AlignedWordPair>(pairs) };
        }
    }
}
