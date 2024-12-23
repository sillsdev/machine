using System.Text.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

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
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
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
                    TextRow("text1", 3, "target segment 3 .", TextRowFlags.None)
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
                        TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange),
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
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange)
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
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
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
    public void GetRows_OverlappingRangesAndMissingRows()
    {
        var sourceCorpus = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow(
                        "text1",
                        1,
                        "source segment 1 . source segment 2 . source segment 3 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
                    TextRow("text1", 3, flags: TextRowFlags.InRange)
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
                        3,
                        "target segment 3 . target segment 4 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 4, flags: TextRowFlags.InRange)
                }
            )
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
        ParallelTextRow[] rows = parallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(1));
        Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 3, 4 }));
        Assert.That(
            rows[0].SourceSegment,
            Is.EqualTo("source segment 1 . source segment 2 . source segment 3 .".Split())
        );
        Assert.That(rows[0].TargetSegment, Is.EqualTo("target segment 3 . target segment 4 .".Split()));
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
                        TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
                    TextRow(
                        "text1",
                        3,
                        "source segment 3 . source segment 4 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 4, flags: TextRowFlags.InRange)
                }
            )
        );
        var targetCorpus = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "target segment 1 .", TextRowFlags.None),
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
                        TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
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
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 4, flags: TextRowFlags.InRange)
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
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange),
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
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:1", ScrVers.Original), "source chapter one, verse one ."),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:2", ScrVers.Original), "source chapter one, verse two ."),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:3", ScrVers.Original), "source chapter one, verse three .")
                }
            )
        )
        {
            Versification = ScrVers.Original
        };
        var targetCorpus = new DictionaryTextCorpus(
            new MemoryText(
                "MAT",
                new[]
                {
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:1", versification), "target chapter one, verse one ."),
                    TextRow(
                        "MAT",
                        ScriptureRef.Parse("MAT 1:2", versification),
                        "target chapter one, verse two . target chapter one, verse three .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:3", versification), flags: TextRowFlags.InRange),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:4", versification), "target chapter one, verse four .")
                }
            )
        )
        {
            Versification = versification
        };

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
        ParallelTextRow[] rows = parallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(3));
        Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { ScriptureRef.Parse("MAT 1:2", ScrVers.Original) }));
        Assert.That(
            rows[1].TargetRefs,
            Is.EqualTo(
                new[] { ScriptureRef.Parse("MAT 1:2", versification), ScriptureRef.Parse("MAT 1:3", versification) }
            )
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
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:1", ScrVers.Original), "source chapter one, verse one ."),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:2", ScrVers.Original), "source chapter one, verse two ."),
                    TextRow(
                        "MAT",
                        ScriptureRef.Parse("MAT 1:3", ScrVers.Original),
                        "source chapter one, verse three ."
                    ),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:4", ScrVers.Original), "source chapter one, verse four .")
                }
            )
        )
        {
            Versification = ScrVers.Original
        };
        var targetCorpus = new DictionaryTextCorpus(
            new MemoryText(
                "MAT",
                new[]
                {
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:1", versification), "target chapter one, verse one ."),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:2", versification), "target chapter one, verse two ."),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:3", versification), "target chapter one, verse three ."),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:4", versification), "target chapter one, verse four ."),
                    TextRow("MAT", ScriptureRef.Parse("MAT 1:5", versification), "target chapter one, verse five .")
                }
            )
        )
        {
            Versification = versification
        };

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
        ParallelTextRow[] rows = parallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(4));

        Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { ScriptureRef.Parse("MAT 1:2", ScrVers.Original) }));
        Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { ScriptureRef.Parse("MAT 1:3", versification) }));
        Assert.That(rows[1].SourceSegment, Is.EqualTo("source chapter one, verse two .".Split()));
        Assert.That(rows[1].TargetSegment, Is.EqualTo("target chapter one, verse three .".Split()));

        Assert.That(rows[2].SourceRefs, Is.EqualTo(new[] { ScriptureRef.Parse("MAT 1:3", ScrVers.Original) }));
        Assert.That(rows[2].TargetRefs, Is.EqualTo(new[] { ScriptureRef.Parse("MAT 1:2", versification) }));
        Assert.That(rows[2].SourceSegment, Is.EqualTo("source chapter one, verse three .".Split()));
        Assert.That(rows[2].TargetSegment, Is.EqualTo("target chapter one, verse two .".Split()));

        Assert.That(rows[3].SourceRefs, Is.EqualTo(new[] { ScriptureRef.Parse("MAT 1:4", ScrVers.Original) }));
        Assert.That(
            rows[3].TargetRefs,
            Is.EqualTo(
                new[] { ScriptureRef.Parse("MAT 1:4", versification), ScriptureRef.Parse("MAT 1:5", versification) }
            )
        );
        Assert.That(rows[3].SourceSegment, Is.EqualTo("source chapter one, verse four .".Split()));
        Assert.That(
            rows[3].TargetSegment,
            Is.EqualTo("target chapter one, verse four . target chapter one, verse five .".Split())
        );
    }

    [Test]
    public void Count_NoRows()
    {
        var sourceCorpus = new DictionaryTextCorpus();
        var targetCorpus = new DictionaryTextCorpus();
        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);

        Assert.That(parallelCorpus.Count(includeEmpty: true), Is.EqualTo(0));
        Assert.That(parallelCorpus.Count(includeEmpty: false), Is.EqualTo(0));
    }

    [Test]
    public void Count_MissingRow()
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

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);

        Assert.That(parallelCorpus.Count(includeEmpty: true), Is.EqualTo(2));
        Assert.That(parallelCorpus.Count(includeEmpty: false), Is.EqualTo(2));

        parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus) { AllTargetRows = true };

        Assert.That(parallelCorpus.Count(includeEmpty: true), Is.EqualTo(3));
        Assert.That(parallelCorpus.Count(includeEmpty: false), Is.EqualTo(2));
    }

    [Test]
    public void Count_EmptyRow()
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
                    TextRow("text1", 2),
                    TextRow("text1", 3, "target segment 3 .")
                }
            )
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);

        Assert.That(parallelCorpus.Count(includeEmpty: true), Is.EqualTo(3));
        Assert.That(parallelCorpus.Count(includeEmpty: false), Is.EqualTo(2));
    }

    private static TextRow TextRow(
        string textId,
        object rowRef,
        string text = "",
        TextRowFlags flags = TextRowFlags.SentenceStart
    )
    {
        return new TextRow(textId, rowRef)
        {
            Segment = text.Length == 0 ? Array.Empty<string>() : text.Split(),
            Flags = flags
        };
    }

    [Test]
    public void GetRows_DeuterocanonicalBooksFullCoverage()
    {
        var sourceCorpus = new DictionaryTextCorpus(
            new MemoryText("Tobit", new[] { TextRow("Tobit", 1, "source segment 1 .") }),
            new MemoryText("Judith", new[] { TextRow("Judith", 2, "source segment 2 .") }),
            new MemoryText("Wisdom", new[] { TextRow("Wisdom", 3, "source segment 3 .") }),
            new MemoryText("Sirach", new[] { TextRow("Sirach", 4, "source segment 4 .") }),
            new MemoryText("Baruch", new[] { TextRow("Baruch", 5, "source segment 5 .") }),
            new MemoryText("1Maccabees", new[] { TextRow("1Maccabees", 6, "source segment 6 .") }),
            new MemoryText("2Maccabees", new[] { TextRow("2Maccabees", 7, "source segment 7 .") })
        );

        var targetCorpus = new DictionaryTextCorpus(
            new MemoryText("Tobit", new[] { TextRow("Tobit", 1, "target segment 1 .") }),
            new MemoryText("Judith", new[] { TextRow("Judith", 2, "target segment 2 .") }),
            new MemoryText("Wisdom", new[] { TextRow("Wisdom", 3, "target segment 3 .") }),
            new MemoryText("Sirach", new[] { TextRow("Sirach", 4, "target segment 4 .") }),
            new MemoryText("Baruch", new[] { TextRow("Baruch", 5, "target segment 5 .") }),
            new MemoryText("1Maccabees", new[] { TextRow("1Maccabees", 6, "target segment 6 .") }),
            new MemoryText("2Maccabees", new[] { TextRow("2Maccabees", 7, "target segment 7 .") })
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        Assert.That(rows.Length, Is.EqualTo(7), JsonSerializer.Serialize(rows));
        Assert.That(
            rows.Select(r => r.TextId).ToArray(),
            Is.EquivalentTo(new[] { "Tobit", "Judith", "Wisdom", "Sirach", "Baruch", "1Maccabees", "2Maccabees" })
        );
    }

    // [Test]
    // public void GetRows_AllDeuterocanonicalBooks_WithAlignments()
    // {
    //     var deuterocanonicalBooks = new[]
    //     {
    //         "TOB", // Tobit
    //         "JDT", // Judith
    //         "WIS", // Wisdom
    //         "SIR", // Sirach (Ecclesiasticus)
    //         "BAR", // Baruch
    //         "1MA", // 1 Maccabees
    //         "2MA", // 2 Maccabees
    //         "LJE", // Letter of Jeremiah
    //         "S3Y", // Song of Three Young Men
    //         "SUS", // Susanna
    //         "BEL", // Bel and the Dragon
    //     };

    //     // Create source corpus with unique segments for each book
    //     var sourceCorpus = new DictionaryTextCorpus(
    //         deuterocanonicalBooks
    //             .Select(bookId => new MemoryText(
    //                 bookId,
    //                 new[]
    //                 {
    //                     TextRow(
    //                         bookId,
    //                         Array.IndexOf(deuterocanonicalBooks, bookId) + 1,
    //                         $"source segment for {bookId}."
    //                     )
    //                 }
    //             ))
    //             .ToArray()
    //     );

    //     // Create target corpus with matching segments for each book
    //     var targetCorpus = new DictionaryTextCorpus(
    //         deuterocanonicalBooks
    //             .Select(bookId => new MemoryText(
    //                 bookId,
    //                 new[]
    //                 {
    //                     TextRow(
    //                         bookId,
    //                         Array.IndexOf(deuterocanonicalBooks, bookId) + 1,
    //                         $"target segment for {bookId} {Array.IndexOf(deuterocanonicalBooks, bookId) + 1}."
    //                     )
    //                 }
    //             ))
    //             .ToArray()
    //     );

    //     // Create alignment corpus with 1:1 aligned word pairs for each book
    //     var alignments = new DictionaryAlignmentCorpus(
    //         deuterocanonicalBooks
    //             .Select(bookId => new MemoryAlignmentCollection(
    //                 bookId,
    //                 new[]
    //                 {
    //                     AlignmentRow(
    //                         bookId,
    //                         Array.IndexOf(deuterocanonicalBooks, bookId) + 1,
    //                         new AlignedWordPair(0, 0)
    //                     )
    //                 }
    //             ))
    //             .ToArray()
    //     );

    //     var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
    //     ParallelTextRow[] rows = parallelCorpus.ToArray();

    //     // Assert the number of rows matches the number of books
    //     Assert.That(rows.Length, Is.EqualTo(deuterocanonicalBooks.Length));
    //     Assert.That(rows.Select(r => r.TextId).ToArray(), Is.EquivalentTo(deuterocanonicalBooks));

    //     // Verify each row
    //     foreach (var row in rows)
    //     {
    //         Assert.That(row.SourceRefs, Is.EqualTo(new[] { 1 }));
    //         Assert.That(row.TargetRefs, Is.EqualTo(new[] { 1 }));
    //         Assert.That(row.SourceSegment, Is.EqualTo($"source segment for {row.TextId}.".Split()));
    //         Assert.That(row.TargetSegment, Is.EqualTo($"target segment for {row.TextId}.".Split()));
    //         Assert.That(row.AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
    //     }
    // }

    // [Test]
    // public void GetRows_AllDeuterocanonicalBooks_WithAlignments()
    // {
    //     var deuterocanonicalBooks = new[]
    //     {
    //         "TOB",
    //         "JDT",
    //         "WIS",
    //         "SIR",
    //         "BAR",
    //         "1MA",
    //         "2MA",
    //         "LJE",
    //         "S3Y",
    //         "SUS",
    //         "BEL"
    //     };

    //     // Create source corpus with unique segments for each book
    //     var sourceCorpus = new DictionaryTextCorpus(
    //         deuterocanonicalBooks
    //             .Select(
    //                 (bookId) =>
    //                     new MemoryText(
    //                         bookId,
    //                         new[]
    //                         {
    //                             TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"source segment for {bookId}.")
    //                         }
    //                     )
    //             )
    //             .ToArray()
    //     );

    //     // Create target corpus with matching segments for each book
    //     var targetCorpus = new DictionaryTextCorpus(
    //         deuterocanonicalBooks
    //             .Select(
    //                 (bookId, index) =>
    //                     new MemoryText(
    //                         bookId,
    //                         new[]
    //                         {
    //                             TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"target segment for {bookId}.")
    //                         }
    //                     )
    //             )
    //             .ToArray()
    //     );

    //     // Create alignment corpus with 1:1 aligned word pairs for each book
    //     var alignments = new DictionaryAlignmentCorpus(
    //         deuterocanonicalBooks
    //             .Select(
    //                 (bookId, index) =>
    //                     new MemoryAlignmentCollection(
    //                         bookId,
    //                         new[]
    //                         {
    //                             AlignmentRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), new AlignedWordPair(0, 0))
    //                         }
    //                     )
    //             )
    //             .ToArray()
    //     );

    //     var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
    //     ParallelTextRow[] rows = parallelCorpus.ToArray();

    //     // Assert the number of rows matches the number of books
    //     Assert.That(rows.Length, Is.EqualTo(deuterocanonicalBooks.Length));
    //     Assert.That(rows.Select(r => r.TextId).ToArray(), Is.EquivalentTo(deuterocanonicalBooks));

    //     // Verify each row
    //     // foreach (var row in rows)
    //     // {
    //     //     var expectedRef = ScriptureRef.Parse($"{row.TextId} 1:1");
    //     //     Assert.That(row.SourceRefs, Is.EqualTo(new[] { expectedRef }));
    //     //     Assert.That(row.TargetRefs, Is.EqualTo(new[] { expectedRef }));
    //     //     Assert.That(row.SourceSegment, Is.EqualTo(new[] { "source", "segment", "for", row.TextId }));
    //     //     Assert.That(row.TargetSegment, Is.EqualTo(new[] { "target", "segment", "for", row.TextId }));
    //     //     Assert.That(row.AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
    //     // }
    // }

    [Test]
    public void GetRows_AllDeuterocanonicalBooks_WithAlignments()
    {
        var deuterocanonicalBooks = new[]
        {
            "TOB",
            "JDT",
            "WIS",
            "SIR",
            "BAR",
            "1MA",
            "2MA",
            "LJE",
            "S3Y",
            "SUS",
            "BEL"
        };

        // Helper to create MemoryText for source or target corpus
        MemoryText CreateMemoryText(string bookId, string segmentType)
        {
            return new MemoryText(
                bookId,
                new[] { TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"{segmentType} segment for {bookId}.") }
            );
        }

        // Helper to create MemoryAlignmentCollection
        MemoryAlignmentCollection CreateMemoryAlignment(string bookId)
        {
            return new MemoryAlignmentCollection(
                bookId,
                new[] { AlignmentRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), new AlignedWordPair(0, 0)) }
            );
        }

        // Create source corpus
        var sourceCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks.Select(bookId => CreateMemoryText(bookId, "source")).ToArray()
        );

        // Create target corpus
        var targetCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks.Select(bookId => CreateMemoryText(bookId, "target")).ToArray()
        );

        // Create alignment corpus
        var alignments = new DictionaryAlignmentCorpus(deuterocanonicalBooks.Select(CreateMemoryAlignment).ToArray());

        // Combine into parallel corpus
        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        // Assert the number of rows matches the number of books
        Assert.That(rows.Length, Is.EqualTo(deuterocanonicalBooks.Length));
        Assert.That(rows.Select(r => r.TextId).ToArray(), Is.EquivalentTo(deuterocanonicalBooks));

        // Verify each row
        foreach (var row in rows)
        {
            var expectedRef = ScriptureRef.Parse($"{row.TextId} 1:1");
            Assert.That(row.SourceRefs, Is.EqualTo(new[] { expectedRef }));
            Assert.That(row.TargetRefs, Is.EqualTo(new[] { expectedRef }));
            Assert.That(row.SourceSegment, Is.EqualTo(new[] { "source", "segment", "for", row.TextId + "." }));
            Assert.That(row.TargetSegment, Is.EqualTo(new[] { "target", "segment", "for", row.TextId + "." }));
            Assert.That(row.AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
        }
    }

    [Test]
    public void GetRows_MultipleRowsPerBookWithMismatches()
    {
        var deuterocanonicalBooks = new[]
        {
            "TOB",
            "JDT",
            "WIS",
            "SIR",
            "BAR",
            "1MA",
            "2MA",
            "LJE",
            "S3Y",
            "SUS",
            "BEL"
        };

        Versification.Table.Implementation.RemoveAllUnknownVersifications();
        string src = "&MAT 1:2-3 = MAT 1:2\nMAT 1:4 = MAT 1:3\n";
        ScrVers versification;
        using (var reader = new StringReader(src))
        {
            versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
        }

        var sourceCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks
                .Select(bookId => new MemoryText(
                    bookId,
                    new[]
                    {
                        TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"source segment 1 for {bookId}."),
                        TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:2"), $"source segment 2 for {bookId}."),
                    }
                ))
                .ToArray()
        )
        {
            Versification = ScrVers.Original
        };

        var targetCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks
                .Select(bookId => new MemoryText(
                    bookId,
                    new[]
                    {
                        TextRow(
                            bookId,
                            ScriptureRef.Parse($"{bookId} 1:1", versification),
                            $"target segment 1 for {bookId}."
                        )
                        // Missing row 1:2 to simulate mismatch
                    }
                ))
                .ToArray()
        )
        {
            Versification = ScrVers.Original
        };

        // Create alignment corpus aligning only existing rows
        var alignments = new DictionaryAlignmentCorpus(
            deuterocanonicalBooks
                .Select(bookId => new MemoryAlignmentCollection(
                    bookId,
                    new[]
                    {
                        AlignmentRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), new AlignedWordPair(0, 0))
                        // No alignment for 1:2 since it is missing in target
                    }
                ))
                .ToArray()
        );

        // var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        // var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";
        // var backupCorpus1 = new ParatextBackupTextCorpus(fileName1);
        // var backupCorpus2 = new ParatextBackupTextCorpus(fileName2);

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        // parallelCorpus = new ParallelTextCorpus(backupCorpus1, backupCorpus2, alignments) { AllTargetRows = false };
        // rows = parallelCorpus.ToArray();

        // TestContext.WriteLine("Source Corpus Rows:");
        // foreach (var book in sourceCorpus.Texts)
        // {
        //     foreach (var row in book.GetRows())
        //         TestContext.WriteLine($"Book: {book.Id}, Ref: {row.Ref}, Segment: {string.Join(" ", row.Segment)}");
        // }

        // TestContext.WriteLine("Target Corpus Rows:");
        // foreach (var book in targetCorpus.Texts)
        // {
        //     foreach (var row in book.GetRows())
        //         TestContext.WriteLine($"Book: {book.Id}, Ref: {row.Ref}, Segment: {string.Join(" ", row.Segment)}");
        // }

        // TestContext.WriteLine("Alignment Corpus Rows:");
        // foreach (var book in alignments.AlignmentCollections)
        // {
        //     foreach (var row in book.GetRows())
        //     {
        //         TestContext.WriteLine(
        //             $"Book: {book.Id}, Ref: {row.Ref}, Alignment Pairs: {row.AlignedWordPairs.Count}"
        //         );
        //     }
        // }

        // Assert the number of rows matches the number of alignable rows
        // Assert.That(rows.Length, Is.EqualTo(deuterocanonicalBooks.Length)); // One valid row per book
        // Assert.That(rows.Length, Is.EqualTo(11));

        // // Validate each row
        foreach (var row in rows)
        {
            var bookId = row.TextId;
            var expectedRef = ScriptureRef.Parse($"{bookId} 1:1");
            Assert.That(row.SourceRefs.First, Is.InstanceOf<ScriptureRef>());
            Assert.That(expectedRef, Is.InstanceOf<ScriptureRef>());
            Assert.That(expectedRef.CompareTo(row.SourceRefs[0]), Is.EqualTo(0)); // Only row 1:1 is valid
            Assert.That(expectedRef.CompareTo(row.TargetRefs[0]), Is.EqualTo(0)); // Only row 1:1 is valid
            Assert.That(row.SourceSegment, Is.EqualTo(new[] { "source", "segment", "1", "for", bookId + "." }));
            Assert.That(row.TargetSegment, Is.EqualTo(new[] { "target", "segment", "1", "for", bookId + "." }));
            Assert.That(row.AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
        }
    }

    [Test]
    public void GetRows_MultipleRowsWithVariousMismatches()
    {
        var deuterocanonicalBooks = new[]
        {
            "TOB",
            "JDT",
            "WIS",
            "SIR",
            "BAR",
            "LJE",
            "S3Y",
            "SUS",
            "BEL",
            "1MA",
            "2MA"
        };

        var sourceCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks
                .Select(bookId => new MemoryText(
                    bookId,
                    new[]
                    {
                        TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"source segment 1 for {bookId}."),
                        TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:2"), $"source segment 2 for {bookId}.")
                    }
                ))
                .ToArray()
        );

        var targetCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks
                .Select(bookId => new MemoryText(bookId, new[]
                    {
                        TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"target segment 1 for {bookId}."),
                        // Simulating a mismatch by missing a row or adding an extra row
                        bookId == "TOB"
                            ? TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:3"), $"target segment 3 for {bookId}.")
                            : null, // Extra row for TOB
                    }.Where(x => x != null).ToArray() // Filter out nulls for the missing rows
                ))
                .ToArray()
        );

        var alignments = new DictionaryAlignmentCorpus(
            deuterocanonicalBooks
                .Select(bookId => new MemoryAlignmentCollection(
                    bookId,
                    new[]
                    {
                        AlignmentRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), new AlignedWordPair(0, 0)),
                        bookId == "WIS"
                            ? AlignmentRow(bookId, ScriptureRef.Parse($"{bookId} 1:2"), new AlignedWordPair(0, 0))
                            : null // Aligned row exists only for WIS 1:2
                    }
                        .Where(x => x != null)
                        .ToArray()
                ))
                .ToArray()
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        Assert.That(rows.Length, Is.EqualTo(6)); // Only valid alignments are TOB 1:1, JDT 1:1, WIS 1:2, and other books' first rows

        // // Validate each row
        // foreach (var row in rows)
        // {
        //     var bookId = row.TextId;
        //     var expectedRef = ScriptureRef.Parse($"{bookId} 1:1");

        //     // Assert the SourceRefs contains only one ScriptureRef and matches the expected reference
        //     Assert.That(row.SourceRefs.Count, Is.EqualTo(1));
        //     Assert.That(row.SourceRefs.First(), Is.EqualTo(expectedRef));
        //     Assert.That(row.SourceRefs.First(), Is.InstanceOf<ScriptureRef>());

        //     // Assert the Source and Target Segments
        //     Assert.That(row.SourceSegment, Is.EqualTo(new[] { "source", "segment", "1", "for", bookId + "." }));
        //     Assert.That(row.TargetSegment, Is.EqualTo(new[] { "target", "segment", "1", "for", bookId + "." }));

        //     // Assert the Aligned Word Pairs
        //     Assert.That(row.AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
        // }
    }

    [Test]
    [TestCase("TOB", "TOB 1:1", "source segment 1 for TOB.", "target segment 1 for TOB.", Description = "Validate TOB")]
    [TestCase("JDT", "JDT 1:1", "source segment 1 for JDT.", "target segment 1 for JDT.", Description = "Validate JDT")]
    [TestCase("WIS", "WIS 1:1", "source segment 1 for WIS.", "target segment 1 for WIS.", Description = "Validate WIS")]
    [TestCase("SIR", "SIR 1:1", "source segment 1 for SIR.", "target segment 1 for SIR.", Description = "Validate SIR")]
    [TestCase("BAR", "BAR 1:1", "source segment 1 for BAR.", "target segment 1 for BAR.", Description = "Validate BAR")]
    [TestCase("1MA", "1MA 1:1", "source segment 1 for 1MA.", "target segment 1 for 1MA.", Description = "Validate 1MA")]
    [TestCase("2MA", "2MA 1:1", "source segment 1 for 2MA.", "target segment 1 for 2MA.", Description = "Validate 2MA")]
    [TestCase("LJE", "LJE 1:1", "source segment 1 for LJE.", "target segment 1 for LJE.", Description = "Validate LJE")]
    [TestCase("S3Y", "S3Y 1:1", "source segment 1 for S3Y.", "target segment 1 for S3Y.", Description = "Validate S3Y")]
    [TestCase("SUS", "SUS 1:1", "source segment 1 for SUS.", "target segment 1 for SUS.", Description = "Validate SUS")]
    [TestCase("BEL", "BEL 1:1", "source segment 1 for BEL.", "target segment 1 for BEL.", Description = "Validate BEL")]
    public void ValidateParallelCorpusRows(string bookId, string verseRef, string sourceSegment, string targetSegment)
    {
        var deuterocanonicalBooks = new[]
        {
            "TOB",
            "JDT",
            "WIS",
            "SIR",
            "BAR",
            "1MA",
            "2MA",
            "LJE",
            "S3Y",
            "SUS",
            "BEL"
        };

        Versification.Table.Implementation.RemoveAllUnknownVersifications();
        string src = "&MAT 1:2-3 = MAT 1:2\nMAT 1:4 = MAT 1:3\n";
        ScrVers versification;
        using (var reader = new StringReader(src))
        {
            versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
        }

        var sourceCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks
                .Select(bId => new MemoryText(
                    bId,
                    new[]
                    {
                        TextRow(bId, ScriptureRef.Parse($"{bId} 1:1"), $"source segment 1 for {bId}."),
                        TextRow(bId, ScriptureRef.Parse($"{bId} 1:2"), $"source segment 2 for {bId}."),
                    }
                ))
                .ToArray()
        )
        {
            Versification = ScrVers.Original
        };

        var targetCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks
                .Select(bId => new MemoryText(
                    bId,
                    new[]
                    {
                        TextRow(bId, ScriptureRef.Parse($"{bId} 1:1", versification), $"target segment 1 for {bId}.")
                    }
                ))
                .ToArray()
        )
        {
            Versification = ScrVers.Original
        };

        var alignments = new DictionaryAlignmentCorpus(
            deuterocanonicalBooks
                .Select(bId => new MemoryAlignmentCollection(
                    bId,
                    new[] { AlignmentRow(bId, ScriptureRef.Parse($"{bId} 1:1"), new AlignedWordPair(0, 0)) }
                ))
                .ToArray()
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        var row = rows.First(r => r.TextId == bookId);

        Assert.That(row.SourceRefs.First, Is.InstanceOf<ScriptureRef>());
        Assert.That(verseRef, Is.InstanceOf<string>());
        Assert.That(verseRef.CompareTo(row.SourceRefs[0].ToString()), Is.EqualTo(0)); // Only row 1:1 is valid
        Assert.That(verseRef.CompareTo(row.TargetRefs[0].ToString()), Is.EqualTo(0)); // Only row 1:1 is valid
        Assert.That(row.SourceRefs[0].ToString(), Is.EqualTo(verseRef));
        Assert.That(row.TargetRefs[0].ToString(), Is.EqualTo(verseRef));
    }

    [Test]
    [TestCase("TOB", Description = "Check TOB in both source and target corpora")]
    [TestCase("JDT", Description = "Check JDT in both source and target corpora")]
    [TestCase("WIS", Description = "Check WIS in both source and target corpora")]
    [TestCase("SIR", Description = "Check SIR in both source and target corpora")]
    [TestCase("BAR", Description = "Check BAR in both source and target corpora")]
    [TestCase("1MA", Description = "Check 1MA in both source and target corpora")]
    [TestCase("2MA", Description = "Check 2MA in both source and target corpora")]
    [TestCase("LJE", Description = "Check LJE in both source and target corpora")]
    [TestCase("S3Y", Description = "Check S3Y in both source and target corpora")]
    [TestCase("SUS", Description = "Check SUS in both source and target corpora")]
    [TestCase("BEL", Description = "Check BEL in both source and target corpora")]
    public void ValidateBookInBackupCorpus(string bookId)
    {
        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        var backupCorpus1 = new ParatextBackupTextCorpus(fileName1);
        var backupCorpus2 = new ParatextBackupTextCorpus(fileName2);

        // Validate Source Corpus
        Assert.That(
            backupCorpus1.Texts.Any(text => text.Id == bookId),
            Is.True,
            $"Source corpus does not contain the book '{bookId}'."
        );

        // Validate Target Corpus
        Assert.That(
            backupCorpus2.Texts.Any(text => text.Id == bookId),
            Is.True,
            $"Target corpus does not contain the book '{bookId}'."
        );
    }

    // [Test]
    // public void ValidateVersifications()
    // {
    //     var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
    //     var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

    //     // Load the source and target corpora
    //     var sourceCorpus = new ParatextBackupTextCorpus(fileName1);
    //     var targetCorpus = new ParatextBackupTextCorpus(fileName2);

    //     // Get the versifications
    //     var sourceVersification = sourceCorpus.Versification;
    //     var targetVersification = targetCorpus.Versification;

    //     // Assert that they are the same
    //     Assert.That(sourceVersification, Is.Not.Null, "Source corpus versification is null.");
    //     Assert.That(targetVersification, Is.Not.Null, "Target corpus versification is null.");
    //     Assert.That(sourceVersification, Is.EqualTo(targetVersification), "Versifications do not match.");
    // }

    // [Test]
    // [TestCase("TOB", "TOB 1:1", Description = "Validate TOB")]
    // [TestCase("JDT", "JDT 1:1", Description = "Validate JDT")]
    // [TestCase("WIS", "WIS 1:1", Description = "Validate WIS")]
    // [TestCase("SIR", "SIR 1:1", Description = "Validate SIR")]
    // [TestCase("BAR", "BAR 1:1", Description = "Validate BAR")]
    // [TestCase("1MA", "1MA 1:1", Description = "Validate 1MA")]
    // [TestCase("2MA", "2MA 1:1", Description = "Validate 2MA")]
    // [TestCase("LJE", "LJE 1:1", Description = "Validate LJE")]
    // [TestCase("S3Y", "S3Y 1:1", Description = "Validate S3Y")]
    // [TestCase("SUS", "SUS 1:1", Description = "Validate SUS")]
    // [TestCase("BEL", "BEL 1:1", Description = "Validate BEL")]
    // public void ValidateParallelCorpusWithBackup(string bookId, string verseRef)
    // {
    //     var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
    //     var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

    //     var sourceCopus = new ParatextBackupTextCorpus(fileName1);
    //     var targetCorpus = new ParatextBackupTextCorpus(fileName2);

    //     var alignments = new DictionaryAlignmentCorpus(
    //         new[]
    //         {
    //             new MemoryAlignmentCollection(
    //                 bookId,
    //                 new[] { AlignmentRow(bookId, ScriptureRef.Parse(verseRef), new AlignedWordPair(0, 0)) }
    //             )
    //         }
    //     );

    //     var parallelCorpus = new ParallelTextCorpus(sourceCopus, targetCorpus, alignments) { AllTargetRows = true };
    //     ParallelTextRow[] rows = parallelCorpus.ToArray();

    //     var row = rows.First(r => r.TextId == bookId);

    //     Assert.That(row.SourceRefs.First, Is.InstanceOf<ScriptureRef>());
    //     Assert.That(verseRef, Is.InstanceOf<string>());
    //     Assert.That(verseRef.CompareTo(row.SourceRefs[0].ToString()), Is.EqualTo(0));
    //     Assert.That(verseRef.CompareTo(row.TargetRefs[0].ToString()), Is.EqualTo(0));
    //     Assert.That(row.AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
    // }

    [Test]
    [TestCase("TOB", "TOB 1:1", Description = "Validate TOB Source and Target References")]
    [TestCase("JDT", "JDT 1:1", Description = "Validate JDT Source and Target References")]
    [TestCase("WIS", "WIS 1:1", Description = "Validate WIS Source and Target References")]
    [TestCase("SIR", "SIR 1:1", Description = "Validate SIR Source and Target References")]
    [TestCase("BAR", "BAR 1:1", Description = "Validate BAR Source and Target References")]
    [TestCase("1MA", "1MA 1:1", Description = "Validate 1MA Source and Target References")]
    [TestCase("2MA", "2MA 1:1", Description = "Validate 2MA Source and Target References")]
    [TestCase("LJE", "LJE 1:1", Description = "Validate LJE Source and Target References")]
    [TestCase("S3Y", "S3Y 1:1", Description = "Validate S3Y Source and Target References")]
    [TestCase("SUS", "SUS 1:1", Description = "Validate SUS Source and Target References")]
    [TestCase("BEL", "BEL 1:1", Description = "Validate BEL Source and Target References")]
    public void ValidateSourceAndTargetReferencesWithBackup(string bookId, string verseRef)
    {
        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        var sourceCorpus = new ParatextBackupTextCorpus(fileName1);
        var targetCorpus = new ParatextBackupTextCorpus(fileName2);

        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    new[] { AlignmentRow(bookId, ScriptureRef.Parse(verseRef), new AlignedWordPair(0, 0)) }
                )
            }
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        var row = rows.First(r => r.TextId == bookId);

        Assert.That(row.SourceRefs.First, Is.InstanceOf<ScriptureRef>());
        Assert.That(verseRef, Is.InstanceOf<string>());
        Assert.That(verseRef.CompareTo(row.SourceRefs[0].ToString()), Is.EqualTo(0));
        Assert.That(verseRef.CompareTo(row.TargetRefs[0].ToString()), Is.EqualTo(0));
    }

    [Test]
    [TestCase("TOB", "TOB 1:1", Description = "Validate TOB Aligned Word Pairs")]
    [TestCase("JDT", "JDT 1:1", Description = "Validate JDT Aligned Word Pairs")]
    [TestCase("WIS", "WIS 1:1", Description = "Validate WIS Aligned Word Pairs")]
    [TestCase("SIR", "SIR 1:1", Description = "Validate SIR Aligned Word Pairs")]
    [TestCase("BAR", "BAR 1:1", Description = "Validate BAR Aligned Word Pairs")]
    [TestCase("1MA", "1MA 1:1", Description = "Validate 1MA Aligned Word Pairs")]
    [TestCase("2MA", "2MA 1:1", Description = "Validate 2MA Aligned Word Pairs")]
    [TestCase("LJE", "LJE 1:1", Description = "Validate LJE Aligned Word Pairs")]
    [TestCase("S3Y", "S3Y 1:1", Description = "Validate S3Y Aligned Word Pairs")]
    [TestCase("SUS", "SUS 1:1", Description = "Validate SUS Aligned Word Pairs")]
    [TestCase("BEL", "BEL 1:1", Description = "Validate BEL Aligned Word Pairs")]
    public void ValidateAlignedWordPairs(string bookId, string verseRef)
    {
        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        var sourceCorpus = new ParatextBackupTextCorpus(fileName1);
        var targetCorpus = new ParatextBackupTextCorpus(fileName2);

        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    new[] { AlignmentRow(bookId, ScriptureRef.Parse(verseRef), new AlignedWordPair(0, 0)) }
                )
            }
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        var row = rows.First(r => r.TextId == bookId);

        Assert.That(row.AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
    }

    [Test]
    [TestCase(ScrVersType.Original, "TOB", "TOB 1:1", Description = "Validate TOB with Original versification")]
    [TestCase(ScrVersType.Septuagint, "JDT", "JDT 1:1", Description = "Validate JDT with Septuagint versification")]
    [TestCase(ScrVersType.Vulgate, "WIS", "WIS 1:1", Description = "Validate WIS with Vulgate versification")]
    [TestCase(ScrVersType.English, "SIR", "SIR 1:1", Description = "Validate SIR with English versification")]
    [TestCase(
        ScrVersType.RussianProtestant,
        "BAR",
        "BAR 1:1",
        Description = "Validate BAR with Russian Protestant versification"
    )]
    [TestCase(
        ScrVersType.RussianOrthodox,
        "1MA",
        "1MA 1:1",
        Description = "Validate 1MA with Russian Orthodox versification"
    )]
    public void ValidateReferencesWithPredefinedVersifications(
        ScrVersType versificationType,
        string bookId,
        string verseRef
    )
    {
        // Load the predefined versification
        ScrVers versification = versificationType switch
        {
            ScrVersType.Original => ScrVers.Original,
            ScrVersType.Septuagint => ScrVers.Septuagint,
            ScrVersType.Vulgate => ScrVers.Vulgate,
            ScrVersType.English => ScrVers.English,
            ScrVersType.RussianProtestant => ScrVers.RussianProtestant,
            ScrVersType.RussianOrthodox => ScrVers.RussianOrthodox,
            _ => throw new ArgumentOutOfRangeException(nameof(versificationType), "Unsupported versification type.")
        };

        // Define file paths for backup corpora
        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        // Create source and target corpora with the specified versification
        var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
        var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

        // Validate that the specified versification is applied
        Assert.That(sourceCorpus.Versification, Is.EqualTo(versification), "Source corpus versification mismatch.");
        Assert.That(targetCorpus.Versification, Is.EqualTo(versification), "Target corpus versification mismatch.");

        // Create alignment corpus
        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    new[]
                    {
                        AlignmentRow(bookId, ScriptureRef.Parse(verseRef, versification), new AlignedWordPair(0, 0))
                    }
                )
            }
        );

        // Create and validate the parallel corpus
        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        // Validate references for the specific book and verse
        var row = rows.FirstOrDefault(r => r.TextId == bookId);
        Assert.That(row, Is.Not.Null, $"No row found for book {bookId}.");

        // Compare references
        Assert.That(row.SourceRefs[0].ToString(), Is.EqualTo(verseRef), "Source reference mismatch.");
        Assert.That(row.TargetRefs[0].ToString(), Is.EqualTo(verseRef), "Target reference mismatch.");
    }

    [Test]
    // TOB Test Cases
    [TestCase("TOB", "TOB 1:1", ScrVersType.Original, Description = "Validate TOB with Original versification")]
    [TestCase("TOB", "TOB 1:1", ScrVersType.Septuagint, Description = "Validate TOB with Septuagint versification")]
    [TestCase("TOB", "TOB 1:1", ScrVersType.Vulgate, Description = "Validate TOB with Vulgate versification")]
    [TestCase("TOB", "TOB 1:1", ScrVersType.English, Description = "Validate TOB with English versification")]
    [TestCase(
        "TOB",
        "TOB 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate TOB with Russian Protestant versification"
    )]
    [TestCase(
        "TOB",
        "TOB 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate TOB with Russian Orthodox versification"
    )]
    // JDT Test Cases
    [TestCase("JDT", "JDT 1:1", ScrVersType.Original, Description = "Validate JDT with Original versification")]
    [TestCase("JDT", "JDT 1:1", ScrVersType.Septuagint, Description = "Validate JDT with Septuagint versification")]
    [TestCase("JDT", "JDT 1:1", ScrVersType.Vulgate, Description = "Validate JDT with Vulgate versification")]
    [TestCase("JDT", "JDT 1:1", ScrVersType.English, Description = "Validate JDT with English versification")]
    [TestCase(
        "JDT",
        "JDT 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate JDT with Russian Protestant versification"
    )]
    [TestCase(
        "JDT",
        "JDT 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate JDT with Russian Orthodox versification"
    )]
    // WIS Test Cases
    [TestCase("WIS", "WIS 1:1", ScrVersType.Original, Description = "Validate WIS with Original versification")]
    [TestCase("WIS", "WIS 1:1", ScrVersType.Septuagint, Description = "Validate WIS with Septuagint versification")]
    [TestCase("WIS", "WIS 1:1", ScrVersType.Vulgate, Description = "Validate WIS with Vulgate versification")]
    [TestCase("WIS", "WIS 1:1", ScrVersType.English, Description = "Validate WIS with English versification")]
    [TestCase(
        "WIS",
        "WIS 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate WIS with Russian Protestant versification"
    )]
    [TestCase(
        "WIS",
        "WIS 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate WIS with Russian Orthodox versification"
    )]
    // SIR Test Cases
    [TestCase("SIR", "SIR 1:1", ScrVersType.Original, Description = "Validate SIR with Original versification")]
    [TestCase("SIR", "SIR 1:1", ScrVersType.Septuagint, Description = "Validate SIR with Septuagint versification")]
    [TestCase("SIR", "SIR 1:1", ScrVersType.Vulgate, Description = "Validate SIR with Vulgate versification")]
    [TestCase("SIR", "SIR 1:1", ScrVersType.English, Description = "Validate SIR with English versification")]
    [TestCase(
        "SIR",
        "SIR 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate SIR with Russian Protestant versification"
    )]
    [TestCase(
        "SIR",
        "SIR 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate SIR with Russian Orthodox versification"
    )]
    // BAR Test Cases
    [TestCase("BAR", "BAR 1:1", ScrVersType.Original, Description = "Validate BAR with Original versification")]
    [TestCase("BAR", "BAR 1:1", ScrVersType.Septuagint, Description = "Validate BAR with Septuagint versification")]
    [TestCase("BAR", "BAR 1:1", ScrVersType.Vulgate, Description = "Validate BAR with Vulgate versification")]
    [TestCase("BAR", "BAR 1:1", ScrVersType.English, Description = "Validate BAR with English versification")]
    [TestCase(
        "BAR",
        "BAR 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate BAR with Russian Protestant versification"
    )]
    [TestCase(
        "BAR",
        "BAR 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate BAR with Russian Orthodox versification"
    )]
    // 1MA Test Cases
    [TestCase("1MA", "1MA 1:1", ScrVersType.Original, Description = "Validate 1MA with Original versification")]
    [TestCase("1MA", "1MA 1:1", ScrVersType.Septuagint, Description = "Validate 1MA with Septuagint versification")]
    [TestCase("1MA", "1MA 1:1", ScrVersType.Vulgate, Description = "Validate 1MA with Vulgate versification")]
    [TestCase("1MA", "1MA 1:1", ScrVersType.English, Description = "Validate 1MA with English versification")]
    [TestCase(
        "1MA",
        "1MA 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate 1MA with Russian Protestant versification"
    )]
    [TestCase(
        "1MA",
        "1MA 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate 1MA with Russian Orthodox versification"
    )]
    // 2MA Test Cases
    [TestCase("2MA", "2MA 1:1", ScrVersType.Original, Description = "Validate 2MA with Original versification")]
    [TestCase("2MA", "2MA 1:1", ScrVersType.Septuagint, Description = "Validate 2MA with Septuagint versification")]
    [TestCase("2MA", "2MA 1:1", ScrVersType.Vulgate, Description = "Validate 2MA with Vulgate versification")]
    [TestCase("2MA", "2MA 1:1", ScrVersType.English, Description = "Validate 2MA with English versification")]
    [TestCase(
        "2MA",
        "2MA 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate 2MA with Russian Protestant versification"
    )]
    [TestCase(
        "2MA",
        "2MA 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate 2MA with Russian Orthodox versification"
    )]
    // LJE Test Cases
    [TestCase("LJE", "LJE 1:1", ScrVersType.Original, Description = "Validate LJE with Original versification")]
    [TestCase("LJE", "LJE 1:1", ScrVersType.Septuagint, Description = "Validate LJE with Septuagint versification")]
    [TestCase("LJE", "LJE 1:1", ScrVersType.Vulgate, Description = "Validate LJE with Vulgate versification")]
    [TestCase("LJE", "LJE 1:1", ScrVersType.English, Description = "Validate LJE with English versification")]
    [TestCase(
        "LJE",
        "LJE 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate LJE with Russian Protestant versification"
    )]
    [TestCase(
        "LJE",
        "LJE 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate LJE with Russian Orthodox versification"
    )]
    // S3Y Test Cases
    [TestCase("S3Y", "S3Y 1:1", ScrVersType.Original, Description = "Validate S3Y with Original versification")]
    [TestCase("S3Y", "S3Y 1:1", ScrVersType.Septuagint, Description = "Validate S3Y with Septuagint versification")]
    [TestCase("S3Y", "S3Y 1:1", ScrVersType.Vulgate, Description = "Validate S3Y with Vulgate versification")]
    [TestCase("S3Y", "S3Y 1:1", ScrVersType.English, Description = "Validate S3Y with English versification")]
    [TestCase(
        "S3Y",
        "S3Y 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate S3Y with Russian Protestant versification"
    )]
    [TestCase(
        "S3Y",
        "S3Y 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate S3Y with Russian Orthodox versification"
    )]
    // SUS Test Cases
    [TestCase("SUS", "SUS 1:1", ScrVersType.Original, Description = "Validate SUS with Original versification")]
    [TestCase("SUS", "SUS 1:1", ScrVersType.Septuagint, Description = "Validate SUS with Septuagint versification")]
    [TestCase("SUS", "SUS 1:1", ScrVersType.Vulgate, Description = "Validate SUS with Vulgate versification")]
    [TestCase("SUS", "SUS 1:1", ScrVersType.English, Description = "Validate SUS with English versification")]
    [TestCase(
        "SUS",
        "SUS 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate SUS with Russian Protestant versification"
    )]
    [TestCase(
        "SUS",
        "SUS 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate SUS with Russian Orthodox versification"
    )]
    // BEL Test Cases
    [TestCase("BEL", "BEL 1:1", ScrVersType.Original, Description = "Validate BEL with Original versification")]
    [TestCase("BEL", "BEL 1:1", ScrVersType.Septuagint, Description = "Validate BEL with Septuagint versification")]
    [TestCase("BEL", "BEL 1:1", ScrVersType.Vulgate, Description = "Validate BEL with Vulgate versification")]
    [TestCase("BEL", "BEL 1:1", ScrVersType.English, Description = "Validate BEL with English versification")]
    [TestCase(
        "BEL",
        "BEL 1:1",
        ScrVersType.RussianProtestant,
        Description = "Validate BEL with Russian Protestant versification"
    )]
    [TestCase(
        "BEL",
        "BEL 1:1",
        ScrVersType.RussianOrthodox,
        Description = "Validate BEL with Russian Orthodox versification"
    )]
    public void ValidateReferencesWithAllVersifications(string bookId, string verseRef, ScrVersType versificationType)
    {
        var versification = versificationType switch
        {
            ScrVersType.Original => ScrVers.Original,
            ScrVersType.Septuagint => ScrVers.Septuagint,
            ScrVersType.Vulgate => ScrVers.Vulgate,
            ScrVersType.English => ScrVers.English,
            ScrVersType.RussianProtestant => ScrVers.RussianProtestant,
            ScrVersType.RussianOrthodox => ScrVers.RussianOrthodox,
            _ => throw new ArgumentException("Invalid versification type", nameof(versificationType))
        };

        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
        var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

        Assert.That(sourceCorpus.Versification, Is.EqualTo(versification));
        Assert.That(targetCorpus.Versification, Is.EqualTo(versification));

        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    new[]
                    {
                        AlignmentRow(bookId, ScriptureRef.Parse(verseRef, versification), new AlignedWordPair(0, 0))
                    }
                )
            }
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        var row = rows.FirstOrDefault(r => r.TextId == bookId);
        Assert.That(row, Is.Not.Null);
        Assert.That(row.SourceRefs[0].ToString(), Is.EqualTo(verseRef));
        Assert.That(row.TargetRefs[0].ToString(), Is.EqualTo(verseRef));
    }

    public static IEnumerable<TestCaseData> VersificationTestCases()
    {
        var allVersifications = new[]
        {
            ScrVers.Original,
            ScrVers.Septuagint,
            ScrVers.Vulgate,
            ScrVers.English,
            ScrVers.RussianProtestant,
            ScrVers.RussianOrthodox
        };

        var deuterocanonicalBooks = new[]
        {
            "TOB",
            "JDT",
            "WIS",
            "SIR",
            "BAR",
            "1MA",
            "2MA",
            "LJE",
            "S3Y",
            "SUS",
            "BEL"
        };

        foreach (var versification in allVersifications)
        {
            foreach (var book in deuterocanonicalBooks)
            {
                string verseRef = $"{book} 1:1"; // Test with the first verse of each book
                yield return new TestCaseData(versification, book, verseRef).SetName(
                    $"Validate_{book}_with_{versification.Type}_versification"
                );
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(VersificationTestCases))]
    public void ValidateReferencesWithAllVersifications(ScrVers versification, string bookId, string verseRef)
    {
        // Define file paths for backup corpora
        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        // Create source and target corpora with the specified versification
        var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
        var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

        // Validate that the specified versification is applied
        Assert.That(sourceCorpus.Versification, Is.EqualTo(versification), "Source corpus versification mismatch.");
        Assert.That(targetCorpus.Versification, Is.EqualTo(versification), "Target corpus versification mismatch.");

        // Create alignment corpus
        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    new[]
                    {
                        AlignmentRow(bookId, ScriptureRef.Parse(verseRef, versification), new AlignedWordPair(0, 0))
                    }
                )
            }
        );

        // Create and validate the parallel corpus
        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        // Validate references for the specific book and verse
        var row = rows.FirstOrDefault(r => r.TextId == bookId);
        Assert.That(row, Is.Not.Null, $"No row found for book {bookId}.");

        // Compare references
        Assert.That(row.SourceRefs[0].ToString(), Is.EqualTo(verseRef), "Source reference mismatch.");
        Assert.That(row.TargetRefs[0].ToString(), Is.EqualTo(verseRef), "Target reference mismatch.");
    }

    // [Test]
    // [TestCase("TOB", ScrVersType.Original)]
    // [TestCase("TOB", ScrVersType.Septuagint)]
    // [TestCase("TOB", ScrVersType.Vulgate)]
    // [TestCase("TOB", ScrVersType.English)]
    // [TestCase("TOB", ScrVersType.RussianProtestant)]
    // [TestCase("TOB", ScrVersType.RussianOrthodox)]
    // [TestCase("JDT", ScrVersType.Original)]
    // [TestCase("JDT", ScrVersType.Septuagint)]
    // [TestCase("JDT", ScrVersType.Vulgate)]
    // [TestCase("JDT", ScrVersType.English)]
    // [TestCase("JDT", ScrVersType.RussianProtestant)]
    // [TestCase("JDT", ScrVersType.RussianOrthodox)]
    // [TestCase("WIS", ScrVersType.Original)]
    // [TestCase("WIS", ScrVersType.Septuagint)]
    // [TestCase("WIS", ScrVersType.Vulgate)]
    // [TestCase("WIS", ScrVersType.English)]
    // [TestCase("WIS", ScrVersType.RussianProtestant)]
    // [TestCase("WIS", ScrVersType.RussianOrthodox)]
    // [TestCase("SIR", ScrVersType.Original)]
    // [TestCase("SIR", ScrVersType.Septuagint)]
    // [TestCase("SIR", ScrVersType.Vulgate)]
    // [TestCase("SIR", ScrVersType.English)]
    // [TestCase("SIR", ScrVersType.RussianProtestant)]
    // [TestCase("SIR", ScrVersType.RussianOrthodox)]
    // [TestCase("BAR", ScrVersType.Original)]
    // [TestCase("BAR", ScrVersType.Septuagint)]
    // [TestCase("BAR", ScrVersType.Vulgate)]
    // [TestCase("BAR", ScrVersType.English)]
    // [TestCase("BAR", ScrVersType.RussianProtestant)]
    // [TestCase("BAR", ScrVersType.RussianOrthodox)]
    // [TestCase("1MA", ScrVersType.Original)]
    // [TestCase("1MA", ScrVersType.Septuagint)]
    // [TestCase("1MA", ScrVersType.Vulgate)]
    // [TestCase("1MA", ScrVersType.English)]
    // [TestCase("1MA", ScrVersType.RussianProtestant)]
    // [TestCase("1MA", ScrVersType.RussianOrthodox)]
    // [TestCase("2MA", ScrVersType.Original)]
    // [TestCase("2MA", ScrVersType.Septuagint)]
    // [TestCase("2MA", ScrVersType.Vulgate)]
    // [TestCase("2MA", ScrVersType.English)]
    // [TestCase("2MA", ScrVersType.RussianProtestant)]
    // [TestCase("2MA", ScrVersType.RussianOrthodox)]
    // [TestCase("LJE", ScrVersType.Original)]
    // [TestCase("LJE", ScrVersType.Septuagint)]
    // [TestCase("LJE", ScrVersType.Vulgate)]
    // [TestCase("LJE", ScrVersType.English)]
    // [TestCase("LJE", ScrVersType.RussianProtestant)]
    // [TestCase("LJE", ScrVersType.RussianOrthodox)]
    // [TestCase("S3Y", ScrVersType.Original)]
    // [TestCase("S3Y", ScrVersType.Septuagint)]
    // [TestCase("S3Y", ScrVersType.Vulgate)]
    // [TestCase("S3Y", ScrVersType.English)]
    // [TestCase("S3Y", ScrVersType.RussianProtestant)]
    // [TestCase("S3Y", ScrVersType.RussianOrthodox)]
    // [TestCase("SUS", ScrVersType.Original)]
    // [TestCase("SUS", ScrVersType.Septuagint)]
    // [TestCase("SUS", ScrVersType.Vulgate)]
    // [TestCase("SUS", ScrVersType.English)]
    // [TestCase("SUS", ScrVersType.RussianProtestant)]
    // [TestCase("SUS", ScrVersType.RussianOrthodox)]
    // [TestCase("BEL", ScrVersType.Original)]
    // [TestCase("BEL", ScrVersType.Septuagint)]
    // [TestCase("BEL", ScrVersType.Vulgate)]
    // [TestCase("BEL", ScrVersType.English)]
    // [TestCase("BEL", ScrVersType.RussianProtestant)]
    // [TestCase("BEL", ScrVersType.RussianOrthodox)]
    // public void ValidateTextIdAlignmentAndFirstLastVerse(string bookId, ScrVersType versificationType)
    // {
    //     var versification = versificationType switch
    //     {
    //         ScrVersType.Original => ScrVers.Original,
    //         ScrVersType.Septuagint => ScrVers.Septuagint,
    //         ScrVersType.Vulgate => ScrVers.Vulgate,
    //         ScrVersType.English => ScrVers.English,
    //         ScrVersType.RussianProtestant => ScrVers.RussianProtestant,
    //         ScrVersType.RussianOrthodox => ScrVers.RussianOrthodox,
    //         _ => throw new ArgumentException("Invalid versification type", nameof(versificationType))
    //     };

    //     var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
    //     var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

    //     var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
    //     var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

    //     var alignments = new DictionaryAlignmentCorpus(
    //         new[]
    //         {
    //             new MemoryAlignmentCollection(
    //                 bookId,
    //                 Enumerable
    //                     .Range(1, 150) // Simulate 150 verses
    //                     .Select(verseNum =>
    //                         AlignmentRow(
    //                             bookId,
    //                             ScriptureRef.Parse($"{bookId} {verseNum}:1", versification),
    //                             new AlignedWordPair(0, 0)
    //                         )
    //                     )
    //             )
    //         }
    //     );

    //     var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
    //     ParallelTextRow[] rows = parallelCorpus.ToArray();

    //     // Validate filtering by TextId
    //     var filteredRows = rows.Where(r => r.TextId == bookId).ToArray();
    //     Assert.That(filteredRows, Is.Not.Empty);

    //     // Validate first and last verse alignment
    //     var firstRow = filteredRows.FirstOrDefault();
    //     var lastRow = filteredRows.LastOrDefault();
    //     Assert.That(firstRow, Is.Not.Null, "First verse row is null");
    //     Assert.That(lastRow, Is.Not.Null, "Last verse row is null");

    //     Assert.That(firstRow.SourceRefs[0].ToString(), Is.EqualTo($"{bookId} 1:1"));
    //     // Assert.That(lastRow.SourceRefs[0].ToString(), Is.EqualTo($"{filteredRows.Last().SourceRefs[0].ToString()}"));
    //     // Assert.That(lastRow.SourceRefs[0].ToString(), Is.EqualTo($"{bookId} {filteredRows.Length}:1"));
    // }

    // [Test]
    // [TestCase("TOB", ScrVersType.English)]
    // [TestCase("JDT", ScrVersType.English)]
    // [TestCase("WIS", ScrVersType.English)]
    // [TestCase("SIR", ScrVersType.English)]
    // [TestCase("BAR", ScrVersType.English)]
    // [TestCase("1MA", ScrVersType.English)]
    // [TestCase("2MA", ScrVersType.English)]
    // [TestCase("LJE", ScrVersType.English)]
    // [TestCase("S3Y", ScrVersType.English)]
    // [TestCase("SUS", ScrVersType.English)]
    // [TestCase("BEL", ScrVersType.English)]
    // [TestCase("TOB", ScrVersType.Original)]
    // [TestCase("TOB", ScrVersType.Septuagint)]
    // [TestCase("TOB", ScrVersType.Vulgate)]
    // [TestCase("TOB", ScrVersType.English)]
    // [TestCase("TOB", ScrVersType.RussianProtestant)]
    // [TestCase("TOB", ScrVersType.RussianOrthodox)]
    // [TestCase("JDT", ScrVersType.Original)]
    // [TestCase("JDT", ScrVersType.Septuagint)]
    // [TestCase("JDT", ScrVersType.Vulgate)]
    // public void ValidateTextIdAlignmentAndFirstLastVerse_RealVerseCount(string bookId, ScrVersType versificationType)
    // {
    //     var versification = versificationType switch
    //     {
    //         ScrVersType.Original => ScrVers.Original,
    //         ScrVersType.Septuagint => ScrVers.Septuagint,
    //         ScrVersType.Vulgate => ScrVers.Vulgate,
    //         ScrVersType.English => ScrVers.English,
    //         ScrVersType.RussianProtestant => ScrVers.RussianProtestant,
    //         ScrVersType.RussianOrthodox => ScrVers.RussianOrthodox,
    //         _ => throw new ArgumentException("Invalid versification type", nameof(versificationType))
    //     };

    //     var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
    //     var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

    //     var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
    //     var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

    //     var sourceText = sourceCorpus.Texts.FirstOrDefault(t => t.Id == bookId);
    //     Assert.That(sourceText, Is.Not.Null, $"Source text for book {bookId} is null.");

    //     var verseRows = sourceText.GetRows().ToArray();
    //     Assert.That(verseRows, Is.Not.Empty, $"No verses found for book {bookId}.");

    //     var alignments = new DictionaryAlignmentCorpus(
    //         new[]
    //         {
    //             new MemoryAlignmentCollection(
    //                 bookId,
    //                 verseRows.Select(row => AlignmentRow(bookId, row.Ref, new AlignedWordPair(0, 0)))
    //             )
    //         }
    //     );

    //     var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
    //     ParallelTextRow[] rows = parallelCorpus.ToArray();

    //     // Validate filtering by TextId
    //     var filteredRows = rows.Where(r => r.TextId == bookId).ToArray();
    //     Assert.That(filteredRows, Is.Not.Empty);

    //     // Validate first and last verse alignment
    //     var firstRow = filteredRows.FirstOrDefault();
    //     var lastRow = filteredRows.LastOrDefault();
    //     Assert.That(firstRow, Is.Not.Null, "First verse row is null");
    //     Assert.That(lastRow, Is.Not.Null, "Last verse row is null");

    //     Assert.That(firstRow.SourceRefs[0].ToString(), Is.EqualTo($"{bookId} 1:1"), "First verse reference mismatch.");
    //     Assert.That(
    //         lastRow.SourceRefs[0].ToString(),
    //         Is.EqualTo(verseRows.Last().Ref.ToString()),
    //         "Last verse reference mismatch."
    //     );
    // }

    [Test]
    [TestCase("TOB", ScrVersType.Original)]
    [TestCase("TOB", ScrVersType.Septuagint)]
    [TestCase("TOB", ScrVersType.Vulgate)]
    [TestCase("TOB", ScrVersType.English)]
    [TestCase("TOB", ScrVersType.RussianProtestant)]
    [TestCase("TOB", ScrVersType.RussianOrthodox)]
    [TestCase("JDT", ScrVersType.Original)]
    [TestCase("JDT", ScrVersType.Septuagint)]
    [TestCase("JDT", ScrVersType.Vulgate)]
    [TestCase("JDT", ScrVersType.English)]
    [TestCase("JDT", ScrVersType.RussianProtestant)]
    [TestCase("JDT", ScrVersType.RussianOrthodox)]
    [TestCase("WIS", ScrVersType.Original)]
    [TestCase("WIS", ScrVersType.Septuagint)]
    [TestCase("WIS", ScrVersType.Vulgate)]
    [TestCase("WIS", ScrVersType.English)]
    [TestCase("WIS", ScrVersType.RussianProtestant)]
    [TestCase("WIS", ScrVersType.RussianOrthodox)]
    [TestCase("SIR", ScrVersType.Original)]
    [TestCase("SIR", ScrVersType.Septuagint)]
    [TestCase("SIR", ScrVersType.Vulgate)]
    [TestCase("SIR", ScrVersType.English)]
    [TestCase("SIR", ScrVersType.RussianProtestant)]
    [TestCase("SIR", ScrVersType.RussianOrthodox)]
    [TestCase("BAR", ScrVersType.Original)]
    [TestCase("BAR", ScrVersType.Septuagint)]
    [TestCase("BAR", ScrVersType.Vulgate)]
    [TestCase("BAR", ScrVersType.English)]
    [TestCase("BAR", ScrVersType.RussianProtestant)]
    [TestCase("BAR", ScrVersType.RussianOrthodox)]
    [TestCase("1MA", ScrVersType.Original)]
    [TestCase("1MA", ScrVersType.Septuagint)]
    [TestCase("1MA", ScrVersType.Vulgate)]
    [TestCase("1MA", ScrVersType.English)]
    [TestCase("1MA", ScrVersType.RussianProtestant)]
    [TestCase("1MA", ScrVersType.RussianOrthodox)]
    [TestCase("2MA", ScrVersType.Original)]
    [TestCase("2MA", ScrVersType.Septuagint)]
    [TestCase("2MA", ScrVersType.Vulgate)]
    [TestCase("2MA", ScrVersType.English)]
    [TestCase("2MA", ScrVersType.RussianProtestant)]
    [TestCase("2MA", ScrVersType.RussianOrthodox)]
    [TestCase("LJE", ScrVersType.Original)]
    [TestCase("LJE", ScrVersType.Septuagint)]
    [TestCase("LJE", ScrVersType.Vulgate)]
    [TestCase("LJE", ScrVersType.English)]
    [TestCase("LJE", ScrVersType.RussianProtestant)]
    [TestCase("LJE", ScrVersType.RussianOrthodox)]
    [TestCase("S3Y", ScrVersType.Original)]
    [TestCase("S3Y", ScrVersType.Septuagint)]
    [TestCase("S3Y", ScrVersType.Vulgate)]
    [TestCase("S3Y", ScrVersType.English)]
    [TestCase("S3Y", ScrVersType.RussianProtestant)]
    [TestCase("S3Y", ScrVersType.RussianOrthodox)]
    [TestCase("SUS", ScrVersType.Original)]
    [TestCase("SUS", ScrVersType.Septuagint)]
    [TestCase("SUS", ScrVersType.Vulgate)]
    [TestCase("SUS", ScrVersType.English)]
    [TestCase("SUS", ScrVersType.RussianProtestant)]
    [TestCase("SUS", ScrVersType.RussianOrthodox)]
    [TestCase("BEL", ScrVersType.Original)]
    [TestCase("BEL", ScrVersType.Septuagint)]
    [TestCase("BEL", ScrVersType.Vulgate)]
    [TestCase("BEL", ScrVersType.English)]
    [TestCase("BEL", ScrVersType.RussianProtestant)]
    [TestCase("BEL", ScrVersType.RussianOrthodox)]
    public void EnsureFirstAndLastRowExist(string bookId, ScrVersType versificationType)
    {
        var versification = GetVersification(versificationType);

        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
        var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

        var sourceText = sourceCorpus.Texts.FirstOrDefault(t => t.Id == bookId);
        Assert.That(sourceText, Is.Not.Null, $"Source text for book {bookId} is null.");

        var verseRows = sourceText.GetRows().ToArray();
        Assert.That(verseRows, Is.Not.Empty, $"No verses found for book {bookId}.");

        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    verseRows.Select(row => AlignmentRow(bookId, row.Ref, new AlignedWordPair(0, 0)))
                )
            }
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        // Validate filtering by TextId
        var filteredRows = rows.Where(r => r.TextId == bookId).ToArray();
        Assert.That(filteredRows, Is.Not.Empty, $"Filtered rows for book {bookId} are empty.");

        // Validate that first and last rows exist
        var firstRow = filteredRows.FirstOrDefault();
        var lastRow = filteredRows.LastOrDefault();
        Assert.That(firstRow, Is.Not.Null, "First verse row is null.");
        Assert.That(lastRow, Is.Not.Null, "Last verse row is null.");
    }

    [Test]
    [TestCase("TOB", ScrVersType.English)]
    [TestCase("JDT", ScrVersType.English)]
    [TestCase("WIS", ScrVersType.English)]
    [TestCase("SIR", ScrVersType.English)]
    [TestCase("BAR", ScrVersType.English)]
    [TestCase("1MA", ScrVersType.English)]
    [TestCase("2MA", ScrVersType.English)]
    [TestCase("LJE", ScrVersType.English)]
    [TestCase("S3Y", ScrVersType.English)]
    [TestCase("SUS", ScrVersType.English)]
    [TestCase("BEL", ScrVersType.English)]
    public void EnsureFirstAndLastRowAlignment(string bookId, ScrVersType versificationType)
    {
        var versification = GetVersification(versificationType);

        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
        var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

        var sourceText = sourceCorpus.Texts.FirstOrDefault(t => t.Id == bookId);
        Assert.That(sourceText, Is.Not.Null, $"Source text for book {bookId} is null.");

        var verseRows = sourceText.GetRows().ToArray();
        Assert.That(verseRows, Is.Not.Empty, $"No verses found for book {bookId}.");

        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    verseRows.Select(row => AlignmentRow(bookId, row.Ref, new AlignedWordPair(0, 0)))
                )
            }
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        // Validate filtering by TextId
        var filteredRows = rows.Where(r => r.TextId == bookId).ToArray();
        Assert.That(filteredRows, Is.Not.Empty, $"Filtered rows for book {bookId} are empty.");

        // Validate first and last verse alignment
        var firstRow = filteredRows.First();
        var lastRow = filteredRows.Last();
        Assert.That(firstRow.SourceRefs[0].ToString(), Is.EqualTo($"{bookId} 1:1"), "First verse reference mismatch.");
        Assert.That(
            lastRow.SourceRefs[0].ToString(),
            Is.EqualTo(verseRows.Last().Ref.ToString()),
            "Last verse reference mismatch."
        );
    }

    [Test]
    [TestCase("TOB", ScrVersType.Original)]
    [TestCase("TOB", ScrVersType.Septuagint)]
    [TestCase("TOB", ScrVersType.Vulgate)]
    [TestCase("TOB", ScrVersType.English)]
    [TestCase("TOB", ScrVersType.RussianProtestant)]
    [TestCase("TOB", ScrVersType.RussianOrthodox)]
    [TestCase("JDT", ScrVersType.Original)]
    [TestCase("JDT", ScrVersType.Septuagint)]
    [TestCase("JDT", ScrVersType.Vulgate)]
    [TestCase("JDT", ScrVersType.English)]
    [TestCase("JDT", ScrVersType.RussianProtestant)]
    [TestCase("JDT", ScrVersType.RussianOrthodox)]
    public void ValidateVerseAlignmentAndTextExtraction_AllVersifications(string bookId, ScrVersType versificationType)
    {
        var versification = versificationType switch
        {
            ScrVersType.Original => ScrVers.Original,
            ScrVersType.Septuagint => ScrVers.Septuagint,
            ScrVersType.Vulgate => ScrVers.Vulgate,
            ScrVersType.English => ScrVers.English,
            ScrVersType.RussianProtestant => ScrVers.RussianProtestant,
            ScrVersType.RussianOrthodox => ScrVers.RussianOrthodox,
            _ => throw new ArgumentException("Invalid versification type", nameof(versificationType))
        };

        var fileName1 = "/home/mudiaga/Downloads/Source - LAT.zip";
        var fileName2 = "/home/mudiaga/Downloads/Target - DRB.zip";

        var sourceCorpus = new ParatextBackupTextCorpus(fileName1) { Versification = versification };
        var targetCorpus = new ParatextBackupTextCorpus(fileName2) { Versification = versification };

        var sourceText = sourceCorpus.Texts.FirstOrDefault(t => t.Id == bookId);
        var targetText = targetCorpus.Texts.FirstOrDefault(t => t.Id == bookId);

        Assert.That(sourceText, Is.Not.Null, $"Source text for book {bookId} is null.");
        Assert.That(targetText, Is.Not.Null, $"Target text for book {bookId} is null.");

        var sourceVerseRows = sourceText.GetRows().ToArray();
        var targetVerseRows = targetText.GetRows().ToArray();

        Assert.That(sourceVerseRows, Is.Not.Empty, $"No verses found for book {bookId} in source.");
        Assert.That(targetVerseRows, Is.Not.Empty, $"No verses found for book {bookId} in target.");

        var alignments = new DictionaryAlignmentCorpus(
            new[]
            {
                new MemoryAlignmentCollection(
                    bookId,
                    sourceVerseRows.Select(row => AlignmentRow(bookId, row.Ref, new AlignedWordPair(0, 0)))
                )
            }
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments) { AllTargetRows = true };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        // Validate filtering by TextId
        var filteredRows = rows.Where(r => r.TextId == bookId).ToArray();
        Assert.That(filteredRows, Is.Not.Empty, $"Filtered rows for book {bookId} are empty.");

        // Check for versification differences and validate alignment
        foreach (var row in filteredRows)
        {
            var sourceRef = row.SourceRefs.FirstOrDefault();
            var targetRef = row.TargetRefs.FirstOrDefault();

            Assert.That(sourceRef, Is.Not.Null, $"Source reference for row in book {bookId} is null.");
            Assert.That(targetRef, Is.Not.Null, $"Target reference for row in book {bookId} is null.");

            // Retrieve the text for the source and target references
            var sourceTextRow = sourceVerseRows.FirstOrDefault(vr =>
                ScriptureRef.Parse(vr.Ref.ToString()).Equals(ScriptureRef.Parse(sourceRef.ToString()))
            );
            var targetTextRow = targetVerseRows.FirstOrDefault(vr =>
                ScriptureRef.Parse(vr.Ref.ToString()).Equals(ScriptureRef.Parse(targetRef.ToString()))
            );

            Assert.That(sourceTextRow, Is.Not.Null, $"Source text row for reference {sourceRef} is null.");
            Assert.That(targetTextRow, Is.Not.Null, $"Target text row for reference {targetRef} is null.");

            var sourceTextValue = sourceTextRow.Text;
            var targetTextValue = targetTextRow.Text;

            Assert.That(sourceTextValue, Is.Not.Null.And.Not.Empty, $"No text found for source verse {sourceRef}.");
            Assert.That(targetTextValue, Is.Not.Null.And.Not.Empty, $"No text found for target verse {targetRef}.");

            // Ensure text alignment between source and target
            Assert.That(
                !string.IsNullOrEmpty(sourceTextValue) && !string.IsNullOrEmpty(targetTextValue),
                Is.True,
                $"Text mismatch or missing text between source ({sourceRef}) and target ({targetRef})."
            );
        }
    }

    private static ScrVers GetVersification(ScrVersType versificationType)
    {
        return versificationType switch
        {
            ScrVersType.Original => ScrVers.Original,
            ScrVersType.Septuagint => ScrVers.Septuagint,
            ScrVersType.Vulgate => ScrVers.Vulgate,
            ScrVersType.English => ScrVers.English,
            ScrVersType.RussianProtestant => ScrVers.RussianProtestant,
            ScrVersType.RussianOrthodox => ScrVers.RussianOrthodox,
            _ => throw new ArgumentException("Invalid versification type", nameof(versificationType))
        };
    }

    private static AlignmentRow AlignmentRow(string textId, object rowRef, params AlignedWordPair[] pairs)
    {
        return new AlignmentRow(textId, rowRef) { AlignedWordPairs = new List<AlignedWordPair>(pairs) };
    }
}
