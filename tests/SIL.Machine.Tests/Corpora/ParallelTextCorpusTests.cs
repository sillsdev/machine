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
            Assert.That(row.SourceRefs.First, Is.EqualTo(expectedRef)); // Only row 1:1 is valid
            Assert.That(row.TargetRefs, Is.EqualTo(new[] { expectedRef }));
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

    private static AlignmentRow AlignmentRow(string textId, object rowRef, params AlignedWordPair[] pairs)
    {
        return new AlignmentRow(textId, rowRef) { AlignedWordPairs = new List<AlignedWordPair>(pairs) };
    }
}
