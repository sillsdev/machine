using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SIL.Scripture;
using SIL.Scripture.Extensions;

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
    public void GetRows_AdjacentRangesBothTexts_EmptyTargetSegments()
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
                        TextRowFlags.InRange | TextRowFlags.RangeStart
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
                    TextRow("text1", 1, flags: TextRowFlags.InRange | TextRowFlags.RangeStart),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
                    TextRow("text1", 3, flags: TextRowFlags.InRange | TextRowFlags.RangeStart),
                    TextRow("text1", 4, flags: TextRowFlags.InRange)
                }
            )
        );

        var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus)
        {
            AllSourceRows = true,
            AllTargetRows = false
        };
        ParallelTextRow[] rows = parallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(2));
        Assert.That(rows[0].SourceRefs, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(rows[0].TargetRefs, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(rows[0].SourceSegment, Is.EqualTo("source segment 1 . source segment 2 .".Split()));
        Assert.That(rows[0].TargetSegment, Is.Empty);
        Assert.That(rows[1].SourceRefs, Is.EqualTo(new[] { 3, 4 }));
        Assert.That(rows[1].TargetRefs, Is.EqualTo(new[] { 3, 4 }));
        Assert.That(rows[1].SourceSegment, Is.EqualTo("source segment 3 . source segment 4 .".Split()));
        Assert.That(rows[1].TargetSegment, Is.Empty);
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
        DictionaryTextCorpus sourceCorpus = new DictionaryTextCorpus(
            new MemoryText("Tobit", new[] { TextRow("Tobit", 1, "source segment 1 .") }),
            new MemoryText("Judith", new[] { TextRow("Judith", 2, "source segment 2 .") }),
            new MemoryText("Wisdom", new[] { TextRow("Wisdom", 3, "source segment 3 .") }),
            new MemoryText("Sirach", new[] { TextRow("Sirach", 4, "source segment 4 .") }),
            new MemoryText("Baruch", new[] { TextRow("Baruch", 5, "source segment 5 .") }),
            new MemoryText("1Maccabees", new[] { TextRow("1Maccabees", 6, "source segment 6 .") }),
            new MemoryText("2Maccabees", new[] { TextRow("2Maccabees", 7, "source segment 7 .") })
        );

        DictionaryTextCorpus targetCorpus = new DictionaryTextCorpus(
            new MemoryText("Tobit", new[] { TextRow("Tobit", 1, "target segment 1 .") }),
            new MemoryText("Judith", new[] { TextRow("Judith", 2, "target segment 2 .") }),
            new MemoryText("Wisdom", new[] { TextRow("Wisdom", 3, "target segment 3 .") }),
            new MemoryText("Sirach", new[] { TextRow("Sirach", 4, "target segment 4 .") }),
            new MemoryText("Baruch", new[] { TextRow("Baruch", 5, "target segment 5 .") }),
            new MemoryText("1Maccabees", new[] { TextRow("1Maccabees", 6, "target segment 6 .") }),
            new MemoryText("2Maccabees", new[] { TextRow("2Maccabees", 7, "target segment 7 .") })
        );

        ParallelTextCorpus parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        Assert.That(rows.Length, Is.EqualTo(7), JsonSerializer.Serialize(rows));
        Assert.That(
            rows.Select(r => r.TextId).ToArray(),
            Is.EquivalentTo(new[] { "Tobit", "Judith", "Wisdom", "Sirach", "Baruch", "1Maccabees", "2Maccabees" })
        );
    }

    [Test]
    public void GetRows_AllDeuterocanonicalBooks_WithAlignments()
    {
        string[] deuterocanonicalBooks = new[]
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

        MemoryText CreateMemoryText(string bookId, string segmentType)
        {
            return new MemoryText(
                bookId,
                new[] { TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"{segmentType} segment for {bookId}.") }
            );
        }

        MemoryAlignmentCollection CreateMemoryAlignment(string bookId)
        {
            return new MemoryAlignmentCollection(
                bookId,
                new[] { AlignmentRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), new AlignedWordPair(0, 0)) }
            );
        }

        DictionaryTextCorpus sourceCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks.Select(bookId => CreateMemoryText(bookId, "source")).ToArray()
        );

        DictionaryTextCorpus targetCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks.Select(bookId => CreateMemoryText(bookId, "target")).ToArray()
        );

        DictionaryAlignmentCorpus alignments = new DictionaryAlignmentCorpus(
            deuterocanonicalBooks.Select(CreateMemoryAlignment).ToArray()
        );

        ParallelTextCorpus parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments);
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        Assert.That(rows.Length, Is.EqualTo(deuterocanonicalBooks.Length));
        Assert.That(rows.Select(r => r.TextId).ToArray(), Is.EquivalentTo(deuterocanonicalBooks));

        foreach (ParallelTextRow row in rows)
        {
            ScriptureRef? expectedRef = ScriptureRef.Parse($"{row.TextId} 1:1");
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
        string[] deuterocanonicalBooks = new[]
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

        string src = "&MAT 1:2-3 = MAT 1:2\nMAT 1:4 = MAT 1:3\n";
        ScrVers versification;
        if (!Versification.Table.Implementation.Exists("custom"))
        {
            using (var reader = new StringReader(src))
            {
                versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            }
        }

        DictionaryTextCorpus sourceCorpus = new DictionaryTextCorpus(
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

        DictionaryTextCorpus targetCorpus = new DictionaryTextCorpus(
            deuterocanonicalBooks
                .Select(bookId => new MemoryText(
                    bookId,
                    new[]
                    {
                        TextRow(bookId, ScriptureRef.Parse($"{bookId} 1:1"), $"target segment 1 for {bookId}.")
                        // Missing row 1:2 to simulate mismatch
                    }
                ))
                .ToArray()
        )
        {
            Versification = ScrVers.Original
        };

        DictionaryAlignmentCorpus alignments = new DictionaryAlignmentCorpus(
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

        ParallelTextCorpus parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignments)
        {
            AllTargetRows = true
        };
        ParallelTextRow[] rows = parallelCorpus.ToArray();

        string mismatchReport = "";
        foreach (ParallelTextRow row in rows)
        {
            string bookId = row.TextId;
            ScriptureRef expectedRef = ScriptureRef.Parse($"{bookId} 1:1");

            // Handle potential nulls for SourceRefs and TargetRefs
            var sourceRef = row.SourceRefs.FirstOrDefault();
            if (sourceRef == null || expectedRef.CompareTo(sourceRef) != 0)
            {
                mismatchReport += $"Mismatch in SourceRefs for {bookId}. Expected: {expectedRef}, Found: {sourceRef}\n";
            }

            var targetRef = row.TargetRefs.FirstOrDefault();
            if (targetRef == null || expectedRef.CompareTo(targetRef) != 0)
            {
                mismatchReport += $"Mismatch in TargetRefs for {bookId}. Expected: {expectedRef}, Found: {targetRef}\n";
            }

            string[] expectedSourceSegment = new[] { "source", "segment", "1", "for", bookId + "." };
            if (!row.SourceSegment.SequenceEqual(expectedSourceSegment))
            {
                mismatchReport +=
                    $"Mismatch in SourceSegment for {bookId}. Expected: {string.Join(" ", expectedSourceSegment)}, Found: {string.Join(" ", row.SourceSegment)}\n";
            }

            string[] expectedTargetSegment = new[] { "target", "segment", "1", "for", bookId + "." };
            if (!row.TargetSegment.SequenceEqual(expectedTargetSegment))
            {
                mismatchReport +=
                    $"Mismatch in TargetSegment for {bookId}. Expected: {string.Join(" ", expectedTargetSegment)}, Found: {string.Join(" ", row.TargetSegment)}\n";
            }

            AlignedWordPair[] expectedAlignedWordPairs = new[] { new AlignedWordPair(0, 0) };

            if (row.AlignedWordPairs == null || !row.AlignedWordPairs.SequenceEqual(expectedAlignedWordPairs))
            {
                string expectedAlignedWordPairsString = string.Join(
                    ", ",
                    expectedAlignedWordPairs.Select(p => p.ToString())
                );
                string actualAlignedWordPairsString =
                    row.AlignedWordPairs != null
                        ? string.Join(", ", row.AlignedWordPairs.Select(p => p.ToString()))
                        : "null";
                mismatchReport +=
                    $"Mismatch in AlignedWordPairs for {bookId}. Expected: {expectedAlignedWordPairsString}, Found: {actualAlignedWordPairsString}\n";
            }
        }

        int sourceSegmentMismatches = Regex.Matches(mismatchReport, "Mismatch in SourceSegment").Count;
        int alignmentMismatches = Regex.Matches(mismatchReport, "Mismatch in AlignedWordPairs").Count;

        Assert.That(mismatchReport, Is.Not.Null, "There are mismatches that should be caught by the test");
        Assert.That(alignmentMismatches, Is.EqualTo(9)); // expecting 9 mismatches
        Assert.That(sourceSegmentMismatches, Is.EqualTo(9));

        if (!string.IsNullOrEmpty(mismatchReport))
        {
            TestContext.WriteLine("Mismatches found:");
            TestContext.WriteLine(mismatchReport);
        }
        else
        {
            TestContext.WriteLine("No mismatches found. All rows match as expected.");
        }
    }

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
    public void ValidateSourceAndTargetReferencesForDeuterocanonicals(string bookId, string verseRef)
    {
        ParatextTextCorpus sourceCorpus = CorporaTestHelpers.GetDeuterocanonicalSourceCorpus();
        ParatextTextCorpus targetCorpus = CorporaTestHelpers.GetDeuterocanonicalTargetCorpus();
        var parallelCorpus = sourceCorpus.AlignRows(targetCorpus);

        var rows = parallelCorpus.GetRows();

        ParallelTextRow row = rows.First(r => r.TextId == bookId);

        Assert.That(row.SourceRefs.First, Is.InstanceOf<ScriptureRef>());
        Assert.That(row.TargetRefs.First, Is.InstanceOf<ScriptureRef>());

        Assert.That(verseRef, Is.InstanceOf<string>());
        Assert.That(verseRef.CompareTo(row.SourceRefs[0].ToString()), Is.EqualTo(0));
        Assert.That(verseRef.CompareTo(row.TargetRefs[0].ToString()), Is.EqualTo(0));
    }

    [Test]
    [TestCase("TOB", ScrVersType.Original)]
    [TestCase("JDT", ScrVersType.Septuagint)]
    [TestCase("WIS", ScrVersType.Vulgate)]
    [TestCase("SIR", ScrVersType.English)]
    [TestCase("BAR", ScrVersType.RussianProtestant)]
    [TestCase("1MA", ScrVersType.RussianOrthodox)]
    [TestCase("2MA", ScrVersType.English)]
    public void GetVersesInVersification_ButNotInSourceOrTarget(string bookId, ScrVersType versificationType)
    {
        ScrVers versification = GetVersification(versificationType);

        ParatextTextCorpus sourceCorpus = CorporaTestHelpers.GetDeuterocanonicalSourceCorpus();
        ParatextTextCorpus targetCorpus = CorporaTestHelpers.GetDeuterocanonicalTargetCorpus();
        sourceCorpus.Versification = versification;
        targetCorpus.Versification = versification;

        var parallelCorpus = sourceCorpus.AlignRows(targetCorpus);

        IText? sourceText = sourceCorpus.Texts.FirstOrDefault(t => t.Id == bookId);
        IText? targetText = targetCorpus.Texts.FirstOrDefault(t => t.Id == bookId);

        List<string> issues = new List<string>();

        if (sourceText == null)
        {
            issues.Add($"Source text for book {bookId} is null.");
        }
        if (targetText == null)
        {
            issues.Add($"Target text for book {bookId} is null.");
        }

        if (sourceText != null && targetText != null)
        {
            int bookNum = Canon.BookIdToNumber(bookId);

            string[] versificationReferences = ScrVersExtensions
                .GetReferencesForBook(versification, bookNum)
                .Select(row => row.Text)
                .ToArray();

            string[] sourceReferences = sourceText
                .GetRows()
                .Select(row => ((ScriptureRef)row.Ref).VerseRef.Text)
                .ToArray();
            string[] targetReferences = targetText
                .GetRows()
                .Select(row => ((ScriptureRef)row.Ref).VerseRef.Text)
                .ToArray();

            string[] missingInSource = versificationReferences.Where(vr => !sourceReferences.Contains(vr)).ToArray();
            string[] missingInTarget = versificationReferences.Where(vr => !targetReferences.Contains(vr)).ToArray();

            if (missingInSource.Any())
            {
                issues.Add($"Verses missing in source for {bookId} ({versificationType}):");
                issues.AddRange(missingInSource.Select(reference => reference.ToString()));
            }

            if (missingInTarget.Any())
            {
                issues.Add($"Verses missing in target for {bookId} ({versificationType}):");
                issues.AddRange(missingInTarget.Select(reference => reference.ToString()));
            }
        }

        if (issues.Any())
        {
            TestContext.WriteLine("The following issues were encountered:");
            foreach (var issue in issues)
            {
                TestContext.WriteLine(issue);
            }
        }
    }

    [Test]
    [TestCase("TOB", ScrVersType.Original)]
    [TestCase("JDT", ScrVersType.Septuagint)]
    [TestCase("WIS", ScrVersType.Vulgate)]
    [TestCase("SIR", ScrVersType.English)]
    [TestCase("BAR", ScrVersType.RussianProtestant)]
    [TestCase("1MA", ScrVersType.RussianOrthodox)]
    [TestCase("2MA", ScrVersType.English)]
    public void GetVersesInSourceOrTarget_ButNotInVersification(string bookId, ScrVersType versificationType)
    {
        ScrVers versification = GetVersification(versificationType);

        ParatextTextCorpus sourceCorpus = CorporaTestHelpers.GetDeuterocanonicalSourceCorpus();
        ParatextTextCorpus targetCorpus = CorporaTestHelpers.GetDeuterocanonicalTargetCorpus();
        sourceCorpus.Versification = versification;
        targetCorpus.Versification = versification;

        var parallelCorpus = sourceCorpus.AlignRows(targetCorpus);

        List<string> issues = new List<string>();

        IText? sourceText = sourceCorpus.Texts.FirstOrDefault(t => t.Id == bookId);
        if (sourceText == null)
        {
            issues.Add($"Source text for book {bookId} is null.");
        }

        IText? targetText = targetCorpus.Texts.FirstOrDefault(t => t.Id == bookId);
        if (targetText == null)
        {
            issues.Add($"Target text for book {bookId} is null.");
        }

        if (sourceText != null && targetText != null)
        {
            int bookNum = Canon.BookIdToNumber(bookId);

            string[] versificationReferences = ScrVersExtensions
                .GetReferencesForBook(versification, bookNum)
                .Select(row => row.Text)
                .ToArray();

            string[] sourceReferences = sourceText
                .GetRows()
                .Select(row => ((ScriptureRef)row.Ref).VerseRef.Text)
                .ToArray();
            string[] targetReferences = targetText
                .GetRows()
                .Select(row => ((ScriptureRef)row.Ref).VerseRef.Text)
                .ToArray();

            string[] inSourceButNotVersification = sourceReferences
                .Where(sr => !versificationReferences.Contains(sr))
                .ToArray();
            string[] inTargetButNotVersification = targetReferences
                .Where(tr => !versificationReferences.Contains(tr))
                .ToArray();

            if (inSourceButNotVersification.Any())
            {
                issues.Add($"Verses in source but not in versification for {bookId} ({versificationType}):");
                issues.AddRange(inSourceButNotVersification.Select(refText => refText.ToString()));
            }

            if (inTargetButNotVersification.Any())
            {
                issues.Add($"Verses in target but not in versification for {bookId} ({versificationType}):");
                issues.AddRange(inTargetButNotVersification.Select(refText => refText.ToString()));
            }
        }

        if (issues.Any())
        {
            TestContext.WriteLine("The following issues were encountered:");
            foreach (var issue in issues)
            {
                TestContext.WriteLine(issue);
            }
        }

        Assert.Pass("Test completed. See TestContext output for any logged issues.");
    }

    [Test]
    public void ValidateCrossBookMappingsAcrossVersifications()
    {
        Dictionary<string, string> expectedMappings = new Dictionary<string, string>
        {
            { "SUS 1:1", "DAG 13:1" },
            { "SUS 1:2", "DAG 13:2" }
        };

        string source1Text = "Et erat vir habitans in Babylone, et nomen ejus Joakim:";
        string source2Text = "et accepit uxorem nomine Susannam, filiam Helciæ, pulchram nimis, et timentem Deum:";

        Dictionary<ScrVersType, ScrVers> versifications = new Dictionary<ScrVersType, ScrVers>
        {
            { ScrVersType.Original, ScrVers.Original },
            { ScrVersType.English, ScrVers.English }
        };

        ParatextTextCorpus corpus = CorporaTestHelpers.GetDeuterocanonicalSourceCorpus();

        foreach (var versificationType in versifications.Keys)
        {
            ScrVers versification = versifications[versificationType];
            corpus.Versification = versification;

            TestContext.WriteLine($"Validating for versification: {versificationType}");

            foreach (var mapping in expectedMappings)
            {
                ScriptureRef sourceVerse = ScriptureRef.Parse(mapping.Key, versification);
                ScriptureRef targetVerse = ScriptureRef.Parse(mapping.Value, versification);

                // Retrieve text for the source verse
                IText? sourceText = corpus.Texts.FirstOrDefault(t => t.Id == sourceVerse.Book);
                IText? mappedText = corpus.Texts.FirstOrDefault(t => t.Id == targetVerse.Book);

                if (sourceText == null || mappedText == null)
                {
                    TestContext.WriteLine(
                        $"Missing text for book {sourceVerse.Book} in versification {versificationType}."
                    );
                    continue;
                }

                TextRow? sourceRow = sourceText
                    .GetRows()
                    .FirstOrDefault(row => sourceVerse.ToString() == row.Ref.ToString());
                TextRow? targetRow = mappedText
                    .GetRows()
                    .FirstOrDefault(row => targetVerse.ToString() == row.Ref.ToString());

                if (sourceRow == null || targetRow == null)
                {
                    TestContext.WriteLine(
                        $"Missing verse: {sourceVerse} or {targetVerse} in versification {versificationType}."
                    );
                    continue;
                }

                string sourceContent = sourceRow.Text;
                string targetContent = targetRow.Text;

                // Normalize text for comparison
                string[] unwanted = { "÷" };
                sourceContent = CorporaTestHelpers.NormalizeSpaces(
                    CorporaTestHelpers.CleanString(sourceContent, unwanted)
                );
                targetContent = CorporaTestHelpers.NormalizeSpaces(
                    CorporaTestHelpers.CleanString(targetContent, unwanted)
                );

                if (sourceVerse.Verse == "1")
                {
                    Assert.That(sourceContent, Is.EqualTo(source1Text), $"Mismatch in text for {sourceVerse}");
                    Assert.That(targetContent, Is.EqualTo(source1Text), $"Mismatch in text for {targetVerse}");
                }
                else if (sourceVerse.Verse == "2")
                {
                    Assert.That(sourceContent, Is.EqualTo(source2Text), $"Mismatch in text for {sourceVerse}");
                    Assert.That(targetContent, Is.EqualTo(source2Text), $"Mismatch in text for {targetVerse}");
                }
            }
        }
    }

    [Test]
    public void GetDoubleMappingsAcrossVersifications()
    {
        Dictionary<string, string> expectedMappings = new Dictionary<string, string>
        {
            { "DAG 1:1-3", "SUS 1:1-3" },
            { "DAG 1:4-6", "SUS 1:1-3" },
            { "DAG 1:7", "SUS 1:7" },
            { "DAG 1:8", "SUS 1:8" }
        };

        Dictionary<ScrVersType, ScrVers> versifications = new Dictionary<ScrVersType, ScrVers>
        {
            { ScrVersType.Original, ScrVers.Original },
            { ScrVersType.English, ScrVers.English }
        };

        Dictionary<string, HashSet<string>> targetToSourceMap = new Dictionary<string, HashSet<string>>();

        foreach (ScrVersType versificationType in versifications.Keys)
        {
            ScrVers versification = versifications[versificationType];
            TestContext.WriteLine($"Validating for versification: {versificationType}");

            Dictionary<string, string> expandedMappings = CorporaTestHelpers.ExpandVerseMappings(expectedMappings);

            foreach (var mapping in expandedMappings)
            {
                string sourceVerse = mapping.Key;

                IEnumerable<ScriptureRef> targetVerses = CorporaTestHelpers.ExpandVerseRange(
                    mapping.Value,
                    versification
                );

                foreach (ScriptureRef targetVerse in targetVerses)
                {
                    string targetVerseKey = targetVerse.ToString();

                    if (!targetToSourceMap.TryGetValue(targetVerseKey, out HashSet<string>? sourceSet))
                    {
                        sourceSet = new HashSet<string>();
                        targetToSourceMap[targetVerseKey] = sourceSet;
                    }

                    sourceSet.Add(sourceVerse);
                }
            }
        }

        foreach (KeyValuePair<string, HashSet<string>> mapping in targetToSourceMap)
        {
            string targetVerse = mapping.Key;
            HashSet<string> sourceVerses = mapping.Value;

            if (sourceVerses.Count > 1)
            {
                TestContext.WriteLine(
                    $"Double mapping detected for Target {targetVerse}: "
                        + $"Mapped from Source(s) {string.Join(", ", sourceVerses)}"
                );
            }
        }

        Assert.Pass("Test completed");
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
